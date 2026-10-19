using System;
using System.Linq;

namespace RungProof.Next.VirtualController;

public enum EditorOperandRole { Project, Network, Contact, Output, Insertion }
public enum EditorCommandId { Delete, Copy, CopyNetwork, MoveLeft, MoveRight, Paste, CreateBlock, CreateTask, BeginMutation }

/// <summary>Popup/keyboard targets never fall back to a changing selection.</summary>
public sealed record EditorTarget(long Revision, string BlockId, string NetworkId,
    string NodeId, string BranchId, EditorOperandRole Role, int InsertionIndex = -1)
{
    public static EditorTarget CaptureProject(LadderEditorDocument document) => new(document.Revision,
        document.Blocks.Count > 0 ? document.Blocks[document.ActiveBlockIndex].Id : string.Empty,
        string.Empty, string.Empty, string.Empty, EditorOperandRole.Project);
    public static EditorTarget Capture(LadderEditorDocument document, int rung, int branch, int node, EditorOperandRole role)
    {
        var network = document.Rungs[rung];
        var path = branch >= 0 ? network.Branches[branch] : null;
        return new(document.Revision, document.Blocks[document.ActiveBlockIndex].Id, network.Id,
            role == EditorOperandRole.Contact ? path!.Contacts[node].Id
                : role == EditorOperandRole.Output ? network.TerminalActionId : network.Id,
            path?.Id ?? string.Empty, role, role == EditorOperandRole.Insertion ? node : -1);
    }
}

public abstract record EditorClipboard;
public sealed record EditorContactClipboard(EditableContact Contact) : EditorClipboard;
public sealed record EditorNetworkClipboard(EditableRung Network) : EditorClipboard;
public sealed record EditorOutputClipboard(EditableRung Action) : EditorClipboard;
public sealed record EditorNamedCreation(string Name, LadderBlockType BlockType = LadderBlockType.Function,
    LadderTaskKind TaskKind = LadderTaskKind.Continuous, TimeSpan Period = default, int Priority = 0, string EntryBlock = "");
public sealed record EditorCommandRequest(EditorCommandId CommandId, EditorTarget Target, EditorClipboard? Payload = null, EditorNamedCreation? Creation = null, string Description = "");
public sealed record EditorCommandResult(bool Applied, string Reason, EditorTarget? Selection = null, EditorClipboard? Clipboard = null);

public static class LadderEditorCommands
{
    public static bool TryResolve(LadderEditorDocument document, EditorTarget target,
        out int rung, out int branch, out int node, out string reason)
    {
        rung = branch = node = -1;
        reason = "stale-target";
        if (document.Revision != target.Revision) return false;
        if (target.Role == EditorOperandRole.Project) { reason = string.Empty; return true; }
        if (document.Blocks.Count == 0 || document.Blocks[document.ActiveBlockIndex].Id != target.BlockId) return false;
        rung = document.Rungs.FindIndex(item => item.Id == target.NetworkId);
        if (rung < 0) return false;
        if (target.Role == EditorOperandRole.Output && document.Rungs[rung].TerminalActionId != target.NodeId) return false;
        if (target.Role is EditorOperandRole.Contact or EditorOperandRole.Insertion)
        {
            branch = document.Rungs[rung].Branches.FindIndex(item => item.Id == target.BranchId);
            if (branch < 0) return false;
            var contacts = document.Rungs[rung].Branches[branch].Contacts;
            node = target.Role == EditorOperandRole.Contact
                ? contacts.FindIndex(item => item.Id == target.NodeId) : target.InsertionIndex;
            if (node < 0 || node > contacts.Count || (target.Role == EditorOperandRole.Contact && node == contacts.Count)) return false;
        }
        reason = string.Empty;
        return true;
    }

    public static bool CanExecute(LadderEditorDocument document, EditorCommandRequest request,
        bool mutationLocked, out string reason)
    {
        reason = "mutation-locked";
        if (mutationLocked && request.CommandId is not (EditorCommandId.Copy or EditorCommandId.CopyNetwork)) return false;
        if (!TryResolve(document, request.Target, out var rung, out var branch, out var node, out reason)) return false;
        if (request.CommandId == EditorCommandId.BeginMutation) return true;
        if (request.CommandId is EditorCommandId.CreateBlock or EditorCommandId.CreateTask)
        {
            var creation = request.Creation;
            reason = "name-required";
            if (creation is null || string.IsNullOrWhiteSpace(creation.Name)) return false;
            var name = creation.Name.Trim();
            if (request.CommandId == EditorCommandId.CreateBlock)
            {
                reason = "duplicate-name";
                if (document.Blocks.Any(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;
                reason = "block-name-conflicts-with-plc-tag";
                if (document.Tags.Any(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;
            }
            else
            {
                reason = "duplicate-name";
                if (document.Tasks.Any(item => item.Name == name)) return false;
            }
            if (request.CommandId == EditorCommandId.CreateTask && !document.Blocks.Any(item => item.Id == creation.EntryBlock))
            { reason = "missing-entry-block"; return false; }
            reason = string.Empty; return true;
        }
        reason = "unsupported-target";
        var role = request.Target.Role;
        if (role == EditorOperandRole.Output && request.CommandId is (EditorCommandId.Copy or EditorCommandId.Delete)
            && !document.Rungs[rung].HasTerminalAction)
        { reason = "missing-terminal-action"; return false; }
        if (request.CommandId == EditorCommandId.Paste && request.Payload is EditorOutputClipboard output && !output.Action.HasTerminalAction)
        { reason = "missing-terminal-action"; return false; }
        if (role == EditorOperandRole.Contact && request.CommandId is (EditorCommandId.MoveLeft or EditorCommandId.MoveRight))
        {
            if (!document.Rungs[rung].IsFlat)
            {
                var parent = EditableLadderTree.Visit(document.Rungs[rung].Root).FirstOrDefault(n => n.Children?.Any(c => c.Id == request.Target.NodeId) == true);
                var currentIndex = parent?.Children?.ToList().FindIndex(n => n.Id == request.Target.NodeId) ?? -1;
                var destination = currentIndex + (request.CommandId == EditorCommandId.MoveLeft ? -1 : 1);
                if (parent?.Children is null || currentIndex < 0 || destination < 0 || destination >= parent.Children.Count)
                { reason = "boundary"; return false; }
            }
            else
            {
            var next = node + (request.CommandId == EditorCommandId.MoveLeft ? -1 : 1);
            if (next < 0 || next >= document.Rungs[rung].Branches[branch].Contacts.Count)
            { reason = "boundary"; return false; }
            }
        }
        var allowed = request.CommandId switch
        {
            EditorCommandId.CopyNetwork => role != EditorOperandRole.Project,
            EditorCommandId.Copy => role is EditorOperandRole.Network or EditorOperandRole.Contact or EditorOperandRole.Output,
            EditorCommandId.Delete => role is EditorOperandRole.Network or EditorOperandRole.Contact or EditorOperandRole.Output,
            EditorCommandId.MoveLeft or EditorCommandId.MoveRight => role == EditorOperandRole.Contact,
            EditorCommandId.Paste => request.Payload switch
            {
                EditorContactClipboard => role is EditorOperandRole.Contact or EditorOperandRole.Insertion,
                EditorOutputClipboard => role == EditorOperandRole.Output,
                EditorNetworkClipboard => role == EditorOperandRole.Network,
                _ => false
            },
            _ => false
        };
        if (allowed) reason = string.Empty;
        return allowed;
    }

    public static EditorCommandResult Execute(LadderEditorDocument document, LadderEditorHistory history,
        EditorCommandRequest request, bool mutationLocked = false)
    {
        if (!CanExecute(document, request, mutationLocked, out var reason)) return new(false, reason);
        if (request.CommandId == EditorCommandId.BeginMutation)
        {
            // Existing synchronous form handlers use this preparation command
            // until their staged transaction payloads are introduced in phase 2.
            history.Record(document, string.IsNullOrWhiteSpace(request.Description) ? "Edit" : request.Description);
            return new(true, string.Empty);
        }
        TryResolve(document, request.Target, out var rung, out var branch, out var node, out _);
        if (request.Target.Role == EditorOperandRole.Contact && !document.Rungs[rung].IsFlat
            && request.CommandId is EditorCommandId.Delete or EditorCommandId.MoveLeft or EditorCommandId.MoveRight or EditorCommandId.Paste)
        {
            var command = request.CommandId == EditorCommandId.Delete ? LadderTreeCommandId.Delete
                : request.CommandId == EditorCommandId.MoveLeft ? LadderTreeCommandId.MoveLeft
                : request.CommandId == EditorCommandId.MoveRight ? LadderTreeCommandId.MoveRight : LadderTreeCommandId.PasteAfter;
            var payload = request.Payload is EditorContactClipboard contact ? EditableLadderTree.FromContact(contact.Contact) : null;
            var target = new LadderTreeEditorTarget(request.Target.Revision, request.Target.BlockId, request.Target.NetworkId, request.Target.NodeId);
            var result = LadderTreeEditorCommands.Execute(document, history, new(command, target, payload), mutationLocked);
            return new(result.Applied, result.Reason);
        }
        if (request.CommandId is EditorCommandId.Copy or EditorCommandId.CopyNetwork)
        {
            var template = document.CaptureRung(rung);
            EditorClipboard clipboard;
            if (request.CommandId == EditorCommandId.CopyNetwork || request.Target.Role == EditorOperandRole.Network)
                clipboard = new EditorNetworkClipboard(template);
            else if (request.Target.Role == EditorOperandRole.Contact)
                clipboard = new EditorContactClipboard(document.Rungs[rung].Branches[branch].Contacts[node] with { });
            else { template.SetRoot(new(template.Id + "-output-clipboard", LadderNodeKind.Parallel, Children: Array.Empty<EditableLadderNode>())); clipboard = new EditorOutputClipboard(template); }
            return new(true, string.Empty, request.Target, clipboard);
        }
        // Reject no-op moves before recording history.
        if (request.CommandId is EditorCommandId.MoveLeft or EditorCommandId.MoveRight)
        {
            var next = node + (request.CommandId == EditorCommandId.MoveLeft ? -1 : 1);
            if (next < 0 || next >= document.Rungs[rung].Branches[branch].Contacts.Count) return new(false, "boundary");
        }
        history.Execute(document, request.CommandId.ToString(), () =>
        {
            switch (request.CommandId)
            {
                case EditorCommandId.CreateBlock:
                    document.AddBlock(request.Creation!.Name.Trim(), request.Creation.BlockType);
                    document.SelectBlock(document.Blocks.Count - 1);
                    break;
                case EditorCommandId.CreateTask:
                    var creation = request.Creation!;
                    document.AddTask(creation.Name.Trim(), creation.TaskKind,
                        creation.Period == default ? document.ScanPeriod : creation.Period, creation.Priority, creation.EntryBlock);
                    break;
                case EditorCommandId.Delete:
                    if (request.Target.Role == EditorOperandRole.Network) document.RemoveRung(rung);
                    else if (request.Target.Role == EditorOperandRole.Output) document.Rungs[rung].HasTerminalAction = false;
                    else document.RemoveContact(rung, branch, node);
                    break;
                case EditorCommandId.MoveLeft: node = document.MoveContact(rung, branch, node, -1); break;
                case EditorCommandId.MoveRight: node = document.MoveContact(rung, branch, node, 1); break;
                case EditorCommandId.Paste:
                    if (request.Payload is EditorContactClipboard contact)
                    {
                        node = request.Target.Role == EditorOperandRole.Insertion ? node : node + 1;
                        document.PasteContact(contact.Contact, rung, branch, node);
                    }
                    else if (request.Payload is EditorNetworkClipboard network)
                        document.PasteRung(network.Network, ++rung);
                    else if (request.Payload is EditorOutputClipboard output)
                        document.ReplaceOutput(rung, output.Action);
                    break;
            }
        });
        EditorTarget? selection = null;
        if (request.CommandId is EditorCommandId.CreateBlock or EditorCommandId.CreateTask) return new(true, string.Empty);
        if (document.Rungs.Count > 0)
        {
            rung = Math.Min(rung, document.Rungs.Count - 1);
            var role = request.CommandId == EditorCommandId.Delete ? EditorOperandRole.Network
                : request.Payload is EditorNetworkClipboard ? EditorOperandRole.Network : request.Target.Role;
            if (role == EditorOperandRole.Insertion) role = EditorOperandRole.Contact;
            selection = EditorTarget.Capture(document, rung, branch, node, role);
        }
        return new(true, string.Empty, selection);
    }
}

public sealed partial class LadderEditorDocument
{
    public long Revision { get; private set; }
    public long ContentRevision { get; private set; }
    internal void InvalidateCommandTargets(bool contentChanged = true)
    {
        Revision++;
        if (contentChanged) ContentRevision++;
    }

    public void ReplaceOutput(int index, EditableRung action)
    {
        // Terminal identity and conditions belong to the destination network.
        CopyOutput(action, Rungs[index]);
    }

    private static void CopyOutput(EditableRung source, EditableRung destination)
    {
        destination.HasTerminalAction = source.HasTerminalAction;
        destination.CoilVariable = source.CoilVariable; destination.CoilMode = source.CoilMode;
        destination.TimerVariable = source.TimerVariable; destination.TimerPreset = source.TimerPreset;
        destination.TimerPresetAssigned = source.TimerPresetAssigned;
        destination.TimerPresetOperand = source.TimerPresetOperand;
        destination.TimerResetOperand = source.TimerResetOperand;
        destination.TimerKind = source.TimerKind; destination.IsTimer = source.IsTimer; destination.IsTimerReset = source.IsTimerReset;
        destination.TimerInstanceDataBlock = null;
        destination.IsCounter = source.IsCounter; destination.IsCounterReset = source.IsCounterReset; destination.IsCounterLoad = source.IsCounterLoad;
        destination.CounterKind = source.CounterKind; destination.CounterVariable = source.CounterVariable; destination.CounterPreset = source.CounterPreset;
        destination.CounterValueType = source.CounterValueType;
        destination.CounterLoadInput = source.CounterLoadInput;
        destination.CounterResetInput = source.CounterResetInput;
        destination.CounterInstanceDataBlock = null;
        destination.IsNumericOperation = source.IsNumericOperation; destination.NumericOperationKind = source.NumericOperationKind;
        destination.NumericSourceA = source.NumericSourceA; destination.NumericSourceB = source.NumericSourceB; destination.NumericSourceC = source.NumericSourceC;
        destination.NumericDestination = source.NumericDestination; destination.NumericDataType = source.NumericDataType;
        destination.NumericInputDataType = source.NumericInputDataType;
        destination.NumericOutputDataType = source.NumericOutputDataType;
        destination.IsCall = source.IsCall; destination.CallTarget = source.CallTarget;
        destination.CallSemanticMode = source.CallSemanticMode; destination.CallBindings = source.CallBindings.ToArray(); destination.CallInstanceId = source.CallInstanceId;
        destination.IsReturn = source.IsReturn; destination.IsJump = source.IsJump; destination.IsLabel = source.IsLabel;
        destination.NetworkLabelId = source.NetworkLabelId; destination.NetworkLabelName = source.NetworkLabelName;
        destination.ProgramControlLabel = source.ProgramControlLabel;
    }
}
