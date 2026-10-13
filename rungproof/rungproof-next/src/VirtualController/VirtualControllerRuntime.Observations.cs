using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RungProof.Next.VirtualController;

public sealed partial class VirtualControllerRuntime
{
    private sealed class ObservationDraft
    {
        public string Id = "", Key = "", Destination = "";
        public object? Before, Calculated, Preset;
        public int DiagnosticStart;
        public bool PreviousKnown;
        public bool? PreviousInput;
        public ObservationDraft? Parent;
        public long? CalledFrameId;
        public readonly List<ObservationOperandValue> Operands = [];
    }
    private static readonly object ObservedTrue = true, ObservedFalse = false;
    private readonly Dictionary<(string Role, string Identity, PlcVariableType? Type, ObservationUnavailableReason Reason, string? Unit), ObservationOperandDescriptor> _operandDescriptors = new();
    private readonly Dictionary<(string Id, string Catalog, string Block, string Task, string Destination), BooleanObservationMetadata> _booleanMetadata = new();
    private ObservationOperandValue Operand(string role, string identity, PlcVariableType? type, bool value)
        => Operand(role, identity, type, value ? ObservedTrue : ObservedFalse);
    private ObservationOperandValue Operand(string role, string identity, PlcVariableType? type, object? value,
        ObservationUnavailableReason reason = ObservationUnavailableReason.None, string? unit = null)
    {
        var key = (role, identity, type, reason, unit);
        if (!_operandDescriptors.TryGetValue(key, out var descriptor))
        {
            descriptor = new(role, identity, type, reason, unit);
            if (_operandDescriptors.Count < 100000) _operandDescriptors.Add(key, descriptor);
        }
        return new(descriptor, value is bool b ? b ? ObservedTrue : ObservedFalse
            : value is string text && text.Length > 4096 ? new InstructionOperand(role, identity, type, text, reason, unit) : value);
    }
    private readonly Stack<ObservationDraft> _observationDraftPool = new();
    private readonly Dictionary<long, ObservationCallPath> _observedPaths = new();
    private ObservationDraft RentObservationDraft()
        => _observationDraftPool.Count == 0 ? new ObservationDraft() : _observationDraftPool.Pop();
    private void ReturnObservationDraft(ObservationDraft draft)
    {
        // The observation constructor copied operands before this builder is reused.
        draft.Id = draft.Key = draft.Destination = "";
        draft.Before = draft.Calculated = draft.Preset = null;
        draft.DiagnosticStart = 0; draft.PreviousKnown = false; draft.PreviousInput = null;
        draft.Parent = null; draft.CalledFrameId = null; draft.Operands.Clear();
        if (_observationDraftPool.Count < 128) _observationDraftPool.Push(draft);
    }
    private InstructionObservationCollector? _collector;
    private InstructionObservationCollector? _reusableCollector;
    private InstructionObservationCapture? _observations;
    private ObservationDraft? _activeObservation;
    private ExecutionItem? _observationContext;
    private ControllerCompletion _completion;
    private VirtualControllerSignalImage? _programEndValues;
    private long _ordinal, _nextFrame;
    private int _completedNetworks;
    private string? _lastFullyObservedId;
    private long _progressOrdinal;
    private string? _programContentId;
    private string[]? _observableIds;
    public bool MonitoringEnabled { get; set; } = true;
    public bool MonitoringCapacityExceeded => (_observableIds??=ObservableIds().ToArray()).Distinct(StringComparer.Ordinal).Count()>InstructionObservationCollector.RegisteredIdCapacity;
    public string ProgramContentId => _programContentId??=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
        LadderProgramJson.Save(_program.Source with { WatchVariables = null }))));

    private IEnumerable<string> ObservableIds()
    {
        static IEnumerable<string> Nodes(LadderNode node)
        {
            yield return node.Id;
            foreach (var child in node.Children ?? []) foreach (var id in Nodes(child)) yield return id;
        }
        foreach (var network in _blocks.Values.SelectMany(b => b.Networks))
        {
            yield return network.Id;
            foreach (var id in Nodes(network.Logic)) yield return id;
            var action = ActionIdentity(network);
            if (action.Id.Length > 0) yield return action.Id;
        }
    }
    private void BeginObservationAttempt()
    {
        _ordinal = 0; _nextFrame = 0; _activeObservation = null; _observedPaths.Clear();
        _completedNetworks=0;_lastFullyObservedId=null;_progressOrdinal=0;
        _collector = MonitoringEnabled ? _reusableCollector ??= new InstructionObservationCollector(_observableIds??=ObservableIds().ToArray()) : null;
        _collector?.Reset();
        _observations = null; _completion = ControllerCompletion.NoExecution;
    }
    private ObservationDraft? BeginNodeObservation(LadderNode node)
    {
        if (_collector is null) return null;
        var draft = RentObservationDraft();
        draft.Id = node.Id; draft.Key = node.Kind switch
        {
            LadderNodeKind.Contact => node.EdgeMode switch { LadderEdgeMode.Rising => "edge-rising", LadderEdgeMode.Falling => "edge-falling", _ => node.NormallyClosed ? "nc" : "no" },
            LadderNodeKind.Compare => $"compare-{(int)node.CompareOperator}",
            LadderNodeKind.Parallel => "branch", _ => "rung"
        }; draft.DiagnosticStart = _diagnostics.Count; draft.Parent = _activeObservation;
        if (node.Kind == LadderNodeKind.Contact && node.EdgeMode != LadderEdgeMode.None)
        {
            draft.PreviousKnown = _edgeInputs.TryGetValue(InstructionMemoryKey(node.Id), out var input);
            draft.PreviousInput = draft.PreviousKnown ? input : null;
        }
        _activeObservation = draft; return draft;
    }
    private void EndNodeObservation(ObservationDraft? draft, LadderNode node, bool result, bool incomingContinuity)
    {
        RecordProgress(node.Id);
        if (draft is null) return;
        if (node.Children is not null)
            foreach (var child in node.Children) draft.Operands.Add(Operand("Child", child.Id, PlcVariableType.Bool, _elements[child.Id].Energized));
        var current = draft.Operands.Count == 0 ? null : draft.Operands[0].Value as bool?;
        var disposition = node.EdgeMode == LadderEdgeMode.None ? InstructionWriteDisposition.NotApplicable
            : !draft.PreviousKnown || draft.PreviousInput != current ? InstructionWriteDisposition.StateUpdatedChanged : InstructionWriteDisposition.StateUpdatedSameValue;
        RecordDraft(draft, result, incomingContinuity, disposition,
            current: node.Kind == LadderNodeKind.Contact ? current : null);
        _activeObservation = draft.Parent;
        ReturnObservationDraft(draft);
    }
    private static (string Id, string Key, string Destination, object? Preset) ActionIdentity(LadderNetwork n)
    {
        if (n.Coil is {} coil) return (coil.Id, coil.Mode switch { LadderCoilMode.Set => "set", LadderCoilMode.Reset => "reset", _ => "coil" }, coil.Variable, null);
        if (n.Timer is {} timer) return (timer.Id, timer.Kind switch { LadderTimerKind.OffDelay => "tof", LadderTimerKind.Pulse => "tp", LadderTimerKind.RetentiveOnDelay => "rto", _ => "ton" }, timer.Variable, timer.Preset);
        if (n.TimerReset is {} tr) return (tr.Id, "timer-reset", tr.Variable, null);
        if (n.Counter is {} counter) return (counter.Id, counter.Kind == LadderCounterKind.CountUp ? "ctu" : "ctd", counter.Variable, counter.Preset);
        if (n.CounterReset is {} cr) return (cr.Id, "counter-reset", cr.Variable, null);
        if (n.CounterLoad is {} cl) return (cl.Id, "counter-load", cl.Variable, cl.Preset);
        if (n.NumericOperation is {} op) return (op.Id, $"numeric-{(int)op.Kind}", op.Destination, null);
        if (n.Call is {} call) return (call.Id, "call", "", null);
        if (n.Return is {} ret) return (ret.Id, "return", "", null);
        if (n.Jump is {} jump) return (jump.Id, "jump", "", null);
        if (n.LabelInstruction is {} label) return (label.Id, "label", "", null);
        return ("", "", "", null);
    }
    private object? StorageValue(string name)
    {
        if (_values.TryGetValue(name, out var b)) return b ? ObservedTrue : ObservedFalse;
        if (_numericValues.TryGetValue(name, out var n)) return n;
        if (_textValues.TryGetValue(name, out var t)) return t;
        if (_timers.TryGetValue(name, out var timer)) return timer;
        if (_counters.TryGetValue(name, out var counter)) return counter;
        return null;
    }
    private ObservationDraft? BeginActionObservation(LadderNetwork network, bool enable)
    {
        if (_collector is null) return null;
        var action = ActionIdentity(network);
        if (action.Id.Length == 0) return null;
        var draft = RentObservationDraft();
        draft.Id = action.Id; draft.Key = action.Key; draft.Destination = action.Destination;
        draft.Before = StorageValue(action.Destination); draft.Preset = action.Preset; draft.DiagnosticStart = _diagnostics.Count;
        if (network.Counter is {} counter) { draft.PreviousKnown = true; draft.PreviousInput = _counterInputs.GetValueOrDefault(CounterMemoryKey(counter)); }
        _activeObservation = draft; return draft;
    }
    private void EndActionObservation(ObservationDraft? draft, LadderNetwork network, bool enable)
    {
        var identity=ActionIdentity(network);
        if(identity.Id.Length>0)RecordProgress(identity.Id);
        if (draft is null) return;
        var after = StorageValue(draft.Destination);
        var failed = _diagnostics.Count > draft.DiagnosticStart;
        var state = network.Timer is not null || network.TimerReset is not null || network.Counter is not null || network.CounterLoad is not null || network.CounterReset is not null;
        var always = network.Timer is not null || network.Counter is not null || network.Coil?.Mode == LadderCoilMode.Assign;
        var disposition = draft.Destination.Length == 0 ? InstructionWriteDisposition.NotApplicable
            : !enable && !always ? InstructionWriteDisposition.SkippedDisabled
            : failed ? network.NumericOperation?.Kind == LadderNumericOperationKind.FormatText ? InstructionWriteDisposition.FailedClearedDestination : InstructionWriteDisposition.FailedNoWrite
            : state ? Equals(draft.Before, after) ? InstructionWriteDisposition.StateUpdatedSameValue : InstructionWriteDisposition.StateUpdatedChanged
            : Equals(draft.Before, after) ? InstructionWriteDisposition.WrittenSameValue : InstructionWriteDisposition.WrittenChanged;
        if (network.NumericOperation is {} operation)
        {
            // Nonconsumed operands are explicit; an enable=false action has no sampled source values.
            foreach (var role in new[] { "A", "B", "C" })
                if (!draft.Operands.Any(o => o.Descriptor.Role == role)) draft.Operands.Add(Operand(role, "", null, null,
                    role == "B" && LadderNumericOperationRules.RequiresSourceB(operation.Kind) || role == "C" && LadderNumericOperationRules.RequiresSourceC(operation.Kind) || role == "A"
                        ? ObservationUnavailableReason.ActionDisabled : ObservationUnavailableReason.NotApplicable));
        }
        RecordDraft(draft, enable, enable, disposition, current: network.Counter is not null ? enable : null,
            control: network.Call is not null ? enable ? "Entered" : "SkippedDisabled" : network.Return is not null || network.Jump is not null ? enable ? "Taken" : "NotTaken" : network.LabelInstruction is not null ? "Visited" : "");
        _activeObservation = null;
        ReturnObservationDraft(draft);
    }
    private void CaptureResolved(string identity, bool value) => CaptureResolved(identity, value ? ObservedTrue : ObservedFalse);
    private void CaptureResolved(string identity, object value)
    {
        if (_activeObservation is not {} draft) return;
        var role = draft.Operands.Count switch { 0 => "A", 1 => "B", _ => "C" };
        PlcVariableType? type = _storageDeclarations.TryGetValue(identity, out var variable) ? variable.Type
            : value is bool ? PlcVariableType.Bool : value is string ? PlcVariableType.String : PlcVariableType.Real;
        var missing=false;
        string? unit=null;
        if(variable is null && !double.TryParse(identity,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out _) && !identity.StartsWith('"'))
        {
            var dot=identity.LastIndexOf('.');var member=dot>0?identity[(dot+1)..]:"";var instance=dot>0?identity[..dot]:"";
            var known=_timers.ContainsKey(instance)&&member is "DN" or "Q" or "TT" or "ET" or "PT"
                ||_counters.ContainsKey(instance)&&member is "DN" or "Q" or "ACC" or "CV" or "PRE" or "PV";
            missing=!known;
            if(_timers.ContainsKey(instance)&&member is "ET" or "PT")unit="ms";
            if(_counters.ContainsKey(instance)&&member is "ACC" or "CV" or "PRE" or "PV")type=PlcVariableType.DInt;
        }
        draft.Operands.Add(Operand(role, identity, type, missing?null:value,missing?ObservationUnavailableReason.OperandMissing:ObservationUnavailableReason.None,unit));
    }
    private string ResolveObservedText(string operand)
    {
        var result = PlcTextValues.Resolve(operand, _textValues); CaptureResolved(operand, result); return result;
    }
    private void RecordDraft(ObservationDraft draft, bool local, bool? enable, InstructionWriteDisposition disposition, bool? current = null, string control = "")
    {
        if (_collector is null || _observationContext is not {} context) return;
        var after = StorageValue(draft.Destination);
        var rounded = false; var clamped = false;
        if (draft.Calculated is double number && _storageDeclarations.TryGetValue(draft.Destination, out var destination) && destination.Type is PlcVariableType.Int or PlcVariableType.DInt)
        {
            var integer = Math.Round(number, MidpointRounding.ToEven);
            rounded = integer != number; clamped = after is double stored && stored != integer;
        }
        var path = context.Path ?? [];
        if (!_observedPaths.TryGetValue(context.FrameId, out var immutablePath))
            _observedPaths.Add(context.FrameId, immutablePath = new ObservationCallPath(path));
        var ordinal = ++_ordinal;
        var prepared = _collector.Prepare(draft.Id, context.TaskId, immutablePath, path.Count > 64);
        if (prepared.Key is null) return;
        var key = (draft.Id, draft.Key, context.BlockId, context.TaskId, draft.Destination);
        if (!_booleanMetadata.TryGetValue(key, out var metadata))
        {
            metadata = new(draft.Id, draft.Key, context.BlockId, context.TaskId, draft.Destination);
            if (_booleanMetadata.Count < 40000) _booleanMetadata.Add(key, metadata);
        }
        var flags = (local ? 1u : 0) | CompactObservationRow.NullableFlag(enable, 1)
            | CompactObservationRow.NullableFlag(draft.Before as bool?, 3) | CompactObservationRow.NullableFlag(after as bool?, 5)
            | (draft.PreviousKnown ? 128u : 0) | CompactObservationRow.NullableFlag(draft.PreviousInput, 8)
            | CompactObservationRow.NullableFlag(current, 10) | (path.Count > 64 ? 4096u : 0)
            | (rounded ? 8192u : 0) | (clamped ? 16384u : 0);
        var booleanOperands = true;
        foreach (var operand in draft.Operands) if (operand.Value is not null and not bool) { booleanOperands = false; break; }
        if (booleanOperands && draft.Before is null or bool && after is null or bool && draft.Calculated is null
            && draft.Preset is null && draft.CalledFrameId is null && control.Length == 0 && !rounded && !clamped
            && _diagnostics.Count == draft.DiagnosticStart)
        {
            _collector.RecordBoolean(metadata, immutablePath, ordinal, context.FrameId, flags, disposition, draft.Operands, prepared);
            return;
        }
        IReadOnlyList<InstructionDiagnostic> diagnostics = _diagnostics.Count == draft.DiagnosticStart ? Array.Empty<InstructionDiagnostic>()
            : _diagnostics.Skip(draft.DiagnosticStart).Select(message => new InstructionDiagnostic(message.Split(' ')[0], draft.Id, "", "Program", message)).ToArray();
        var payload = new RichObservationPayload(draft.Before, draft.Calculated, after, draft.Preset, control, draft.CalledFrameId, null, diagnostics);
        _collector.RecordRich(metadata, immutablePath, ordinal, context.FrameId, flags, disposition, draft.Operands, payload, prepared);
    }
    private void RecordProgress(string id){_lastFullyObservedId=id;_progressOrdinal++;}
    private void ObserveNetwork(LadderNetwork network, bool enable)
    {
        RecordProgress(network.Id);
        if (_collector is null) return;
        var draft = RentObservationDraft();
        draft.Id = network.Id; draft.Key = "rung"; draft.DiagnosticStart = _diagnostics.Count;
        RecordDraft(draft, enable, enable, InstructionWriteDisposition.NotApplicable);
        ReturnObservationDraft(draft);
    }
}


