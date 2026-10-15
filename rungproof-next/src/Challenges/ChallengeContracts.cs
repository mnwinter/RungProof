using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace RungProof.Next.Challenges;

public enum ChallengeOutcome
{
    Passed, Failed, InvalidProgram, InvalidChallenge, Cancelled, InfrastructureError
}
public enum ChallengeCaseKind
{
    ColdStart, Holding, BeamStop, ProcessStop, SimultaneousStartStop, BlockedStart, ResetRepeat
}
public enum ChallengeTraceEventKind
{
    CaseStarted, ResetCheckpoint, CaseFinished
}
public enum ChallengeUiState
{
    Idle, Validating, Running, Cancelling, Finished
}
public enum ChallengePersistenceStatus
{
    NotAttempted, Saved, NotSaved
}
public enum ChallengePendingAction
{
    None, Navigate, Close, Reset
}
public sealed record ConveyorPlantParameters(double Length, double Speed, double ObjectLength, double PhotoeyePosition, double MinimumPhotoeyeOn);
public sealed record ChallengeTestCase(string Id, ChallengeCaseKind Kind, int MaximumScans);
public sealed record ChallengeGlossaryEntry(string Binding, string DisplayName, string Ownership, string Meaning);
public sealed record ChallengeStarterResource(string ResourceId, string Sha256, long ByteLength);
public sealed record ChallengeDefinition(int SchemaVersion, string Id, int Revision,
    string SceneId, string ProtocolRevision, int ScanPeriodMs, string Title,
    string Objective, IReadOnlyList<string> Hints, ConveyorPlantParameters Plant,
    IReadOnlyList<ChallengeTestCase> Cases, IReadOnlyList<ChallengeGlossaryEntry> Glossary,
    string EligibilityPolicy, ChallengeStarterResource Starter)
{
    public IReadOnlyList<string> Hints { get; } = ChallengeCopies.List(Hints);
    public IReadOnlyList<ChallengeTestCase> Cases { get; } = ChallengeCopies.List(Cases);
    public IReadOnlyList<ChallengeGlossaryEntry> Glossary { get; } = ChallengeCopies.List(Glossary);
}

public sealed record ChallengeDefinitionLoadResult(ChallengeDefinition? Definition, IReadOnlyList<string> Issues)
{
    public IReadOnlyList<string> Issues { get; } = ChallengeCopies.List(Issues);
}

public sealed record ChallengeRunIdentity(string IdentityFormat, string ChallengeId,
    int ChallengeRevision, string ProgramSha256, string ChallengeSha256,
    string PlantSha256, string EngineRevision, string EngineSha256,
    string EvaluatorRevision, string EvaluatorSha256, int ScanPeriodMs, long RunEpoch);
public sealed record ChallengePlantState(IReadOnlyDictionary<string, bool> Feedback,
    IReadOnlyDictionary<string, double> Numbers, IReadOnlyDictionary<string, string> Text,
    bool ObjectPresent, long Completed)
{
    public IReadOnlyDictionary<string, bool> Feedback { get; } = ChallengeCopies.Map(Feedback);
    public IReadOnlyDictionary<string, double> Numbers { get; } = ChallengeCopies.Map(Numbers);
    public IReadOnlyDictionary<string, string> Text { get; } = ChallengeCopies.Map(Text);
}

public sealed record ChallengeObservation(ChallengeRunIdentity Identity, string CaseId,
    int Cycle, long CaseSequence, long ScanNumber, TimeSpan SimulatedTime,
    IReadOnlyDictionary<string, bool> EffectiveInputs,
    IReadOnlyDictionary<string, bool> AppliedOutputs, ChallengePlantState PlantAfter,
    IReadOnlyList<string> RuntimeDiagnostics)
{
    public IReadOnlyDictionary<string, bool> EffectiveInputs { get; } = ChallengeCopies.Map(EffectiveInputs);
    public IReadOnlyDictionary<string, bool> AppliedOutputs { get; } = ChallengeCopies.Map(AppliedOutputs);
    public IReadOnlyList<string> RuntimeDiagnostics { get; } = ChallengeCopies.List(RuntimeDiagnostics);
}

public sealed record ChallengeTraceEvent(string CaseId, int Cycle, long CaseSequence,
    ChallengeTraceEventKind Kind, long ScanNumber, TimeSpan SimulatedTime,
    ChallengePlantState Plant, string Detail);
public sealed record ChallengeCaseResult(string CaseId, ChallengeOutcome Outcome,
    string Code, string Expected, string Observed, ChallengeObservation? FirstFailure);
public sealed record ChallengeRunResult(ChallengeRunIdentity Identity,
    ChallengeOutcome Outcome, IReadOnlyList<ChallengeCaseResult> Cases,
    IReadOnlyList<ChallengeObservation> Trace, IReadOnlyList<ChallengeTraceEvent> Events,
    string Code, string Diagnostic)
{
    public IReadOnlyList<ChallengeCaseResult> Cases { get; } = ChallengeCopies.List(Cases);
    public IReadOnlyList<ChallengeObservation> Trace { get; } = ChallengeCopies.List(Trace);
    public IReadOnlyList<ChallengeTraceEvent> Events { get; } = ChallengeCopies.List(Events);
}

public sealed record ChallengePresentationState(ChallengeUiState State,
    ChallengeRunIdentity? ActiveRun, ChallengeRunResult? LastResult, bool IsCurrentProof,
    bool CanTest, bool IsWorkerOutstanding, bool MutationLocked,
    ChallengePendingAction PendingAction, ChallengePersistenceStatus PersistenceStatus,
    string StatusText);
public sealed record ChallengeCasePresentation(string CaseId, int Index, ChallengeCaseResult? Result);
// Result == null is the presentation label "Not Run", not a grading outcome.
public sealed record ChallengeProgressRecord(ChallengeRunIdentity Identity,
    DateTimeOffset CompletedUtc, IReadOnlyList<string> UsedHintIds)
{
    public IReadOnlyList<string> UsedHintIds { get; } = ChallengeCopies.List(UsedHintIds);
}

public sealed record ChallengeProgressLoadResult(IReadOnlyList<ChallengeProgressRecord> Records, IReadOnlyList<string> Issues)
{
    public IReadOnlyList<ChallengeProgressRecord> Records { get; } = ChallengeCopies.List(Records);
    public IReadOnlyList<string> Issues { get; } = ChallengeCopies.List(Issues);
}

public sealed record ChallengeProgressWriteResult(bool Success, string Diagnostic);
public sealed record ChallengeSemanticSourceEntry(string RelativePath, string Sha256, long ByteLength);
public sealed partial record ChallengeSemanticManifest(int SchemaVersion, string IdentityFormat,
    string EngineRevision, string EngineSha256, string EvaluatorRevision,
    string EvaluatorSha256, string GeneratorSha256,
    IReadOnlyList<ChallengeSemanticSourceEntry> EngineInputs,
    IReadOnlyList<ChallengeSemanticSourceEntry> EvaluatorInputs)
{
    public IReadOnlyList<ChallengeSemanticSourceEntry> EngineInputs { get; } = ChallengeCopies.List(EngineInputs);
    public IReadOnlyList<ChallengeSemanticSourceEntry> EvaluatorInputs { get; } = ChallengeCopies.List(EvaluatorInputs);
}

public sealed record ChallengeSemanticManifestLoadResult(ChallengeSemanticManifest? Manifest, IReadOnlyList<string> Issues)
{
    public IReadOnlyList<string> Issues { get; } = ChallengeCopies.List(Issues);
}

public sealed record ChallengeRunLifecycleState(long? WorkerEpoch, long? AwardEpoch,
    bool IsWorkerOutstanding, bool MutationLocked,
    ChallengePendingAction PendingAction, TimeSpan? PendingActionDeadline);
public interface IChallengePlantAdapter
{
    void Reset();
    IReadOnlyDictionary<string, bool> SampleBoolInputs();
    IReadOnlyDictionary<string, double> SampleNumericInputs();
    IReadOnlyDictionary<string, string> SampleTextInputs();
    void CommitBoolOutputs(IReadOnlyDictionary<string, bool> outputs);
    void CommitNumericOutputs(IReadOnlyDictionary<string, double> outputs);
    void CommitTextOutputs(IReadOnlyDictionary<string, string> outputs);
    void Advance(TimeSpan period);
    ChallengePlantState CaptureState();
}
internal static class ChallengeCopies
{
    public static IReadOnlyList<T> List<T>(IEnumerable<T> source) => Array.AsReadOnly(source.ToArray());
    public static IReadOnlyDictionary<string, T> Map<T>(IReadOnlyDictionary<string, T> source) => new ReadOnlyDictionary<string, T>(source.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
}

