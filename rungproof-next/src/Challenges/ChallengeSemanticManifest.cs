using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
namespace RungProof.Next.Challenges;

public sealed partial record ChallengeSemanticManifest
{
    public static ChallengeSemanticManifestLoadResult Load(string json) => ChallengeSemanticManifestJson.Load(json);
}
/// <summary>Checks bundled manifest structure and independent canonical entry identity.</summary>
public static class ChallengeSemanticManifestJson
{
    private static readonly string[] EvaluatorPaths = ["src/Challenges/ChallengeEvaluator.cs", "src/Challenges/ChallengeTestRunner.cs", "src/Challenges/ConveyorChallengeProtocol.cs"];
    public static ChallengeSemanticManifestLoadResult Load(string json)
    {
        try
        {
            using var document = ChallengeProgramIdentity.StrictDocument(json, 262144);
            var root = document.RootElement;
            ChallengeDefinitionJson.Fields(root, "schemaVersion", "identityFormat", "engineRevision", "engineSha256", "evaluatorRevision", "evaluatorSha256", "generatorSha256", "engineInputs", "evaluatorInputs");
            var schema = ChallengeDefinitionJson.Int(root, "schemaVersion");
            var format = Text(root, "identityFormat");
            var engineRevision = Text(root, "engineRevision");
            var evaluatorRevision = Text(root, "evaluatorRevision");
            if (schema != 1 || format != "RP-CANON-1" || engineRevision != "conveyor-engine-v1" || evaluatorRevision != "conveyor-evaluator-v1")
                throw new ArgumentException("Unsupported manifest identity/revisions.");
            var engineHash = Hash(root, "engineSha256");
            var evaluatorHash = Hash(root, "evaluatorSha256");
            var generatorHash = Hash(root, "generatorSha256");
            var engineArray = ChallengeDefinitionJson.Array(root, "engineInputs", 6, 1024);
            var evaluatorArray = ChallengeDefinitionJson.Array(root, "evaluatorInputs", 3, 3);
            var engine = Entries(engineArray);
            var evaluator = Entries(evaluatorArray);
            if (!engine.Any(e => e.RelativePath == "src/Scenes/ConveyorPlantModel.cs") || !engine.Any(e => e.RelativePath == "src/Challenges/ChallengeProgramIdentity.cs") || !engine.Any(e => e.RelativePath.StartsWith("src/VirtualController/", StringComparison.Ordinal)))
                throw new ArgumentException("Manifest missing model, canonicalizer or controller inputs.");
            if (!evaluator.Select(e => e.RelativePath).SequenceEqual(EvaluatorPaths, StringComparer.Ordinal) || evaluator.Any(e => !engine.Contains(e)))
                throw new ArgumentException("Evaluator requires exact supported source subset, identical to engine entries.");
            if (engineHash != ChallengeProgramIdentity.CanonicalSha256(engineArray.GetRawText()) || evaluatorHash != ChallengeProgramIdentity.CanonicalSha256(evaluatorArray.GetRawText()))
                throw new ArgumentException("Manifest canonical source hash mismatch.");
            return new(new(schema, format, engineRevision, engineHash, evaluatorRevision, evaluatorHash, generatorHash, engine, evaluator), []);
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        {
            return new(null, ["CH_INVALID_CHALLENGE: " + error.Message]);
        }
    }
    private static string Text(JsonElement root, string name) => ChallengeDefinitionJson.Text(root, name, 64);
    private static string Hash(JsonElement root, string name)
    {
        var hash = Text(root, name);
        if (!ChallengeDefinitionJson.ValidHash(hash))
            throw new ArgumentException("Invalid lowercase SHA256.");
        return hash;
    }
    private static IReadOnlyList<ChallengeSemanticSourceEntry> Entries(JsonElement array)
    {
        var entries = new List<ChallengeSemanticSourceEntry>();
        string? previous = null;
        foreach (var element in array.EnumerateArray())
        {
            ChallengeDefinitionJson.Fields(element, "relativePath", "sha256", "byteLength");
            var path = ChallengeDefinitionJson.Text(element, "relativePath", 256);
            var length = element.GetProperty("byteLength").GetInt64();
            if (length < 0 || length > 9007199254740991L || path.Contains('\\') || path.Contains(':') || path.Split('/').Any(p => p is "" or "." or "..") || !path.EndsWith(".cs", StringComparison.Ordinal) || !(path.StartsWith("src/Challenges/", StringComparison.Ordinal) || path.StartsWith("src/VirtualController/", StringComparison.Ordinal) || path == "src/Scenes/ConveyorPlantModel.cs"))
                throw new ArgumentException("Unsupported source path or byte length.");
            if (previous is not null && StringComparer.Ordinal.Compare(previous, path) >= 0)
                throw new ArgumentException("Source entries must be unique and ordinal sorted.");
            entries.Add(new(path, Hash(element, "sha256"), length));
            previous = path;
        }
        return entries.AsReadOnly();
    }
}
