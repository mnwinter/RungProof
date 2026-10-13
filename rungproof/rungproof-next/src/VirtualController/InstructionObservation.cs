using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum ControllerCompletion { NoExecution, Completed, Aborted, PublicationFailed }
public enum ControllerPublicationKind { Loaded, Reset, Run, Stop, Pause, ForceChanged, ScanCompleted, ScanAborted, PublicationFailed }
public enum InstructionWriteDisposition { NotApplicable, SkippedDisabled, WrittenChanged, WrittenSameValue, StateUpdatedChanged, StateUpdatedSameValue, RetainedNoWrite, FailedNoWrite, FailedClearedDestination }
public enum ObservationUnavailableReason { None, NoProgramLoaded, DraftMismatch, InvalidDraft, NoExecutionYet, NotEvaluatedThisScan, ActionDisabled, PartialScanNotReached, ObservationCapacityExceeded, ContextNotCaptured, HistoryEvicted, OperandMissing, UnsupportedRuntimeBinding, NotApplicable, CaptureFault }
[Flags] public enum ControllerPhaseCompletion { None=0, InputSampled=1, ProgramExecuted=2, OutputsCommitted=4, PlantAdvanced=8, PostPlantSampled=16 }
[Flags] public enum ControllerOutputCommitProgress { None=0, Bool=1, Numeric=2, Text=4 }
public sealed record ObservationOrigin(Guid SessionId, Guid LoadId, long ExecutionGeneration, long ScanAttemptNumber);
public sealed record ObservationCallSite(string CallSiteId, string TargetBlockId, long InvocationOrdinal, string? InstanceId = null);
// Only this assembly can create this immutable backing collection. Publicly supplied
// IReadOnlyList values are still defensively copied by InstructionObservation.
internal sealed class ObservationCallPath : IReadOnlyList<ObservationCallSite>
{
    private readonly ObservationCallSite[] _items;
    internal ObservationCallPath(IReadOnlyList<ObservationCallSite> source)
    {
        _items = new ObservationCallSite[Math.Min(64, source.Count)];
        for (var i = 0; i < _items.Length; i++) _items[i] = source[i];
    }
    public int Count => _items.Length;
    public ObservationCallSite this[int index] => _items[index];
    public IEnumerator<ObservationCallSite> GetEnumerator() => ((IEnumerable<ObservationCallSite>)_items).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
public sealed record InstructionDiagnostic(string Code, string InstructionId, string OperandRole, string Phase, string Message);
public sealed record InstructionOperand(string Role, string Identity, PlcVariableType? DeclaredType, object? Value,
    ObservationUnavailableReason Reason = ObservationUnavailableReason.None, string? Unit = null)
{
    public int TotalTextLength { get; } = Value is string text ? text.Length : 0;
    public bool TextTruncated { get; } = Value is string text && text.Length > 4096;
    public object? Value { get; } = Limit(Value);
    private static object? Limit(object? value)
    {
        if (value is not string text || text.Length <= 4096) return value;
        var length = char.IsHighSurrogate(text[4095]) && char.IsLowSurrogate(text[4096]) ? 4095 : 4096;
        return text[..length];
    }
}
public sealed record InstructionObservation(string InstructionId, string CatalogKey, string BlockId, string TaskId,
    long ExecutionOrdinal, long FrameInvocationId, IReadOnlyList<ObservationCallSite> CallPath,
    IReadOnlyList<InstructionOperand> Operands, bool Evaluated, bool LocalResult)
{
    public IReadOnlyList<ObservationCallSite> CallPath { get; } = CallPath is ObservationCallPath ? CallPath : CallPath.Count == 0 ? Array.Empty<ObservationCallSite>() : Array.AsReadOnly(CallPath.ToArray());
    private readonly IReadOnlyList<InstructionOperand> _operands = Operands.Count == 0 ? Array.Empty<InstructionOperand>() : Array.AsReadOnly(Operands.ToArray());
    public IReadOnlyList<InstructionOperand> Operands { get => _operands; init => _operands = value.Count == 0 ? Array.Empty<InstructionOperand>() : Array.AsReadOnly(value.ToArray()); }
    public ObservationOrigin? Origin { get; init; }
    public bool? NetworkEnable { get; init; }
    public object? DestinationBefore { get; init; }
    public object? CalculatedResult { get; init; }
    public object? StoredAfter { get; init; }
    public string DestinationIdentity { get; init; } = "";
    public InstructionWriteDisposition WriteDisposition { get; init; }
    public bool Rounded { get; init; }
    public bool Clamped { get; init; }
    public bool PreviousKnown { get; init; }
    public bool? PreviousInput { get; init; }
    public bool? CurrentInput { get; init; }
    public object? ConfiguredPreset { get; init; }
    public string ControlOutcome { get; init; } = "";
    public long? CalledFrameInvocationId { get; init; }
    public string? CalledInstanceId { get; init; }
    private readonly IReadOnlyList<InstructionDiagnostic> _diagnostics = Array.Empty<InstructionDiagnostic>();
    public IReadOnlyList<InstructionDiagnostic> Diagnostics { get=>_diagnostics; init=>_diagnostics=value.Count == 0 ? Array.Empty<InstructionDiagnostic>() : Array.AsReadOnly(value.ToArray()); }
    public bool PathTruncated { get; init; }
    public long ObservationCountInScan { get; init; }
    public long ContextCountInScan { get; init; }
}
public readonly record struct InstructionObservationSummary(string InstructionId, string CatalogKey, long ExecutionOrdinal);
public sealed record ObservationLookup(InstructionObservation? Observation, ObservationUnavailableReason Reason);
public sealed record InstructionObservationCapture(
    IReadOnlyDictionary<string, InstructionObservation> GlobalLast,
    IReadOnlyDictionary<string, long> Counts,
    IReadOnlyDictionary<string, InstructionObservation> Contexts,
    IReadOnlyList<InstructionObservation> Trace, long TraceDropped, bool IsPartial,
    bool CapacityExceeded, bool CountSaturated)
{
    public InstructionObservationCapture WithOrigin(ObservationOrigin origin)
    {
        if (FrozenObservationTable.From(this) is {} table) return table.WithOrigin(this, origin);
        // Preserve immutable event sharing across global, context and trace indexes.
        var bound = new Dictionary<InstructionObservation, InstructionObservation>(ReferenceEqualityComparer.Instance);
        InstructionObservation Bind(InstructionObservation value)
        {
            if (!bound.TryGetValue(value, out var result)) bound.Add(value, result = value with { Origin = origin });
            return result;
        }
        IReadOnlyDictionary<string, InstructionObservation> Copy(IReadOnlyDictionary<string, InstructionObservation> values)
        {
            var result = new Dictionary<string, InstructionObservation>(values.Count, StringComparer.Ordinal);
            foreach (var pair in values) result.Add(pair.Key, Bind(pair.Value));
            return new ReadOnlyDictionary<string, InstructionObservation>(result);
        }
        var global = Copy(GlobalLast);
        var contexts = Copy(Contexts);
        var trace = new InstructionObservation[Trace.Count];
        for (var i = 0; i < trace.Length; i++) trace[i] = Bind(Trace[i]);
        return this with { GlobalLast = global, Contexts = contexts, Trace = Array.AsReadOnly(trace) };
    }
    public long EstimatedBytes
    {
        get
        {
            if (FrozenObservationTable.From(this) is {} table) return table.EstimatedBytes;
            // Preserve the conservative per-index charge without temporary arrays.
            long total = 0;
            foreach (var pair in GlobalLast) total += Estimate(pair.Value) + 64L + pair.Key.Length * 2L;
            foreach (var pair in Contexts) total += Estimate(pair.Value) + 64L + pair.Key.Length * 2L;
            for (var i = 0; i < Trace.Count; i++) total += Estimate(Trace[i]);
            foreach (var key in Counts.Keys) total += 64L + key.Length * 2L;
            return total;
        }
    }
    internal static long Estimate(InstructionObservation o)
    {
        long total = 512L + 2L * (o.InstructionId.Length + o.CatalogKey.Length + o.BlockId.Length
            + o.TaskId.Length + o.DestinationIdentity.Length + o.ControlOutcome.Length + (o.CalledInstanceId?.Length ?? 0));
        for (var i = 0; i < o.CallPath.Count; i++) { var p = o.CallPath[i]; total += 128L + 2L * (p.CallSiteId.Length + p.TargetBlockId.Length + (p.InstanceId?.Length ?? 0)); }
        for (var i = 0; i < o.Operands.Count; i++) { var p = o.Operands[i]; total += 128L + 2L * (p.Identity.Length + p.Role.Length + (p.Unit?.Length ?? 0)) + (p.Value is string s ? s.Length * 2L : 64L); }
        for (var i = 0; i < o.Diagnostics.Count; i++) { var d = o.Diagnostics[i]; total += 128L + 2L * (d.Code.Length + d.InstructionId.Length + d.OperandRole.Length + d.Phase.Length + d.Message.Length); }
        total += ValueBytes(o.DestinationBefore) + ValueBytes(o.CalculatedResult) + ValueBytes(o.StoredAfter);
        return total;
    }
    private static long ValueBytes(object? value) => value is string text ? 64L + text.Length * 2L : 64L;
    public IEnumerable<InstructionObservationSummary> GlobalSummaries => FrozenObservationTable.From(this)?.GlobalSummaries
        ?? GlobalLast.Values.Select(o => new InstructionObservationSummary(o.InstructionId, o.CatalogKey, o.ExecutionOrdinal));
    public IEnumerable<InstructionObservation> ContextsForInstruction(string id) => FrozenObservationTable.From(this)?.ContextsForInstruction(id)
        ?? Contexts.Values.Where(o => o.InstructionId == id);
    public ObservationLookup FindContext(string id, string task, long frame)
    {
        if (FrozenObservationTable.From(this) is {} table) return table.FindContext(id, task, frame);
        var value=Contexts.Values.FirstOrDefault(o=>o.InstructionId==id&&o.TaskId==task&&o.FrameInvocationId==frame);
        return value is not null?new(value,ObservationUnavailableReason.None):new(null,ObservationUnavailableReason.ContextNotCaptured);
    }
    public ObservationLookup Find(string id) => GlobalLast.TryGetValue(id, out var value) ? new(value, ObservationUnavailableReason.None)
        : new(null, CapacityExceeded ? ObservationUnavailableReason.ObservationCapacityExceeded : IsPartial ? ObservationUnavailableReason.PartialScanNotReached : ObservationUnavailableReason.NotEvaluatedThisScan);
    public ObservationLookup FindPath(string id, string task, IReadOnlyList<string> callSitePath)
    {
        if (FrozenObservationTable.From(this) is {} table) return table.FindPath(id, task, callSitePath);
        var match=Contexts.Values.Where(o=>o.InstructionId==id&&o.TaskId==task&&o.CallPath.Select(p=>p.CallSiteId).SequenceEqual(callSitePath))
            .OrderByDescending(o=>o.ExecutionOrdinal).FirstOrDefault();
        return match is null?new(null,ObservationUnavailableReason.ContextNotCaptured):new(match,ObservationUnavailableReason.None);
    }
}
public sealed record ControllerPublication(Guid SessionId, string ProgramContentId, Guid LoadId,
    long ExecutionGeneration, long ScanAttemptNumber, long RuntimeScanNumber, long Revision,
    ControllerPublicationKind Kind, ControllerCompletion Completion, ControllerPhaseCompletion Phases,
    ObservationOrigin? ObservationOrigin, InstructionObservationCapture? Observations,
    VirtualControllerSnapshot Snapshot, string? FailurePhase = null, string? FailureCode = null)
{
    public VirtualControllerSignalImage? SampledInputs { get; init; }
    public VirtualControllerSignalImage? EffectiveInputs { get; init; }
    public VirtualControllerSignalImage? ProgramEndValues { get; init; }
    public VirtualControllerSignalImage? PublishedValues { get; init; }
    public VirtualControllerSignalImage? PostPlantInputs { get; init; }
    public ControllerOutputCommitProgress CommitProgress { get; init; }
    public int ExecutedNetworkCount { get; init; }
    public string? LastFullyObservedInstructionId { get; init; }
    public long LastFullyObservedOrdinal { get; init; }
    public IReadOnlyDictionary<string,string> ImageUnavailableReasons { get; init; } = new ReadOnlyDictionary<string,string>(new Dictionary<string,string>());
}
public static class InstructionObservationRegistry
{
    // Explicit base keys and enum-derived operation keys make new enum/catalog additions fail set equality.
    public static IReadOnlyList<string> CatalogKeys { get; } = Array.AsReadOnly(new[] {
        "no", "nc", "edge-rising", "edge-falling", "coil", "set", "reset", "branch", "rung",
        "ton", "tof", "tp", "rto", "timer-reset", "ctu", "ctd", "counter-load", "counter-reset",
        "call", "return", "jump", "label" }.Concat(Enumerable.Range(0,7).Select(v => $"compare-{v}"))
        .Concat(Enumerable.Range(0,25).Select(v => $"numeric-{v}")).ToArray());
}
