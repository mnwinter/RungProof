using System;
using System.Collections.Generic;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum LadderTreeCommandId { Copy, Cut, PasteBefore, PasteAfter, PasteAt, Delete, WrapParallel, InsertBefore, InsertAfter, InsertAt, ConvertLegacy, MoveLeft, MoveRight }
public sealed record LadderTreeEditorTarget(long Revision, string BlockId, string NetworkId, string NodeId,
    string ParentId = "", int Slot = -1);
public sealed record LadderTreeEditorRequest(LadderTreeCommandId Command, LadderTreeEditorTarget Target, EditableLadderNode? Payload = null);
public sealed record LadderTreeEditorResult(bool Applied, string Reason, string SelectionId = "", EditableLadderNode? Clipboard = null);

public static class LadderTreeEditorCommands
{
    public static LadderTreeEditorResult Execute(LadderEditorDocument document, LadderEditorHistory history,
        LadderTreeEditorRequest request, bool mutationLocked = false)
    {
        var target = request.Target;
        if (document.Revision != target.Revision || document.Blocks.Count == 0
            || document.Blocks[document.ActiveBlockIndex].Id != target.BlockId) return new(false, "stale-target");
        var index = document.Rungs.FindIndex(r => r.Id == target.NetworkId);
        if (index < 0) return new(false, "stale-target");
        var rung = document.Rungs[index]; var selected = EditableLadderTree.Find(rung.Root, target.NodeId);
        if (selected is null) return new(false, "stale-target");
        if (mutationLocked && request.Command != LadderTreeCommandId.Copy) return new(false, "mutation-locked");
        if (request.Command == LadderTreeCommandId.Copy) return new(true, "", selected.Id, selected);
        if (request.Command == LadderTreeCommandId.ConvertLegacy)
        {
            if (!rung.LegacyEmptyBranchSemantics) return new(false, "already-recursive");
            history.Execute(document, "Convert network to recursive semantics", () => document.ConvertToRecursiveSemantics(index));
            return new(true, "", selected.Id);
        }
        if (rung.LegacyEmptyBranchSemantics) return new(false, "Convert legacy semantics first: empty Series paths become TRUE wires.");
        // Prepare all identities and validate first. Failed commands never add a history entry.
        var occupied = new HashSet<string>(StringComparer.Ordinal);
        foreach (var other in document.Blocks.SelectMany(b => b.Rungs))
        {
            occupied.Add(other.Id); occupied.Add(other.TerminalActionId);
            foreach (var n in EditableLadderTree.Visit(other.Root)) occupied.Add(n.Id);
        }
        var next = 1;
        string NewId() { string id; do { id = "tree-node-" + next++; } while (!occupied.Add(id)); return id; }
        try
        {
            EditableLadderNode Payload() => EditableLadderTree.CloneWithFreshIds(request.Payload
                ?? throw new ArgumentException("A subtree payload is required."), NewId);
            EditableLadderNode prepared;
            switch (request.Command)
            {
                case LadderTreeCommandId.Cut:
                case LadderTreeCommandId.Delete: prepared = EditableLadderTree.RemoveSubtree(rung.Root, selected.Id, NewId); break;
                case LadderTreeCommandId.WrapParallel: prepared = EditableLadderTree.WrapParallel(rung.Root, selected.Id, NewId); break;
                case LadderTreeCommandId.InsertBefore:
                case LadderTreeCommandId.PasteBefore: prepared = EditableLadderTree.Insert(rung.Root, selected.Id, Payload(), false, NewId); break;
                case LadderTreeCommandId.InsertAfter:
                case LadderTreeCommandId.PasteAfter: prepared = EditableLadderTree.Insert(rung.Root, selected.Id, Payload(), true, NewId); break;
                case LadderTreeCommandId.InsertAt:
                case LadderTreeCommandId.PasteAt:
                    var inserted = Payload();
                    if (request.Command == LadderTreeCommandId.InsertAt
                        && EditableLadderTree.Find(rung.Root, target.ParentId)?.Kind == LadderNodeKind.Parallel
                        && inserted.Kind != LadderNodeKind.Series)
                        inserted = new(NewId(), LadderNodeKind.Series, Children: Array.AsReadOnly(new[] { inserted }));
                    prepared = EditableLadderTree.InsertChild(rung.Root, target.ParentId, target.Slot, inserted); break;
                case LadderTreeCommandId.MoveLeft:
                case LadderTreeCommandId.MoveRight:
                    var parent = EditableLadderTree.Visit(rung.Root).FirstOrDefault(n => n.Children?.Any(c => c.Id == selected.Id) == true);
                    if (parent is null) return new(false, "boundary");
                    var currentIndex = parent.Children!.ToList().FindIndex(n => n.Id == selected.Id);
                    var direction = request.Command == LadderTreeCommandId.MoveLeft ? -1 : 1;
                    if (currentIndex + direction < 0 || currentIndex + direction >= parent.Children!.Count) return new(false, "boundary");
                    prepared = EditableLadderTree.MoveSubtree(rung.Root, selected.Id, parent.Id,
                        currentIndex + (direction > 0 ? 2 : -1), NewId); break;
                default: return new(false, "unsupported-command");
            }
            EditableLadderTree.Validate(prepared);
            document.ValidateTreeReplacement(index, prepared);
            history.Execute(document, request.Command.ToString(), () => document.ReplaceSubtree(index, rung.Root.Id, prepared));
            return new(true, "", EditableLadderTree.Find(prepared, selected.Id)?.Id ?? prepared.Id,
                request.Command == LadderTreeCommandId.Cut ? selected : null);
        }
        catch (ArgumentException error) { return new(false, error.Message); }
        catch (InvalidOperationException error) { return new(false, error.Message); }
    }
}
