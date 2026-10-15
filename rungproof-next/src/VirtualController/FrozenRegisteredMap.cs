using System;
using System.Collections;
using System.Collections.Generic;

namespace RungProof.Next.VirtualController;

// Program-owned index never changes. Frozen scans share symbol identity, not live values.
internal sealed class ObservationIdRegistry
{
    private readonly string[] _ids;
    private readonly Dictionary<string, int> _indices;
    internal ObservationIdRegistry(string[] ids)
    {
        _ids = (string[])ids.Clone(); _indices = new(ids.Length, StringComparer.Ordinal);
        for (var i = 0; i < ids.Length; i++) _indices.Add(ids[i], i);
    }
    internal int Count => _ids.Length;
    internal string Id(int index) => _ids[index];
    internal int Index(string id) => _indices[id];
    internal bool TryIndex(string id, out int index) => _indices.TryGetValue(id, out index);
}
internal sealed class FrozenRegisteredMap<T>(ObservationIdRegistry registry, T[] values, int[] presentRows, int[] order) : IReadOnlyDictionary<string, T>
{
    // Both frozen maps share presence and order arrays; the table charges these once.
    internal int RegisteredCapacity => registry.Count;
    public int Count => order.Length;
    public IEnumerable<string> Keys { get { foreach (var index in order) yield return registry.Id(index); } }
    public IEnumerable<T> Values { get { foreach (var index in order) yield return values[index]; } }
    public T this[string key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException("Observation ID was not captured: " + key);
    public bool ContainsKey(string key) => registry.TryIndex(key, out var index) && presentRows[index] >= 0;
    public bool TryGetValue(string key, out T value)
    {
        if (registry.TryIndex(key, out var index) && presentRows[index] >= 0) { value = values[index]; return true; }
        value = default!; return false;
    }
    public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
    { foreach (var index in order) yield return new(registry.Id(index), values[index]); }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
