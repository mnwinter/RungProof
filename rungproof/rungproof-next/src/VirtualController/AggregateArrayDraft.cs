using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

/// <summary>Array form state keyed by PLC indices rather than row positions; malformed text stays editable.</summary>
public sealed class AggregateArrayDraft
{
    private readonly Dictionary<int, string> _retained = new();
    public PlcVariableType Type { get; private set; }
    public int LowerBound { get; private set; }
    public string[] Values { get; private set; }
    public AggregateArrayDraft(PlcVariableType type, int lowerBound, string[] values)
    {
        Type = type; LowerBound = lowerBound; Values = values.ToArray();
        ValidateRange(lowerBound, values.Length);
    }
    private static void ValidateRange(int lower, int length)
    {
        if (length < 1 || length > 4096 || (long)lower + length - 1 > int.MaxValue)
            throw new ArgumentException("Array range must contain 1–4096 valid INT32 indices.");
    }
    public void Resize(int lowerBound, int length)
    {
        ValidateRange(lowerBound, length);
        for (var i = 0; i < Values.Length; i++) _retained[LowerBound + i] = Values[i];
        Values = Enumerable.Range(0, length).Select(i => _retained.GetValueOrDefault(lowerBound + i, Default(Type))).ToArray();
        LowerBound = lowerBound;
    }
    public bool TryChangeType(PlcVariableType type, bool applyTypeChange)
    {
        if (type == Type) return true;
        if (!applyTypeChange) return false;
        // Type selection is a pending form edit. Preserve literal text, including invalid
        // text and temporarily hidden indices; Apply validates it against the new type.
        Type = type; return true;
    }
    private static string Default(PlcVariableType type) => type == PlcVariableType.Bool ? "FALSE" : "0";
}
