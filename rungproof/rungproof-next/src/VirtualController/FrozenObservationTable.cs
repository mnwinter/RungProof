using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

internal sealed class ObservationOperandDescriptor(string role, string identity, PlcVariableType? type, ObservationUnavailableReason reason, string? unit)
{
    internal string Role { get; } = role;
    internal string Identity { get; } = identity;
    internal PlcVariableType? Type { get; } = type;
    internal ObservationUnavailableReason Reason { get; } = reason;
    internal string? Unit { get; } = unit;
    private InstructionOperand? _true, _false, _null;
    // Only the single-threaded collector freeze path fills these immutable values.
    internal InstructionOperand BooleanValue(object? value) => value is bool boolean
        ? boolean ? _true ??= new(Role, Identity, Type, true, Reason, Unit) : _false ??= new(Role, Identity, Type, false, Reason, Unit)
        : _null ??= new(Role, Identity, Type, null, Reason, Unit);
}
internal readonly record struct ObservationOperandValue(ObservationOperandDescriptor Descriptor, object? Value)
{
    internal InstructionOperand Materialize() => Value is InstructionOperand bounded ? bounded : new(Descriptor.Role, Descriptor.Identity, Descriptor.Type, Value, Descriptor.Reason, Descriptor.Unit);
}
internal readonly record struct RichObservationPayload(object? Before, object? Calculated, object? After, object? Preset,
    string Control, long? CalledFrame, string? CalledInstance, IReadOnlyList<InstructionDiagnostic> Diagnostics);
internal sealed record BooleanObservationMetadata(string Id, string Catalog, string Block, string Task, string Destination);
internal struct CompactObservationRow
{
    internal object? Definition;
    internal InstructionObservation? Full => Definition as InstructionObservation;
    internal BooleanObservationMetadata? Metadata => Definition as BooleanObservationMetadata;
    internal ObservationCallPath? Path;
    internal long Ordinal, Frame, Count, ContextCount;
    internal int OperandOffset, OperandCount, RichIndex;
    internal bool HasBooleanOperands => (Flags & 0x40000000u) != 0;
    internal bool HasRichPayload => (Flags & 0x80000000u) != 0;
    internal uint Flags;
    internal InstructionWriteDisposition Disposition;
    internal string Id => Full?.InstructionId ?? Metadata!.Id;
    internal string Catalog => Full?.CatalogKey ?? Metadata!.Catalog;
    internal string Task => Full?.TaskId ?? Metadata!.Task;
    internal long ExecutionOrdinal => Full?.ExecutionOrdinal ?? Ordinal;
    internal long FrameInvocationId => Full?.FrameInvocationId ?? Frame;
    internal IReadOnlyList<ObservationCallSite> CallPath => Full?.CallPath ?? Path!;
    internal static uint NullableFlag(bool? value, int shift) => value.HasValue ? (1u << shift) | (value.Value ? 2u << shift : 0) : 0;
    internal static bool? ReadFlag(uint flags, int shift) => (flags & (1u << shift)) == 0 ? null : (flags & (2u << shift)) != 0;
    internal InstructionObservation Materialize(ObservationOperandValue[] operands, InstructionOperand[] booleanOperands, RichObservationPayload[] rich, long count, ObservationOrigin? origin)
    {
        if (Full is {} full)
            return (full.ObservationCountInScan == count && origin is null) ? full : full with { ObservationCountInScan = count, Origin = origin ?? full.Origin };
        var metadata = Metadata!;
        var values = new InstructionOperand[OperandCount];
        for (var i = 0; i < values.Length; i++) values[i] = HasBooleanOperands ? booleanOperands[OperandOffset + i] : operands[OperandOffset + i].Materialize();
        var payload = HasRichPayload ? rich[RichIndex] : default;
        return new(metadata.Id, metadata.Catalog, metadata.Block, metadata.Task, Ordinal, Frame, Path!, values, true, (Flags & 1) != 0)
        {
            Origin = origin, NetworkEnable = ReadFlag(Flags, 1), DestinationIdentity = metadata.Destination,
            DestinationBefore = HasRichPayload ? payload.Before : ReadFlag(Flags, 3), StoredAfter = HasRichPayload ? payload.After : ReadFlag(Flags, 5),
            CalculatedResult = payload.Calculated, ConfiguredPreset = payload.Preset,
            Rounded = (Flags & 8192) != 0, Clamped = (Flags & 16384) != 0,
            ControlOutcome = HasRichPayload ? payload.Control : "", CalledFrameInvocationId = payload.CalledFrame, CalledInstanceId = payload.CalledInstance,
            Diagnostics = HasRichPayload ? payload.Diagnostics : Array.Empty<InstructionDiagnostic>(), PreviousKnown = (Flags & 128) != 0,
            PreviousInput = ReadFlag(Flags, 8), CurrentInput = ReadFlag(Flags, 10), PathTruncated = (Flags & 4096) != 0,
            WriteDisposition = Disposition, ObservationCountInScan = count, ContextCountInScan = ContextCount
        };
    }
    internal long Estimate(ObservationOperandValue[] operands, InstructionOperand[] booleanOperands, RichObservationPayload[] rich)
    {
        if (Full is {} full) return InstructionObservationCapture.Estimate(full);
        var metadata = Metadata!;
        long total = 512L + 2L * (metadata.Id.Length + metadata.Catalog.Length + metadata.Block.Length + metadata.Task.Length + metadata.Destination.Length) + 192L;
        for (var i = 0; i < Path!.Count; i++) { var p = Path[i]; total += 128L + 2L * (p.CallSiteId.Length + p.TargetBlockId.Length + (p.InstanceId?.Length ?? 0)); }
        for (var i = 0; i < OperandCount; i++)
        {
            if (HasBooleanOperands)
            {
                var p = booleanOperands[OperandOffset + i];
                total += 128L + 2L * (p.Identity.Length + p.Role.Length + (p.Unit?.Length ?? 0)) + 64L;
            }
            else
            {
                var value = operands[OperandOffset + i]; var p = value.Descriptor;
                var actual = value.Value is InstructionOperand bounded ? bounded.Value : value.Value;
                total += 128L + 2L * (p.Identity.Length + p.Role.Length + (p.Unit?.Length ?? 0)) + (actual is string text ? text.Length * 2L : 64L);
            }
        }
        if (HasRichPayload)
        {
            var payload = rich[RichIndex];
            total += 2L * (payload.Control.Length + (payload.CalledInstance?.Length ?? 0));
            total += ValueExtra(payload.Before) + ValueExtra(payload.Calculated) + ValueExtra(payload.After);
            for (var i = 0; i < payload.Diagnostics.Count; i++) { var d = payload.Diagnostics[i]; total += 128L + 2L * (d.Code.Length + d.InstructionId.Length + d.OperandRole.Length + d.Phase.Length + d.Message.Length); }
        }
        return total;
    }
    private static long ValueExtra(object? value) => value is string text ? text.Length * 2L : 0;
}

// Every array and dictionary passed here is a fresh Freeze-owned copy. The lazy
// records are immutable; thread-safe memoization changes no observable event data.
internal sealed class FrozenObservationTable
{
    private readonly CompactObservationRow[] _rows;
    private readonly ObservationOperandValue[] _operands;
    private readonly RichObservationPayload[] _rich;
    private readonly InstructionOperand[] _booleanOperands;
    private readonly IReadOnlyDictionary<string, int> _global;
    private readonly Dictionary<string, int> _contexts;
    private readonly int[] _trace;
    private readonly int[] _firstContext, _nextContext;
    private readonly ObservationOrigin? _origin;
    private readonly Dictionary<(int Row, long Count), InstructionObservation> _materialized = new();
    internal IReadOnlyDictionary<string, long> Counts { get; }
    internal long EstimatedBytes { get; }
    internal FrozenObservationTable(CompactObservationRow[] rows, ObservationOperandValue[] operands, InstructionOperand[] booleanOperands, RichObservationPayload[] rich,
        IReadOnlyDictionary<string, int> global, Dictionary<string, int> contexts, int[] trace, IReadOnlyDictionary<string, long> counts)
    {
        _rows = rows; _operands = operands; _booleanOperands = booleanOperands; _rich = rich; _global = global; _contexts = contexts; _trace = trace; Counts = counts;
        _firstContext = new int[rows.Length]; _nextContext = new int[rows.Length];
        Array.Fill(_firstContext, -1); Array.Fill(_nextContext, -1);
        foreach (var index in contexts.Values)
        {
            var globalIndex = global[rows[index].Id];
            _nextContext[index] = _firstContext[globalIndex]; _firstContext[globalIndex] = index;
        }
        // Reverse each linked group to preserve original context insertion order.
        foreach (var globalIndex in global.Values)
        {
            var previous = -1; var index = _firstContext[globalIndex];
            while (index >= 0) { var next = _nextContext[index]; _nextContext[index] = previous; previous = index; index = next; }
            _firstContext[globalIndex] = previous;
        }
        // Charge private backing even when no instruction ran. Per-index charges below
        // remain conservative allowances for lazy public records and referenced data.
        // Registry identity is shared program storage; dense values/order belong to this scan.
        long estimate = 512L
            + rows.LongLength * System.Runtime.CompilerServices.Unsafe.SizeOf<CompactObservationRow>()
            + operands.LongLength * System.Runtime.CompilerServices.Unsafe.SizeOf<ObservationOperandValue>()
            + rich.LongLength * System.Runtime.CompilerServices.Unsafe.SizeOf<RichObservationPayload>()
            + booleanOperands.LongLength * IntPtr.Size
            + 4L * (trace.LongLength + _firstContext.LongLength + _nextContext.LongLength);
        if (global is FrozenRegisteredMap<int> dense)
            estimate += 12L * dense.RegisteredCapacity + 4L * dense.Count;
        foreach (var p in global) estimate += rows[p.Value].Estimate(operands, booleanOperands, rich) + 64L + p.Key.Length * 2L;
        foreach (var p in contexts) estimate += rows[p.Value].Estimate(operands, booleanOperands, rich) + 64L + p.Key.Length * 2L;
        foreach (var index in trace) estimate += rows[index].Estimate(operands, booleanOperands, rich);
        foreach (var key in counts.Keys) estimate += 64L + key.Length * 2L;
        EstimatedBytes = estimate;
    }
    private FrozenObservationTable(FrozenObservationTable source, ObservationOrigin origin)
    {
        _rows = source._rows; _operands = source._operands; _booleanOperands = source._booleanOperands; _rich = source._rich; _global = source._global; _contexts = source._contexts;
        _trace = source._trace; _firstContext = source._firstContext; _nextContext = source._nextContext;
        Counts = source.Counts; EstimatedBytes = source.EstimatedBytes; _origin = origin;
    }
    internal InstructionObservationCapture Capture(long dropped, bool partial, bool capacity, bool saturated)
        => new(new DictionaryView(this, _global), Counts, new DictionaryView(this, _contexts), new TraceView(this), dropped, partial, capacity, saturated);
    internal InstructionObservationCapture WithOrigin(InstructionObservationCapture capture, ObservationOrigin origin)
        => new FrozenObservationTable(this, origin).Capture(capture.TraceDropped, capture.IsPartial, capture.CapacityExceeded, capture.CountSaturated);
    internal static FrozenObservationTable? From(InstructionObservationCapture capture)
        => capture.GlobalLast is DictionaryView g && capture.Contexts is DictionaryView c && capture.Trace is TraceView t
            && ReferenceEquals(g.Owner, c.Owner) && ReferenceEquals(g.Owner, t.Owner) && ReferenceEquals(g.Owner.Counts, capture.Counts) ? g.Owner : null;
    internal IEnumerable<InstructionObservationSummary> GlobalSummaries
    {
        get { foreach (var index in _global.Values) { var row = _rows[index]; yield return new(row.Id, row.Catalog, row.ExecutionOrdinal); } }
    }
    internal IEnumerable<InstructionObservation> ContextsForInstruction(string id)
    {
        if (!_global.TryGetValue(id, out var global)) yield break;
        for (var index = _firstContext[global]; index >= 0; index = _nextContext[index]) yield return Get(index, true);
    }
    internal ObservationLookup FindContext(string id, string task, long frame)
    {
        if (_global.TryGetValue(id, out var global))
            for (var index = _firstContext[global]; index >= 0; index = _nextContext[index])
            { var row = _rows[index]; if (row.Task == task && row.FrameInvocationId == frame) return new(Get(index, true), ObservationUnavailableReason.None); }
        return new(null, ObservationUnavailableReason.ContextNotCaptured);
    }
    internal ObservationLookup FindPath(string id, string task, IReadOnlyList<string> path)
    {
        var best = -1;
        if (_global.TryGetValue(id, out var global))
            for (var index = _firstContext[global]; index >= 0; index = _nextContext[index])
            {
                var row = _rows[index]; if (row.Task != task || row.CallPath.Count != path.Count) continue;
                var matches = true;
                for (var i = 0; i < path.Count; i++) if (row.CallPath[i].CallSiteId != path[i]) { matches = false; break; }
                if (matches && (best < 0 || row.ExecutionOrdinal > _rows[best].ExecutionOrdinal)) best = index;
            }
        return best < 0 ? new(null, ObservationUnavailableReason.ContextNotCaptured) : new(Get(best, true), ObservationUnavailableReason.None);
    }
    private InstructionObservation Get(int index, bool final)
    {
        var row = _rows[index]; var count = final ? Counts[row.Id] : row.Count;
        lock (_materialized)
        {
            var key = (index, count);
            if (!_materialized.TryGetValue(key, out var value)) _materialized.Add(key, value = row.Materialize(_operands, _booleanOperands, _rich, count, _origin));
            return value;
        }
    }
    private sealed class DictionaryView(FrozenObservationTable owner, IReadOnlyDictionary<string, int> indices) : IReadOnlyDictionary<string, InstructionObservation>
    {
        internal FrozenObservationTable Owner => owner;
        public int Count => indices.Count;
        public IEnumerable<string> Keys => indices.Keys;
        public IEnumerable<InstructionObservation> Values { get { foreach (var index in indices.Values) yield return owner.Get(index, true); } }
        public InstructionObservation this[string key] => owner.Get(indices[key], true);
        public bool ContainsKey(string key) => indices.ContainsKey(key);
        public bool TryGetValue(string key, out InstructionObservation value)
        { if (indices.TryGetValue(key, out var index)) { value = owner.Get(index, true); return true; } value = null!; return false; }
        public IEnumerator<KeyValuePair<string, InstructionObservation>> GetEnumerator()
        { foreach (var pair in indices) yield return new(pair.Key, owner.Get(pair.Value, true)); }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class TraceView(FrozenObservationTable owner) : IReadOnlyList<InstructionObservation>
    {
        internal FrozenObservationTable Owner => owner;
        public int Count => owner._trace.Length;
        public InstructionObservation this[int index] => owner.Get(owner._trace[index], false);
        public IEnumerator<InstructionObservation> GetEnumerator() { for (var i = 0; i < Count; i++) yield return this[i]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
