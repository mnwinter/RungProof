using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private static LadderEditorDocument TypedLearnerDocument(string scene, int period, string mode)
    {
        if (!AuthoredLessonLadderPrograms.TryCreate(scene, out var document)) throw new InvalidOperationException("Missing typed factory.");
        document.ScanPeriod = TimeSpan.FromMilliseconds(period);
        document.AddTag("qa_custom_saved_memory", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.Add("qa_custom_saved_memory");
        if (mode == "wrong")
        {
            if (scene.Contains("motor-enum")) document.Rungs.Single(r => r.IsNumericOperation && r.NumericSourceA == "\"Running\"").NumericSourceA = "\"Stopped\"";
            else
            {
                var name = scene.Contains("chicken-label") ? "label_template" : "selected_alarm_code";
                var index = document.Tags.FindIndex(t => t.Name == name);
                document.Tags[index] = document.Tags[index] with { InitialValue = scene.Contains("chicken-label") ? "CHICKEN {0:F2} kg" : "F030" };
            }
        }
        if (mode == "invalid") document.AddRung("Invalid unresolved current output", "qa_missing_output");
        return document;
    }
    // Native QA only. Normal editor Run and engine callbacks own all execution;
    // this review never injects feedback or commits commands directly.
    private async void VerifyTypedLearnerWorkflow()
    {
        var failures = 0;
        var evidence = ProjectSettings.GlobalizePath("res://.tools/typed-learner-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(evidence);
        void Check(bool ok, string name) { GD.Print($"TYPED_LEARNER_CHECK {name}={ok}"); if (!ok) failures++; }
        bool On(string point) => _sceneRuntime!.Points[point] is true;
        string Text(string point) => Convert.ToString(_sceneRuntime!.Points[point]) ?? "";
        async Task Frames(double seconds)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed.TotalSeconds < seconds) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        async Task Until(Func<bool> predicate, double timeout = 8)
        {
            var watch = Stopwatch.StartNew();
            while (!predicate())
            {
                if (watch.Elapsed.TotalSeconds > timeout) throw new InvalidOperationException("Accepted engine workflow timeout.");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
        }
        async Task Scans(int count = 2)
        {
            var target = _virtualController!.Snapshot.ScanNumber + count;
            await Until(() => _virtualController!.Snapshot.ScanNumber >= target);
        }
        void Action(string name)
        {
            if (!ExecuteSelectedControllerAction(name)) throw new InvalidOperationException("Normal action rejected: " + name);
        }
        async Task Joint(string scene, string mode, int ms)
        {
            var draft = _simulatorShell!.CaptureIntegratedLadderDraft();
            var layout = JsonSerializer.Serialize(CreateWorkspaceDocument());
            var path = Path.Combine(evidence, $"{scene}-{ms}-{mode}.rpworkspace.json");
            SaveWorkspaceToPath(path);
            Check(File.Exists(path), $"joint_saved_{scene}_{ms}_{mode}");
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();
            AddMigratedScene("lab-2-01-workstation-call", _candidateCatalog!, _mainCamera!, false, false);
            MarkWorkspaceSaved(); LoadWorkspaceFromPath(path);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(_currentSceneId == scene && _simulatorShell.CaptureIntegratedLadderDraft() == draft
                && JsonSerializer.Serialize(CreateWorkspaceDocument()) == layout, $"joint_exact_custom_draft_layout_{scene}_{ms}_{mode}");
            Check(_virtualController is null || _virtualController.Snapshot.State != VirtualControllerState.Running,
                $"joint_reopen_stopped_{scene}_{ms}_{mode}");
        }
        async Task Open(string scene, int ms, string mode)
        {
            StopActiveController(); DisableVirtualController();
            AddMigratedScene(scene, _candidateCatalog!, _mainCamera!, false, false);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var document = TypedLearnerDocument(scene, ms, mode);
            var path = Path.Combine(evidence, $"{scene}-{ms}-{mode}.rpproj.json");
            File.WriteAllText(path, LadderEditorProjectJson.Save(document));
            Check(_simulatorShell!.OpenNativeMotionReviewProject(path, out var result), $"disk_actual_editor_open_{scene}_{ms}_{mode}");
            GD.Print(result);
            Check(_simulatorShell.TryBuildCurrentLadderProgram(out _, out _), $"current_draft_scene_compile_{scene}_{ms}_{mode}");
            await Joint(scene, mode, ms);
            RunEditorDraftController();
            Check(_virtualController is not null && _virtualController.Snapshot.State == VirtualControllerState.Running
                && _virtualController.ScanPeriod == TimeSpan.FromMilliseconds(ms), $"current_editor_run_cadence_{scene}_{ms}_{mode}");
            await Scans();
            var before = JsonSerializer.Serialize(_virtualController!.Snapshot);
            var pose = FiveDemoPose();
            PauseActiveController(); StepActiveController();
            Check(!ScanInspectionSupported && !_virtualController.IsPaused && JsonSerializer.Serialize(_virtualController.Snapshot) == before
                && FiveDemoPose() == pose, $"typed_unsupported_pause_step_no_mutation_{scene}_{ms}_{mode}");
            Check(_virtualController.History.Count > 0, $"typed_accepted_history_recorded_{scene}_{ms}_{mode}");
            var row = _virtualController.History.Last();
            var retained = JsonSerializer.Serialize(row);
            await Scans();
            Check(JsonSerializer.Serialize(row) == retained, $"typed_retained_history_immutable_after_real_scans_{scene}_{ms}_{mode}");
        }
        async Task Invalid(string scene, int ms)
        {
            var invalid = LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!;
            invalid.AddRung("Invalid unresolved current output", "qa_missing_output");
            var json = LadderEditorProjectJson.Save(invalid);
            _simulatorShell.RestoreIntegratedLadderDraft(json);
            Check(!_simulatorShell.TryBuildCurrentLadderProgram(out _, out _), $"invalid_current_rejected_{scene}_{ms}");
            RunEditorDraftController();
            Check(_virtualController is null || _virtualController.Snapshot.State != VirtualControllerState.Running,
                $"invalid_current_no_prior_fallback_{scene}_{ms}");
            var scan = _virtualController?.Snapshot.ScanNumber;
            var time = _virtualController?.Snapshot.SimulatedTime;
            var pose = FiveDemoPose();
            await Frames(.25);
            Check(FiveDemoPose() == pose && _virtualController?.Snapshot.ScanNumber == scan
                && _virtualController?.Snapshot.SimulatedTime == time && _simulatorShell.CaptureIntegratedLadderDraft() == json,
                $"invalid_freezes_engine_pose_scan_time_draft_{scene}_{ms}");
            await Joint(scene, "invalid", ms);
            RunEditorDraftController();
            Check((_virtualController is null || _virtualController.Snapshot.State != VirtualControllerState.Running)
                && _simulatorShell.CaptureIntegratedLadderDraft() == json, $"invalid_reopened_no_fallback_{scene}_{ms}");
        }
        try
        {
            foreach (var scene in new[] { "lab-10-04-motor-enum-state", "lab-10-02-chicken-label-print", "lab-10-01-drive-alarm-code-string" })
            foreach (var ms in new[] { 20, 40, 100 })
            foreach (var mode in new[] { "intended", "wrong" })
            {
                await Open(scene, ms, mode);
                var label = $"{scene}_{ms}_{mode}";
                if (scene.Contains("motor-enum"))
                {
                    var motor = _sceneCompositionRoot!.GetNode<Node3D>("motor_0");
                    var shaft = motor.FindChildren("KIN_motor_*", "", true, false).OfType<Node3D>().First();
                    var home = shaft.Transform;
                    var mountHome = motor.Transform;
                    Action("toggle-start_request"); await Scans(); await Frames(.12);
                    Check(Text("motor_state") == (mode == "wrong" ? "Stopped" : "Running")
                        && On("motor_running") == (mode == "intended") && (shaft.Transform != home) == (mode == "intended"), "actual_state_shaft_" + label);
                    Check(motor.Transform == mountHome, "motor_mounted_parent_does_not_rotate_" + label);
                    var readout = _sceneCompositionRoot.GetNode<Node3D>("training_accessory_7").FindChild("StaticReadout", true, false) as Label3D;
                    Check(readout is not null && readout.Text.Contains(Text("motor_state")), "actual_enum_readout_" + label);
                    if (mode == "intended")
                    {
                        Action("toggle-stop_request"); await Scans(); var held = shaft.Transform; await Frames(.12);
                        Check(!On("motor_running") && Text("motor_state") == "Stopped" && shaft.Transform == held, "stop_request_holds_shaft_" + label);
                        Action("toggle-stop_request"); await Scans(); Check(!On("motor_running"), "held_start_no_restart_" + label);
                        Action("toggle-fault_active"); Action("reset-motor-fault"); await Scans(); Check(Text("motor_state") == "Fault" && !On("motor_running"), "fault_dominates_reset_" + label);
                        Action("toggle-fault_active"); await Scans(); Check(Text("motor_state") == "Fault", "fault_latched_" + label);
                        Action("reset-motor-fault"); await Scans(); Check(Text("motor_state") == "Stopped" && !On("motor_running"), "reset_held_start_stopped_" + label);
                        Action("toggle-start_request"); await Scans(); Action("toggle-start_request"); await Scans(); await Frames(.12);
                        Check(On("motor_running") && shaft.Transform != held, "fresh_restart_actual_shaft_" + label);
                    }
                    StopActiveController(); var stopped = shaft.Transform; await Frames(.12);
                    Check(shaft.Transform == stopped && Text("motor_state") == "Stopped" && !On("motor_running"), "controller_stop_safe_enum_pose_" + label);
                    ResetActiveController(); Check(shaft.Transform == home && !On("start_request") && _virtualController!.Snapshot.ScanNumber == 0, "reset_actual_home_inputs_scan_" + label);
                    RunEditorDraftController(); await Scans(); Action("toggle-start_request"); await Scans();
                }
                else if (scene.Contains("chicken-label"))
                {
                    var paper = _sceneCompositionRoot!.GetNode<MeshInstance3D>("ChickenLabelPaper");
                    var home = paper.Transform;
                    Action("toggle-label_data_valid"); Action("toggle-product_weighed");
                    await Until(() => On("product_weighed")); await Scans();
                    Check(Math.Abs(Convert.ToDouble(_sceneRuntime!.Points["weight_kg"]) - 1.237) < 1e-9
                        && !On("print_request") && !paper.Visible, "actual_weight_printer_off_" + label);
                    Action("toggle-label_data_valid"); Action("toggle-printer_ready"); await Scans();
                    Check(!On("print_request") && !On("print_complete") && !paper.Visible, "invalid_data_ready_printer_blocks_actual_print_" + label);
                    var accepted = new List<VirtualControllerAcceptedScan>();
                    var recordedSession = _virtualController!;
                    void RecordChicken(VirtualControllerScanCompleted completed) => accepted.Add(recordedSession.History.Last());
                    recordedSession.ScanCompleted += RecordChicken;
                    Action("toggle-label_data_valid");
                    await Until(() => paper.Visible);
                    // Readiness loss holds an already accepted job; its request is
                    // not queued or treated as physical completion by the observer.
                    Action("toggle-printer_ready"); await Scans();
                    var unavailablePose = paper.Transform;
                    var capturedBefore = paper.GetNode<Label3D>("PrintedInk").Text;
                    await Scans(3);
                    Check(paper.Transform == unavailablePose && !On("print_complete") && !On("print_request")
                        && paper.GetNode<Label3D>("PrintedInk").Text == capturedBefore, "printer_loss_holds_accepted_job_and_captured_ink_" + label);
                    Action("toggle-printer_ready");
                    StopActiveController(); var held = paper.Transform; var scan = _virtualController!.Snapshot.ScanNumber; await Frames(.15);
                    Check(paper.Transform == held && _virtualController.Snapshot.ScanNumber == scan && !On("print_request"), "stop_midprint_actual_freeze_" + label);
                    RunEditorDraftController(); await Until(() => On("print_complete") && On("apply_request") && !On("application_complete"));
                    StopActiveController(); held = paper.Transform; scan = _virtualController!.Snapshot.ScanNumber; await Frames(.15);
                    Check(paper.Transform == held && _virtualController.Snapshot.ScanNumber == scan && !On("apply_request")
                        && On("print_complete") && !On("application_complete"), "stop_midapply_retains_print_ack_freezes_paper_" + label);
                    RunEditorDraftController(); await Until(() => On("application_complete")); await Scans();
                    recordedSession.ScanCompleted -= RecordChicken;
                    foreach (var feedback in new[] { "print_complete", "application_complete" })
                    {
                        var edge = accepted.FindIndex(row => !row.CompletedScan.SampledInputs.Bools.GetValueOrDefault(feedback)
                            && row.PostPlantInputs.Bools.GetValueOrDefault(feedback));
                        var next = edge >= 0 ? accepted.Skip(edge + 1).FirstOrDefault() : null;
                        Check(edge >= 0 && next is not null
                            && next.CompletedScan.ScanNumber == accepted[edge].CompletedScan.ScanNumber + 1
                            && next.CompletedScan.SampledInputs.Bools.GetValueOrDefault(feedback)
                            && (feedback == "print_complete"
                                ? !next.CompletedScan.Outputs.Bools.GetValueOrDefault("print_request") && next.CompletedScan.Outputs.Bools.GetValueOrDefault("apply_request")
                                : !next.CompletedScan.Outputs.Bools.GetValueOrDefault("apply_request") && next.CompletedScan.Outputs.Bools.GetValueOrDefault("label_applied")),
                            "actual_postplant_N_sampled_Nplus1_" + feedback + "_" + label);
                    }
                    var expected = mode == "wrong" ? "CHICKEN 1.24 kg" : "CHICKEN 1.237 kg";
                    Check(On("print_complete") && On("application_complete") && On("label_applied") && !On("apply_request")
                        && paper.Transform != home && paper.GetNode<Label3D>("PrintedInk").Text == expected
                        && paper.GetNode<Label3D>("CapturedLabelText").Text == expected, "actual_generated_ack_applied_exact_ink_" + label);
                    ResetActiveController(); Check(!paper.Visible && paper.Transform == home && !On("product_present")
                        && !On("print_complete") && !On("application_complete") && Text("label_text") == "", "reset_actual_job_paper_text_" + label);
                    // Reject a changed current draft during a real new print job.
                    RunEditorDraftController(); await Scans(); Action("toggle-label_data_valid"); Action("toggle-product_weighed"); Action("toggle-printer_ready");
                    await Until(() => paper.Visible && !On("print_complete"));
                    var jobInk = paper.GetNode<Label3D>("PrintedInk").Text;
                    var jobPose = paper.Transform;
                    var changed = LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!;
                    var template = changed.Tags.FindIndex(t => t.Name == "label_template");
                    changed.Tags[template] = changed.Tags[template] with { InitialValue = "EDITED {0:F1} kg" };
                    _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(changed));
                    RunEditorDraftController();
                    Check(paper.Transform == jobPose && paper.GetNode<Label3D>("PrintedInk").Text == jobInk,
                        "changed_valid_current_run_preserves_accepted_job_pose_ink_" + label);
                    await Scans(1);
                    Check(Text("label_text") == "EDITED 1.2 kg" && paper.GetNode<Label3D>("PrintedInk").Text == jobInk,
                        "new_command_text_does_not_rewrite_captured_printed_job_" + label);
                }
                else
                {
                    Action("toggle-drive_alarm_string_valid"); Action("next-alarm-message"); await Scans();
                    Check(On("alarm_code_found") == (mode == "intended") && On("drive_alarm_active") == (mode == "intended"), "actual_whole_token_vs_changed_selected_code_" + label);
                    var readout = _sceneCompositionRoot!.GetNode<Node3D>("training_accessory_4").FindChild("StaticReadout", true, false) as Label3D;
                    Check(readout is not null && readout.Text.Contains(Text("drive_alarm_text")), "actual_received_text_readout_" + label);
                    Action("next-alarm-message"); await Scans(); Check(On("alarm_code_found") == (mode == "wrong"), "F030_selected_custom_behavior_" + label);
                    Action("next-alarm-message"); await Scans(); Check(!On("alarm_code_found"), "F0030_rejected_" + label);
                    Action("next-alarm-message"); await Scans(); Check(!On("alarm_code_found"), "XF003_rejected_" + label);
                    Action("next-alarm-message"); await Scans(); Check(!On("alarm_code_found"), "lowercase_rejected_" + label);
                    Action("next-alarm-message"); await Scans(); Check(!On("alarm_code_found"), "empty_rejected_" + label);
                    Action("next-alarm-message"); Action("toggle-alarm_reset"); await Scans(); Check(!On("drive_alarm_active"), "reset_dominates_match_" + label);
                    StopActiveController(); ResetActiveController(); Check(Text("drive_alarm_text") == "" && !On("drive_alarm_active"), "reset_actual_text_outputs_" + label);
                    RunEditorDraftController(); await Scans();
                }
                await Invalid(scene, ms);
            }
        }
        catch (Exception error) { failures++; GD.PushError("TYPED_LEARNER_EXCEPTION " + error); }
        GD.Print($"TYPED_LEARNER_RESULT failures={failures} evidence={evidence} scope=actual_editor_joint_native_callbacks_20_40_100");
        GD.Print("REAL_PLC_TRANSPORT_CONSTRUCTED FALSE"); GD.Print("REAL_PLC_CONNECTION_ATTEMPTED FALSE");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
