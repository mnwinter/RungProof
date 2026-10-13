using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    /// <summary>Native Godot signal/input route regressions; visual acceptance remains separate.</summary>
    public bool VerifyEditorCommandRoutes(out string detail)
    {
        const string path = "Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/TIA Portal";
        var host = GetNode<Control>(path);
        T Find<T>(string name) where T : Node => host.FindChild(name, true, false) as T
            ?? throw new InvalidOperationException("Missing editor control " + name);
        var snapshot = _ladderDocument.CaptureSnapshot(); var history = _ladderHistory.Copy();
        var programBefore = _virtualProgram; var runtimeBefore = _virtualSnapshot;
        var clipboardBefore = _ladderCommandClipboard;
        var contactClipboardBefore = _ladderContactClipboard; var networkClipboardBefore = _ladderRungClipboard;
        var assertions = new List<string>();
        void Require(string name, bool passed)
        {
            GD.Print($"EDITOR_COMMAND_ROUTE {name} {(passed ? "PASS" : "FAIL")}");
            if (!passed) throw new InvalidOperationException(name);
            assertions.Add(name);
        }
        void Refresh() { foreach (var refresh in _ladderEditorRefreshers) refresh(); }
        var panels = new[] { Find<Control>("ProjectDockHost"), Find<Control>("ProjectOrganization"),
            Find<Control>("BottomDockHost"), Find<Control>("InspectorOutputDock") };
        var priorVisibility = panels.Select(panel => panel.Visible).ToArray();
        try
        {
            _ladderDocument.ReplaceFromProgram(LadderEditorDocument.CreateConveyorExample().BuildProgram());
            _ladderHistory.Clear(); Refresh();
            var canvas = Find<LadderEditorCanvas>("GraphicalLadderCanvas");
            var menu = Find<PopupMenu>("InstructionContextMenu");
            var edit = Find<MenuButton>("LadderEditMenu").GetPopup();
            var view = Find<MenuButton>("LadderViewMenu").GetPopup();
            var output = Find<RichTextLabel>("OutputWindow");
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
            Find<MenuButton>("LadderToolsMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, 0L);
            var tagName = Find<LineEdit>("NewTagName"); tagName.Text = "verify_bool_menu";
            Find<Button>("AddTag").EmitSignal(BaseButton.SignalName.Pressed);
            Require("BoolMenuResetsPendingType", tagType.Selected == 0 && _ladderDocument.Tags.Any(tag => tag.Name == "verify_bool_menu" && tag.Type == PlcVariableType.Bool));
            TryUndoLadderEdit(out _); Refresh();

            void NamedCreation(string buttonName, string dialogName, string customName)
            {
                _ladderHistory.Clear(); var before = LadderEditorProjectJson.Save(_ladderDocument);
                Find<Button>(buttonName).EmitSignal(BaseButton.SignalName.Pressed);
                var form = Find<ConfirmationDialog>(dialogName);
                var name = form.FindChild("CreationName", true, false) as LineEdit ?? throw new InvalidOperationException("Creation name missing");
                var error = form.FindChild("CreationError", true, false) as Label ?? throw new InvalidOperationException("Creation error missing");
                name.Text = " "; form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(dialogName + "BlankRejected", before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo && error.Text.Length > 0 && form.Visible);
                name.Text = buttonName == "AddBlock" ? _ladderDocument.Blocks[0].Name : _ladderDocument.Tasks[0].Name;
                form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(dialogName + "DuplicateRejected", before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo && error.Text.Length > 0);
                name.Text = customName; form.EmitSignal(AcceptDialog.SignalName.Canceled); form.Hide();
                Require(dialogName + "CancelUnchanged", before == LadderEditorProjectJson.Save(_ladderDocument) && !_ladderHistory.CanUndo);
                Find<Button>(buttonName).EmitSignal(BaseButton.SignalName.Pressed);
                name.Text = customName; form.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                Require(dialogName + "NamedCreation", buttonName == "AddBlock" ? _ladderDocument.Blocks[^1].Name == customName : _ladderDocument.Tasks[^1].Name == customName);
                TryUndoLadderEdit(out _); Refresh();
            }
            NamedCreation("AddBlock", "CreateBlockDialog", "VerifierNamedFC");
            NamedCreation("AddTask", "CreateTaskDialog", "VerifierNamedOB");

            var deletionFixture = _ladderDocument.CaptureSnapshot();
            foreach (var kind in new[] { "coil", "counter", "call" })
            foreach (var route in new[] { "context", "toolbar", "keyboard" })
            {
                _ladderDocument.RestoreSnapshot(deletionFixture);
                var terminal = _ladderDocument.Rungs[0];
                terminal.IsCounter = kind == "counter"; terminal.IsCall = kind == "call";
                var beforeRoot = terminal.Root; var terminalId = terminal.TerminalActionId;
                _ladderHistory.Clear(); Refresh();
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
                TryUndoLadderEdit(out _); Refresh();
                Require("OutputDeleteUndo_" + kind + "_" + route, _ladderDocument.Rungs[0].HasTerminalAction
                    && _ladderDocument.Rungs[0].TerminalActionId == terminalId);
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
                { if (row.GetMetadata(0).AsString() == "verify_drive.Ready") return row; var child = Search(row.GetFirstChild()); if (child is not null) return child; }
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
            _ladderContactClipboard = contactClipboardBefore; _ladderRungClipboard = networkClipboardBefore;
            _virtualProgram = programBefore; _virtualSnapshot = runtimeBefore; Refresh(); RefreshLadderMonitorMatch();
            for (var i = 0; i < panels.Length; i++) panels[i].Visible = priorVisibility[i];
        }
    }
}
