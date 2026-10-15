using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace RungProof.Next.Challenges;
/// <summary>Strict trusted-resource packet parser; it performs no file I/O.</summary>
public static class ChallengeDefinitionJson
{
    public const string DefinitionResource = "res://challenges/conveyor-stop.challenge.json";
    public const string StarterResource = "res://challenges/starters/conveyor-stop.rpproj.json";
    public static ChallengeDefinitionLoadResult Load(string json)
    {
        try
        {
            using var document = ChallengeProgramIdentity.StrictDocument(json, 262144);
            var root = document.RootElement;
            Fields(root, "schemaVersion", "id", "revision", "sceneId", "protocolRevision", "scanPeriodMs", "title", "objective", "hints", "plant", "cases", "glossary", "eligibilityPolicy", "starter");
            var schema = Int(root, "schemaVersion");
            var id = Text(root, "id", 64);
            var revision = Int(root, "revision");
            var scene = Text(root, "sceneId", 64);
            var protocol = Text(root, "protocolRevision", 64);
            var period = Int(root, "scanPeriodMs");
            var policy = Text(root, "eligibilityPolicy", 64);
            if (schema != 1 || id != "scene-1-conveyor-stop" || revision < 1 || scene != "scene-1-conveyor-stop" || protocol != "conveyor-v1" || period != 20 || policy != "conveyor-bool-v1")
                throw new ArgumentException("Unsupported challenge/schema/protocol/policy or period.");
            var p = root.GetProperty("plant");
            Fields(p, "length", "speed", "objectLength", "photoeyePosition", "minimumPhotoeyeOn");
            var plant = new ConveyorPlantParameters(Number(p, "length"), Number(p, "speed"), Number(p, "objectLength"), Number(p, "photoeyePosition"), Number(p, "minimumPhotoeyeOn"));
            if (plant != new ConveyorPlantParameters(1, 0.5, 0.2, 0.5, 0.1))
                throw new ArgumentException("Conveyor-v1 plant constants must match the fixed model.");
            var caseArray = Array(root, "cases", 7, 7);
            var cases = new List<ChallengeTestCase>();
            var kinds = new HashSet<ChallengeCaseKind>();
            foreach (var element in caseArray.EnumerateArray())
            {
                Fields(element, "id", "kind", "maximumScans");
                var kindText = Text(element, "kind", 64);
                if (!Enum.TryParse<ChallengeCaseKind>(kindText, false, out var kind) || !Enum.IsDefined(kind) || kind.ToString() != kindText || !kinds.Add(kind))
                    throw new ArgumentException("Seven unique implemented case kinds required.");
                var caseId = Text(element, "id", 64);
                if (caseId != $"CV{(int)kind + 1:00}" || Int(element, "maximumScans") != 250)
                    throw new ArgumentException("Fixed case ID and 250 accepted scan budget required.");
                cases.Add(new(caseId, kind, 250));
            }
            var hints = Array(root, "hints", 0, 8).EnumerateArray().Select(v => TextValue(v, 2048)).ToArray();
            var glossary = new List<ChallengeGlossaryEntry>();
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in Array(root, "glossary", 4, 4).EnumerateArray())
            {
                Fields(element, "binding", "displayName", "ownership", "meaning");
                var binding = Text(element, "binding", 64);
                if (!new[] { "operator.start", "operator.stop", "simulated_photoeye", "conveyor_running" }.Contains(binding, StringComparer.Ordinal) || !bindings.Add(binding))
                    throw new ArgumentException("Glossary must explain each required binding exactly once.");
                glossary.Add(new(binding, Text(element, "displayName", 512), Text(element, "ownership", 512), Text(element, "meaning", 512)));
            }
            var starter = root.GetProperty("starter");
            Fields(starter, "resourceId", "sha256", "byteLength");
            var resource = Text(starter, "resourceId", 128);
            var hash = Text(starter, "sha256", 64);
            var size = starter.GetProperty("byteLength").GetInt64();
            if (resource != StarterResource || !ValidHash(hash) || size < 1 || size > 262144)
                throw new ArgumentException("Starter requires fixed resource, lowercase SHA256 and bounded byte length.");
            return new(new(schema, id, revision, scene, protocol, period, Text(root, "title", 512), Text(root, "objective", 512), hints, plant, cases, glossary, policy, new(resource, hash, size)), []);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            return new(null, ["CH_INVALID_CHALLENGE: " + error.Message]);
        }
    }
    public static IReadOnlyList<string> ValidateStarter(ChallengeDefinition definition, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != definition.Starter.ByteLength || Convert.ToHexStringLower(SHA256.HashData(bytes)) != definition.Starter.Sha256)
            return System.Array.AsReadOnly(new[] { "CH_INVALID_CHALLENGE: Starter byte length or SHA256 mismatch." });
        return System.Array.AsReadOnly(System.Array.Empty<string>());
    }
    public static IReadOnlyDictionary<string, string> LoadCatalog(string json)
    {
        using var document = ChallengeProgramIdentity.StrictDocument(json, 262144);
        var root = document.RootElement;
        Fields(root, "schemaVersion", "challenges");
        if (Int(root, "schemaVersion") != 1)
            throw new ArgumentException("Unsupported catalog version.");
        var entry = Array(root, "challenges", 1, 1)[0];
        Fields(entry, "id", "resourceId");
        if (Text(entry, "id", 64) != "scene-1-conveyor-stop" || Text(entry, "resourceId", 128) != DefinitionResource)
            throw new ArgumentException("Catalog uses only fixed trusted challenge resources.");
        return ChallengeCopies.Map(new Dictionary<string, string>(StringComparer.Ordinal) { { "scene-1-conveyor-stop", DefinitionResource } });
    }
    internal static void Fields(JsonElement value, params string[] expected)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Object required.");
        var properties = value.EnumerateObject().Select(p => p.Name).ToArray();
        if (properties.Length != expected.Length || properties.Any(p => !expected.Contains(p, StringComparer.Ordinal)))
            throw new ArgumentException("Missing or unknown object properties.");
    }
    internal static string Text(JsonElement value, string name, int maximum) => TextValue(value.GetProperty(name), maximum);
    private static string TextValue(JsonElement value, int maximum)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw new ArgumentException("String required.");
        var text = value.GetString()!;
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximum)
            throw new ArgumentException("Text is empty or exceeds its limit.");
        return text;
    }
    internal static int Int(JsonElement value, string name) => value.GetProperty(name).GetInt32();
    private static double Number(JsonElement value, string name)
    {
        var n = value.GetProperty(name).GetDouble();
        if (!double.IsFinite(n))
            throw new ArgumentException("Finite number required.");
        return n;
    }
    internal static JsonElement Array(JsonElement value, string name, int minimum, int maximum)
    {
        var array = value.GetProperty(name);
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() < minimum || array.GetArrayLength() > maximum)
            throw new ArgumentException(name + " array count outside bounds.");
        return array;
    }
    internal static bool ValidHash(string hash) => hash.Length == 64 && hash.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
}
