using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;
public partial class Main
{
    private string? _workspaceDriveCaptureState;
    private bool _workspaceDriveCapturePending;
    private bool _workspaceDriveCaptureHeld;

    // Ephemeral rendered evidence uses the actual authoring button event,
    // joint file reopening and current editor Run; no plant feedback injection.
    private async void PrepareWorkspaceDriveCapture()
    {
        try
        {
            var mode = _workspaceDriveCaptureState!;
            if (_capturePath is null || _currentSceneId != WorkspaceDriveConnectionPolicy.SourceId
                || mode is not ("eligible" or "connected" or "intended" or "wrong" or "invalid" or "history"))
                throw new InvalidOperationException("Drive capture requires explicit source and bounded state.");
            StopActiveController(); DisableVirtualController();
            var belt = _candidateCatalog!.Assets.Single(a => a.Id == WorkspaceDriveConnectionPolicy.BeltAsset);
            PlaceAsset(belt);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var instance = _selectedPlacementId!;
            var tabs = _simulatorShell!.GetNode<TabContainer>("Workspace/RightDock/InspectorTabs");
            void Connections()
            {
                for (var i = 0; i < tabs.GetTabCount(); i++)
                    if (tabs.GetTabTitle(i) == "Connections") tabs.CurrentTab = i;
            }
            Connections();
            var button = _simulatorShell.GetNode<Button>("Workspace/RightDock/InspectorTabs/Connections/ConnectionEditorBody/ConnectBeltDrive");
            if (!button.Visible || button.Disabled || CreateWorkspaceDocument().DriveConnection is not null)
                throw new InvalidOperationException("Actual selected added belt authoring button is unavailable.");
            var originalDraft = _simulatorShell.CaptureIntegratedLadderDraft();
            if (mode != "eligible")
            {
                var requests = 0;
                void Request(string id) { if (id == instance) requests++; }
                _simulatorShell.WorkspaceDriveConnectionRequested += Request;
                try { button.EmitSignal(Button.SignalName.Pressed); }
                finally { _simulatorShell.WorkspaceDriveConnectionRequested -= Request; }
                var relation = CreateWorkspaceDocument().DriveConnection;
                if (requests != 1 || relation is null || relation.InstanceId != instance
                    || _simulatorShell.CaptureIntegratedLadderDraft() != originalDraft)
                    throw new InvalidOperationException("Actual authoring event did not preserve selected identity/current draft.");
                var draft = new LadderEditorDocument();
                draft.ResetProject("capture-added-drive", "Added_Drive_Main", TimeSpan.FromMilliseconds(40));
                draft.SourceSceneId = WorkspaceDriveConnectionPolicy.SourceId;
                draft.AddTag("enable", PlcVariableRole.Memory, initialValue: true);
                draft.AddTag("run", PlcVariableRole.Output, relation.RunPoint);
                draft.AddTag("running", PlcVariableRole.Input, relation.RunningPoint);
                draft.AddTag("speed", PlcVariableRole.Input, relation.ActualSpeedPoint, PlcVariableType.Real);
                draft.WatchVariables.AddRange(["run", "running", "speed"]);
                draft.AddRung("Learner enable drives selected added belt", "run");
                draft.AddContact(0, 0, "enable", mode == "wrong");
                // This explicit QA-authored learner document is loaded through
                // the editor; the authoring button itself never supplies logic.
                var directory = ProjectSettings.GlobalizePath("res://.tools/drive-capture-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                var project = Path.Combine(directory, "current.rpproj.json");
                File.WriteAllText(project, LadderEditorProjectJson.Save(draft));
                if (!_simulatorShell.OpenNativeMotionReviewProject(project, out var result)) throw new InvalidOperationException(result);
                var json = _simulatorShell.CaptureIntegratedLadderDraft();
                var layout = JsonSerializer.Serialize(CreateWorkspaceDocument());
                var joint = Path.Combine(directory, "current.rpworkspace.json");
                SaveWorkspaceToPath(joint); _simulatorShell.DismissIntegratedLoadConfirmationForReview();
                AddMigratedScene("lab-2-01-workstation-call", _candidateCatalog, _mainCamera!, false, false);
                MarkWorkspaceSaved(); LoadWorkspaceFromPath(joint);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                _simulatorShell.DismissIntegratedLoadConfirmationForReview();
                if (_simulatorShell.CaptureIntegratedLadderDraft() != json || JsonSerializer.Serialize(CreateWorkspaceDocument()) != layout)
                    throw new InvalidOperationException("Drive capture joint draft/layout changed.");
                SetSelection([instance]); Connections();
                if (mode != "connected")
                {
                    RunEditorDraftController();
                    if (_virtualController?.Snapshot.State != VirtualControllerState.Running) throw new InvalidOperationException("Current drive draft did not Run.");
                    var target = _virtualController.Snapshot.ScanNumber + 12;
                    var timer = Stopwatch.StartNew();
                    while (_virtualController.Snapshot.ScanNumber < target)
                    {
                        if (timer.Elapsed.TotalSeconds > 10) throw new InvalidOperationException("Drive capture accepted scan timeout.");
                        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    }
                    if ((_workspaceDriveAdapter!.ActualSpeedMps > 0) != (mode != "wrong"))
                        throw new InvalidOperationException("Current learner drive behavior differs from capture mode.");
                    if (mode == "invalid")
                    {
                        draft.AddRung("Unresolved current output", "missing_current_output");
                        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(draft)); RunEditorDraftController();
                        if (_virtualController.Snapshot.State == VirtualControllerState.Running || _workspaceDriveAdapter.ActualSpeedMps != 0)
                            throw new InvalidOperationException("Invalid current drive draft continued motion.");
                    }
                }
            }
            // Freeze only capture callbacks. This is not a product Pause state.
            _workspaceDriveCaptureHeld = true;
            void Hold(Node node) { node.ProcessMode = ProcessModeEnum.Disabled; foreach (var child in node.GetChildren()) Hold(child); }
            Hold(_sceneCompositionRoot!);
            foreach (var node in _workspaceNodes.Values) Hold(node.Node);
            _demoEnvironment?.SetAmbientMotionEnabled(false);
            if (mode == "history")
            {
                var before = JsonSerializer.Serialize(_virtualController!.Snapshot);
                PublishScanInspectionState();
                if (!_simulatorShell.VerifyScanInspectionHistorySelection(0, out var text)
                    || JsonSerializer.Serialize(_virtualController.Snapshot) != before)
                    throw new InvalidOperationException("Drive historical selection changed current snapshot.");
                File.WriteAllText(_capturePath + ".history.txt", text);
            }
            _simulatorShell.SetWorkspaceStatus("REVIEW CAPTURE · CLOCK HELD · added belt drive · " + mode);
            File.WriteAllText(_capturePath + ".workspace-drive.json", JsonSerializer.Serialize(new
            {
                mode, instance, ephemeralQA = true, diagnosticClockHeld = true,
                workspace = CreateWorkspaceDocument(), currentDraft = _simulatorShell.CaptureIntegratedLadderDraft(),
                snapshot = _virtualController?.Snapshot, history = _virtualController?.History,
                points = _sceneRuntime!.Points, adapterSpeed = _workspaceDriveAdapter?.ActualSpeedMps,
                buttonVisible = button.Visible, buttonDisabled = button.Disabled,
                selectedTab = tabs.GetTabTitle(tabs.CurrentTab)
            }, new JsonSerializerOptions { WriteIndented = true }));
            if (mode is "eligible" or "connected") _simulatorShell.SetReviewState("connections", "");
            _captureFramesRemaining = 30; _workspaceDriveCapturePending = false;
            GD.Print("WORKSPACE_DRIVE_CAPTURE_READY state=" + mode);
        }
        catch (Exception error) { GD.PushError("WORKSPACE_DRIVE_CAPTURE_FAIL " + error); _workspaceDriveCapturePending = false; GetTree().Quit(1); }
    }
}
