using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RungProof.Next.VirtualController;

/// <summary>Fixed, scalar-leaf aggregates. No packing or physical PLC layout is implied.</summary>
public sealed record PlcAggregateField(string Name, PlcVariableType Type, string Comment = "");
public sealed record PlcAggregateSchema(
    IReadOnlyList<PlcAggregateField>? Fields = null,
    PlcVariableType ElementType = PlcVariableType.Bool,
    int LowerBound = 0,
    int Length = 0);

public static class PlcAggregates
{
    private static readonly JsonSerializerOptions SchemaJson = new() { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
    public static PlcAggregateSchema? ParseSchema(JsonElement element) => element.ValueKind == JsonValueKind.Object
        ? JsonSerializer.Deserialize<PlcAggregateSchema>(element.GetRawText(), SchemaJson) : null;
    public static bool IsAggregate(PlcVariableType type) => type is PlcVariableType.Struct or PlcVariableType.Array;
    private static bool IsScalar(PlcVariableType type) => type is PlcVariableType.Bool or PlcVariableType.Int or PlcVariableType.DInt or PlcVariableType.Real;

    public static void ValidateSchema(string name, PlcVariableType type, PlcAggregateSchema? schema)
    {
        if (schema is null) throw new ArgumentException($"Aggregate '{name}' requires a schema.");
        static object Default(PlcVariableType scalar) => scalar == PlcVariableType.Bool ? (object)false : 0L;
        object initial;
        if (type == PlcVariableType.Struct)
        {
            var fields = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var field in schema.Fields ?? [])
            {
                if (field is null || string.IsNullOrWhiteSpace(field.Name)) throw new ArgumentException("STRUCT field name is required.");
                fields.TryAdd(field.Name, Default(field.Type));
            }
            initial = fields;
        }
        else
        {
            if (schema.Length <= 0 || schema.Length > 4096) throw new ArgumentException($"Invalid ARRAY length for '{name}'.");
            initial = Enumerable.Range(0, schema.Length).Select(_ => Default(schema.ElementType)).ToArray();
        }
        Expand([new(name, type, PlcVariableRole.Memory, initial, Aggregate: schema)]);
    }

    /// <summary>Produces validated leaf declarations while retaining parent declarations in source.</summary>
    public static IReadOnlyList<PlcVariable> Expand(IReadOnlyList<PlcVariable> declarations)
    {
        var result = new List<PlcVariable>();
        if (declarations.Any(v => IsAggregate(v.Type)) && declarations.Select(v => v.Name).Distinct(StringComparer.Ordinal).Count() != declarations.Count)
            throw new ArgumentException("Duplicate variable declaration.");
        foreach (var root in declarations.Where(v => IsAggregate(v.Type)))
        foreach (var other in declarations.Where(v => !ReferenceEquals(v, root)))
        {
            static bool Descendant(string name, string parent) => name.StartsWith(parent + ".", StringComparison.Ordinal) || name.StartsWith(parent + "[", StringComparison.Ordinal);
            if (Descendant(other.Name, root.Name) || Descendant(root.Name, other.Name))
                throw new ArgumentException($"Aggregate root '{root.Name}' overlaps declaration namespace '{other.Name}'.");
        }
        foreach (var tag in declarations)
        {
            if (!IsAggregate(tag.Type)) { result.Add(tag); continue; }
            if (string.IsNullOrWhiteSpace(tag.Name)) throw new ArgumentException("Aggregate root name is required.");
            var schema = tag.Aggregate ?? throw new ArgumentException($"Aggregate '{tag.Name}' requires a schema.");
            var value = tag.InitialValue is JsonElement json ? json : JsonSerializer.SerializeToElement(tag.InitialValue);
            if (tag.Type == PlcVariableType.Struct)
            {
                if (schema.Fields is not { Count: > 0 } || value.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException($"STRUCT '{tag.Name}' requires fields and an object initial value.");
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var field in schema.Fields)
                {
                    if (field is null || string.IsNullOrWhiteSpace(field.Name) || field.Name.Any(c => !(char.IsLetterOrDigit(c) || c == '_')) || !names.Add(field.Name) || !IsScalar(field.Type))
                        throw new ArgumentException($"STRUCT '{tag.Name}' has an invalid or duplicate scalar field.");
                    if (!value.TryGetProperty(field.Name, out var initial)) throw new ArgumentException($"STRUCT '{tag.Name}' is missing initial field '{field.Name}'.");
                    result.Add(Leaf(tag, "." + field.Name, field.Type, initial));
                }
                if (value.EnumerateObject().Count() != names.Count) throw new ArgumentException($"STRUCT '{tag.Name}' has extra or duplicate initial fields.");
            }
            else
            {
                if (!IsScalar(schema.ElementType) || schema.Length <= 0 || schema.Length > 4096 || (long)schema.LowerBound + schema.Length - 1 > int.MaxValue || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != schema.Length)
                    throw new ArgumentException($"ARRAY '{tag.Name}' requires scalar elements, valid fixed bounds (maximum 4096), and matching initial length.");
                for (var i = 0; i < schema.Length; i++) result.Add(Leaf(tag, $"[{schema.LowerBound + i}]", schema.ElementType, value[i]));
            }
        }
        if (declarations.Any(v => IsAggregate(v.Type)) && result.Select(v => v.Name).Distinct(StringComparer.Ordinal).Count() != result.Count)
            throw new ArgumentException("Aggregate leaf names overlap another declared variable.");
        return result;
    }

    private static PlcVariable Leaf(PlcVariable root, string suffix, PlcVariableType type, JsonElement value)
    {
        object initial = type switch
        {
            PlcVariableType.Bool when value.ValueKind is JsonValueKind.True or JsonValueKind.False => value.GetBoolean(),
            PlcVariableType.Int or PlcVariableType.DInt when value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var integer) => integer,
            PlcVariableType.Real when value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var real) && double.IsFinite(real) => real,
            _ => throw new ArgumentException($"Invalid initial value for aggregate leaf '{root.Name}{suffix}' ({type})."),
        };
        if (type == PlcVariableType.Int && ((long)initial < short.MinValue || (long)initial > short.MaxValue)
            || type == PlcVariableType.DInt && ((long)initial < int.MinValue || (long)initial > int.MaxValue))
            throw new ArgumentException($"Aggregate leaf '{root.Name}{suffix}' initial value is outside its declared range.");
        return new(root.Name + suffix, type, root.Role, initial, root.Binding.Length == 0 ? "" : root.Binding + suffix);
    }

    public static IReadOnlyDictionary<string, object> Reconstruct(IReadOnlyList<PlcVariable> roots, IReadOnlyDictionary<string, bool> booleans, IReadOnlyDictionary<string, double> numbers)
    {
        object Read(string name, PlcVariableType type) => type == PlcVariableType.Bool ? (object)booleans[name] : numbers[name];
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var root in roots.Where(root => IsAggregate(root.Type)))
        {
            var schema = root.Aggregate!;
            values[root.Name] = root.Type == PlcVariableType.Struct
                ? new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(schema.Fields!.ToDictionary(field => field.Name, field => Read(root.Name + "." + field.Name, field.Type), StringComparer.Ordinal))
                : Array.AsReadOnly(Enumerable.Range(schema.LowerBound, schema.Length).Select(i => Read($"{root.Name}[{i}]", schema.ElementType)).ToArray());
        }
        return new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(values);
    }
}
