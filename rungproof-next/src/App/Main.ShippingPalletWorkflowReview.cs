using System;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyShippingPalletControllerWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-18-pallet-pickup", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        var pallet = _sceneCompositionRoot!.GetNode<Node3D>("shipping_pallet");
        var home = pallet.Position;
        var document = new LadderEditorDocument();
        document.ResetProject("review-shipping-pallet", "Shipping_Pallet_QA", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-2-18-pallet-pickup";
        document.AddTag("auto_mode", PlcVariableRole.Input, "auto_mode");
        document.AddTag("pickup_sensor", PlcVariableRole.Input, "pickup_sensor");
        document.AddTag("auto_start_request", PlcVariableRole.Input, "auto_start_request");
        document.AddTag("manual_jog_request", PlcVariableRole.Input, "manual_jog_request");
        document.AddTag("conveyor_run", PlcVariableRole.Output, "conveyor_run");
        // QA example: one nonretriggerable two-second jog (1.5 m at .75 m/s).
        // Mode and the physical pickup beam also gate the final command.
        document.AddTag("qa_jog", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        document.AddTimerRung("Bounded manual jog", "qa_jog", TimeSpan.FromSeconds(2), LadderTimerKind.Pulse);
        document.AddContact(0, 0, "auto_mode", true);
        document.AddContact(0, 0, "pickup_sensor", true);
        document.AddContact(0, 0, "manual_jog_request", false);
        document.AddRung("Automatic travel until actual pickup beam", "conveyor_run");
        document.AddContact(1, 0, "auto_mode", false);
        document.AddContact(1, 0, "pickup_sensor", true);
        document.AddContact(1, 0, "auto_start_request", false);
        document.AddParallelBranch(1);
        document.AddContact(1, 1, "auto_mode", false);
        document.AddContact(1, 1, "pickup_sensor", true);
        document.AddContact(1, 1, "conveyor_run", false);
        document.AddParallelBranch(1);
        document.AddContact(1, 2, "auto_mode", true);
        document.AddContact(1, 2, "pickup_sensor", true);
        document.AddContact(1, 2, "qa_jog.Q", false);
        var program = document.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        check(compiled.IsValid, "shipping_pallet_qa_compiles");
        if (!compiled.IsValid) throw new InvalidOperationException("Shipping QA compile failed.");
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-shipping-pallet-native-qa.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(program);
        try
        {
            void Tick(int count) { for (var scan = 0; scan < count; scan++) _PhysicsProcess(.02); }
            RunActiveController(); Tick(50);
            check(pallet.Position.IsEqualApprox(home) && runtime.Points["conveyor_run"] is false,
                "shipping_pallet_loaded_run_waits_for_start");
            check(ExecuteSelectedControllerAction("start-auto"), "shipping_pallet_start_button_accepted");
            Tick(50);
            check(MathF.Abs(pallet.Position.X - home.X - .75f) < .001f && runtime.Points["conveyor_run"] is true,
                "shipping_pallet_loaded_ladder_moves_at_configured_speed");
            StopActiveController(); var stopped = pallet.Position; Tick(50);
            check(pallet.Position.IsEqualApprox(stopped) && runtime.Points["conveyor_run"] is false,
                "shipping_pallet_loaded_ladder_stop_holds");
            check(Equals(runtime.Points["status_color"], "red"), "shipping_pallet_stop_tower_refreshes_without_motion_tick");
            RunActiveController(); Tick(10);
            check(pallet.Position.IsEqualApprox(stopped), "shipping_pallet_restart_requires_new_start");
            check(ExecuteSelectedControllerAction("start-auto"), "shipping_pallet_restart_start_button_accepted");
            Tick(400);
            check(runtime.Points["pickup_sensor"] is true && runtime.Points["conveyor_run"] is false
                && runtime.Points["cycle_complete"] is true,
                "shipping_pallet_loaded_ladder_stops_on_geometric_sensor");
            var endpoint = pallet.Position; Tick(50);
            check(pallet.Position.IsEqualApprox(endpoint), "shipping_pallet_loaded_ladder_endpoint_remains_held");
            ResetActiveController();
            check(pallet.Position.IsEqualApprox(home) && runtime.Points["pickup_sensor"] is false,
                "shipping_pallet_loaded_ladder_reset_restores_home");
            RunActiveController();
            check(ExecuteSelectedControllerAction("set-auto"), "shipping_pallet_manual_mode_selected");
            Tick(5);
            check(!ExecuteSelectedControllerAction("start-auto"), "shipping_pallet_manual_blocks_auto_start");
            check(ExecuteSelectedControllerAction("manual-jog"), "shipping_pallet_manual_request_accepted");
            Tick(50);
            check(pallet.Position.X > home.X + .7f && runtime.Points["conveyor_run"] is true,
                "shipping_pallet_manual_jog_moves_under_controller");
            Tick(75);
            var jogEnd = pallet.Position;
            check(jogEnd.X > home.X + 1.4f && jogEnd.X < home.X + 1.6f && runtime.Points["conveyor_run"] is false,
                "shipping_pallet_manual_jog_is_bounded");
            Tick(100);
            check(pallet.Position.IsEqualApprox(jogEnd), "shipping_pallet_manual_jog_does_not_repeat");
            check(ExecuteSelectedControllerAction("manual-jog"), "shipping_pallet_second_manual_jog_accepted");
            Tick(25); StopActiveController(); var jogStop = pallet.Position;
            RunActiveController(); Tick(125);
            check(pallet.Position.IsEqualApprox(jogStop) && runtime.Points["conveyor_run"] is false,
                "shipping_pallet_stopped_jog_requires_new_request");
            ResetActiveController();
            // Exercise the same diagnostic clock used by the native operator
            // view. The TP advances on its triggering scan, so its last active
            // scan is 99 (1.98 s), followed by expiry at scan 100 (2.00 s).
            var reviewWasEnabled = _visualSceneReview;
            _visualSceneReview = true;
            try
            {
                SetGantryReviewClockHeld(true); RunActiveController();
                check(GantryReviewClockHeld && _virtualController!.Snapshot.ScanNumber == 0
                    && pallet.Position.IsEqualApprox(home), "shipping_review_hold_freezes_home");
                check(ExecuteSelectedControllerAction("set-auto"), "shipping_review_manual_mode_selected");
                check(ExecuteSelectedControllerAction("manual-jog"), "shipping_review_actual_jog_button_accepted");
                AdvanceGantryReviewClock(99);
                check(_virtualController!.Snapshot.ScanNumber == 99 && runtime.Points["conveyor_run"] is true
                    && MathF.Abs(pallet.Position.X - home.X - 1.485f) < .001f,
                    "shipping_review_TP_last_active_scan_moves_supported_load");
                var lastActive = pallet.Position;
                AdvanceGantryReviewClock(1);
                check(_virtualController.Snapshot.ScanNumber == 100 && runtime.Points["conveyor_run"] is false
                    && pallet.Position.IsEqualApprox(lastActive), "shipping_review_TP_expiry_scan_holds_load");
                StepGantryReviewClock();
                check(_virtualController.Snapshot.ScanNumber == 125 && pallet.Position.IsEqualApprox(lastActive),
                    "shipping_review_coarse_step_does_not_retrigger_jog");
                StopActiveController(); AdvanceGantryReviewClock(1);
                check(_virtualController.Snapshot.ScanNumber == 125 && pallet.Position.IsEqualApprox(lastActive),
                    "shipping_review_Stop_blocks_manual_step");
                ResetActiveController();
                check(_virtualController.Snapshot.ScanNumber == 0 && pallet.Position.IsEqualApprox(home)
                    && runtime.Points["conveyor_run"] is false, "shipping_review_Reset_restores_home_while_held");
            }
            finally { ReleaseGantryReviewClock(); _visualSceneReview = reviewWasEnabled; }
        }
        finally { DisableVirtualController(); }
    }
}
