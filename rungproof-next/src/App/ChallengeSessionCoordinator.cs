using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RungProof.Next.Challenges;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;

/// <summary>Main-thread facade; the worker owns no Godot objects.</summary>
public sealed class ChallengeSessionCoordinator : IDisposable
{
    private readonly ChallengeProgressRepository _repository;
    private readonly Func<ChallengeDefinition, LadderProgram, ChallengeRunIdentity, CancellationToken, ChallengeRunResult> _runner;
    private readonly Func<TimeSpan> _now;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly string _packetHash;
    private readonly string _plantHash;
    private readonly ChallengeSemanticManifest _manifest;
    private readonly byte[] _starterBytes;
    private readonly ChallengeRunLifecycle _lifecycle = new();
    private readonly int _ownerThread = Environment.CurrentManagedThreadId;
    private Task<ChallengeRunResult>? _worker;
    private CancellationTokenSource? _cancellation;
    private ChallengeRunIdentity? _identity;
    private ChallengeDefinition? _definition;
    private IReadOnlyList<string> _hints = Array.Empty<string>();
    private IReadOnlyList<ChallengeProgressRecord> _progress;
    private ChallengeRunResult? _lastResult;
    private ChallengeUiState _phase = ChallengeUiState.Idle;
    private ChallengePersistenceStatus _persistence;
    private bool _proof;
    private bool _disposed;
    private string _status = "Ready";
    public ChallengeSessionCoordinator(ChallengeProgressRepository repository,
        Func<ChallengeDefinition, LadderProgram, ChallengeRunIdentity, CancellationToken, ChallengeRunResult> runner,
        Func<TimeSpan> monotonicNow, ChallengeDefinition trustedDefinition,
        byte[] actualStarter, ChallengeSemanticManifest trustedManifest, Func<DateTimeOffset>? utcNow = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        _now = monotonicNow ?? throw new ArgumentNullException(nameof(monotonicNow));
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _packetHash = ChallengeContentIdentity.PacketSha256(trustedDefinition, actualStarter);
        _starterBytes = actualStarter.ToArray();
        _plantHash = ChallengeContentIdentity.PlantSha256(trustedDefinition.Plant);
        _manifest = trustedManifest ?? throw new ArgumentNullException(nameof(trustedManifest));
        var loaded = repository.Load();
        _progress = loaded.Records;
        if (loaded.Issues.Count > 0) _status = string.Join("; ", loaded.Issues);
    }
    public ChallengePresentationState State => new(_phase,
        _lifecycle.State.IsWorkerOutstanding ? _identity : null, _lastResult, _proof,
        !_disposed && !_lifecycle.State.IsWorkerOutstanding && _lifecycle.State.PendingAction == ChallengePendingAction.None,
        _lifecycle.State.IsWorkerOutstanding, _lifecycle.State.MutationLocked,
        _lifecycle.State.PendingAction, _persistence, _status);
    public IReadOnlyList<ChallengeProgressRecord> Progress => _progress;
    public event Action<ChallengePendingAction>? PendingActionReady;
    public bool Begin(ChallengeDefinition definition, LadderProgram original, ChallengeRunIdentity identity, IReadOnlyList<string> hints)
    {
        AssertOwner();
        if (_disposed || !State.CanTest) return false;
        _phase = ChallengeUiState.Validating;
        _proof = false;
        _persistence = ChallengePersistenceStatus.NotAttempted;
        try
        {
            // Inspect original metadata before serialization can drop it.
            var issues = ChallengeEligibilityValidator.Validate(original);
            if (issues.Count > 0) { _phase = ChallengeUiState.Finished; _status = "Invalid program: " + issues[0].Message; return false; }
            if (identity.IdentityFormat != ChallengeProgramIdentity.IdentityFormat || identity.ChallengeId != definition.Id
                || identity.ChallengeRevision != definition.Revision || identity.ScanPeriodMs != definition.ScanPeriodMs
                || identity.ProgramSha256 != ChallengeProgramIdentity.ProgramSha256(original)
                || identity.ChallengeSha256 != _packetHash || identity.PlantSha256 != _plantHash
                || identity.EngineRevision != _manifest.EngineRevision || identity.EngineSha256 != _manifest.EngineSha256
                || identity.EvaluatorRevision != _manifest.EvaluatorRevision || identity.EvaluatorSha256 != _manifest.EvaluatorSha256
                || ChallengeContentIdentity.PlantSha256(definition.Plant) != _plantHash
                || ChallengeContentIdentity.PacketSha256(definition, _starterBytes) != _packetHash)
            { _phase = ChallengeUiState.Finished; _status = "Invalid run identity"; return false; }
            var frozen = LadderProgramJson.Load(LadderProgramJson.Save(original)).Program;
            if (frozen is null || ChallengeEligibilityValidator.Validate(frozen).Count > 0)
            { _phase = ChallengeUiState.Finished; _status = "Invalid cloned program"; return false; }
            var frozenHints = Array.AsReadOnly(hints.Distinct(StringComparer.Ordinal).ToArray());
            if (!_lifecycle.TryBegin(identity.RunEpoch)) { _phase = ChallengeUiState.Finished; _status = "Run epoch unavailable"; return false; }
            _identity = identity;
            _definition = definition;
            _hints = frozenHints;
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;
            _phase = ChallengeUiState.Running;
            _status = "Testing seven offline cases";
            _worker = Task.Run(() => _runner(definition, frozen, identity, token));
            return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.Text.Json.JsonException)
        { _phase = ChallengeUiState.Finished; _status = "Validation failed: " + error.Message; return false; }
    }
    public void Poll()
    {
        AssertOwner();
        if (_disposed) return;
        var expired = _lifecycle.ExpirePendingAction(_now());
        if (expired != ChallengePendingAction.None) _status = "Cleanup is taking longer; request " + expired + " again after it finishes";
        if (_worker is not { IsCompleted: true }) return;
        var identity = _identity!;
        ChallengeRunResult result;
        try { result = _worker.GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { result = Failure(identity, ChallengeOutcome.Cancelled, "CH_CANCELLED", "Test cancelled"); }
        catch (Exception error) { result = Failure(identity, ChallengeOutcome.InfrastructureError, "CH_WORKER_ERROR", error.Message); }
        var eligible = _lifecycle.Complete(identity.RunEpoch);
        _worker = null;
        _cancellation?.Dispose();
        _cancellation = null;
        _phase = ChallengeUiState.Finished;
        if (eligible)
        {
            if (result.Identity != identity)
                result = Failure(identity, ChallengeOutcome.InfrastructureError, "CH_IDENTITY_MISMATCH", "Worker result did not match the frozen run identity");
            if (result.Outcome == ChallengeOutcome.Passed && !CompletePassingLedger(result))
                result = Failure(identity, ChallengeOutcome.InfrastructureError, "CH_INCOMPLETE_LEDGER", "Passed result lacks seven ordered passing cases");
            _lastResult = result;
            _status = result.Outcome == ChallengeOutcome.Passed ? "Passed" : result.Outcome + ": " + result.Diagnostic;
            _proof = result.Outcome == ChallengeOutcome.Passed;
            if (_proof)
            {
                var record = new ChallengeProgressRecord(identity, _utcNow(), _hints);
                // Epoch is cancellation ownership, not content identity. Keep
                // passes from older packet/plant/engine versions in history.
                var persistedIdentity = identity with { RunEpoch = 0 };
                var candidate = _progress.Where(p => (p.Identity with { RunEpoch = 0 }) != persistedIdentity)
                    .Append(record).TakeLast(1000).ToArray();
                var saved = _repository.Save(candidate);
                _persistence = saved.Success ? ChallengePersistenceStatus.Saved : ChallengePersistenceStatus.NotSaved;
                if (saved.Success) _progress = Array.AsReadOnly(candidate);
                else _status = "Passed / progress not saved: " + saved.Diagnostic;
            }
        }
        else { _proof = false; if (expired == ChallengePendingAction.None) _status = "Test cancelled; previous result is historical"; }
        // Drain before a callback can request a new worker. An expired intent
        // is never replayed, including completion between polling ticks.
        var pending = _lifecycle.TakePendingActionAfterCleanup(_now());
        if (pending != ChallengePendingAction.None) PendingActionReady?.Invoke(pending);
    }
    public void Cancel()
    {
        AssertOwner();
        if (_identity is not null && _lifecycle.Retire(_identity.RunEpoch))
        { _proof = false; _phase = ChallengeUiState.Cancelling; _status = "Cancelling; waiting for worker cleanup"; _cancellation?.Cancel(); }
    }
    public void DraftChanged()
    {
        AssertOwner(); _proof = false;
        if (_lifecycle.State.IsWorkerOutstanding) Cancel();
        else if (_lastResult is not null) _status = "Draft changed; previous result is historical";
    }
    public void RequestAction(ChallengePendingAction action)
    {
        AssertOwner();
        if (_disposed || action is not (ChallengePendingAction.Navigate or ChallengePendingAction.Close or ChallengePendingAction.Reset)) return;
        if (_lifecycle.State.IsWorkerOutstanding)
        { _lifecycle.QueuePendingAction(action, _now()); Cancel(); }
        else PendingActionReady?.Invoke(action);
    }
    public void Dispose()
    {
        AssertOwner();
        if (_disposed) return;
        Cancel(); _disposed = true;
        var cancellation = _cancellation;
        _cancellation = null;
        if (_worker is null) cancellation?.Dispose();
        else _ = _worker.ContinueWith(task => { _ = task.Exception; cancellation?.Dispose(); }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
    private bool CompletePassingLedger(ChallengeRunResult result) => _definition is not null
        && result.Cases.Count == _definition.Cases.Count
        && result.Cases.Select(c => c.CaseId).SequenceEqual(_definition.Cases.Select(c => c.Id), StringComparer.Ordinal)
        && result.Cases.All(c => c.Outcome == ChallengeOutcome.Passed);
    private static ChallengeRunResult Failure(ChallengeRunIdentity id, ChallengeOutcome outcome, string code, string diagnostic) => new(id,outcome,[],[],[],code,diagnostic);
    private void AssertOwner()
    {
        if (Environment.CurrentManagedThreadId != _ownerThread) throw new InvalidOperationException("Coordinator must be called on its owning UI thread");
    }
}
