using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public sealed record InstructionPresentationRow(string Label, string Value, string? Identity = null)
{
    public string Text => Label + ": " + Value + (string.IsNullOrEmpty(Identity) ? "" : "  [" + Identity + "]");
}
public sealed record InstructionContextChoice(string Key, string Label);
public sealed record InstructionPresentation(string CatalogKey, string Status, string Provenance,
    IReadOnlyList<InstructionPresentationRow> Rows, IReadOnlyList<InstructionContextChoice> Contexts,
    bool Partial, bool CurrentExecution, bool EarlierCycle, ObservationUnavailableReason UnavailableReason)
{
    public string Detail => string.Join("\n", new[] { Status, Provenance }.Concat(Rows.Select(r => r.Text)));
}

/// <summary>Pure presentation of captured execution. Never evaluates PLC expressions or substitutes
/// final memory for consumed operands. The canvas and detail inspector share this model.</summary>
public static class LadderInstructionPresentation
{
    private static readonly string[] BaseKeys = ["no", "nc", "edge-rising", "edge-falling", "coil", "set", "reset", "branch", "rung",
        "ton", "tof", "tp", "rto", "timer-reset", "ctu", "ctd", "counter-load", "counter-reset", "call", "return", "jump", "label"];
    public static IReadOnlyList<string> CatalogKeys { get; } = Array.AsReadOnly(BaseKeys.Concat(Enumerable.Range(0, 7).Select(n => "compare-" + n))
        .Concat(Enumerable.Range(0, 25).Select(n => "numeric-" + n)).ToArray());

    public static string ContextKey(InstructionObservation observation) => observation.TaskId.Length + ":" + observation.TaskId +
        string.Concat(observation.CallPath.Select(p => "|" + p.CallSiteId.Length + ":" + p.CallSiteId + "/" + p.TargetBlockId.Length + ":" + p.TargetBlockId +
            "/" + (p.InstanceId?.Length ?? 0) + ":" + p.InstanceId));

    /// <summary>
    /// Returns the Boolean flow leaving an instruction box in monitored LAD.
    /// IEC timer/counter boxes expose Q as their outgoing Boolean result; their
    /// input rung state only controls instruction execution and is not a
    /// substitute for Q. Other terminal instructions retain their captured
    /// incoming network flow unless their renderer provides instruction-specific
    /// ENO behavior.
    /// </summary>
    public static bool? OutputPowerFlow(InstructionObservation? observation)
    {
        if (observation is not { Evaluated: true }) return null;
        return observation.CatalogKey switch
        {
            "ton" or "tof" or "tp" or "rto" when observation.StoredAfter is LadderTimerState timer => timer.Done,
            "ctu" or "ctd" when observation.StoredAfter is LadderCounterState counter => counter.Done,
            "call" => observation.LocalResult,
            _ => observation.NetworkEnable
        };
    }

    public static InstructionPresentation Present(string instructionId, string key, ControllerPublication? publication,
        bool draftMatches = true, bool draftValid = true, string? selectedContext = null,
        ObservationUnavailableReason unavailable = ObservationUnavailableReason.None)
    {
        if (!CatalogKeys.Contains(key, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(key), key, "Unclassified instruction.");
        var reason = unavailable != ObservationUnavailableReason.None ? unavailable : !draftValid ? ObservationUnavailableReason.InvalidDraft
            : !draftMatches ? ObservationUnavailableReason.DraftMismatch : publication is null ? ObservationUnavailableReason.NoProgramLoaded
            : publication.Observations is null || publication.ObservationOrigin is null ? ObservationUnavailableReason.NoExecutionYet : ObservationUnavailableReason.None;
        if (reason != ObservationUnavailableReason.None) return Missing(key, reason);
        var envelope = publication!;
        var origin = envelope.ObservationOrigin!;
        if (origin.SessionId != envelope.SessionId || origin.LoadId != envelope.LoadId || origin.ExecutionGeneration != envelope.ExecutionGeneration)
            return Missing(key, ObservationUnavailableReason.CaptureFault);
        return PresentCapture(instructionId, key, envelope.Observations!, envelope.Snapshot, selectedContext,
            envelope.Kind == ControllerPublicationKind.ScanCompleted && envelope.Completion == ControllerCompletion.Completed,
            $"Last execution: generation {origin.ExecutionGeneration}, attempt {origin.ScanAttemptNumber} · publication {envelope.Revision} {envelope.Kind}", origin);
    }

    // Standalone canvas QA and legacy callers can supply a captured runtime scan. Its provenance is
    // explicitly local; production shell uses the identity-checked publication overload above.
    public static InstructionPresentation PresentSnapshot(string instructionId, string key, VirtualControllerSnapshot? snapshot, string? selectedContext = null)
        => snapshot?.Observations is not { } capture ? Missing(key, snapshot is null ? ObservationUnavailableReason.NoProgramLoaded : ObservationUnavailableReason.NoExecutionYet)
            : PresentCapture(instructionId, key, capture, snapshot, selectedContext, snapshot.State == VirtualControllerState.Running && snapshot.Completion == ControllerCompletion.Completed,
                $"Local runtime capture · scan {snapshot.ScanNumber} · {snapshot.Completion}", null);

    private static InstructionPresentation PresentCapture(string id, string key, InstructionObservationCapture capture, VirtualControllerSnapshot snapshot,
        string? selectedContext, bool current, string provenance, ObservationOrigin? origin)
    {
        var contexts = capture.ContextsForInstruction(id).Where(o => !o.PathTruncated)
            .GroupBy(ContextKey).Select(g => g.OrderByDescending(o => o.ExecutionOrdinal).First()).OrderBy(o => o.TaskId, StringComparer.Ordinal)
            .ThenBy(o => o.ExecutionOrdinal).ToArray();
        var choices = new[] { new InstructionContextChoice("", "Global last invocation this scan") }.Concat(contexts.Select(o =>
            new InstructionContextChoice(ContextKey(o), o.TaskId + " / " + string.Join(" → ", o.CallPath.Select(p => p.TargetBlockId +
                (p.InstanceId is null ? "" : " [" + p.InstanceId + "]") + " @" + p.CallSiteId))))).ToArray();
        var lookup = string.IsNullOrEmpty(selectedContext) ? capture.Find(id)
            : new ObservationLookup(contexts.FirstOrDefault(o => ContextKey(o) == selectedContext), ObservationUnavailableReason.ContextNotCaptured);
        if (lookup.Observation is not { } observation)
            return Missing(key, lookup.Reason) with { Contexts = choices, Partial = capture.IsPartial, Provenance = provenance };
        if (observation.CatalogKey != key || origin is not null && observation.Origin != origin)
            return Missing(key, ObservationUnavailableReason.CaptureFault) with { Contexts = choices };
        if (!observation.Evaluated) return Missing(key, ObservationUnavailableReason.NotEvaluatedThisScan) with { Contexts = choices };

        var rows = new List<InstructionPresentationRow>();
        void Row(string label, object? value, string? identity = null) => rows.Add(new(label, Value(value), identity));
        foreach (var operand in observation.Operands)
        {
            if (operand.Reason == ObservationUnavailableReason.NotApplicable) continue;
            var role = key == "ctd" && operand.Role == "A" ? "LD"
                : key == "ctu" && operand.Role == "A" ? "R"
                : key == "rto" && operand.Role == "A" ? "R"
                : key is "numeric-18" or "numeric-19" ? operand.Role switch { "A" => "MIN", "B" => "VALUE", "C" => "MAX", _ => operand.Role }
                : operand.Role;
            var text = operand.Reason == ObservationUnavailableReason.None ? Value(operand.Value) : UnavailableText(operand.Reason);
            if (operand.Unit is { } unit) text += " " + unit;
            if (operand.DeclaredType is { } type) text += " (" + type.ToString().ToUpperInvariant() + ")";
            if (operand.TextTruncated) text += $" … [{operand.TotalTextLength - ((string)operand.Value!).Length} code units omitted; full consumed text unavailable]";
            rows.Add(new("Consumed " + role, text, operand.Identity));
        }
        Row("Network enable", observation.NetworkEnable);
        if (key is "no" or "nc" or "edge-rising" or "edge-falling" or "branch" or "rung" || key.StartsWith("compare-", StringComparison.Ordinal))
            Row("Local result", observation.LocalResult);
        if (key is "edge-rising" or "edge-falling" or "ctu" or "ctd")
        {
            rows.Add(new("Previous box input", observation.PreviousKnown ? Value(observation.PreviousInput) : "Baseline unknown — first evaluation is not a pulse"));
            Row("Box input", observation.CurrentInput);
        }
        if (observation.ConfiguredPreset is not null) Row("Configured preset", observation.ConfiguredPreset);
        if (observation.StoredAfter is LadderCounterState counter)
        {
            Row("Current count", counter.Accumulated); Row("Instance preset", counter.Preset);
            Row("Instance count input", counter.CountInput); Row("Done", counter.Done);
        }
        else if (observation.StoredAfter is LadderTimerState timer)
        {
            Row("Elapsed", timer.Accumulated); Row("Instance preset", timer.Preset);
            Row("Instance input", timer.Input); Row("Timing", timer.Timing); Row("Done", timer.Done);
        }
        if (observation.DestinationIdentity.Length > 0)
        {
            Row("Before", observation.DestinationBefore, observation.DestinationIdentity);
            if (observation.CalculatedResult is not null) Row("Calculated", observation.CalculatedResult);
            Row("Stored after", observation.StoredAfter, observation.DestinationIdentity);
            var final = FinalValue(snapshot, observation.DestinationIdentity);
            rows.Add(new("Final snapshot", final.Found ? Value(final.Value) : "Unavailable in published global image", observation.DestinationIdentity));
            if (snapshot.Forces.ContainsKey(observation.DestinationIdentity)) rows.Add(new("Publication override", "Forced after program writes"));
        }
        if (observation.ControlOutcome.Length > 0) rows.Add(new("Control", observation.ControlOutcome));
        if (observation.CalledFrameInvocationId is { } calledFrame) Row("Called frame", calledFrame);
        if (observation.CalledInstanceId is { } calledInstance) rows.Add(new("Called instance", calledInstance));
        if (observation.Rounded || observation.Clamped) rows.Add(new("Normalization", string.Join(", ", new[] { observation.Rounded ? "rounded half to even" : "", observation.Clamped ? "clamped" : "" }.Where(s => s.Length > 0))));
        rows.Add(new("Effect", DispositionText(observation.WriteDisposition)));
        rows.Add(new("Invocations this scan", observation.ObservationCountInScan.ToString(CultureInfo.InvariantCulture) + " global / " + observation.ContextCountInScan + " selected path"));
        rows.Add(new("Context", observation.TaskId + " / " + observation.BlockId + " / frame " + observation.FrameInvocationId + " / ordinal " + observation.ExecutionOrdinal));
        rows.Add(new("Call path", observation.PathTruncated ? "Truncated — exact instance selection unavailable" : string.Join(" → ", observation.CallPath.Select(p => p.TargetBlockId + " @" + p.CallSiteId + (p.InstanceId is null ? "" : " [" + p.InstanceId + "]")))));
        foreach (var diagnostic in observation.Diagnostics) rows.Add(new("Fault " + diagnostic.Code, diagnostic.Message, diagnostic.OperandRole));
        var status = capture.IsPartial ? "PARTIAL / ABORTED EXECUTION" : current ? "OBSERVED THIS SCAN" : "LAST EXECUTION — retained observation";
        return new(key, status, provenance, rows.AsReadOnly(), choices, capture.IsPartial, current && !capture.IsPartial, false, ObservationUnavailableReason.None);
    }

    private static (bool Found, object? Value) FinalValue(VirtualControllerSnapshot snapshot, string identity)
    {
        if (snapshot.Variables.TryGetValue(identity, out var b)) return (true, b);
        if (snapshot.NumericVariables.TryGetValue(identity, out var n)) return (true, n);
        if (snapshot.TextVariables.TryGetValue(identity, out var s)) return (true, s);
        if (snapshot.Timers.TryGetValue(identity, out var t)) return (true, t);
        if (snapshot.Counters.TryGetValue(identity, out var c)) return (true, c);
        return (false, null);
    }
    private static InstructionPresentation Missing(string key, ObservationUnavailableReason reason) => new(key, UnavailableText(reason), "", [], [], false, false, false, reason);
    public static string Value(object? value) => value switch
    {
        null => "Unavailable", bool b => b ? "TRUE" : "FALSE", string s => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\"",
        TimeSpan t => t.TotalMilliseconds.ToString("G17", CultureInfo.InvariantCulture) + " ms",
        LadderCounterState c => $"CV {c.Accumulated}; PV {c.Preset}; input {Value(c.CountInput)}; done {Value(c.Done)}",
        LadderTimerState t => $"ET {Value(t.Accumulated)}; PT {Value(t.Preset)}; input {Value(t.Input)}; timing {Value(t.Timing)}; done {Value(t.Done)}",
        double n => n.ToString("G17", CultureInfo.InvariantCulture), float n => n.ToString("G9", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture), _ => value.ToString() ?? "Unavailable",
    };
    public static string DispositionText(InstructionWriteDisposition disposition) => disposition switch
    {
        InstructionWriteDisposition.NotApplicable => "Read/control only — no destination write",
        InstructionWriteDisposition.SkippedDisabled => "Skipped — enable FALSE; no write",
        InstructionWriteDisposition.WrittenChanged => "Written — value changed",
        InstructionWriteDisposition.WrittenSameValue => "Written — same value",
        InstructionWriteDisposition.StateUpdatedChanged => "State evaluated — changed",
        InstructionWriteDisposition.StateUpdatedSameValue => "State evaluated — unchanged",
        InstructionWriteDisposition.RetainedNoWrite => "Retained — no write",
        InstructionWriteDisposition.FailedNoWrite => "Failed — destination retained",
        InstructionWriteDisposition.FailedClearedDestination => "Failed — destination cleared",
        InstructionWriteDisposition.FailedInvalidValueWritten => "Failed — invalid value written",
        InstructionWriteDisposition.FailedErrorValueWritten => "Failed — defined error value written",
        _ => throw new ArgumentOutOfRangeException(nameof(disposition)),
    };
    public static string UnavailableText(ObservationUnavailableReason reason) => reason switch
    {
        ObservationUnavailableReason.None => "Available", ObservationUnavailableReason.NoProgramLoaded => "Not monitored — no program loaded",
        ObservationUnavailableReason.DraftMismatch => "Not monitored — draft differs from loaded program", ObservationUnavailableReason.InvalidDraft => "Not monitored — invalid draft",
        ObservationUnavailableReason.NoExecutionYet => "No execution yet", ObservationUnavailableReason.NotEvaluatedThisScan => "Not evaluated this scan",
        ObservationUnavailableReason.ActionDisabled => "Not consumed — action disabled", ObservationUnavailableReason.PartialScanNotReached => "Not observed — scan aborted before instruction",
        ObservationUnavailableReason.ObservationCapacityExceeded => "Observation capacity exceeded", ObservationUnavailableReason.ContextNotCaptured => "Selected context not captured",
        ObservationUnavailableReason.HistoryEvicted => "Selected history evicted", ObservationUnavailableReason.OperandMissing => "Operand missing — capture invariant fault",
        ObservationUnavailableReason.UnsupportedRuntimeBinding => "Unsupported runtime binding", ObservationUnavailableReason.NotApplicable => "Not applicable",
        ObservationUnavailableReason.CaptureFault => "Observation identity/capture fault", _ => throw new ArgumentOutOfRangeException(nameof(reason)),
    };
}
