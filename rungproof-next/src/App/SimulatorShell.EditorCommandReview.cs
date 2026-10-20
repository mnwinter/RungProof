using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    /// <summary>Native Godot signal/input route regressions; visual acceptance remains separate.</summary>
    public bool VerifyEditorCommandRoutes(out string detail, Action<string, bool>? assertionObserver = null)
    {
        const string path = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal";
        var host = GetNode<Control>(path);
        T Find<T>(string name) where T : Node => host.FindChild(name, true, false) as T
            ?? throw new InvalidOperationException("Missing editor control " + name);
        var snapshot = _ladderDocument.CaptureSnapshot(); var history = _ladderHistory.Copy();
        var programBefore = _virtualProgram; var runtimeBefore = _virtualSnapshot;
        var clipboardBefore = _ladderCommandClipboard;
        var contactClipboardBefore = _ladderContactClipboard; var networkClipboardBefore = _ladderRungClipboard;
        var tiaViewBefore = (Operand: _tiaOperandRepresentation, Information: _tiaTagInformationDisplay, Position: _tiaTagInformationPosition);
        var assertions = new List<string>();
        void Require(string name, bool passed)
        {
            GD.Print($"EDITOR_COMMAND_ROUTE {name} {(passed ? "PASS" : "FAIL")}");
            assertionObserver?.Invoke(name, passed);
            if (!passed) throw new InvalidOperationException(name);
            assertions.Add(name);
        }
        void Refresh() { foreach (var refresh in _ladderEditorRefreshers) refresh(); }
        bool ActivateBlockDocument(string blockId)
        {
            var key = EditorDocumentIdentity.ForBlock(blockId);
            var tab = Find<Control>("EditorDocuments").GetChildren().OfType<HBoxContainer>()
                .SelectMany(item => item.GetChildren().OfType<Button>())
                .FirstOrDefault(button => button.GetMeta("editorDocumentKey").AsString() == key);
            if (tab is null) return false;
            tab.EmitSignal(BaseButton.SignalName.Pressed);
            return true;
        }
        var panels = new[] { Find<Control>("ProjectDockHost"), Find<Control>("ProjectOrganization"),
            Find<Control>("BottomDockHost"), Find<Control>("InspectorOutputDock") };
        var priorVisibility = panels.Select(panel => panel.Visible).ToArray();
        try
        {
            _ladderDocument.ReplaceFromProgram(LadderEditorDocument.CreateConveyorExample().BuildProgram());
            _ladderDocument.Tags[0] = _ladderDocument.Tags[0] with { Address = "%I0.0", Comment = "Operator start request" };
            _ladderHistory.Clear(); Refresh();
            var canvas = Find<LadderEditorCanvas>("GraphicalLadderCanvas");
            var menu = Find<PopupMenu>("InstructionContextMenu");
            var edit = Find<MenuButton>("LadderEditMenu").GetPopup();
            var view = Find<MenuButton>("LadderViewMenu").GetPopup();
            var output = Find<RichTextLabel>("OutputWindow");
            var editorTitle = Find<Label>("EditorTabTitle");
            var originalRungHeight = canvas.GetRungBounds(0).Size.Y;
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 8L);
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 11L);
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 14L);
            Require("TiaOperandAndTagInfoSettings", canvas.OperandRepresentation == TiaOperandRepresentation.Absolute
                && canvas.TagInformationDisplay == TiaTagInformationDisplay.Show
                && canvas.TagInformationPosition == TiaTagInformationPosition.BelowNetwork
                && canvas.GetRungBounds(0).Size.Y > originalRungHeight);
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 12L);
            Require("TiaHierarchicalCommentSetting", canvas.TagInformationDisplay == TiaTagInformationDisplay.HierarchicalComments);
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 10L);
            Require("TiaHideTagInfoSetting", canvas.TagInformationDisplay == TiaTagInformationDisplay.Hidden);

            _ladderDocument.AddAggregateTag("motor_state", PlcVariableRole.Memory, PlcVariableType.Struct,
                new PlcAggregateSchema([new PlcAggregateField("ready", PlcVariableType.Bool, "Motor permission is satisfied")]),
                new Dictionary<string, object> { ["ready"] = true }, comment: "Motor control state");
            var representationExpectations = new Dictionary<TiaOperandRepresentation, string[]>()
            {
                [TiaOperandRepresentation.Symbolic] = ["start_command"],
                [TiaOperandRepresentation.Absolute] = ["%I0.0"],
                [TiaOperandRepresentation.SymbolicAndAbsolute] = ["start_command", "%I0.0"],
            };
            var tagExpectations = new Dictionary<TiaOperandRepresentation, string>
            {
                [TiaOperandRepresentation.Symbolic] = "%I0.0 · Operator start request",
                [TiaOperandRepresentation.Absolute] = "start_command · Operator start request",
                [TiaOperandRepresentation.SymbolicAndAbsolute] = "Operator start request",
            };
            var cartesianFailure = string.Empty;
            foreach (var representation in Enum.GetValues<TiaOperandRepresentation>())
            foreach (var information in Enum.GetValues<TiaTagInformationDisplay>())
            foreach (var position in Enum.GetValues<TiaTagInformationPosition>())
            {
                canvas.SetTiaDisplaySettings(representation, information, position);
                var prefix = $"{representation}/{information}/{position}";
                if (!canvas.ReviewOperandDisplayLines("start_command").SequenceEqual(representationExpectations[representation]))
                    cartesianFailure = prefix + " changed the operand representation";
                if (!canvas.ReviewOperandDisplayLines("123.456").SequenceEqual(["123.456"]))
                    cartesianFailure = prefix + " replaced a numeric literal";
                if (!canvas.ReviewOperandDisplayLines("???").SequenceEqual(["???"]))
                    cartesianFailure = prefix + " changed an unassigned operand";
                var expectedInfo = information == TiaTagInformationDisplay.Hidden ? string.Empty : tagExpectations[representation];
                var onOperand = canvas.ReviewOnOperandTagInformation("start_command");
                var below = canvas.ReviewBelowNetworkTagInformation(_ladderDocument.Rungs[0]);
                if (onOperand != (position == TiaTagInformationPosition.OnOperands ? expectedInfo : string.Empty))
                    cartesianFailure = prefix + " produced incorrect on-operand information";
                var belowStart = below.FirstOrDefault(entry => entry.Operand == "start_command").Info ?? string.Empty;
                if (belowStart != (position == TiaTagInformationPosition.BelowNetwork ? expectedInfo : string.Empty))
                    cartesianFailure = prefix + " produced incorrect below-network information";
                if (information == TiaTagInformationDisplay.Hidden && below.Length != 0)
                    cartesianFailure = prefix + " retained hidden below-network information";
            }
            Require("TiaViewSettingsCartesianProduct" + (cartesianFailure.Length == 0 ? "" : ": " + cartesianFailure), cartesianFailure.Length == 0);
            canvas.SetTiaDisplaySettings(TiaOperandRepresentation.Symbolic, TiaTagInformationDisplay.Show,
                TiaTagInformationPosition.OnOperands);
            Require("TiaStructuredSimpleCommentUsesLeaf", canvas.ReviewOnOperandTagInformation("motor_state.ready")
                == "Motor permission is satisfied");
            canvas.SetTiaDisplaySettings(TiaOperandRepresentation.Symbolic, TiaTagInformationDisplay.HierarchicalComments,
                TiaTagInformationPosition.OnOperands);
            Require("TiaStructuredHierarchicalCommentUsesBracketedParent", canvas.ReviewOnOperandTagInformation("motor_state.ready")
                == "Motor permission is satisfied [Motor control state]");
            _tiaOperandRepresentation = tiaViewBefore.Operand;
            _tiaTagInformationDisplay = tiaViewBefore.Information;
            _tiaTagInformationPosition = tiaViewBefore.Position;
            foreach (var ladderCanvas in _ladderCanvases)
                ladderCanvas.SetTiaDisplaySettings(_tiaOperandRepresentation, _tiaTagInformationDisplay, _tiaTagInformationPosition);
            foreach (var (itemId, selected) in new[]
            {
                (7, _tiaOperandRepresentation == TiaOperandRepresentation.Symbolic),
                (8, _tiaOperandRepresentation == TiaOperandRepresentation.Absolute),
                (9, _tiaOperandRepresentation == TiaOperandRepresentation.SymbolicAndAbsolute),
                (10, _tiaTagInformationDisplay == TiaTagInformationDisplay.Hidden),
                (11, _tiaTagInformationDisplay == TiaTagInformationDisplay.Show),
                (12, _tiaTagInformationDisplay == TiaTagInformationDisplay.HierarchicalComments),
                (13, _tiaTagInformationPosition == TiaTagInformationPosition.OnOperands),
                (14, _tiaTagInformationPosition == TiaTagInformationPosition.BelowNetwork),
            }) view.SetItemChecked(view.GetItemIndex(itemId), selected);
            void Click(Vector2 point) => canvas._GuiInput(new InputEventMouseButton
                { Pressed = true, ButtonIndex = MouseButton.Right, Position = point });
            Click(canvas.GetInsertionPointPosition(0, 0, 1));
            Require("WirePopupPreservesSlot", canvas.SelectedInsertionIndex == 1 && canvas.SelectedContact == -1 && !canvas.OutputSelected);
            menu.Hide();
            var network = _ladderDocument.Rungs[0]; var id = network.Id;
            Click(canvas.GetRungBounds(0).Position + new Vector2(20, 15));
            menu.EmitSignal(PopupMenu.SignalName.IdPressed, 1L);
            Require("NetworkDeleteNeverDeletesFinalContact", _ladderDocument.Rungs.All(item => item.Id != id));
            TryUndoLadderEdit(out _); Refresh();
            network = _ladderDocument.Rungs[0];
            var contacts = network.Branches.SelectMany(branch => branch.Contacts).Select(contact => contact.Id).ToArray();
            Click(canvas.GetElementPosition(0, -1, -1, true));
            menu.EmitSignal(PopupMenu.SignalName.IdPressed, 3L);
            Require("OutputCopyIsOutputOnly", _ladderCommandClipboard is EditorOutputClipboard action && action.Action.Branches.Count == 0);
            menu.EmitSignal(PopupMenu.SignalName.IdPressed, 4L);
            Require("OutputPastePreservesConditions", contacts.SequenceEqual(_ladderDocument.Rungs[0].Branches.SelectMany(branch => branch.Contacts).Select(contact => contact.Id)));
            Click(canvas.GetElementPosition(0, 0, 0));
            TryUndoLadderEdit(out _); Refresh();
            var prior = LadderEditorProjectJson.Save(_ladderDocument);
            menu.EmitSignal(PopupMenu.SignalName.IdPressed, 1L);
            Require("StalePopupIsRejected", prior == LadderEditorProjectJson.Save(_ladderDocument) && output.Text.Contains("expired", StringComparison.Ordinal));
            menu.Hide();

            var tagType = Find<OptionButton>("NewTagType"); tagType.Select(5);
            tagType.EmitSignal(OptionButton.SignalName.ItemSelected, 5L);
            Find<MenuButton>("LadderToolsMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
            var pendingTypeDialog = _pendingEditorDialog;
            Require("BoolMenuPendingDraftRequiresResolution", pendingTypeDialog?.Visible == true);
            var discardPendingType = pendingTypeDialog!.FindChildren("*", "Button", true, false)
                .OfType<Button>().FirstOrDefault(button => button.Text == "Discard");
            Require("BoolMenuPendingDraftOffersDiscard", discardPendingType is not null);
            discardPendingType!.EmitSignal(BaseButton.SignalName.Pressed);
            Require("BoolMenuPendingDraftDiscardContinues", !pendingTypeDialog.Visible
                && tagType.Selected == 0);
            var tagName = Find<LineEdit>("NewTagName"); tagName.Text = "verify_bool_menu";
            Find<Button>("AddTag").EmitSignal(BaseButton.SignalName.Pressed);
            var boolMenuTag = _ladderDocument.Tags.SingleOrDefault(tag => tag.Name == "verify_bool_menu");
            var boolMenuReset = tagType.Selected == 0 && boolMenuTag?.Type == PlcVariableType.Bool;
            GD.Print($"EDITOR_COMMAND_ROUTE_BOOL_MENU selected={tagType.Selected} createdType={boolMenuTag?.Type.ToString() ?? "<missing>"} addDisabled={Find<Button>("AddTag").Disabled} status={output.Text}");
            Require("BoolMenuResetsPendingType", boolMenuReset);
            TryUndoLadderEdit(out _); Refresh();

            void NamedCreation(string buttonName, string dialogName, string customName)
            {
                _ladderHistory.Clear(); var before = LadderEditorProjectJson.Save(_ladderDocument);
                Find<Button>(buttonName).EmitSignal(BaseButton.SignalName.Pressed);
                var form = Find<ConfirmationDialog>(dialogName);
                var name = form.FindChild("CreationName", true, false) as LineEdit ?? throw new InvalidOperationException("Creation name missing");
                var error = form.FindChild("CreationError", true, false) as Label ?? throw new InvalidOperationException("Creation error missing");
                Button? fcType = null;
                if (buttonName == "AddBlock")
                {
                    var fbType = form.FindChild("CreateBlockType_FunctionBlock", true, false) as Button
                        ?? throw new InvalidOperationException("FB type choice missing");
                    fcType = form.FindChild("CreateBlockType_Function", true, false) as Button
                        ?? throw new InvalidOperationException("FC type choice missing");
                    var dbType = form.FindChild("CreateBlockType_DataBlock", true, false) as Button
                        ?? throw new InvalidOperationException("DB type choice missing");
                    var obType = form.FindChild("CreateBlockType_OrganizationBlock", true, false) as Button
                        ?? throw new InvalidOperationException("OB type choice missing");
                    Require("TiaAddBlockOffersObFbFcDbChoices", fbType.ButtonPressed && !fcType.ButtonPressed && !dbType.ButtonPressed && !obType.ButtonPressed
                        && name.Text.StartsWith("FB", StringComparison.Ordinal));
                    dbType.ButtonPressed = true;
                    dbType.EmitSignal(BaseButton.SignalName.Pressed);
                    var dbTypeDetails = form.FindChild("CreateDataBlockTypeDetails", true, false) as Control
                        ?? throw new InvalidOperationException("DB type details missing");
                    var dbTypeSelector = form.FindChild("CreateDataBlockType", true, false) as OptionButton
                        ?? throw new InvalidOperationException("DB type selector missing");
                    Require("TiaDbCreationExposesSupportedGlobalType", dbTypeDetails.Visible
                        && dbTypeSelector.ItemCount == 1 && dbTypeSelector.GetItemText(0) == "Global DB" && dbTypeSelector.Disabled);
                    fcType.ButtonPressed = true;
                    fcType.EmitSignal(BaseButton.SignalName.Pressed);
                    Require("TiaAddBlockTypeChoiceUpdatesSuggestedName", fcType.ButtonPressed
                        && name.Text.StartsWith("FC", StringComparison.Ordinal));
                }
                name.Text = " "; form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(dialogName + "BlankRejected", before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo && error.Text.Length > 0 && form.Visible);
                name.Text = buttonName == "AddBlock" ? _ladderDocument.Blocks[0].Name : _ladderDocument.Tasks[0].Name;
                form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(dialogName + "DuplicateRejected", before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo && error.Text.Length > 0);
                name.Text = customName; form.EmitSignal(AcceptDialog.SignalName.Canceled); form.Hide();
                Require(dialogName + "CancelUnchanged", before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo);
                Find<Button>(buttonName).EmitSignal(BaseButton.SignalName.Pressed);
                if (fcType is not null)
                {
                    fcType.ButtonPressed = true;
                    fcType.EmitSignal(BaseButton.SignalName.Pressed);
                }
                name.Text = customName; form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(dialogName + "NamedCreation", buttonName == "AddBlock"
                    ? _ladderDocument.Blocks[^1].Name == customName && _ladderDocument.Blocks[^1].BlockType == LadderBlockType.Function
                    : _ladderDocument.Tasks[^1].Name == customName);
                TryUndoLadderEdit(out _); Refresh();
            }
            TreeItem? FindTreeItem(TreeItem? item, string metadata)
            {
                for (var current = item; current is not null; current = current.GetNext())
                {
                    if (current.GetMetadata(0).AsString() == metadata) return current;
                    var child = FindTreeItem(current.GetFirstChild(), metadata);
                    if (child is not null) return child;
                }
                return null;
            }
            IEnumerable<TreeItem> EnumerateTreeItems(TreeItem? item)
            {
                for (var current = item; current is not null; current = current.GetNext())
                {
                    yield return current;
                    foreach (var child in EnumerateTreeItems(current.GetFirstChild())) yield return child;
                }
            }
            var projectTree = Find<Tree>("ProjectTree");
            var visibleProgramObs = EnumerateTreeItems(projectTree.GetRoot())
                .Count(item => item.Visible
                    && item.GetMetadata(0).AsString().StartsWith("program:", StringComparison.Ordinal)
                    && item.GetText(0).Contains("[OB", StringComparison.Ordinal));
            Require("TiaOrganizationBlockListedOnce", visibleProgramObs == 1
                && FindTreeItem(projectTree.GetRoot(), "tasks") is { Visible: false });
            var treeDbProbeSnapshot = _ladderDocument.CaptureSnapshot();
            var treeDbProbeRung = _ladderDocument.Rungs[0];
            treeDbProbeRung.IsCounter = true;
            treeDbProbeRung.CounterVariable = "verify_counter";
            treeDbProbeRung.CounterInstanceDataBlock = new LadderInstanceDataBlock(123, "IEC_Counter_0_DB");
            Refresh();
            var instanceDbRow = FindTreeItem(projectTree.GetRoot(), "instance-db:123");
            Require("TiaInstanceDbListedInsideProgramBlocks", instanceDbRow is not null
                && instanceDbRow.GetParent()?.GetMetadata(0).AsString() == "blocks"
                && FindTreeItem(projectTree.GetRoot(), "instance-dbs") is null);
            var instanceDbDocument = Find<VBoxContainer>("InstanceDbDocument");
            var instanceDbSubtitle = Find<Label>("InstanceDbSubtitle");
            var instanceDbDeclarations = Find<Tree>("InstanceDbDeclarations");
            instanceDbRow!.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemSelected);
            Require("TiaInstanceDbSelectionDoesNotOpen", !instanceDbDocument.Visible
                && output.Text.Contains("Double-click", StringComparison.Ordinal));
            instanceDbRow.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            if (_pendingEditorDialog?.Visible == true)
            {
                _pendingEditorDialog.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
                _pendingEditorDialog.Hide();
            }
            var instanceDbMember = instanceDbDeclarations.GetRoot()?.GetFirstChild()?.GetFirstChild();
            Require("TiaInstanceDbActivationOpensDefinition", instanceDbDocument.IsVisibleInTree()
                && instanceDbSubtitle.Text.Contains("Instruction instance", StringComparison.Ordinal)
                && instanceDbSubtitle.Text.Contains("verify_counter", StringComparison.Ordinal)
                && instanceDbMember?.GetText(0) == "Q" && instanceDbMember.GetText(1) == "BOOL");
            var initialBlockId = _ladderDocument.Blocks[0].Id;
            var initialBlockTabActivated = ActivateBlockDocument(initialBlockId);
            // Keep using the production Discard action when earlier command-route
            // checks have staged a form; do not bypass the navigation guard.
            if (_pendingEditorDialog?.Visible == true)
                _pendingEditorDialog.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
            Require("TiaInstanceDbReturnRestoresLadder", initialBlockTabActivated && canvas.IsVisibleInTree()
                && editorTitle.Text.Contains(_ladderDocument.Blocks[0].Name, StringComparison.Ordinal)
                && !editorTitle.Text.Contains("IEC_Counter_0_DB", StringComparison.Ordinal));
            _ladderDocument.RestoreSnapshot(treeDbProbeSnapshot); Refresh();
            var globalDbFixture = _ladderDocument.CaptureSnapshot();
            var globalDb = _ladderDocument.AddBlock("CommissioningData", LadderBlockType.DataBlock);
            _ladderDocument.AddInterfaceParameter(globalDb.Id, "TargetSpeed", PlcVariableType.Real, LadderInterfaceSection.Static, 1250.0,
                retain: true, comment: "Retained conveyor speed setpoint");
            var globalDbIndex = _ladderDocument.Blocks.IndexOf(globalDb);
            _ladderDocument.SelectBlock(globalDbIndex); Refresh();
            var globalDbTreeItem = FindTreeItem(projectTree.GetRoot(), $"program:{globalDbIndex}")
                ?? throw new InvalidOperationException("Global DB project-tree row missing");
            globalDbTreeItem.Select(0); projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            var globalDbDocument = Find<Control>("GlobalDbDocument");
            var globalDbDeclarations = Find<Tree>("GlobalDbDeclarations");
            var globalDbRow = globalDbDeclarations.GetRoot()?.GetFirstChild();
            Require("TiaGlobalDbActivationOpensDeclarationsNotLad", globalDbDocument.IsVisibleInTree()
                && !canvas.IsVisibleInTree() && globalDbRow?.GetText(0) == "TargetSpeed"
                && globalDbRow.GetText(1) == "REAL" && globalDbRow.GetText(2) == "1250");
            Require("TiaGlobalDbRetainAndCommentRender", globalDbRow?.IsChecked(3) == true
                && globalDbRow.GetText(4) == "Retained conveyor speed setpoint");
            Require("TiaGlobalDbDeclarationCellsEditable", globalDbRow is not null
                && Enumerable.Range(0, globalDbDeclarations.Columns).All(globalDbRow.IsEditable)
                && globalDbRow.GetCellMode(3) == TreeItem.TreeCellMode.Check);
            var blockTypeSelector = Find<OptionButton>("BlockTypeSelector");
            var interfaceSectionSelector = Find<OptionButton>("InterfaceSection");
            Require("TiaGlobalDbTypeAndStaticSectionAreImmutable", blockTypeSelector.Disabled
                && interfaceSectionSelector.Disabled && interfaceSectionSelector.Selected == (int)LadderInterfaceSection.Static);
            var taskTargetSelector = Find<OptionButton>("TaskTarget");
            var callTargetSelector = Find<OptionButton>("CallTarget");
            Require("TiaGlobalDbExcludedFromExecutableTargets",
                !Enumerable.Range(0, taskTargetSelector.ItemCount).Any(index => taskTargetSelector.GetItemMetadata(index).AsString() == globalDb.Id)
                && !Enumerable.Range(0, callTargetSelector.ItemCount).Any(index => callTargetSelector.GetItemMetadata(index).AsString() == globalDb.Id));
            var globalDbRungCount = globalDb.Rungs.Count;
            Find<Button>("AddRung").EmitSignal(BaseButton.SignalName.Pressed);
            Require("TiaGlobalDbRejectsLadNetwork", globalDb.Rungs.Count == globalDbRungCount
                && output.Text.Contains("contains declarations", StringComparison.Ordinal));
            _ladderDocument.RestoreSnapshot(globalDbFixture); Refresh();
            var restoredBlockItem = FindTreeItem(projectTree.GetRoot(), "program:0")
                ?? throw new InvalidOperationException("Restored entry-block row missing");
            restoredBlockItem.Select(0); projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            void ActivateTreeItemWithEnter(TreeItem item)
            {
                item.Select(0);
                projectTree.ScrollToItem(item);
                projectTree.GrabFocus();
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true });
            }
            var addBlockCommand = FindTreeItem(projectTree.GetRoot(), "add-block")
                ?? throw new InvalidOperationException("TIA Add new block tree command missing");
            var createBlockDialog = Find<ConfirmationDialog>("CreateBlockDialog");
            addBlockCommand.Select(0);
            Require("TiaTreeCommandSelectDoesNotLaunchDialog", !createBlockDialog.Visible);
            ActivateTreeItemWithEnter(addBlockCommand);
            Require("TiaAddNewBlockOpensOnPhysicalEnter", createBlockDialog.Visible);
            createBlockDialog.Hide();
            var addTagTableCommand = FindTreeItem(projectTree.GetRoot(), "add-tag-table")
                ?? throw new InvalidOperationException("TIA Add new tag table tree command missing");
            addTagTableCommand.Select(0);
            var createTagTableDialog = Find<ConfirmationDialog>("CreateTagTableDialog");
            Require("TiaTagTableCommandSelectDoesNotLaunchDialog", !createTagTableDialog.Visible);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaAddNewTagTableOpensOnActivation", createTagTableDialog.Visible);
            var tagTableName = createTagTableDialog.FindChild("CreationName", true, false) as LineEdit
                ?? throw new InvalidOperationException("Tag-table name field missing");
            var tagTableError = createTagTableDialog.FindChild("CreationError", true, false) as Label
                ?? throw new InvalidOperationException("Tag-table error field missing");
            var tablesBeforeCreate = _ladderDocument.TagTables.Count;
            tagTableName.Text = " ";
            createTagTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require("TiaTagTableRejectsBlankName", _ladderDocument.TagTables.Count == tablesBeforeCreate
                && tagTableError.Text.Length > 0 && createTagTableDialog.Visible);
            tagTableName.Text = "Default tag table";
            createTagTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require("TiaTagTableRejectsDuplicateName", _ladderDocument.TagTables.Count == tablesBeforeCreate
                && tagTableError.Text.Length > 0);
            createTagTableDialog.EmitSignal(AcceptDialog.SignalName.Canceled);
            createTagTableDialog.Hide();
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            tagTableName.Text = "VerifierTagTable";
            createTagTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            var persistedTagTables = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(_ladderDocument));
            Require("TiaTagTableCreateAndPersist", _ladderDocument.TagTables.Count == tablesBeforeCreate + 1
                && _ladderDocument.TagTables.Contains("VerifierTagTable", StringComparer.Ordinal)
                && persistedTagTables.Document?.TagTables.Contains("VerifierTagTable", StringComparer.Ordinal) == true);
            TryUndoLadderEdit(out _); Refresh();
            Require("TiaTagTableCreateUndo", _ladderDocument.TagTables.Count == tablesBeforeCreate);
            var addWatchTableCommand = FindTreeItem(projectTree.GetRoot(), "add-watch-table")
                ?? throw new InvalidOperationException("TIA Add new watch table tree command missing");
            var createWatchTableDialog = Find<ConfirmationDialog>("CreateWatchTableDialog");
            addWatchTableCommand.Select(0);
            Require("TiaWatchTableCommandSelectDoesNotLaunchDialog", !createWatchTableDialog.Visible);
            ActivateTreeItemWithEnter(addWatchTableCommand);
            Require("TiaAddNewWatchTableOpensOnPhysicalEnter", createWatchTableDialog.Visible);
            var watchTableName = createWatchTableDialog.FindChild("CreationName", true, false) as LineEdit
                ?? throw new InvalidOperationException("Watch-table name field missing");
            var watchTableError = createWatchTableDialog.FindChild("CreationError", true, false) as Label
                ?? throw new InvalidOperationException("Watch-table error field missing");
            var watchTablesBeforeCreate = _ladderDocument.WatchTables.Count;
            watchTableName.Text = " ";
            createWatchTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require("TiaWatchTableRejectsBlankName", _ladderDocument.WatchTables.Count == watchTablesBeforeCreate
                && watchTableError.Text.Length > 0 && createWatchTableDialog.Visible);
            watchTableName.Text = "Watch table 1";
            createWatchTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require("TiaWatchTableRejectsDuplicateName", _ladderDocument.WatchTables.Count == watchTablesBeforeCreate
                && watchTableError.Text.Length > 0);
            createWatchTableDialog.EmitSignal(AcceptDialog.SignalName.Canceled);
            createWatchTableDialog.Hide();
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            watchTableName.Text = "Commissioning";
            createWatchTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            var commissioningEditorKey = _getActiveTiaEditorDocumentKey?.Invoke();
            addWatchTableCommand.Select(0);
            ActivateTreeItemWithEnter(addWatchTableCommand);
            watchTableName.Text = "WatchTableCreationRoute";
            createWatchTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            var createdWatchTableIndex = _ladderDocument.WatchTables.Count - 1;
            var createdWatchTableKey = EditorDocumentIdentity.ForWatchTable(_ladderDocument.WatchTables[createdWatchTableIndex]);
            Require("TiaWatchTableCreateWhileWatchDocumentActiveOpensCreatedTable",
                _ladderDocument.ActiveWatchTable.Id == _ladderDocument.WatchTables[createdWatchTableIndex].Id
                && _getActiveTiaEditorDocumentKey?.Invoke() == createdWatchTableKey
                && Find<OptionButton>("WatchTableSelector").Selected == createdWatchTableIndex);
            TryUndoLadderEdit(out _); Refresh();
            Require("TiaWatchTableCreateUndoRestoresPreviouslyActiveDocument",
                _ladderDocument.ActiveWatchTable.Name == "Commissioning"
                && _getActiveTiaEditorDocumentKey?.Invoke() == commissioningEditorKey);
            _ladderDocument.WatchVariables.Add("start_command"); Refresh();
            var persistedWatchTables = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(_ladderDocument));
            Require("TiaWatchTableCreateOpenAndPersist", _ladderDocument.ActiveWatchTable.Name == "Commissioning"
                && _ladderDocument.WatchVariables.SequenceEqual(["start_command"], StringComparer.Ordinal)
                && persistedWatchTables.Document?.WatchTables.Count == watchTablesBeforeCreate + 1
                && persistedWatchTables.Document.WatchTables[^1].Name == "Commissioning");
            var defaultWatchTableKey = EditorDocumentIdentity.ForWatchTable(_ladderDocument.WatchTables[0]);
            var firstWatchTableRow = FindTreeItem(projectTree.GetRoot(), defaultWatchTableKey)
                ?? throw new InvalidOperationException("Default watch table row missing");
            firstWatchTableRow.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemSelected);
            Require("TiaWatchTableSelectionDoesNotOpen", _ladderDocument.ActiveWatchTable.Name == "Commissioning"
                && output.Text.Contains("Double-click", StringComparison.Ordinal));
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaWatchTableActivationSelectsIndependently", _ladderDocument.ActiveWatchTableIndex == 0
                && Find<OptionButton>("WatchTableSelector").Selected == 0);
            var sourceWatchTableIndex = _ladderDocument.WatchTables.Count - 1;
            var sourceWatchTableKey = EditorDocumentIdentity.ForWatchTable(_ladderDocument.WatchTables[sourceWatchTableIndex]);
            var sourceWatchTableRow = FindTreeItem(projectTree.GetRoot(), sourceWatchTableKey)
                ?? throw new InvalidOperationException("Commissioning watch table row missing before copy");
            sourceWatchTableRow.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemSelected);
            Require("TiaWatchTableCopySourceSelectionDoesNotOpen",
                _ladderDocument.ActiveWatchTableIndex == 0
                && Find<OptionButton>("WatchTableSelector").Selected == 0);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaWatchTableCopySourceActivationSelectsExactTable",
                _ladderDocument.ActiveWatchTableIndex == sourceWatchTableIndex
                && Find<OptionButton>("WatchTableSelector").Selected == sourceWatchTableIndex
                && _getActiveTiaEditorDocumentKey?.Invoke() == EditorDocumentIdentity.ForWatchTable(_ladderDocument.WatchTables[sourceWatchTableIndex]));
            var copyWatchTableButton = Find<Button>("DuplicateWatchTable");
            copyWatchTableButton.EmitSignal(BaseButton.SignalName.Pressed);
            Refresh();
            var copyWatchTableIndex = _ladderDocument.WatchTables.Count - 1;
            var copyWatchTableId = _ladderDocument.WatchTables[copyWatchTableIndex].Id;
            var copyWatchTableKey = EditorDocumentIdentity.ForWatchTable(copyWatchTableId);
            var copyWatchTableRow = FindTreeItem(projectTree.GetRoot(), copyWatchTableKey);
            var copyWatchCountPassed = _ladderDocument.WatchTables.Count == watchTablesBeforeCreate + 2;
            var copyWatchNamePassed = _ladderDocument.ActiveWatchTable.Name == "Commissioning Copy";
            var copyWatchMembershipPassed = _ladderDocument.WatchVariables.SequenceEqual(["start_command"], StringComparer.Ordinal);
            var copyWatchSelectorIndex = (int)Find<OptionButton>("WatchTableSelector").Selected;
            var copyWatchTreeSelection = projectTree.GetSelected()?.GetMetadata(0).AsString() ?? "<none>";
            GD.Print($"TIA_WATCH_TABLE_COPY_DETAIL count={_ladderDocument.WatchTables.Count}/{watchTablesBeforeCreate + 2} active={_ladderDocument.ActiveWatchTable.Name} membership={string.Join(',', _ladderDocument.WatchVariables)} selector={copyWatchSelectorIndex}/{copyWatchTableIndex} rowFound={copyWatchTableRow is not null} treeSelection={copyWatchTreeSelection}");
            Require("TiaWatchTableCopyActionCopiesMembershipAndSelectsCopy",
                copyWatchCountPassed && copyWatchNamePassed && copyWatchMembershipPassed
                && copyWatchSelectorIndex == copyWatchTableIndex
                && copyWatchTableRow is not null
                && copyWatchTreeSelection == copyWatchTableKey);

            Find<Button>("RenameWatchTable").EmitSignal(BaseButton.SignalName.Pressed);
            var renameWatchTableDialog = Find<ConfirmationDialog>("RenameWatchTableDialog");
            var renameWatchTableName = renameWatchTableDialog.FindChild("CreationName", true, false) as LineEdit
                ?? throw new InvalidOperationException("Watch-table rename field missing");
            var renameWatchTableError = renameWatchTableDialog.FindChild("CreationError", true, false) as Label
                ?? throw new InvalidOperationException("Watch-table rename error field missing");
            renameWatchTableName.Text = "Watch table 1";
            renameWatchTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Require("TiaWatchTableRenameRejectsDuplicateWithoutMutation",
                _ladderDocument.ActiveWatchTable.Name == "Commissioning Copy"
                && renameWatchTableError.Text.Length > 0 && renameWatchTableDialog.Visible);
            renameWatchTableName.Text = "Commissioning QA";
            renameWatchTableDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            Refresh();
            Require("TiaWatchTableRenameCommitsAndKeepsSelection",
                _ladderDocument.ActiveWatchTable.Name == "Commissioning QA"
                && Find<OptionButton>("WatchTableSelector").Selected == copyWatchTableIndex
                && projectTree.GetSelected()?.GetMetadata(0).AsString() == copyWatchTableKey);

            Find<Button>("MoveWatchTableUp").EmitSignal(BaseButton.SignalName.Pressed);
            Refresh();
            Require("TiaWatchTableMoveUpChangesActiveSelection",
                _ladderDocument.ActiveWatchTableIndex == copyWatchTableIndex - 1
                && Find<OptionButton>("WatchTableSelector").Selected == copyWatchTableIndex - 1
                && projectTree.GetSelected()?.GetMetadata(0).AsString() == copyWatchTableKey);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaWatchTableTreeActivationResolvesStableIdentityAfterReorder",
                _ladderDocument.ActiveWatchTable.Id == copyWatchTableId
                && EditorDocumentIdentity.ForWatchTable(_ladderDocument.ActiveWatchTable) == copyWatchTableKey);
            var watchMoveUndoWorked = TryUndoLadderEdit(out var watchMoveUndoDescription);
            var watchMoveUndoSelector = Find<OptionButton>("WatchTableSelector").Selected;
            var watchMoveUndoTree = projectTree.GetSelected()?.GetMetadata(0).AsString() ?? "<none>";
            Require($"TiaWatchTableMoveUndoRestoresActiveSelection [undo={watchMoveUndoWorked}:{watchMoveUndoDescription} active={_ladderDocument.ActiveWatchTableIndex} selector={watchMoveUndoSelector} tree={watchMoveUndoTree}]",
                _ladderDocument.ActiveWatchTableIndex == copyWatchTableIndex
                && watchMoveUndoSelector == copyWatchTableIndex
                && watchMoveUndoTree == copyWatchTableKey);
            TryRedoLadderEdit(out _);
            Require("TiaWatchTableMoveRedoRestoresActiveSelection",
                _ladderDocument.ActiveWatchTableIndex == copyWatchTableIndex - 1
                && Find<OptionButton>("WatchTableSelector").Selected == copyWatchTableIndex - 1
                && projectTree.GetSelected()?.GetMetadata(0).AsString() == copyWatchTableKey);

            Find<Button>("RemoveWatchTable").EmitSignal(BaseButton.SignalName.Pressed);
            Refresh();
            Require("TiaWatchTableTreeActivationRejectsDeletedStableIdentity",
                !EditorDocumentIdentity.TryResolveWatchTableIndex(_ladderDocument, copyWatchTableKey, out _)
                && FindTreeItem(projectTree.GetRoot(), copyWatchTableKey) is null);
            var survivingWatchTable = _ladderDocument.WatchTables[0];
            var survivingWatchTableKey = EditorDocumentIdentity.ForWatchTable(survivingWatchTable);
            var survivingWatchTableRow = FindTreeItem(projectTree.GetRoot(), survivingWatchTableKey)
                ?? throw new InvalidOperationException("Surviving watch table row missing after delete");
            survivingWatchTableRow.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaWatchTableTreeActivationDoesNotRetargetDeletedIdentity",
                _ladderDocument.ActiveWatchTable.Id == survivingWatchTable.Id
                && _ladderDocument.ActiveWatchTable.Id != copyWatchTableId);
            TryUndoLadderEdit(out _); Refresh();
            var restoredCopyWatchRow = FindTreeItem(projectTree.GetRoot(), copyWatchTableKey)
                ?? throw new InvalidOperationException("Undo did not restore the deleted watch table row");
            restoredCopyWatchRow.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaWatchTableTreeActivationRestoresDeletedStableIdentityOnUndo",
                EditorDocumentIdentity.TryResolveWatchTableIndex(_ladderDocument, copyWatchTableKey, out _)
                && _ladderDocument.ActiveWatchTable.Id == copyWatchTableId);
            TryRedoLadderEdit(out _); Refresh();
            Require("TiaWatchTableTreeActivationKeepsDeletedIdentityStaleOnRedo",
                !EditorDocumentIdentity.TryResolveWatchTableIndex(_ladderDocument, copyWatchTableKey, out _)
                && FindTreeItem(projectTree.GetRoot(), copyWatchTableKey) is null);
            TryUndoLadderEdit(out _); Refresh();
            restoredCopyWatchRow = FindTreeItem(projectTree.GetRoot(), copyWatchTableKey)
                ?? throw new InvalidOperationException("Undo did not restore the watch table after redo");
            restoredCopyWatchRow.Select(0);
            projectTree.EmitSignal(Tree.SignalName.ItemActivated);
            Require("TiaWatchTableDeleteUndoReturnsToPriorFixture",
                _ladderDocument.ActiveWatchTable.Id == copyWatchTableId);

            var watchTableBoundarySelector = Find<OptionButton>("WatchTableSelector");
            watchTableBoundarySelector.Select(0);
            watchTableBoundarySelector.EmitSignal(OptionButton.SignalName.ItemSelected, 0L);
            Require("TiaWatchTableMoveBoundaryControlsDisableAtFirstRow",
                Find<Button>("MoveWatchTableUp").Disabled && !Find<Button>("MoveWatchTableDown").Disabled);
            var lastWatchTableIndex = _ladderDocument.WatchTables.Count - 1;
            watchTableBoundarySelector.Select(lastWatchTableIndex);
            watchTableBoundarySelector.EmitSignal(OptionButton.SignalName.ItemSelected, (long)lastWatchTableIndex);
            Require("TiaWatchTableMoveBoundaryControlsDisableAtLastRow",
                !Find<Button>("MoveWatchTableUp").Disabled && Find<Button>("MoveWatchTableDown").Disabled);

            TryUndoLadderEdit(out _); Refresh(); // Undo the verified move.
            TryUndoLadderEdit(out _); Refresh(); // Undo the verified rename.
            TryUndoLadderEdit(out _); Refresh(); // Undo the verified copy.
            TryUndoLadderEdit(out _); Refresh();
            Require("TiaWatchTableCreateUndo", _ladderDocument.WatchTables.Count == watchTablesBeforeCreate);
            NamedCreation("AddBlock", "CreateBlockDialog", "VerifierNamedFC");
            NamedCreation("AddTask", "CreateTaskDialog", "VerifierNamedOB");

            // The named block-creation route opens the new FC in the central
            // document area before undo removes it. Restore the original ladder
            // through the same document-tab route a user takes so keyboard input
            // is tested only while its canvas is visible and focused.
            initialBlockTabActivated = ActivateBlockDocument(_ladderDocument.Blocks[0].Id);
            Refresh();
            Require("OutputDeleteLadderDocumentVisible", initialBlockTabActivated && canvas.IsVisibleInTree());

            var deletionFixture = _ladderDocument.CaptureSnapshot();
            foreach (var kind in new[] { "coil", "counter", "call" })
            foreach (var route in new[] { "context", "toolbar", "keyboard" })
            {
                _ladderDocument.RestoreSnapshot(deletionFixture);
                var terminal = _ladderDocument.Rungs[0];
                terminal.IsCounter = kind == "counter"; terminal.IsCall = kind == "call";
                var beforeRoot = terminal.Root; var terminalId = terminal.TerminalActionId;
                _ladderHistory.Clear(); Refresh();
                var routeBaseline = LadderEditorProjectJson.Save(_ladderDocument);
                if (route == "context")
                { Click(canvas.GetElementPosition(0, -1, -1, true)); menu.EmitSignal(PopupMenu.SignalName.IdPressed, 1L); }
                else
                {
                    canvas.SelectElement(0, -1, -1, true); canvas.GrabFocus();
                    if (route == "toolbar") Find<Button>("RemoveInstruction").EmitSignal(BaseButton.SignalName.Pressed);
                    else canvas._ShortcutInput(new InputEventKey { Pressed = true, Keycode = Key.Delete });
                }
                Require("OutputDelete_" + kind + "_" + route, !terminal.HasTerminalAction
                    && ReferenceEquals(beforeRoot, terminal.Root) && terminal.TerminalActionId == terminalId && _ladderHistory.CanUndo);
                var routeDeleted = LadderEditorProjectJson.Save(_ladderDocument);
                TryUndoLadderEdit(out _); Refresh();
                Require("OutputDeleteUndo_" + kind + "_" + route, _ladderDocument.Rungs[0].HasTerminalAction
                    && _ladderDocument.Rungs[0].TerminalActionId == terminalId
                    && LadderEditorProjectJson.Save(_ladderDocument) == routeBaseline);
                TryRedoLadderEdit(out _); Refresh();
                Require("OutputDeleteRedo_" + kind + "_" + route, !_ladderDocument.Rungs[0].HasTerminalAction
                    && _ladderDocument.Rungs[0].TerminalActionId == terminalId
                    && LadderEditorProjectJson.Save(_ladderDocument) == routeDeleted);
                menu.Hide();
            }
            _ladderDocument.RestoreSnapshot(deletionFixture); _ladderHistory.Clear(); Refresh();

            // Verify compiler diagnostics in two blocks remain valid while
            // navigation invalidates popup targets. A subsequent edit is stale.
            var diagnosticsFixture = _ladderDocument.CaptureSnapshot();
            var originalContact = _ladderDocument.Rungs[0].Branches[0].Contacts[0];
            _ladderDocument.ReplaceContact(0, 0, 0, originalContact with { Variable = "verify_missing_A" });
            _ladderDocument.AddBlock("DiagnosticB"); _ladderDocument.SelectBlock(1);
            _ladderDocument.AddRung("Bad B", "seal_in"); _ladderDocument.AddContact(0, 0, "verify_missing_B", false);
            _ladderDocument.SelectBlock(0); Refresh();
            Find<Button>("ValidateAndLoadButton").EmitSignal(BaseButton.SignalName.Pressed);
            var diagnostics = Find<ItemList>("ValidationIssues");
            int DiagnosticIndex(int block) => Enumerable.Range(0, diagnostics.ItemCount).FirstOrDefault(index =>
                diagnostics.GetItemMetadata(index).AsString().StartsWith($"$.blocks[{block}].networks[0].logic", StringComparison.Ordinal), -1);
            var a = DiagnosticIndex(0); var b = DiagnosticIndex(1);
            if (a < 0 || b < 0) throw new InvalidOperationException("Two-block diagnostic fixture lacked exact contact issues.");
            foreach (var pair in new[] { (a, 0), (b, 1), (a, 0) })
            {
                diagnostics.EmitSignal(ItemList.SignalName.ItemActivated, pair.Item1);
                Require("DiagnosticNavigationBlock" + pair.Item2, _ladderDocument.ActiveBlockIndex == pair.Item2
                    && canvas.SelectedContact >= 0 && !output.Text.Contains("stale", StringComparison.Ordinal));
            }
            edit.EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
            diagnostics.EmitSignal(ItemList.SignalName.ItemActivated, a);
            Require("DiagnosticStaleAfterEdit", output.Text.Contains("stale", StringComparison.Ordinal));
            Find<Control>("InstructionPropertiesPopup").Hide();
            _ladderDocument.RestoreSnapshot(diagnosticsFixture); _ladderHistory.Clear(); Refresh();

            foreach (var panel in panels) panel.Hide();
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 1L);
            var projectVisible = panels[0].Visible && panels[1].Visible;
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 2L);
            Require("HiddenDockNavigationReopensHost", projectVisible && panels[2].Visible && panels[3].Visible);

            _virtualProgram = null; _virtualSnapshot = null;
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 2L);
            var unloadedTruth = output.Text.Contains("no controller loaded", StringComparison.Ordinal);
            var schema = new PlcAggregateSchema([new PlcAggregateField("Ready", PlcVariableType.Bool)]);
            _ladderDocument.AddAggregateTag("verify_drive", PlcVariableRole.Memory, PlcVariableType.Struct, schema,
                new Dictionary<string, object> { ["Ready"] = true });
            _ladderDocument.WatchVariables.Add("verify_drive.Ready"); Refresh();
            var compiled = LadderCompiler.Compile(_ladderDocument.BuildProgram());
            if (compiled.Program is null) throw new InvalidOperationException("Native command fixture did not compile.");
            var runtime = new VirtualControllerRuntime(compiled.Program); runtime.Run();
            AttachVirtualController(compiled.Program.Source, runtime.Scan(new Dictionary<string, bool>()));
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 2L);
            var loadedTruth = output.Text.Contains("matches loaded controller", StringComparison.Ordinal);
            canvas.SelectElement(0, 0, 0); canvas.GrabFocus(); _ladderHistory.Clear();
            var beforeBoundaryDocument = LadderEditorProjectJson.Save(_ladderDocument);
            var beforeBoundarySnapshot = _virtualSnapshot; var beforeBoundaryScan = canvas.MonitorScanNumber;
            var invalidationCount = 0; Action invalidated = () => invalidationCount++;
            ChallengeDraftChanged += invalidated;
            try
            {
                Find<Button>("MoveSelectedLeft").EmitSignal(BaseButton.SignalName.Pressed);
                canvas._ShortcutInput(new InputEventKey { Pressed = true, AltPressed = true, Keycode = Key.Left });
                Require("BoundaryMovePreservesLoadedState", _ladderMonitorMatchesLoadedProgram && canvas.MonitorActive
                    && canvas.MonitorScanNumber == beforeBoundaryScan && ReferenceEquals(_virtualSnapshot, beforeBoundarySnapshot)
                    && invalidationCount == 0 && !_ladderHistory.CanUndo
                    && beforeBoundaryDocument == LadderEditorProjectJson.Save(_ladderDocument));
            }
            finally { ChallengeDraftChanged -= invalidated; }
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 5L);
            var watch = Find<Tree>("WatchTable");
            TreeItem? Search(TreeItem? row)
            {
                for (; row is not null; row = row.GetNext())
                { if (row.GetText(0) == "verify_drive.Ready") return row; var child = Search(row.GetFirstChild()); if (child is not null) return child; }
                return null;
            }
            var watchMember = Search(watch.GetRoot()); watchMember?.Select(0);
            watch.EmitSignal(Tree.SignalName.ItemActivated);
            var aggregate = Find<AggregateTagEditor>("AggregateTagEditor");
            Require("AggregateWatchRevealsMember", watchMember is not null && aggregate.Visible
                && aggregate.FindChildren("FieldInitial", "LineEdit", true, false).Cast<LineEdit>().Any(field => field.HasFocus()));
            canvas.SelectRung(0);
            edit.EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
            view.EmitSignal(PopupMenu.SignalName.IdPressed, 2L);
            Require("DiagnosticsReflectLoadedState", unloadedTruth && loadedTruth && output.Text.Contains("previous controller retained", StringComparison.Ordinal));
            detail = string.Join(", ", assertions); return true;
        }
        catch (Exception error) { detail = error.Message + "; passed=" + string.Join(", ", assertions); return false; }
        finally
        {
            foreach (var form in host.FindChildren("Create*Dialog", "ConfirmationDialog", true, false).Cast<ConfirmationDialog>()) form.Hide();
            Find<PopupMenu>("InstructionContextMenu").Hide();
            _ladderDocument.RestoreSnapshot(snapshot); _ladderHistory = history; _ladderCommandClipboard = clipboardBefore;
            _tiaOperandRepresentation = tiaViewBefore.Operand;
            _tiaTagInformationDisplay = tiaViewBefore.Information;
            _tiaTagInformationPosition = tiaViewBefore.Position;
            foreach (var ladderCanvas in _ladderCanvases)
                ladderCanvas.SetTiaDisplaySettings(_tiaOperandRepresentation, _tiaTagInformationDisplay, _tiaTagInformationPosition);
            var restoreView = Find<MenuButton>("LadderViewMenu").GetPopup();
            foreach (var (itemId, selected) in new[]
            {
                (7, _tiaOperandRepresentation == TiaOperandRepresentation.Symbolic),
                (8, _tiaOperandRepresentation == TiaOperandRepresentation.Absolute),
                (9, _tiaOperandRepresentation == TiaOperandRepresentation.SymbolicAndAbsolute),
                (10, _tiaTagInformationDisplay == TiaTagInformationDisplay.Hidden),
                (11, _tiaTagInformationDisplay == TiaTagInformationDisplay.Show),
                (12, _tiaTagInformationDisplay == TiaTagInformationDisplay.HierarchicalComments),
                (13, _tiaTagInformationPosition == TiaTagInformationPosition.OnOperands),
                (14, _tiaTagInformationPosition == TiaTagInformationPosition.BelowNetwork),
            }) restoreView.SetItemChecked(restoreView.GetItemIndex(itemId), selected);
            _ladderContactClipboard = contactClipboardBefore; _ladderRungClipboard = networkClipboardBefore;
            _virtualProgram = programBefore; _virtualSnapshot = runtimeBefore; Refresh(); RefreshLadderMonitorMatch();
            for (var i = 0; i < panels.Length; i++) panels[i].Visible = priorVisibility[i];
        }
    }
}
