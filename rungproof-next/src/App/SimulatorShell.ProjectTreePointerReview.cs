using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    /// <summary>
    /// Proves the real Tree pointer route: a single click selects without opening,
    /// while the second click of a viewport-routed double-click activates the row.
    /// The existing ItemActivated production handler remains the only opener.
    /// </summary>
    public async Task<(bool Passed, string Detail)> VerifyTiaProjectTreePointerAsync()
    {
        const string editorRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/EditorAndTasks/EditorAndInspector/ProgramEditor";
        const string treePath = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree";

        var projectTree = GetNodeOrNull<LadderProgramTree>(treePath)
            ?? throw new InvalidOperationException("TIA project tree is unavailable.");
        var centralTagDocument = GetNodeOrNull<ScrollContainer>($"{editorRoot}/PLC tags")
            ?? throw new InvalidOperationException("TIA central tag document is unavailable.");
        var tabTitle = GetNodeOrNull<Label>($"{editorRoot}/EditorTab/EditorTabTitle")
            ?? throw new InvalidOperationException("TIA editor title is unavailable.");
        var globalDbDocument = FindChild("GlobalDbDocument", true, false) as Control
            ?? throw new InvalidOperationException("TIA Global DB document is unavailable.");
        var instanceDbDocument = FindChild("InstanceDbDocument", true, false) as RichTextLabel
            ?? throw new InvalidOperationException("TIA instance DB document is unavailable.");
        var bottomPanel = FindChild("InspectorOutputDock", true, false) as Control
            ?? throw new InvalidOperationException("TIA bottom workspace is unavailable.");
        var bottomTabs = FindChild("InspectorOutputTabs", true, false) as TabContainer
            ?? throw new InvalidOperationException("TIA bottom workspace tabs are unavailable.");

        TreeItem? FindMetadata(TreeItem? first, string metadata)
        {
            for (var item = first; item is not null; item = item.GetNext())
            {
                if (item.GetMetadata(0).AsString() == metadata) return item;
                if (FindMetadata(item.GetFirstChild(), metadata) is { } nested) return nested;
            }
            return null;
        }

        async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        void Send(InputEvent input) => GetViewport().PushInput(input, true);
        void PointerButton(Vector2 position, bool pressed, bool doubleClick = false) => Send(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0,
            Pressed = pressed,
            DoubleClick = doubleClick,
            Position = position,
            GlobalPosition = position,
        });
        async Task<(bool Selected, Vector2 Pointer, Rect2 Area)> SingleClick(TreeItem item)
        {
            item.GetParent()?.SetCollapsed(false);
            projectTree.ScrollToItem(item);
            await Frame();
            await Frame();
            var area = projectTree.GetItemAreaRect(item, 0);
            var at = projectTree.GetGlobalRect().Position + area.GetCenter();
            Send(new InputEventMouseMotion { Position = at, GlobalPosition = at });
            PointerButton(at, true);
            PointerButton(at, false);
            await Frame();
            return (projectTree.GetSelected() == item, at, area);
        }
        async Task DoubleClickSecondPress(Vector2 at)
        {
            PointerButton(at, true, doubleClick: true);
            PointerButton(at, false);
            await Frame();
            await Frame();
        }

        async Task<bool> PressButton(Button button)
        {
            await Frame();
            if (!button.IsVisibleInTree() || button.Disabled) return false;
            button.EmitSignal(BaseButton.SignalName.Pressed);
            await Frame();
            await Frame();
            return true;
        }

        var pointerCallTarget = _ladderDocument.AddBlock("PointerDoubleClickVerifier");
        pointerCallTarget.SemanticMode = LadderSemanticMode.Parameterized;
        var globalDb = _ladderDocument.AddBlock("PointerGlobalData", LadderBlockType.DataBlock);
        _ladderDocument.AddInterfaceParameter(globalDb.Id, "PointerValue", PlcVariableType.Int,
            LadderInterfaceSection.Static, 7L, comment: "Physical pointer activation fixture");
        _ladderDocument.SelectBlock(0);
        var instanceRung = _ladderDocument.Rungs[0];
        instanceRung.IsCounter = true;
        instanceRung.CounterVariable = "Pointer_Count";
        instanceRung.CounterInstanceDataBlock = new LadderInstanceDataBlock(321, "Pointer_Count_DB");
        foreach (var refresh in _ladderEditorRefreshers) refresh();
        await Frame();
        await Frame();

        var row = FindMetadata(projectTree.GetRoot(), "program:1")
            ?? throw new InvalidOperationException("Pointer verification block was not added to the project tree.");
        var single = await SingleClick(row);
        var rowArea = single.Area;
        var treeRect = projectTree.GetGlobalRect();
        var pointer = single.Pointer;
        var rowVisible = rowArea.Size.X > 0 && rowArea.Size.Y > 0
            && treeRect.Grow(-1).HasPoint(pointer)
            && projectTree.IsVisibleInTree();
        if (!rowVisible)
            return (false, $"rowVisible=false tree={treeRect} row={rowArea} pointer={pointer}");

        var selectedBySingleClick = single.Selected
            && projectTree.GetSelected()?.GetMetadata(0).AsString() == "program:1";
        var singleClickDidNotOpen = _ladderDocument.ActiveBlockIndex == 0
            && !centralTagDocument.Visible
            && !tabTitle.Text.Contains("PointerDoubleClickVerifier", StringComparison.Ordinal);

        await DoubleClickSecondPress(pointer);
        var doubleClickOpened = _ladderDocument.ActiveBlockIndex == 1
            && !centralTagDocument.Visible
            && tabTitle.Text.Contains("PointerDoubleClickVerifier", StringComparison.Ordinal);

        async Task OpenProgramFixture()
        {
            row = FindMetadata(projectTree.GetRoot(), "program:1")
                ?? throw new InvalidOperationException("Pointer verification block disappeared from the project tree.");
            var programSingle = await SingleClick(row);
            await DoubleClickSecondPress(programSingle.Pointer);
        }

        async Task<bool> VerifyObject(string metadata, Func<bool> stayedClosed, Func<bool> opened)
        {
            var item = FindMetadata(projectTree.GetRoot(), metadata)
                ?? throw new InvalidOperationException($"TIA project-tree object is unavailable: {metadata}.");
            var objectSingle = await SingleClick(item);
            var negative = objectSingle.Selected && stayedClosed();
            await DoubleClickSecondPress(objectSingle.Pointer);
            return negative && opened();
        }

        await OpenProgramFixture();
        var tagTableObject = await VerifyObject("tag-table:Default tag table",
            () => !centralTagDocument.IsVisibleInTree() && _ladderDocument.ActiveBlockIndex == 1,
            () => centralTagDocument.IsVisibleInTree() && tabTitle.Text == "Default tag table");

        await OpenProgramFixture();
        var globalDbObject = await VerifyObject("program:2",
            () => !globalDbDocument.Visible && _ladderDocument.ActiveBlockIndex == 1,
            () => globalDbDocument.IsVisibleInTree()
                && tabTitle.Text.Contains("PointerGlobalData", StringComparison.Ordinal));

        await OpenProgramFixture();
        var instanceDbObject = await VerifyObject("instance-db:321",
            () => !instanceDbDocument.Visible && _ladderDocument.ActiveBlockIndex == 1,
            () => instanceDbDocument.IsVisibleInTree()
                && instanceDbDocument.Text.Contains("Pointer_Count_DB", StringComparison.Ordinal));

        await OpenProgramFixture();
        bottomPanel.Hide();
        await Frame();
        var watchTableObject = await VerifyObject("watch-table:0",
            () => !bottomPanel.Visible && _ladderDocument.ActiveBlockIndex == 1,
            () => bottomPanel.Visible && bottomTabs.CurrentTab == 2
                && _ladderDocument.ActiveWatchTableIndex == 0);
        bottomPanel.Hide();
        await Frame();
        await OpenProgramFixture();

        async Task<bool> VerifyCommand(string metadata, string dialogName)
        {
            var command = FindMetadata(projectTree.GetRoot(), metadata)
                ?? throw new InvalidOperationException($"TIA project-tree command is unavailable: {metadata}.");
            var dialog = FindChild(dialogName, true, false) as ConfirmationDialog
                ?? throw new InvalidOperationException($"TIA project-tree dialog is unavailable: {dialogName}.");
            dialog.Hide();
            var commandSingle = await SingleClick(command);
            var stayedClosed = commandSingle.Selected && !dialog.Visible;
            await DoubleClickSecondPress(commandSingle.Pointer);
            var opened = dialog.Visible;
            dialog.Hide();
            await Frame();
            return stayedClosed && opened;
        }

        var addBlockCommand = await VerifyCommand("add-block", "CreateBlockDialog");
        var addTagTableCommand = await VerifyCommand("add-tag-table", "CreateTagTableDialog");
        var addWatchTableCommand = await VerifyCommand("add-watch-table", "CreateWatchTableDialog");

        async Task<bool> VerifyProjectTreeCreation(
            string commandMetadata,
            string dialogName,
            string validName,
            Func<bool> objectExists,
            Func<bool> treeRowExists)
        {
            _ladderHistory.Clear();
            var baselineSnapshot = _ladderDocument.CaptureSnapshot();
            var baselineJson = LadderEditorProjectJson.Save(_ladderDocument);
            var invalidRejected = false;
            var created = false;
            var undoExact = false;
            var redoExact = false;
            var cleanupExact = false;
            try
            {
                var command = FindMetadata(projectTree.GetRoot(), commandMetadata)
                    ?? throw new InvalidOperationException($"TIA project-tree creation command disappeared: {commandMetadata}.");
                var dialog = FindChild(dialogName, true, false) as ConfirmationDialog
                    ?? throw new InvalidOperationException($"TIA creation dialog disappeared: {dialogName}.");
                var name = dialog.FindChild("CreationName", true, false) as LineEdit
                    ?? throw new InvalidOperationException($"TIA creation dialog has no name field: {dialogName}.");
                var error = dialog.FindChild("CreationError", true, false) as Label
                    ?? throw new InvalidOperationException($"TIA creation dialog has no error field: {dialogName}.");

                var commandClick = await SingleClick(command);
                await DoubleClickSecondPress(commandClick.Pointer);
                if (!dialog.Visible) return false;

                name.Text = string.Empty;
                dialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                await Frame(); await Frame();
                invalidRejected = dialog.Visible && error.Text.Length > 0
                    && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                    && !_ladderHistory.CanUndo;

                name.Text = validName;
                dialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                await Frame(); await Frame();
                var appliedJson = LadderEditorProjectJson.Save(_ladderDocument);
                created = !dialog.Visible && appliedJson != baselineJson
                    && objectExists() && treeRowExists() && _ladderHistory.CanUndo;

                undoExact = TryUndoLadderEdit(out _)
                    && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                    && !_ladderHistory.CanUndo && !objectExists();
                await Frame(); await Frame();

                redoExact = TryRedoLadderEdit(out _)
                    && LadderEditorProjectJson.Save(_ladderDocument) == appliedJson
                    && objectExists() && treeRowExists();
                await Frame(); await Frame();

                cleanupExact = TryUndoLadderEdit(out _)
                    && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                    && !_ladderHistory.CanUndo && !objectExists();
                await Frame(); await Frame();
            }
            finally
            {
                var dialog = FindChild(dialogName, true, false) as ConfirmationDialog;
                dialog?.Hide();
                _ladderDocument.RestoreSnapshot(baselineSnapshot);
                _ladderHistory.Clear();
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                await Frame(); await Frame();
            }
            var passed = invalidRejected && created && undoExact && redoExact && cleanupExact;
            GD.Print($"TIA_PROJECT_TREE_MUTATION_DETAIL name={validName} pass={passed} invalid={invalidRejected} "
                + $"create={created} undo={undoExact} redo={redoExact} cleanup={cleanupExact}");
            return passed;
        }

        var blockCountBeforeMutation = _ladderDocument.Blocks.Count;
        var blockMutation = await VerifyProjectTreeCreation(
            "add-block", "CreateBlockDialog", "AuditFB99",
            () => _ladderDocument.Blocks.Any(block => block.Name == "AuditFB99"),
            () => FindMetadata(projectTree.GetRoot(), $"program:{blockCountBeforeMutation}")?.GetText(0)
                .Contains("AuditFB99", StringComparison.Ordinal) == true);
        var tagTableMutation = await VerifyProjectTreeCreation(
            "add-tag-table", "CreateTagTableDialog", "AuditTagTable99",
            () => _ladderDocument.TagTables.Contains("AuditTagTable99", StringComparer.Ordinal),
            () => FindMetadata(projectTree.GetRoot(), "tag-table:AuditTagTable99") is not null);
        var watchCountBeforeMutation = _ladderDocument.WatchTables.Count;
        var watchTableMutation = await VerifyProjectTreeCreation(
            "add-watch-table", "CreateWatchTableDialog", "AuditWatchTable99",
            () => _ladderDocument.WatchTables.Any(table => table.Name == "AuditWatchTable99"),
            () => FindMetadata(projectTree.GetRoot(), $"watch-table:{watchCountBeforeMutation}")?.GetText(0)
                .Contains("AuditWatchTable99", StringComparison.Ordinal) == true);
        var projectTreeMutations = blockMutation && tagTableMutation && watchTableMutation;
        GD.Print($"TIA_PROJECT_TREE_MUTATION_MATRIX_VERIFY {(projectTreeMutations ? "PASS" : "FAIL")} "
            + $"block={blockMutation} tagTable={tagTableMutation} watchTable={watchTableMutation}");

        // Exercise the actual Tree -> canvas drag route. The target FC has no
        // formals, so Apply can prove the transaction boundary without hiding
        // binding setup behind programmatic form manipulation.
        var ownerRow = FindMetadata(projectTree.GetRoot(), "program:0")
            ?? throw new InvalidOperationException("TIA owner block row is unavailable for physical CALL drag.");
        var ownerSingle = await SingleClick(ownerRow);
        await DoubleClickSecondPress(ownerSingle.Pointer);
        var tiaHost = GetNodeOrNull<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal")
            ?? throw new InvalidOperationException("TIA workbench is unavailable for physical CALL drag.");
        var canvas = tiaHost.FindChild("GraphicalLadderCanvas", true, false) as LadderEditorCanvas
            ?? throw new InvalidOperationException("TIA ladder canvas is unavailable for physical CALL drag.");
        var ladderScroll = canvas.GetParent();
        while (ladderScroll is not null && ladderScroll is not ScrollContainer) ladderScroll = ladderScroll.GetParent();
        if (ladderScroll is not ScrollContainer scroll)
            throw new InvalidOperationException("TIA ladder canvas has no scroll container.");
        canvas.SelectElement(0, -1, -1, output: true);
        LadderEditorZoom.RevealSelection(canvas, scroll);
        await Frame(); await Frame();

        async Task<bool> DragTargetToTerminal()
        {
            var source = FindMetadata(projectTree.GetRoot(), "program:1")
                ?? throw new InvalidOperationException("TIA FC source row disappeared before physical drag.");
            source.GetParent()?.SetCollapsed(false);
            projectTree.ScrollToItem(source);
            await Frame(); await Frame();
            var sourceArea = projectTree.GetItemAreaRect(source, 0);
            var from = projectTree.GetGlobalRect().Position + sourceArea.GetCenter();
            var localTarget = canvas.GetElementPosition(0, -1, -1, output: true);
            var to = canvas.GetGlobalTransformWithCanvas() * localTarget;
            if (!projectTree.GetGlobalRect().HasPoint(from) || !canvas.GetGlobalRect().HasPoint(to)
                || !scroll.GetGlobalRect().HasPoint(to))
            {
                GD.Print($"TIA_CALL_DRAG_DETAIL stage=visibility from={from} tree={projectTree.GetGlobalRect()} to={to} canvas={canvas.GetGlobalRect()} scroll={scroll.GetGlobalRect()}");
                return false;
            }

            // Headless Windows cannot reliably synthesize Godot's native drag
            // threshold: Relative motion is accumulated in physical pixels
            // while Control geometry is reported in viewport coordinates.
            // Resolve the exact production tree payload and exercise the real
            // canvas acceptance/drop methods at the visible terminal instead.
            var payloadText = ProgramBlockDragPayload.FromTreeMetadata(source.GetMetadata(0).AsString());
            if (payloadText is null) return false;
            var payload = Variant.From(payloadText);
            if (!canvas._CanDropData(localTarget, payload)) return false;
            canvas._DropData(localTarget, payload);
            await Frame(); await Frame();
            var opened = _callAuthoring is { Visible: true };
            GD.Print($"TIA_CALL_DRAG_DETAIL stage=drop opened={opened} from={from} to={to} source={source.GetText(0)} "
                + $"dragData={projectTree.ReviewDragDataRequests} canDrop={canvas.ReviewBlockDropChecks} drops={canvas.ReviewBlockDrops} rejection={canvas.ReviewBlockDropRejection}");
            return opened;
        }

        _ladderHistory.Clear();
        var beforeDrop = LadderEditorProjectJson.Save(_ladderDocument);
        var dropOpenedForCancel = await DragTargetToTerminal();
        var cancelButton = _callAuthoring?.FindChild("CancelCall", true, false) as Button;
        var cancelClicked = cancelButton is not null && await PressButton(cancelButton);
        var dropCancelExact = dropOpenedForCancel && cancelClicked
            && LadderEditorProjectJson.Save(_ladderDocument) == beforeDrop
            && !_ladderHistory.CanUndo && !_ladderDocument.Rungs[0].IsCall;

        var dropOpenedForApply = await DragTargetToTerminal();
        var applyButton = _callAuthoring?.FindChild("ApplyCall", true, false) as Button;
        var applyClicked = applyButton is not null && await PressButton(applyButton);
        var appliedDrop = LadderEditorProjectJson.Save(_ladderDocument);
        var dropApplied = dropOpenedForApply && applyClicked
            && _ladderDocument.Rungs[0].IsCall
            && _ladderDocument.Rungs[0].CallTarget == pointerCallTarget.Id
            && _ladderHistory.CanUndo;
        var dropUndoExact = TryUndoLadderEdit(out _)
            && LadderEditorProjectJson.Save(_ladderDocument) == beforeDrop && !_ladderHistory.CanUndo;
        var dropRedoExact = TryRedoLadderEdit(out _)
            && LadderEditorProjectJson.Save(_ladderDocument) == appliedDrop;

        row = FindMetadata(projectTree.GetRoot(), "program:1")
            ?? throw new InvalidOperationException("Pointer verification block disappeared after CALL drag lifecycle.");
        var finalSelection = await SingleClick(row);
        // Presentation-only cleanup after every physical-input assertion has
        // already been recorded. Keep the screenshot on the FC document.
        projectTree.EmitSignal(Tree.SignalName.ItemActivated);
        projectTree.GrabFocus();
        await Frame();
        var finalTitleRestored = tabTitle.Text.Contains("PointerDoubleClickVerifier", StringComparison.Ordinal)
            && !tabTitle.Text.Contains("Pointer_Count_DB", StringComparison.Ordinal);
        var passed = selectedBySingleClick && singleClickDidNotOpen && doubleClickOpened
            && tagTableObject && watchTableObject && globalDbObject && instanceDbObject
            && addBlockCommand && addTagTableCommand && addWatchTableCommand
            && projectTreeMutations
            && dropCancelExact && dropApplied && dropUndoExact && dropRedoExact
            && finalSelection.Selected && finalTitleRestored;
        return (passed,
            $"singleSelected={selectedBySingleClick} singleStayed={singleClickDidNotOpen} "
            + $"doubleOpened={doubleClickOpened} commands={addBlockCommand}/{addTagTableCommand}/{addWatchTableCommand} "
            + $"mutations={blockMutation}/{tagTableMutation}/{watchTableMutation} "
            + $"objects={tagTableObject}/{watchTableObject}/{globalDbObject}/{instanceDbObject} "
            + $"callDrag={dropOpenedForCancel}/{dropCancelExact}/{dropOpenedForApply}/{dropApplied}/{dropUndoExact}/{dropRedoExact} "
            + $"finalSelected={finalSelection.Selected} finalTitle={finalTitleRestored} "
            + $"focus={projectTree.HasFocus()} "
            + $"tree={treeRect.Size.X:0}x{treeRect.Size.Y:0} row={rowArea.Size.X:0}x{rowArea.Size.Y:0} "
            + $"route=Viewport.PushInput+TreePayloadCanvasDrop");
    }
}
