using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public sealed class SceneIoTableConflictException(string binding, int declarationCount)
    : InvalidOperationException($"Scene I/O binding '{binding}' has {declarationCount} tag declarations; resolve the duplicate before migrating this project.")
{
    public string Binding { get; } = binding;
    public int DeclarationCount { get; } = declarationCount;
}

/// <summary>
/// Creates an exercise project from declared symbolic I/O. It does not invent
/// a solution, translate scene reference rules into PLC code, or coerce types
/// that the offline controller cannot execute.
/// </summary>
public static class SceneLadderProject
{
    public const string SceneIoTagTableName = "SceneIO";

    public static LadderEditorDocument Create(string sceneId, string sceneName,
        IReadOnlyList<SceneIoPoint> points, IReadOnlyDictionary<string, object?> initialValues,
        out IReadOnlyList<SceneIoPoint> unsupportedPoints)
    {
        var document = new LadderEditorDocument();
        document.ResetProject("exercise-" + sceneId, "Main", TimeSpan.FromMilliseconds(20));
        document.Name = sceneName;
        document.SourceSceneId = sceneId;
        EnsureSceneIoTable(document, points, initialValues, out unsupportedPoints);
        // New exercise projects start with the scene's declared points in the
        // default watch table. Migrations of existing drafts never rewrite it.
        foreach (var tag in document.Tags.Where(tag => tag.Role is PlcVariableRole.Input or PlcVariableRole.Output))
            if (!document.WatchVariables.Contains(tag.Name, StringComparer.Ordinal))
                document.WatchVariables.Add(tag.Name);
        return document;
    }

    /// <summary>
    /// Adds the scene's declared PC/PLC points to its SceneIO tag table.
    /// Existing declarations are reused by binding so a restored draft keeps
    /// its symbol and ID. Scene bindings are normalized to SceneIO and the
    /// active scene contract is authoritative for type, direction, and schema.
    /// This is safe to call whenever a cached scene draft opens.
    /// </summary>
    public static void EnsureSceneIoTable(LadderEditorDocument document,
        IReadOnlyList<SceneIoPoint> points, IReadOnlyDictionary<string, object?> initialValues,
        out IReadOnlyList<SceneIoPoint> unsupportedPoints)
    {
        var sceneBindings = points.Select(point => point.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var point in points.Where(point => sceneBindings.Contains(point.Name)))
        {
            var declarations = document.Tags.Count(tag => tag.Binding.Equals(point.Name, StringComparison.Ordinal));
            if (declarations > 1) throw new SceneIoTableConflictException(point.Name, declarations);
        }
        EnsureCanonicalSceneIoTable(document);
        PlaceBoundIoTagsInSceneIo(document, sceneBindings);

        var unsupported = new List<SceneIoPoint>();
        foreach (var point in points)
        {
            var input = point.Owner.Equals("PC", StringComparison.OrdinalIgnoreCase);
            var output = point.Owner.Equals("PLC", StringComparison.OrdinalIgnoreCase);
            if (!input && !output) continue; // Internal plant state has no PLC binding.
            var type = point.Type.ToUpperInvariant() switch
            {
                "BOOL" => (PlcVariableType?)PlcVariableType.Bool,
                "INT" => PlcVariableType.Int,
                "DINT" => PlcVariableType.DInt,
                "REAL" => PlcVariableType.Real,
                "STRUCT" => PlcVariableType.Struct,
                "ARRAY" => PlcVariableType.Array,
                "STRING" => PlcVariableType.String,
                "ENUM" => PlcVariableType.Enum,
                _ => null,
            };
            if (type is null)
            {
                unsupported.Add(point);
                continue;
            }
            if (PlcAggregates.IsAggregate(type.Value)
                && (point.Aggregate is null || initialValues.GetValueOrDefault(point.Name) is null))
            {
                unsupported.Add(point);
                continue;
            }

            var role = input ? PlcVariableRole.Input : PlcVariableRole.Output;
            var existingByBinding = document.Tags.FindIndex(tag =>
                tag.Binding.Equals(point.Name, StringComparison.Ordinal));
            if (existingByBinding >= 0)
            {
                var existing = document.Tags[existingByBinding];
                var migratedInitial = NormalizeInitialValue(type.Value, initialValues.GetValueOrDefault(point.Name));
                // The scene I/O contract is authoritative for bound scene tags.
                // Older project files may contain a stale role/type/table; repair
                // that declaration in place so symbols, IDs, and references survive.
                document.Tags[existingByBinding] = existing with
                {
                    Role = role,
                    Type = type.Value,
                    InitialValue = existing.Type == type.Value
                        ? ExistingInitialValue(type.Value, existing.InitialValue, point, migratedInitial)
                        : migratedInitial ?? DefaultInitialValue(type.Value, point),
                    Aggregate = PlcAggregates.IsAggregate(type.Value) ? point.Aggregate : null,
                    EnumMembers = type.Value == PlcVariableType.Enum ? point.EnumMembers : null,
                    TagTable = SceneIoTagTableName,
                    Comment = string.IsNullOrWhiteSpace(existing.Comment) ? point.Purpose : existing.Comment,
                };
                continue;
            }

            var initial = NormalizeInitialValue(type.Value, initialValues.GetValueOrDefault(point.Name));
            if (PlcAggregates.IsAggregate(type.Value))
            {
                if (point.Aggregate is null || initial is null) throw new ArgumentException($"Aggregate scene point '{point.Name}' requires schema and initial data.");
                document.AddAggregateTag(UniqueSceneSymbol(document, point.Name), role, type.Value, point.Aggregate, initial,
                    point.Name, SceneIoTagTableName, comment: point.Purpose);
            }
            else
            {
                var symbol = UniqueSceneSymbol(document, point.Name);
                document.AddTag(symbol, role, point.Name, type.Value, initial,
                    enumMembers: point.EnumMembers, tagTable: SceneIoTagTableName, comment: point.Purpose);
            }
        }
        unsupportedPoints = unsupported;
    }

    /// <summary>Places already-bound scene I/O declarations in the scene table.</summary>
    public static void PlaceBoundIoTagsInSceneIo(LadderEditorDocument document,
        IReadOnlySet<string>? allowedSceneBindings = null)
    {
        EnsureCanonicalSceneIoTable(document);

        for (var index = 0; index < document.Tags.Count; index++)
        {
            var tag = document.Tags[index];
            var operatorPulse = tag.Role == PlcVariableRole.Input
                && tag.Binding is "operator.start" or "operator.stop";
            var sceneBinding = tag.Role is PlcVariableRole.Input or PlcVariableRole.Output
                && !string.IsNullOrWhiteSpace(tag.Binding)
                && allowedSceneBindings?.Contains(tag.Binding) == true;
            if ((operatorPulse || sceneBinding)
                && tag.TagTable.Equals("Default tag table", StringComparison.OrdinalIgnoreCase))
                document.Tags[index] = tag with { TagTable = SceneIoTagTableName };
        }
    }

    private static void EnsureCanonicalSceneIoTable(LadderEditorDocument document)
    {
        var sceneTable = document.TagTables.FirstOrDefault(table =>
            table.Equals(SceneIoTagTableName, StringComparison.OrdinalIgnoreCase));
        if (sceneTable is null)
        {
            document.AddTagTable(SceneIoTagTableName);
            return;
        }
        if (sceneTable.Equals(SceneIoTagTableName, StringComparison.Ordinal)) return;

        var index = document.TagTables.FindIndex(table => table.Equals(sceneTable, StringComparison.Ordinal));
        document.TagTables[index] = SceneIoTagTableName;
        for (var tagIndex = 0; tagIndex < document.Tags.Count; tagIndex++)
            if (document.Tags[tagIndex].TagTable.Equals(sceneTable, StringComparison.OrdinalIgnoreCase))
                document.Tags[tagIndex] = document.Tags[tagIndex] with { TagTable = SceneIoTagTableName };
        for (var constantIndex = 0; constantIndex < document.UserConstants.Count; constantIndex++)
            if (document.UserConstants[constantIndex].TagTable.Equals(sceneTable, StringComparison.OrdinalIgnoreCase))
                document.UserConstants[constantIndex] = document.UserConstants[constantIndex] with { TagTable = SceneIoTagTableName };
    }

    private static object? NormalizeInitialValue(PlcVariableType type, object? initial)
    {
        // Scene values can come from JSON or from a running plant. Normalize
        // the CLR numeric width to the controller's declared storage type so a
        // runtime-updated DINT value such as Int32 0 remains reopenable.
        if (type is PlcVariableType.Int or PlcVariableType.DInt && initial is sbyte or byte or short or ushort or int or uint)
            return Convert.ToInt64(initial, System.Globalization.CultureInfo.InvariantCulture);
        if (type == PlcVariableType.Real && initial is sbyte or byte or short or ushort or int or uint or long or ulong or float)
            return Convert.ToDouble(initial, System.Globalization.CultureInfo.InvariantCulture);
        return initial;
    }

    private static object ExistingInitialValue(PlcVariableType type, object current,
        SceneIoPoint point, object? fallback)
    {
        if (type == PlcVariableType.Enum
            && (current is not string currentState || point.EnumMembers?.Contains(currentState, StringComparer.Ordinal) != true))
            return fallback ?? DefaultInitialValue(type, point);
        return current;
    }

    private static object DefaultInitialValue(PlcVariableType type, SceneIoPoint point) => type switch
    {
        PlcVariableType.Bool => false,
        PlcVariableType.Int => 0,
        PlcVariableType.DInt => 0L,
        PlcVariableType.Real => 0d,
        PlcVariableType.String => string.Empty,
        PlcVariableType.Enum => point.EnumMembers?.FirstOrDefault() ?? PlcTextValues.MotorStates[0],
        _ => throw new InvalidOperationException($"Scene point '{point.Name}' is missing an initial value for {type}.")
    };


    private static string UniqueSceneSymbol(LadderEditorDocument document, string preferred)
    {
        if (!document.Tags.Any(tag => tag.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase)))
            return preferred;

        var safeSuffix = new string(preferred.Select(character => char.IsLetterOrDigit(character) || character == '_'
            ? character : '_').ToArray());
        if (safeSuffix.Length == 0 || !char.IsLetter(safeSuffix[0]) && safeSuffix[0] != '_')
            safeSuffix = "_" + safeSuffix;
        var baseName = "SceneIO_" + safeSuffix;
        var candidate = baseName;
        for (var suffix = 2; document.Tags.Any(tag => tag.Name.Equals(candidate, StringComparison.OrdinalIgnoreCase)); suffix++)
            candidate = baseName + "_" + suffix;
        return candidate;
    }
}
