using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private readonly Dictionary<string, PlcVariable> _aggregateRoots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _aggregateFeedbackAliases = new(StringComparer.Ordinal);

    // Coordinator calls this after LoadInitialPoints, before ResetSimulation.
    private void InitializeAggregatePoints()
    {
        var validatedAliases = SceneAggregateContract.Validate(_definition);
        if (_definition.ValueKind != JsonValueKind.Object || !_definition.TryGetProperty("points", out var points)) return;
        foreach (var point in points.EnumerateArray())
        {
            var type = Text(point, "type", "").ToUpperInvariant();
            if (type is not ("STRUCT" or "ARRAY")) continue;
            var name = Text(point, "name", "");
            var owner = Text(point, "owner", "").ToUpperInvariant();
            if (owner is not ("PC" or "PLC")) throw new ArgumentException($"Aggregate '{name}' requires PC or PLC ownership.");
            var schema = point.TryGetProperty("aggregate", out var metadata)
                ? PlcAggregates.ParseSchema(metadata) : null;
            var root = new PlcVariable(name, type == "STRUCT" ? PlcVariableType.Struct : PlcVariableType.Array,
                owner == "PC" ? PlcVariableRole.Input : PlcVariableRole.Output, point.GetProperty("initial").Clone(), Aggregate: schema);
            var leaves = PlcAggregates.Expand([root]);
            if (!_aggregateRoots.TryAdd(name, root)) throw new ArgumentException($"Duplicate aggregate root '{name}'.");
            _initialPoints[name] = root.InitialValue;
            _pointOwners[name] = owner; _pointTypes[name] = type;
            foreach (var leaf in leaves)
            {
                if (_pointTypes.ContainsKey(leaf.Name)) throw new ArgumentException($"Aggregate leaf overlaps point '{leaf.Name}'.");
                _initialPoints[leaf.Name] = leaf.InitialValue;
                _pointOwners[leaf.Name] = owner;
                _pointTypes[leaf.Name] = leaf.Type.ToString().ToUpperInvariant();
                if (owner == "PLC") _outputPoints.Add(leaf.Name);
            }
        }
        foreach (var alias in validatedAliases) _aggregateFeedbackAliases.Add(alias.Key, alias.Value);
    }

    // Coordinator calls at start ApplyBindings. Root feedback never derives from command bits.
    private void ProjectAggregatePoints()
    {
        if (_aggregateRoots.Count == 0) return;
        foreach (var alias in _aggregateFeedbackAliases) _points[alias.Key] = _points[alias.Value];
        var leaves = PlcAggregates.Expand(_aggregateRoots.Values.ToArray());
        var booleans = leaves.Where(v => v.Type == PlcVariableType.Bool).ToDictionary(v => v.Name, v => AsBool(_points.GetValueOrDefault(v.Name)));
        var numbers = leaves.Where(v => v.Type != PlcVariableType.Bool).ToDictionary(v => v.Name, v => Convert.ToDouble(_points.GetValueOrDefault(v.Name)));
        foreach (var root in PlcAggregates.Reconstruct(_aggregateRoots.Values.ToArray(), booleans, numbers)) _points[root.Key] = root.Value;
        if (_definition.TryGetProperty("motorAggregateCommands", out var motorMode) && motorMode.GetString() == "single")
        {
            _points["motor_effective_enable"] = AsBool(_points.GetValueOrDefault("motor_enable")) || booleans.GetValueOrDefault("motor_command.enable");
            _points["motor_effective_ready"] = AsBool(_points.GetValueOrDefault("record_ready")) || booleans.GetValueOrDefault("motor_command.record_ready");
            if (_sceneRoot.GetNodeOrNull<Node3D>("training_accessory_3")?.FindChild("StaticReadout", true, false) is Label3D readout)
                readout.Text = "RECORD FIXTURE\n" + (booleans.GetValueOrDefault("motor_feedback.record_valid") ? "VALID" : "INVALID");
        }
    }

    public IReadOnlyDictionary<string, object> SampleAggregateSceneInputs() => _aggregateRoots.Values.Where(v => v.Role == PlcVariableRole.Input)
        .ToDictionary(v => v.Name, v => _points[v.Name]!, StringComparer.Ordinal);

    public void SetAggregateSceneInput(string name, object value)
    {
        if (!_aggregateRoots.TryGetValue(name, out var root) || root.Role != PlcVariableRole.Input)
            throw new ArgumentException($"Aggregate '{name}' is not PC-owned feedback.");
        var leaves = PlcAggregates.Expand([root with { InitialValue = value }]);
        foreach (var leaf in leaves) _points[leaf.Name] = leaf.InitialValue;
        // Legacy switches and aggregate feedback refer to the same operator fixture.
        foreach (var alias in _aggregateFeedbackAliases)
            if (leaves.FirstOrDefault(v => v.Name == alias.Key) is { } leaf) _points[alias.Value] = leaf.InitialValue;
        ApplyBindings();
        StateChanged?.Invoke();
    }

    public void CommitAggregateSceneOutputs(IReadOnlyDictionary<string, object> outputs)
    {
        var roots = new List<PlcVariable>();
        foreach (var pair in outputs)
        {
            if (!_aggregateRoots.TryGetValue(pair.Key, out var root) || root.Role != PlcVariableRole.Output)
                throw new ArgumentException($"Aggregate '{pair.Key}' is not a PLC-owned command root.");
            roots.Add(root with { InitialValue = pair.Value });
        }
        var leaves = PlcAggregates.Expand(roots);
        foreach (var leaf in leaves) _points[leaf.Name] = leaf.InitialValue;
        ApplyBindings();
        StateChanged?.Invoke();
    }
}
