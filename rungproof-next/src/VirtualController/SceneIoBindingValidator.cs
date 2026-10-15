using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed record SceneIoPoint(
    string Name,
    string Type,
    string Owner,
    string Role,
    string Purpose,
    PlcAggregateSchema? Aggregate = null,
    IReadOnlyList<string>? EnumMembers = null);

/// <summary>
/// Validates the symbolic seam between an offline Ladder program and the
/// currently loaded simulator scene. It has no PLC transport or address model.
/// </summary>
public static class SceneIoBindingValidator
{
    private static readonly HashSet<string> OperatorPulseBindings = new(StringComparer.Ordinal)
    {
        "operator.start",
        "operator.stop",
    };

    public static IReadOnlyList<LadderValidationIssue> Validate(
        LadderProgram program,
        IReadOnlyList<SceneIoPoint> points)
    {
        var issues = new List<LadderValidationIssue>();
        try
        {
            foreach (var point in points.Where(p => p.Type.Equals("STRUCT", StringComparison.OrdinalIgnoreCase) || p.Type.Equals("ARRAY", StringComparison.OrdinalIgnoreCase)))
                PlcAggregates.ValidateSchema(point.Name, point.Type.Equals("STRUCT", StringComparison.OrdinalIgnoreCase) ? PlcVariableType.Struct : PlcVariableType.Array, point.Aggregate);
        }
        catch (ArgumentException exception) { return [new("IO002", "$.scenePoints", exception.Message)]; }
        foreach (var root in program.Variables.Where(v => PlcAggregates.IsAggregate(v.Type) && v.Binding.Length > 0))
        {
            var point = points.FirstOrDefault(p => p.Name == root.Binding);
            if (point is null || !point.Type.Equals(root.Type.ToString(), StringComparison.OrdinalIgnoreCase) || point.Aggregate is null || root.Aggregate is null
                || point.Aggregate.LowerBound != root.Aggregate.LowerBound || point.Aggregate.Length != root.Aggregate.Length || point.Aggregate.ElementType != root.Aggregate.ElementType
                || !(point.Aggregate.Fields ?? []).SequenceEqual(root.Aggregate.Fields ?? []))
                issues.Add(new("IO002", "$.variables", $"Aggregate binding '{root.Binding}' requires the same declared root type and complete schema."));
        }
        if (issues.Count > 0) return issues;
        try { program = program with { Variables = PlcAggregates.Expand(program.Variables) }; }
        catch (ArgumentException exception) { return [new("IO002", "$.variables", exception.Message)]; }
        var expandedPoints = new List<SceneIoPoint>();
        foreach (var point in points)
        {
            if (point.Type.Equals("STRUCT", StringComparison.OrdinalIgnoreCase) && point.Aggregate?.Fields is { } fields)
                expandedPoints.AddRange(fields.Select(field => point with { Name = point.Name + "." + field.Name, Type = field.Type.ToString(), Aggregate = null }));
            else if (point.Type.Equals("ARRAY", StringComparison.OrdinalIgnoreCase) && point.Aggregate is { } array)
            {
                if (array.Length <= 0 || array.Length > 4096 || (long)array.LowerBound + array.Length - 1 > int.MaxValue)
                    return [new("IO002", "$.scenePoints", "Invalid scene ARRAY bounds.")];
                expandedPoints.AddRange(Enumerable.Range(array.LowerBound, array.Length).Select(i => point with { Name = $"{point.Name}[{i}]", Type = array.ElementType.ToString(), Aggregate = null }));
            }
            else expandedPoints.Add(point);
        }
        if (expandedPoints.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != expandedPoints.Count)
            return [new("IO002", "$.scenePoints", "Duplicate or overlapping scene point declarations.")];
        var pointsByName = expandedPoints.ToDictionary(point => point.Name, StringComparer.Ordinal);
        var outputBindings = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < program.Variables.Count; index++)
        {
            var variable = program.Variables[index];
            if (string.IsNullOrWhiteSpace(variable.Binding)) continue;
            var path = $"$.variables[{index}].binding";
            if (OperatorPulseBindings.Contains(variable.Binding))
            {
                if (variable.Type != PlcVariableType.Bool || variable.Role != PlcVariableRole.Input)
                    issues.Add(new("IO002", path,
                        $"Operator pulse binding '{variable.Binding}' requires a BOOL Input tag."));
                continue;
            }
            if (!pointsByName.TryGetValue(variable.Binding, out var point))
            {
                issues.Add(new("IO001", path,
                    $"Scene point '{variable.Binding}' is not declared by the active scene."));
                continue;
            }
            if (variable.Type == PlcVariableType.Enum && !(variable.EnumMembers ?? []).SequenceEqual(point.EnumMembers ?? [], StringComparer.Ordinal))
                issues.Add(new("IO002", path, "ENUM binding requires the same declared member domain."));
            if (!TypesCompatible(variable.Type, point.Type))
            {
                issues.Add(new("IO002", path,
                    $"Tag '{variable.Name}' is {variable.Type.ToString().ToUpperInvariant()} but scene point '{point.Name}' is {point.Type}; BOOL, INT, DINT, and REAL bindings require matching types."));
                continue;
            }
            if (variable.Role == PlcVariableRole.Input
                && !point.Owner.Equals("PC", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new("IO003", path,
                    $"Input tag '{variable.Name}' must bind to a simulator-owned (PC) scene point; '{point.Name}' is owned by {point.Owner}."));
            }
            else if (variable.Role == PlcVariableRole.Output)
            {
                if (!point.Owner.Equals("PLC", StringComparison.OrdinalIgnoreCase))
                    issues.Add(new("IO003", path,
                        $"Output tag '{variable.Name}' must bind to a PLC-owned scene point; '{point.Name}' is owned by {point.Owner}."));
                else if (!outputBindings.Add(point.Name))
                    issues.Add(new("IO004", path,
                        $"PLC-owned scene point '{point.Name}' is already driven by another output tag."));
            }
            else if (variable.Role == PlcVariableRole.Memory)
            {
                issues.Add(new("IO003", path,
                    $"Memory tag '{variable.Name}' cannot bind to scene I/O."));
            }
        }
        return issues;
    }

    public static bool TypesCompatible(PlcVariableType variableType, string pointType) => variableType switch
    {
        PlcVariableType.Bool => pointType.Equals("BOOL", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.Int => pointType.Equals("INT", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.DInt => pointType.Equals("DINT", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.Real => pointType.Equals("REAL", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.Struct => pointType.Equals("STRUCT", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.Array => pointType.Equals("ARRAY", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.String => pointType.Equals("STRING", StringComparison.OrdinalIgnoreCase),
        PlcVariableType.Enum => pointType.Equals("ENUM", StringComparison.OrdinalIgnoreCase),
        _ => false,
    };
}
