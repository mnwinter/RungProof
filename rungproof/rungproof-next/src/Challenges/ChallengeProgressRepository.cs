using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace RungProof.Next.Challenges;

public enum ChallengeProgressWriteStage { TemporaryWrite, Replace }

/// <summary>Separate local history. Loaded records never award current proof.
/// Invalid originals are retained in place; backup recovery is read-only.</summary>
public sealed class ChallengeProgressRepository
{
    private const int MaximumBytes = 1048576;
    private const int MaximumRecords = 1000;
    // Process-local serialization by normalized path; aliases and other processes
    // need separate coordination. File replacement fixtures do not prove power-loss durability.
    private static readonly ConcurrentDictionary<string, object> Gates = new(StringComparer.OrdinalIgnoreCase);
    private readonly string path;
    private readonly object gate;
    // Optional deterministic filesystem-fault seam. Called inside the write lock,
    // immediately before the actual filesystem operation; never used by the app.
    private readonly Action<ChallengeProgressWriteStage>? beforeWriteStage;

    public ChallengeProgressRepository(string absolutePath, Action<ChallengeProgressWriteStage>? beforeWriteStage = null)
    {
        if (string.IsNullOrWhiteSpace(absolutePath) || !Path.IsPathFullyQualified(absolutePath))
            throw new ArgumentException("Progress path must be absolute.", nameof(absolutePath));
        path = Path.GetFullPath(absolutePath);
        if (string.IsNullOrEmpty(Path.GetFileName(path)))
            throw new ArgumentException("Progress path must name a file.", nameof(absolutePath));
        gate = Gates.GetOrAdd(path, _ => new object());
        this.beforeWriteStage = beforeWriteStage;
    }

    public ChallengeProgressLoadResult Load()
    {
        lock (gate)
        {
            try { return new(Read(path), []); }
            catch (FileNotFoundException) { return new([], []); }
            catch (DirectoryNotFoundException) { return new([], []); }
            catch (Exception error) when (Recoverable(error))
            {
                var issue = "Progress file preserved: " + Message(error);
                try
                {
                    return new(Read(path + ".bak"), [issue, "Recovered historical progress from last-good backup. Original must be recovered explicitly before saving."]);
                }
                catch (FileNotFoundException) { return new([], [issue]); }
                catch (DirectoryNotFoundException) { return new([], [issue]); }
                catch (Exception backupError) when (Recoverable(backupError))
                { return new([], [issue, "Backup could not be read: " + Message(backupError)]); }
            }
        }
    }

    public ChallengeProgressWriteResult Save(IReadOnlyList<ChallengeProgressRecord> records)
    {
        lock (gate)
        {
            string? temporary = null;
            try
            {
                var bytes = Serialize(records);
                // Never replace an unrecognized, corrupt, future or oversized original.
                // A recovered backup is historical evidence, not authorization to erase it.
                if (File.Exists(path)) _ = Read(path);
                var directory = Path.GetDirectoryName(path)!;
                Directory.CreateDirectory(directory);
                temporary = Path.Combine(directory, ".challenge-progress-" + Guid.NewGuid().ToString("N") + ".tmp");
                beforeWriteStage?.Invoke(ChallengeProgressWriteStage.TemporaryWrite);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes); stream.Flush(true); }
                beforeWriteStage?.Invoke(ChallengeProgressWriteStage.Replace);
                if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
                else File.Move(temporary, path);
                temporary = null;
                return new(true, "Progress saved.");
            }
            catch (Exception error) when (Recoverable(error))
            { return new(false, "Progress not saved: " + Message(error)); }
            finally
            {
                if (temporary is not null)
                {
                    try { File.Delete(temporary); }
                    catch (Exception error) when (Recoverable(error)) { /* Prior good file remains authoritative. */ }
                }
            }
        }
    }

    private static IReadOnlyList<ChallengeProgressRecord> Read(string filename)
    {
        using var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaximumBytes) throw new InvalidDataException("Progress exceeds 1 MiB.");
        // Read at most limit+1 even if the file grows after the length check.
        var buffer = new byte[MaximumBytes + 1];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = stream.Read(buffer, count, buffer.Length - count);
            if (read == 0) break;
            count += read;
        }
        if (count > MaximumBytes) throw new InvalidDataException("Progress exceeds 1 MiB.");
        using var document = JsonDocument.Parse(buffer.AsMemory(0, count), new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        Exact(root, "schemaVersion", "records");
        if (root.GetProperty("schemaVersion").GetInt32() != 1)
            throw new InvalidDataException("Unsupported progress schema; original retained.");
        var array = root.GetProperty("records");
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > MaximumRecords)
            throw new InvalidDataException("Progress must contain at most 1000 records.");
        var records = new List<ChallengeProgressRecord>();
        foreach (var record in array.EnumerateArray())
        {
            Exact(record, "identity", "completedUtc", "usedHintIds");
            var identity = record.GetProperty("identity");
            Exact(identity, "identityFormat", "challengeId", "challengeRevision", "programSha256", "challengeSha256", "plantSha256", "engineRevision", "engineSha256", "evaluatorRevision", "evaluatorSha256", "scanPeriodMs");
            var run = new ChallengeRunIdentity(Text(identity, "identityFormat"), Text(identity, "challengeId"), identity.GetProperty("challengeRevision").GetInt32(), Text(identity, "programSha256"), Text(identity, "challengeSha256"), Text(identity, "plantSha256"), Text(identity, "engineRevision"), Text(identity, "engineSha256"), Text(identity, "evaluatorRevision"), Text(identity, "evaluatorSha256"), identity.GetProperty("scanPeriodMs").GetInt32(), 0);
            var hints = record.GetProperty("usedHintIds");
            if (hints.ValueKind != JsonValueKind.Array || hints.GetArrayLength() > 8) throw new InvalidDataException("Invalid hint history.");
            records.Add(new(run, record.GetProperty("completedUtc").GetDateTimeOffset(), hints.EnumerateArray().Select(h => h.GetString() ?? throw new InvalidDataException("Null hint ID.")).ToArray()));
        }
        Validate(records);
        return records;
    }

    private static byte[] Serialize(IReadOnlyList<ChallengeProgressRecord> records)
    {
        Validate(records);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteNumber("schemaVersion", 1); writer.WriteStartArray("records");
            foreach (var record in records)
            {
                var id = record.Identity;
                writer.WriteStartObject(); writer.WriteStartObject("identity");
                writer.WriteString("identityFormat", id.IdentityFormat); writer.WriteString("challengeId", id.ChallengeId); writer.WriteNumber("challengeRevision", id.ChallengeRevision);
                writer.WriteString("programSha256", id.ProgramSha256); writer.WriteString("challengeSha256", id.ChallengeSha256); writer.WriteString("plantSha256", id.PlantSha256);
                writer.WriteString("engineRevision", id.EngineRevision); writer.WriteString("engineSha256", id.EngineSha256); writer.WriteString("evaluatorRevision", id.EvaluatorRevision); writer.WriteString("evaluatorSha256", id.EvaluatorSha256); writer.WriteNumber("scanPeriodMs", id.ScanPeriodMs);
                writer.WriteEndObject(); writer.WriteString("completedUtc", record.CompletedUtc); writer.WriteStartArray("usedHintIds");
                foreach (var hint in record.UsedHintIds) writer.WriteStringValue(hint);
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        if (stream.Length > MaximumBytes) throw new InvalidDataException("Progress exceeds 1 MiB.");
        return stream.ToArray();
    }

    private static void Validate(IReadOnlyList<ChallengeProgressRecord> records)
    {
        if (records is null || records.Count > MaximumRecords) throw new InvalidDataException("Progress must contain at most 1000 records.");
        var identities = new HashSet<ChallengeRunIdentity>();
        foreach (var record in records)
        {
            if (record is null || record.Identity is not { } id) throw new InvalidDataException("Missing progress identity.");
            if (id.IdentityFormat != "RP-CANON-1" || !Identifier(id.ChallengeId) || id.ChallengeRevision < 1 || id.ScanPeriodMs is < 1 or > 60000 || !Identifier(id.EngineRevision) || !Identifier(id.EvaluatorRevision)) throw new InvalidDataException("Invalid progress identity.");
            if (!new[] { id.ProgramSha256, id.ChallengeSha256, id.PlantSha256, id.EngineSha256, id.EvaluatorSha256 }.All(Hash)) throw new InvalidDataException("Invalid identity hash.");
            if (!identities.Add(id with { RunEpoch = 0 })) throw new InvalidDataException("Duplicate progress identity.");
            if (record.UsedHintIds.Count > 8 || record.UsedHintIds.Any(h => !Identifier(h)) || record.UsedHintIds.Distinct(StringComparer.Ordinal).Count() != record.UsedHintIds.Count) throw new InvalidDataException("Invalid hint history.");
        }
    }
    private static bool Identifier(string value) => !string.IsNullOrEmpty(value) && value.Length <= 64 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
    private static bool Hash(string value) => value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString() ?? throw new InvalidDataException("Null identity field.");
    private static void Exact(JsonElement value, params string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Expected progress object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!names.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name)) throw new InvalidDataException("Unknown or duplicate progress property.");
        if (seen.Count != names.Length) throw new InvalidDataException("Missing progress property.");
    }
    private static bool Recoverable(Exception error) => error is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or FormatException or OverflowException;
    private static string Message(Exception error) => error.Message.Length <= 256 ? error.Message : error.Message[..256];
}



