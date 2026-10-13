using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.Workspace;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _verifyWorkspaceDrive;
    private async void VerifyWorkspaceDrive()
    {
        var failures = 0;
        void Check(bool ok, string name) { GD.Print($"WORKSPACE_DRIVE_CHECK {name}={ok}"); if (!ok) failures++; }
        async System.Threading.Tasks.Task Frames(int count)
        { for (var i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
        async System.Threading.Tasks.Task Scans(int count)
        {
            var target = _virtualController!.Snapshot.ScanNumber + count;
            var timer = Stopwatch.StartNew();
            while (_virtualController.Snapshot.ScanNumber < target)
            {
                if (timer.Elapsed.TotalSeconds > 10) throw new InvalidOperationException("Drive accepted scan timeout.");
                await Frames(1);
            }
        }
        var directory = ProjectSettings.GlobalizePath($"res://.tools/workspace-drive-native-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var source = SceneCatalogLoader.LoadScene(_sceneCatalog!.Scenes.Single(s => s.Id == WorkspaceDriveConnectionPolicy.SourceId));
            const string instance = "placement-001-learner-belt";
            var relation = WorkspaceDriveConnectionPolicy.CreateRelation(source, instance);
            var workspace = new WorkspaceDocument(9, source.Id,
                [new(instance, WorkspaceDriveConnectionPolicy.BeltAsset, [0, 0, 3], [0, 0, 0], [1, 1, 1])],
                SignalLinks: [new("added_run", instance, "run_command", relation.RunPoint),
                    new("added_running", instance, "running", relation.RunningPoint),
                    new("added_speed", instance, "actual_speed", relation.ActualSpeedPoint)], DriveConnection: relation);
            foreach (var period in new[] { 20, 40, 100 })
            {
                StopActiveController(); DisableVirtualController(); MarkWorkspaceSaved();
                var draft = new LadderEditorDocument();
                draft.ResetProject("learner-drive-" + period, "Drive_Main", TimeSpan.FromMilliseconds(period));
                draft.SourceSceneId = source.Id;
                draft.AddTag("enable", PlcVariableRole.Memory, initialValue: true);
                draft.AddTag("run", PlcVariableRole.Output, relation.RunPoint);
                draft.AddTag("running", PlcVariableRole.Input, relation.RunningPoint);
                draft.AddTag("speed", PlcVariableRole.Input, relation.ActualSpeedPoint, PlcVariableType.Real);
                draft.WatchVariables.Add("speed");
                draft.AddRung("Learner authored enable", "run"); draft.AddContact(0, 0, "enable", false);
                var json = LadderEditorProjectJson.Save(draft);
                var path = Path.Combine(directory, period + ".integrated.json");
                WorkspaceProjectBundleRepository.Save(new(1, WorkspaceProjectBundleRepository.Kind, workspace, json), path, _candidateCatalog!, _sceneCatalog);
                LoadWorkspaceFromPath(path); await Frames(2);
                Check(_currentSceneId == source.Id && _sceneRuntime!.Points.ContainsKey(relation.RunPoint), "actual_joint_load_effective_source_" + period);
                if (_currentSceneId != source.Id || !_sceneRuntime!.Points.ContainsKey(relation.RunPoint))
                    throw new InvalidOperationException("Saved drive runtime is not integrated.");
                Check(_simulatorShell!.CaptureIntegratedLadderDraft() == json && CreateWorkspaceDocument().DriveConnection == relation,
                    "exact_joint_draft_watch_period_relation_preserved_" + period);
                var node = _workspaceNodes[instance].Node;
                var adapter = node.GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                var drum = (Node3D)node.FindChild("KIN_drive_drum*", true, false);
                var home = drum.Transform;
                Check(!adapter.IsPhysicsProcessing() && _sceneRuntime.Points.ContainsKey("simulated_photoeye"),
                    "sole_added_drive_clock_original_plant_preserved_" + period);
                RunEditorDraftController(); await Scans(4); PauseActiveController();
                Check(adapter.ActualSpeedMps > 0 && drum.Transform != home && _sceneRuntime.Points[relation.RunningPoint] is true,
                    "actual_current_run_drum_and_measured_feedback_" + period);
                var scan = _virtualController!.Snapshot.ScanNumber;
                var speed = adapter.ActualSpeedMps; var held = drum.Transform;
                await Frames(15);
                Check(_virtualController.Snapshot.ScanNumber == scan && drum.Transform == held && adapter.ActualSpeedMps == speed,
                    "pause_holds_wall_clock_drum_feedback_" + period);
                var pausedSnapshot = JsonSerializer.Serialize(_virtualController.Snapshot);
                var pausedPoints = JsonSerializer.Serialize(_sceneRuntime.Points);
                var pausedPose = ScanInspectionNativePose();
                var pausedRows = JsonSerializer.Serialize(_virtualController.History);
                var pausedDropped = _virtualController.HistoryDroppedCount;
                var pausedTime = _virtualController.Snapshot.SimulatedTime;
                Check(_simulatorShell.VerifyScanInspectionHistorySelection(0, out var historyDiagnostic),
                    "actual_connected_history_selection_historical_label_" + period);
                GD.Print("WORKSPACE_DRIVE_HISTORY " + historyDiagnostic);
                Check(_virtualController.IsPaused && _virtualController.Snapshot.ScanNumber == scan
                    && _virtualController.Snapshot.SimulatedTime == pausedTime
                    && JsonSerializer.Serialize(_virtualController.Snapshot) == pausedSnapshot
                    && JsonSerializer.Serialize(_sceneRuntime.Points) == pausedPoints
                    && ScanInspectionNativePose() == pausedPose && adapter.ActualSpeedMps == speed
                    && JsonSerializer.Serialize(_virtualController.History) == pausedRows
                    && _virtualController.HistoryDroppedCount == pausedDropped,
                    "history_selection_preserves_all_original_added_poses_points_snapshot_clock_rows_" + period);
                // Exercise the actual declared runtime publisher. Each rejected pair must
                // validate both endpoints/values before either point or event is changed.
                var feedbackEvents = 0;
                void FeedbackChanged() => feedbackEvents++;
                _sceneRuntime.StateChanged += FeedbackChanged;
                try
                {
                    var badPairs = new (string Name, string Running, string Speed, bool? Value, double? Mps)[]
                    {
                        ("invalid_running_owner", relation.RunPoint, relation.ActualSpeedPoint, true, .1),
                        ("invalid_second_endpoint_owner", relation.RunningPoint, relation.RunPoint, true, .1),
                        ("mixed_null_speed", relation.RunningPoint, relation.ActualSpeedPoint, true, null),
                        ("mixed_null_running", relation.RunningPoint, relation.ActualSpeedPoint, null, .1),
                        ("nan_speed", relation.RunningPoint, relation.ActualSpeedPoint, true, double.NaN)
                    };
                    foreach (var bad in badPairs)
                    {
                        var rejected = false;
                        try { _sceneRuntime.PublishWorkspaceDriveFeedback(bad.Running, bad.Speed, bad.Value, bad.Mps); }
                        catch (InvalidOperationException) { rejected = true; }
                        Check(rejected && feedbackEvents == 0
                            && JsonSerializer.Serialize(_sceneRuntime.Points) == pausedPoints
                            && JsonSerializer.Serialize(_virtualController.Snapshot) == pausedSnapshot
                            && ScanInspectionNativePose() == pausedPose && adapter.ActualSpeedMps == speed
                            && JsonSerializer.Serialize(_virtualController.History) == pausedRows
                            && _virtualController.HistoryDroppedCount == pausedDropped,
                            "feedback_pair_atomic_rejection_" + bad.Name + "_" + period);
                    }
                }
                finally { _sceneRuntime.StateChanged -= FeedbackChanged; }
                StepActiveController();
                Check(_virtualController.Snapshot.ScanNumber == scan + 1
                    && Math.Abs(adapter.ActualSpeedMps - Math.Min(.65f, speed + .75f * period / 1000f)) < 1e-6,
                    "step_exactly_one_accepted_drive_period_" + period);
                var rows = _virtualController.History.ToArray();
                Check(rows.Length >= 2 && rows.Skip(1).Select((row, i) =>
                    row.CompletedScan.SampledInputs.Numbers["speed"] == rows[i].PostPlantInputs.Numbers[relation.ActualSpeedPoint]).All(v => v),
                    "postplant_speed_is_sampled_on_following_scan_" + period);
                StopActiveController(); held = drum.Transform; await Frames(10);
                Check(adapter.ActualSpeedMps == 0 && drum.Transform == held && _sceneRuntime.Points[relation.RunningPoint] is false,
                    "stop_zero_speed_hold_pose_" + period);
                ResetActiveController();
                Check(drum.Transform == home && adapter.ActualSpeedMps == 0 && _virtualController.Snapshot.ScanNumber == 0,
                    "reset_actual_drum_home_scan_zero_" + period);
                RunEditorDraftController(); await Scans(4);
                var beforeCoast = adapter.ActualSpeedMps;
                DeleteSignalLink("added_run"); await Scans(2);
                Check(adapter.ActualSpeedMps > 0 && adapter.ActualSpeedMps < beforeCoast
                    && _sceneRuntime.Points[relation.RunningPoint] is true,
                    "command_detach_coasts_with_valid_feedback_" + period);
                DeleteSignalLink("added_speed"); held = drum.Transform;
                await Frames(10);
                Check(_virtualController.Snapshot.State == VirtualControllerState.Stopped
                    && adapter.ActualSpeedMps == 0 && drum.Transform == held
                    && _sceneRuntime.Points[relation.RunningPoint] is null
                    && _sceneRuntime.Points[relation.ActualSpeedPoint] is null,
                    "feedback_detach_stops_holds_marks_unavailable_" + period);
                RunEditorDraftController();
                Check(_virtualController.Snapshot.State == VirtualControllerState.Stopped,
                    "missing_feedback_current_run_blocked_" + period);
                CreateSignalLink(instance, "actual_speed", relation.ActualSpeedPoint);
                CreateSignalLink(instance, "run_command", relation.RunPoint);
                RunEditorDraftController(); await Scans(2);
                Check(adapter.ActualSpeedMps > 0 && _sceneRuntime.Points[relation.ActualSpeedPoint] is double,
                    "rebind_current_draft_fresh_run_actual_feedback_" + period);
                PauseActiveController(); held = drum.Transform;
                scan = _virtualController.Snapshot.ScanNumber; speed = adapter.ActualSpeedMps;
                for (var i = 0; i < 20; i++) RefreshWorkspaceUi();
                Check(drum.Transform == held && adapter.ActualSpeedMps == speed && _virtualController.Snapshot.ScanNumber == scan,
                    "ui_observers_never_integrate_drive_" + period);
                StopActiveController();
                draft.Rungs[0].Branches[0].Contacts[0] = draft.Rungs[0].Branches[0].Contacts[0] with { NormallyClosed = true };
                _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(draft));
                RunEditorDraftController(); await Scans(2);
                Check(adapter.ActualSpeedMps == 0 && _sceneRuntime.Points[relation.RunPoint] is false,
                    "wrong_current_program_changes_real_drive_result_" + period);
                draft.AddRung("Invalid current output", "missing_current_output");
                var invalidJson = LadderEditorProjectJson.Save(draft);
                _simulatorShell.RestoreIntegratedLadderDraft(invalidJson); RunEditorDraftController();
                held = drum.Transform; scan = _virtualController.Snapshot.ScanNumber; await Frames(10);
                Check(_virtualController.Snapshot.State == VirtualControllerState.Stopped && drum.Transform == held
                    && _virtualController.Snapshot.ScanNumber == scan && _simulatorShell.CaptureIntegratedLadderDraft() == invalidJson,
                    "invalid_current_draft_retained_no_stale_execution_" + period);
                var invalidPath = Path.Combine(directory, period + ".invalid.integrated.json");
                WorkspaceProjectBundleRepository.Save(new(1, WorkspaceProjectBundleRepository.Kind,
                    CreateWorkspaceDocument(), invalidJson), invalidPath, _candidateCatalog!, _sceneCatalog);
                MarkWorkspaceSaved(); LoadWorkspaceFromPath(invalidPath); await Frames(2);
                RunEditorDraftController();
                Check(_simulatorShell.CaptureIntegratedLadderDraft() == invalidJson
                    && (_virtualController is null || _virtualController.Snapshot.State == VirtualControllerState.Stopped),
                    "invalid_joint_reopen_preserves_exact_draft_blocks_run_" + period);
            }
        }
        catch (Exception error) { failures++; GD.PushError("WORKSPACE_DRIVE_EXCEPTION " + error); }
        GD.Print($"WORKSPACE_DRIVE_RESULT failures={failures}"); GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
