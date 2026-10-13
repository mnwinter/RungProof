using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    public bool VerifyEditorTransactions(out string detail)
    {
        var host = GetNode<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal");
        T Find<T>(string name) where T : Node => host.FindChild(name, true, false) as T ?? throw new InvalidOperationException("Missing " + name);
        var original = _ladderDocument.CaptureSnapshot(); var history = _ladderHistory.Copy();
        var checks = new List<string>();
        void Require(string label, bool ok) { GD.Print($"EDITOR_TRANSACTION {label} {(ok ? "PASS" : "FAIL")}"); if (!ok) throw new InvalidOperationException(label); checks.Add(label); }
        void Refresh() { foreach (var refresh in _ladderEditorRefreshers) refresh(); }
        try
        {
            foreach (var form in _pendingEditorForms) form.Discard();
            SetReviewState("ladder", ""); _ladderEnvironmentTabs.CurrentTab = 0;
            _ladderDocument.ReplaceFromProgram(LadderEditorDocument.CreateConveyorExample().BuildProgram()); _ladderHistory.Clear(); Refresh();
            var canvas = Find<LadderEditorCanvas>("GraphicalLadderCanvas");
            canvas.SelectElement(0, 0, 0);
            canvas.GrabFocus(); GD.Print($"EDITOR_TRANSACTION_FOCUS visible={canvas.IsVisibleInTree()} focused={canvas.HasFocus()} selected={canvas.SelectedNodeId} rung={canvas.SelectedRung}"); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            var editor = Find<LadderOperandEditor>("InlineOperandEditor"); var text = Find<LineEdit>("OperandText");
            Require("F2OpensInlineOperand", editor.Visible);
            var before = LadderEditorProjectJson.Save(_ladderDocument); text.Text = "staged_counter_input";
            text.EmitSignal(Control.SignalName.FocusExited);
            Require("FocusLossKeepsPendingDraft", editor.Visible && before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo);
            editor.CancelEdit();
            Require("EscapeLeavesDocumentAndHistoryUnchanged", !editor.Visible && before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo);
            canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            text.Text = "staged_counter_input"; var count = _ladderDocument.Tags.Count;
            Require("CreateAndAssignCommit", editor.TryApply(true));
            Require("CreateTagAndAssignIsAtomic", _ladderDocument.Tags.Count == count + 1 && _ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable == "staged_counter_input");
            Require("SingleUndoRestoresBoth", TryUndoLadderEdit(out _) && _ladderDocument.Tags.Count == count && !_ladderHistory.CanUndo);
            canvas.SelectElement(0, 0, 0); canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            text.Text = "another_pending_input"; var continued = false;
            ResolvePendingEditorForms(() => continued = true);
            Require("SaveResolvesPendingForm", _pendingEditorDialog?.Visible == true && !continued);
            _pendingEditorDialog!.EmitSignal(Window.SignalName.CloseRequested); _pendingEditorDialog.Hide(); editor.CancelEdit();
            // Exercise the actual Main integrated-save event, not merely the shared resolver.
            var integratedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "rungproof-pending-save-" + Guid.NewGuid().ToString("N") + ".json");
            canvas.SelectElement(0, 0, 0); canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            text.Text = "missing_pending_operand";
            var savedBaseline = CaptureIntegratedLadderSavedBaseline();
            SaveWorkspaceToPathRequested?.Invoke(integratedPath);
            Require("IntegratedSaveResolvesBeforeWriting", _pendingEditorDialog?.Visible == true && !System.IO.File.Exists(integratedPath)
                && CaptureIntegratedLadderSavedBaseline() == savedBaseline);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("IntegratedSaveCancelKeepsDraft", editor.Visible && text.Text == "missing_pending_operand" && !System.IO.File.Exists(integratedPath));
            SaveWorkspaceToPathRequested?.Invoke(integratedPath);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed); _pendingEditorDialog.Hide();
            Require("IntegratedSaveInvalidApplyDoesNotWrite", editor.Visible && !System.IO.File.Exists(integratedPath)
                && CaptureIntegratedLadderSavedBaseline() == savedBaseline);
            var beforeDiscard = CaptureIntegratedLadderDraft();
            SaveWorkspaceToPathRequested?.Invoke(integratedPath);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
            using (var saved = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(integratedPath)))
                Require("IntegratedSaveDiscardWritesExactCommittedDraft", !editor.Visible
                    && saved.RootElement.GetProperty("ladderProjectJson").GetString() == beforeDiscard);
            canvas.SelectElement(0, 0, 0); canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            var replacement = _ladderDocument.Tags.First(tag => tag.Type == PlcVariableType.Bool
                && tag.Name != _ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable).Name;
            text.Text = replacement;
            SaveWorkspaceToPathRequested?.Invoke(integratedPath);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed); _pendingEditorDialog.Hide();
            using (var saved = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(integratedPath)))
                Require("IntegratedSaveApplyWritesExactResolvedDraft", !editor.Visible
                    && _ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable == replacement
                    && saved.RootElement.GetProperty("ladderProjectJson").GetString() == CaptureIntegratedLadderDraft());
            canvas.SelectElement(0, -1, -1, true); var edit = Find<MenuButton>("LadderEditMenu").GetPopup();
            edit.EmitSignal(PopupMenu.SignalName.IdPressed, 7L);
            Require("OutputCutKeepsConditions", !_ladderDocument.Rungs[0].HasTerminalAction && _ladderDocument.Rungs[0].Branches[0].Contacts.Count > 0 && _ladderCommandClipboard is EditorOutputClipboard);
            Require("CutUndo", TryUndoLadderEdit(out _) && _ladderDocument.Rungs[0].HasTerminalAction);
            // Actual property controls must remain staged, reject a malformed operand, and commit atomically.
            _ladderHistory.Clear();
            _ladderDocument.AddTag("review_number", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            var contact = _ladderDocument.Rungs[0].Branches[0].Contacts[0];
            _ladderDocument.ReplaceContact(0, 0, 0, contact with { IsComparison = true, Variable = "review_number", RightOperand = "0" }); Refresh();
            canvas.SelectElement(0, 0, 0);
            canvas._GuiInput(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left, DoubleClick = true, Position = canvas.GetElementPosition(0, 0, 0) });
            var right = Find<LineEdit>("CompareRightOperand");
            var apply = Find<Button>("ApplyInstructionProperties");
            var propertyBefore = LadderEditorProjectJson.Save(_ladderDocument); right.Text = "missing_number"; right.EmitSignal(LineEdit.SignalName.TextChanged, "missing_number");
            GD.Print($"EDITOR_TRANSACTION_PROPERTY pending={string.Join(",", _pendingEditorForms.ConvertAll(f => f.HasPending()))} status={Find<Label>("InstructionPropertyStatus").Text} node={canvas.SelectedNodeId}");
            right.EmitSignal(Control.SignalName.FocusExited); apply.EmitSignal(BaseButton.SignalName.Pressed);
            Require("InvalidPropertyHasNoPartialEdit", propertyBefore == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo && Find<Label>("InstructionPropertyStatus").Text.Contains("missing_number", StringComparison.Ordinal));
            GD.Print($"EDITOR_TRANSACTION_PROPERTY_AFTER pending={string.Join(",", _pendingEditorForms.ConvertAll(f => f.HasPending()))} status={Find<Label>("InstructionPropertyStatus").Text}");
            var priorNode = canvas.SelectedNodeId;
            canvas.SelectElement(0, 0, 1);
            Require("NavigationRequiresPendingResolution", _pendingEditorDialog?.Visible == true);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("NavigationCancelRestoresSelection", canvas.SelectedNodeId == priorNode && right.Text == "missing_number");
            var projectMenu = Find<MenuButton>("LadderProjectMenu").GetPopup();
            projectMenu.EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
            Require("NewProjectResolvesPendingForm", _pendingEditorDialog?.Visible == true && propertyBefore == LadderEditorProjectJson.Save(_ladderDocument));
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("NewProjectCancelKeepsDraft", right.Text == "missing_number" && _pendingEditorForms.Any(form => form.HasPending()));
            right.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.Delete });
            Require("TextFieldShortcutsDoNotDeleteNetwork", propertyBefore == LadderEditorProjectJson.Save(_ladderDocument));
            right.Text = "7"; right.EmitSignal(LineEdit.SignalName.TextChanged, "7"); apply.EmitSignal(BaseButton.SignalName.Pressed);
            Require("PropertyApplyCommitsWholeDraft", _ladderDocument.Rungs[0].Branches[0].Contacts[0].RightOperand == "7");
            Require("PropertyApplySingleUndo", TryUndoLadderEdit(out _) && !_ladderHistory.CanUndo && _ladderDocument.Rungs[0].Branches[0].Contacts[0].RightOperand == "0");
            var tags = Find<Tree>("TagTable"); var tagName = Find<LineEdit>("NewTagName");
            SelectIndexedTreeRow(tags, 0); tags.EmitSignal(Tree.SignalName.ItemSelected);
            var oldTag = _ladderDocument.Tags[0].Name;
            tagName.Text = "stale_edit"; tagName.EmitSignal(LineEdit.SignalName.TextChanged, "stale_edit");
            SelectIndexedTreeRow(tags, 1); tags.EmitSignal(Tree.SignalName.ItemSelected);
            Require("TagNavigationPrompts", _pendingEditorDialog?.Visible == true);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("TagNavigationCancelRestoresRow", SelectedTreeRowIndex(tags) == 0 && tagName.Text == "stale_edit");
            _ladderHistory.Execute(_ladderDocument, "External change", () => _ladderDocument.Rungs[0].Label = "Another edit");
            var staleBefore = LadderEditorProjectJson.Save(_ladderDocument);
            Find<Button>("ApplyTagEdit").EmitSignal(BaseButton.SignalName.Pressed);
            Require("StaleTagFormCannotApply", staleBefore == LadderEditorProjectJson.Save(_ladderDocument)
                && _ladderDocument.Tags[0].Name == oldTag && Find<RichTextLabel>("OutputWindow").Text.Contains("stale", StringComparison.Ordinal));
            foreach (var form in _pendingEditorForms) if (form.HasPending()) form.Discard();
            SelectIndexedTreeRow(tags, 0); tags.EmitSignal(Tree.SignalName.ItemSelected);
            tagName.Text = "valid_tag_edit"; tagName.EmitSignal(LineEdit.SignalName.TextChanged, "valid_tag_edit");
            Find<Button>("ApplyTagEdit").EmitSignal(BaseButton.SignalName.Pressed);
            Require("FreshTagFormApplies", _ladderDocument.Tags[0].Name == "valid_tag_edit");
            Require("TagFormUndo", TryUndoLadderEdit(out _) && _ladderDocument.Tags[0].Name == oldTag);
            tagName.Text = "delete_pending_regression"; tagName.EmitSignal(LineEdit.SignalName.TextChanged, "delete_pending_regression");
            Find<Button>("AddTag").EmitSignal(BaseButton.SignalName.Pressed);
            var deleteIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "delete_pending_regression");
            Require("TagCreatedForDelete", deleteIndex >= 0);
            SelectIndexedTreeRow(tags, deleteIndex); tags.EmitSignal(Tree.SignalName.ItemSelected);
            Find<Button>("DeleteTag").EmitSignal(BaseButton.SignalName.Pressed);
            Require("TagDeleteDoesNotCreatePhantomDraft", _ladderDocument.Tags.All(tag => tag.Name != "delete_pending_regression") && !_pendingEditorForms.Any(form => form.HasPending()));
            var aggregate = new AggregateTagEditor(); host.AddChild(aggregate);
            try
            {
                aggregate.Load(PlcVariableType.Array, new PlcAggregateSchema(ElementType: PlcVariableType.Int, LowerBound: -2, Length: 3), new object[] { 10L, 20L, 30L });
                var lower = (SpinBox)aggregate.FindChild("ArrayLowerBound", true, false); lower.Value = -1;
                var valid = aggregate.TryRead(PlcVariableType.Array, out var arraySchema, out var arrayValues, out _);
                var values = arrayValues as object[];
                Require("ArrayResizePreservesAbsoluteIndices", valid && arraySchema.LowerBound == -1 && values is not null && Equals(values[0], 20L) && Equals(values[1], 30L) && Equals(values[2], 0L));
                var valueField = (LineEdit)aggregate.FindChild("FieldInitial", true, false); valueField.Text = "malformed"; lower.Value = -2;
                Require("ArrayMalformedValueRemainsPending", !aggregate.TryRead(PlcVariableType.Array, out _, out _, out var arrayError) && arrayError.Contains("[-1]", StringComparison.Ordinal));
                var typeSelector = (OptionButton)aggregate.FindChild("ArrayElementType", true, false); typeSelector.Select(0); typeSelector.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
                Require("ArrayTypeChangePreservesInvalidText", !aggregate.TryRead(PlcVariableType.Array, out _, out _, out _)
                    && ((LineEdit)aggregate.FindChild("AggregateFieldRow1", true, false).FindChild("FieldInitial", true, false)).Text == "malformed");
                typeSelector.Select(3); typeSelector.EmitSignal(OptionButton.SignalName.ItemSelected, 3L);
                typeSelector.Select(1); typeSelector.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
                Require("ArrayTypeRoundTripPreservesText", !aggregate.TryRead(PlcVariableType.Array, out _, out _, out _)
                    && ((LineEdit)aggregate.FindChild("AggregateFieldRow0", true, false).FindChild("FieldInitial", true, false)).Text == "10"
                    && ((LineEdit)aggregate.FindChild("AggregateFieldRow1", true, false).FindChild("FieldInitial", true, false)).Text == "malformed");
            }
            finally { host.RemoveChild(aggregate); aggregate.QueueFree(); }
            var targetBlock = _ladderDocument.AddBlock("NavigationTarget"); Refresh();
            var navigationName = Find<LineEdit>("BlockName"); var initialRevision = _ladderDocument.Revision; var initialBlock = _ladderDocument.ActiveBlockIndex;
            navigationName.Text = "KeptPendingName"; navigationName.EmitSignal(LineEdit.SignalName.TextChanged, navigationName.Text);
            var reference = LadderProjectIndex.Build(_ladderDocument.BuildProgram()).References.First(item => item.BlockId == targetBlock.Id);
            try { _navigateDeviceReference!(reference); } catch (Exception navigationError) { GD.Print("REFERENCE_NAVIGATION_ERROR " + navigationError); }
            Require("ReferenceNavigationResolvesBeforeMutation", _pendingEditorDialog?.Visible == true && _ladderDocument.ActiveBlockIndex == initialBlock && _ladderDocument.Revision == initialRevision);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("ReferenceCancelKeepsPendingValue", navigationName.Text == "KeptPendingName" && _ladderDocument.Revision == initialRevision);
            Find<Button>("ApplyBlock").EmitSignal(BaseButton.SignalName.Pressed);
            Require("ReferenceCancelDraftStillApplies", _ladderDocument.Blocks[initialBlock].Name == "KeptPendingName");
            _ladderDocument.SelectBlock(_ladderDocument.Blocks.FindIndex(block => block.Id == targetBlock.Id));
            _ladderDocument.AddRung("Invalid target network", "missing_diagnostic_output");
            _ladderDocument.SelectBlock(initialBlock); Refresh();
            Find<Button>("ValidateAndLoadButton").EmitSignal(BaseButton.SignalName.Pressed);
            var diagnostics = Find<ItemList>("ValidationIssues");
            var diagnosticIndex = Enumerable.Range(0, diagnostics.ItemCount).First(index => diagnostics.GetItemText(index).Contains("missing_diagnostic_output", StringComparison.Ordinal));
            navigationName.Text = "DiagnosticPendingName"; navigationName.EmitSignal(LineEdit.SignalName.TextChanged, navigationName.Text);
            var diagnosticRevision = _ladderDocument.Revision;
            diagnostics.EmitSignal(ItemList.SignalName.ItemActivated, (long)diagnosticIndex);
            Require("DiagnosticNavigationResolvesBeforeMutation", _pendingEditorDialog?.Visible == true && _ladderDocument.ActiveBlockIndex == initialBlock && _ladderDocument.Revision == diagnosticRevision);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("DiagnosticCancelKeepsPendingValue", navigationName.Text == "DiagnosticPendingName" && _ladderDocument.Revision == diagnosticRevision);
            Find<Button>("ApplyBlock").EmitSignal(BaseButton.SignalName.Pressed);
            Require("DiagnosticCancelDraftStillApplies", _ladderDocument.Blocks[initialBlock].Name == "DiagnosticPendingName");
            var diagnosticTask = _ladderDocument.AddTask("TaskDiagnosticTarget", LadderTaskKind.Continuous, TimeSpan.FromMilliseconds(20), 20, targetBlock.Id);
            diagnosticTask.EntryBlock = "missing_task_entry"; Refresh();
            Find<Button>("ValidateAndLoadButton").EmitSignal(BaseButton.SignalName.Pressed);
            var taskDiagnosticIndex = Enumerable.Range(0, diagnostics.ItemCount).First(index => diagnostics.GetItemMetadata(index).AsString().Contains("tasks[1]", StringComparison.Ordinal));
            var taskName = Find<LineEdit>("TaskName"); taskName.Text = "TaskPendingName"; taskName.EmitSignal(LineEdit.SignalName.TextChanged, taskName.Text);
            var taskRevision = _ladderDocument.Revision;
            diagnostics.EmitSignal(ItemList.SignalName.ItemActivated, (long)taskDiagnosticIndex);
            Require("TaskDiagnosticWaitsForPendingResolution", _pendingEditorDialog?.Visible == true && taskName.Text == "TaskPendingName" && _ladderDocument.Revision == taskRevision);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide();
            Require("TaskDiagnosticCancelKeepsFields", taskName.Text == "TaskPendingName");
            diagnostics.EmitSignal(ItemList.SignalName.ItemActivated, (long)taskDiagnosticIndex);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed); _pendingEditorDialog.Hide();
            Require("TaskDiagnosticApplyCommitsBeforeNavigation", _ladderDocument.Tasks[0].Name == "TaskPendingName" && taskName.Text == "TaskDiagnosticTarget");
            // Review regression: independent forms must never partially apply each other.
            var blockName = Find<LineEdit>("BlockName"); var parameterName = Find<LineEdit>("InterfaceName");
            var originalBlockName = _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Name;
            blockName.Text = "DiscardedBlockName"; blockName.EmitSignal(LineEdit.SignalName.TextChanged, blockName.Text);
            parameterName.Text = "review_parameter"; parameterName.EmitSignal(LineEdit.SignalName.TextChanged, parameterName.Text);
            var beforeBatch = LadderEditorProjectJson.Save(_ladderDocument); var beforeBatchUndo = _ladderHistory.CanUndo;
            var batchContinued = false; ResolvePendingEditorForms(() => batchContinued = true);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed); _pendingEditorDialog.Hide();
            Require("MultipleFormsRejectBeforeAnyMutation", beforeBatch == LadderEditorProjectJson.Save(_ladderDocument) && beforeBatchUndo == _ladderHistory.CanUndo && !batchContinued);
            parameterName.Text = "invalid parameter!"; parameterName.EmitSignal(LineEdit.SignalName.TextChanged, parameterName.Text);
            ResolvePendingEditorForms(() => batchContinued = true);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed); _pendingEditorDialog.Hide();
            Require("InvalidSecondFormHasNoPartialCommit", beforeBatch == LadderEditorProjectJson.Save(_ladderDocument) && !batchContinued);
            var logixHost = GetNode<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/Studio 5000");
            var logixBlockName = (LineEdit)logixHost.FindChild("BlockName", true, false);
            logixBlockName.Text = "OtherEnvironmentDraft"; logixBlockName.EmitSignal(LineEdit.SignalName.TextChanged, logixBlockName.Text);
            ResolvePendingEditorForms(() => batchContinued = true);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Confirmed); _pendingEditorDialog.Hide();
            Require("MixedEnvironmentBatchHasNoPartialCommit", beforeBatch == LadderEditorProjectJson.Save(_ladderDocument) && !batchContinued);
            ResolvePendingEditorForms(() => batchContinued = true);
            _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
            Require("DiscardRestoresAllVisibleFields", blockName.Text == originalBlockName && logixBlockName.Text == originalBlockName && !_pendingEditorForms.Any(form => form.HasPending()));
            Find<Button>("ApplyBlock").EmitSignal(BaseButton.SignalName.Pressed);
            Require("DiscardedNameCannotResurrect", _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Name == originalBlockName);
            var blockType = Find<OptionButton>("BlockTypeSelector"); var originalBlockType = blockType.Selected;
            blockType.Select((originalBlockType + 1) % blockType.ItemCount); blockType.EmitSignal(OptionButton.SignalName.ItemSelected, (long)blockType.Selected);
            blockName.Text = "EscapeDiscardedName"; blockName.EmitSignal(LineEdit.SignalName.TextChanged, blockName.Text);
            parameterName.Text = "kept_parameter"; parameterName.EmitSignal(LineEdit.SignalName.TextChanged, parameterName.Text);
            var beforeEscapeDiscard = LadderEditorProjectJson.Save(_ladderDocument);
            blockName.EmitSignal(Control.SignalName.GuiInput, new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Require("PerFormEscapeRestoresFieldsWithSiblingPending", blockName.Text == originalBlockName && blockType.Selected == originalBlockType && parameterName.Text == "kept_parameter" && _pendingEditorForms.Any(form => form.HasPending()) && LadderEditorProjectJson.Save(_ladderDocument) == beforeEscapeDiscard);
            Find<Button>("ApplyBlock").EmitSignal(BaseButton.SignalName.Pressed);
            Require("EscapedNameCannotResurrect", _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Name == originalBlockName);
            detail = $"{checks.Count} transaction routes passed"; return true;
        }
        catch (Exception error) { detail = $"{checks.Count} passed; {error}"; return false; }
        finally
        {
            foreach (var form in _pendingEditorForms) form.Discard(); _pendingEditorDialog?.Hide();
            _ladderDocument.RestoreSnapshot(original); _ladderHistory = history; Refresh();
        }
    }
    public async System.Threading.Tasks.Task<(bool Passed, string Detail)> VerifyOperandEditorPlacementAsync()
    {
        var host = GetNode<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal");
        var original = _ladderDocument.CaptureSnapshot(); var history = _ladderHistory.Copy();
        LadderOperandEditor? editor = null;
        try
        {
            foreach (var form in _pendingEditorForms) form.Discard();
            GetWindow().Size = new Vector2I(1600, 900);
            SetReviewState("ladder", ""); _ladderEnvironmentTabs.CurrentTab = 0;
            _ladderDocument.ReplaceFromProgram(LadderEditorDocument.CreateConveyorExample().BuildProgram()); _ladderHistory.Clear();
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            for (var frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var canvas = (LadderEditorCanvas)host.FindChild("GraphicalLadderCanvas", true, false);
            canvas.SelectElement(0, 0, 0); canvas.GrabFocus();
            canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            editor = (LadderOperandEditor)host.FindChild("InlineOperandEditor", true, false);
            editor.PlaceNear(GetViewport().GetVisibleRect().End - new Vector2(10, 10));
            for (var frame = 0; frame < 4; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var viewport = GetViewport().GetVisibleRect();
            var fits = editor.Visible && viewport.Size.X >= 1280 && viewport.Size.Y >= 720 && viewport.Encloses(editor.GetGlobalRect());
            foreach (var name in new[] { "OperandText", "OperandSuggestions", "ApplyOperand", "CancelOperand" })
            {
                var field = (Control)editor.FindChild(name, true, false);
                fits &= field.IsVisibleInTree() && viewport.Encloses(field.GetGlobalRect()) && editor.GetGlobalRect().Encloses(field.GetGlobalRect());
            }
            const string outputPrefix = "--operand-placement-output=";
            var outputArgument = OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.StartsWith(outputPrefix, StringComparison.Ordinal));
            if (outputArgument is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var captureError = GetViewport().GetTexture().GetImage().SavePng(outputArgument[outputPrefix.Length..]);
                if (captureError != Error.Ok) return (false, $"Operand placement capture failed: {captureError}");
            }
            return (fits, $"viewport={viewport} panel={editor.GetGlobalRect()} minimum={editor.GetCombinedMinimumSize()} controlsFit={fits}");
        }
        finally
        {
            editor?.CancelEdit();
            _ladderDocument.RestoreSnapshot(original); _ladderHistory = history;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
    }

}
