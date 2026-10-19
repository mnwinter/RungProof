using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    public bool VerifyEditorTransactions(out string detail, System.Action<string, bool>? assertionObserver = null)
    {
        var host = GetNode<Control>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal");
        T Find<T>(string name) where T : Node => host.FindChild(name, true, false) as T ?? throw new InvalidOperationException("Missing " + name);
        var original = _ladderDocument.CaptureSnapshot(); var history = _ladderHistory.Copy();
        var checks = new List<string>();
        void Require(string label, bool ok) { GD.Print($"EDITOR_TRANSACTION {label} {(ok ? "PASS" : "FAIL")}"); assertionObserver?.Invoke(label, ok); if (!ok) throw new InvalidOperationException(label); checks.Add(label); }
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
            canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.Enter });
            Require("ReturnActivatesFirstOperand", editor.Visible && text.HasFocus()
                && text.Text.Length == 0);
            var originalOperand = _ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable;
            var submittedOperand = _ladderDocument.Tags.First(tag => tag.Type == PlcVariableType.Bool
                && tag.Name != originalOperand).Name;
            text.Text = submittedOperand;
            text.EmitSignal(LineEdit.SignalName.TextSubmitted, submittedOperand);
            Require("ReturnApplyCommitsExactlyOnce", !editor.Visible
                && _ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable == submittedOperand
                && _ladderHistory.CanUndo);
            Require("ReturnApplySingleUndoExact", TryUndoLadderEdit(out _)
                && _ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable == originalOperand
                && !_ladderHistory.CanUndo);
            canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.P, Unicode = 'p' });
            Require("TypingActivatesFirstOperand", editor.Visible && text.HasFocus() && text.Text == "p");
            editor.CancelEdit();
            canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            var suggestions = Find<ItemList>("OperandSuggestions");
            text.EmitSignal(Control.SignalName.GuiInput, new InputEventKey { Pressed = true, Keycode = Key.I, CtrlPressed = true });
            Require("InlineAutocompleteShortcut", editor.Visible && suggestions.Visible);
            editor.CancelEdit();
            canvas.GrabFocus(); canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            text.Text = "new_learning_tag";
            text.EmitSignal(Control.SignalName.GuiInput, new InputEventKey { Pressed = true, Keycode = Key.I, CtrlPressed = true, ShiftPressed = true });
            Require("DefineTagShortcutRevealsCreateAction", Find<Button>("CreateAndAssignOperand").Visible);
            editor.CancelEdit();
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
            var beforeTagApply = LadderEditorProjectJson.Save(_ladderDocument);
            Find<Button>("ApplyTagEdit").EmitSignal(BaseButton.SignalName.Pressed);
            var afterTagApply = LadderEditorProjectJson.Save(_ladderDocument);
            Require("FreshTagFormApplies", _ladderDocument.Tags[0].Name == "valid_tag_edit" && afterTagApply != beforeTagApply);
            Require("TagFormUndo", TryUndoLadderEdit(out _) && LadderEditorProjectJson.Save(_ladderDocument) == beforeTagApply);
            var tagFormRedo = TryRedoLadderEdit(out _) && LadderEditorProjectJson.Save(_ladderDocument) == afterTagApply;
            var tagLoadResult = LadderEditorProjectJson.Load(afterTagApply);
            var tagRoundTripJson = tagLoadResult.Document is null ? string.Empty : LadderEditorProjectJson.Save(tagLoadResult.Document);
            var canonicalTagLoad = LadderEditorProjectJson.Load(tagRoundTripJson);
            var reserializedTagRoundTrip = canonicalTagLoad.Document is null ? string.Empty : LadderEditorProjectJson.Save(canonicalTagLoad.Document);
            var persistedTag = tagLoadResult.Document?.Tags.SingleOrDefault(tag => tag.Name == "valid_tag_edit");
            // Loading repairs a stale allocation cursor in older/in-memory
            // documents, so require the authored tag to survive and the
            // normalized Save/Load form to be stable instead of byte-equality
            // against the pre-normalized source JSON.
            var tagFormSaveLoad = tagLoadResult.IsReadable && persistedTag is not null
                && persistedTag.Comment == Find<LineEdit>("TagComment").Text
                && canonicalTagLoad.IsReadable && tagRoundTripJson == reserializedTagRoundTrip;
            GD.Print($"EDITOR_TRANSACTION_TAG_PERSISTENCE redo={tagFormRedo} loaded={tagLoadResult.IsReadable} roundTrip={tagFormSaveLoad} firstTag={tagLoadResult.Document?.Tags.FirstOrDefault()?.Name ?? "<missing>"} issues={string.Join(";", tagLoadResult.Issues)}");
            Require("TagFormRedo", tagFormRedo);
            Require("TagFormSaveLoad", tagFormSaveLoad);
            Require("TagFormRedoCleanup", TryUndoLadderEdit(out _) && LadderEditorProjectJson.Save(_ladderDocument) == beforeTagApply
                && _ladderDocument.Tags[0].Name == oldTag);
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
            var text = (LineEdit)editor.FindChild("OperandText", true, false);
            for (var frame = 0; frame < 4; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var viewport = GetViewport().GetVisibleRect();
            var expected = canvas.GetOperandEditorBounds(_ladderDocument.Rungs[0].Branches[0].Contacts[0].Variable);
            var editorBounds = editor.GetGlobalRect(); var fieldBounds = text.GetGlobalRect();
            var fits = editor.Visible && viewport.Size.X >= 1280 && viewport.Size.Y >= 720
                && canvas.IsAncestorOf(editor) && text.IsVisibleInTree() && editorBounds.Size.X <= 250
                && Math.Abs(editor.Position.X - expected.Position.X) <= 1 && Math.Abs(editor.Position.Y - expected.Position.Y) <= 1
                && editorBounds.Encloses(fieldBounds) && viewport.Encloses(editorBounds);
            foreach (var name in new[] { "OperandSuggestions", "CreateAndAssignOperand", "ApplyOperand", "CancelOperand" })
                fits &= !((Control)editor.FindChild(name, true, false)).IsVisibleInTree();
            editor.CancelEdit();
            canvas.SelectElement(0, -1, -1, output: true); canvas.GrabFocus();
            canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.F2 });
            for (var frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var outputExpected = canvas.GetOperandEditorBounds(_ladderDocument.Rungs[0].CoilVariable);
            var outputBounds = editor.GetGlobalRect();
            var outputAligned = editor.Visible && Math.Abs(editor.Position.X - outputExpected.Position.X) <= 1
                && Math.Abs(editor.Position.Y - outputExpected.Position.Y) <= 1
                && canvas.IsAncestorOf(editor) && viewport.Encloses(outputBounds);
            fits &= outputAligned;
            editor.CancelEdit();

            // Siemens opens the operand field at the pin that was double-clicked.
            // Exercise distinct counter, timer, comparison, coil, ordinary
            // numeric, lower-row conversion, range, and EXPT pin families
            // through the actual viewport event path, then commit each edit.
            _ladderDocument.AddTag("review_counter", PlcVariableRole.Memory, type: PlcVariableType.Counter);
            _ladderDocument.AddTag("review_counter_down", PlcVariableRole.Memory, type: PlcVariableType.Counter);
            _ladderDocument.AddTag("review_counter_alt", PlcVariableRole.Memory, type: PlcVariableType.Counter);
            _ladderDocument.AddTag("review_reset", PlcVariableRole.Input, type: PlcVariableType.Bool);
            _ladderDocument.AddTag("review_operand", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_result", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_result_2", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_a", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_c", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_int", PlcVariableRole.Memory, type: PlcVariableType.Int);
            _ladderDocument.AddTag("review_int_2", PlcVariableRole.Memory, type: PlcVariableType.Int);
            _ladderDocument.AddTag("review_real", PlcVariableRole.Memory, type: PlcVariableType.Real);
            _ladderDocument.AddTag("review_real_2", PlcVariableRole.Memory, type: PlcVariableType.Real);
            _ladderDocument.AddTag("review_real_result", PlcVariableRole.Memory, type: PlcVariableType.Real);
            _ladderDocument.AddTag("review_real_result_2", PlcVariableRole.Memory, type: PlcVariableType.Real);
            _ladderDocument.AddTag("review_compare", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_compare_2", PlcVariableRole.Memory, type: PlcVariableType.DInt);
            _ladderDocument.AddTag("review_coil", PlcVariableRole.Memory, type: PlcVariableType.Bool);
            _ladderDocument.AddTag("review_timer", PlcVariableRole.Memory, type: PlcVariableType.Timer);
            _ladderDocument.AddTag("review_timer_off", PlcVariableRole.Memory, type: PlcVariableType.Timer);
            _ladderDocument.AddTag("review_timer_pulse", PlcVariableRole.Memory, type: PlcVariableType.Timer);
            _ladderDocument.AddTag("review_timer_retentive", PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var counter = _ladderDocument.AddCounterRung("Operand pin CTU", "review_counter", 4);
            counter.CounterResetInput = "stop_command";
            var math = _ladderDocument.AddNumericOperationRung("Operand pin ADD", LadderNumericOperationKind.Add,
                "0", "3", "review_result");
            math.NumericDataType = LadderNumericDataType.DInt;
            var downCounter = _ladderDocument.AddCounterRung("Operand pin CTD", "review_counter_down", 6,
                kind: LadderCounterKind.CountDown);
            downCounter.CounterLoadInput = "stop_command";
            var ton = _ladderDocument.AddTimerRung("Operand pin TON", "review_timer", TimeSpan.FromSeconds(1));
            var tof = _ladderDocument.AddTimerRung("Operand pin TOF", "review_timer_off",
                TimeSpan.FromSeconds(1), LadderTimerKind.OffDelay);
            var tp = _ladderDocument.AddTimerRung("Operand pin TP", "review_timer_pulse",
                TimeSpan.FromSeconds(1), LadderTimerKind.Pulse);
            var rto = _ladderDocument.AddTimerRung("Operand pin RTO", "review_timer_retentive",
                TimeSpan.FromSeconds(1), LadderTimerKind.RetentiveOnDelay);
            rto.TimerResetOperand = "stop_command";
            var scale = _ladderDocument.AddNumericOperationRung("Operand pins SCALE_X",
                LadderNumericOperationKind.Scale, "1", "2", "review_result", "3");
            scale.NumericInputDataType = LadderNumericDataType.Real;
            scale.NumericOutputDataType = LadderNumericDataType.DInt;
            var move = _ladderDocument.AddNumericOperationRung("Operand pins MOVE",
                LadderNumericOperationKind.Move, "11", "0", "review_result");
            var convert = _ladderDocument.AddNumericOperationRung("Operand pins CONV",
                LadderNumericOperationKind.Convert, "review_int", "0", "review_result");
            convert.NumericInputDataType = LadderNumericDataType.Int;
            convert.NumericOutputDataType = LadderNumericDataType.DInt;
            var normalize = _ladderDocument.AddNumericOperationRung("Operand pins NORM_X",
                LadderNumericOperationKind.Normalize, "1", "2", "review_real_result", "3");
            normalize.NumericInputDataType = LadderNumericDataType.DInt;
            normalize.NumericOutputDataType = LadderNumericDataType.Real;
            var exponentiate = _ladderDocument.AddNumericOperationRung("Operand pins EXPT",
                LadderNumericOperationKind.Exponentiate, "review_real", "2", "review_real_result");
            exponentiate.NumericDataType = LadderNumericDataType.DInt;
            var squareRoot = _ladderDocument.AddNumericOperationRung("Operand pins SQRT",
                LadderNumericOperationKind.SquareRoot, "9", "0", "review_real_result");
            squareRoot.NumericDataType = LadderNumericDataType.Real;
            var round = _ladderDocument.AddNumericOperationRung("Operand pins ROUND",
                LadderNumericOperationKind.Round, "1.5", "0", "review_result");
            round.NumericInputDataType = LadderNumericDataType.Real;
            round.NumericOutputDataType = LadderNumericDataType.DInt;

            // Build one rendered witness for every remaining native numeric
            // mnemonic that owns a TIA type selector. These are not compiler-
            // only proxies: each is selected, revealed, clicked, and committed
            // through its actual popup below.
            var selectorRungs = new Dictionary<LadderNumericOperationKind, string>
            {
                [LadderNumericOperationKind.Add] = math.Id,
                [LadderNumericOperationKind.SquareRoot] = squareRoot.Id,
                [LadderNumericOperationKind.Exponentiate] = exponentiate.Id,
                [LadderNumericOperationKind.Normalize] = normalize.Id,
                [LadderNumericOperationKind.Scale] = scale.Id,
                [LadderNumericOperationKind.Convert] = convert.Id,
                [LadderNumericOperationKind.Round] = round.Id,
            };
            foreach (var kind in new[]
                     {
                         LadderNumericOperationKind.Subtract, LadderNumericOperationKind.Multiply,
                         LadderNumericOperationKind.Divide, LadderNumericOperationKind.Modulo,
                     })
            {
                var rung = _ladderDocument.AddNumericOperationRung($"Type popup {kind}", kind,
                    "review_a", "review_operand", "review_result");
                rung.NumericDataType = LadderNumericDataType.DInt;
                selectorRungs[kind] = rung.Id;
            }
            foreach (var kind in new[] { LadderNumericOperationKind.Absolute, LadderNumericOperationKind.Negate })
            {
                var rung = _ladderDocument.AddNumericOperationRung($"Type popup {kind}", kind,
                    "review_operand", "0", "review_result");
                rung.NumericDataType = LadderNumericDataType.DInt;
                selectorRungs[kind] = rung.Id;
            }
            foreach (var kind in new[]
                     {
                         LadderNumericOperationKind.NaturalLog, LadderNumericOperationKind.Sine,
                         LadderNumericOperationKind.Cosine, LadderNumericOperationKind.Tangent,
                         LadderNumericOperationKind.ArcSine, LadderNumericOperationKind.ArcCosine,
                         LadderNumericOperationKind.ArcTangent,
                     })
            {
                var rung = _ladderDocument.AddNumericOperationRung($"Type popup {kind}", kind,
                    "review_real", "0", "review_real_result");
                rung.NumericDataType = LadderNumericDataType.Real;
                selectorRungs[kind] = rung.Id;
            }
            foreach (var kind in new[]
                     {
                         LadderNumericOperationKind.Truncate, LadderNumericOperationKind.Ceiling,
                         LadderNumericOperationKind.Floor,
                     })
            {
                var rung = _ladderDocument.AddNumericOperationRung($"Type popup {kind}", kind,
                    "review_real", "0", "review_result");
                rung.NumericInputDataType = LadderNumericDataType.Real;
                rung.NumericOutputDataType = LadderNumericDataType.DInt;
                selectorRungs[kind] = rung.Id;
            }
            var comparisonRung = _ladderDocument.AddRung("Operand pins comparison", "review_coil");
            var comparisonIndex = _ladderDocument.Rungs.IndexOf(comparisonRung);
            var comparison = _ladderDocument.AddComparison(comparisonIndex, 0, "review_compare",
                LadderCompareOperator.Equal, "8", LadderComparisonValueType.DInt);
            var selectorComparisons = new List<(string RungId, string NodeId, LadderCompareOperator Operator)>
            {
                (comparisonRung.Id, comparison.Id, LadderCompareOperator.Equal),
            };
            foreach (var comparisonOperator in new[]
                     {
                         LadderCompareOperator.NotEqual, LadderCompareOperator.GreaterThan,
                         LadderCompareOperator.GreaterOrEqual, LadderCompareOperator.LessThan,
                         LadderCompareOperator.LessOrEqual,
                     })
            {
                var rung = _ladderDocument.AddRung($"Type popup {comparisonOperator}", "review_coil");
                var rungIndex = _ladderDocument.Rungs.IndexOf(rung);
                var node = _ladderDocument.AddComparison(rungIndex, 0, "review_compare",
                    comparisonOperator, "8", LadderComparisonValueType.DInt);
                selectorComparisons.Add((rung.Id, node.Id, comparisonOperator));
            }
            // Property-form witnesses cover instruction families whose operands
            // are intentionally edited as a staged form rather than as one
            // inline pin. Keep these in the same physical-input sweep so a
            // single gate detects regressions in body hit-testing, selection,
            // scrolling, and form routing across the whole supported editor.
            var timerReset = _ladderDocument.AddTimerResetRung("Property form RT", "review_timer_retentive");
            var counterLoad = _ladderDocument.AddCounterRung("Property form LD", "review_counter_down", 6,
                kind: LadderCounterKind.CountDown, load: true);
            var counterReset = _ladderDocument.AddCounterRung("Property form R", "review_counter", 0,
                reset: true);
            _ladderDocument.AddCounterRung("Property form alternate counter owner", "review_counter_alt", 2);
            var returnRung = _ladderDocument.AddReturnRung("Property form RET");
            var jumpRung = _ladderDocument.AddJumpRung("Property form JMP", "JumpTarget");
            _ladderDocument.AddLabelRung("Property form JMP target", "JumpTarget");
            _ladderDocument.AddLabelRung("Property form alternate JMP target", "JumpTargetAlt");
            var labelRung = _ladderDocument.AddLabelRung("Property form LABEL", "EditableLabel");
            var callTarget = _ladderDocument.AddBlock("PhysicalPropertyTarget", LadderBlockType.Function);
            var callRung = _ladderDocument.AddCallRung("Property form CALL", callTarget.Id);
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            for (var frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var ladderScroll = canvas.GetParent();
            while (ladderScroll is not null && ladderScroll is not ScrollContainer) ladderScroll = ladderScroll.GetParent();
            if (ladderScroll is not ScrollContainer scroll) throw new InvalidOperationException("Ladder canvas has no scroll container.");

            var propertyPopup = (Control)host.FindChild("InstructionPropertiesPopup", true, false);
            var cancelProperties = (Button)host.FindChild("CancelInstructionProperties", true, false);
            var physicalPropertyResults = new List<string>();
            async System.Threading.Tasks.Task<bool> OpenPropertyForm(
                string name, int rungIndex, string expectedRow, string? nodeId = null, bool networkLabel = false,
                bool callEditor = false, Vector2? activationOffset = null, bool closeAfterOpen = true,
                bool recordResult = true)
            {
                if (nodeId is not null) canvas.SelectTreeNode(rungIndex, nodeId);
                else if (networkLabel) canvas.SelectNetworkLabel(rungIndex);
                else canvas.SelectElement(rungIndex, -1, -1, output: true);
                LadderEditorZoom.RevealSelection(canvas, scroll);
                for (var frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                // Type selectors are real child controls layered over the
                // center of compare/numeric bodies. For those families use a
                // documented clear portion of the instruction body rather
                // than accidentally clicking the selector itself.
                var localPoint = canvas.GetSelectionPosition() + (activationOffset ?? Vector2.Zero) * canvas.ZoomFactor;
                var viewportPoint = canvas.GetGlobalTransformWithCanvas() * localPoint;
                var visible = canvas.GetGlobalRect().HasPoint(viewportPoint)
                    && scroll.GetGlobalRect().HasPoint(viewportPoint)
                    && GetViewport().GetVisibleRect().HasPoint(viewportPoint);
                if (!visible)
                {
                    GD.Print($"PROPERTY_FORM_PHYSICAL_DETAIL name={name} stage=visibility pointer={viewportPoint} canvas={canvas.GetGlobalRect()} scroll={scroll.GetGlobalRect()}");
                    if (recordResult) physicalPropertyResults.Add($"{name}:False");
                    return false;
                }

                void Mouse(bool pressed, bool doubleClick = false) => GetViewport().PushInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0,
                    Pressed = pressed,
                    DoubleClick = doubleClick,
                    Position = viewportPoint,
                    GlobalPosition = viewportPoint,
                }, true);
                GetViewport().PushInput(new InputEventMouseMotion
                {
                    Position = viewportPoint,
                    GlobalPosition = viewportPoint,
                }, true);
                Mouse(true); Mouse(false);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Mouse(true, doubleClick: true); Mouse(false);
                for (var frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

                var opened = callEditor
                    ? _callAuthoring is { Visible: true }
                    : propertyPopup.Visible && ((Control)host.FindChild(expectedRow, true, false)).Visible;
                GD.Print($"PROPERTY_FORM_PHYSICAL_DETAIL name={name} opened={opened} popup={propertyPopup.Visible} row={expectedRow} pointer={viewportPoint}");
                if (closeAfterOpen)
                {
                    if (callEditor) _callAuthoring?.CancelEdit();
                    else if (propertyPopup.Visible) cancelProperties.EmitSignal(BaseButton.SignalName.Pressed);
                    for (var frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                }
                if (recordResult) physicalPropertyResults.Add($"{name}:{opened}");
                return opened;
            }

            var contactNodeId = _ladderDocument.Rungs[0].Branches[0].Contacts[0].Id;
            var allPhysicalPropertyForms = await OpenPropertyForm("contact", 0, "ContactPropertyRow", contactNodeId);
            comparisonIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == comparisonRung.Id);
            allPhysicalPropertyForms &= await OpenPropertyForm("compare", comparisonIndex, "ComparePropertyRow",
                comparison.Id, activationOffset: new Vector2(-60, 0));
            allPhysicalPropertyForms &= await OpenPropertyForm("coil", 0, "CoilPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("timer", _ladderDocument.Rungs.FindIndex(item => item.Id == ton.Id), "TimerPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("timer-reset", _ladderDocument.Rungs.FindIndex(item => item.Id == timerReset.Id), "TimerPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("counter", _ladderDocument.Rungs.FindIndex(item => item.Id == counter.Id), "CounterPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("counter-load", _ladderDocument.Rungs.FindIndex(item => item.Id == counterLoad.Id), "CounterPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("counter-reset", _ladderDocument.Rungs.FindIndex(item => item.Id == counterReset.Id), "CounterPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("numeric", _ladderDocument.Rungs.FindIndex(item => item.Id == math.Id),
                "NumericPropertyRow", activationOffset: new Vector2(-50, 0));
            allPhysicalPropertyForms &= await OpenPropertyForm("return", _ladderDocument.Rungs.FindIndex(item => item.Id == returnRung.Id), "ReturnPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("jump", _ladderDocument.Rungs.FindIndex(item => item.Id == jumpRung.Id), "JumpLabelPropertyRow");
            allPhysicalPropertyForms &= await OpenPropertyForm("label", _ladderDocument.Rungs.FindIndex(item => item.Id == labelRung.Id), "JumpLabelPropertyRow", networkLabel: true);
            allPhysicalPropertyForms &= await OpenPropertyForm("call", _ladderDocument.Rungs.FindIndex(item => item.Id == callRung.Id), "CallAuthoring", callEditor: true);

            // One fail-closed transaction matrix owns the common form contract.
            // Every non-CALL family must reject invalid input without touching
            // the live document, cancel exactly, commit once, preserve the
            // selected object's identity, undo/redo exactly, and reopen the
            // committed value. CALL uses its dedicated detached-form gate.
            var applyProperties = (Button)host.FindChild("ApplyInstructionProperties", true, false);
            var propertyStatus = (Label)host.FindChild("InstructionPropertyStatus", true, false);
            var undoButton = (Button)host.FindChild("LadderUndo", true, false);
            var redoButton = (Button)host.FindChild("LadderRedo", true, false);
            var contactSelector = (OptionButton)host.FindChild("ContactTagSelector", true, false);
            var compareRightField = (LineEdit)host.FindChild("CompareRightOperand", true, false);
            var coilSelector = (OptionButton)host.FindChild("CoilTagSelector", true, false);
            var timerSelector = (OptionButton)host.FindChild("TimerTagSelector", true, false);
            var timerPresetField = (LineEdit)host.FindChild("TimerPresetOperand", true, false);
            var counterSelector = (OptionButton)host.FindChild("CounterTagSelector", true, false);
            var counterPresetField = (SpinBox)host.FindChild("CounterPreset", true, false);
            var counterResetField = (LineEdit)host.FindChild("CounterResetInput", true, false);
            var numericSourceAField = (LineEdit)host.FindChild("NumericSourceA", true, false);
            var returnKindSelector = (OptionButton)host.FindChild("ReturnValueSelector", true, false);
            var returnOperandField = (LineEdit)host.FindChild("ReturnValueOperand", true, false);
            var labelField = (LineEdit)host.FindChild("ProgramControlLabel", true, false);
            var propertyTransactionResults = new List<string>();

            static string OptionText(OptionButton selector) => selector.Selected >= 0
                ? selector.GetItemText(selector.Selected) : string.Empty;
            static void SelectOption(OptionButton selector, string text)
            {
                var index = Enumerable.Range(0, selector.ItemCount)
                    .FirstOrDefault(item => selector.GetItemText(item) == text, -1);
                if (index < 0) { selector.AddItem(text); index = selector.ItemCount - 1; }
                selector.Select(index);
                selector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
            }
            static void SetText(LineEdit field, string value)
            {
                field.Text = value;
                field.EmitSignal(LineEdit.SignalName.TextChanged, value);
            }
            static void SetNumber(SpinBox field, double value)
            {
                field.Value = value;
                field.EmitSignal(Godot.Range.SignalName.ValueChanged, value);
            }
            int RungIndex(string id) => _ladderDocument.Rungs.FindIndex(rung => rung.Id == id);
            EditableRung? CurrentRung(string id) => _ladderDocument.Rungs.FirstOrDefault(rung => rung.Id == id);
            string NodeIdentity(string rungId, string nodeId) => CurrentRung(rungId) is { } rung
                ? EditableLadderTree.Find(rung.Root, nodeId)?.Id ?? string.Empty : string.Empty;
            string TerminalIdentity(string rungId) => CurrentRung(rungId)?.TerminalActionId ?? string.Empty;
            string LabelIdentity(string rungId) => CurrentRung(rungId)?.NetworkLabelId ?? string.Empty;
            async System.Threading.Tasks.Task Frames(int count = 2)
            {
                for (var frame = 0; frame < count; frame++)
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            async System.Threading.Tasks.Task<bool> VerifyPropertyTransaction(
                string name,
                System.Func<System.Threading.Tasks.Task<bool>> open,
                System.Action stageInvalid,
                System.Action stageValid,
                System.Func<bool> appliedState,
                System.Func<bool> reopenedState,
                System.Func<string> identity)
            {
                _ladderHistory.Clear();
                var baselineSnapshot = _ladderDocument.CaptureSnapshot();
                var baselineJson = LadderEditorProjectJson.Save(_ladderDocument);
                var baselineIdentity = identity();
                var invalidOpened = false;
                var invalidRejected = false;
                var cancelExact = false;
                var validOpened = false;
                var applied = false;
                var stableIdentity = false;
                var undoExact = false;
                var redoExact = false;
                var reopened = false;
                var reopenCancelExact = false;
                var cleanupExact = false;
                try
                {
                    invalidOpened = await open();
                    if (invalidOpened)
                    {
                        stageInvalid();
                        applyProperties.EmitSignal(BaseButton.SignalName.Pressed);
                        await Frames();
                        invalidRejected = propertyPopup.Visible
                            && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                            && !_ladderHistory.CanUndo
                            && !string.IsNullOrWhiteSpace(propertyStatus.Text);
                        cancelProperties.EmitSignal(BaseButton.SignalName.Pressed);
                        await Frames();
                        cancelExact = !propertyPopup.Visible
                            && LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                            && !_ladderHistory.CanUndo && identity() == baselineIdentity;
                    }

                    validOpened = await open();
                    if (validOpened)
                    {
                        stageValid();
                        applyProperties.EmitSignal(BaseButton.SignalName.Pressed);
                        await Frames();
                        var appliedJson = LadderEditorProjectJson.Save(_ladderDocument);
                        applied = !propertyPopup.Visible && appliedJson != baselineJson
                            && appliedState() && _ladderHistory.CanUndo;
                        stableIdentity = identity() == baselineIdentity;

                        if (applied)
                        {
                            undoButton.EmitSignal(BaseButton.SignalName.Pressed);
                            await Frames();
                            undoExact = LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                                && !_ladderHistory.CanUndo && identity() == baselineIdentity;

                            redoButton.EmitSignal(BaseButton.SignalName.Pressed);
                            await Frames();
                            redoExact = LadderEditorProjectJson.Save(_ladderDocument) == appliedJson
                                && appliedState() && identity() == baselineIdentity;

                            reopened = await open() && reopenedState();
                            if (propertyPopup.Visible)
                            {
                                cancelProperties.EmitSignal(BaseButton.SignalName.Pressed);
                                await Frames();
                            }
                            reopenCancelExact = reopened && LadderEditorProjectJson.Save(_ladderDocument) == appliedJson
                                && identity() == baselineIdentity;

                            undoButton.EmitSignal(BaseButton.SignalName.Pressed);
                            await Frames();
                            cleanupExact = LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                                && !_ladderHistory.CanUndo && identity() == baselineIdentity;
                        }
                        else if (propertyPopup.Visible)
                        {
                            cancelProperties.EmitSignal(BaseButton.SignalName.Pressed);
                            await Frames();
                        }
                    }
                }
                finally
                {
                    if (propertyPopup.Visible) cancelProperties.EmitSignal(BaseButton.SignalName.Pressed);
                    _pendingEditorDialog?.Hide();
                    _ladderDocument.RestoreSnapshot(baselineSnapshot);
                    _ladderHistory.Clear();
                    foreach (var refresh in _ladderEditorRefreshers) refresh();
                    await Frames();
                }
                var passed = invalidOpened && invalidRejected && cancelExact && validOpened && applied
                    && stableIdentity && undoExact && redoExact && reopened && reopenCancelExact && cleanupExact;
                propertyTransactionResults.Add($"{name}:{passed}");
                GD.Print($"PROPERTY_TRANSACTION_DETAIL name={name} pass={passed} invalidOpen={invalidOpened} "
                    + $"invalidReject={invalidRejected} cancel={cancelExact} validOpen={validOpened} apply={applied} "
                    + $"stableId={stableIdentity} undo={undoExact} redo={redoExact} reopen={reopened} "
                    + $"reopenCancel={reopenCancelExact} cleanup={cleanupExact} status={propertyStatus.Text}");
                return passed;
            }

            System.Func<System.Threading.Tasks.Task<bool>> OpenForm(
                string name, string rungId, string row, string? nodeId = null,
                bool networkLabel = false, Vector2? offset = null) =>
                () => OpenPropertyForm(name, RungIndex(rungId), row, nodeId, networkLabel,
                    activationOffset: offset, closeAfterOpen: false, recordResult: false);

            var allPropertyTransactions = await VerifyPropertyTransaction(
                "contact", OpenForm("contact-transaction", _ladderDocument.Rungs[0].Id, "ContactPropertyRow", contactNodeId),
                () => SelectOption(contactSelector, "missing_bool"),
                () => SelectOption(contactSelector, "review_reset"),
                () => EditableLadderTree.Find(CurrentRung(_ladderDocument.Rungs[0].Id)!.Root, contactNodeId)?.Variable == "review_reset",
                () => OptionText(contactSelector) == "review_reset",
                () => NodeIdentity(_ladderDocument.Rungs[0].Id, contactNodeId));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "compare", OpenForm("compare-transaction", comparisonRung.Id, "ComparePropertyRow", comparison.Id, offset: new Vector2(-60, 0)),
                () => SetText(compareRightField, "missing_number"),
                () => SetText(compareRightField, "9"),
                () => EditableLadderTree.Find(CurrentRung(comparisonRung.Id)!.Root, comparison.Id)?.RightOperand == "9",
                () => compareRightField.Text == "9",
                () => NodeIdentity(comparisonRung.Id, comparison.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "coil", OpenForm("coil-transaction", _ladderDocument.Rungs[0].Id, "CoilPropertyRow"),
                () => SelectOption(coilSelector, "missing_bool"),
                () => SelectOption(coilSelector, "review_coil"),
                () => CurrentRung(_ladderDocument.Rungs[0].Id)?.CoilVariable == "review_coil",
                () => OptionText(coilSelector) == "review_coil",
                () => TerminalIdentity(_ladderDocument.Rungs[0].Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "timer", OpenForm("timer-transaction", ton.Id, "TimerPropertyRow"),
                () => SetText(timerPresetField, "not_time"),
                () => SetText(timerPresetField, "T#2s"),
                () => CurrentRung(ton.Id) is { TimerPresetOperand: "T#2s" } timer
                    && timer.TimerPreset == TimeSpan.FromSeconds(2),
                () => timerPresetField.Text == "T#2s",
                () => TerminalIdentity(ton.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "timer-reset", OpenForm("timer-reset-transaction", timerReset.Id, "TimerPropertyRow"),
                () => SelectOption(timerSelector, "missing_timer"),
                () => SelectOption(timerSelector, "review_timer"),
                () => CurrentRung(timerReset.Id)?.TimerVariable == "review_timer",
                () => OptionText(timerSelector) == "review_timer",
                () => TerminalIdentity(timerReset.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "counter", OpenForm("counter-transaction", counter.Id, "CounterPropertyRow"),
                () => SetText(counterResetField, "missing_bool"),
                () => { SetText(counterResetField, "review_reset"); SetNumber(counterPresetField, 5); },
                () => CurrentRung(counter.Id) is { CounterPreset: 5, CounterResetInput: "review_reset" },
                () => counterPresetField.Value == 5 && counterResetField.Text == "review_reset",
                () => TerminalIdentity(counter.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "counter-load", OpenForm("counter-load-transaction", counterLoad.Id, "CounterPropertyRow"),
                () => SelectOption(counterSelector, "missing_counter"),
                () => SetNumber(counterPresetField, 8),
                () => CurrentRung(counterLoad.Id)?.CounterPreset == 8,
                () => counterPresetField.Value == 8,
                () => TerminalIdentity(counterLoad.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "counter-reset", OpenForm("counter-reset-transaction", counterReset.Id, "CounterPropertyRow"),
                () => SelectOption(counterSelector, "missing_counter"),
                () => SelectOption(counterSelector, "review_counter_alt"),
                () => CurrentRung(counterReset.Id)?.CounterVariable == "review_counter_alt",
                () => OptionText(counterSelector) == "review_counter_alt",
                () => TerminalIdentity(counterReset.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "numeric", OpenForm("numeric-transaction", math.Id, "NumericPropertyRow", offset: new Vector2(-50, 0)),
                () => SetText(numericSourceAField, "missing_number"),
                () => SetText(numericSourceAField, "review_a"),
                () => CurrentRung(math.Id)?.NumericSourceA == "review_a",
                () => numericSourceAField.Text == "review_a",
                () => TerminalIdentity(math.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "return", OpenForm("return-transaction", returnRung.Id, "ReturnPropertyRow"),
                () => { SelectOption(returnKindSelector, "Value"); SetText(returnOperandField, "missing_bool"); },
                () => { SelectOption(returnKindSelector, "Value"); SetText(returnOperandField, "review_reset"); },
                () => CurrentRung(returnRung.Id) is { ReturnValueKind: LadderReturnValueKind.Operand, ReturnValueOperand: "review_reset" },
                () => returnKindSelector.Selected == (int)LadderReturnValueKind.Operand && returnOperandField.Text == "review_reset",
                () => TerminalIdentity(returnRung.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "jump", OpenForm("jump-transaction", jumpRung.Id, "JumpLabelPropertyRow"),
                () => SetText(labelField, "1bad"),
                () => SetText(labelField, "JumpTargetAlt"),
                () => CurrentRung(jumpRung.Id)?.ProgramControlLabel == "JumpTargetAlt",
                () => labelField.Text == "JumpTargetAlt",
                () => TerminalIdentity(jumpRung.Id));
            allPropertyTransactions &= await VerifyPropertyTransaction(
                "label", OpenForm("label-transaction", labelRung.Id, "JumpLabelPropertyRow", networkLabel: true),
                () => SetText(labelField, "1bad"),
                () => SetText(labelField, "EditableLabelNew"),
                () => CurrentRung(labelRung.Id) is { NetworkLabelName: "EditableLabelNew", ProgramControlLabel: "EditableLabelNew" },
                () => labelField.Text == "EditableLabelNew",
                () => LabelIdentity(labelRung.Id));

            GD.Print($"PROPERTY_TRANSACTION_MATRIX_VERIFY {(allPropertyTransactions ? "PASS" : "FAIL")} "
                + $"cases=[{string.Join(',', propertyTransactionResults)}]");

            // Exercise every visible Siemens palette mutation button through
            // the viewport. The semantic/render sweep covers all catalog keys;
            // this matrix proves the actual button routes, one-entry history,
            // and byte-exact Undo/Redo without conflating local-only commands.
            var instructionPalette = (TabContainer)host.FindChild("InstructionPalette", true, false);
            var paletteRungId = _ladderDocument.Rungs[0].Id;
            var baselineNetworkCount = _ladderDocument.Rungs.Count;
            EditableRung PaletteRung() => _ladderDocument.Rungs.First(rung => rung.Id == paletteRungId);
            int PaletteRungIndex() => _ladderDocument.Rungs.FindIndex(rung => rung.Id == paletteRungId);
            void SelectPaletteInsertion()
            {
                var index = PaletteRungIndex();
                canvas.SelectInsertionPoint(index, 0, _ladderDocument.Rungs[index].Branches[0].Contacts.Count);
            }
            void SelectPaletteOutput() => canvas.SelectElement(PaletteRungIndex(), -1, -1, output: true);
            void SelectPaletteContact() => canvas.SelectElement(PaletteRungIndex(), 0, 0);
            void SelectPaletteNetwork() => canvas.SelectRung(PaletteRungIndex());
            int PaletteContactCount() => PaletteRung().Branches.Sum(branch => branch.Contacts.Count);

            async System.Threading.Tasks.Task<bool> ClickPaletteButton(string buttonName)
            {
                var button = host.FindChild(buttonName, true, false) as Button;
                if (button is null) return false;
                var page = button.GetParent();
                while (page?.GetParent() != instructionPalette) page = page?.GetParent();
                if (page is null) return false;
                instructionPalette.CurrentTab = page.GetIndex();
                await Frames(3);
                var rect = button.GetGlobalRect();
                if (!button.IsVisibleInTree() || button.Disabled || rect.Size.X <= 0 || rect.Size.Y <= 0
                    || !GetViewport().GetVisibleRect().HasPoint(rect.GetCenter())) return false;
                var at = rect.GetCenter();
                GetViewport().PushInput(new InputEventMouseMotion { Position = at, GlobalPosition = at }, true);
                GetViewport().PushInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left, ButtonMask = MouseButtonMask.Left,
                    Pressed = true, Position = at, GlobalPosition = at,
                }, true);
                GetViewport().PushInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left, ButtonMask = (MouseButtonMask)0,
                    Pressed = false, Position = at, GlobalPosition = at,
                }, true);
                await Frames(3);
                return true;
            }

            var paletteMutationResults = new List<string>();
            async System.Threading.Tasks.Task<bool> VerifyPaletteMutation(
                string name, string buttonName, Action prepare, Func<bool> mutated)
            {
                _ladderHistory.Clear();
                var baselineSnapshot = _ladderDocument.CaptureSnapshot();
                var baselineJson = LadderEditorProjectJson.Save(_ladderDocument);
                var baselineRungId = PaletteRung().Id;
                var clicked = false;
                var applied = false;
                var undoExact = false;
                var redoExact = false;
                var cleanupExact = false;
                try
                {
                    prepare();
                    clicked = await ClickPaletteButton(buttonName);
                    var appliedJson = LadderEditorProjectJson.Save(_ladderDocument);
                    applied = clicked && appliedJson != baselineJson && mutated()
                        && _ladderHistory.CanUndo && PaletteRung().Id == baselineRungId;
                    if (applied)
                    {
                        undoButton.EmitSignal(BaseButton.SignalName.Pressed);
                        await Frames();
                        undoExact = LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                            && !_ladderHistory.CanUndo && PaletteRung().Id == baselineRungId;
                        redoButton.EmitSignal(BaseButton.SignalName.Pressed);
                        await Frames();
                        redoExact = LadderEditorProjectJson.Save(_ladderDocument) == appliedJson
                            && mutated() && PaletteRung().Id == baselineRungId;
                        undoButton.EmitSignal(BaseButton.SignalName.Pressed);
                        await Frames();
                        cleanupExact = LadderEditorProjectJson.Save(_ladderDocument) == baselineJson
                            && !_ladderHistory.CanUndo;
                    }
                }
                finally
                {
                    _ladderDocument.RestoreSnapshot(baselineSnapshot);
                    _ladderHistory.Clear();
                    foreach (var refresh in _ladderEditorRefreshers) refresh();
                    await Frames();
                }
                var passed = clicked && applied && undoExact && redoExact && cleanupExact;
                paletteMutationResults.Add($"{name}:{passed}");
                GD.Print($"TIA_PALETTE_MUTATION_DETAIL name={name} button={buttonName} pass={passed} "
                    + $"click={clicked} apply={applied} undo={undoExact} redo={redoExact} cleanup={cleanupExact}");
                return passed;
            }

            var baselineContactCount = PaletteContactCount();
            var allPaletteMutations = await VerifyPaletteMutation("no", "AddNormallyOpen", SelectPaletteInsertion,
                () => PaletteContactCount() == baselineContactCount + 1
                    && PaletteRung().Branches[0].Contacts[^1].EdgeMode == LadderEdgeMode.None
                    && !PaletteRung().Branches[0].Contacts[^1].NormallyClosed);
            allPaletteMutations &= await VerifyPaletteMutation("nc", "AddNormallyClosed", SelectPaletteInsertion,
                () => PaletteContactCount() == baselineContactCount + 1
                    && PaletteRung().Branches[0].Contacts[^1].NormallyClosed);
            allPaletteMutations &= await VerifyPaletteMutation("positive-edge", "AddRisingEdge", SelectPaletteInsertion,
                () => PaletteContactCount() == baselineContactCount + 1
                    && PaletteRung().Branches[0].Contacts[^1].EdgeMode == LadderEdgeMode.Rising);
            allPaletteMutations &= await VerifyPaletteMutation("negative-edge", "AddFallingEdge", SelectPaletteInsertion,
                () => PaletteContactCount() == baselineContactCount + 1
                    && PaletteRung().Branches[0].Contacts[^1].EdgeMode == LadderEdgeMode.Falling);
            allPaletteMutations &= await VerifyPaletteMutation("compare", "AddComparison", SelectPaletteInsertion,
                () => PaletteContactCount() == baselineContactCount + 1
                    && EditableLadderTree.Visit(PaletteRung().Root).Any(node => node.Kind == LadderNodeKind.Compare));
            allPaletteMutations &= await VerifyPaletteMutation("branch", "AddBranch", SelectPaletteInsertion,
                () => EditableLadderTree.Visit(PaletteRung().Root).Any(node => node.Kind == LadderNodeKind.Parallel));
            allPaletteMutations &= await VerifyPaletteMutation("network", "AddRung", SelectPaletteNetwork,
                () => _ladderDocument.Rungs.Count == baselineNetworkCount + 1);
            allPaletteMutations &= await VerifyPaletteMutation("delete", "RemoveInstruction", SelectPaletteContact,
                () => PaletteContactCount() == baselineContactCount - 1);
            allPaletteMutations &= await VerifyPaletteMutation("empty-box", "AddEmptyInstructionBox", SelectPaletteOutput,
                () => PaletteRung() is { HasEmptyInstructionPlaceholder: true, HasTerminalAction: false });
            allPaletteMutations &= await VerifyPaletteMutation("set", "AddSetLatch", SelectPaletteOutput,
                () => PaletteRung().CoilMode == LadderCoilMode.Set && PaletteRung().HasTerminalAction);
            allPaletteMutations &= await VerifyPaletteMutation("reset", "AddResetUnlatch", SelectPaletteOutput,
                () => PaletteRung().CoilMode == LadderCoilMode.Reset && PaletteRung().HasTerminalAction);

            foreach (var timerCase in new[]
                     {
                         (Name: "ton", Button: "AddTimerOnDelay", Kind: LadderTimerKind.OnDelay, Reset: false),
                         (Name: "tof", Button: "AddTimerOffDelay", Kind: LadderTimerKind.OffDelay, Reset: false),
                         (Name: "tp", Button: "AddPulseTimer", Kind: LadderTimerKind.Pulse, Reset: false),
                         (Name: "tonr", Button: "AddRetentiveTimer", Kind: LadderTimerKind.RetentiveOnDelay, Reset: false),
                         (Name: "rt", Button: "AddTimerReset", Kind: LadderTimerKind.OnDelay, Reset: true),
                     })
                allPaletteMutations &= await VerifyPaletteMutation(timerCase.Name, timerCase.Button, SelectPaletteOutput,
                    () => timerCase.Reset
                        ? PaletteRung().IsTimerReset && !PaletteRung().IsTimer
                        : PaletteRung().IsTimer && !PaletteRung().IsTimerReset
                            && PaletteRung().TimerKind == timerCase.Kind);

            foreach (var counterCase in new[]
                     {
                         (Name: "ctu", Button: "AddCountUp", Kind: LadderCounterKind.CountUp),
                         (Name: "ctd", Button: "AddCountDown", Kind: LadderCounterKind.CountDown),
                     })
                allPaletteMutations &= await VerifyPaletteMutation(counterCase.Name, counterCase.Button, SelectPaletteOutput,
                    () => PaletteRung().IsCounter && PaletteRung().CounterKind == counterCase.Kind);

            var localCounterLoad = (Button)host.FindChild("AddCounterLoad", true, false);
            var localCounterReset = (Button)host.FindChild("AddCounterReset", true, false);
            var localFormatText = (Button)host.FindChild("AddFormatText", true, false);
            var localCategorySeparated = localCounterLoad.GetParent().Name == "RungProof local (not Siemens)"
                && localCounterReset.GetParent() == localCounterLoad.GetParent()
                && localFormatText.GetParent() == localCounterLoad.GetParent();
            allPaletteMutations &= localCategorySeparated;
            allPaletteMutations &= await VerifyPaletteMutation("local-counter-load", "AddCounterLoad", SelectPaletteOutput,
                () => PaletteRung().IsCounterLoad && !PaletteRung().IsCounter);
            allPaletteMutations &= await VerifyPaletteMutation("local-counter-reset", "AddCounterReset", SelectPaletteOutput,
                () => PaletteRung().IsCounterReset && !PaletteRung().IsCounter);
            allPaletteMutations &= await VerifyPaletteMutation("local-format-text", "AddFormatText", SelectPaletteOutput,
                () => PaletteRung().IsNumericOperation
                    && PaletteRung().NumericOperationKind == LadderNumericOperationKind.FormatText);

            foreach (var numericCase in new[]
                     {
                         ("move", "AddMove", LadderNumericOperationKind.Move),
                         ("add", "AddAdd", LadderNumericOperationKind.Add),
                         ("subtract", "AddSubtract", LadderNumericOperationKind.Subtract),
                         ("multiply", "AddMultiply", LadderNumericOperationKind.Multiply),
                         ("divide", "AddDivide", LadderNumericOperationKind.Divide),
                         ("modulo", "AddModulo", LadderNumericOperationKind.Modulo),
                         ("absolute", "AddAbsolute", LadderNumericOperationKind.Absolute),
                         ("negate", "AddNegate", LadderNumericOperationKind.Negate),
                         ("square-root", "AddSquareRoot", LadderNumericOperationKind.SquareRoot),
                         ("exponentiate", "AddExponentiate", LadderNumericOperationKind.Exponentiate),
                         ("natural-log", "AddNaturalLog", LadderNumericOperationKind.NaturalLog),
                         ("sine", "AddSine", LadderNumericOperationKind.Sine),
                         ("cosine", "AddCosine", LadderNumericOperationKind.Cosine),
                         ("tangent", "AddTangent", LadderNumericOperationKind.Tangent),
                         ("arc-sine", "AddArcSine", LadderNumericOperationKind.ArcSine),
                         ("arc-cosine", "AddArcCosine", LadderNumericOperationKind.ArcCosine),
                         ("arc-tangent", "AddArcTangent", LadderNumericOperationKind.ArcTangent),
                         ("truncate", "AddTruncate", LadderNumericOperationKind.Truncate),
                         ("convert", "AddConvert", LadderNumericOperationKind.Convert),
                         ("round", "AddRound", LadderNumericOperationKind.Round),
                         ("ceiling", "AddCeiling", LadderNumericOperationKind.Ceiling),
                         ("floor", "AddFloor", LadderNumericOperationKind.Floor),
                         ("normalize", "AddNormalize", LadderNumericOperationKind.Normalize),
                         ("scale", "AddScale", LadderNumericOperationKind.Scale),
                     })
                allPaletteMutations &= await VerifyPaletteMutation(numericCase.Item1, numericCase.Item2, SelectPaletteOutput,
                    () => PaletteRung().IsNumericOperation && PaletteRung().NumericOperationKind == numericCase.Item3);

            allPaletteMutations &= await VerifyPaletteMutation("return", "AddReturn", SelectPaletteOutput,
                () => PaletteRung().IsReturn);
            allPaletteMutations &= await VerifyPaletteMutation("jump", "AddJump", SelectPaletteOutput,
                () => PaletteRung().IsJump && TiaProgramControlLabel.IsValid(PaletteRung().ProgramControlLabel));
            allPaletteMutations &= await VerifyPaletteMutation("label", "AddLabel", SelectPaletteOutput,
                () => PaletteRung().IsLabel && TiaProgramControlLabel.IsValid(PaletteRung().NetworkLabelName));
            GD.Print($"TIA_PALETTE_MUTATION_MATRIX_VERIFY {(allPaletteMutations ? "PASS" : "FAIL")} "
                + $"cases={paletteMutationResults.Count} localSeparated={localCategorySeparated} "
                + $"results=[{string.Join(',', paletteMutationResults)}]");

            async System.Threading.Tasks.Task<bool> EditPin(int rungIndex, string target, string originalText,
                string replacement, string? nodeId = null)
            {
                if (nodeId is null) canvas.SelectElement(rungIndex, -1, -1, output: true);
                else canvas.SelectTreeNode(rungIndex, nodeId);
                LadderEditorZoom.RevealSelection(canvas, scroll);
                for (var frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var pinBounds = canvas.GetOperandEditorBounds(originalText, target);
                scroll.ScrollHorizontal = LadderEditorZoomGeometry.Reveal(scroll.ScrollHorizontal,
                    scroll.Size.X, pinBounds.Position.X, pinBounds.End.X);
                scroll.ScrollVertical = LadderEditorZoomGeometry.Reveal(scroll.ScrollVertical,
                    scroll.Size.Y, pinBounds.Position.Y, pinBounds.End.Y);
                for (var frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var pinPoint = pinBounds.Position + pinBounds.Size / 2;
                var viewportPoint = canvas.GetGlobalTransformWithCanvas() * pinPoint;
                if (!canvas.GetGlobalRect().HasPoint(viewportPoint)
                    || !scroll.GetGlobalRect().HasPoint(viewportPoint)
                    || !GetViewport().GetVisibleRect().HasPoint(viewportPoint))
                {
                    GD.Print($"OPERAND_PIN_PHYSICAL_DETAIL target={target} stage=visibility local={pinPoint} viewport={viewportPoint} canvas={canvas.GetGlobalRect()} scroll={scroll.GetGlobalRect()}");
                    return false;
                }
                void Button(bool pressed, bool doubleClick = false) => GetViewport().PushInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0,
                    Pressed = pressed,
                    DoubleClick = doubleClick,
                    Position = viewportPoint,
                    GlobalPosition = viewportPoint,
                }, true);
                GetViewport().PushInput(new InputEventMouseMotion
                {
                    Position = viewportPoint,
                    GlobalPosition = viewportPoint,
                }, true);
                Button(true);
                Button(false);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Button(true, doubleClick: true);
                Button(false);
                var pinEditor = (LadderOperandEditor)host.FindChild("InlineOperandEditor", true, false);
                var pinText = (LineEdit)pinEditor.FindChild("OperandText", true, false);
                for (var frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var openedAtPin = pinEditor.Visible && pinText.Text == originalText
                    && Math.Abs(pinEditor.Position.X - pinBounds.Position.X) <= 1
                    && Math.Abs(pinEditor.Position.Y - pinBounds.Position.Y) <= 1;
                if (!openedAtPin)
                {
                    GD.Print($"OPERAND_PIN_PHYSICAL_DETAIL target={target} stage=open visible={pinEditor.Visible} text={pinText.Text} expected={originalText} editor={pinEditor.Position} expectedPosition={pinBounds.Position} pointer={viewportPoint}");
                    pinEditor.CancelEdit();
                    return false;
                }
                pinText.Text = replacement;
                if (!pinEditor.TryApply(false))
                {
                    var status = pinEditor.FindChild("OperandStatus", true, false) as Label;
                    GD.Print($"OPERAND_PIN_PHYSICAL_DETAIL target={target} stage=apply replacement={replacement} status={status?.Text}");
                    return false;
                }
                GD.Print($"OPERAND_PIN_PHYSICAL_DETAIL target={target} stage=pass pointer={viewportPoint} replacement={replacement}");
                return true;
            }

            var selectorSearchKeys = new Dictionary<string, char>();
            async System.Threading.Tasks.Task<bool> ChooseAdjacentType(int rungIndex, string selectorName,
                System.Func<int, bool> applied, string? nodeId = null)
            {
                if (nodeId is null) canvas.SelectElement(rungIndex, -1, -1, output: true);
                else canvas.SelectTreeNode(rungIndex, nodeId);
                LadderEditorZoom.RevealSelection(canvas, scroll);
                for (var frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var selector = canvas.GetNode<OptionButton>(selectorName);
                var selectorRect = selector.GetGlobalRect();
                if (!selector.IsVisibleInTree() || !scroll.GetGlobalRect().HasPoint(selectorRect.GetCenter())
                    || !GetViewport().GetVisibleRect().HasPoint(selectorRect.GetCenter()))
                {
                    GD.Print($"TYPE_SELECTOR_PHYSICAL_DETAIL selector={selectorName} stage=visibility visible={selector.IsVisibleInTree()} rect={selectorRect}");
                    return false;
                }
                void Mouse(bool pressed) => GetViewport().PushInput(new InputEventMouseButton
                {
                    ButtonIndex = MouseButton.Left,
                    ButtonMask = pressed ? MouseButtonMask.Left : (MouseButtonMask)0,
                    Pressed = pressed,
                    Position = selectorRect.GetCenter(),
                    GlobalPosition = selectorRect.GetCenter(),
                }, true);
                Mouse(true); Mouse(false);
                for (var frame = 0; frame < 2; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var popup = selector.GetPopup();
                if (!popup.Visible)
                {
                    GD.Print($"TYPE_SELECTOR_PHYSICAL_DETAIL selector={selectorName} stage=open selected={selector.Selected}");
                    return false;
                }
                var before = selector.Selected;
                // Prefer a different row so the document mutation is observable. Fixed-REAL
                // inputs have only ???/REAL, so re-select REAL there and prove the popup press.
                var desired = before + 1 < selector.ItemCount ? before + 1 : before;
                long pressedIndex = -1;
                void RecordPressed(long index) => pressedIndex = index;
                popup.IndexPressed += RecordPressed;
                void KeyEvent(Key key, bool pressed, uint unicode = 0) => GetViewport().PushInput(new InputEventKey
                {
                    Keycode = key,
                    PhysicalKeycode = key,
                    Unicode = unicode,
                    Pressed = pressed,
                }, true);
                var search = char.ToUpperInvariant(popup.GetItemText(desired)[0]);
                if (selectorSearchKeys.TryGetValue(selectorName, out var previousSearch)
                    && previousSearch != search)
                {
                    var resetMilliseconds = ProjectSettings.GetSetting(
                        "gui/timers/incremental_search_max_interval_msec").AsInt64();
                    await ToSignal(GetTree().CreateTimer((Math.Max(0, resetMilliseconds) + 50) / 1000d),
                        SceneTreeTimer.SignalName.Timeout);
                }
                selectorSearchKeys[selectorName] = search;
                KeyEvent((Key)search, true, search); KeyEvent((Key)search, false, search);
                KeyEvent(Key.Enter, true); KeyEvent(Key.Enter, false);
                for (var frame = 0; frame < 3; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                popup.IndexPressed -= RecordPressed;
                var passed = !popup.Visible && pressedIndex == desired
                    && selector.Selected == desired && applied(desired);
                GD.Print($"TYPE_SELECTOR_PHYSICAL_DETAIL selector={selectorName} stage={(passed ? "pass" : "apply")} before={before} desired={desired} pressed={pressedIndex} selected={selector.Selected} popup={popup.Visible}");
                if (popup.Visible) popup.Hide();
                return passed;
            }

            var counterIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == counter.Id);
            var counterPinEdited = await EditPin(counterIndex, "terminal-counter-r", "stop_command", "review_reset")
                && _ladderDocument.Rungs.First(item => item.Id == counter.Id).CounterResetInput == "review_reset";
            var numericIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == math.Id);
            var numericPinEdited = await EditPin(numericIndex, "terminal-numeric-b", "3", "review_operand")
                && _ladderDocument.Rungs.First(item => item.Id == math.Id).NumericSourceB == "review_operand";
            var coilRung = _ladderDocument.Rungs[0];
            var coilEdited = await EditPin(0, "terminal-output", coilRung.CoilVariable, "review_coil")
                && _ladderDocument.Rungs.First(item => item.Id == coilRung.Id).CoilVariable == "review_coil";
            counterIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == counter.Id);
            var counterPresetEdited = await EditPin(counterIndex, "terminal-counter-pv", "4", "5")
                && _ladderDocument.Rungs.First(item => item.Id == counter.Id).CounterPreset == 5;
            var downCounterIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == downCounter.Id);
            var counterLoadEdited = await EditPin(downCounterIndex, "terminal-counter-load", "stop_command", "review_reset")
                && _ladderDocument.Rungs.First(item => item.Id == downCounter.Id).CounterLoadInput == "review_reset";
            downCounterIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == downCounter.Id);
            var downCounterPresetEdited = await EditPin(downCounterIndex, "terminal-counter-pv", "6", "7")
                && _ladderDocument.Rungs.First(item => item.Id == downCounter.Id).CounterPreset == 7;
            var tonIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == ton.Id);
            var tonPresetEdited = await EditPin(tonIndex, "terminal-timer-pt", "T#1000ms", "T#2s")
                && _ladderDocument.Rungs.First(item => item.Id == ton.Id).TimerPresetOperand == "T#2s";
            var tofIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == tof.Id);
            var tofPresetEdited = await EditPin(tofIndex, "terminal-timer-pt", "T#1000ms", "T#4s")
                && _ladderDocument.Rungs.First(item => item.Id == tof.Id).TimerPresetOperand == "T#4s";
            var tpIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == tp.Id);
            var tpPresetEdited = await EditPin(tpIndex, "terminal-timer-pt", "T#1000ms", "T#5s")
                && _ladderDocument.Rungs.First(item => item.Id == tp.Id).TimerPresetOperand == "T#5s";
            var rtoIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == rto.Id);
            var rtoResetEdited = await EditPin(rtoIndex, "terminal-timer-reset", "stop_command", "review_reset")
                && _ladderDocument.Rungs.First(item => item.Id == rto.Id).TimerResetOperand == "review_reset";
            rtoIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == rto.Id);
            var rtoPresetEdited = await EditPin(rtoIndex, "terminal-timer-pt", "T#1000ms", "T#3s")
                && _ladderDocument.Rungs.First(item => item.Id == rto.Id).TimerPresetOperand == "T#3s";
            var scaleIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == scale.Id);
            var numericAEdited = await EditPin(scaleIndex, "terminal-numeric-a", "1", "review_a")
                && _ladderDocument.Rungs.First(item => item.Id == scale.Id).NumericSourceA == "review_a";
            scaleIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == scale.Id);
            var numericBEdited = await EditPin(scaleIndex, "terminal-numeric-b", "2", "review_real")
                && _ladderDocument.Rungs.First(item => item.Id == scale.Id).NumericSourceB == "review_real";
            scaleIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == scale.Id);
            var numericCEdited = await EditPin(scaleIndex, "terminal-numeric-c", "3", "review_c")
                && _ladderDocument.Rungs.First(item => item.Id == scale.Id).NumericSourceC == "review_c";
            scaleIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == scale.Id);
            var numericOutputEdited = await EditPin(scaleIndex, "terminal-output", "review_result", "review_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == scale.Id).NumericDestination == "review_result_2";
            var moveIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == move.Id);
            var moveInputEdited = await EditPin(moveIndex, "terminal-numeric-a", "11", "review_operand")
                && _ladderDocument.Rungs.First(item => item.Id == move.Id).NumericSourceA == "review_operand";
            moveIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == move.Id);
            var moveOutputEdited = await EditPin(moveIndex, "terminal-output", "review_result", "review_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == move.Id).NumericDestination == "review_result_2";
            var convertIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == convert.Id);
            var convertInputEdited = await EditPin(convertIndex, "terminal-numeric-a", "review_int", "review_int_2")
                && _ladderDocument.Rungs.First(item => item.Id == convert.Id).NumericSourceA == "review_int_2";
            convertIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == convert.Id);
            var convertOutputEdited = await EditPin(convertIndex, "terminal-output", "review_result", "review_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == convert.Id).NumericDestination == "review_result_2";
            var normalizeIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == normalize.Id);
            var normalizeAEdited = await EditPin(normalizeIndex, "terminal-numeric-a", "1", "review_a")
                && _ladderDocument.Rungs.First(item => item.Id == normalize.Id).NumericSourceA == "review_a";
            normalizeIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == normalize.Id);
            var normalizeBEdited = await EditPin(normalizeIndex, "terminal-numeric-b", "2", "review_operand")
                && _ladderDocument.Rungs.First(item => item.Id == normalize.Id).NumericSourceB == "review_operand";
            normalizeIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == normalize.Id);
            var normalizeCEdited = await EditPin(normalizeIndex, "terminal-numeric-c", "3", "review_c")
                && _ladderDocument.Rungs.First(item => item.Id == normalize.Id).NumericSourceC == "review_c";
            normalizeIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == normalize.Id);
            var normalizeOutputEdited = await EditPin(normalizeIndex, "terminal-output", "review_real_result", "review_real_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == normalize.Id).NumericDestination == "review_real_result_2";
            var exponentiateIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == exponentiate.Id);
            var exponentiateBaseEdited = await EditPin(exponentiateIndex, "terminal-numeric-a", "review_real", "review_real_2")
                && _ladderDocument.Rungs.First(item => item.Id == exponentiate.Id).NumericSourceA == "review_real_2";
            exponentiateIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == exponentiate.Id);
            var exponentiateExponentEdited = await EditPin(exponentiateIndex, "terminal-numeric-b", "2", "review_operand")
                && _ladderDocument.Rungs.First(item => item.Id == exponentiate.Id).NumericSourceB == "review_operand";
            exponentiateIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == exponentiate.Id);
            var exponentiateOutputEdited = await EditPin(exponentiateIndex, "terminal-output", "review_real_result", "review_real_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == exponentiate.Id).NumericDestination == "review_real_result_2";
            var squareRootIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == squareRoot.Id);
            var squareRootInputEdited = await EditPin(squareRootIndex, "terminal-numeric-a", "9", "review_real_2")
                && _ladderDocument.Rungs.First(item => item.Id == squareRoot.Id).NumericSourceA == "review_real_2";
            squareRootIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == squareRoot.Id);
            var squareRootOutputEdited = await EditPin(squareRootIndex, "terminal-output", "review_real_result", "review_real_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == squareRoot.Id).NumericDestination == "review_real_result_2";
            var roundIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == round.Id);
            var roundInputEdited = await EditPin(roundIndex, "terminal-numeric-a", "1.5", "review_real_2")
                && _ladderDocument.Rungs.First(item => item.Id == round.Id).NumericSourceA == "review_real_2";
            roundIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == round.Id);
            var roundOutputEdited = await EditPin(roundIndex, "terminal-output", "review_result", "review_result_2")
                && _ladderDocument.Rungs.First(item => item.Id == round.Id).NumericDestination == "review_result_2";

            var perMnemonicNumericPinResults = new List<string>();
            async System.Threading.Tasks.Task<bool> EditNumericPins(
                LadderNumericOperationKind kind,
                params (string Target, string Original, string Replacement, int SourceIndex)[] pins)
            {
                var rungId = selectorRungs[kind];
                var passed = true;
                foreach (var pin in pins)
                {
                    var rungIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == rungId);
                    var edited = await EditPin(rungIndex, pin.Target, pin.Original, pin.Replacement);
                    var current = _ladderDocument.Rungs.First(item => item.Id == rungId);
                    var stored = pin.SourceIndex switch
                    {
                        0 => current.NumericSourceA,
                        1 => current.NumericSourceB,
                        2 => current.NumericSourceC,
                        _ => current.NumericDestination,
                    };
                    passed &= edited && stored == pin.Replacement;
                }
                perMnemonicNumericPinResults.Add($"{kind}:{passed}");
                return passed;
            }

            var allPerMnemonicNumericPins = await EditNumericPins(LadderNumericOperationKind.Add,
                ("terminal-numeric-a", "0", "review_a", 0),
                ("terminal-numeric-b", "review_operand", "review_c", 1),
                ("terminal-output", "review_result", "review_result_2", -1));
            foreach (var kind in new[]
                     {
                         LadderNumericOperationKind.Subtract, LadderNumericOperationKind.Multiply,
                         LadderNumericOperationKind.Divide, LadderNumericOperationKind.Modulo,
                     })
                allPerMnemonicNumericPins &= await EditNumericPins(kind,
                    ("terminal-numeric-a", "review_a", "review_operand", 0),
                    ("terminal-numeric-b", "review_operand", "review_c", 1),
                    ("terminal-output", "review_result", "review_result_2", -1));
            foreach (var kind in new[] { LadderNumericOperationKind.Absolute, LadderNumericOperationKind.Negate })
                allPerMnemonicNumericPins &= await EditNumericPins(kind,
                    ("terminal-numeric-a", "review_operand", "review_a", 0),
                    ("terminal-output", "review_result", "review_result_2", -1));
            foreach (var kind in new[]
                     {
                         LadderNumericOperationKind.NaturalLog, LadderNumericOperationKind.Sine,
                         LadderNumericOperationKind.Cosine, LadderNumericOperationKind.Tangent,
                         LadderNumericOperationKind.ArcSine, LadderNumericOperationKind.ArcCosine,
                         LadderNumericOperationKind.ArcTangent,
                     })
                allPerMnemonicNumericPins &= await EditNumericPins(kind,
                    ("terminal-numeric-a", "review_real", "review_real_2", 0),
                    ("terminal-output", "review_real_result", "review_real_result_2", -1));
            foreach (var kind in new[]
                     {
                         LadderNumericOperationKind.Truncate, LadderNumericOperationKind.Ceiling,
                         LadderNumericOperationKind.Floor,
                     })
                allPerMnemonicNumericPins &= await EditNumericPins(kind,
                    ("terminal-numeric-a", "review_real", "review_real_2", 0),
                    ("terminal-output", "review_result", "review_result_2", -1));

            comparisonIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == comparisonRung.Id);
            var comparisonLeftEdited = await EditPin(comparisonIndex, "compare-left", "review_compare",
                "review_compare_2", comparison.Id)
                && EditableLadderTree.Find(_ladderDocument.Rungs.First(item => item.Id == comparisonRung.Id).Root,
                    comparison.Id)?.Variable == "review_compare_2";
            comparisonIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == comparisonRung.Id);
            var comparisonRightEdited = await EditPin(comparisonIndex, "compare-right", "8", "9", comparison.Id)
                && EditableLadderTree.Find(_ladderDocument.Rungs.First(item => item.Id == comparisonRung.Id).Root,
                    comparison.Id)?.RightOperand == "9";
            var perOperatorComparisonPinResults = new List<string> { $"{LadderCompareOperator.Equal}:{comparisonLeftEdited && comparisonRightEdited}" };
            var allPerOperatorComparisonPins = comparisonLeftEdited && comparisonRightEdited;
            foreach (var witness in selectorComparisons.Where(item => item.Operator != LadderCompareOperator.Equal))
            {
                var witnessIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == witness.RungId);
                var leftEdited = await EditPin(witnessIndex, "compare-left", "review_compare",
                    "review_compare_2", witness.NodeId)
                    && EditableLadderTree.Find(
                        _ladderDocument.Rungs.First(item => item.Id == witness.RungId).Root, witness.NodeId)?.Variable
                    == "review_compare_2";
                witnessIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == witness.RungId);
                var rightEdited = await EditPin(witnessIndex, "compare-right", "8", "9", witness.NodeId)
                    && EditableLadderTree.Find(
                        _ladderDocument.Rungs.First(item => item.Id == witness.RungId).Root, witness.NodeId)?.RightOperand
                    == "9";
                allPerOperatorComparisonPins &= leftEdited && rightEdited;
                perOperatorComparisonPinResults.Add($"{witness.Operator}:{leftEdited && rightEdited}");
            }
            var counterSelectorResults = new List<string>();
            var allCounterSelectors = true;
            foreach (var counterWitness in new[]
                     {
                         (Name: "CTU", Id: counter.Id),
                         (Name: "CTD", Id: downCounter.Id),
                     })
            {
                var witnessIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == counterWitness.Id);
                var selected = await ChooseAdjacentType(witnessIndex, "TiaCounterValueTypeSelector",
                    selectedIndex => _ladderDocument.Rungs.First(item => item.Id == counterWitness.Id).CounterValueType
                        == (LadderCounterValueType)selectedIndex);
                allCounterSelectors &= selected;
                counterSelectorResults.Add($"{counterWitness.Name}:{selected}");
            }

            var comparisonSelectorResults = new List<string>();
            var allComparisonSelectors = true;
            foreach (var witness in selectorComparisons)
            {
                var witnessIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == witness.RungId);
                var selected = await ChooseAdjacentType(witnessIndex, "TiaComparisonValueTypeSelector",
                    selectedIndex => EditableLadderTree.Find(
                        _ladderDocument.Rungs.First(item => item.Id == witness.RungId).Root, witness.NodeId)?.ComparisonValueType
                        == LadderComparisonValueTypePresentation.FromTiaSelector(selectedIndex), witness.NodeId);
                allComparisonSelectors &= selected;
                comparisonSelectorResults.Add($"{witness.Operator}:{selected}");
            }

            var numericSelectorResults = new List<string>();
            var allNumericSelectors = true;
            foreach (var kind in Enum.GetValues<LadderNumericOperationKind>()
                         .Where(kind => LadderNumericOperationRules.HasTiaDataTypeSelector(kind)))
            {
                if (!selectorRungs.TryGetValue(kind, out var rungId))
                {
                    allNumericSelectors = false;
                    numericSelectorResults.Add($"{kind}:missing");
                    continue;
                }
                var witnessIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == rungId);
                if (LadderNumericOperationRules.HasTiaDualDataTypeSelectors(kind))
                {
                    var inputSelected = await ChooseAdjacentType(witnessIndex, "TiaConvertInputTypeSelector",
                        selectedIndex => _ladderDocument.Rungs.First(item => item.Id == rungId).NumericInputDataType
                            == LadderNumericDataTypePresentation.FromTiaSideSelector(kind, true, selectedIndex));
                    witnessIndex = _ladderDocument.Rungs.FindIndex(item => item.Id == rungId);
                    var outputSelected = await ChooseAdjacentType(witnessIndex, "TiaConvertOutputTypeSelector",
                        selectedIndex => _ladderDocument.Rungs.First(item => item.Id == rungId).NumericOutputDataType
                            == LadderNumericDataTypePresentation.FromTiaSideSelector(kind, false, selectedIndex));
                    allNumericSelectors &= inputSelected && outputSelected;
                    numericSelectorResults.Add($"{kind}:{inputSelected}/{outputSelected}");
                }
                else
                {
                    var selected = await ChooseAdjacentType(witnessIndex, "TiaNumericDataTypeSelector",
                        selectedIndex => _ladderDocument.Rungs.First(item => item.Id == rungId).NumericDataType
                            == LadderNumericDataTypePresentation.FromTiaSelector(kind, selectedIndex));
                    allNumericSelectors &= selected;
                    numericSelectorResults.Add($"{kind}:{selected}");
                }
            }
            fits &= counterPinEdited && numericPinEdited && coilEdited && counterPresetEdited
                && counterLoadEdited && downCounterPresetEdited && tonPresetEdited
                && tofPresetEdited && tpPresetEdited && rtoResetEdited
                && rtoPresetEdited && numericAEdited && numericBEdited && numericCEdited
                && numericOutputEdited && moveInputEdited && moveOutputEdited
                && convertInputEdited && convertOutputEdited
                && normalizeAEdited && normalizeBEdited && normalizeCEdited && normalizeOutputEdited
                && exponentiateBaseEdited && exponentiateExponentEdited && exponentiateOutputEdited
                && squareRootInputEdited && squareRootOutputEdited && roundInputEdited && roundOutputEdited
                && comparisonLeftEdited && comparisonRightEdited
                && allPerMnemonicNumericPins && allPerOperatorComparisonPins
                && allCounterSelectors && allComparisonSelectors && allNumericSelectors
                && allPhysicalPropertyForms && allPropertyTransactions && allPaletteMutations;
            const string outputPrefix = "--operand-placement-output=";
            var outputArgument = OS.GetCmdlineUserArgs().FirstOrDefault(argument => argument.StartsWith(outputPrefix, StringComparison.Ordinal));
            if (outputArgument is not null)
            {
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                var captureError = GetViewport().GetTexture().GetImage().SavePng(outputArgument[outputPrefix.Length..]);
                if (captureError != Error.Ok) return (false, $"Operand placement capture failed: {captureError}");
            }
            return (fits, $"viewport={viewport} contact={editorBounds} expectedContact={expected} coil={outputBounds} expectedCoil={outputExpected} physicalRoute=Viewport.PushInput "
                + $"counterPins={counterPinEdited}/{counterPresetEdited}/{counterLoadEdited}/{downCounterPresetEdited} "
                + $"timerPins=TON:{tonPresetEdited},TOF:{tofPresetEdited},TP:{tpPresetEdited},TONR:{rtoResetEdited}/{rtoPresetEdited} "
                + $"numericPins={numericPinEdited}/{numericAEdited}/{numericBEdited}/{numericCEdited}/{numericOutputEdited} "
                + $"numericFamilies=move:{moveInputEdited}/{moveOutputEdited},convert:{convertInputEdited}/{convertOutputEdited},"
                + $"normalize:{normalizeAEdited}/{normalizeBEdited}/{normalizeCEdited}/{normalizeOutputEdited},"
                + $"expt:{exponentiateBaseEdited}/{exponentiateExponentEdited}/{exponentiateOutputEdited},"
                + $"sqrt:{squareRootInputEdited}/{squareRootOutputEdited},round:{roundInputEdited}/{roundOutputEdited} "
                + $"perMnemonicNumericPins=[{string.Join(',', perMnemonicNumericPinResults)}] "
                + $"comparisonPins=[{string.Join(',', perOperatorComparisonPinResults)}] coilPin={coilEdited} "
                + $"typeSelectors=counter[{string.Join(',', counterSelectorResults)}],"
                + $"compare[{string.Join(',', comparisonSelectorResults)}],"
                + $"numeric[{string.Join(',', numericSelectorResults)}] "
                + $"propertyForms=[{string.Join(',', physicalPropertyResults)}] "
                + $"propertyTransactions=[{string.Join(',', propertyTransactionResults)}] "
                + $"paletteMutations=[{string.Join(',', paletteMutationResults)}] controlsFit={fits}");
        }
        finally
        {
            editor?.CancelEdit();
            _ladderDocument.RestoreSnapshot(original); _ladderHistory = history;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
    }

}
