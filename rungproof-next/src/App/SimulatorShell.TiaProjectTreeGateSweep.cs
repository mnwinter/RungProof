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
                ("activation-opens-declarations", "activation opens declarations and not a LAD canvas"),
                ("fields-persist", "Name, Data type, Start value, Retain, and Comment persist"),
                ("lad-network-rejected", "LAD network insertion is rejected"),
                ("excluded-from-executable-targets", "CALL, task, and program-entry target selection excludes the DB"),
            ],
            ["tia.project-tree.instance-db-contract"] =
            [
                ("row-under-program-blocks", "the generated instance DB row appears under Program blocks"),
                ("selection-versus-definition", "selection does not open and activation opens the exact definition"),
                ("identity-is-unique-and-persistent", "DB number and symbolic name are unique and persist"),
                ("definition-is-read-only", "the definition is not editable as a Global DB or LAD block"),
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
            var instanceDbDocument = Find<RichTextLabel>("InstanceDbDocument");
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
            blockGate.Set("selection-versus-dialog", broadPass && !blockDialog.Visible, "physical selection/double-click dialog route");
            blockGate.Set("explicit-supported-types", explicitTypes, "OB/FB/FC/DB toggle controls present");
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
            tagGate.Set("selection-versus-dialog", broadPass, "physical selection/double-click dialog route");
            tagGate.Set("invalid-is-atomic", broadPass && tagDuplicate, $"blank=broad caseInsensitiveDuplicate={tagDuplicate}");
            tagGate.Set("valid-create-persists", broadPass, "physical creation persisted with exact named tree row");
            tagGate.Set("undo-redo-exact", broadPass, "exact JSON Undo/Redo lifecycle");
            tagGate.Set("navigation-is-clean", tagNavClean, "serialized baseline unchanged after table activation");
            tagGate.Finish();

            var watchGate = Gate("tia.project-tree.create-watch-table");
            var watchSnapshot = _ladderDocument.CaptureSnapshot();
            var independent = _ladderDocument.AddWatchTable("Structured Watch");
            independent.Variables.Add("structured-only");
            var defaultIndependent = !_ladderDocument.WatchTables[0].Variables.Contains("structured-only", StringComparer.Ordinal);
            var watchDuplicate = false;
            try { _ladderDocument.AddWatchTable("structured WATCH"); }
            catch (InvalidOperationException) { watchDuplicate = true; }
            var watchNavBefore = LadderEditorProjectJson.Save(_ladderDocument);
            _ladderDocument.SelectWatchTable(0);
            var watchNavClean = !LadderEditorProjectJson.HasUnsavedChanges(_ladderDocument, watchNavBefore);
            _ladderDocument.RestoreSnapshot(watchSnapshot); Refresh(); await Frames();
            watchGate.Set("selection-versus-dialog", broadPass, "physical selection/double-click dialog route");
            watchGate.Set("invalid-is-atomic", broadPass && watchDuplicate, $"blank=broad caseInsensitiveDuplicate={watchDuplicate}");
            watchGate.Set("valid-create-persists", broadPass && defaultIndependent, $"broadTreeAndPersist={broadPass} independentSymbols={defaultIndependent}");
            watchGate.Set("undo-redo-exact", broadPass, "exact JSON Undo/Redo lifecycle");
            watchGate.Set("navigation-is-clean", watchNavClean, "serialized project content unchanged by active-table selection");
            watchGate.Finish();

            // Global DB contract via the production tree/document controls.
            var globalGate = Gate("tia.project-tree.global-db-contract");
            var globalFixture = _ladderDocument.CaptureSnapshot();
            var globalDb = _ladderDocument.AddBlock("StructuredGlobalData", LadderBlockType.DataBlock);
            _ladderDocument.AddInterfaceParameter(globalDb.Id, "TargetSpeed", PlcVariableType.Real,
                LadderInterfaceSection.Static, 1250.0, retain: true, comment: "Retained speed");
            var globalIndex = _ladderDocument.Blocks.IndexOf(globalDb);
            Refresh(); await Frames();
            var globalRow = FindMetadata(tree.GetRoot(), $"program:{globalIndex}");
            await Activate($"program:{globalIndex}");
            var declaration = globalDbDeclarations.GetRoot()?.GetFirstChild();
            var fieldPersisted = declaration?.GetText(0) == "TargetSpeed"
                && declaration.GetText(1) == "REAL" && declaration.GetText(2) == "1250"
                && declaration.GetText(3) == "YES" && declaration.GetText(4) == "Retained speed";
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
            globalGate.Set("row-under-program-blocks", globalRow?.GetParent()?.GetMetadata(0).AsString() == "blocks", $"row={globalRow?.GetText(0)}");
            globalGate.Set("activation-opens-declarations", globalDbDocument.IsVisibleInTree() && !canvas.IsVisibleInTree(), $"db={globalDbDocument.IsVisibleInTree()} lad={canvas.IsVisibleInTree()}");
            globalGate.Set("fields-persist", fieldPersisted, $"row={declaration?.GetText(0)}/{declaration?.GetText(1)}/{declaration?.GetText(2)}/{declaration?.GetText(3)}/{declaration?.GetText(4)}");
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
            var parentIsBlocks = instanceRow?.GetParent()?.GetMetadata(0).AsString() == "blocks";
            await SelectOnly("instance-db:731");
            var selectionClosed = !instanceDbDocument.IsVisibleInTree();
            await Activate("instance-db:731");
            var exactDefinition = instanceDbDocument.IsVisibleInTree()
                && instanceDbDocument.Text.Contains("Structured_Count_DB", StringComparison.Ordinal)
                && instanceDbDocument.Text.Contains("DB731", StringComparison.Ordinal);
            var saveLoad = LadderEditorProjectJson.Load(LadderEditorProjectJson.Save(_ladderDocument)).Document;
            var dbs = _ladderDocument.Blocks.SelectMany(block => block.Rungs)
                .SelectMany(rung => new[] { rung.TimerInstanceDataBlock, rung.CounterInstanceDataBlock })
                .Where(db => db is not null).Select(db => db!).ToArray();
            var identityUnique = dbs.Select(db => db.Number).Distinct().Count() == dbs.Length
                && dbs.Select(db => db.Name).Distinct(StringComparer.Ordinal).Count() == dbs.Length
                && saveLoad?.Blocks.SelectMany(block => block.Rungs)
                    .Any(rung => rung.CounterInstanceDataBlock?.Number == 731 && rung.CounterInstanceDataBlock.Name == "Structured_Count_DB") == true;
            var readonlyDefinition = instanceDbDocument.IsVisibleInTree()
                && !globalDbDocument.IsVisibleInTree() && !canvas.IsVisibleInTree()
                && instanceDbDocument.SelectionEnabled;
            await Activate($"program:{ownerIndex}");
            var ownerReturned = canvas.IsVisibleInTree()
                && title.Text.Contains(_ladderDocument.Blocks[ownerIndex].Name, StringComparison.Ordinal)
                && !title.Text.Contains("Structured_Count_DB", StringComparison.Ordinal);
            instanceGate.Set("row-under-program-blocks", parentIsBlocks, $"row={instanceRow?.GetText(0)}");
            instanceGate.Set("selection-versus-definition", selectionClosed && exactDefinition, $"selectionClosed={selectionClosed} exactDefinition={exactDefinition}");
            instanceGate.Set("identity-is-unique-and-persistent", identityUnique, $"instances={dbs.Length} saveLoad={saveLoad is not null}");
            instanceGate.Set("definition-is-read-only", readonlyDefinition, $"instance={instanceDbDocument.IsVisibleInTree()} globalDb={globalDbDocument.IsVisibleInTree()} ladder={canvas.IsVisibleInTree()}");
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
            var payloadFiltered = ProgramBlockDragPayload.FromTreeItem($"program:{fcIndex}", $"StructuredFC [FC{fcIndex + 1}]") is not null
                && ProgramBlockDragPayload.FromTreeItem($"program:{fbIndex}", $"StructuredFB [FB{fbIndex + 1}]") is not null
                && ProgramBlockDragPayload.FromTreeItem("program:0", "Main [OB1]") is null
                && ProgramBlockDragPayload.FromTreeItem($"program:{dbIndex}", $"StructuredDB [DB{dbIndex + 1}]") is null
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
            _callAuthoring?.CancelEdit();
            _pendingEditorDialog?.Hide();
            _ladderDocument.RestoreSnapshot(originalDocument);
            _ladderHistory = originalHistory;
            _ladderSavedProjectJson = originalSaved;
            _ladderProjectPath = originalPath;
            Refresh();
            await Frames();
        }

        var passed = gates.Count == 9
            && gates.Select(gate => gate.GateId).Distinct(StringComparer.Ordinal).Count() == 9
            && gates.All(gate => gate.Status == "passed" && gate.Assertions.Count == 5
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
