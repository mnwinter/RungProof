using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RungProof.Next.Challenges;
namespace RungProof.Next.ChallengeTests;

internal static class ChallengeProgressTests
{
    public static IEnumerable<(string, Action)> Tests()
    {
        yield return ("PassIdentityAndHintHistoryRoundTripWithoutRunEpoch", () => InDirectory(path =>
        {
            var repository = new ChallengeProgressRepository(path);
            var record = Record();
            Require(repository.Save([record]).Success, "save failed");
            var json = File.ReadAllText(path);
            Require(!json.Contains("runEpoch", StringComparison.OrdinalIgnoreCase), "ephemeral epoch persisted");
            var loaded = repository.Load();
            Require(loaded.Issues.Count == 0 && loaded.Records.Count == 1, "round trip failed");
            Require(loaded.Records[0].Identity == record.Identity with { RunEpoch = 0 }, "identity lost");
            Require(loaded.Records[0].CompletedUtc == record.CompletedUtc && loaded.Records[0].UsedHintIds.SequenceEqual(record.UsedHintIds), "history lost");
        }));
        yield return ("ChangedContentEngineOrProgramIsHistoricalNotCurrent", () => InDirectory(path =>
        {
            var old = Record();
            var records = new[] { old, old with { Identity = old.Identity with { ProgramSha256 = new string('b', 64) } }, old with { Identity = old.Identity with { EngineSha256 = new string('c', 64) } }, old with { Identity = old.Identity with { ChallengeSha256 = new string('d', 64) } } };
            var repository = new ChallengeProgressRepository(path);
            Require(repository.Save(records).Success, "history save failed");
            Require(repository.Load().Records.Count == 4, "changed identities lost or merged");
            Require(repository.Load().Records.All(record => record.Identity.RunEpoch == 0), "loaded history has an active run epoch");
        }));
        yield return ("UnknownFutureSchemaPreservesOriginal", () => InDirectory(path =>
        {
            File.WriteAllText(path, "{\"schemaVersion\":99,\"records\":[]}");
            var original = File.ReadAllBytes(path);
            var repository = new ChallengeProgressRepository(path);
            Require(repository.Load().Issues.Count > 0, "future schema accepted");
            Require(!repository.Save([Record()]).Success, "future schema overwritten");
            Require(File.ReadAllBytes(path).SequenceEqual(original), "future original changed");
        }));
        yield return ("CorruptOversizedAndDuplicateRecordInputsRejected", () => InDirectory(path =>
        {
            var repository = new ChallengeProgressRepository(path);
            Require(repository.Save([Record()]).Success, "fixture save failed");
            var valid = File.ReadAllText(path);
            var duplicate = JsonNode.Parse(valid)!;
            var array = duplicate["records"]!.AsArray();
            array.Add(array[0]!.DeepClone());
            foreach (var invalid in new[] { "{", new string(' ', 1048577), duplicate.ToJsonString(), valid.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal), "{\"schemaVersion\":1,\"records\":[],\"extra\":0}" })
            {
                File.WriteAllText(path, invalid);
                Require(repository.Load().Issues.Count > 0, "invalid input accepted");
                Require(!repository.Save([Record()]).Success, "invalid input overwritten");
                Require(File.ReadAllText(path) == invalid, "corrupt original lost");
            }
            Require(!repository.Save(Enumerable.Range(0, 1001).Select(i => Record() with { Identity = Record().Identity with { ChallengeRevision = i + 1 } }).ToArray()).Success, "1001 records accepted");
        }));
        yield return ("TempOrReplaceFailurePreservesPriorGoodFile", () => InDirectory(path =>
        {
            var original = Record();
            Require(new ChallengeProgressRepository(path).Save([original]).Success, "fixture save failed");
            var bytes = File.ReadAllBytes(path);
            foreach (var fault in Enum.GetValues<ChallengeProgressWriteStage>())
            {
                var repository = new ChallengeProgressRepository(path, stage => { if (stage == fault) throw new IOException("injected " + fault); });
                Require(!repository.Save([new ChallengeProgressRecord(original.Identity, original.CompletedUtc, ["hint-2"])]).Success, "fault reported saved");
                Require(File.ReadAllBytes(path).SequenceEqual(bytes), "prior valid file changed");
                Require(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length == 0, "temporary file leaked");
            }
            Require(new ChallengeProgressRepository(path).Save([new ChallengeProgressRecord(original.Identity, original.CompletedUtc, ["hint-2"])]).Success, "replacement failed");
            File.WriteAllText(path, "corrupt");
            var recovered = new ChallengeProgressRepository(path).Load();
            Require(recovered.Records.Count == 1 && recovered.Records[0].UsedHintIds.SequenceEqual(original.UsedHintIds), "last-good backup not recovered");
            Require(recovered.Issues.Count > 0 && File.ReadAllText(path) == "corrupt", "recovery concealed corruption or altered original");
        }));
        yield return ("ProgressWritesSerialized", () => InDirectory(path =>
        {
            var entered = 0;
            using var firstEntered = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();
            var repository = new ChallengeProgressRepository(path, stage =>
            {
                if (stage != ChallengeProgressWriteStage.TemporaryWrite) return;
                var count = Interlocked.Increment(ref entered);
                if (count == 1) { firstEntered.Set(); Require(release.Wait(TimeSpan.FromSeconds(5)), "fixture release timeout"); }
            });
            var first = Task.Run(() => repository.Save([Record()]));
            Require(firstEntered.Wait(TimeSpan.FromSeconds(5)), "first write never entered");
            using var secondAttempted = new ManualResetEventSlim();
            var secondRepository = new ChallengeProgressRepository(path, stage => { if (stage == ChallengeProgressWriteStage.TemporaryWrite) Interlocked.Increment(ref entered); });
            var second = Task.Run(() => { secondAttempted.Set(); return secondRepository.Save([new ChallengeProgressRecord(Record().Identity, Record().CompletedUtc, ["hint-2"])]); });
            Require(secondAttempted.Wait(TimeSpan.FromSeconds(5)), "second write not scheduled");
            var completedBeforeRelease = second.Wait(TimeSpan.FromMilliseconds(100));
            var entriesBeforeRelease = Volatile.Read(ref entered);
            release.Set();
            Require(!completedBeforeRelease && entriesBeforeRelease == 1, "second instance bypassed write serialization");
            Require(Task.WaitAll(new Task[] { first, second }, TimeSpan.FromSeconds(5)), "writes stalled");
            Require(first.Result.Success && second.Result.Success && entered == 2, "serialized writes failed");
            Require(repository.Load().Records.Single().UsedHintIds.SequenceEqual(new[] { "hint-2" }), "later write not retained");
        }));
        yield return ("FailedWriteReturnsNotSavedWithoutChangingSemanticPass", () => InDirectory(path =>
        {
            var result = new ChallengeRunResult(Record().Identity, ChallengeOutcome.Passed, [], [], [], "passed", "");
            var write = new ChallengeProgressRepository(path, _ => throw new IOException("disk unavailable")).Save([Record()]);
            Require(!write.Success && write.Diagnostic.Contains("not saved", StringComparison.OrdinalIgnoreCase), "recoverable status missing");
            Require(result.Outcome == ChallengeOutcome.Passed, "semantic pass altered");
        }));
        yield return ("Valid1000RecordsAndStrictDepthBoundary", () => InDirectory(path =>
        {
            var repository = new ChallengeProgressRepository(path);
            var records = Enumerable.Range(0, 1000).Select(i => Record() with { Identity = Record().Identity with { ChallengeRevision = i + 1 } }).ToArray();
            Require(repository.Save(records).Success && repository.Load().Records.Count == 1000, "valid 1000-record boundary rejected");
            var original = File.ReadAllText(path);
            Require(!repository.Save([Record(), Record() with { Identity = Record().Identity with { RunEpoch = 999 } }]).Success, "epoch-only duplicate accepted");
            Require(File.ReadAllText(path) == original, "duplicate save changed valid original");
            File.WriteAllText(path, new string('[', 33) + "0" + new string(']', 33));
            Require(repository.Load().Issues.Count > 0 && !repository.Save([Record()]).Success, "depth33 accepted or erased");
        }));
        yield return ("UnavailablePrimaryReportsIssuesInsteadOfCleanEmptyHistory", () => InDirectory(path =>
        {
            var repository = new ChallengeProgressRepository(path);
            Require(repository.Load().Records.Count == 0 && repository.Load().Issues.Count == 0, "missing file should be clean absence");
            // A directory at the fixed filename is an actual path/access error.
            // File.Exists hides it; direct FileStream opening must report it.
            Directory.CreateDirectory(path);
            Require(repository.Load().Issues.Count > 0, "unreadable primary concealed as clean empty history");
            Directory.Delete(path);
            Require(repository.Save([Record()]).Success, "fixture save failed");
            var original = File.ReadAllBytes(path);
            using (var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var blocked = repository.Load();
                Require(blocked.Issues.Count > 0 && blocked.Records.Count == 0, "exclusive lock concealed as clean absence");
            }
            Require(File.ReadAllBytes(path).SequenceEqual(original), "read error altered primary");
            Require(repository.Load().Issues.Count == 0 && repository.Load().Records.Count == 1, "read did not recover after lock release");
        }));
        yield return ("ProgressPathMustBeAbsolute", () =>
        {
            try { _ = new ChallengeProgressRepository("relative.json"); throw new Exception("relative accepted"); }
            catch (ArgumentException) { }
        });
    }
    private static ChallengeProgressRecord Record() => new(new ChallengeRunIdentity("RP-CANON-1", "scene-1-conveyor-stop", 1, new string('a', 64), new string('a', 64), new string('a', 64), "engine-1", new string('a', 64), "evaluator-1", new string('a', 64), 20, 42), DateTimeOffset.Parse("2026-10-10T18:00:00+00:00"), ["hint-1"]);
    private static void InDirectory(Action<string> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "rungproof-progress-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { test(Path.Combine(directory, "challenge-progress.json")); }
        finally { Directory.Delete(directory, true); }
    }
    private static void Require(bool condition, string diagnostic) { if (!condition) throw new InvalidOperationException(diagnostic); }
}




