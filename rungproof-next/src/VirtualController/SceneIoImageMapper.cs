using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>
/// Converts between scene-owned symbolic point names and controller-owned tag
/// names. This is deliberately a pure mapping boundary: it has no PLC
/// transport dependency and cannot resolve physical addresses.
/// </summary>
public static class SceneIoImageMapper
{
    public static IReadOnlyDictionary<string, string> SampleTextInputs(LadderProgram program, IReadOnlyDictionary<string, string> sceneInputs) =>
        program.Variables.Where(variable => variable.Role == PlcVariableRole.Input && PlcTextValues.IsText(variable.Type)
            && variable.Binding.Length > 0 && sceneInputs.ContainsKey(variable.Binding))
            .ToDictionary(variable => variable.Name, variable => sceneInputs[variable.Binding], StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, string> CommitTextOutputs(LadderProgram program, IReadOnlyDictionary<string, string> outputs) =>
        program.Variables.Where(variable => variable.Role == PlcVariableRole.Output && PlcTextValues.IsText(variable.Type)
            && variable.Binding.Length > 0 && outputs.ContainsKey(variable.Name))
            .ToDictionary(variable => variable.Binding, variable => outputs[variable.Name], StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, object> SampleAggregateInputs(LadderProgram program, IReadOnlyDictionary<string, object> sceneInputs)
    {
        var roots = program.Variables.Where(v => v.Role == PlcVariableRole.Input && PlcAggregates.IsAggregate(v.Type) && v.Binding.Length > 0 && sceneInputs.ContainsKey(v.Binding)).ToArray();
        // Validate the complete shape before handing any values to the scan executor.
        PlcAggregates.Expand(roots.Select(v => v with { InitialValue = sceneInputs[v.Binding] }).ToArray());
        return roots.ToDictionary(v => v.Name, v => sceneInputs[v.Binding], StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, object> CommitAggregateOutputs(LadderProgram program, VirtualControllerSnapshot snapshot) =>
        program.Variables.Where(v => v.Role == PlcVariableRole.Output && PlcAggregates.IsAggregate(v.Type) && v.Binding.Length > 0)
            .ToDictionary(v => v.Binding, v => snapshot.Aggregates[v.Name], StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, bool> SampleBoolInputs(
        LadderProgram program,
        IReadOnlyDictionary<string, bool> sceneInputs)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(sceneInputs);
        var mapped = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var variable in PlcAggregates.Expand(program.Variables))
        {
            if (variable.Role != PlcVariableRole.Input
                || variable.Type != PlcVariableType.Bool
                || string.IsNullOrWhiteSpace(variable.Binding)) continue;
            if (sceneInputs.TryGetValue(variable.Binding, out var value)) mapped[variable.Name] = value;
        }
        return mapped;
    }

    public static IReadOnlyDictionary<string, double> SampleNumericInputs(
        LadderProgram program,
        IReadOnlyDictionary<string, double> sceneInputs)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(sceneInputs);
        var mapped = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var variable in PlcAggregates.Expand(program.Variables))
        {
            if (variable.Role != PlcVariableRole.Input
                || variable.Type is not (PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
                || string.IsNullOrWhiteSpace(variable.Binding)) continue;
            if (sceneInputs.TryGetValue(variable.Binding, out var value)) mapped[variable.Name] = value;
        }
        return mapped;
    }

    public static IReadOnlyDictionary<string, bool> CommitBoolOutputs(
        LadderProgram program,
        IReadOnlyDictionary<string, bool> controllerOutputs)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(controllerOutputs);
        var mapped = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (var variable in PlcAggregates.Expand(program.Variables))
        {
            if (variable.Role != PlcVariableRole.Output
                || variable.Type != PlcVariableType.Bool
                || string.IsNullOrWhiteSpace(variable.Binding)
                || !controllerOutputs.TryGetValue(variable.Name, out var value)) continue;
            mapped[variable.Binding] = value;
        }
        return mapped;
    }

    public static IReadOnlyDictionary<string, double> CommitNumericOutputs(
        LadderProgram program,
        IReadOnlyDictionary<string, double> controllerOutputs)
    {
        ArgumentNullException.ThrowIfNull(program);
        ArgumentNullException.ThrowIfNull(controllerOutputs);
        var mapped = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var variable in PlcAggregates.Expand(program.Variables))
        {
            if (variable.Role != PlcVariableRole.Output
                || variable.Type is not (PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real)
                || string.IsNullOrWhiteSpace(variable.Binding)
                || !controllerOutputs.TryGetValue(variable.Name, out var value)) continue;
            mapped[variable.Binding] = value;
        }
        return mapped;
    }
}
