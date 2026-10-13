using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Bounded observer storage. Eviction never changes the authoritative per-ID last value/count.</summary>
public sealed class InstructionObservationCollector
{
    public const int RegisteredIdCapacity = 20000;
    private readonly HashSet<string> _registered;
    private readonly ObservationIdRegistry _registry;
    private readonly Dictionary<string, int> _last = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _contexts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _counts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _contextCounts = new(StringComparer.Ordinal);
    private readonly Queue<int> _trace = new();
    private readonly Dictionary<(string Id, string Task, string Path), string> _stableContextKeys = new();
    private readonly Dictionary<IReadOnlyList<ObservationCallSite>, string> _pathSignatures = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, string> _internedPaths = new(StringComparer.Ordinal);
    private readonly int _contextCapacity, _traceCapacity;
    private readonly List<CompactObservationRow> _rows = new();
    private readonly List<ObservationOperandValue[]> _operandSlots = new();
    private readonly List<int> _references = new();
    private readonly List<RichObservationPayload> _richSlots = new();
    private readonly Queue<int> _freeSlots = new();
    private readonly Dictionary<int, int> _freezeRemap = new();
    private int _nextUnusedSlot;
    private long _traceDropped;
    private bool _saturated;
    public bool CapacityExceeded { get; }
    public InstructionObservationCollector(IEnumerable<string> ids, int contextCapacity = 40000, int traceCapacity = 10000)
    {
        if (contextCapacity < 0 || traceCapacity < 0) throw new ArgumentOutOfRangeException(nameof(contextCapacity));
        var all = ids.Distinct(StringComparer.Ordinal).ToArray();
        CapacityExceeded = all.Length > RegisteredIdCapacity;
        _registered = all.Take(RegisteredIdCapacity).ToHashSet(StringComparer.Ordinal);
        _registry = new ObservationIdRegistry(all.Take(RegisteredIdCapacity).ToArray());
        _contextCapacity = contextCapacity; _traceCapacity = traceCapacity;
    }
    internal static string ContextKey(string id, string task, long frame) => $"{id.Length}:{id}{task.Length}:{task}:{frame}";
    internal void Reset()
    {
        // Freeze owns independent dictionaries/arrays; clearing the mutable working
        // indexes cannot alter retained scans. Reuse their allocated buckets.
        _last.Clear(); _contexts.Clear(); _counts.Clear(); _contextCounts.Clear(); _trace.Clear();
        for (var i = 0; i < _nextUnusedSlot; i++)
        {
            _rows[i] = default; _richSlots[i] = default; _references[i] = 0;
            Array.Clear(_operandSlots[i]);
        }
        _freeSlots.Clear(); _freezeRemap.Clear(); _pathSignatures.Clear(); _nextUnusedSlot = 0;
        _traceDropped = 0; _saturated = false;
    }
    private string GetPathKey(string id, string task, IReadOnlyList<ObservationCallSite> path)
    {
        var signature = "";
        if (path.Count != 0 && !_pathSignatures.TryGetValue(path, out signature))
        {
            var candidate = string.Concat(path.Select(p => $"{p.CallSiteId.Length}:{p.CallSiteId}{p.TargetBlockId.Length}:{p.TargetBlockId}{p.InstanceId?.Length ?? 0}:{p.InstanceId}"));
            if (!_internedPaths.TryGetValue(candidate, out signature))
            {
                signature = candidate;
                if (_internedPaths.Count < _contextCapacity) _internedPaths.Add(candidate, candidate);
            }
            if (_pathSignatures.Count < _contextCapacity) _pathSignatures.Add(path, signature);
        }
        var identity = (id, task, signature);
        if (_stableContextKeys.TryGetValue(identity, out var cached)) return cached;
        var key = $"{id.Length}:{id}{task.Length}:{task}" + signature;
        if (_stableContextKeys.Count < _contextCapacity) _stableContextKeys.Add(identity, key);
        return key;
    }
    internal readonly record struct PreparedObservation(string? Key, long Count, long ContextCount, bool RetainContext);
    // Internal runtime path prepares counts before constructing its immutable event.
    // Prepare and RecordPrepared are adjacent and synchronous; there is no callback.
    internal PreparedObservation Prepare(string id, string task, IReadOnlyList<ObservationCallSite> path, bool truncated)
    {
        if (!_registered.Contains(id)) return default;
        var count = _counts.GetValueOrDefault(id);
        if (count == long.MaxValue) _saturated = true; else count++;
        _counts[id] = count;
        var key = GetPathKey(id, task, path);
        var retained = !truncated && (_contexts.ContainsKey(key) || _contexts.Count < _contextCapacity);
        var contextCount = retained ? _contextCounts.GetValueOrDefault(key) : 0;
        if (retained)
        {
            if (contextCount == long.MaxValue) _saturated = true; else contextCount++;
            _contextCounts[key] = contextCount;
        }
        return new(key, count, contextCount, retained);
    }
    public void Record(InstructionObservation observation)
    {
        var prepared = Prepare(observation.InstructionId, observation.TaskId, observation.CallPath, observation.PathTruncated);
        if (prepared.Key is null) return;
        RecordPrepared(observation with { ObservationCountInScan = prepared.Count, ContextCountInScan = prepared.ContextCount }, prepared);
    }
    internal void RecordPrepared(InstructionObservation observation, PreparedObservation prepared)
        => Store(new CompactObservationRow { Definition = observation, Count = prepared.Count, ContextCount = prepared.ContextCount }, null, prepared);
    internal void RecordBoolean(BooleanObservationMetadata metadata, ObservationCallPath path, long ordinal, long frame,
        uint flags, InstructionWriteDisposition disposition, IReadOnlyList<ObservationOperandValue> operands, PreparedObservation prepared)
        => Store(new CompactObservationRow { Definition = metadata, Path = path, Ordinal = ordinal, Frame = frame,
            Count = prepared.Count, ContextCount = prepared.ContextCount, Flags = flags | 0x40000000u, Disposition = disposition, OperandCount = operands.Count }, operands, prepared);
    internal void RecordRich(BooleanObservationMetadata metadata, ObservationCallPath path, long ordinal, long frame,
        uint flags, InstructionWriteDisposition disposition, IReadOnlyList<ObservationOperandValue> operands,
        RichObservationPayload payload, PreparedObservation prepared)
        => Store(new CompactObservationRow { Definition = metadata, Path = path, Ordinal = ordinal, Frame = frame,
            Count = prepared.Count, ContextCount = prepared.ContextCount, Flags = flags | 0x80000000u | (AreBooleanOperands(operands) ? 0x40000000u : 0), Disposition = disposition, OperandCount = operands.Count }, operands, prepared, payload);
    private static bool AreBooleanOperands(IReadOnlyList<ObservationOperandValue> operands)
    {
        for (var i = 0; i < operands.Count; i++) if (operands[i].Value is not null and not bool) return false;
        return true;
    }
    private void Store(CompactObservationRow row, IReadOnlyList<ObservationOperandValue>? operands, PreparedObservation prepared, RichObservationPayload payload = default)
    {
        if (prepared.Key is null) return;
        int slot;
        if (_freeSlots.Count > 0) slot = _freeSlots.Dequeue();
        else
        {
            slot = _nextUnusedSlot++;
            if (slot == _rows.Count) { _rows.Add(default); _richSlots.Add(default); _references.Add(0); _operandSlots.Add(Array.Empty<ObservationOperandValue>()); }
        }
        if (operands is not null)
        {
            if (_operandSlots[slot].Length < operands.Count) _operandSlots[slot] = new ObservationOperandValue[operands.Count];
            for (var i = 0; i < operands.Count; i++) _operandSlots[slot][i] = operands[i];
        }
        _rows[slot] = row; _richSlots[slot] = payload;
        Replace(_last, row.Id, slot);
        if (prepared.RetainContext) Replace(_contexts, prepared.Key, slot);
        if (_traceCapacity == 0) { _traceDropped++; return; }
        if (_trace.Count == _traceCapacity) { Release(_trace.Dequeue()); _traceDropped++; }
        _trace.Enqueue(slot); _references[slot]++;
    }
    private void Replace(Dictionary<string, int> index, string key, int slot)
    {
        if (index.TryGetValue(key, out var old)) Release(old);
        index[key] = slot; _references[slot]++;
    }
    private void Release(int slot)
    {
        if (--_references[slot] != 0) return;
        _rows[slot] = default; _richSlots[slot] = default; Array.Clear(_operandSlots[slot]); _freeSlots.Enqueue(slot);
    }
    public InstructionObservationCapture Freeze(bool partial)
    {
        _freezeRemap.Clear();
        var operandCount = 0; var booleanOperandCount = 0; var richCount = 0;
        void Retain(int slot)
        {
            if (_freezeRemap.ContainsKey(slot)) return;
            _freezeRemap.Add(slot, _freezeRemap.Count);
            if (_rows[slot].HasBooleanOperands) booleanOperandCount += _rows[slot].OperandCount; else operandCount += _rows[slot].OperandCount;
            if (_rows[slot].HasRichPayload) richCount++;
        }
        foreach (var slot in _last.Values) Retain(slot);
        foreach (var slot in _contexts.Values) Retain(slot);
        foreach (var slot in _trace) Retain(slot);
        var rows = new CompactObservationRow[_freezeRemap.Count];
        var operands = new ObservationOperandValue[operandCount];
        var booleanOperands = new InstructionOperand[booleanOperandCount];
        var rich = new RichObservationPayload[richCount];
        var cursor = 0; var booleanCursor = 0; var richCursor = 0;
        foreach (var pair in _freezeRemap)
        {
            var row = _rows[pair.Key]; row.OperandOffset = row.HasBooleanOperands ? booleanCursor : cursor;
            if (row.HasRichPayload) { row.RichIndex = richCursor; rich[richCursor++] = _richSlots[pair.Key]; }
            if (row.HasBooleanOperands)
            {
                for (var i = 0; i < row.OperandCount; i++) { var value = _operandSlots[pair.Key][i]; booleanOperands[booleanCursor++] = value.Descriptor.BooleanValue(value.Value); }
            }
            else { Array.Copy(_operandSlots[pair.Key], 0, operands, cursor, row.OperandCount); cursor += row.OperandCount; }
            rows[pair.Value] = row;
        }
        Dictionary<string, int> Copy(Dictionary<string, int> source)
        {
            var result = new Dictionary<string, int>(source.Count, StringComparer.Ordinal);
            foreach (var pair in source) result.Add(pair.Key, _freezeRemap[pair.Value]);
            return result;
        }
        var trace = new int[_trace.Count]; var t = 0;
        foreach (var slot in _trace) trace[t++] = _freezeRemap[slot];
        var globalRows = new int[_registry.Count]; Array.Fill(globalRows, -1);
        var countValues = new long[_registry.Count]; var order = new int[_last.Count]; var position = 0;
        foreach (var pair in _last)
        {
            var id = _registry.Index(pair.Key); order[position++] = id;
            globalRows[id] = _freezeRemap[pair.Value]; countValues[id] = _counts[pair.Key];
        }
        var globals = new FrozenRegisteredMap<int>(_registry, globalRows, globalRows, order);
        var counts = new FrozenRegisteredMap<long>(_registry, countValues, globalRows, order);
        return new FrozenObservationTable(rows, operands, booleanOperands, rich, globals, Copy(_contexts), trace, counts)
            .Capture(_traceDropped, partial, CapacityExceeded, _saturated);
    }
}
