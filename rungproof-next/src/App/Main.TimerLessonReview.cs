using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditTimerLessons;
    private void AuditTimerLessons()
    {
        var failures = 0;
        void Check(bool value, string name) { if (!value) failures++; GD.Print($"TIMER_LESSON_CHECK {name}={value}"); }
        try { VerifyTimerLessonWorkflows(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"TIMER_LESSON_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyTimerLessonWorkflows(Action<bool, string> check)
    {
        VerifyTimerLessonWorkflow(check, delayed: true);
        VerifyTimerLessonWorkflow(check, delayed: false);
    }

    private void VerifyTimerLessonWorkflow(Action<bool, string> check, bool delayed)
    {
        var sceneId = delayed ? "lab-5-01-delayed-lamp" : "lab-5-02-timed-lamp-off";
        var prefix = delayed ? "delayed_lamp" : "timed_lamp";
        void Check(bool value, string name) => check(value, $"{prefix}_{name}");
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var operatorNode = root.GetNode<Node3D>("switch_0");
        var initialPose = operatorNode.Transform;
        var input = delayed ? "timer_request" : "start_pulse";
        var output = delayed ? "delayed_lamp" : "timed_lamp";
        var timer = delayed ? "delay_timer" : "pulse_timer";
        var action = delayed ? "toggle-timer_request" : "press-start";
        Check(LogBoundsCandidates(root) == 0 && root.GetChildren().OfType<Node3D>().Where(n => ReviewMeshes(n).Length > 0)
            .All(n => ReviewBounds(n).Position.Y >= -.005f), "equipment_separated_and_grounded");
        Check(runtime.Points.Count == 2 && runtime.Points[input] is false && runtime.Points[output] is false
            && !runtime.Points.ContainsKey(delayed ? "delay_complete" : "time_active"), "raw_input_only_no_manual_timer_result");
        if (delayed)
            Check(operatorNode.GetNode<Label3D>("OperatorFaceLabel").Text == "REQUEST"
                && operatorNode.GetNode<Label3D>("SelectorOrdinal_0").Text == "OFF"
                && operatorNode.GetNode<Label3D>("SelectorOrdinal_1").Text == "ON"
                && operatorNode.FindChild("KIN_selector_handle", true, false) is Node3D,
                "maintained_selector_identifies_request_off_on");
        else
            Check(operatorNode.GetNode<Label3D>("OperatorFaceLabel").Text == "START"
                && root.GetNodeOrNull<Node3D>("switch_2") is null, "one_start_button_replaces_precomputed_time_switch");

        LadderEditorDocument document;
        if (delayed)
        {
            // Test the actual existing authored Demo 2, without duplicating it
            // or installing a new program in an unrelated exercise editor.
            if (!AuthoredDemoLadderPrograms.TryCreate(sceneId, out document))
                throw new InvalidOperationException("Authored delayed-lamp demo missing.");
        }
        else
        {
            // Original three-second TP training choice. The lesson supplies no
            // numeric preset. This reference is opened explicitly, never auto-loaded.
            document = new();
            document.ResetProject("review-timed-lamp", "Timed_Lamp_Reference", TimeSpan.FromMilliseconds(20));
            document.SourceSceneId = sceneId;
            document.AddTag(input, PlcVariableRole.Input, input);
            document.AddTag(output, PlcVariableRole.Output, output);
            document.AddTag(timer, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            document.WatchVariables.AddRange([input, timer, output]);
            document.AddTimerRung("Three-second nonretriggerable pulse", timer, TimeSpan.FromSeconds(3), LadderTimerKind.Pulse);
            document.AddContact(0, 0, input, false);
            document.AddRung("Lamp follows actual PLC pulse timer", output); document.AddContact(1, 0, timer + ".Q", false);
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-timed-lamp-off.rpproj.json"), LadderEditorProjectJson.Save(document));
        }
        var program = document.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        try
        {
            void Tick(int count = 1) { for (var i = 0; i < count; i++) _PhysicsProcess(.02); }
            void Action() { if (!ExecuteSelectedControllerAction(action)) throw new InvalidOperationException("Timer input rejected."); }
            bool Lamp() => runtime.Points[output] is true;
            double Elapsed() => _virtualController!.Snapshot.Timers[timer].Accumulated.TotalSeconds;
            bool Time(double seconds) => Math.Abs(Elapsed() - seconds) < 1e-9;
            RunActiveController(); Tick(10);
            Check(!Lamp() && Time(0), "idle_run_does_not_start_timer");
            Action();
            if (delayed)
            {
                Tick(99); Check(!Lamp() && Time(1.98), "authored_ton_off_at_99_scans");
                Tick(); Check(Lamp() && Time(2), "authored_ton_on_at_100_scans");
                Tick(20); Check(Lamp() && Time(2), "maintained_request_holds_lamp_after_expiry");
                Action(); Tick(); Check(!Lamp() && Time(0), "off_clears_lamp_and_nonretentive_timer");
                Action(); Tick(50); Check(!Lamp() && Time(1), "new_on_requires_fresh_delay");
                StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1);
                Check(!Lamp() && Time(0) && runtime.Points[input] is true && _virtualController.Snapshot.ScanNumber == scan,
                    "stop_resets_ton_retains_selector_and_freezes_scan");
                RunActiveController(); Tick(99); Check(!Lamp() && Time(1.98), "run_with_retained_on_restarts_full_delay");
                Tick(); Check(Lamp() && Time(2), "restarted_ton_completes_at_new_100th_scan");
                Action(); Tick(); Check(!Lamp() && Time(0), "off_after_restart_clears_again");
            }
            else
            {
                Tick(); Check(Lamp() && Time(.02) && runtime.Points[input] is false, "start_consumed_once_and_tp_lamp_immediately_on");
                Tick(74); Action(); Tick(); Check(Lamp() && Time(1.52), "extra_active_start_does_not_retrigger_or_extend_tp");
                Tick(73); Check(Lamp() && Time(2.98), "tp_remains_on_before_three_second_boundary");
                Tick(); Check(!Lamp() && Time(3), "tp_turns_off_at_three_second_boundary");
                Tick(100); Check(!Lamp() && Time(0) && runtime.Points[input] is false, "idle_after_expiry_cannot_repeat");
                Action(); Tick(50); Check(Lamp() && Time(1), "fresh_start_after_expiry_runs_new_interval");
                StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1);
                Check(!Lamp() && Time(0) && _virtualController.Snapshot.ScanNumber == scan, "stop_cancels_nonretentive_tp_and_freezes_scan");
                RunActiveController(); Tick(200); Check(!Lamp() && Time(0), "run_alone_cannot_retrigger_cancelled_interval");
                Action(); StopActiveController(); Check(runtime.Points[input] is false, "stop_discards_pending_unscanned_start");
                RunActiveController(); Tick(); Check(!Lamp() && Time(0), "discarded_start_cannot_fire_on_run");
                Action(); Action(); Tick(); Check(Lamp() && Time(.02), "same_scan_double_clicks_coalesce_to_single_start");
                Tick(149); Check(!Lamp() && Time(3), "coalesced_start_keeps_original_three_second_interval");
            }
            Check(operatorNode.Transform == initialPose, "operator_station_does_not_move_with_input_projection");
            ResetActiveController();
            Check(!Lamp() && Time(0) && runtime.Points[input] is false && _virtualController!.Snapshot.ScanNumber == 0
                && _virtualController.Snapshot.State == VirtualControllerState.Stopped, "application_reset_clears_input_timer_lamp_and_stays_stopped");
        }
        finally { DisableVirtualController(); }
    }
}
