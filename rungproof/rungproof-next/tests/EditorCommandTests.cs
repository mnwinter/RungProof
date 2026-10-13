using System;
using RungProof.Next.VirtualController;

internal static partial class Program
{
    private static void RunEditorCommandTests()
    {
        Test("OutputDeletePreservesConditionsAndUndo", () =>
        {
            foreach (var kind in new[] { "coil", "counter", "call" })
            {
                var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
                var rung = d.Rungs[0]; rung.IsCounter = kind == "counter"; rung.IsCall = kind == "call";
                var rootId = rung.Root.Id; var actionId = rung.TerminalActionId;
                var contacts = rung.Branches[0].Contacts.Count;
                var target = EditorTarget.Capture(d, 0, -1, -1, EditorOperandRole.Output);
                True(LadderEditorCommands.Execute(d, h, new(EditorCommandId.Delete, target)).Applied);
                False(rung.HasTerminalAction); Equal(rootId, rung.Root.Id); Equal(actionId, rung.TerminalActionId);
                Equal(contacts, rung.Branches[0].Contacts.Count);
                True(h.Undo(d, out _)); True(d.Rungs[0].HasTerminalAction); Equal(actionId, d.Rungs[0].TerminalActionId);
                Equal(kind == "counter", d.Rungs[0].IsCounter); Equal(kind == "call", d.Rungs[0].IsCall);
            }
        });
        Test("OutputPasteRestoresTerminalPresence", () =>
        {
            foreach (var kind in new[] { "coil", "counter", "call" })
            {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            d.Rungs[0].IsCounter = kind == "counter"; d.Rungs[0].IsCall = kind == "call";
            var source = EditorTarget.Capture(d, 0, -1, -1, EditorOperandRole.Output);
            var copied = LadderEditorCommands.Execute(d, h, new(EditorCommandId.Copy, source));
            d.Rungs[1].HasTerminalAction = false;
            var target = EditorTarget.Capture(d, 1, -1, -1, EditorOperandRole.Output);
            False(LadderEditorCommands.Execute(d, h, new(EditorCommandId.Copy, target)).Applied); False(h.CanUndo);
            True(LadderEditorCommands.Execute(d, h, new(EditorCommandId.Paste, target, copied.Clipboard)).Applied);
            True(d.Rungs[1].HasTerminalAction);
            var emitted = d.BuildProgram().Networks[1];
            True(kind == "coil" ? emitted.Coil is not null : kind == "counter" ? emitted.Counter is not null : emitted.Call is not null);
            True(h.Undo(d, out _)); False(d.Rungs[1].HasTerminalAction);
            emitted = d.BuildProgram().Networks[1]; True(emitted.Coil is null && emitted.Counter is null && emitted.Call is null);
            }
        });
        Test("DiagnosticContentRevisionSurvivesNavigation", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            d.AddBlock("Second"); var content = d.ContentRevision; var popup = d.Revision;
            d.SelectBlock(1); Equal(content, d.ContentRevision); True(d.Revision > popup);
            d.SelectBlock(0); Equal(content, d.ContentRevision);
            h.Execute(d, "content edit", () => d.Rungs[0].Label = "Changed"); True(d.ContentRevision > content);
        });
        Test("BoundaryMoveIsRejectedByPredicate", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var target = EditorTarget.Capture(d, 0, 0, 0, EditorOperandRole.Contact);
            var request = new EditorCommandRequest(EditorCommandId.MoveLeft, target);
            var revision = d.Revision;
            False(LadderEditorCommands.CanExecute(d, request, false, out var reason)); Equal("boundary", reason);
            False(LadderEditorCommands.Execute(d, h, request).Applied); False(h.CanUndo); Equal(revision, d.Revision);
        });
        Test("NamedBlockAndTaskCreation", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var initialBlocks = d.Blocks.Count;
            var target = EditorTarget.CaptureProject(d);
            False(LadderEditorCommands.Execute(d, h, new(EditorCommandId.CreateBlock, target, Creation: new(" ", LadderBlockType.FunctionBlock))).Applied);
            False(h.CanUndo); Equal(initialBlocks, d.Blocks.Count);
            True(LadderEditorCommands.Execute(d, h, new(EditorCommandId.CreateBlock, target, Creation: new("Mixer", LadderBlockType.FunctionBlock))).Applied);
            Equal("Mixer", d.Blocks[^1].Name); Equal(LadderBlockType.FunctionBlock, d.Blocks[^1].BlockType);
            target = EditorTarget.CaptureProject(d);
            False(LadderEditorCommands.Execute(d, h, new(EditorCommandId.CreateBlock, target, Creation: new("Mixer"))).Applied);
            Equal(initialBlocks + 1, d.Blocks.Count);
            True(h.Undo(d, out _)); Equal(initialBlocks, d.Blocks.Count); False(h.CanUndo);
            target = EditorTarget.CaptureProject(d);
            True(LadderEditorCommands.Execute(d, h, new(EditorCommandId.CreateTask, target, Creation: new("FastOB", EntryBlock: d.Blocks[0].Id))).Applied);
            Equal("FastOB", d.Tasks[^1].Name); True(h.Undo(d, out _)); False(h.CanUndo);
            // Cancellation never dispatches the pending name/type and cannot record history.
            Equal(initialBlocks, d.Blocks.Count);
        });
        Test("ExactDiagnosticTargetsOperand", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample();
            var diagnostic = EditorDiagnosticTarget.Capture(d, "$.blocks[0].networks[0].logic.children[0].children[1].variable");
            True(diagnostic is not null); Equal(d.Rungs[0].Branches[0].Contacts[1].Id, diagnostic!.Target.NodeId);
            Equal("variable", diagnostic.OperandRole);
            True(EditorDiagnosticTarget.Capture(d, "$.blocks[0].networks[99].coil.variable") is null);
        });
        Test("StaleTargetIsRejected", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var label = d.Rungs[0].Label;
            var target = EditorTarget.Capture(d, 0, 0, 0, EditorOperandRole.Contact);
            h.Execute(d, "edit", () => d.Rungs[0].Label = "Changed");
            var result = LadderEditorCommands.Execute(d, h, new(EditorCommandId.Delete, target));
            False(result.Applied); Equal("stale-target", result.Reason);
            True(h.Undo(d, out _)); Equal(label, d.Rungs[0].Label);
        });
        Test("NetworkDeleteNeverDeletesFinalContact", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var original = d.CaptureSnapshot(); var id = d.Rungs[0].Id;
            var target = EditorTarget.Capture(d, 0, -1, -1, EditorOperandRole.Network);
            True(LadderEditorCommands.Execute(d, h, new(EditorCommandId.Delete, target)).Applied);
            False(d.Rungs.Exists(r => r.Id == id)); Equal(original.Blocks[0].Rungs.Count - 1, d.Rungs.Count);
            True(h.Undo(d, out _)); Equal(id, d.Rungs[0].Id);
            Equal(original.Blocks[0].Rungs[0].Branches[0].Contacts.Count, d.Rungs[0].Branches[0].Contacts.Count);
        });
        Test("AllRoutesHonorMutationLock", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var target = EditorTarget.Capture(d, 0, 0, 0, EditorOperandRole.Contact);
            foreach (var command in new[] { EditorCommandId.Delete, EditorCommandId.MoveLeft, EditorCommandId.MoveRight, EditorCommandId.BeginMutation })
            {
                var result = LadderEditorCommands.Execute(d, h, new(command, target), mutationLocked: true);
                False(result.Applied); Equal("mutation-locked", result.Reason); False(h.CanUndo);
            }
        });
        Test("WirePopupPreservesSlot", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample();
            var t = EditorTarget.Capture(d, 0, 0, 1, EditorOperandRole.Insertion);
            True(LadderEditorCommands.TryResolve(d, t, out var r, out var b, out var slot, out _));
            Equal(0, r); Equal(0, b); Equal(1, slot);
            False(LadderEditorCommands.CanExecute(d, new(EditorCommandId.Delete, t), false, out _));
        });
        Test("OutputCopyIsOutputOnly", () =>
        {
            var d = LadderEditorDocument.CreateConveyorExample(); var h = new LadderEditorHistory();
            var t = EditorTarget.Capture(d, 0, -1, -1, EditorOperandRole.Output);
            var result = LadderEditorCommands.Execute(d, h, new(EditorCommandId.Copy, t));
            True(result.Applied); True(result.Clipboard is EditorOutputClipboard); False(h.CanUndo);
            var output = ((EditorOutputClipboard)result.Clipboard!).Action;
            Equal(d.Rungs[0].CoilVariable, output.CoilVariable); Equal(0, output.Branches.Count);
        });
    }
}
