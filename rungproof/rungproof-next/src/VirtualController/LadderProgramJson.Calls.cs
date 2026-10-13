using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RungProof.Next.VirtualController;

public static partial class LadderProgramJson
{
    // The v1 writer remains byte-structure compatible with frozen legacy fixtures.
    private static void RemoveVersionTwoMetadata(Dictionary<string, object?> document)
    {
        document.Remove("instances");
        foreach (var variable in (Dictionary<string, object?>[])document["variables"]!) variable.Remove("symbolId");
        static void Networks(object? value)
        {
            foreach (var network in (Dictionary<string, object?>[])value!)
                if (network.TryGetValue("call", out var raw) && raw is Dictionary<string, object?> call)
                { call.Remove("semanticMode"); call.Remove("bindings"); call.Remove("instanceId"); }
        }
        Networks(document["networks"]);
        if (document.TryGetValue("blocks", out var blocks))
            foreach (var block in (Dictionary<string, object?>[])blocks!)
            { block.Remove("blockType"); block.Remove("semanticMode"); block.Remove("interface"); Networks(block["networks"]); }
    }
    private static T ReadEnum<T>(JsonElement value, string key, T fallback, string path,
        ICollection<LadderValidationIssue> issues) where T : struct, Enum
    {
        if (!value.TryGetProperty(key, out var property)) return fallback;
        if (property.ValueKind == JsonValueKind.String && Enum.TryParse<T>(property.GetString(), true, out var parsed)
            && Enum.IsDefined(parsed)) return parsed;
        issues.Add(new("VC_CALL_SCHEMA", path + "." + key, "Unknown " + key + "."));
        return fallback;
    }

    private static IReadOnlyList<T>? ReadRecords<T>(JsonElement element, string key, string path,
        ICollection<LadderValidationIssue> issues)
    {
        if (!element.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        try
        {
            var result = JsonSerializer.Deserialize<List<T>>(value.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result is null || result.Any(x => x is null)) throw new JsonException("Null record.");
            foreach (var record in result)
            {
                var malformed = record switch
                {
                    LadderInstance i => i.Id is null || i.Name is null || i.BlockId is null,
                    LadderInterfaceParameter p => p.Id is null || p.Name is null || p.DataType is null || p.InitialValue is null
                        || p.InitialValue is JsonElement { ValueKind: JsonValueKind.Null } || p.EnumMembers?.Any(m => m is null) == true,
                    LadderCallBinding b => b.ParameterId is null || b.Symbol is null || b.EnumMembers?.Any(m => m is null) == true,
                    _ => false
                };
                if (malformed) throw new JsonException("Required identity, name, reference, initial value, or enum member must not be null.");
            }
            return result;
        }
        catch (JsonException ex) { issues.Add(new("VC_CALL_SCHEMA", path + "." + key, ex.Message)); return null; }
    }

    private static IReadOnlyList<LadderInterfaceParameter>? ReadParameters(JsonElement element, string path,
        ICollection<LadderValidationIssue> issues) => ReadRecords<LadderInterfaceParameter>(element, "interface", path, issues)?
            .Select(p => p with { InitialValue = p.InitialValue is JsonElement j ? JsonValue(j) : p.InitialValue }).ToArray();

    private static IReadOnlyList<LadderCallBinding>? ReadBindings(JsonElement element, string path,
        ICollection<LadderValidationIssue> issues) => ReadRecords<LadderCallBinding>(element, "bindings", path, issues)?
            .Select(p => p with { Literal = p.Literal is JsonElement j ? j.ValueKind == JsonValueKind.Null ? null : JsonValue(j) : p.Literal }).ToArray();
}
