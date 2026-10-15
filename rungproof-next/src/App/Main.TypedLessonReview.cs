using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    // Exercises actual editor validation, selected controller actions, ordinary
    // engine physics and scene feedback. No direct plant/output/image writes.
    private async void VerifyTypedLessons()
    {
        var failures = 0;
        void Check(bool value, string name)
        {
            if (!value) failures++;
            GD.Print($"TYPED_LESSON_CHECK {name}={value}");
        }
        async Task Wait(double seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
        bool Point(string name) => _sceneRuntime!.Points[name] is true;
        string Text(string name) => Convert.ToString(_sceneRuntime!.Points[name]) ?? "";
        double Number(string name) => Convert.ToDouble(_sceneRuntime!.Points[name]);
        void Action(string name)
        {
            if (!ExecuteSelectedControllerAction(name)) throw new InvalidOperationException("Normal action rejected: " + name);
        }
        async Task Load(string scene)
        {
            DisableVirtualController();
            AddMigratedScene(scene, _candidateCatalog!, _mainCamera!, false, false);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (!_simulatorShell!.TryBuildCurrentLadderProgram(out var program, out var issues))
                throw new InvalidOperationException("Normal editor verification failed: " + string.Join("; ", issues.Select(i => i.Message)));
            Check(program.Id == scene + "-lesson", scene + "_normal_editor_scene_identity");
            EnableVirtualControllerProgram(program);
            RunActiveController(); await Wait(.06);
            Check(_virtualController!.Snapshot.State == VirtualControllerState.Running, scene + "_normal_run_loaded_controller");
        }
        try
        {
            await Load("lab-10-01-drive-alarm-code-string");
            Check(!Point("alarm_code_found") && !Point("drive_alarm_active"), "drive_empty_message_no_alarm");
            Action("toggle-drive_alarm_string_valid"); Action("next-alarm-message"); await Wait(.06);
            var readout = _sceneCompositionRoot!.GetNode<Node3D>("training_accessory_4").FindChild("StaticReadout", true, false) as Label3D;
            Check(Point("alarm_code_found") && Point("drive_alarm_active") && Point("alarm_match_valid")
                && readout is not null && readout.Text.Contains(Text("drive_alarm_text"), StringComparison.Ordinal), "drive_F003_actual_match_and_received_readout");
            Action("next-alarm-message"); await Wait(.06);
            Check(Text("drive_alarm_text").Contains("F030") && !Point("alarm_code_found") && !Point("drive_alarm_active"), "drive_F030_not_selected_code");
            Action("next-alarm-message"); await Wait(.06);
            Check(Text("drive_alarm_text").Contains("F0030") && !Point("alarm_code_found"), "drive_F0030_whole_token_rejected");
            for (var i = 0; i < 4; i++) Action("next-alarm-message");
            await Wait(.06); Check(Point("drive_alarm_active"), "drive_return_to_F003_match");
            Action("toggle-drive_alarm_string_valid"); await Wait(.06);
            Check(!Point("alarm_code_found") && !Point("alarm_match_valid") && !Point("drive_alarm_active"), "drive_invalid_message_removes_all_commands");
            Action("toggle-drive_alarm_string_valid"); Action("toggle-alarm_reset"); await Wait(.06);
            Check(!Point("alarm_code_found") && !Point("drive_alarm_active"), "drive_reset_dominates_valid_match");
            StopActiveController(); ResetActiveController();
            Check(Text("drive_alarm_text") == "" && !Point("drive_alarm_active"), "drive_reset_empty_message_and_outputs");

            await Load("lab-10-02-chicken-label-print");
            var paper = _sceneCompositionRoot!.GetNode<MeshInstance3D>("ChickenLabelPaper");
            var paperHome = paper.Transform;
            Check(!paper.Visible && !Point("product_present"), "chicken_initial_absent_product_hidden_paper");
            Action("toggle-label_data_valid"); Action("toggle-product_weighed"); await Wait(.6);
            Check(Point("product_weighed") && Math.Abs(Number("weight_kg") - 1.237) < 1e-9
                && !Point("print_request") && !Point("print_complete") && !paper.Visible, "chicken_actual_settled_weight_printer_off_blocks_print");
            // Preserve the original full-footprint support criterion at its
            // correct lifecycle boundary: actual loaded travel has settled.
            var deck = ReviewBounds((MeshInstance3D)_sceneCompositionRoot!.GetNode("training_accessory_5")
                .FindChild("WEIGH_DECK_surface", true, false));
            var food = ReviewBounds(_sceneCompositionRoot.GetNode<Node3D>("training_accessory_4"));
            Check(MathF.Abs(deck.End.Y - 0.9f) < 0.005f && MathF.Abs(food.Position.Y - deck.End.Y) < 0.005f
                && food.Position.X >= deck.Position.X && food.End.X <= deck.End.X
                && food.Position.Z >= deck.Position.Z && food.End.Z <= deck.End.Z,
                "chicken_settled_food_tray_supported_by_actual_weigh_deck");
            Action("toggle-printer_ready"); await Wait(.15);
            Check(Point("print_request") && !Point("print_complete") && paper.Visible && paper.Transform != paperHome,
                "chicken_loaded_format_command_moves_actual_print_paper");
            StopActiveController(); var frozenPaper = paper.Transform; var frozenWeight = Number("weight_kg"); await Wait(.2);
            Check(paper.Transform == frozenPaper && Number("weight_kg") == frozenWeight && !Point("print_request"), "chicken_stop_mid_print_freezes_paper_and_measurement");
            ResetActiveController();
            Check(!paper.Visible && paper.Transform == paperHome && !Point("product_present") && !Point("print_complete")
                && !Point("application_complete") && Number("weight_kg") == 0 && Text("label_text") == "", "chicken_reset_restores_absent_product_hidden_home_paper");
            RunActiveController(); await Wait(.06); Action("toggle-label_data_valid"); Action("toggle-product_weighed"); await Wait(.6);
            Action("toggle-printer_ready"); await Wait(.65);
            var printedPose = paper.Transform;
            Check(Point("print_complete") && Text("label_text") == "CHICKEN 1.237 kg" && !Point("print_request") && Point("apply_request"),
                "chicken_actual_print_completion_starts_separate_application");
            await Wait(.6);
            var captured = paper.GetNode<Label3D>("CapturedLabelText");
            Check(Point("application_complete") && Point("label_applied") && !Point("apply_request") && paper.Visible
                && paper.Transform != printedPose && captured.Text == "CHICKEN 1.237 kg", "chicken_application_moves_and_retains_exact_printed_text");
            Check(paper.GetNode<Label3D>("PrintedInk").Text == captured.Text, "chicken_actual_paper_ink_matches_captured_text");
            StopActiveController(); var appliedPose = paper.Transform; await Wait(.15);
            Check(paper.Transform == appliedPose && captured.Text == "CHICKEN 1.237 kg", "chicken_stop_retains_applied_paper_and_text");
            ResetActiveController();

            await Load("lab-10-04-motor-enum-state");
            var shaft = _sceneCompositionRoot!.GetNode<Node3D>("motor_0").FindChildren("KIN_motor_*", "", true, false).OfType<Node3D>().First();
            var shaftHome = shaft.Transform;
            Check(Text("motor_state") == "Stopped" && !Point("motor_running") && Point("state_valid"), "motor_initial_actual_enum_stopped");
            Action("toggle-start_request"); await Wait(.15);
            Check(Text("motor_state") == "Running" && Point("motor_running") && shaft.Transform != shaftHome, "motor_fresh_start_enum_drives_actual_shaft_motion");
            Action("toggle-stop_request"); await Wait(.06); var stoppedPose = shaft.Transform; await Wait(.15);
            Check(Text("motor_state") == "Stopped" && !Point("motor_running") && shaft.Transform == stoppedPose, "motor_stop_request_removes_command_and_holds_shaft");
            Action("toggle-stop_request"); await Wait(.06);
            Check(!Point("motor_running") && Text("motor_state") == "Stopped", "motor_held_start_cannot_restart_after_stop_permission");
            Action("toggle-fault_active"); Action("reset-motor-fault"); await Wait(.06);
            Check(Text("motor_state") == "Fault" && !Point("motor_running"), "motor_actual_fault_dominates_reset");
            Action("toggle-fault_active"); await Wait(.06);
            Check(Text("motor_state") == "Fault", "motor_fault_latch_retains_after_fault_input_clears");
            Action("reset-motor-fault"); await Wait(.06);
            Check(Text("motor_state") == "Stopped" && !Point("motor_running"), "motor_reset_clears_fault_but_held_start_stays_stopped");
            Action("toggle-start_request"); await Wait(.06); Action("toggle-start_request"); await Wait(.15);
            Check(Text("motor_state") == "Running" && Point("motor_running") && shaft.Transform != stoppedPose, "motor_new_start_edge_resumes_actual_motion");
            StopActiveController(); var heldShaft = shaft.Transform; await Wait(.15);
            Check(shaft.Transform == heldShaft && !Point("motor_running") && Text("motor_state") == "Stopped", "motor_controller_stop_holds_shaft_and_safe_enum");
            ResetActiveController();
            Check(shaft.Transform == shaftHome && Text("motor_state") == "Stopped" && !Point("start_request")
                && !Point("motor_running") && _virtualController!.Snapshot.ScanNumber == 0, "motor_reset_restores_home_inputs_enum_and_scan_zero");
        }
        catch (Exception error) { failures++; GD.PushError("TYPED_LESSON_EXCEPTION " + error); }
        GD.Print($"TYPED_LESSON_VERIFY {(failures == 0 ? "PASS" : "FAIL")} failures={failures}; ordinary editor/controller/physics feedback; native pixel acceptance separate");
        GD.Print("REAL_PLC_TRANSPORT_CONSTRUCTED FALSE"); GD.Print("REAL_PLC_CONNECTION_ATTEMPTED FALSE");
        DisableVirtualController(); GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
