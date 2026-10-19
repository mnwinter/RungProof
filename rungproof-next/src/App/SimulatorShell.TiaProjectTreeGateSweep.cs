using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

/// <summary>
/// Structured, local-only evidence for the supported Siemens project-tree
/// subset. It exercises production tree/dialog/workspace routes. It does not
/// run TIA Portal and must not be interpreted as a vendor-match artifact.
/// </summary>
public partial class SimulatorShell
{
    private const string TiaProjectTreeCommandId = "tia-project-tree-gate-sweep";

    private sealed class ProjectTreeAssertion(string id, string requirement)
    {
        public string Id { get; } = id;
        public string Requirement { get; } = requirement;
        public string Status { get; set; } = "not-run";
        public bool Passed { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    private sealed class ProjectTreeGate(string gateId, IEnumerable<(string Id, string Requirement)> requirements,
        string commandId, string sourceRevision, string assemblySha256, string startedUtc)
    {
        public string GateId { get; } = gateId;
        public string Status { get; set; } = "failed";
        public List<ProjectTreeAssertion> Assertions { get; } = requirements
            .Select(item => new ProjectTreeAssertion(item.Id, item.Requirement)).ToList();
        public string CommandId { get; } = commandId;
        public string SourceRevision { get; } = sourceRevision;
        public string AssemblySha256 { get; } = assemblySha256;
        public string StartedUtc { get; } = startedUtc;
        public string CompletedUtc { get; set; } = startedUtc;
        public bool ExternalTia => false;
        public bool LivePlc => false;

        public void Set(string id, bool passed, string detail)
        {
            var assertion = Assertions.Single(item => item.Id == id);
            assertion.Passed = passed;
            assertion.Status = passed ? "passed" : "failed";
            assertion.Detail = detail;
        }

        public void Finish()
        {
            CompletedUtc = DateTimeOffset.UtcNow.ToString("O");
            Status = Assertions.All(assertion => assertion.Status == "passed" && assertion.Passed)
                ? "passed" : "failed";
        }

        public void FailPending(string detail)
        {
            foreach (var assertion in Assertions.Where(item => item.Status == "not-run"))
                Set(assertion.Id, false, detail);
        }
    }

    private static readonly IReadOnlyDictionary<string, (string Id, string Requirement)[]> ProjectTreeRequirements =
        new Dictionary<string, (string, string)[]>(StringComparer.Ordinal)
        {
            ["tia.project-tree.activation-semantics"] =
            [
                ("single-click-selects-without-opening", "single-click selects every supported row without opening it"),
                ("double-click-activates-supported-rows", "double-click activates every supported object and command row"),
                ("enter-activates-selected-row", "focused Enter activates the selected supported row"),
                ("activation-opens-exact-target", "activation opens the exact matching document or dialog"),
                ("instance-return-restores-owner", "returning from an instance DB restores the correct active block title"),
                ("block-tab-identity-survives-reindex", "open block tabs keep their block identity across rename, Undo/Redo, and deletion of an earlier block"),
                ("project-open-replaces-document-tabs", "opening another project closes documents owned by the previous project and selects the new project's active block"),
                ("project-open-does-not-retarget-colliding-keys", "project-local block IDs and DB numbers do not retarget old document tabs to objects in the replacement project"),
            ],
            ["tia.project-tree.visible-form-cancel-actions"] =
            [
                ("cancel-controls-visible", "Tag, Block, and Task property forms provide visible Cancel actions"),
                ("tag-add-cancel-is-atomic", "Cancel discards a staged new tag without changing project data or history"),
                ("tag-edit-cancel-is-atomic", "Cancel restores an edited tag form without changing project data or history"),
                ("block-cancel-is-atomic", "Cancel restores the selected block name without changing project data or history"),
                ("task-cancel-is-atomic", "Cancel restores the selected schedule without changing project data or history"),
            ],
            ["tia.project-tree.show-all-tags"] =
            [
                ("stable-row", "the Show all tags row is present with stable metadata"),
                ("selection-does-not-open", "selection alone leaves the current document open"),
                ("activation-opens-unfiltered", "activation opens the unfiltered tag document with the matching title"),
                ("all-tables-represented", "all tag tables are represented without changing active project content"),
                ("navigation-is-clean", "navigation does not change the serialized content baseline"),
            ],
            ["tia.project-tree.create-block"] =
            [
                ("selection-versus-dialog", "selection does not open the dialog and activation does"),
                ("explicit-supported-types", "OB, FB, FC, and Global DB choices are explicit"),
                ("no-index-derived-block-numbers", "program-block rows do not fabricate Siemens block numbers from list position"),
                ("invalid-is-atomic", "blank and duplicate names are rejected without history mutation"),
                ("valid-create-persists", "valid creation persists and creates the exact tree row"),
                ("undo-redo-cleanup-exact", "Undo, Redo, and cleanup reproduce exact serialized states"),
            ],
            ["tia.project-tree.create-tag-table"] =
            [
                ("selection-versus-dialog", "selection does not open the dialog and activation does"),
                ("invalid-is-atomic", "blank and duplicate names are rejected without history mutation"),
                ("valid-create-persists", "valid creation persists with stable name and exact tree row"),
                ("undo-redo-exact", "Undo removes and Redo restores the exact table"),
                ("navigation-is-clean", "active-table navigation does not itself dirty project content"),
                ("properties-assign-membership", "the tag properties editor assigns a new tag to the selected table"),
                ("rename-preserves-membership-and-history", "renaming through the table editor preserves membership and remains consistent through Undo/Redo and Save/Load"),
                ("empty-table-delete-and-history", "the table editor deletes only an empty table and Undo/Redo restores exact state"),
            ],
            ["tia.project-tree.create-watch-table"] =
            [
                ("selection-versus-dialog", "selection does not open the dialog and activation does"),
                ("invalid-is-atomic", "blank and case-insensitive duplicate names are rejected without history mutation"),
                ("valid-create-persists", "valid creation persists with independent symbols and exact tree row"),
                ("undo-redo-exact", "Undo removes and Redo restores the exact table"),
                ("navigation-is-clean", "selecting another table is navigation rather than a content edit"),
            ],
            ["tia.project-tree.global-db-contract"] =
            [
                ("row-under-program-blocks", "the Global DB row appears under Program blocks"),
                ("block-number-not-inferred-from-row-order", "Global DB tree and document labels do not derive DB numbers from block order"),
                ("activation-opens-declarations", "activation opens declarations and not a LAD canvas"),
                ("empty-member-name-rejected", "applying a blank member name shows validation, focuses Name, and leaves the DB and Undo history unchanged"),
                ("fields-persist", "Name, Data type, Start value, Retain, and Comment persist"),
                ("declaration-grid-inline-edit", "all five Global DB fields render as editable cells, with Retain shown as a checkbox"),
                ("pending-form-blocks-inline-edit", "all declaration cells become noneditable while a member draft is pending"),
                ("member-draft-cancel-restores-inline-edit", "Cancel discards the draft without changing the DB and restores cell editability"),
                ("new-member-resolves-pending-form", "starting a new member resolves the pending form without silently discarding or mutating it"),
                ("type-change-seeds-valid-initial", "changing a new member type replaces only the untouched default with a valid type-specific value"),
                ("enum-default-is-domain-member", "selecting ENUM seeds a nonempty member domain and a matching initial value"),
                ("enum-domain-edit-validates-atomically", "editing an ENUM domain reseeds an untouched initial, valid domains apply, and invalid domains are rejected before history changes"),
                ("retain-only-draft-prompts", "Retain-only edits on existing and new members prompt before navigation, and Cancel restores the form without model mutation"),
                ("remove-resolves-pending-form", "removing a selected member resolves its pending form; Cancel preserves it, Discard removes the declaration, and Undo/Redo restore exact states"),
                ("ordinary-remove-preserves-survivors", "ordinary Remove without a pending form deletes exactly the selected declaration, preserves all other declarations and DBs, and supports exact Undo/Redo"),
                ("cancel-member-form", "Cancel discards an unapplied member draft without changing the declaration or Undo history"),
                ("form-context-labels", "initial DB entry and DB-to-FB-to-DB navigation refresh member labels and defaults"),
                ("member-editor-route", "the DB screen exposes the member form and returns to the tree while restoring the prior dock width"),
                ("declaration-grid-actions", "the Global DB declaration grid exposes Add, Edit, and Remove actions that target the selected declaration"),
                ("member-selection-stable-id", "a selected declaration remains selected by persistent member ID after same-DB row insertion and edits the same member"),
                ("edit-selection-block-scoped", "declaration selection is cleared when activation changes the owning block"),
                ("block-switch-does-not-mutate", "switching blocks leaves the other DB declaration byte-identical"),
                ("lad-network-rejected", "LAD network insertion is rejected"),
                ("excluded-from-executable-targets", "CALL, task, and program-entry target selection excludes the DB"),
            ],
            ["tia.project-tree.instance-db-contract"] =
            [
                ("row-under-program-blocks", "the generated instance DB row appears under Program blocks"),
                ("selection-versus-definition", "selection does not open and activation opens the exact definition"),
                ("identity-is-unique-and-persistent", "DB number and symbolic name are unique and persist"),
                ("definition-is-read-only", "the read-only instance table renders inherited defaults for FB Input, Output, and Static members, renders modeled IEC timer/counter member names and types, excludes TEMP locals, and discloses unsupported properties"),
                ("owner-return-restores-editor", "returning to the owner restores the correct block title and canvas"),
            ],
            ["tia.project-tree.call-drag-drop"] =
            [
                ("supported-rows-only", "only supported program-block rows produce a CALL drag payload"),
                ("terminal-validates-payload", "the visible terminal accepts the exact payload and rejects invalid targets"),
                ("drop-opens-exact-call", "drop opens CALL authoring for the exact source block"),
                ("cancel-is-exact", "Cancel preserves exact serialized state and creates no Undo entry"),
                ("apply-undo-redo-exact", "Apply, Undo, and Redo reproduce exact serialized states"),
            ],
            ["tia.project-tree.watch-table-delete"] =
            [
                ("workspace-targets-active", "the workspace delete button targets only the active watch table"),
                ("removes-one-preserves-survivors", "deletion removes exactly one table and preserves all survivors"),
                ("undo-redo-restores-selection", "Undo and Redo restore exact serialized states and active selection"),
                ("final-table-protected", "the delete control is disabled or rejected when one table remains"),
                ("no-tree-row-delete", "no project-tree row context-delete route is exposed"),
            ],
            ["tia.project-tree.watch-symbol-remove"] =
            [
                ("removes-selected-symbol-from-active-table", "Remove deletes the selected symbol from the active table, preserves other symbols and tables, and refreshes the visible count"),
                ("undo-redo-restores-exact-state", "Undo and Redo restore exact watch-table JSON and active selection"),
                ("save-load-preserves-table-isolation", "Save/Load retains the removed symbol's absence in the active table and its presence in another table"),
            ],
            ["tia.project-tree.watch-stale-symbol-visible"] =
            [
                ("persisted-unresolved-symbol-remains-visible", "a saved watch symbol with no current PLC tag remains visible and counted as unresolved"),
                ("unresolved-symbol-save-load", "Save/Load preserves an unresolved watch symbol"),
                ("unresolved-symbol-remove-and-undo-redo", "the unresolved row can be removed and Undo/Redo restores the exact state"),
            ],
        };

    public async Task<(bool Passed, string Detail)> VerifyTiaProjectTreeGateSweepAsync(string evidencePath)
    {
        var originalDocument = _ladderDocument.CaptureSnapshot();
        var originalHistory = _ladderHistory;
        var originalSaved = _ladderSavedProjectJson;
        var originalPath = _ladderProjectPath;
        var sweepStarted = DateTimeOffset.UtcNow.ToString("O");
        var sourceRevision = ComputeSourceRevision();
        var assemblyPath = typeof(SimulatorShell).Assembly.Location;
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
        {
            assemblyPath = Path.Combine(AppContext.BaseDirectory, "RungProof.Next.dll");
            if (!File.Exists(assemblyPath))
                assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), ".godot", "mono", "temp", "bin", "Debug", "RungProof.Next.dll");
        }
        if (!File.Exists(assemblyPath))
            throw new InvalidOperationException("Cannot locate the loaded RungProof.Next assembly for build identity.");
        var assemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assemblyPath)));
        var gates = ProjectTreeRequirements.Select(pair => new ProjectTreeGate(
            pair.Key, pair.Value, TiaProjectTreeCommandId, sourceRevision, assemblySha256, sweepStarted)).ToList();

        ProjectTreeGate Gate(string id) => gates.Single(gate => gate.GateId == id);
        async Task Frames(int count = 2)
        {
            for (var index = 0; index < count; index++)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        void Refresh()
        {
            foreach (var refresh in _ladderEditorRefreshers) refresh();
        }
        T Find<T>(string name) where T : Node => FindChild(name, true, false) as T
            ?? throw new InvalidOperationException($"Required project-tree verifier control '{name}' is unavailable.");
        TreeItem? FindMetadata(TreeItem? first, string metadata)
        {
            for (var item = first; item is not null; item = item.GetNext())
            {
                if (item.Visible && item.GetMetadata(0).AsString() == metadata) return item;
                if (FindMetadata(item.GetFirstChild(), metadata) is { } nested) return nested;
            }
            return null;
        }

        try
        {
            // Gate actions exercise production mutations and Undo/Redo. Isolate
            // them from any history the user already has in the live editor.
            // Swap only after entering try so every setup failure restores the
            // user's original history reference in finally.
            _ladderHistory = new LadderEditorHistory();

            // Preserve the proven physical pointer/CALL lifecycle, then restore
            // before the isolated supplemental assertions below.
            var broad = await VerifyTiaProjectTreePointerAsync();
            var broadPass = broad.Passed;
            _ladderDocument.RestoreSnapshot(originalDocument);
            _ladderHistory.Clear();
            Refresh();
            await Frames();

            const string workbenchRoot = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal/Workbench";
            const string editorRoot = workbenchRoot + "/EditorAndTasks/EditorAndInspector/ProgramEditor";
            var tree = GetNodeOrNull<LadderProgramTree>(workbenchRoot + "/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree")
                ?? throw new InvalidOperationException("TIA project tree is unavailable.");
            var title = Find<Label>("EditorTabTitle");
            var tagDocument = GetNodeOrNull<ScrollContainer>(editorRoot + "/PLC tags")
                ?? throw new InvalidOperationException("TIA central tag document is unavailable.");
            var globalDbDocument = Find<Control>("GlobalDbDocument");
            var globalDbDeclarations = Find<Tree>("GlobalDbDeclarations");
            var instanceDbDocument = Find<VBoxContainer>("InstanceDbDocument");
            var instanceDbHeading = Find<Label>("InstanceDbHeading");
            var instanceDbDeclarations = Find<Tree>("InstanceDbDeclarations");
            var instanceDbBoundary = Find<Label>("InstanceDbBoundary");
            var canvas = GetNodeOrNull<LadderEditorCanvas>(editorRoot + "/RoutineView/GraphicalLadderCanvas")
                ?? throw new InvalidOperationException("TIA ladder canvas is unavailable.");
            var output = Find<RichTextLabel>("OutputWindow");

            async Task SelectOnly(string metadata)
            {
                var row = FindMetadata(tree.GetRoot(), metadata)
                    ?? throw new InvalidOperationException($"Project-tree row '{metadata}' is missing.");
                row.Select(0);
                tree.EmitSignal(Tree.SignalName.ItemSelected);
                await Frames();
            }
            async Task Activate(string metadata)
            {
                await SelectOnly(metadata);
                tree.EmitSignal(Tree.SignalName.ItemActivated);
                await Frames();
            }
            void CloseDialogs()
            {
                Find<ConfirmationDialog>("CreateBlockDialog").Hide();
                Find<ConfirmationDialog>("CreateTagTableDialog").Hide();
                Find<ConfirmationDialog>("CreateWatchTableDialog").Hide();
                _callAuthoring?.CancelEdit();
                _pendingEditorDialog?.Hide();
            }

            // Show all tags has no coverage in the older broad pointer result.
            var showAll = Gate("tia.project-tree.show-all-tags");
            var showAllRow = FindMetadata(tree.GetRoot(), "tag-table:");
            var showBaseline = LadderEditorProjectJson.Save(_ladderDocument);
            var beforeShowTitle = title.Text;
            if (showAllRow is not null) await SelectOnly("tag-table:");
            var selectionStayed = title.Text == beforeShowTitle && !tagDocument.IsVisibleInTree();
            if (showAllRow is not null) await Activate("tag-table:");
            var tagRows = _ladderDocument.TagTables.All(table => FindMetadata(tree.GetRoot(), "tag-table:" + table) is not null);
            var showClean = LadderEditorProjectJson.Save(_ladderDocument) == showBaseline;
            showAll.Set("stable-row", showAllRow?.GetText(0) == "Show all tags", "metadata=tag-table: label=Show all tags");
            showAll.Set("selection-does-not-open", selectionStayed, $"titleBefore={beforeShowTitle} titleAfter={title.Text}");
            showAll.Set("activation-opens-unfiltered", tagDocument.IsVisibleInTree() && title.Text == "Show all tags", $"visible={tagDocument.IsVisibleInTree()} title={title.Text}");
            showAll.Set("all-tables-represented", tagRows && showClean, $"tables={_ladderDocument.TagTables.Count} treeRows={tagRows}");
            showAll.Set("navigation-is-clean", showClean, "serialized baseline unchanged after selection and activation");
            showAll.Finish();

            // The broad route physically checks click selection, double-click,
            // object/command activation, and returning from an instance DB.
            var activation = Gate("tia.project-tree.activation-semantics");
            var activationRows = new[] { "program:0", "tag-table:", "tag-table:Default tag table", "watch-table:0", "add-block", "add-tag-table", "add-watch-table" };
            var rowsPresent = activationRows.All(metadata => FindMetadata(tree.GetRoot(), metadata) is not null);
            // Focused Enter is already routed through the production Tree in the
            // editor-command verifier; prove it here on every command kind.
            var enterTargets = new[] { ("add-block", "CreateBlockDialog"), ("add-tag-table", "CreateTagTableDialog"), ("add-watch-table", "CreateWatchTableDialog") };
            var enterAll = true;
            foreach (var (metadata, dialogName) in enterTargets)
            {
                CloseDialogs();
                var row = FindMetadata(tree.GetRoot(), metadata)!;
                row.Select(0); tree.ScrollToItem(row); tree.GrabFocus();
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true }, true);
                GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = false }, true);
                await Frames();
                enterAll &= Find<ConfirmationDialog>(dialogName).Visible;
            }
            CloseDialogs();
            activation.Set("single-click-selects-without-opening", broadPass && rowsPresent, "physical pointer aggregate plus exact row inventory: " + broad.Detail);
            activation.Set("double-click-activates-supported-rows", broadPass && rowsPresent, "physical double-click aggregate: " + broad.Detail);
            activation.Set("enter-activates-selected-row", enterAll, $"commandRows={enterTargets.Length} allOpened={enterAll}");
            activation.Set("activation-opens-exact-target", broadPass && showAll.Status == "passed", "program/tag/watch/DB/instance/command aggregate plus Show all tags");
            activation.Set("instance-return-restores-owner", broadPass, "physical pointer verifier finalTitle assertion");
            activation.Finish();

            var cancelGate = Gate("tia.project-tree.visible-form-cancel-actions");
            var cancelTagButton = Find<Button>("CancelTagEdit");
            var cancelBlockButton = Find<Button>("CancelBlockEdit");
            var cancelTaskButton = Find<Button>("CancelTaskEdit");
            var cancelFixture = _ladderDocument.CaptureSnapshot();
            var cancelHistory = _ladderHistory.Copy();
            var cancelTagAddAtomic = false;
            var cancelTagEditAtomic = false;
            var cancelBlockAtomic = false;
            var cancelTaskAtomic = false;
            var cancelTagVisible = false;
            var cancelBlockVisible = false;
            var cancelTaskVisible = false;
            try
            {
                await Activate("tag-table:Default tag table");
                cancelTagVisible = cancelTagButton.IsVisibleInTree();
                var tagNameControl = Find<LineEdit>("NewTagName");
                var tagCommentControl = Find<LineEdit>("TagComment");
                var beforeAddCancel = LadderEditorProjectJson.Save(_ladderDocument);
                var addCancelHistory = (_ladderHistory.CanUndo, _ladderHistory.CanRedo);
                tagNameControl.Text = "CancelGate_NewTag";
                tagCommentControl.Text = "unapplied new-tag draft";
                cancelTagButton.EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                var tagAddressControl = Find<LineEdit>("TagAddress");
                cancelTagAddAtomic = LadderEditorProjectJson.Save(_ladderDocument) == beforeAddCancel
                    && _ladderDocument.Tags.All(tag => tag.Name != "CancelGate_NewTag")
                    && tagNameControl.Text.Length == 0
                    && tagCommentControl.Text.Length == 0
                    && tagAddressControl.Text.Length == 0
                    && Find<LineEdit>("TagInitialValue").Text == "FALSE"
                    && !_pendingEditorForms.Any(form => form.HasPending())
                    && (_ladderHistory.CanUndo, _ladderHistory.CanRedo) == addCancelHistory;

                _ladderDocument.AddTag("CancelGate_ExistingTag", PlcVariableRole.Memory,
                    type: PlcVariableType.Bool, initialValue: false, comment: "persisted comment");
                Refresh();
                await Frames();
                await Activate("tag-table:Default tag table");
                var tagTree = Find<Tree>("TagTable");
                var fixtureTagIndex = _ladderDocument.Tags.FindIndex(tag => tag.Name == "CancelGate_ExistingTag");
                var fixtureTagRow = FindMetadata(tagTree.GetRoot(), $"tag:{fixtureTagIndex}");
                fixtureTagRow?.Select(0);
                tagTree.EmitSignal(Tree.SignalName.ItemSelected);
                await Frames();
                var beforeEditCancel = LadderEditorProjectJson.Save(_ladderDocument);
                var editCancelHistory = (_ladderHistory.CanUndo, _ladderHistory.CanRedo);
                tagCommentControl.Text = "unapplied edit draft";
                cancelTagButton.EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                cancelTagEditAtomic = fixtureTagRow is not null
                    && LadderEditorProjectJson.Save(_ladderDocument) == beforeEditCancel
                    && _ladderDocument.Tags.Single(tag => tag.Name == "CancelGate_ExistingTag").Comment == "persisted comment"
                    && tagCommentControl.Text == "persisted comment"
                    && !_pendingEditorForms.Any(form => form.HasPending())
                    && (_ladderHistory.CanUndo, _ladderHistory.CanRedo) == editCancelHistory;

                await Activate("program:0");
                Find<TabContainer>("ProjectTreeAndTags").CurrentTab = 2;
                await Frames();
                cancelBlockVisible = cancelBlockButton.IsVisibleInTree();
                cancelTaskVisible = cancelTaskButton.IsVisibleInTree();
                var blockNameControl = Find<LineEdit>("BlockName");
                var originalBlockName = _ladderDocument.Blocks[0].Name;
                var beforeBlockCancel = LadderEditorProjectJson.Save(_ladderDocument);
                var blockCancelHistory = (_ladderHistory.CanUndo, _ladderHistory.CanRedo);
                blockNameControl.Text = "Unapplied_Block_Rename";
                cancelBlockButton.EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                cancelBlockAtomic = LadderEditorProjectJson.Save(_ladderDocument) == beforeBlockCancel
                    && _ladderDocument.Blocks[0].Name == originalBlockName
                    && blockNameControl.Text == originalBlockName
                    && !_pendingEditorForms.Any(form => form.HasPending())
                    && (_ladderHistory.CanUndo, _ladderHistory.CanRedo) == blockCancelHistory;

                if (_ladderDocument.Tasks.Count > 0)
                {
                    var taskNameControl = Find<LineEdit>("TaskName");
                    var originalTaskName = _ladderDocument.Tasks[0].Name;
                    var beforeTaskCancel = LadderEditorProjectJson.Save(_ladderDocument);
                    var taskCancelHistory = (_ladderHistory.CanUndo, _ladderHistory.CanRedo);
                    taskNameControl.Text = "Unapplied_Task_Rename";
                    cancelTaskButton.EmitSignal(BaseButton.SignalName.Pressed);
                    await Frames();
                    cancelTaskAtomic = LadderEditorProjectJson.Save(_ladderDocument) == beforeTaskCancel
                        && _ladderDocument.Tasks[0].Name == originalTaskName
                        && taskNameControl.Text == originalTaskName
                        && !_pendingEditorForms.Any(form => form.HasPending())
                        && (_ladderHistory.CanUndo, _ladderHistory.CanRedo) == taskCancelHistory;
                }
            }
            finally
            {
                _ladderDocument.RestoreSnapshot(cancelFixture);
                _ladderHistory = cancelHistory;
                Refresh();
                await Frames();
            }
            cancelGate.Set("cancel-controls-visible", cancelTagVisible && cancelBlockVisible && cancelTaskVisible,
                $"tag={cancelTagVisible} block={cancelBlockVisible} task={cancelTaskVisible}");
            cancelGate.Set("tag-add-cancel-is-atomic", cancelTagAddAtomic, "new-tag draft discarded without project or history changes");
            cancelGate.Set("tag-edit-cancel-is-atomic", cancelTagEditAtomic, "existing tag draft discarded and original comment reloaded");
            cancelGate.Set("block-cancel-is-atomic", cancelBlockAtomic, "block name draft discarded and original value reloaded");
            cancelGate.Set("task-cancel-is-atomic", cancelTaskAtomic, "schedule name draft discarded and original value reloaded");
            cancelGate.Finish();

            // Block tabs must resolve stable block IDs rather than whatever
            // block later occupies the original list position. Exercise the
            // actual production tab buttons and rename/delete/Undo/Redo routes.
            var tabSnapshot = _ladderDocument.CaptureSnapshot();
            var tabHistory = _ladderHistory.Copy();
            var tabIdentityPassed = false;
            var tabIdentityDetail = "not run";
            try
            {
                var earlierBlock = _ladderDocument.AddBlock("TabIdentity_Earlier", LadderBlockType.Function);
                var laterBlock = _ladderDocument.AddBlock("TabIdentity_Survivor", LadderBlockType.Function);
                Refresh();
                await Frames();
                int BlockIndex(string id) => _ladderDocument.Blocks.FindIndex(block => block.Id == id);
                var documentTabs = Find<Control>("EditorDocuments");
                IEnumerable<Button> OpenDocumentButtons() => documentTabs.GetChildren().OfType<HBoxContainer>()
                    .SelectMany(item => item.GetChildren().OfType<Button>())
                    .Where(button => button.HasMeta("editorDocumentKey"));
                Button BlockTab(string blockId) => OpenDocumentButtons()
                    .Single(button => button.GetMeta("editorDocumentKey").AsString() == EditorDocumentIdentity.ForBlock(blockId));
                var initialBlockTab = BlockTab(_ladderDocument.Blocks[0].Id);
                var startupBlockTabUsesStableIdentity = !initialBlockTab.Disabled
                    && OpenDocumentButtons().All(button =>
                        button.GetMeta("editorDocumentKey").AsString() != "block:0");
                void ActivateTab(string blockId) => BlockTab(blockId).EmitSignal(BaseButton.SignalName.Pressed);

                _tiaProgramBlockActivator!(BlockIndex(earlierBlock.Id));
                await Frames();
                _tiaProgramBlockActivator!(BlockIndex(laterBlock.Id));
                await Frames();
                var bothTabsOpened = BlockTab(earlierBlock.Id).IsVisibleInTree()
                    && BlockTab(laterBlock.Id).IsVisibleInTree();

                Find<LineEdit>("BlockName").Text = "TabIdentity_Survivor_Renamed";
                Find<Button>("ApplyBlock").EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                var renameTargetsOriginal = BlockTab(laterBlock.Id).IsVisibleInTree()
                    && BlockTab(laterBlock.Id).Text.Contains("TabIdentity_Survivor_Renamed [", StringComparison.Ordinal);
                Find<Button>("LadderUndo").EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                ActivateTab(laterBlock.Id);
                var undoRenameTargetsOriginal = _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == laterBlock.Id;
                Find<Button>("LadderRedo").EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                ActivateTab(laterBlock.Id);
                var redoRenameTargetsOriginal = _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == laterBlock.Id;

                ActivateTab(earlierBlock.Id);
                var earlierTabSelected = _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == earlierBlock.Id;
                Find<Button>("RemoveBlock").EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                var earlierTabDisabled = BlockTab(earlierBlock.Id).Disabled
                    && BlockTab(earlierBlock.Id).Text == "[Deleted block]";
                var survivorTabStillTargetsOriginal = _ladderDocument.Blocks.Any(block => block.Id == laterBlock.Id);
                ActivateTab(laterBlock.Id);
                survivorTabStillTargetsOriginal &= _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == laterBlock.Id;

                Find<Button>("LadderUndo").EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                var undoDeleteRestoresTab = _ladderDocument.Blocks.Any(block => block.Id == earlierBlock.Id)
                    && !BlockTab(earlierBlock.Id).Disabled;
                ActivateTab(earlierBlock.Id);
                undoDeleteRestoresTab &= _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == earlierBlock.Id;
                Find<Button>("LadderRedo").EmitSignal(BaseButton.SignalName.Pressed);
                await Frames();
                var redoDeleteDisablesOnlyDeletedTab = BlockTab(earlierBlock.Id).Disabled
                    && BlockTab(earlierBlock.Id).Text == "[Deleted block]";
                ActivateTab(laterBlock.Id);
                redoDeleteDisablesOnlyDeletedTab &= _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == laterBlock.Id;

                tabIdentityPassed = startupBlockTabUsesStableIdentity && bothTabsOpened && renameTargetsOriginal && undoRenameTargetsOriginal
                    && redoRenameTargetsOriginal && earlierTabSelected && earlierTabDisabled
                    && survivorTabStillTargetsOriginal && undoDeleteRestoresTab && redoDeleteDisablesOnlyDeletedTab;
                tabIdentityDetail = $"startupBlockTabUsesStableIdentity={startupBlockTabUsesStableIdentity} opened={bothTabsOpened} rename={renameTargetsOriginal} undoRename={undoRenameTargetsOriginal} "
                    + $"redoRename={redoRenameTargetsOriginal} deletedTabDisabled={earlierTabDisabled} survivorStable={survivorTabStillTargetsOriginal} "
                    + $"undoRestores={undoDeleteRestoresTab} redoKeepsDeletedTabDisabled={redoDeleteDisablesOnlyDeletedTab}";
            }
            finally
            {
                _ladderDocument.RestoreSnapshot(tabSnapshot);
                _ladderHistory = tabHistory;
                Refresh();
                await Frames();
            }
            activation.Set("block-tab-identity-survives-reindex", tabIdentityPassed, tabIdentityDetail);

            var replacementTabsPassed = false;
            var replacementTabsDetail = "not run";
            var replacementFile = Path.Combine(Path.GetTempPath(), $"rungproof-project-replacement-{Guid.NewGuid():N}.rpproj.json");
            try
            {
                // Open documents from the current project through production
                // routes, then replace it with a fresh project whose first
                // block deliberately has the same project-local ID.
                _ladderDocument.EnsureInstanceDataBlocks();
                if (!_ladderDocument.Instances.Any(instance => instance.InstanceDataBlock is not null))
                {
                    var oldInstanceBlock = _ladderDocument.AddBlock("ProjectReplacement_OldFB", LadderBlockType.FunctionBlock);
                    _ladderDocument.AddInterfaceParameter(oldInstanceBlock.Id, "Ready", PlcVariableType.Bool,
                        LadderInterfaceSection.Input, false);
                    var usedDbNumbers = _ladderDocument.Blocks.SelectMany(block => block.Rungs)
                        .SelectMany(rung => new[] { rung.TimerInstanceDataBlock?.Number, rung.CounterInstanceDataBlock?.Number })
                        .Concat(_ladderDocument.Instances.Select(instance => instance.InstanceDataBlock?.Number))
                        .Where(number => number.HasValue).Select(number => number!.Value).ToHashSet();
                    var newDbNumber = 60000;
                    while (usedDbNumbers.Contains(newDbNumber)) newDbNumber++;
                    _ladderDocument.Instances.Add(new LadderInstance("project_replacement_old", "ProjectReplacementOld",
                        oldInstanceBlock.Id, new LadderInstanceDataBlock(newDbNumber, "ProjectReplacement_Old_DB")));
                    Refresh();
                    await Frames();
                }
                var staleBlock = _ladderDocument.Blocks.Skip(1).FirstOrDefault()
                    ?? throw new InvalidOperationException("Project-replacement gate requires a non-entry block.");
                var staleDbNumber = _ladderDocument.Instances
                    .Select(instance => instance.InstanceDataBlock?.Number)
                    .FirstOrDefault(number => number.HasValue)
                    ?? throw new InvalidOperationException("Project-replacement gate requires an instance DB.");
                await Activate("tag-table:Default tag table");
                await Activate($"instance-db:{staleDbNumber}");
                var staleBlockIndex = _ladderDocument.Blocks.FindIndex(block => block.Id == staleBlock.Id);
                _tiaProgramBlockActivator?.Invoke(staleBlockIndex);
                await Frames();
                var beforeReplacementKeys = Find<Control>("EditorDocuments").GetChildren().OfType<HBoxContainer>()
                    .SelectMany(item => item.GetChildren().OfType<Button>())
                    .Where(button => button.HasMeta("editorDocumentKey"))
                    .Select(button => button.GetMeta("editorDocumentKey").AsString())
                    .ToArray();
                var previousInitialId = _ladderDocument.Blocks[0].Id;
                var staleDocumentsWereOpen = beforeReplacementKeys.Contains("tags:Default tag table", StringComparer.Ordinal)
                    && beforeReplacementKeys.Contains($"instance-db:{staleDbNumber}", StringComparer.Ordinal)
                    && beforeReplacementKeys.Contains(EditorDocumentIdentity.ForBlock(previousInitialId), StringComparer.Ordinal)
                    && beforeReplacementKeys.Contains(EditorDocumentIdentity.ForBlock(staleBlock.Id), StringComparer.Ordinal);

                var replacementProject = new LadderEditorDocument();
                replacementProject.ResetProject("replacement-project", "Replacement_Main", TimeSpan.FromMilliseconds(20));
                var replacementMain = replacementProject.Blocks[0];
                var replacementHelper = replacementProject.AddBlock("Replacement_Axis", LadderBlockType.FunctionBlock);
                replacementProject.AddInterfaceParameter(replacementHelper.Id, "Ready", PlcVariableType.Bool,
                    LadderInterfaceSection.Input, false);
                replacementProject.Instances.Add(new LadderInstance("replacement_axis", "ReplacementAxis",
                    replacementHelper.Id, new LadderInstanceDataBlock(staleDbNumber, "ReplacementAxis_DB")));
                replacementProject.AddTag("replacement_ready", PlcVariableRole.Memory);
                replacementProject.AddTagTable("Replacement_Tags");
                // Reuse the old project's exact IDs and DB number so this is
                // a true collision test rather than merely a different-project test.
                var replacementMainId = previousInitialId;
                var replacementHelperId = staleBlock.Id;
                var replacementJson = LadderEditorProjectJson.Save(replacementProject)
                    .Replace(replacementMain.Id, replacementMainId, StringComparison.Ordinal)
                    .Replace(replacementHelper.Id, replacementHelperId, StringComparison.Ordinal);
                File.WriteAllText(replacementFile, replacementJson);
                var replacementOpened = TryOpenLadderProject(replacementFile, out var replacementMessage);
                await Frames();

                var replacementTabs = Find<Control>("EditorDocuments").GetChildren().OfType<HBoxContainer>()
                    .SelectMany(item => item.GetChildren().OfType<Button>())
                    .Where(button => button.HasMeta("editorDocumentKey"))
                    .ToArray();
                var expectedKey = EditorDocumentIdentity.ForBlock(replacementMainId);
                var exactNewDocumentSelected = replacementTabs.Length == 1
                    && replacementTabs[0].GetMeta("editorDocumentKey").AsString() == expectedKey
                    && replacementTabs[0].ButtonPressed
                    && _ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == replacementMainId
                    && title.Text.Contains("Replacement_Main [OB]", StringComparison.Ordinal)
                    && canvas.IsVisibleInTree();
                var staleDocumentsCleared = replacementTabs.All(button =>
                {
                    var key = button.GetMeta("editorDocumentKey").AsString();
                    return key.StartsWith("block:", StringComparison.Ordinal)
                        && !key.StartsWith("tags:", StringComparison.Ordinal)
                        && !key.StartsWith("instance-db:", StringComparison.Ordinal);
                });
                var projectLocalIdCollision = beforeReplacementKeys.Contains(EditorDocumentIdentity.ForBlock(previousInitialId), StringComparer.Ordinal)
                    && beforeReplacementKeys.Contains(EditorDocumentIdentity.ForBlock(staleBlock.Id), StringComparer.Ordinal)
                    && replacementMainId == previousInitialId && replacementHelperId == staleBlock.Id;
                var projectLocalDbNumberCollision = beforeReplacementKeys.Contains($"instance-db:{staleDbNumber}", StringComparer.Ordinal)
                    && replacementOpened
                    && _ladderDocument.Instances.Any(instance => instance.InstanceDataBlock?.Number == staleDbNumber);
                replacementTabsPassed = staleDocumentsWereOpen && replacementOpened && exactNewDocumentSelected
                    && staleDocumentsCleared && projectLocalIdCollision && projectLocalDbNumberCollision;
                replacementTabsDetail = $"staleOpen={staleDocumentsWereOpen} opened={replacementOpened} idCollision={projectLocalIdCollision} dbNumberCollision={projectLocalDbNumberCollision} "
                    + $"tabs={replacementTabs.Length} selected={exactNewDocumentSelected} staleCleared={staleDocumentsCleared} message={replacementMessage}";
                activation.Set("project-open-replaces-document-tabs", replacementTabsPassed, replacementTabsDetail);
                activation.Set("project-open-does-not-retarget-colliding-keys", replacementTabsPassed,
                    $"oldInitial={previousInitialId} replacementInitial={replacementMainId} oldBlock={staleBlock.Id} replacementBlock={replacementHelperId} oldDb={staleDbNumber} {replacementTabsDetail}");
            }
            finally
            {
                if (File.Exists(replacementFile)) File.Delete(replacementFile);
                _ladderDocument.RestoreSnapshot(originalDocument);
                _ladderSavedProjectJson = originalSaved;
                _ladderProjectPath = originalPath;
                foreach (var resetDocuments in _ladderEditorProjectReplacementResetters) resetDocuments();
                Refresh();
                await Frames();
            }
            activation.Finish();

            // Creation gates: the physical broad route proves dialog selection,
            // valid creation, tree rows, persistence, Undo/Redo, and cleanup.
            // Add isolated model checks for the duplicate/case boundaries.
            var blockGate = Gate("tia.project-tree.create-block");
            var blockDialog = Find<ConfirmationDialog>("CreateBlockDialog");
            var explicitTypes = new[] { "CreateBlockType_OrganizationBlock", "CreateBlockType_FunctionBlock", "CreateBlockType_Function", "CreateBlockType_DataBlock" }
                .All(name => FindChild(name, true, false) is Button);
            var duplicateBlockRejected = false;
            try { _ladderDocument.AddBlock(_ladderDocument.Blocks[0].Name); }
            catch (InvalidOperationException) { duplicateBlockRejected = true; }
            var noSyntheticBlockNumbers = _ladderDocument.Blocks.Select((block, index) =>
            {
                var code = block.BlockType switch
                {
                    LadderBlockType.OrganizationBlock => "OB",
                    LadderBlockType.FunctionBlock => "FB",
                    LadderBlockType.DataBlock => "DB",
                    _ => "FC",
                };
                return FindMetadata(tree.GetRoot(), $"program:{index}")?.GetText(0) == $"{block.Name} [{code}]";
            }).All(matches => matches);
            blockGate.Set("selection-versus-dialog", broadPass && !blockDialog.Visible, "physical selection/double-click dialog route");
            blockGate.Set("explicit-supported-types", explicitTypes, "OB/FB/FC/DB toggle controls present");
            blockGate.Set("no-index-derived-block-numbers", noSyntheticBlockNumbers,
                "tree rows show block kind without asserting an unmodeled Siemens block number");
            blockGate.Set("invalid-is-atomic", broadPass && duplicateBlockRejected, $"blank=broad duplicate={duplicateBlockRejected}");
            blockGate.Set("valid-create-persists", broadPass, "physical creation, JSON mutation, and exact tree-row assertion");
            blockGate.Set("undo-redo-cleanup-exact", broadPass, "exact JSON baseline/apply/undo/redo/cleanup lifecycle");
            blockGate.Finish();

            var tagGate = Gate("tia.project-tree.create-tag-table");
            var tagDuplicate = false;
            try { _ladderDocument.AddTagTable("Default tag table"); }
            catch (InvalidOperationException) { tagDuplicate = true; }
            var tagNavBefore = LadderEditorProjectJson.Save(_ladderDocument);
            await Activate("tag-table:Default tag table");
            var tagNavClean = LadderEditorProjectJson.Save(_ladderDocument) == tagNavBefore;
            var tagTableFixture = _ladderDocument.CaptureSnapshot();
            var tagTableFixtureJson = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderHistory.Clear();
            await Activate("add-tag-table");
            var tagTableCreateDialog = Find<ConfirmationDialog>("CreateTagTableDialog");
            (tagTableCreateDialog.FindChild("CreationName", true, false) as LineEdit)!.Text = "GateSource";
            tagTableCreateDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            await Frames();
            var createdTagTableJson = LadderEditorProjectJson.Save(_ladderDocument);
            var createdTagTable = _ladderDocument.TagTables.Contains("GateSource", StringComparer.Ordinal)
                && FindMetadata(tree.GetRoot(), "tag-table:GateSource") is not null;
            var createTagTableUndo = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == tagTableFixtureJson;
            var createTagTableRedo = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == createdTagTableJson;

            await Activate("tag-table:Default tag table");
            var membership = Find<OptionButton>("TagTableMembership");
            var membershipIndex = Enumerable.Range(0, membership.ItemCount)
                .FirstOrDefault(index => membership.GetItemMetadata(index).AsString() == "GateSource", -1);
            var newTagName = Find<LineEdit>("NewTagName");
            var addTag = Find<Button>("AddTag");
            newTagName.Text = "gate_table_member";
            membership.Select(membershipIndex);
            _ladderHistory.Clear();
            addTag.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var memberTag = _ladderDocument.Tags.SingleOrDefault(tag => tag.Name == "gate_table_member");
            var membershipAssigned = membershipIndex >= 0 && memberTag?.TagTable == "GateSource";

            await Activate("tag-table:GateSource");
            var renameTable = Find<Button>("RenameTagTable");
            renameTable.EmitSignal(BaseButton.SignalName.Pressed);
            var renameDialog = Find<ConfirmationDialog>("RenameTagTableDialog");
            (renameDialog.FindChild("CreationName", true, false) as LineEdit)!.Text = "GateRenamed";
            renameDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            await Frames();
            var renamedJson = LadderEditorProjectJson.Save(_ladderDocument);
            var renamePreservedMember = !_ladderDocument.TagTables.Contains("GateSource", StringComparer.Ordinal)
                && _ladderDocument.TagTables.Contains("GateRenamed", StringComparer.Ordinal)
                && _ladderDocument.Tags.SingleOrDefault(tag => tag.Name == "gate_table_member")?.TagTable == "GateRenamed"
                && FindMetadata(tree.GetRoot(), "tag-table:GateRenamed") is not null;
            var renameSavedLoaded = LadderEditorProjectJson.Load(renamedJson) is { IsReadable: true, Document: not null } renameLoaded
                && LadderEditorProjectJson.Save(renameLoaded.Document) == renamedJson;
            var renameUndo = TryUndoLadderEdit(out _);
            var undoReconciled = renameUndo
                && _ladderDocument.TagTables.Contains("GateSource", StringComparer.Ordinal)
                && _ladderDocument.Tags.SingleOrDefault(tag => tag.Name == "gate_table_member")?.TagTable == "GateSource"
                && Find<Label>("TagTableContextHeading").Text == "TAG TABLE: DEFAULT TAG TABLE";
            var renameRedo = TryRedoLadderEdit(out _);
            var redoReconciled = renameRedo
                && _ladderDocument.TagTables.Contains("GateRenamed", StringComparer.Ordinal)
                && _ladderDocument.Tags.SingleOrDefault(tag => tag.Name == "gate_table_member")?.TagTable == "GateRenamed"
                && Find<Label>("TagTableContextHeading").Text == "TAG TABLE: DEFAULT TAG TABLE";

            await Activate("add-tag-table");
            tagTableCreateDialog = Find<ConfirmationDialog>("CreateTagTableDialog");
            (tagTableCreateDialog.FindChild("CreationName", true, false) as LineEdit)!.Text = "GateEmpty";
            tagTableCreateDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            await Frames();
            await Activate("tag-table:GateEmpty");
            var deleteEmptyTable = Find<Button>("RemoveEmptyTagTable");
            var emptyDeleteEnabled = !deleteEmptyTable.Disabled;
            _ladderHistory.Clear();
            var beforeDeleteEmptyTable = LadderEditorProjectJson.Save(_ladderDocument);
            deleteEmptyTable.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var afterDeleteEmptyTable = LadderEditorProjectJson.Save(_ladderDocument);
            var emptyTableDeleted = !_ladderDocument.TagTables.Contains("GateEmpty", StringComparer.Ordinal);
            var deleteEmptyUndo = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeDeleteEmptyTable;
            var deleteEmptyRedo = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == afterDeleteEmptyTable;
            _ladderDocument.RestoreSnapshot(tagTableFixture); _ladderHistory.Clear(); Refresh(); await Frames();
            tagGate.Set("selection-versus-dialog", broadPass, "physical selection/double-click dialog route");
            tagGate.Set("invalid-is-atomic", broadPass && tagDuplicate, $"blank=broad caseInsensitiveDuplicate={tagDuplicate}");
            tagGate.Set("valid-create-persists", broadPass, "physical creation persisted with exact named tree row");
            tagGate.Set("undo-redo-exact", broadPass && createdTagTable && createTagTableUndo && createTagTableRedo,
                $"physical={broadPass} created={createdTagTable} undo={createTagTableUndo} redo={createTagTableRedo}");
            tagGate.Set("navigation-is-clean", tagNavClean, "serialized baseline unchanged after table activation");
            tagGate.Set("properties-assign-membership", membershipAssigned,
                $"selectedIndex={membershipIndex} assigned={memberTag?.TagTable ?? "<missing>"}");
            tagGate.Set("rename-preserves-membership-and-history", renamePreservedMember && renameSavedLoaded && undoReconciled && redoReconciled,
                $"member={renamePreservedMember} saveLoad={renameSavedLoaded} undoReconciled={undoReconciled} redoReconciled={redoReconciled}");
            tagGate.Set("empty-table-delete-and-history", emptyDeleteEnabled && emptyTableDeleted && deleteEmptyUndo && deleteEmptyRedo,
                $"enabled={emptyDeleteEnabled} deleted={emptyTableDeleted} undo={deleteEmptyUndo} redo={deleteEmptyRedo}");
            tagGate.Finish();

            var watchGate = Gate("tia.project-tree.create-watch-table");
            var watchSnapshot = _ladderDocument.CaptureSnapshot();
            _ladderHistory.Clear();
            var watchBeforeCreate = LadderEditorProjectJson.Save(_ladderDocument);
            Find<Button>("AddWatchTable").EmitSignal(BaseButton.SignalName.Pressed);
            var createWatchDialog = Find<ConfirmationDialog>("CreateWatchTableDialog");
            var createWatchName = (LineEdit)createWatchDialog.FindChild("CreationName", true, false);
            var createWatchError = (Label)createWatchDialog.FindChild("CreationError", true, false);
            createWatchName.Text = "Structured Watch";
            createWatchDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            await Frames();
            var watchAfterCreate = LadderEditorProjectJson.Save(_ladderDocument);
            var createdWatch = !_ladderDocument.WatchTables[0].Variables.Contains("structured-only", StringComparer.Ordinal)
                && _ladderDocument.WatchTables.Any(table => table.Name == "Structured Watch")
                && _ladderDocument.ActiveWatchTable.Name == "Structured Watch";
            createWatchDialog.PopupCentered();
            createWatchName.Text = "structured WATCH";
            createWatchDialog.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
            await Frames();
            var duplicateRejected = createWatchDialog.Visible
                && createWatchError.Text.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                && LadderEditorProjectJson.Save(_ladderDocument) == watchAfterCreate;
            createWatchDialog.Hide();
            var watchCreateUndo = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == watchBeforeCreate;
            var watchCreateRedo = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == watchAfterCreate;
            var watchCreateSavedLoaded = LadderEditorProjectJson.Load(watchAfterCreate) is { IsReadable: true, Document: not null } watchLoaded
                && LadderEditorProjectJson.Save(watchLoaded.Document) == watchAfterCreate;
            var watchNavBefore = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderDocument.SelectWatchTable(0);
            var watchNavClean = !LadderEditorProjectJson.HasUnsavedChanges(_ladderDocument, watchNavBefore);
            _ladderDocument.RestoreSnapshot(watchSnapshot); _ladderHistory.Clear(); Refresh(); await Frames();
            watchGate.Set("selection-versus-dialog", broadPass, "physical selection/double-click dialog route");
            watchGate.Set("invalid-is-atomic", broadPass && duplicateRejected, $"physical={broadPass} caseInsensitiveDuplicateRejected={duplicateRejected}");
            watchGate.Set("valid-create-persists", broadPass && createdWatch && watchCreateSavedLoaded, $"physical={broadPass} created={createdWatch} saveLoad={watchCreateSavedLoaded}");
            watchGate.Set("undo-redo-exact", watchCreateUndo && watchCreateRedo, $"undo={watchCreateUndo} redo={watchCreateRedo}");
            watchGate.Set("navigation-is-clean", watchNavClean, "serialized project content unchanged by active-table selection");
            watchGate.Finish();

            var watchRemoveGate = Gate("tia.project-tree.watch-symbol-remove");
            var watchRemoveFixture = _ladderDocument.CaptureSnapshot();
            var watchSymbols = _ladderDocument.Tags.Select(tag => tag.Name).Distinct(StringComparer.Ordinal).Take(2).ToArray();
            if (watchSymbols.Length < 2)
                throw new InvalidOperationException("Watch-symbol removal gate requires two distinct project tags.");
            var activeWatch = _ladderDocument.AddWatchTable("Symbol Remove Active");
            activeWatch.Variables.AddRange(watchSymbols);
            var activeWatchIndex = _ladderDocument.WatchTables.IndexOf(activeWatch);
            var otherWatch = _ladderDocument.AddWatchTable("Symbol Remove Other");
            otherWatch.Variables.Add(watchSymbols[0]);
            var otherWatchIndex = _ladderDocument.WatchTables.IndexOf(otherWatch);
            _ladderDocument.SelectWatchTable(activeWatchIndex);
            Refresh(); await Frames();
            var watchRows = Find<Tree>("WatchTable");
            var selectedWatchRow = watchRows.GetRoot()?.GetFirstChild();
            while (selectedWatchRow is not null && selectedWatchRow.GetMetadata(0).AsString() != watchSymbols[0])
                selectedWatchRow = selectedWatchRow.GetNext();
            selectedWatchRow?.Select(0);
            watchRows.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            var selectedWatchSymbol = watchRows.GetSelected()?.GetMetadata(0).AsString() == watchSymbols[0];
            var beforeWatchSymbolRemove = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderHistory.Clear();
            Find<Button>("RemoveWatchSymbol").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var afterWatchSymbolRemove = LadderEditorProjectJson.Save(_ladderDocument);
            var activeWatchAfterRemove = _ladderDocument.WatchTables[activeWatchIndex];
            var otherWatchAfterRemove = _ladderDocument.WatchTables[otherWatchIndex];
            var removeIsolated = selectedWatchSymbol
                && _ladderDocument.ActiveWatchTable.Name == "Symbol Remove Active"
                && activeWatchAfterRemove.Variables.SequenceEqual([watchSymbols[1]], StringComparer.Ordinal)
                && otherWatchAfterRemove.Variables.SequenceEqual([watchSymbols[0]], StringComparer.Ordinal)
                && _ladderDocument.WatchTables.Count == watchRemoveFixture.WatchTables!.Count + 2
                && Find<Label>("WatchStatus").Text.Contains("1 SYMBOL(S)", StringComparison.Ordinal)
                && watchRows.GetRoot()?.GetFirstChild()?.GetMetadata(0).AsString() == watchSymbols[1];
            var watchRemovalStatus = Find<Label>("WatchStatus").Text;
            var undoWatchSymbolRemove = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeWatchSymbolRemove
                && _ladderDocument.ActiveWatchTable.Name == "Symbol Remove Active";
            var redoWatchSymbolRemove = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == afterWatchSymbolRemove
                && _ladderDocument.ActiveWatchTable.Name == "Symbol Remove Active";
            var loadedWatchRemoval = LadderEditorProjectJson.Load(afterWatchSymbolRemove);
            var loadedWatchDocument = loadedWatchRemoval.Document;
            var saveLoadWatchRemove = loadedWatchRemoval.IsReadable
                && loadedWatchDocument is not null
                && loadedWatchDocument.WatchTables[activeWatchIndex].Variables.SequenceEqual([watchSymbols[1]], StringComparer.Ordinal)
                && loadedWatchDocument.WatchTables[otherWatchIndex].Variables.SequenceEqual([watchSymbols[0]], StringComparer.Ordinal)
                && loadedWatchDocument.ActiveWatchTable.Name == "Symbol Remove Active";
            _ladderDocument.RestoreSnapshot(watchRemoveFixture);
            _ladderHistory.Clear(); Refresh(); await Frames();
            watchRemoveGate.Set("removes-selected-symbol-from-active-table", removeIsolated,
                $"selected={selectedWatchSymbol} active={activeWatchAfterRemove.Name} remaining={string.Join(',', activeWatchAfterRemove.Variables)} other={string.Join(',', otherWatchAfterRemove.Variables)} refreshed={watchRemovalStatus}");
            watchRemoveGate.Set("undo-redo-restores-exact-state", undoWatchSymbolRemove && redoWatchSymbolRemove,
                $"undo={undoWatchSymbolRemove} redo={redoWatchSymbolRemove}");
            watchRemoveGate.Set("save-load-preserves-table-isolation", saveLoadWatchRemove,
                $"readable={loadedWatchRemoval.IsReadable} activeAndOtherIsolated={saveLoadWatchRemove}");
            watchRemoveGate.Finish();

            var staleWatchGate = Gate("tia.project-tree.watch-stale-symbol-visible");
            var staleWatchFixture = _ladderDocument.CaptureSnapshot();
            _ladderDocument.SelectWatchTable(0);
            const string staleWatchSymbol = "DeletedLegacyTag";
            _ladderDocument.WatchVariables.Clear();
            _ladderDocument.WatchVariables.Add(staleWatchSymbol);
            Refresh(); await Frames();
            var staleWatchTree = Find<Tree>("WatchTable");
            var staleWatchRow = staleWatchTree.GetRoot()?.GetFirstChild();
            var staleWatchRowName = staleWatchRow?.GetText(0) ?? "<missing>";
            var staleWatchRowType = staleWatchRow?.GetText(1) ?? "<missing>";
            var staleWatchStatus = Find<Label>("WatchStatus").Text;
            var staleWatchVisible = staleWatchRow is not null
                && staleWatchRow.GetMetadata(0).AsString() == staleWatchSymbol
                && staleWatchRow.GetText(1) == "UNRESOLVED"
                && staleWatchRow.GetText(4) == "UNRESOLVED SYMBOL"
                && staleWatchStatus.Contains("1 SYMBOL(S)", StringComparison.Ordinal);
            var staleWatchJson = LadderEditorProjectJson.Save(_ladderDocument);
            var staleWatchLoaded = LadderEditorProjectJson.Load(staleWatchJson);
            var staleWatchRoundTrip = staleWatchLoaded.IsReadable
                && staleWatchLoaded.Document?.WatchVariables.SequenceEqual([staleWatchSymbol], StringComparer.Ordinal) == true;
            staleWatchRow?.Select(0);
            staleWatchTree.EmitSignal(Tree.SignalName.ItemSelected);
            var beforeStaleWatchRemove = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderHistory.Clear();
            Find<Button>("RemoveWatchSymbol").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var afterStaleWatchRemove = LadderEditorProjectJson.Save(_ladderDocument);
            var staleWatchRemoved = !_ladderDocument.WatchVariables.Contains(staleWatchSymbol, StringComparer.Ordinal)
                && staleWatchTree.GetRoot()?.GetFirstChild() is null
                && Find<Label>("WatchStatus").Text.Contains("0 SYMBOL(S)", StringComparison.Ordinal);
            var staleWatchUndo = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeStaleWatchRemove;
            var staleWatchRedo = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == afterStaleWatchRemove;
            _ladderDocument.RestoreSnapshot(staleWatchFixture);
            _ladderHistory.Clear(); Refresh(); await Frames();
            staleWatchGate.Set("persisted-unresolved-symbol-remains-visible", staleWatchVisible,
                $"row={staleWatchRowName} type={staleWatchRowType} status={staleWatchStatus}");
            staleWatchGate.Set("unresolved-symbol-save-load", staleWatchRoundTrip,
                $"readable={staleWatchLoaded.IsReadable} savedSymbol={staleWatchLoaded.Document?.WatchVariables.FirstOrDefault() ?? "<missing>"}");
            staleWatchGate.Set("unresolved-symbol-remove-and-undo-redo", staleWatchRemoved && staleWatchUndo && staleWatchRedo,
                $"removed={staleWatchRemoved} undo={staleWatchUndo} redo={staleWatchRedo}");
            staleWatchGate.Finish();

            // Global DB contract via the production tree/document controls.
            var globalGate = Gate("tia.project-tree.global-db-contract");
            var globalFixture = _ladderDocument.CaptureSnapshot();
            var globalDb = _ladderDocument.AddBlock("StructuredGlobalData", LadderBlockType.DataBlock);
            _ladderDocument.AddInterfaceParameter(globalDb.Id, "TargetSpeed", PlcVariableType.Real,
                LadderInterfaceSection.Static, 1250.0, retain: true, comment: "Retained speed");
            var globalIndex = _ladderDocument.Blocks.IndexOf(globalDb);
            Refresh(); await Frames();
            var globalRow = FindMetadata(tree.GetRoot(), $"program:{globalIndex}");
            var globalRowName = globalRow?.GetText(0) ?? "<missing>";
            var globalRowUnderBlocks = globalRow?.GetParent()?.GetMetadata(0).AsString() == "blocks";
            await Activate($"program:{globalIndex}");
            var globalDbLabelHasNoInventedNumber = globalRowName == "StructuredGlobalData [DB]"
                && title.Text.Contains("StructuredGlobalData [DB]", StringComparison.Ordinal)
                && !title.Text.Contains("[DB2]", StringComparison.Ordinal);
            var applyDeclaration = Find<Button>("AddInterface");
            var newMemberButton = Find<Button>("NewInterface");
            var editSection = Find<OptionButton>("InterfaceSection");
            var editRetain = Find<CheckBox>("InterfaceRetain");
            var editComment = Find<LineEdit>("InterfaceComment");
            var interfaceExecutionNote = Find<Label>("InterfaceExecutionNote");
            var interfaceHeading = Find<Label>("BlockInterfaceHeading");
            var globalMemberLabelsOnEntry = applyDeclaration.Text == "+ MEMBER"
                && newMemberButton.Text == "NEW MEMBER"
                && editSection.Selected == (int)LadderInterfaceSection.Static
                && !editSection.Visible
                && editRetain.Visible && editComment.Visible
                && interfaceHeading.Text == "GLOBAL DB DECLARATIONS"
                && interfaceExecutionNote.Text == "Optimized-access Global DB subset: supported scalar members only.";
            var targetSpeedId = globalDb.Interface.Single(parameter => parameter.Name == "TargetSpeed").Id;
            var declaration = FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId);
            var fieldPersisted = declaration?.GetText(0) == "TargetSpeed"
                && declaration.GetText(1) == "REAL" && declaration.GetText(2) == "1250"
                && declaration.IsChecked(3) && declaration.GetText(4) == "Retained speed";
            var inlineGridFieldsEditable = declaration is not null
                && Enumerable.Range(0, globalDbDeclarations.Columns).All(declaration.IsEditable)
                && declaration.GetCellMode(3) == TreeItem.TreeCellMode.Check;
            LineEdit? FindFocusedTreeEditor()
            {
                var pending = new Stack<Node>();
                pending.Push(globalDbDeclarations);
                while (pending.Count > 0)
                {
                    var current = pending.Pop();
                    if (current is LineEdit editor && editor.HasFocus() && editor.IsVisibleInTree()) return editor;
                    foreach (var child in current.GetChildren(true)) pending.Push(child);
                }
                return null;
            }
            async Task<bool> EditGlobalDeclarationCell(string id, int column, string value)
            {
                var item = FindMetadata(globalDbDeclarations.GetRoot(), id);
                if (item is null) return false;
                globalDbDeclarations.SetSelected(item, column);
                if (!globalDbDeclarations.EditSelected()) return false;
                await Frames();
                var editor = FindFocusedTreeEditor();
                if (editor is null) return false;
                editor.Text = value;
                editor.EmitSignal(LineEdit.SignalName.TextSubmitted, value);
                await Frames();
                return globalDbDeclarations.GetEdited() is null;
            }

            // Exercise the same editable-cell path used by the operator, then
            // prove one accepted commit is a single exact history transaction.
            _ladderHistory.Clear();
            var inlineOriginalJson = LadderEditorProjectJson.Save(_ladderDocument);
            var inlineOriginalDocument = LadderEditorProjectJson.Load(inlineOriginalJson).Document;
            globalDb.Interface.Add(new LadderInterfaceParameter("DuplicateGuard", PlcVariableType.Bool,
                LadderInterfaceSection.Static, false, Id: "gate-inline-duplicate-guard"));
            Refresh(); await Frames();
            var beforeInlineComment = LadderEditorProjectJson.Save(_ladderDocument);
            var inlineCommentCommitted = await EditGlobalDeclarationCell(targetSpeedId, 4, "Inline comment");
            var afterInlineComment = LadderEditorProjectJson.Save(_ladderDocument);
            var inlineCommentValue = globalDb.Interface.Single(parameter => parameter.Id == targetSpeedId).Comment;
            var inlineCommentUndo = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeInlineComment;
            var inlineCommentRedo = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == afterInlineComment;
            var inlineCommentSavedLoaded = LadderEditorProjectJson.Load(afterInlineComment) is { IsReadable: true, Document: not null } inlineCommentLoaded
                && LadderEditorProjectJson.Save(inlineCommentLoaded.Document) == afterInlineComment;

            var historyBeforeInlineReject = (_ladderHistory.CanUndo, _ladderHistory.CanRedo);
            var beforeDuplicateInlineName = LadderEditorProjectJson.Save(_ladderDocument);
            var duplicateInlineSubmitted = await EditGlobalDeclarationCell(targetSpeedId, 0, "duplicateguard");
            var duplicateInlineRejected = duplicateInlineSubmitted
                && output.Text.Contains("Global DB edit rejected", StringComparison.Ordinal)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeDuplicateInlineName
                && FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId)?.GetText(0) == "TargetSpeed"
                && (_ladderHistory.CanUndo, _ladderHistory.CanRedo) == historyBeforeInlineReject;
            var beforeBlankInlineName = LadderEditorProjectJson.Save(_ladderDocument);
            var blankInlineSubmitted = await EditGlobalDeclarationCell(targetSpeedId, 0, "   ");
            var blankInlineRejected = blankInlineSubmitted
                && output.Text.Contains("Global DB edit rejected", StringComparison.Ordinal)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeBlankInlineName
                && FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId)?.GetText(0) == "TargetSpeed"
                && (_ladderHistory.CanUndo, _ladderHistory.CanRedo) == historyBeforeInlineReject;
            var inlineCommentTransaction = inlineCommentCommitted && inlineCommentValue == "Inline comment"
                && inlineCommentUndo && inlineCommentRedo && inlineCommentSavedLoaded
                && duplicateInlineRejected && blankInlineRejected;
            // Restore the fixture before the existing side-form transaction
            // checks; the new inline case owns and clears its history entries.
            var inlineCommentCleanup = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeInlineComment;
            if (inlineOriginalDocument is not null)
            {
                _ladderDocument.RestoreSnapshot(inlineOriginalDocument.CaptureSnapshot());
                inlineCommentCleanup = inlineCommentCleanup
                    && LadderEditorProjectJson.Save(_ladderDocument) == inlineOriginalJson;
            }
            _ladderHistory.Clear();
            Refresh(); await Frames();
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);
            declaration = FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId);
            var inlineCommentRestored = globalDb.Interface.Single(parameter => parameter.Id == targetSpeedId).Comment == "Retained speed"
                && declaration?.GetText(4) == "Retained speed" && inlineCommentCleanup
                && !globalDb.Interface.Any(parameter => parameter.Id == "gate-inline-duplicate-guard");
            inlineGridFieldsEditable = inlineGridFieldsEditable && inlineCommentTransaction && inlineCommentRestored;
            var memberEditorButton = Find<Button>("EditGlobalDbMembers");
            var addMemberAction = Find<Button>("GlobalDbAddMember");
            var editMemberAction = Find<Button>("GlobalDbEditMember");
            var removeMemberAction = Find<Button>("GlobalDbRemoveMember");
            var projectTabs = Find<TabContainer>("ProjectTreeAndTags");
            var projectDock = Find<Control>("ProjectOrganization");
            var projectSplit = Find<SplitContainer>("Workbench");
            var directAddBefore = LadderEditorProjectJson.Save(_ladderDocument);
            addMemberAction.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var directAddOpened = projectTabs.CurrentTab == 2 && applyDeclaration.Text == "+ MEMBER"
                && Find<LineEdit>("InterfaceName").Text.Length == 0;
            var beforeEmptyMemberApply = LadderEditorProjectJson.Save(_ladderDocument);
            var undoBeforeEmptyMemberApply = _ladderHistory.CanUndo;
            var emptyMemberName = Find<LineEdit>("InterfaceName");
            applyDeclaration.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var emptyMemberFeedback = output.Text.Contains("non-empty declaration name", StringComparison.OrdinalIgnoreCase);
            var emptyMemberFocused = emptyMemberName.HasFocus();
            var emptyMemberDocumentUnchanged = LadderEditorProjectJson.Save(_ladderDocument) == beforeEmptyMemberApply;
            var emptyMemberUndoUnchanged = _ladderHistory.CanUndo == undoBeforeEmptyMemberApply;
            var emptyMemberRejected = emptyMemberFeedback && emptyMemberFocused
                && emptyMemberDocumentUnchanged && emptyMemberUndoUnchanged;
            Find<Button>("CancelInterface").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var directAddIsDraftOnly = LadderEditorProjectJson.Save(_ladderDocument) == directAddBefore
                && !_pendingEditorForms.Any(form => form.HasPending());
            memberEditorButton.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var globalEditRow = globalDbDeclarations.GetRoot()?.GetFirstChild();
            globalEditRow?.Select(0);
            globalDbDeclarations.EmitSignal(Tree.SignalName.ItemSelected);
            editMemberAction.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var directEditLoaded = projectTabs.CurrentTab == 2 && applyDeclaration.Text == "APPLY MEMBER"
                && Find<LineEdit>("InterfaceName").Text == "TargetSpeed";
            Find<Button>("CancelInterface").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var directGridActions = addMemberAction.IsVisibleInTree() && editMemberAction.IsVisibleInTree()
                && removeMemberAction.IsVisibleInTree() && directAddOpened && directAddIsDraftOnly && directEditLoaded;
            memberEditorButton.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var priorDockWidth = projectSplit.SplitOffsets.Length > 0 ? projectSplit.SplitOffsets[0] : 280;
            // 240 is a valid manually selected width above the 150 px dock
            // minimum and is below the route's 280 px editing minimum. This
            // proves both the widen and exact restore behavior even headless.
            projectSplit.SplitOffsets = [240];
            await Frames();
            var baselineMemberDockWidth = projectDock.GetGlobalRect().Size.X;
            var memberEditorRouteVisible = memberEditorButton.IsVisibleInTree();
            memberEditorButton.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var expectedMemberDockWidth = Math.Min(600, Math.Max(560, (int)projectSplit.Size.X / 3));
            var requestedMemberDockWidth = projectSplit.SplitOffsets.Length > 0 ? projectSplit.SplitOffsets[0] : -1;
            var projectDockRect = projectDock.GetGlobalRect();
            var actualMemberDockWidth = projectDockRect.Size.X;
            var memberActionButtons = new[]
            {
                Find<Button>("AddInterface"), Find<Button>("NewInterface"),
                Find<Button>("CancelInterface"), Find<Button>("RemoveInterface"),
            };
            var interfaceCommands = Find<HFlowContainer>("InterfaceCommands");
            var memberActionsFitDock = memberActionButtons.All(button =>
            {
                var rect = button.GetGlobalRect();
                return button.IsVisibleInTree()
                    && rect.Position.X >= projectDockRect.Position.X
                    && rect.End.X <= projectDockRect.End.X;
            });
            var memberActionsWrap = interfaceCommands.Size.Y
                > memberActionButtons.Max(button => button.Size.Y) + 1;
            var wideDockReached = actualMemberDockWidth >= 560 && requestedMemberDockWidth >= 560;
            var narrowDockUsable = actualMemberDockWidth >= 150 && memberActionsWrap && memberActionsFitDock;
            var memberEditorTreeVisible = Find<Tree>("BlockInterface").IsVisibleInTree();
            var memberEditorTab = projectTabs.CurrentTab;
            var memberEditorButtonText = memberEditorButton.Text;
            var memberEditorOpened = projectTabs.CurrentTab == 2 && memberEditorButton.Text == "PROJECT TREE"
                && projectDock.Visible && memberEditorTreeVisible
                && (wideDockReached || narrowDockUsable) && memberActionsFitDock;
            memberEditorButton.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var restoredDockWidth = projectSplit.SplitOffsets.Length > 0 ? projectSplit.SplitOffsets[0] : -1;
            var restoredActualDockWidth = projectDock.GetGlobalRect().Size.X;
            var projectTreeRestored = projectTabs.CurrentTab == 0 && memberEditorButton.Text == "EDIT MEMBERS"
                && projectDock.Visible && restoredDockWidth == 240
                && Math.Abs(restoredActualDockWidth - baselineMemberDockWidth) <= 1;
            // Keep the gate isolated from the shell state expected by later
            // assertions and restore the splitter value that preceded setup.
            projectSplit.SplitOffsets = [priorDockWidth];
            await Frames();
            var originalDeclarationText = declaration is null ? "<missing>"
                : $"{declaration.GetText(0)}/{declaration.GetText(1)}/{declaration.GetText(2)}/{declaration.GetText(3)}/{declaration.GetText(4)}";
            var declarationsTable = Find<Tree>("BlockInterface");
            // BlockInterface hides its root and stores declaration rows as
            // direct children. The extra GetFirstChild() returned null and
            // made this gate report a false failure without selecting a row.
            var existingDeclarationRow = declarationsTable.GetRoot()?.GetFirstChild();
            existingDeclarationRow?.Select(0);
            declarationsTable.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            var editName = Find<LineEdit>("InterfaceName");
            var editInitial = Find<LineEdit>("InterfaceInitial");
            var editEnumMembers = Find<LineEdit>("InterfaceEnumMembers");
            var editType = Find<OptionButton>("InterfaceType");
            var cancelMember = Find<Button>("CancelInterface");
            var selectedDeclarationLoaded = editName.Text == "TargetSpeed" && applyDeclaration.Text == "APPLY MEMBER";
            var beforePendingMemberDraft = LadderEditorProjectJson.Save(_ladderDocument);
            editComment.Text = "Discarded pending comment";
            editComment.EmitSignal(LineEdit.SignalName.TextChanged, editComment.Text);
            await Frames();
            var pendingDraftRow = FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId);
            var pendingDraftCellEditability = pendingDraftRow is null
                ? "<row-missing>"
                : string.Join(",", Enumerable.Range(0, globalDbDeclarations.Columns).Select(column => pendingDraftRow.IsEditable(column)));
            var pendingFormCountDuringGridCheck = _pendingEditorForms.Count(form => form.HasPending());
            var inlineGridBlockedByMemberDraft = pendingDraftRow is not null
                && Enumerable.Range(0, globalDbDeclarations.Columns).All(column => !pendingDraftRow.IsEditable(column));
            cancelMember.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var rowAfterDraftCancel = FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId);
            var inlineGridRestoredAfterMemberCancel = rowAfterDraftCancel is not null
                && Enumerable.Range(0, globalDbDeclarations.Columns).All(rowAfterDraftCancel.IsEditable)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforePendingMemberDraft
                && globalDb.Interface.Single(parameter => parameter.Name == "TargetSpeed").Comment == "Retained speed";
            var sideRowAfterDraftCancel = FindMetadata(declarationsTable.GetRoot(), targetSpeedId);
            sideRowAfterDraftCancel?.Select(0);
            declarationsTable.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            globalDb.Interface.Insert(0, new LadderInterfaceParameter("ReorderMarker", PlcVariableType.Bool,
                LadderInterfaceSection.Static, false, Id: "gate-reorder-marker"));
            Refresh(); await Frames();
            var selectedDeclarationSurvivesReorder = editName.Text == "TargetSpeed"
                && applyDeclaration.Text == "APPLY MEMBER"
                && declarationsTable.GetSelected()?.GetMetadata(0).AsString() == targetSpeedId;
            var originalDeclaration = LadderEditorProjectJson.Save(_ladderDocument);
            editName.Text = "TargetSpeedEdited";
            editInitial.Text = "1500";
            editRetain.ButtonPressed = false;
            editComment.Text = "Edited speed setpoint";
            applyDeclaration.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var editedDeclaration = globalDb.Interface.SingleOrDefault(parameter => parameter.Name == "TargetSpeedEdited");
            var editedRow = FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId);
            var declarationEdited = editedDeclaration is { Type: PlcVariableType.Real, InitialValue: double speed }
                && speed == 1500.0 && !editedDeclaration.Retain && editedDeclaration.Comment == "Edited speed setpoint"
                && editedRow?.GetText(0) == "TargetSpeedEdited" && editedRow.GetText(2) == "1500";
            var editedDeclarationState = LadderEditorProjectJson.Save(_ladderDocument);
            Find<Button>("LadderUndo").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var declarationUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == originalDeclaration;
            Find<Button>("LadderRedo").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var declarationRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == editedDeclarationState;
            var declarationEditLifecycle = selectedDeclarationLoaded && declarationEdited
                && declarationUndoExact && declarationRedoExact;
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);

            // Starting another member must resolve an unapplied form before clearing it.
            var beforeNewMemberDraft = LadderEditorProjectJson.Save(_ladderDocument);
            editName.Text = "UnappliedMember";
            editName.EmitSignal(LineEdit.SignalName.TextChanged, editName.Text);
            newMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            var newMemberWaitsForResolution = _pendingEditorDialog?.Visible == true
                && editName.Text == "UnappliedMember"
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeNewMemberDraft;
            _pendingEditorDialog?.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            _pendingEditorDialog?.Hide();
            var newMemberCancelKeepsDraft = editName.Text == "UnappliedMember"
                && _pendingEditorForms.Any(form => form.HasPending())
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeNewMemberDraft;
            newMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            var newMemberDiscardDialogReopened = _pendingEditorDialog?.Visible == true;
            _pendingEditorDialog?.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
            _pendingEditorDialog?.Hide();
            var newMemberDiscardIsAtomic = LadderEditorProjectJson.Save(_ladderDocument) == beforeNewMemberDraft
                && editName.Text.Length == 0 && applyDeclaration.Text == "+ MEMBER"
                && !_pendingEditorForms.Any(form => form.HasPending());

            // A new numeric member must get a numeric zero instead of carrying
            // the default BOOL literal into an invalid type/value pair. Cancel
            // must discard that form state without adding a declaration/history.
            var beforeCancelMember = LadderEditorProjectJson.Save(_ladderDocument);
            var undoBeforeCancelMember = _ladderHistory.CanUndo;
            editName.Text = "CanceledMember";
            editType.Select(1);
            editType.EmitSignal(OptionButton.SignalName.ItemSelected, 1L);
            var numericDefaultIsValid = editInitial.Text == "0";
            cancelMember.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var cancelMemberSerializedUnchanged = LadderEditorProjectJson.Save(_ladderDocument) == beforeCancelMember;
            var cancelMemberNoPendingForm = !_pendingEditorForms.Any(form => form.HasPending());
            var cancelMemberFormCleared = editName.Text.Length == 0 && editInitial.Text == "FALSE"
                && editType.Selected == 0 && cancelMember.Disabled;
            var cancelMemberUndoUnchanged = undoBeforeCancelMember == _ladderHistory.CanUndo;
            var cancelMemberIsAtomic = cancelMemberSerializedUnchanged && cancelMemberNoPendingForm
                && cancelMemberUndoUnchanged && cancelMemberFormCleared;

            var labelFb = _ladderDocument.AddBlock("MemberFormContextFB", LadderBlockType.FunctionBlock);
            var labelFbIndex = _ladderDocument.Blocks.IndexOf(labelFb);
            Refresh(); await Frames();
            await Activate($"program:{labelFbIndex}");
            var fbMemberLabels = applyDeclaration.Text == "+ PARAMETER"
                && newMemberButton.Text == "NEW"
                && editSection.Selected == (int)LadderInterfaceSection.Input
                && !editRetain.Visible && !editComment.Visible;
            await Activate($"program:{globalIndex}");
            var globalMemberLabelsAfterReturn = applyDeclaration.Text == "+ MEMBER"
                && newMemberButton.Text == "NEW MEMBER"
                && editSection.Selected == (int)LadderInterfaceSection.Static
                && editRetain.Visible && editComment.Visible;
            var formContextLabels = globalMemberLabelsOnEntry && fbMemberLabels && globalMemberLabelsAfterReturn;

            // A selected row belongs to one specific block. Switching blocks
            // must clear the edit form so its old row index cannot target a
            // declaration at the same index in a different DB.
            var secondGlobalDb = _ladderDocument.AddBlock("SecondGlobalData", LadderBlockType.DataBlock);
            _ladderDocument.AddInterfaceParameter(secondGlobalDb.Id, "PressureLimit", PlcVariableType.Real,
                LadderInterfaceSection.Static, 2300.0, comment: "Second DB member");
            var secondGlobalIndex = _ladderDocument.Blocks.IndexOf(secondGlobalDb);
            Refresh(); await Frames();
            var globalDeclarationRow = FindMetadata(declarationsTable.GetRoot(), targetSpeedId);
            globalDeclarationRow?.Select(0);
            declarationsTable.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            var selectionLoadedBeforeSwitch = applyDeclaration.Text == "APPLY MEMBER"
                && editName.Text == "TargetSpeedEdited";
            var secondDbDeclarationBeforeSwitch = JsonSerializer.Serialize(secondGlobalDb.Interface[0]);
            await Activate($"program:{secondGlobalIndex}");
            var editSelectionClearedOnSwitch = editName.Text.Length == 0
                && applyDeclaration.Text == "+ MEMBER"
                && editSection.Selected == (int)LadderInterfaceSection.Static;
            applyDeclaration.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var otherDbUnchangedOnSwitch = JsonSerializer.Serialize(secondGlobalDb.Interface[0]) == secondDbDeclarationBeforeSwitch
                && secondGlobalDb.Interface.SingleOrDefault(parameter => parameter.Name == "PressureLimit") is
                    { InitialValue: double pressureLimit, Comment: "Second DB member" } && pressureLimit == 2300.0;
            await Activate($"program:{globalIndex}");

            var rungCount = globalDb.Rungs.Count;
            Find<Button>("AddRung").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var ladRejected = globalDb.Rungs.Count == rungCount && output.Text.Contains("declarations", StringComparison.OrdinalIgnoreCase);
            var callTargets = Find<OptionButton>("CallTarget");
            var taskTargets = Find<OptionButton>("TaskTarget");
            bool Excluded(OptionButton selector) => !Enumerable.Range(0, selector.ItemCount)
                .Any(index => selector.GetItemMetadata(index).AsString() == globalDb.Id);
            var executableExcluded = Excluded(callTargets) && Excluded(taskTargets)
                && _ladderDocument.EntryBlockId != globalDb.Id;
            var currentGridDeclaration = FindMetadata(globalDbDeclarations.GetRoot(), targetSpeedId);
            var currentGridEditability = currentGridDeclaration is not null
                && Enumerable.Range(0, globalDbDeclarations.Columns).All(currentGridDeclaration.IsEditable);
            globalGate.Set("row-under-program-blocks", globalRowUnderBlocks, $"row={globalRowName}");
            globalGate.Set("block-number-not-inferred-from-row-order", globalDbLabelHasNoInventedNumber,
                $"tree={globalRowName} tab={title.Text}");
            globalGate.Set("activation-opens-declarations", globalDbDocument.IsVisibleInTree() && !canvas.IsVisibleInTree(), $"db={globalDbDocument.IsVisibleInTree()} lad={canvas.IsVisibleInTree()}");
            globalGate.Set("empty-member-name-rejected", emptyMemberRejected,
                $"visibleFeedback={emptyMemberFeedback} focus={emptyMemberFocused} documentUnchanged={emptyMemberDocumentUnchanged} undoUnchanged={emptyMemberUndoUnchanged}");
            globalGate.Set("fields-persist", fieldPersisted && declarationEditLifecycle,
                $"original={originalDeclarationText} selected={selectedDeclarationLoaded} edited={declarationEdited} undo={declarationUndoExact} redo={declarationRedoExact}");
            globalGate.Set("declaration-grid-inline-edit", inlineGridFieldsEditable,
                $"editable={currentGridEditability} retainMode={currentGridDeclaration?.GetCellMode(3)} committed={inlineCommentCommitted} value={inlineCommentValue} undo={inlineCommentUndo} redo={inlineCommentRedo} saveLoad={inlineCommentSavedLoaded} duplicateRejected={duplicateInlineRejected} blankRejected={blankInlineRejected} restored={inlineCommentRestored}");
            globalGate.Set("pending-form-blocks-inline-edit", inlineGridBlockedByMemberDraft,
                $"pendingForms={pendingFormCountDuringGridCheck} editable={pendingDraftCellEditability}");
            globalGate.Set("member-draft-cancel-restores-inline-edit", inlineGridRestoredAfterMemberCancel,
                "Cancel discards the draft without changing the DB and restores cell editability");
            globalGate.Set("member-selection-stable-id", selectedDeclarationSurvivesReorder,
                $"selected={selectedDeclarationLoaded} survivesSameDbPrepend={selectedDeclarationSurvivesReorder} id={targetSpeedId}");
            globalGate.Set("new-member-resolves-pending-form", newMemberWaitsForResolution && newMemberCancelKeepsDraft
                    && newMemberDiscardDialogReopened && newMemberDiscardIsAtomic,
                $"waits={newMemberWaitsForResolution} cancelKeepsDraft={newMemberCancelKeepsDraft} discardDialog={newMemberDiscardDialogReopened} discardedAtomically={newMemberDiscardIsAtomic}");
            globalGate.Set("type-change-seeds-valid-initial", numericDefaultIsValid,
                $"selectedType={editType.GetItemText(1)} initial={numericDefaultIsValid}");
            editType.Select(7);
            editType.EmitSignal(OptionButton.SignalName.ItemSelected, 7L);
            var enumMembers = editEnumMembers.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var enumDefaultIsDomainMember = enumMembers.Length > 0
                && LadderEditorDocument.TryParseInitialValue(PlcVariableType.Enum, editInitial.Text, out var enumInitial, out _)
                && enumMembers.Contains(enumInitial.ToString(), StringComparer.Ordinal);
            globalGate.Set("enum-default-is-domain-member", enumDefaultIsDomainMember,
                $"members={editEnumMembers.Text} initial={editInitial.Text}");
            cancelMember.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();

            var beforeEnumApply = LadderEditorProjectJson.Save(_ladderDocument);
            editName.Text = "EnumDomainMember";
            editName.EmitSignal(LineEdit.SignalName.TextChanged, editName.Text);
            editType.Select(7);
            editType.EmitSignal(OptionButton.SignalName.ItemSelected, 7L);
            editEnumMembers.Text = "Stopped,Running";
            editEnumMembers.EmitSignal(LineEdit.SignalName.TextChanged, editEnumMembers.Text);
            var enumDomainReseedsInitial = editInitial.Text == "Stopped";
            applyDeclaration.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var appliedEnum = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id).Interface
                .SingleOrDefault(parameter => parameter.Name == "EnumDomainMember");
            var validEnumDomainApplied = enumDomainReseedsInitial
                && appliedEnum?.Type == PlcVariableType.Enum
                && appliedEnum.InitialValue is string appliedEnumInitial && appliedEnumInitial == "Stopped"
                && appliedEnum.EnumMembers?.SequenceEqual(["Stopped", "Running"], StringComparer.Ordinal) == true;
            Find<Button>("LadderUndo").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var enumUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == beforeEnumApply;
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);

            var beforeInvalidEnum = LadderEditorProjectJson.Save(_ladderDocument);
            var undoBeforeInvalidEnum = _ladderHistory.CanUndo;
            editName.Text = "InvalidEnumDomain";
            editName.EmitSignal(LineEdit.SignalName.TextChanged, editName.Text);
            editType.Select(7);
            editType.EmitSignal(OptionButton.SignalName.ItemSelected, 7L);
            editEnumMembers.Text = "Not valid";
            editEnumMembers.EmitSignal(LineEdit.SignalName.TextChanged, editEnumMembers.Text);
            applyDeclaration.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var invalidEnumRejectedAtomically = LadderEditorProjectJson.Save(_ladderDocument) == beforeInvalidEnum
                && undoBeforeInvalidEnum == _ladderHistory.CanUndo
                && output.Text.Contains("ENUM members must be", StringComparison.Ordinal);
            cancelMember.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            globalGate.Set("enum-domain-edit-validates-atomically", validEnumDomainApplied && enumUndoExact && invalidEnumRejectedAtomically,
                $"reseeds={enumDomainReseedsInitial} applied={validEnumDomainApplied} undo={enumUndoExact} invalidRejected={invalidEnumRejectedAtomically}");

            var interfaceTable = Find<Tree>("BlockInterface");
            var existingRetainRow = FindMetadata(interfaceTable.GetRoot(), targetSpeedId);
            existingRetainRow?.Select(0);
            interfaceTable.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            var beforeExistingRetainOnlyEdit = LadderEditorProjectJson.Save(_ladderDocument);
            var undoBeforeExistingRetainOnlyEdit = _ladderHistory.CanUndo;
            editRetain.ButtonPressed = true;
            editRetain.EmitSignal(BaseButton.SignalName.Toggled, true);
            newMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            var existingRetainPrompts = editName.Text == "TargetSpeedEdited"
                && editRetain.ButtonPressed
                && _pendingEditorDialog?.Visible == true
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeExistingRetainOnlyEdit;
            _pendingEditorDialog?.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            _pendingEditorDialog?.Hide();
            var existingRetainDraftKeeps = editRetain.ButtonPressed
                && _pendingEditorForms.Any(form => form.HasPending())
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeExistingRetainOnlyEdit;
            cancelMember.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var existingRetainCancelRestores = !editRetain.ButtonPressed
                && !_pendingEditorForms.Any(form => form.HasPending())
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeExistingRetainOnlyEdit
                && undoBeforeExistingRetainOnlyEdit == _ladderHistory.CanUndo;

            var beforeRetainOnlyEdit = LadderEditorProjectJson.Save(_ladderDocument);
            var undoBeforeRetainOnlyEdit = _ladderHistory.CanUndo;
            editRetain.ButtonPressed = true;
            editRetain.EmitSignal(BaseButton.SignalName.Toggled, true);
            newMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            var retainOnlyPrompts = editRetain.ButtonPressed
                && _pendingEditorDialog?.Visible == true
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeRetainOnlyEdit;
            _pendingEditorDialog?.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            _pendingEditorDialog?.Hide();
            var retainOnlyCancelKeepsDraft = editRetain.ButtonPressed
                && _pendingEditorForms.Any(form => form.HasPending())
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeRetainOnlyEdit;
            cancelMember.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var retainOnlyCancelRestoresForm = !editRetain.ButtonPressed
                && !_pendingEditorForms.Any(form => form.HasPending())
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeRetainOnlyEdit
                && undoBeforeRetainOnlyEdit == _ladderHistory.CanUndo;
            globalGate.Set("retain-only-draft-prompts", existingRetainPrompts && existingRetainDraftKeeps && existingRetainCancelRestores
                    && retainOnlyPrompts && retainOnlyCancelKeepsDraft && retainOnlyCancelRestoresForm,
                $"existingPrompt={existingRetainPrompts} existingDraftPreserved={existingRetainDraftKeeps} existingCancelRestored={existingRetainCancelRestores}; newPrompt={retainOnlyPrompts} newDraftPreserved={retainOnlyCancelKeepsDraft} newCancelRestored={retainOnlyCancelRestoresForm}");

            // Remove must not silently throw away an unapplied member draft.
            // Cancel keeps both the row and draft; Discard continues the delete,
            // and one Undo/Redo must restore the exact serialized states.
            var removeTargetRow = globalDbDeclarations.GetRoot()?.GetFirstChild();
            while (removeTargetRow is not null && removeTargetRow.GetText(0) != "TargetSpeedEdited")
                removeTargetRow = removeTargetRow.GetNext();
            removeTargetRow?.Select(0);
            globalDbDeclarations.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            var beforePendingRemove = LadderEditorProjectJson.Save(_ladderDocument);
            var removeSurvivorsBefore = JsonSerializer.Serialize(globalDb.Interface
                .Where(parameter => !StringComparer.Ordinal.Equals(parameter.Id, targetSpeedId)).ToArray());
            editComment.Text = "Unapplied comment before remove";
            editComment.EmitSignal(LineEdit.SignalName.TextChanged, editComment.Text);
            var removeMemberButton = removeMemberAction;
            removeMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            var removeWaitsForResolution = _pendingEditorDialog?.Visible == true
                && _ladderDocument.Blocks.First(block => block.Id == globalDb.Id).Interface.Any(parameter => parameter.Name == "TargetSpeedEdited")
                && LadderEditorProjectJson.Save(_ladderDocument) == beforePendingRemove
                && editComment.Text == "Unapplied comment before remove";
            _pendingEditorDialog?.EmitSignal(ConfirmationDialog.SignalName.Canceled);
            _pendingEditorDialog?.Hide();
            var removeCancelKeepsDraft = _pendingEditorForms.Any(form => form.HasPending())
                && _ladderDocument.Blocks.First(block => block.Id == globalDb.Id).Interface.Any(parameter => parameter.Name == "TargetSpeedEdited")
                && LadderEditorProjectJson.Save(_ladderDocument) == beforePendingRemove;
            removeMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            var removeDiscardDialogReopened = _pendingEditorDialog?.Visible == true;
            _pendingEditorDialog?.EmitSignal(ConfirmationDialog.SignalName.CustomAction, "discard");
            _pendingEditorDialog?.Hide();
            await Frames();
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);
            var afterDiscardedRemove = LadderEditorProjectJson.Save(_ladderDocument);
            var removeDiscardDeletesOriginal = !globalDb.Interface.Any(parameter => parameter.Name == "TargetSpeedEdited")
                && !_pendingEditorForms.Any(form => form.HasPending())
                && globalDb.Interface.Count == 1
                && JsonSerializer.Serialize(globalDb.Interface) == removeSurvivorsBefore;
            Find<Button>("LadderUndo").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var removeUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == beforePendingRemove;
            Find<Button>("LadderRedo").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var removeRedoExact = LadderEditorProjectJson.Save(_ladderDocument) == afterDiscardedRemove;
            Find<Button>("LadderUndo").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var removeCleanupUndoExact = LadderEditorProjectJson.Save(_ladderDocument) == beforePendingRemove;
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);
            var removeResolvesPending = removeWaitsForResolution && removeCancelKeepsDraft
                && removeDiscardDialogReopened && removeDiscardDeletesOriginal && removeUndoExact && removeRedoExact && removeCleanupUndoExact;
            globalGate.Set("remove-resolves-pending-form", removeResolvesPending,
                $"waits={removeWaitsForResolution} cancelKeepsDraft={removeCancelKeepsDraft} discardDialog={removeDiscardDialogReopened} removed={removeDiscardDeletesOriginal} undo={removeUndoExact} redo={removeRedoExact} cleanup={removeCleanupUndoExact}");

            var ordinaryRemoveRow = FindMetadata(globalDbDeclarations.GetRoot(), "gate-reorder-marker");
            ordinaryRemoveRow?.Select(0);
            globalDbDeclarations.EmitSignal(Tree.SignalName.ItemSelected);
            await Frames();
            var beforeOrdinaryRemove = LadderEditorProjectJson.Save(_ladderDocument);
            var ordinaryRemoveSurvivorsBefore = JsonSerializer.Serialize(globalDb.Interface
                .Where(parameter => !StringComparer.Ordinal.Equals(parameter.Id, "gate-reorder-marker")).ToArray());
            var ordinaryRemoveHadNoDraft = !_pendingEditorForms.Any(form => form.HasPending());
            var ordinaryRemoveTargetSelected = globalDbDeclarations.GetSelected()?.GetMetadata(0).AsString() == "gate-reorder-marker";
            _ladderHistory.Clear();
            removeMemberButton.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);
            var afterOrdinaryRemove = LadderEditorProjectJson.Save(_ladderDocument);
            var ordinaryRemoveTargetGone = !globalDb.Interface.Any(parameter => parameter.Id == "gate-reorder-marker");
            var ordinaryRemoveSurvivorExact = globalDb.Interface.Count == 1
                && globalDb.Interface.Single().Id == targetSpeedId
                && JsonSerializer.Serialize(globalDb.Interface) == ordinaryRemoveSurvivorsBefore;
            var currentSecondGlobalDb = _ladderDocument.Blocks.First(block => block.Id == secondGlobalDb.Id);
            var ordinaryRemoveOtherDbExact = JsonSerializer.Serialize(currentSecondGlobalDb.Interface[0]) == secondDbDeclarationBeforeSwitch;
            var ordinaryRemoveExact = ordinaryRemoveTargetSelected && ordinaryRemoveHadNoDraft
                && !_pendingEditorForms.Any(form => form.HasPending())
                && ordinaryRemoveTargetGone && ordinaryRemoveSurvivorExact && ordinaryRemoveOtherDbExact;
            var ordinaryRemoveUndo = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeOrdinaryRemove;
            var ordinaryRemoveRedo = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == afterOrdinaryRemove;
            var ordinaryRemoveCleanup = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeOrdinaryRemove;
            globalDb = _ladderDocument.Blocks.First(block => block.Id == globalDb.Id);
            globalGate.Set("ordinary-remove-preserves-survivors", ordinaryRemoveExact && ordinaryRemoveUndo
                    && ordinaryRemoveRedo && ordinaryRemoveCleanup,
                $"selected={ordinaryRemoveTargetSelected} noDraft={ordinaryRemoveHadNoDraft} targetGone={ordinaryRemoveTargetGone} survivorExact={ordinaryRemoveSurvivorExact} otherDbExact={ordinaryRemoveOtherDbExact} undo={ordinaryRemoveUndo} redo={ordinaryRemoveRedo} cleanup={ordinaryRemoveCleanup} survivors={globalDb.Interface.Count}");

            globalGate.Set("cancel-member-form", cancelMemberIsAtomic,
                $"serializedUnchanged={cancelMemberSerializedUnchanged} noPending={cancelMemberNoPendingForm} cleared={cancelMemberFormCleared} undoUnchanged={cancelMemberUndoUnchanged}");
            globalGate.Set("form-context-labels", formContextLabels,
                $"initialDb={globalMemberLabelsOnEntry} fb={fbMemberLabels} returnedDb={globalMemberLabelsAfterReturn}");
            globalGate.Set("member-editor-route", memberEditorRouteVisible && memberEditorOpened && projectTreeRestored,
                $"visible={memberEditorRouteVisible} opened={memberEditorOpened} requested={requestedMemberDockWidth}/{expectedMemberDockWidth} actual={actualMemberDockWidth} "
                + $"wide={wideDockReached} wrapped={memberActionsWrap} actionsFit={memberActionsFitDock} from=240 restored={projectTreeRestored}:{restoredDockWidth}/240 actual={restoredActualDockWidth}/{baselineMemberDockWidth} "
                + $"openTab={memberEditorTab} openButton={memberEditorButtonText} dock={projectDock.Visible} interface={memberEditorTreeVisible} "
                + $"split={projectSplit.Size.X}");
            globalGate.Set("declaration-grid-actions", directGridActions && removeResolvesPending,
                $"addOpensDraft={directAddOpened} addIsAtomic={directAddIsDraftOnly} editLoadsSelection={directEditLoaded} removeResolvesDraft={removeResolvesPending}");
            globalGate.Set("edit-selection-block-scoped", selectionLoadedBeforeSwitch && editSelectionClearedOnSwitch,
                $"loadedBeforeSwitch={selectionLoadedBeforeSwitch} clearedOnSwitch={editSelectionClearedOnSwitch} section={editSection.GetItemText(editSection.Selected)}");
            globalGate.Set("block-switch-does-not-mutate", otherDbUnchangedOnSwitch,
                $"serializedUnchanged={otherDbUnchangedOnSwitch} secondDb={secondGlobalDb.Name}");
            globalGate.Set("lad-network-rejected", ladRejected, $"rungs={rungCount}->{globalDb.Rungs.Count}");
            globalGate.Set("excluded-from-executable-targets", executableExcluded, $"call={Excluded(callTargets)} task={Excluded(taskTargets)} entry={_ladderDocument.EntryBlockId != globalDb.Id}");
            globalGate.Finish();
            _ladderDocument.RestoreSnapshot(globalFixture); Refresh(); await Frames();

            // Generated IEC instance DB contract.
            var instanceGate = Gate("tia.project-tree.instance-db-contract");
            var instanceFixture = _ladderDocument.CaptureSnapshot();
            var ownerIndex = _ladderDocument.ActiveBlockIndex;
            var firstRung = _ladderDocument.Rungs[0];
            firstRung.IsCounter = true; firstRung.CounterVariable = "Structured_Count";
            firstRung.CounterInstanceDataBlock = new LadderInstanceDataBlock(731, "Structured_Count_DB");
            var second = _ladderDocument.AddRung("Second instance", "output_1");
            second.IsTimer = true; second.TimerVariable = "Structured_Timer";
            second.TimerInstanceDataBlock = new LadderInstanceDataBlock(732, "Structured_Timer_DB");
            Refresh(); await Frames();
            var instanceRow = FindMetadata(tree.GetRoot(), "instance-db:731");
            var instanceRowName = instanceRow?.GetText(0) ?? "<missing>";
            var parentIsBlocks = instanceRow?.GetParent()?.GetMetadata(0).AsString() == "blocks";
            await SelectOnly("instance-db:731");
            var selectionClosed = !instanceDbDocument.IsVisibleInTree();
            await Activate("instance-db:731");
            var exactDefinition = instanceDbDocument.IsVisibleInTree()
                && instanceDbHeading.Text.Contains("Structured_Count_DB", StringComparison.Ordinal)
                && instanceDbHeading.Text.Contains("DB731", StringComparison.Ordinal)
                && instanceDbDeclarations.GetRoot()?.GetFirstChild()?.GetFirstChild()?.GetText(0) == "Q";
            TreeItem? FindInstanceMember(string sectionName, string memberName)
            {
                for (var section = instanceDbDeclarations.GetRoot()?.GetFirstChild(); section is not null; section = section.GetNext())
                {
                    if (section.GetText(0) != sectionName) continue;
                    for (var member = section.GetFirstChild(); member is not null; member = member.GetNext())
                        if (member.GetText(0) == memberName) return member;
                }
                return null;
            }
            var counterQ = FindInstanceMember("STATIC", "Q");
            var counterCv = FindInstanceMember("STATIC", "CV");
            var counterMembersRendered = counterQ?.GetText(1) == "BOOL" && counterCv?.GetText(1) == "DINT";
            await Activate("instance-db:732");
            var timerQ = FindInstanceMember("STATIC", "Q");
            var timerEt = FindInstanceMember("STATIC", "ET");
            var timerPt = FindInstanceMember("STATIC", "PT");
            var timerIn = FindInstanceMember("STATIC", "IN");
            TreeItem? FindInstanceSection(string sectionName)
            {
                for (var section = instanceDbDeclarations.GetRoot()?.GetFirstChild(); section is not null; section = section.GetNext())
                    if (section.GetText(0) == sectionName) return section;
                return null;
            }
            var timerStaticSection = FindInstanceSection("STATIC");
            var timerMemberCount = 0;
            for (var member = timerStaticSection?.GetFirstChild(); member is not null; member = member.GetNext())
                timerMemberCount++;
            var timerMembersRendered = instanceDbHeading.Text.Contains("Structured_Timer_DB", StringComparison.Ordinal)
                && timerPt?.GetText(1) == "TIME" && timerEt?.GetText(1) == "TIME"
                && timerIn?.GetText(1) == "BOOL" && timerQ?.GetText(1) == "BOOL"
                && timerStaticSection?.GetFirstChild()?.GetText(0) == "PT"
                && timerPt?.GetNext()?.GetText(0) == "ET"
                && timerEt?.GetNext()?.GetText(0) == "IN"
                && timerIn?.GetNext()?.GetText(0) == "Q"
                && timerQ?.GetNext() is null && timerMemberCount == 4;
            var saveLoad = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(_ladderDocument)).Document;
            var dbs = _ladderDocument.Blocks.SelectMany(block => block.Rungs)
                .SelectMany(rung => new[] { rung.TimerInstanceDataBlock, rung.CounterInstanceDataBlock })
                .Where(db => db is not null).Select(db => db!).ToArray();
            var identityUnique = dbs.Select(db => db.Number).Distinct().Count() == dbs.Length
                && dbs.Select(db => db.Name).Distinct(StringComparer.Ordinal).Count() == dbs.Length
                && saveLoad?.Blocks.SelectMany(block => block.Rungs)
                    .Any(rung => rung.CounterInstanceDataBlock?.Number == 731 && rung.CounterInstanceDataBlock.Name == "Structured_Count_DB") == true;
            var fbFixture = _ladderDocument.AddBlock("StructuredInstanceFB", LadderBlockType.FunctionBlock);
            _ladderDocument.AddInterfaceParameter(fbFixture.Id, "Enable", PlcVariableType.Bool,
                LadderInterfaceSection.Input, false, comment: "FB default enable");
            _ladderDocument.AddInterfaceParameter(fbFixture.Id, "AtPosition", PlcVariableType.Bool,
                LadderInterfaceSection.Output, true, comment: "Position feedback");
            _ladderDocument.AddInterfaceParameter(fbFixture.Id, "ActualPosition", PlcVariableType.Real,
                LadderInterfaceSection.Static, 12.5, comment: "Measured position");
            _ladderDocument.AddInterfaceParameter(fbFixture.Id, "TemporaryScratch", PlcVariableType.Real,
                LadderInterfaceSection.Temp, 0.0, comment: "Invocation-local scratch");
            _ladderDocument.Instances.Add(new LadderInstance("structured_axis", "StructuredAxis", fbFixture.Id,
                new LadderInstanceDataBlock(733, "StructuredAxis_DB")));
            Refresh(); await Frames();
            await Activate("instance-db:733");
            var fbEnable = FindInstanceMember("INPUT", "Enable");
            var fbAtPosition = FindInstanceMember("OUTPUT", "AtPosition");
            var fbActualPosition = FindInstanceMember("STATIC", "ActualPosition");
            var fbMembersRendered = instanceDbDocument.IsVisibleInTree()
                && instanceDbDeclarations.GetColumnTitle(2) == "DEFAULT VALUE"
                && instanceDbDeclarations.GetColumnTitle(3) == "START VALUE"
                && fbEnable?.GetText(0) == "Enable"
                && fbEnable.GetText(1) == "BOOL"
                && fbEnable.GetText(2) == "FALSE"
                && fbEnable.GetText(3) == fbEnable.GetText(2)
                && fbEnable.GetTooltipText(3).Contains("Inherited FB default", StringComparison.Ordinal)
                && fbEnable.GetText(5) == "FB default enable"
                && fbAtPosition?.GetText(1) == "BOOL" && fbAtPosition.GetText(2) == "TRUE"
                && fbAtPosition.GetText(3) == "TRUE" && fbAtPosition.GetText(5) == "Position feedback"
                && fbActualPosition?.GetText(1) == "REAL" && fbActualPosition.GetText(2) == "12.5"
                && fbActualPosition.GetText(3) == "12.5" && fbActualPosition.GetText(5) == "Measured position"
                && instanceDbBoundary.Text.Contains("per-instance start/comment/retain overrides", StringComparison.Ordinal)
                && instanceDbBoundary.Text.Contains("power-cycle retention", StringComparison.Ordinal)
                && instanceDbBoundary.Text.Contains("standard-access offsets", StringComparison.Ordinal)
                && instanceDbBoundary.Text.Contains("monitor/snapshots", StringComparison.Ordinal)
                && instanceDbBoundary.Text.Contains("HMI attributes", StringComparison.Ordinal)
                && instanceDbBoundary.Text.Contains("supervision", StringComparison.Ordinal);
            var fbDefaultValue = fbEnable?.GetText(2) ?? "<missing>";
            var fbStartValue = fbEnable?.GetText(3) ?? "<missing>";
            var fbOutputDefault = fbAtPosition?.GetText(2) ?? "<missing>";
            var fbOutputStart = fbAtPosition?.GetText(3) ?? "<missing>";
            var fbStaticDefault = fbActualPosition?.GetText(2) ?? "<missing>";
            var fbStaticStart = fbActualPosition?.GetText(3) ?? "<missing>";
            var tempSectionRendered = false;
            for (var section = instanceDbDeclarations.GetRoot()?.GetFirstChild(); section is not null; section = section.GetNext())
                tempSectionRendered |= section.GetText(0) == "TEMP";
            var tempOnlyFb = _ladderDocument.AddBlock("StructuredTempOnlyFB", LadderBlockType.FunctionBlock);
            _ladderDocument.AddInterfaceParameter(tempOnlyFb.Id, "TemporaryScratch", PlcVariableType.Real,
                LadderInterfaceSection.Temp, 0.0, comment: "Invocation-local scratch");
            _ladderDocument.Instances.Add(new LadderInstance("structured_temp_only", "StructuredTempOnly", tempOnlyFb.Id,
                new LadderInstanceDataBlock(734, "StructuredTempOnly_DB")));
            Refresh(); await Frames();
            await Activate("instance-db:734");
            var tempOnlyEmptyState = instanceDbDeclarations.GetRoot()?.GetFirstChild()?.GetText(0)
                == "No declared FB interface members.";
            var readonlyDefinition = instanceDbDocument.IsVisibleInTree()
                && !globalDbDocument.IsVisibleInTree() && !canvas.IsVisibleInTree()
                && instanceDbBoundary.Text.StartsWith("Read-only offline instance view", StringComparison.Ordinal)
                && exactDefinition && counterMembersRendered && timerMembersRendered
                && fbMembersRendered && !tempSectionRendered && tempOnlyEmptyState;
            await Activate($"program:{ownerIndex}");
            var ownerReturned = canvas.IsVisibleInTree()
                && title.Text.Contains(_ladderDocument.Blocks[ownerIndex].Name, StringComparison.Ordinal)
                && !title.Text.Contains("Structured_Count_DB", StringComparison.Ordinal)
                && !title.Text.Contains("StructuredAxis_DB", StringComparison.Ordinal);
            instanceGate.Set("row-under-program-blocks", parentIsBlocks, $"row={instanceRowName}");
            instanceGate.Set("selection-versus-definition", selectionClosed && exactDefinition, $"selectionClosed={selectionClosed} exactDefinition={exactDefinition}");
            instanceGate.Set("identity-is-unique-and-persistent", identityUnique, $"instances={dbs.Length} saveLoad={saveLoad is not null}");
            instanceGate.Set("definition-is-read-only", readonlyDefinition,
                $"instance={instanceDbDocument.IsVisibleInTree()} globalDb={globalDbDocument.IsVisibleInTree()} ladder={canvas.IsVisibleInTree()} input={fbDefaultValue}/{fbStartValue} output={fbOutputDefault}/{fbOutputStart} static={fbStaticDefault}/{fbStaticStart} counter={counterMembersRendered} timer={timerMembersRendered} tempSection={tempSectionRendered} tempOnlyEmpty={tempOnlyEmptyState}");
            instanceGate.Set("owner-return-restores-editor", ownerReturned, $"title={title.Text} ladder={canvas.IsVisibleInTree()}");
            instanceGate.Finish();
            _ladderDocument.RestoreSnapshot(instanceFixture); Refresh(); await Frames();

            // CALL drag: broad route owns visible-terminal Cancel/Apply/Undo/Redo.
            // The structured addition proves tree payload filtering and invalid
            // source/target rejection without weakening that physical route.
            var dragGate = Gate("tia.project-tree.call-drag-drop");
            var dragFixture = _ladderDocument.CaptureSnapshot();
            var fc = _ladderDocument.AddBlock("StructuredFC", LadderBlockType.Function);
            var fb = _ladderDocument.AddBlock("StructuredFB", LadderBlockType.FunctionBlock);
            var db = _ladderDocument.AddBlock("StructuredDB", LadderBlockType.DataBlock);
            fc.SemanticMode = LadderSemanticMode.Parameterized;
            fb.SemanticMode = LadderSemanticMode.Parameterized;
            Refresh(); await Frames();
            var fcIndex = _ladderDocument.Blocks.IndexOf(fc);
            var fbIndex = _ladderDocument.Blocks.IndexOf(fb);
            var dbIndex = _ladderDocument.Blocks.IndexOf(db);
            var payloadFiltered = ProgramBlockDragPayload.FromTreeItem($"program:{fcIndex}", "StructuredFC [FC]") is not null
                && ProgramBlockDragPayload.FromTreeItem($"program:{fbIndex}", "StructuredFB [FB]") is not null
                && ProgramBlockDragPayload.FromTreeItem("program:0", "Main [OB]") is null
                && ProgramBlockDragPayload.FromTreeItem($"program:{dbIndex}", "StructuredDB [DB]") is null
                && ProgramBlockDragPayload.FromTreeItem("tag-table:", "Show all tags") is null;
            await Activate("program:0");
            canvas.SelectElement(0, -1, -1, output: true);
            var terminal = canvas.GetElementPosition(0, -1, -1, output: true);
            var validPayload = Variant.From(ProgramBlockDragPayload.FromTreeMetadata($"program:{fcIndex}")!);
            var invalidPayload = Variant.From(ProgramBlockDragPayload.FromTreeMetadata($"program:{dbIndex}")!);
            var terminalValidation = canvas._CanDropData(terminal, validPayload) && !canvas._CanDropData(terminal, invalidPayload);
            dragGate.Set("supported-rows-only", payloadFiltered, $"fc/fb accepted, ob/db/non-program rejected={payloadFiltered}");
            dragGate.Set("terminal-validates-payload", broadPass && terminalValidation, $"physicalVisible=broad validFcInvalidDb={terminalValidation} rejection={canvas.ReviewBlockDropRejection}");
            dragGate.Set("drop-opens-exact-call", broadPass, "physical broad route opened CALL authoring for exact FC source");
            dragGate.Set("cancel-is-exact", broadPass, "physical broad route exact JSON/no-history Cancel assertion");
            dragGate.Set("apply-undo-redo-exact", broadPass, "physical broad route exact Apply/Undo/Redo assertion");
            dragGate.Finish();
            _ladderDocument.RestoreSnapshot(dragFixture); Refresh(); await Frames();

            // Active-workspace-only watch-table deletion.
            var deleteGate = Gate("tia.project-tree.watch-table-delete");
            var deleteFixture = _ladderDocument.CaptureSnapshot();
            var firstName = _ladderDocument.WatchTables[0].Name;
            _ladderDocument.AddWatchTable("Delete Me").Variables.Add("delete-only");
            _ladderDocument.AddWatchTable("Survivor").Variables.Add("survivor-only");
            _ladderDocument.SelectWatchTable(1);
            Refresh(); await Frames();
            var removeWatch = Find<Button>("RemoveWatchTable");
            var beforeDelete = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderHistory.Clear();
            removeWatch.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var afterDelete = LadderEditorProjectJson.Save(_ladderDocument);
            var activeTargeted = _ladderDocument.WatchTables.All(table => table.Name != "Delete Me")
                && _ladderDocument.ActiveWatchTable.Name == "Survivor";
            var survivors = _ladderDocument.WatchTables.Count == 2
                && _ladderDocument.WatchTables[0].Name == firstName
                && _ladderDocument.WatchTables[1].Name == "Survivor"
                && _ladderDocument.WatchTables[1].Variables.SequenceEqual(new[] { "survivor-only" }, StringComparer.Ordinal);
            var undoDelete = TryUndoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == beforeDelete
                && _ladderDocument.ActiveWatchTable.Name == "Delete Me";
            var redoDelete = TryRedoLadderEdit(out _)
                && LadderEditorProjectJson.Save(_ladderDocument) == afterDelete
                && _ladderDocument.ActiveWatchTable.Name == "Survivor";
            _ladderDocument.RestoreSnapshot(deleteFixture); _ladderHistory.Clear(); Refresh(); await Frames();
            var finalBefore = LadderEditorProjectJson.Save(_ladderDocument);
            removeWatch = Find<Button>("RemoveWatchTable");
            var finalDisabled = removeWatch.Disabled;
            removeWatch.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames();
            var finalProtected = _ladderDocument.WatchTables.Count == 1
                && LadderEditorProjectJson.Save(_ladderDocument) == finalBefore && !_ladderHistory.CanUndo;
            var treeHasDeleteRoute = tree.GetChildren().OfType<PopupMenu>().Any()
                || FindMetadata(tree.GetRoot(), "delete-watch-table") is not null;
            deleteGate.Set("workspace-targets-active", activeTargeted, $"remainingActive={_ladderDocument.ActiveWatchTable.Name} targeted={activeTargeted}");
            deleteGate.Set("removes-one-preserves-survivors", survivors, $"count=2 survivors={survivors}");
            deleteGate.Set("undo-redo-restores-selection", undoDelete && redoDelete, $"undo={undoDelete} redo={redoDelete}");
            deleteGate.Set("final-table-protected", finalDisabled && finalProtected, $"disabled={finalDisabled} rejected={finalProtected}");
            deleteGate.Set("no-tree-row-delete", !treeHasDeleteRoute, $"treeDeleteRoute={treeHasDeleteRoute} supportedRoute=RemoveWatchTable workspace button");
            deleteGate.Finish();

            foreach (var gate in gates.Where(gate => gate.CompletedUtc == gate.StartedUtc)) gate.Finish();
        }
        catch (Exception error)
        {
            foreach (var gate in gates)
            {
                gate.FailPending(error.GetType().Name + ": " + error.Message);
                gate.Finish();
            }
            GD.PushError("TIA_PROJECT_TREE_GATE_SWEEP_CASE_FAIL " + error);
        }
        finally
        {
            // Restore the live editor's history before any cleanup that might
            // itself fail, so verifier teardown cannot strand isolated history.
            _ladderHistory = originalHistory;
            _callAuthoring?.CancelEdit();
            _pendingEditorDialog?.Hide();
            _ladderDocument.RestoreSnapshot(originalDocument);
            _ladderSavedProjectJson = originalSaved;
            _ladderProjectPath = originalPath;
            Refresh();
            await Frames();
        }

        var passed = gates.Count == 12
            && gates.Select(gate => gate.GateId).Distinct(StringComparer.Ordinal).Count() == 12
            && gates.All(gate => gate.Status == "passed"
                && gate.Assertions.Count == ProjectTreeRequirements[gate.GateId].Count()
                && gate.Assertions.All(assertion => assertion.Passed && assertion.Status == "passed"));
        var completed = DateTimeOffset.UtcNow.ToString("O");
        var artifact = new
        {
            schemaVersion = 1,
            resultKind = "tia-project-tree-gate-sweep",
            commandId = TiaProjectTreeCommandId,
            status = passed ? "passed" : "failed",
            sourceRevision,
            assemblySha256,
            startedUtc = sweepStarted,
            completedUtc = completed,
            externalTia = false,
            livePlc = false,
            installedTiaExecuted = false,
            livePlcExecuted = false,
            gateCount = gates.Count,
            assertionCount = gates.Sum(gate => gate.Assertions.Count),
            gates,
        };
        var outputPath = Path.GetFullPath(evidencePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(artifact, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        }), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var detail = $"TIA_PROJECT_TREE_GATE_SWEEP_{(passed ? "PASS" : "FAIL")} gates={gates.Count} assertions={gates.Sum(gate => gate.Assertions.Count)} externalTia=false livePlc=false artifact={outputPath}";
        return (passed, detail);
    }

    private static string ComputeSourceRevision()
    {
        var root = Directory.GetCurrentDirectory();
        var files = new[] { "src/App", "src/VirtualController" }
            .Select(path => Path.Combine(root, path))
            .Where(Directory.Exists)
            .SelectMany(path => Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
            .OrderBy(path => Path.GetRelativePath(root, path), StringComparer.Ordinal)
            .ToArray();
        if (files.Length == 0) throw new InvalidOperationException("Cannot compute source revision: no source files found from current directory.");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(root, file).Replace('\\', '/') + "\n"));
            hash.AppendData(File.ReadAllBytes(file));
            hash.AppendData([(byte)'\n']);
        }
        return "sha256:" + Convert.ToHexString(hash.GetHashAndReset());
    }
}
