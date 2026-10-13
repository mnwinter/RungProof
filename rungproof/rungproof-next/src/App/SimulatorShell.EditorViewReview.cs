using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class SimulatorShell
{
    private long _blockViewRememberCount;

    public async Task<bool> VerifyLadderBlockViewStateAsync()
    {
        var original = _ladderDocument.CaptureSnapshot(); var originalHistory = _ladderHistory;
        var originalView = _ladderCanvases.Select(c => c.CaptureViewState(0, 0)).ToArray();
        PendingEditorForm? syntheticPending = null;
        async Task Frames() { for (var frame = 0; frame < 4; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        void Check(bool ok, string name) { GD.Print($"BLOCK_VIEW_CHECK {name}={ok}"); if (!ok) throw new InvalidOperationException(name); }
        LadderNetwork Network(string prefix) => new(prefix + "-network", "Stable view fixture", new(prefix + "-root", LadderNodeKind.Series, Children: [
            new(prefix + "-choice", LadderNodeKind.Parallel, Children: [
                new(prefix + "-left", LadderNodeKind.Series, Children: [new(prefix + "-left-leaf", LadderNodeKind.Contact, "Input_a")]),
                new(prefix + "-right", LadderNodeKind.Series, Children: [new(prefix + "-right-leaf", LadderNodeKind.Contact, "Input_b")])])]), Coil: new(prefix + "-terminal", "Output"));
        try
        {
            foreach (var vendor in new[] { "TIA Portal", "Studio 5000" })
            {
                var program = new LadderProgram(2, "block-view-native-fixture", "Block view", "LD", TimeSpan.FromMilliseconds(20), [
                    new("Input_a", PlcVariableType.Bool, PlcVariableRole.Input, false), new("Input_b", PlcVariableType.Bool, PlcVariableRole.Input, false),
                    new("Output", PlcVariableType.Bool, PlcVariableRole.Output, false)], [],
                    [new("view-a", "View A", [Network("a1"), Network("a2")]), new("view-b", "View B", [Network("b1"), Network("b2")])], "view-a");
                _ladderDocument.ReplaceFromProgram(program); _ladderHistory = new LadderEditorHistory();
                foreach (var refresh in _ladderEditorRefreshers) refresh();
                var tabs = GetNode<TabContainer>("Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs");
                tabs.CurrentTab = vendor == "TIA Portal" ? 0 : 1;
                var scope = $"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{vendor}/Workbench/EditorAndTasks/EditorAndInspector";
                var scroll = GetNode<ScrollContainer>(scope + "/ProgramEditor/RoutineView");
                var canvas = scroll.GetNode<LadderEditorCanvas>("GraphicalLadderCanvas");
                var organization = GetNode<Tree>($"Workspace/LadderWorkspace/LadderMargin/LadderBody/LadderEnvironmentTabs/{vendor}/Workbench/ProjectDockHost/ProjectOrganization/ProjectBody/ProjectTreeAndTags/ProjectTree");
                TreeItem? Find(TreeItem? item, string metadata)
                {
                    for (var next = item; next is not null; next = next.GetNext())
                    { if (next.GetMetadata(0).AsString() == metadata) return next; var child = Find(next.GetFirstChild(), metadata); if (child is not null) return child; }
                    return null;
                }
                void Navigate(int block)
                {
                    var item = Find(organization.GetRoot(), "program:" + block) ?? throw new InvalidOperationException("Block row missing");
                    item.Select(0); organization.EmitSignal(Tree.SignalName.ItemSelected);
                }
                await Frames();
                canvas.SelectTreeNode(0, "a1-left-leaf"); canvas.SetZoomPercent(150); await Frames();
                scroll.ScrollHorizontal = 140; scroll.ScrollVertical = 180; await Frames();
                var savedA = canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical);
                Navigate(1); await Frames(); canvas.SelectElement(1, -1, -1, true); canvas.SetZoomPercent(75); await Frames();
                scroll.ScrollHorizontal = 80; scroll.ScrollVertical = 240; await Frames();
                var savedB = canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical);
                Navigate(0); await Frames();
                GD.Print($"BLOCK_VIEW_STATE vendor={vendor} A.expected={savedA} A.actual={canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical)} canvasSize={canvas.Size} page={scroll.Size} maxH={scroll.GetHScrollBar().MaxValue} maxV={scroll.GetVScrollBar().MaxValue}");
                Check(canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical) == savedA, vendor + "AStableTargetScrollZoom");
                Navigate(1); await Frames();
                GD.Print($"BLOCK_VIEW_STATE vendor={vendor} B.expected={savedB} B.actual={canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical)}");
                Check(canvas.CaptureViewState(scroll.ScrollHorizontal, scroll.ScrollVertical) == savedB, vendor + "BStableTargetScrollZoom");
                Navigate(0); await Frames();
                var pending = true; syntheticPending = new PendingEditorForm(() => pending, () => false, () => pending = false);
                _pendingEditorForms.Add(syntheticPending); var beforeStore = _blockViewRememberCount;
                Navigate(1); await Frames(); Check(_ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == "view-a" && _blockViewRememberCount == beforeStore, vendor + "PendingGuardBeforeStore");
                _pendingEditorDialog!.EmitSignal(ConfirmationDialog.SignalName.Canceled); _pendingEditorDialog.Hide(); await Frames();
                Check(_ladderDocument.Blocks[_ladderDocument.ActiveBlockIndex].Id == "view-a" && _blockViewRememberCount == beforeStore, vendor + "PendingCancelDoesNotStoreOrNavigate");
                _pendingEditorForms.Remove(syntheticPending); syntheticPending = null;
                Navigate(1); await Frames();
                var deleted = _ladderDocument.Blocks[0].Rungs[0]; deleted.SetRoot(EditableLadderTree.RemoveSubtree(deleted.Root, "a1-left-leaf", () => "unused-replacement"));
                Navigate(0); await Frames(); Check(canvas.SelectedRung == 0 && canvas.SelectedNodeId == "" && !canvas.OutputSelected && scroll.ScrollHorizontal == 0 && scroll.ScrollVertical == 0,
                    vendor + "DeletedTargetFallsBackToHeader");
                // Culling must update when scroll changes; an actual rendered capture remains a separate gate.
                canvas.SelectTreeNode(0, "a1-right-leaf");
                LadderEditorZoom.RevealSelection(canvas, scroll); await Frames();
                canvas.QueueRedraw(); await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GD.Print($"BLOCK_VIEW_DRAW vendor={vendor} drawn={canvas.LastDrawnInstructionCount} target={canvas.GetSelectionBounds()} H={scroll.ScrollHorizontal} V={scroll.ScrollVertical} page={scroll.GetGlobalRect()} canvasGlobal={canvas.GetGlobalRect()}");
                Check(canvas.LastDrawnInstructionCount > 0, vendor + "ScrolledViewportDrawsInstructions");
            }
            GD.Print("BLOCK_VIEW_REVIEW_PASS scripted=true physicalInput=false"); return true;
        }
        catch (Exception error) { GD.PushError("BLOCK_VIEW_REVIEW_FAIL " + error); return false; }
        finally
        {
            if (syntheticPending is not null) _pendingEditorForms.Remove(syntheticPending);
            _pendingEditorDialog?.Hide(); _pendingEditorContinuation = null;
            _ladderDocument.RestoreSnapshot(original); _ladderHistory = originalHistory;
            foreach (var refresh in _ladderEditorRefreshers) refresh();
            for (var i = 0; i < originalView.Length; i++) _ladderCanvases[i].RestoreViewState(originalView[i]);
        }
    }
}
