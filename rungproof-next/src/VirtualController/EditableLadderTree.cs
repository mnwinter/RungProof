using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RungProof.Next.VirtualController;

/// <summary>Immutable authoring expression. Mutations publish a new root only after validation.</summary>
public sealed record EditableLadderNode(
    string Id, LadderNodeKind Kind, string Variable = "", bool NormallyClosed = false,
    IReadOnlyList<EditableLadderNode>? Children = null,
    LadderCompareOperator CompareOperator = LadderCompareOperator.Equal,
    string RightOperand = "", LadderEdgeMode EdgeMode = LadderEdgeMode.None,
    LadderComparisonValueType ComparisonValueType = LadderComparisonValueType.Unspecified)
{
    public string? EdgeMemoryOperand { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool OpenEnd { get; init; }
    public bool IsLeaf => Kind is LadderNodeKind.Contact or LadderNodeKind.Compare;
}

public static class EditableLadderTree
{
    public const int MaximumDepth = 32;

    public static EditableLadderNode Import(LadderNode source, string path = "logic")
    {
        ArgumentNullException.ThrowIfNull(source);
        EditableLadderNode Copy(LadderNode n, int depth, string at)
        {
            if (n is null) throw new ArgumentException($"{at}: expression node cannot be null.");
            if (depth > MaximumDepth) throw new ArgumentException($"{at}: expression depth {depth} exceeds {MaximumDepth}.");
            if (!Enum.IsDefined(n.Kind)) throw new ArgumentException($"{at}: unsupported expression kind '{n.Kind}'.");
            if (n.Kind is LadderNodeKind.Contact or LadderNodeKind.Compare && n.Children is { Count: > 0 })
                throw new ArgumentException($"{at}: a leaf cannot contain children.");
            return new EditableLadderNode(n.Id, n.Kind, n.Variable, n.NormallyClosed,
                n.Children is null ? null : Array.AsReadOnly(n.Children.Select((child, index) => Copy(child, depth + 1, $"{at}.children[{index}]")).ToArray()),
                n.CompareOperator, n.RightOperand, n.EdgeMode, n.ComparisonValueType)
            { EdgeMemoryOperand = n.EdgeMemoryOperand, OpenEnd = n.OpenEnd };
        }
        var result = Copy(source, 1, path);
        Validate(result, path);
        return result;
    }

    public static LadderNode Export(EditableLadderNode root) => new(root.Id, root.Kind, root.Variable,
        root.NormallyClosed, root.Children is null ? null : Array.AsReadOnly(root.Children.Select(Export).ToArray()),
        root.CompareOperator, root.RightOperand, root.EdgeMode, root.ComparisonValueType)
    { EdgeMemoryOperand = root.EdgeMemoryOperand, OpenEnd = root.OpenEnd };

    public static void Validate(EditableLadderNode root, string path = "logic", ISet<string>? occupiedIds = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        var seen = occupiedIds ?? new HashSet<string>(StringComparer.Ordinal);
        void Walk(EditableLadderNode n, int depth, string at,
            LadderNodeKind? parentKind = null, int childIndex = -1)
        {
            if (n is null) throw new ArgumentException($"{at}: expression node cannot be null.");
            if (depth > MaximumDepth) throw new ArgumentException($"{at}: expression depth {depth} exceeds {MaximumDepth}.");
            if (string.IsNullOrWhiteSpace(n.Id)) throw new ArgumentException($"{at}.id: instruction ID is required.");
            if (!seen.Add(n.Id)) throw new ArgumentException($"{at}.id: duplicate instruction ID '{n.Id}'.");
            if (!Enum.IsDefined(n.Kind)) throw new ArgumentException($"{at}.kind: unsupported expression kind '{n.Kind}'.");
            if (!Enum.IsDefined(n.CompareOperator)) throw new ArgumentException($"{at}.compareOperator: unsupported comparison '{n.CompareOperator}'.");
            if (!Enum.IsDefined(n.EdgeMode)) throw new ArgumentException($"{at}.edgeMode: unsupported edge mode '{n.EdgeMode}'.");
            if (!Enum.IsDefined(n.ComparisonValueType)) throw new ArgumentException($"{at}.comparisonValueType: unsupported type '{n.ComparisonValueType}'.");
            if (n.IsLeaf && n.Children is { Count: > 0 }) throw new ArgumentException($"{at}.children: leaf cannot contain children.");
            if (n.OpenEnd && (n.Kind != LadderNodeKind.Series
                || parentKind != LadderNodeKind.Parallel || childIndex <= 0))
                throw new ArgumentException($"{at}.openEnd: only a lower Series path in a Parallel branch may remain open.");
            for (var i = 0; i < (n.Children?.Count ?? 0); i++)
                Walk(n.Children![i], depth + 1, $"{at}.children[{i}]", n.Kind, i);
        }
        Walk(root, 1, path);
    }

    /// <summary>Runtime order: series left to right, parallel top to bottom; never distribute paths.</summary>
    public static IEnumerable<EditableLadderNode> Visit(EditableLadderNode root)
    {
        yield return root;
        foreach (var child in root.Children ?? []) foreach (var descendant in Visit(child)) yield return descendant;
    }

    public static EditableLadderNode? Find(EditableLadderNode root, string id) => Visit(root).FirstOrDefault(n => n.Id == id);

    public static EditableLadderNode Replace(EditableLadderNode root, string id, EditableLadderNode replacement)
    {
        var found = false;
        EditableLadderNode Walk(EditableLadderNode n)
        {
            if (n.Id == id) { found = true; return replacement; }
            return n.Children is null ? n : n with { Children = Array.AsReadOnly(n.Children.Select(Walk).ToArray()) };
        }
        var result = Walk(root);
        if (!found) throw new ArgumentException($"logic: node '{id}' does not exist.");
        Validate(result);
        return result;
    }

    public static EditableLadderNode Insert(EditableLadderNode root, string targetId,
        EditableLadderNode inserted, bool after, Func<string> newId)
    {
        var target = Find(root, targetId) ?? throw new ArgumentException($"logic: node '{targetId}' does not exist.");
        var parent = Visit(root).FirstOrDefault(n => n.Children?.Any(c => c.Id == targetId) == true);
        if (parent?.Kind == LadderNodeKind.Series)
        {
            var children = parent.Children!.ToList();
            children.Insert(children.FindIndex(n => n.Id == targetId) + (after ? 1 : 0), inserted);
            return Replace(root, parent.Id, parent with { Children = Array.AsReadOnly(children.ToArray()) });
        }
        return Replace(root, targetId, new(newId(), LadderNodeKind.Series,
            Children: Array.AsReadOnly(after ? new[] { target, inserted } : new[] { inserted, target })));
    }

    public static EditableLadderNode InsertChild(EditableLadderNode root, string parentId, int slot, EditableLadderNode inserted)
    {
        var parent = Find(root, parentId) ?? throw new ArgumentException($"logic: node '{parentId}' does not exist.");
        if (parent.IsLeaf) throw new ArgumentException($"logic.{parentId}: insertion requires a Series or Parallel parent.");
        var children = (parent.Children ?? []).ToList();
        if (slot < 0 || slot > children.Count) throw new ArgumentOutOfRangeException(nameof(slot));
        children.Insert(slot, inserted);
        return Replace(root, parentId, parent with { Children = Array.AsReadOnly(children.ToArray()) });
    }

    public static EditableLadderNode WrapParallel(EditableLadderNode root, string targetId, Func<string> newId)
    {
        var target = Find(root, targetId) ?? throw new ArgumentException($"logic: node '{targetId}' does not exist.");
        // Keep the selected expression once. Each new branch is an explicit Series wire.
        var first = target.Kind == LadderNodeKind.Series ? target
            : new EditableLadderNode(newId(), LadderNodeKind.Series, Children: Array.AsReadOnly(new[] { target }));
        return Replace(root, targetId, new(newId(), LadderNodeKind.Parallel,
            Children: Array.AsReadOnly(new[] { first, new EditableLadderNode(newId(), LadderNodeKind.Series, Children: Array.Empty<EditableLadderNode>()) })));
    }

    /// <summary>
    /// Opens a simultaneous branch at the selected wire slot. For a Series slot,
    /// the prefix remains in series and the existing suffix becomes the original
    /// (upper) path; the new path is empty and ready for LAD elements. A slot on a
    /// Parallel node inserts another complete path at that split.
    /// </summary>
    public static (EditableLadderNode Root, string NewPathId) OpenParallelAtSlot(
        EditableLadderNode root, string parentId, int slot, Func<string> newId)
    {
        var parent = Find(root, parentId)
            ?? throw new ArgumentException($"logic: branch slot parent '{parentId}' does not exist.");
        if (parent.IsLeaf) throw new ArgumentException("logic: a branch can only open at a wire slot.");
        var children = (parent.Children ?? []).ToList();
        if (slot < 0 || slot > children.Count) throw new ArgumentOutOfRangeException(nameof(slot));

        EditableLadderNode emptyPath() => new EditableLadderNode(newId(), LadderNodeKind.Series,
            Children: Array.Empty<EditableLadderNode>()) { OpenEnd = true };
        var newPath = emptyPath();
        EditableLadderNode result;
        if (parent.Kind == LadderNodeKind.Parallel)
        {
            if (slot == 0)
                throw new ArgumentException("logic: a simultaneous branch opens downward and cannot be inserted above the main path.");
            // A slot between paths adds another leg to this existing split/join.
            children.Insert(slot, newPath);
            result = Replace(root, parent.Id, parent with { Children = Array.AsReadOnly(children.ToArray()) });
        }
        else
        {
            // The selected slot is a horizontal wire in a Series. Branch after
            // the preceding instruction, preserving the original downstream path.
            var prefix = children.Take(slot).ToList();
            var suffix = children.Skip(slot).ToArray();
            var originalPath = new EditableLadderNode(newId(), LadderNodeKind.Series,
                Children: Array.AsReadOnly(suffix));
            var parallel = new EditableLadderNode(newId(), LadderNodeKind.Parallel,
                Children: Array.AsReadOnly(new[] { originalPath, newPath }));
            prefix.Add(parallel);
            result = Replace(root, parent.Id, parent with { Children = Array.AsReadOnly(prefix.ToArray()) });
        }
        return (result, newPath.Id);
    }

    /// <summary>
    /// Moves the join of an open simultaneous-branch path to an exact downstream
    /// slot on its main (upper) path. Instructions after the selected slot remain
    /// in series after the join. For a multi-path split, an earlier close becomes
    /// an explicit nested parallel in the main path so every other leg retains
    /// its original split/join and identity.
    /// </summary>
    public static EditableLadderNode CloseParallelAtSlot(
        EditableLadderNode root, string branchPathId, string mainPathId, int slot,
        Func<string> newId)
    {
        var branchPath = Find(root, branchPathId)
            ?? throw new ArgumentException($"logic: branch path '{branchPathId}' does not exist.");
        var mainPath = Find(root, mainPathId)
            ?? throw new ArgumentException($"logic: main path '{mainPathId}' does not exist.");
        if (branchPath.Kind != LadderNodeKind.Series || mainPath.Kind != LadderNodeKind.Series)
            throw new ArgumentException("logic: branch closure requires Series paths.");
        if (!branchPath.OpenEnd)
            throw new ArgumentException("logic: selected branch path is already closed.");

        var parallel = Visit(root).FirstOrDefault(node => node.Kind == LadderNodeKind.Parallel
            && node.Children?.Any(child => child.Id == branchPathId) == true)
            ?? throw new ArgumentException("logic: selected path is not a simultaneous branch.");
        var parallelChildren = parallel.Children?.ToList() ?? [];
        var branchIndex = parallelChildren.FindIndex(child => child.Id == branchPathId);
        if (parallelChildren.Count < 2 || parallelChildren[0].Id != mainPathId || branchIndex <= 0)
            throw new ArgumentException("logic: branch closure requires a lower path and the upper main path from the same parallel split.");

        var mainChildren = (mainPath.Children ?? []).ToArray();
        if (slot <= 0)
            throw new ArgumentException("logic: the main branch must contain a LAD element before the branch can close.");
        if (slot > mainChildren.Length)
            throw new ArgumentException("logic: select a downstream connection before the current branch join.");

        var closedBranch = branchPath with { OpenEnd = false };
        if (slot == mainChildren.Length)
            return Replace(root, branchPath.Id, closedBranch);

        if (parallelChildren.Count > 2)
        {
            var closedMainPrefix = new EditableLadderNode(newId(), LadderNodeKind.Series,
                Children: Array.AsReadOnly(mainChildren.Take(slot).ToArray()));
            var nestedJoin = new EditableLadderNode(newId(), LadderNodeKind.Parallel,
                Children: Array.AsReadOnly(new[] { closedMainPrefix, closedBranch }));
            var restructuredMain = mainPath with
            {
                Children = Array.AsReadOnly(new[] { nestedJoin }
                    .Concat(mainChildren.Skip(slot)).ToArray())
            };
            parallelChildren[0] = restructuredMain;
            parallelChildren.RemoveAt(branchIndex);
            return Replace(root, parallel.Id,
                parallel with { Children = Array.AsReadOnly(parallelChildren.ToArray()) });
        }

        var containingSeries = Visit(root).FirstOrDefault(node => node.Kind == LadderNodeKind.Series
            && node.Children?.Any(child => child.Id == parallel.Id) == true);
        var closedMain = mainPath with { Children = Array.AsReadOnly(mainChildren.Take(slot).ToArray()) };
        var closedParallel = parallel with
        {
            Children = Array.AsReadOnly(new[] { closedMain, closedBranch })
        };
        if (containingSeries is null)
        {
            if (root.Id != parallel.Id)
                throw new ArgumentException("logic: the branch join is not contained by a Series path.");
            return new EditableLadderNode(newId(), LadderNodeKind.Series,
                Children: Array.AsReadOnly(new[] { closedParallel }
                    .Concat(mainChildren.Skip(slot)).ToArray()));
        }
        var containingChildren = containingSeries.Children!.ToList();
        var parallelIndex = containingChildren.FindIndex(child => child.Id == parallel.Id);
        containingChildren[parallelIndex] = closedParallel;
        containingChildren.InsertRange(parallelIndex + 1, mainChildren.Skip(slot));
        return Replace(root, containingSeries.Id,
            containingSeries with { Children = Array.AsReadOnly(containingChildren.ToArray()) });
    }

    public static EditableLadderNode RemoveSubtree(EditableLadderNode root, string targetId, Func<string> newId)
    {
        if (root.Id == targetId) return new(newId(), LadderNodeKind.Series, Children: Array.Empty<EditableLadderNode>());
        var parent = Visit(root).FirstOrDefault(n => n.Children?.Any(c => c.Id == targetId) == true)
            ?? throw new ArgumentException($"logic: node '{targetId}' does not exist.");
        return Replace(root, parent.Id, parent with { Children = Array.AsReadOnly(parent.Children!.Where(n => n.Id != targetId).ToArray()) });
    }

    public static EditableLadderNode MoveSubtree(EditableLadderNode root, string targetId, string parentId, int slot, Func<string> newId)
    {
        var target = Find(root, targetId) ?? throw new ArgumentException($"logic: node '{targetId}' does not exist.");
        if (Visit(target).Any(n => n.Id == parentId)) throw new ArgumentException("logic: cannot move a subtree into itself.");
        var parent = Find(root, parentId) ?? throw new ArgumentException($"logic: node '{parentId}' does not exist.");
        if (parent.IsLeaf) throw new ArgumentException("logic: move destination must be Series or Parallel.");
        var oldIndex = parent.Children?.ToList().FindIndex(n => n.Id == targetId) ?? -1;
        if (slot < 0 || slot > (parent.Children?.Count ?? 0)) throw new ArgumentOutOfRangeException(nameof(slot));
        if (oldIndex >= 0 && slot > oldIndex) slot--;
        var without = RemoveSubtree(root, targetId, newId);
        return InsertChild(without, parentId, slot, target);
    }

    public static EditableLadderNode CloneWithFreshIds(EditableLadderNode root, Func<string> newId)
    {
        Validate(root);
        EditableLadderNode Copy(EditableLadderNode n) => n with { Id = newId(),
            Children = n.Children is null ? null : Array.AsReadOnly(n.Children.Select(Copy).ToArray()) };
        var result = Copy(root); Validate(result); return result;
    }

    public static EditableLadderNode FromContact(EditableContact c) => new(c.Id,
        c.IsComparison ? LadderNodeKind.Compare : LadderNodeKind.Contact, c.Variable, c.NormallyClosed,
        CompareOperator: c.CompareOperator, RightOperand: c.RightOperand, EdgeMode: c.EdgeMode,
        ComparisonValueType: c.ComparisonValueType) { EdgeMemoryOperand = c.EdgeMemoryOperand };
    public static EditableContact ToContact(EditableLadderNode n)
    {
        if (!n.IsLeaf) throw new ArgumentException("Only expression leaves have contact operands.");
        return new EditableContact(n.Id, n.Variable, n.NormallyClosed, n.Kind == LadderNodeKind.Compare,
            n.CompareOperator, n.RightOperand, n.EdgeMode, n.ComparisonValueType) { EdgeMemoryOperand = n.EdgeMemoryOperand };
    }
}

/// <summary>
/// Compatibility collection for old flat authoring calls. Reads resolve the current tree;
/// mutations commit through one topology callback. It never owns a second editable copy.
/// </summary>
public sealed class EditorTreeList<T> : IList<T>, IReadOnlyList<T>
{
    private readonly Func<List<T>> _read;
    private readonly Action<List<T>> _write;
    public EditorTreeList(Func<List<T>> read, Action<List<T>> write) { _read = read; _write = write; }
    public T this[int index] { get => _read()[index]; set => Change(items => items[index] = value); }
    public int Count => _read().Count;
    public bool IsReadOnly => false;
    private void Change(Action<List<T>> mutation) { var items = _read(); mutation(items); _write(items); }
    public void Add(T item) => Change(items => items.Add(item));
    public void AddRange(IEnumerable<T> items) { var copy = items.ToArray(); Change(list => list.AddRange(copy)); }
    public void Clear() => Change(items => items.Clear());
    public bool Contains(T item) => _read().Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _read().CopyTo(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => _read().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public int IndexOf(T item) => _read().IndexOf(item);
    public int FindIndex(Predicate<T> predicate) => _read().FindIndex(predicate);
    public void Insert(int index, T item) => Change(items => items.Insert(index, item));
    public bool Remove(T item) { var items = _read(); if (!items.Remove(item)) return false; _write(items); return true; }
    public int RemoveAll(Predicate<T> predicate) { var items = _read(); var count = items.RemoveAll(predicate); if (count > 0) _write(items); return count; }
    public void RemoveAt(int index) => Change(items => items.RemoveAt(index));
}
