using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private async void CaptureRecursiveEditorReview(string[] arguments)
    {
        try
        {
            string Option(string name,string fallback)=>arguments.FirstOrDefault(a=>a.StartsWith(name+"=",StringComparison.Ordinal))?.Split('=',2)[1]??fallback;
            ShowRecursiveEditorReview(Option("--review-style","tia"),Option("--review-fixture","nested"),arguments.Contains("--review-terminal"));
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(_capturePath is not null)
            {
                await ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
                var error=GetViewport().GetTexture().GetImage().SavePng(_capturePath);
                if(error!=Error.Ok) throw new InvalidOperationException("Review capture failed: "+error);
                GD.Print("RECURSIVE_REVIEW_CAPTURE "+_capturePath); GetTree().Quit(0);
            }
        }
        catch(Exception error) { GD.PushError(error.ToString()); GetTree().Quit(1); }
    }
    /// <summary>Review fixture uses the production editable document and actual canvas.</summary>
    private void ShowRecursiveEditorReview(string style, string fixture, bool revealTerminal = false)
    {
        var document = RecursiveEditorReviewDocument(fixture);
        var overlay = new CanvasLayer { Name = "RecursiveEditorReview", Layer = 100 };
        var background = new PanelContainer { Name = "ReviewBackground" };
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        overlay.AddChild(background); AddChild(overlay);
        var stack = new VBoxContainer(); background.AddChild(stack);
        stack.AddChild(new Label { Text = $"RECURSIVE EDITOR REVIEW · {style} · {fixture} · actual production canvas" });
        var scroll = new ScrollContainer { Name = "ReviewScroll", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto };
        stack.AddChild(scroll);
        var canvas = new LadderEditorCanvas(style == "logix" ? LadderVendorStyle.RockwellLogix : LadderVendorStyle.SiemensTia, document);
        scroll.AddChild(canvas); canvas.RefreshDocument();
        var history = new LadderEditorHistory();
        LadderTreeEditorResult? lastResult = null;
        LadderTreeEditorResult ApplyTreeCommand(LadderTreeEditorRequest request)
        {
            var result = LadderTreeEditorCommands.Execute(document, history, request);
            lastResult = result;
            if (result.Clipboard is not null) canvas.TreeClipboard = result.Clipboard;
            canvas.RefreshDocument();
            if (result.Applied && result.SelectionSlot >= 0)
                canvas.SelectTreeInsertionPoint(0, result.SelectionParentId, result.SelectionSlot, notify: false);
            else if (result.Applied)
                canvas.SelectTreeNode(0, result.SelectionId, notify: false);
            GD.Print($"RECURSIVE_REVIEW_COMMAND command={request.Command} applied={result.Applied} reason={result.Reason}");
            return result;
        }
        canvas.TreeCommandRequested += request => ApplyTreeCommand(request);
        if (fixture == "branch-slot")
        {
            canvas.SelectTreeInsertionPoint(0, "seal-path", 1, notify: false);
            var target = canvas.CaptureTreeTarget();
            var result = ApplyTreeCommand(new(LadderTreeCommandId.OpenParallelAtSlot, target));
            if (!result.Applied) throw new InvalidOperationException("Branch slot review failed: " + result.Reason);
            if (!canvas.SelectTreeInsertionPoint(0, result.SelectionParentId, result.SelectionSlot, notify: false))
                throw new InvalidOperationException("New branch insertion slot was not selectable.");
            GD.Print($"RECURSIVE_REVIEW_BRANCH_SLOT parent={target.ParentId} slot={target.Slot} newPath={result.SelectionId}");
        }
        else if (fixture is "branch-pointer" or "branch-delete-menu" or "branch-close-preview" or "branch-close-final" or "branch-close-current-final" or "branch-close-multipath-preview" or "branch-close-multipath-final" or "branch-close-multiopen-preview" or "branch-close-multiopen-final")
        {
            var originalProject = LadderEditorProjectJson.Save(document);
            var originalIds = EditableLadderTree.Visit(document.Rungs[0].Root).Select(node => node.Id).ToArray();
            var pointer = canvas.GetInsertionPointPosition(0, 1, 1);
            canvas._GuiInput(new InputEventMouseButton
            {
                ButtonIndex = MouseButton.Left,
                Pressed = true,
                Position = pointer,
            });
            var pointerTarget = canvas.CaptureTreeTarget();
            if (pointerTarget.ParentId != "seal-path" || pointerTarget.Slot != 1)
                throw new InvalidOperationException($"Pointer selected {pointerTarget.ParentId}[{pointerTarget.Slot}] instead of seal-path[1].");

            var previewedBranchSlot = canvas.PreviewInsertionAt(pointer);
            var previewTarget = canvas.CaptureTreeTarget();
            var branchDropped = canvas.DropInstructionAt("branch", canvas.GetSelectionPosition());
            if (!previewedBranchSlot || previewTarget.ParentId != "seal-path" || previewTarget.Slot != 1
                || !branchDropped || lastResult is not { Applied: true } branchResult
                || branchResult.SelectionSlot != 0)
                throw new InvalidOperationException("Pointer branch drop did not open an exact editable path: "
                    + $"preview={previewedBranchSlot} previewTarget={previewTarget.ParentId}[{previewTarget.Slot}] "
                    + $"drop={branchDropped} command={lastResult?.Applied}/{lastResult?.Reason}.");
            var newPathId = branchResult.SelectionParentId;
            var newPathSlot = canvas.GetSelectionPosition();
            if (!canvas.DropInstructionAt("no", newPathSlot) || lastResult is not { Applied: true })
                throw new InvalidOperationException("Pointer contact drop did not fill the new parallel path.");

            var newPath = EditableLadderTree.Find(document.Rungs[0].Root, newPathId);
            var insertedContactId = newPath?.Children is { Count: 1 } ? newPath.Children[0].Id : string.Empty;
            var finalIds = EditableLadderTree.Visit(document.Rungs[0].Root).Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
            var stableIdsPreserved = originalIds.All(finalIds.Contains)
                && insertedContactId.Length > 0 && !originalIds.Contains(newPathId) && !originalIds.Contains(insertedContactId);
            var openCompile = LadderCompiler.Compile(document.BuildProgram());
            var remainsOpen = newPath?.OpenEnd == true
                && !openCompile.IsValid
                && openCompile.Issues.Any(issue => issue.Code == "VC003"
                    && issue.Path.EndsWith(".openEnd", StringComparison.Ordinal)
                    && issue.Message.Contains("must be closed", StringComparison.OrdinalIgnoreCase));
            var finalProject = LadderEditorProjectJson.Save(document);

            var undoContact = history.Undo(document, out var undoContactDescription);
            var undoBranch = history.Undo(document, out var undoBranchDescription);
            var undoExact = undoContact && undoBranch && LadderEditorProjectJson.Save(document) == originalProject;
            var redoBranch = history.Redo(document, out var redoBranchDescription);
            var redoContact = history.Redo(document, out var redoContactDescription);
            var redoExact = redoBranch && redoContact && LadderEditorProjectJson.Save(document) == finalProject;
            if (!stableIdsPreserved || !remainsOpen || !undoExact || !redoExact
                || undoContactDescription != LadderTreeCommandId.InsertAt.ToString()
                || undoBranchDescription != LadderTreeCommandId.OpenParallelAtSlot.ToString()
                || redoBranchDescription != LadderTreeCommandId.OpenParallelAtSlot.ToString()
                || redoContactDescription != LadderTreeCommandId.InsertAt.ToString())
                throw new InvalidOperationException("Pointer branch edit did not preserve the open topology, stable IDs, compile rejection, or exact history.");

            canvas.RefreshDocument();
            canvas.SelectTreeNode(0, newPathId, notify: false);
            GD.Print($"TIA_POINTER_BRANCH_EDIT_PASS parent={pointerTarget.ParentId} slot={pointerTarget.Slot} "
                + $"newPath={newPathId} contact={insertedContactId} stableIds=true open=true compileRejected=true undo=true redo=true pointer={pointer}");
            if (fixture is "branch-close-multipath-preview" or "branch-close-multipath-final" or "branch-close-multiopen-preview" or "branch-close-multiopen-final")
            {
                var firstSplit = EditableLadderTree.Visit(document.Rungs[0].Root)
                    .Single(node => node.Kind == LadderNodeKind.Parallel
                        && node.Children?.Any(child => child.Id == newPathId) == true);
                var firstMain = firstSplit.Children![0];
                var firstClose = ApplyTreeCommand(new(LadderTreeCommandId.CloseParallelAtSlot,
                    new(document.Revision, "main", "network", newPathId, firstMain.Id,
                        firstMain.Children!.Count)));
                var secondOpen = ApplyTreeCommand(new(LadderTreeCommandId.OpenParallelAtSlot,
                    new(document.Revision, "main", "network", firstSplit.Id, firstSplit.Id,
                        firstSplit.Children.Count)));
                var secondInsert = ApplyTreeCommand(new(LadderTreeCommandId.InsertAt,
                    new(document.Revision, "main", "network", secondOpen.SelectionId,
                        secondOpen.SelectionParentId, 0),
                    new("multipath-contact-template", LadderNodeKind.Contact, "Start_request")));
                if (!firstClose.Applied || !secondOpen.Applied || !secondInsert.Applied)
                    throw new InvalidOperationException("Multi-path branch fixture setup failed.");
                newPathId = secondOpen.SelectionId;
                insertedContactId = EditableLadderTree.Find(document.Rungs[0].Root, newPathId)
                    ?.Children?.Single().Id ?? string.Empty;
                var multiOpenCompile = LadderCompiler.Compile(document.BuildProgram());
                if (insertedContactId.Length == 0 || multiOpenCompile.IsValid)
                    throw new InvalidOperationException("Multi-path branch fixture did not retain its second open path.");
                canvas.RefreshDocument();
                canvas.SelectTreeNode(0, newPathId, notify: false);
                GD.Print($"TIA_MULTIPATH_BRANCH_SETUP_PASS split={firstSplit.Id} path={newPathId} contact={insertedContactId} open=true");
                if (fixture is "branch-close-multiopen-preview" or "branch-close-multiopen-final")
                {
                    var secondPathId = newPathId;
                    var refreshedSplit = EditableLadderTree.Find(document.Rungs[0].Root, firstSplit.Id)!;
                    var thirdOpen = ApplyTreeCommand(new(LadderTreeCommandId.OpenParallelAtSlot,
                        new(document.Revision, "main", "network", refreshedSplit.Id, refreshedSplit.Id,
                            refreshedSplit.Children!.Count)));
                    var thirdInsert = ApplyTreeCommand(new(LadderTreeCommandId.InsertAt,
                        new(document.Revision, "main", "network", thirdOpen.SelectionId,
                            thirdOpen.SelectionParentId, 0),
                        new("multiopen-contact-template", LadderNodeKind.Contact, "Start_request")));
                    if (!thirdOpen.Applied || !thirdInsert.Applied)
                        throw new InvalidOperationException("Multiple-open branch fixture setup failed.");
                    var thirdPathId = thirdOpen.SelectionId;
                    var thirdContactId = EditableLadderTree.Find(document.Rungs[0].Root, thirdPathId)
                        ?.Children?.Single().Id ?? string.Empty;
                    var openIssues = LadderCompiler.Compile(document.BuildProgram()).Issues.Count(issue =>
                        issue.Code == "VC003" && issue.Path.EndsWith(".openEnd", StringComparison.Ordinal));
                    if (thirdContactId.Length == 0 || openIssues != 2)
                        throw new InvalidOperationException("Multiple-open branch fixture did not retain two independent open paths.");
                    if (fixture == "branch-close-multiopen-final")
                    {
                        var currentMain = EditableLadderTree.Find(document.Rungs[0].Root, firstMain.Id)!;
                        var secondClose = ApplyTreeCommand(new(LadderTreeCommandId.CloseParallelAtSlot,
                            new(document.Revision, "main", "network", secondPathId, currentMain.Id, 1)));
                        if (!secondClose.Applied
                            || EditableLadderTree.Find(document.Rungs[0].Root, secondPathId)?.OpenEnd != false
                            || EditableLadderTree.Find(document.Rungs[0].Root, thirdPathId)?.OpenEnd != true)
                            throw new InvalidOperationException("Closing the first of two open paths consumed or changed its sibling.");
                        newPathId = thirdPathId;
                        insertedContactId = thirdContactId;
                    }
                    else
                    {
                        newPathId = secondPathId;
                    }
                    canvas.RefreshDocument();
                    canvas.SelectTreeNode(0, newPathId, notify: false);
                    GD.Print($"TIA_MULTIPLE_OPEN_BRANCH_SETUP_PASS split={firstSplit.Id} second={secondPathId} third={thirdPathId} openIssues=2");
                }
            }
            if (fixture is "branch-close-preview" or "branch-close-final" or "branch-close-current-final" or "branch-close-multipath-preview" or "branch-close-multipath-final" or "branch-close-multiopen-preview" or "branch-close-multiopen-final")
            {
                var split = EditableLadderTree.Visit(document.Rungs[0].Root)
                    .Single(node => node.Kind == LadderNodeKind.Parallel
                        && node.Children?.Any(child => child.Id == newPathId) == true);
                var mainPath = split.Children![0];
                if (mainPath.Children is not { Count: > 1 })
                    throw new InvalidOperationException("Branch close fixture needs two downstream main-path elements.");
                var trailingId = mainPath.Children[1].Id;
                var closeStart = canvas.GetTreeOutputWirePosition(0, newPathId);
                var closeSlot = fixture == "branch-close-current-final" ? mainPath.Children.Count : 1;
                var closeTarget = canvas.GetTreeSlotPosition(0, mainPath.Id, closeSlot);
                canvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    Pressed = true,
                    Position = closeStart,
                });
                canvas._GuiInput(new InputEventMouseMotion
                {
                    Position = closeTarget,
                    ButtonMask = MouseButtonMask.Left,
                });
                if (!canvas.ReviewBranchCloseDragActive || canvas.ReviewBranchCloseCandidateSlot != closeSlot)
                    throw new InvalidOperationException("Branch close drag did not expose the exact permissible downstream slot.");
                GD.Print($"TIA_BRANCH_CLOSE_PREVIEW_PASS path={newPathId} main={mainPath.Id} slot={closeSlot} start={closeStart} target={closeTarget}");
                if (fixture is "branch-close-final" or "branch-close-current-final" or "branch-close-multipath-final" or "branch-close-multiopen-final")
                {
                    var beforeClose = LadderEditorProjectJson.Save(document);
                    var idsBeforeClose = EditableLadderTree.Visit(document.Rungs[0].Root)
                        .Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
                    canvas._GuiInput(new InputEventMouseButton
                    {
                        ButtonIndex = MouseButton.Left,
                        Pressed = false,
                        Position = closeTarget,
                    });
                    var closedRoot = document.Rungs[0].Root;
                    var closedSplit = EditableLadderTree.Find(closedRoot, split.Id);
                    var container = closedSplit is null ? null : EditableLadderTree.Visit(closedRoot)
                        .FirstOrDefault(node => node.Kind == LadderNodeKind.Series
                            && node.Children?.Any(child => child.Id == closedSplit.Id) == true);
                    var splitIndex = container?.Children?.ToList().FindIndex(child => child.Id == split.Id) ?? -1;
                    var topologyClosed = fixture is "branch-close-multipath-final" or "branch-close-multiopen-final"
                        ? closedSplit?.Children is { Count: 2 } outerPaths
                            && outerPaths[0].Children is { Count: 2 } restructuredMain
                            && restructuredMain[0].Kind == LadderNodeKind.Parallel
                            && restructuredMain[0].Children is { Count: 2 } nestedPaths
                            && nestedPaths[1].Id == newPathId
                            && !nestedPaths[1].OpenEnd
                            && restructuredMain[1].Id == trailingId
                        : closeSlot == mainPath.Children.Count
                            ? closedSplit?.Children?[1].OpenEnd == false
                            : closedSplit?.Children?[0].Children?.Count == 1
                                && splitIndex >= 0 && splitIndex + 1 < container!.Children!.Count
                                && container.Children[splitIndex + 1].Id == trailingId;
                    var closedIds = EditableLadderTree.Visit(closedRoot).Select(node => node.Id).ToHashSet(StringComparer.Ordinal);
                    var identitiesPreserved = fixture is "branch-close-multipath-final" or "branch-close-multiopen-final"
                        ? idsBeforeClose.IsSubsetOf(closedIds) && closedIds.Count == idsBeforeClose.Count + 2
                        : idsBeforeClose.SetEquals(closedIds);
                    var closed = lastResult is { Applied: true }
                        && topologyClosed
                        && identitiesPreserved
                        && LadderCompiler.Compile(document.BuildProgram()).IsValid;
                    var afterClose = LadderEditorProjectJson.Save(document);
                    var undoClose = history.Undo(document, out var undoCloseDescription)
                        && LadderEditorProjectJson.Save(document) == beforeClose;
                    var redoClose = history.Redo(document, out var redoCloseDescription)
                        && LadderEditorProjectJson.Save(document) == afterClose;
                    if (!closed || !undoClose || !redoClose
                        || undoCloseDescription != LadderTreeCommandId.CloseParallelAtSlot.ToString()
                        || redoCloseDescription != LadderTreeCommandId.CloseParallelAtSlot.ToString())
                        throw new InvalidOperationException("Branch close did not preserve exact topology, identities, compilation, or history.");
                    canvas.RefreshDocument();
                    canvas.SelectTreeNode(0, newPathId, notify: false);
                    var finalGate = fixture == "branch-close-multipath-final" ? "TIA_MULTIPATH_BRANCH_CLOSE_FINAL_PASS"
                        : fixture == "branch-close-multiopen-final" ? "TIA_MULTIPLE_OPEN_BRANCH_CLOSE_FINAL_PASS"
                        : "TIA_BRANCH_CLOSE_FINAL_PASS";
                    GD.Print($"{finalGate} "
                        + $"path={newPathId} main={mainPath.Id} slot={closeSlot} stableIds=true compile=true undo=true redo=true");
                }
            }
            else if (fixture == "branch-delete-menu")
            {
                var connection = canvas.GetTreeInputWirePosition(0, newPathId);
                canvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Right,
                    Pressed = true,
                    Position = connection,
                });
                var popupItems = canvas.ReviewTreePopupItems();
                var connectionSelected = canvas.SelectedNodeId == newPathId;
                var menuMatched = popupItems.Contains("Delete branch")
                    && !popupItems.Contains("Remove selected subtree");
                var menuActivated = canvas.ReviewActivateTreePopup(LadderTreeCommandId.Delete);
                var deleted = menuActivated && lastResult is { Applied: true }
                    && EditableLadderTree.Find(document.Rungs[0].Root, newPathId) is null
                    && LadderCompiler.Compile(document.BuildProgram()).IsValid;
                var deletedProject = LadderEditorProjectJson.Save(document);
                var deleteUndo = history.Undo(document, out var deleteUndoDescription)
                    && LadderEditorProjectJson.Save(document) == finalProject;
                var deleteRedo = history.Redo(document, out var deleteRedoDescription)
                    && LadderEditorProjectJson.Save(document) == deletedProject;
                var captureRestore = history.Undo(document, out _)
                    && LadderEditorProjectJson.Save(document) == finalProject;
                if (!connectionSelected || !menuMatched || !deleted || !deleteUndo || !deleteRedo || !captureRestore
                    || deleteUndoDescription != LadderTreeCommandId.Delete.ToString()
                    || deleteRedoDescription != LadderTreeCommandId.Delete.ToString())
                    throw new InvalidOperationException("TIA branch connection-line menu did not delete and restore the exact nested path.");
                canvas.RefreshDocument();
                connection = canvas.GetTreeInputWirePosition(0, newPathId);
                canvas._GuiInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Right,
                    Pressed = true,
                    Position = connection,
                });
                GD.Print($"TIA_BRANCH_CONNECTION_DELETE_PASS path={newPathId} pointer={connection} "
                    + "selected=true menu=Delete_branch compile=true undo=true redo=true toolbarShortcut=hidden");
            }
        }
        else canvas.SelectTreeNode(0, document.Rungs[0].Root.Id);
        if (revealTerminal) Callable.From(() => scroll.ScrollHorizontal = (int)canvas.CustomMinimumSize.X).CallDeferred();
        GD.Print($"RECURSIVE_REVIEW_READY style={style} fixture={fixture} width={canvas.CustomMinimumSize.X} height={canvas.CustomMinimumSize.Y}");
    }

    private static LadderEditorDocument RecursiveEditorReviewDocument(string fixture)
    {
        if (fixture is "tia-move" or "tia-add" or "tia-norm")
        {
            var kind = fixture switch
            {
                "tia-move" => LadderNumericOperationKind.Move,
                "tia-norm" => LadderNumericOperationKind.Normalize,
                _ => LadderNumericOperationKind.Add,
            };
            var numericRoot = new LadderNode("root", LadderNodeKind.Series, Children: [
                new("enable", LadderNodeKind.Contact, "Enable_request")]);
            var numeric = kind switch
            {
                LadderNumericOperationKind.Move => new LadderNumericOperation("instruction", kind, "Source_A", "0", "Destination"),
                LadderNumericOperationKind.Normalize => new LadderNumericOperation("instruction", kind, "-50", "Source_A", "Destination", "50"),
                _ => new LadderNumericOperation("instruction", kind, "Source_A", "Source_B", "Destination"),
            };
            var numericNetwork = new LadderNetwork("network", fixture.ToUpperInvariant() + " TIA pin review", numericRoot, NumericOperation: numeric);
            var numericProgram = new LadderProgram(2, "tia-numeric-pin-review", "TIA numeric pin review", "LD", TimeSpan.FromMilliseconds(20), [
                new("Enable_request", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.0", Comment: "Enable"),
                new("Source_A", PlcVariableType.Real, PlcVariableRole.Memory, 12.5, Address: "%MD20", Comment: "First operand"),
                new("Source_B", PlcVariableType.Real, PlcVariableRole.Memory, 2.5, Address: "%MD24", Comment: "Second operand"),
                new("Destination", PlcVariableType.Real, PlcVariableRole.Memory, 0.0, Address: "%MD28", Comment: "Instruction result")],
                [numericNetwork], [new("main", "TIA numeric pin review", [numericNetwork])], "main");
            var numericDocument = new LadderEditorDocument(); numericDocument.ReplaceFromProgram(numericProgram);
            return numericDocument;
        }

        if (fixture == "tia-edge-memory")
        {
            var edgeDocument = new LadderEditorDocument { Id = "tia-edge-memory-review", Name = "Edge Memory Review" };
            edgeDocument.AddTag("Start_request", PlcVariableRole.Input, address: "%I0.0", comment: "Rising edge input");
            edgeDocument.AddTag("Stop_request", PlcVariableRole.Input, address: "%I0.1", comment: "Falling edge input");
            edgeDocument.AddTag("Start_pulse", PlcVariableRole.Output, "start_pulse", address: "%Q0.0");
            edgeDocument.AddTag("Stop_pulse", PlcVariableRole.Output, "stop_pulse", address: "%Q0.1");
            edgeDocument.AddRung("Positive edge with its memory operand", "Start_pulse");
            var positive = edgeDocument.InsertEdgeContact(0, 0, 0, "Start_request", LadderEdgeMode.Rising);
            edgeDocument.UpdateTag(positive.EdgeMemoryOperand!, "M_Start_previous", PlcVariableType.Bool,
                PlcVariableRole.Memory, address: "%M0.0", comment: "Positive edge memory");
            edgeDocument.AddRung("Negative edge with its memory operand", "Stop_pulse");
            var negative = edgeDocument.InsertEdgeContact(1, 0, 0, "Stop_request", LadderEdgeMode.Falling);
            edgeDocument.UpdateTag(negative.EdgeMemoryOperand!, "M_Stop_previous", PlcVariableType.Bool,
                PlcVariableRole.Memory, address: "%M0.1", comment: "Negative edge memory");
            return edgeDocument;
        }

        if (fixture == "timer-missing-pt")
        {
            var timerNetwork = new LadderNetwork("network", "TON with unassigned PT",
                new("root", LadderNodeKind.Series, Children: [new("start", LadderNodeKind.Contact, "Start_request")]),
                Timer: new("timer", "IEC_Timer_0_DB", TimeSpan.FromMilliseconds(1000),
                    LadderTimerKind.OnDelay, new LadderInstanceDataBlock(8, "IEC_Timer_0_DB")));
            var timerProgram = new LadderProgram(2, "timer-missing-pt-review", "Timer PT review", "LD",
                TimeSpan.FromMilliseconds(20), [
                    new("Start_request", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "Timer enable"),
                    new("IEC_Timer_0_DB", PlcVariableType.Timer, PlcVariableRole.Memory, TimeSpan.Zero, Address: "%DB8", Comment: "TON instance data")],
                [timerNetwork], [new("main", "Timer review", [timerNetwork])], "main");
            var timerDocument = new LadderEditorDocument();
            timerDocument.ReplaceFromProgram(timerProgram);
            timerDocument.Rungs[0].TimerPresetAssigned = false;
            timerDocument.Rungs[0].TimerPresetOperand = string.Empty;
            return timerDocument;
        }

        LadderNode Contact(string id, string variable) => new(id, LadderNodeKind.Contact, variable);
        var root = new LadderNode("root", LadderNodeKind.Series, Children: [
            new("choices", LadderNodeKind.Parallel, Children: [
                new("upper-path", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
                new("lower-path", LadderNodeKind.Series, Children: [
                    new("nested-choice", LadderNodeKind.Parallel, Children: [
                        new("edge-path", LadderNodeKind.Series, Children: [
                            new LadderNode("edge", LadderNodeKind.Contact, "Start_request", EdgeMode: LadderEdgeMode.Rising)
                            { EdgeMemoryOperand = "Start_request_edge_memory" }]),
                        new("empty-wire", LadderNodeKind.Series, Children: [])])])]),
            Contact("permissive", "Process_permissive")]);
        if (fixture is "branch-slot" or "branch-pointer" or "branch-delete-menu" or "branch-close-preview" or "branch-close-final" or "branch-close-current-final" or "branch-close-multipath-preview" or "branch-close-multipath-final" or "branch-close-multiopen-preview" or "branch-close-multiopen-final") root = new("root", LadderNodeKind.Parallel, Children: [
            new("start-path", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
            new("seal-path", LadderNodeKind.Series, Children: [
                Contact("seal-in", "Drive_enable"),
                new("stop", LadderNodeKind.Contact, "Stop_request", NormallyClosed: true),
                new("photoeye", LadderNodeKind.Contact, "Photoeye_clear", NormallyClosed: true)])]);
        if (fixture == "tag-info") root = new("root", LadderNodeKind.Series, Children: [
            new("seal-in-branches", LadderNodeKind.Parallel, Children: [
                new("start-path", LadderNodeKind.Series, Children: [
                    Contact("start", "Start_request"), Contact("start-permissive", "Process_permissive"),
                    new("start-not-complete", LadderNodeKind.Contact, "Layer_complete", NormallyClosed: true)]),
                new("hold-path", LadderNodeKind.Series, Children: [
                    Contact("seal-in", "Drive_enable"), Contact("hold-permissive", "Process_permissive"),
                    new("hold-not-complete", LadderNodeKind.Contact, "Layer_complete", NormallyClosed: true)])])]);
        if (fixture == "view-controls") root = new("root", LadderNodeKind.Series, Children: [
            Contact("start", "Start_request"), Contact("permissive", "Process_permissive")]);
        if (fixture is "legacy" or "converted") root = new("root", LadderNodeKind.Parallel, Children: [
            new("active-path", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
            new("ignored-empty-path", LadderNodeKind.Series, Children: [])]);
        if (fixture == "empty") root = new("root", LadderNodeKind.Series, Children: [
            new("wire-true", LadderNodeKind.Series, Children: []), new("parallel-false", LadderNodeKind.Parallel, Children: [])]);
        if (fixture == "long") root = new("root", LadderNodeKind.Series, Children: [
            new("compare", LadderNodeKind.Compare, "Measured_process_value_with_long_qualified_name", CompareOperator: LadderCompareOperator.GreaterOrEqual, RightOperand: "123.456"),
            new("choice", LadderNodeKind.Parallel, Children: [
                new("left", LadderNodeKind.Series, Children: [Contact("start", "Start_request")]),
                new("right", LadderNodeKind.Series, Children: [Contact("permissive", "Process_permissive")])])]);
        var network = new LadderNetwork("network", "Recursive topology / exact subtree boundaries", root, Coil: new("terminal", "Drive_enable"));
        var program = new LadderProgram(2, "recursive-native-review", "Recursive review", "LD", TimeSpan.FromMilliseconds(20), [
            new("Start_request", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.0", Comment: "Operator start request"),
            new("Stop_request", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.1", Comment: "Stop request"),
            new("Photoeye_clear", PlcVariableType.Bool, PlcVariableRole.Input, true, Address: "%I0.2", Comment: "Photoeye clear"),
            new("Process_permissive", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.3", Comment: "Process safety permissive"),
            new("Layer_complete", PlcVariableType.Bool, PlcVariableRole.Input, false, Address: "%I0.4", Comment: "Layer complete"),
            new("Start_request_edge_memory", PlcVariableType.Bool, PlcVariableRole.Memory, false, Address: "%M0.0", Comment: "Positive-edge memory"),
            new("Drive_enable", PlcVariableType.Bool, PlcVariableRole.Output, false, Address: "%Q0.0", Comment: "Drive enable command"),
            new("Measured_process_value_with_long_qualified_name", PlcVariableType.Real, PlcVariableRole.Memory, 100.0, Address: "%MD20", Comment: "Measured process value")],
            [network], [new("main", "Recursive review", [network])], "main");
        var document = new LadderEditorDocument(); document.ReplaceFromProgram(program);
        document.Rungs[0].LegacyEmptyBranchSemantics = fixture == "legacy";
        if (fixture == "terminal-draft") document.Rungs[0].HasTerminalAction = false;
        if (fixture == "view-controls")
        {
            document.Tags.Add(new("Measured_value", PlcVariableType.Real, PlcVariableRole.Memory, 100.0,
                Address: "%MD20", Comment: "Measured process value"));
            document.Tags.Add(new("Calculation_result", PlcVariableType.Real, PlcVariableRole.Memory, 0.0,
                Address: "%MD24", Comment: "Calculated process command"));
            var reviewRung = document.Rungs[0];
            reviewRung.CoilVariable = string.Empty;
            reviewRung.IsNumericOperation = true;
            reviewRung.NumericOperationKind = LadderNumericOperationKind.Add;
            reviewRung.NumericDataType = LadderNumericDataType.Real;
            reviewRung.NumericSourceA = "Measured_value";
            reviewRung.NumericSourceB = "123.456";
            reviewRung.NumericDestination = "Calculation_result";
        }
        if (fixture == "view-hierarchy")
        {
            document.AddAggregateTag("motor_state", PlcVariableRole.Memory, PlcVariableType.Struct,
                new PlcAggregateSchema([new PlcAggregateField("ready", PlcVariableType.Bool, "Motor permission is satisfied")]),
                new Dictionary<string, object> { ["ready"] = true }, comment: "Motor control state");
            document.Rungs[0].SetRoot(new EditableLadderNode("root", LadderNodeKind.Series, Children: [
                new("motor-ready", LadderNodeKind.Contact, "motor_state.ready"),
                new("start", LadderNodeKind.Contact, "Start_request")]));
            document.Rungs[0].Label = "Structured permissive / hierarchical comment";
        }
        return document;
    }
}
