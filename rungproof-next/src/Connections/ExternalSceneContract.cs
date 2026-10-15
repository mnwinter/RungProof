using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RungProof.Next.Connections;

/// <summary>Symbolic scene ownership and types, independent of S7 addresses.</summary>
public static class ExternalSceneContract
{
    public static void Validate(JsonElement simulation, JsonElement descriptor)
    {
        var points = simulation.GetProperty("points").EnumerateArray()
            .ToDictionary(item => item.GetProperty("name").GetString()!, StringComparer.Ordinal);
        foreach (var (scopeName, owner) in new[] { ("pcPointScope", "PC"), ("plcPointScope", "PLC") })
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in descriptor.GetProperty(scopeName).EnumerateArray())
            {
                var name = item.GetProperty("name").GetString() ?? "";
                var type = item.GetProperty("dataType").GetString() ?? "";
                if (!names.Add(name) || !points.TryGetValue(name, out var point)
                    || !string.Equals(point.GetProperty("owner").GetString(), owner, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(point.GetProperty("type").GetString(), type, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"External {scopeName} point '{name}' does not match the scene's {owner}-owned type {type}.");
                if (type is not ("BOOL" or "INT" or "DINT" or "REAL"))
                    throw new InvalidOperationException($"External scene point type '{type}' is unsupported.");
            }
        }
        if (descriptor.GetProperty("cycleMs").GetInt32() <= 0 || descriptor.GetProperty("heartbeatTimeoutMs").GetInt32() <= 0)
            throw new InvalidOperationException("External profile timing must be positive.");
    }

    public static Dictionary<string, object?> SamplePcPoints(JsonElement descriptor, IReadOnlyDictionary<string, object?> scenePoints)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var item in descriptor.GetProperty("pcPointScope").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString()!;
            if (!scenePoints.TryGetValue(name, out var value))
                throw new InvalidOperationException($"Scene is missing external input '{name}'.");
            result.Add(name, TypedValue(item.GetProperty("dataType").GetString()!, value, name));
        }
        return result;
    }

    public static Dictionary<string, object?> ReadPlcPoints(JsonElement descriptor, JsonElement outputs)
    {
        if (outputs.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("plcPoints must be an object.");
        var values = outputs.EnumerateObject().ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal);
        var scope = descriptor.GetProperty("plcPointScope").EnumerateArray().ToArray();
        if (values.Count != scope.Length || scope.Any(item => !values.ContainsKey(item.GetProperty("name").GetString()!)))
            throw new InvalidOperationException("PLC outputs must exactly match the active profile's scene point scope.");
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var item in scope)
        {
            var name = item.GetProperty("name").GetString()!;
            var value = values[name];
            object? primitive = value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
                JsonValueKind.Number when value.TryGetDouble(out var number) => number,
                _ => null,
            };
            result.Add(name, TypedValue(item.GetProperty("dataType").GetString()!, primitive, name));
        }
        return result;
    }

    public static object TypedValue(string type, object? value, string name)
    {
        if (type.Equals("BOOL", StringComparison.OrdinalIgnoreCase) && value is bool boolean) return boolean;
        if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal)
        {
            var number = Convert.ToDouble(value);
            if (double.IsFinite(number))
            {
                if (type.Equals("REAL", StringComparison.OrdinalIgnoreCase) && Math.Abs(number) <= float.MaxValue) return number;
                if (Math.Truncate(number) == number)
                {
                    if (type.Equals("INT", StringComparison.OrdinalIgnoreCase) && number is >= short.MinValue and <= short.MaxValue) return (long)number;
                    if (type.Equals("DINT", StringComparison.OrdinalIgnoreCase) && number is >= int.MinValue and <= int.MaxValue) return (long)number;
                }
            }
        }
        throw new InvalidOperationException($"External point '{name}' requires a valid {type} value.");
    }
}
