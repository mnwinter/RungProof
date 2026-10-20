using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum WatchReferenceKind
{
    Symbolic,
    AbsoluteAddress,
}

/// <summary>
/// Authored watch-table row data. Runtime monitor quality and force state live
/// in controller snapshots and are deliberately not part of this row.
/// </summary>
public sealed class EditableWatchRow
{
    public string Id { get; }
    public WatchReferenceKind ReferenceKind { get; set; }
    public string Reference { get; set; }
    public string DisplayFormat { get; set; }
    public string? ModifyValue { get; set; }
    public string Comment { get; set; }

    public EditableWatchRow(
        string reference,
        WatchReferenceKind referenceKind = WatchReferenceKind.Symbolic,
        string displayFormat = "Auto",
        string? modifyValue = null,
        string comment = "",
        string? id = null)
    {
        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
        ReferenceKind = referenceKind;
        Reference = reference ?? string.Empty;
        DisplayFormat = string.IsNullOrWhiteSpace(displayFormat) ? "Auto" : displayFormat;
        ModifyValue = modifyValue;
        Comment = comment ?? string.Empty;
    }

    internal EditableWatchRow Clone() => new(Reference, ReferenceKind, DisplayFormat, ModifyValue, Comment, Id);

    internal EditableWatchRow Duplicate() => new(Reference, ReferenceKind, DisplayFormat, ModifyValue, Comment);
}

/// <summary>
/// Live symbolic-only compatibility view over an editable watch table's rows.
/// Existing callers can continue to use list-style string operations without
/// flattening or deleting absolute-address rows.
/// </summary>
public sealed class EditableWatchVariableList : IList<string>, IReadOnlyList<string>
{
    private readonly EditableWatchTable _table;

    internal EditableWatchVariableList(EditableWatchTable table) => _table = table;

    private EditableWatchRow[] SymbolicRows => _table.Rows
        .Where(row => row.ReferenceKind == WatchReferenceKind.Symbolic).ToArray();

    public string this[int index]
    {
        get => SymbolicRows[index].Reference;
        set => SymbolicRows[index].Reference = value ?? string.Empty;
    }

    public int Count => _table.Rows.Count(row => row.ReferenceKind == WatchReferenceKind.Symbolic);
    public bool IsReadOnly => false;

    public void Add(string item) => _table.Rows.Add(new EditableWatchRow(item));

    public void AddRange(IEnumerable<string> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var item in items) Add(item);
    }

    public void Clear() => _table.Rows.RemoveAll(row => row.ReferenceKind == WatchReferenceKind.Symbolic);

    public bool Contains(string item) => IndexOf(item) >= 0;

    public void CopyTo(string[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        foreach (var item in this) array[arrayIndex++] = item;
    }

    public IEnumerator<string> GetEnumerator() => SymbolicRows.Select(row => row.Reference).GetEnumerator();

    public int IndexOf(string item) => Array.FindIndex(SymbolicRows,
        row => string.Equals(row.Reference, item, StringComparison.Ordinal));

    public void Insert(int index, string item)
    {
        var symbolic = SymbolicRows;
        if (index < 0 || index > symbolic.Length) throw new ArgumentOutOfRangeException(nameof(index));
        var row = new EditableWatchRow(item);
        if (index < symbolic.Length)
            _table.Rows.Insert(_table.Rows.IndexOf(symbolic[index]), row);
        else
            _table.Rows.Add(row);
    }

    public bool Remove(string item)
    {
        var row = SymbolicRows.FirstOrDefault(candidate =>
            string.Equals(candidate.Reference, item, StringComparison.Ordinal));
        return row is not null && _table.Rows.Remove(row);
    }

    public int RemoveAll(Predicate<string> match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return _table.Rows.RemoveAll(row => row.ReferenceKind == WatchReferenceKind.Symbolic
            && match(row.Reference));
    }

    public void RemoveAt(int index) => _table.Rows.Remove(SymbolicRows[index]);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
