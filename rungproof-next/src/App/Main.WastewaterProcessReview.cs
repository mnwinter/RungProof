using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditWastewaterProcess;
    private void AuditWastewaterProcess()
    {
        var failures = 0;
        var checks = 0;
        void Check(bool ok, string name)
        {
            checks++;
            if (!ok) failures++;
            GD.Print($"WASTEWATER_PROCESS_CHECK {name}={ok}");
        }
        try
        {
            AddMigratedScene("lab-11-06-wastewater-collection", _candidateCatalog!, _mainCamera!, false, false);
            var runtime = _sceneRuntime!;
            var root = _sceneCompositionRoot!;
            double Point(string name) => Convert.ToDouble(runtime.Points[name]);
            bool On(string name) => Convert.ToBoolean(runtime.Points[name]);
            void Tick(int count) { for (var i = 0; i < count; i++) runtime.AdvanceSimulation(.02); }
            void Commands(bool pump, bool valve) => runtime.CommitVirtualControllerOutputs(
                new Dictionary<string, bool> { ["transfer_pump_run"] = pump, ["outlet_valve_open"] = valve });
            var valveAdapter = root.GetNode<Node3D>("valve_4").FindChildren("*", string.Empty, true, false)
                .OfType<EquipmentMotionController>().Single();
            bool ValveFeedbackMatches() => Math.Abs(Point("outlet_valve_position_percent")
                - valveAdapter.InputPositionNormalized * 100.0) < 1e-8;
            Check(ValveFeedbackMatches() && Point("outlet_valve_position_percent") == 0,
                "initial_closed_valve_feedback_matches_adapter_without_tick");
            Commands(false, true);
            Check(ValveFeedbackMatches() && Point("outlet_valve_position_percent") == 100,
                "open_commit_refreshes_actual_valve_feedback_without_tick");
            runtime.SetControllerPlaybackRunning(false);
            Check(ValveFeedbackMatches() && Point("outlet_valve_position_percent") == 100 && On("outlet_valve_open"),
                "stop_immediately_reports_retained_actual_valve_without_rewriting_command");
            Commands(false, false);
            Check(ValveFeedbackMatches() && Point("outlet_valve_position_percent") == 0,
                "close_commit_refreshes_actual_valve_feedback_while_stopped_without_tick");
            Commands(false, true);
            runtime.ResetSimulation();
            Check(ValveFeedbackMatches() && Point("outlet_valve_position_percent") == 0 && !On("outlet_valve_open"),
                "reset_from_open_refreshes_closed_actual_valve_feedback_without_tick");
            // Shell composition selects the controller clock even before a
            // program is loaded. This explicit branch tests the standalone
            // runtime, without changing the product's shell selection policy.
            runtime.UsesExternalClock = false;
            runtime.ExecuteAction("toggle-inflow_enabled");
            Check(runtime.RunDefault(), "unauthored_exercise_can_start_without_controller");
            Tick(10);
            Check(Point("source_level_percent") > 35 && !On("transfer_pump_run") && !On("outlet_valve_open"),
                "standalone_inflow_does_not_synthesize_plc_outputs");
            runtime.StopSimulation();
            var standaloneLevel = Point("source_level_percent");
            Tick(10);
            Check(Point("source_level_percent") == standaloneLevel, "standalone_stop_freezes_process");
            runtime.ResetSimulation();
            runtime.UsesExternalClock = true;
            runtime.SetControllerPlaybackRunning(true);
            Check(Math.Abs(Point("source_level_percent") - 35) < 1e-8 && !On("source_level_high"), "initial_measured_bank_level");
            Check(runtime.SampleVirtualControllerNumericInputs().ContainsKey("source_level_ma"), "analog_feedback_sampled_for_virtual_controller");
            Check(!runtime.ExecuteAction("toggle-source_level_high"), "derived_level_feedback_cannot_be_manually_toggled");
            runtime.ExecuteAction("toggle-inflow_enabled");
            runtime.AdvanceSimulation(.5);
            Check(Math.Abs(Point("source_level_percent") - 37) < 1e-8, "large_caller_interval_substeps_without_dropping_time");
            runtime.ResetSimulation();
            Commands(true, true);
            Tick(100);
            Check(Math.Abs(Point("source_level_percent") - 35) < 1e-8 && On("transfer_inhibited"), "held_bad_command_does_not_bypass_missing_permissives");
            Commands(false, false);
            runtime.ExecuteAction("toggle-inflow_enabled");
            Tick(376);
            Check(On("source_level_high") && Point("source_level_percent") >= 65, "inflow_drives_measured_high_feedback");
            var tanks = new[] { "tank_0", "tank_1", "tank_2", "training_accessory_6" }
                .Select(id => root.GetNode<Node3D>(id)).ToArray();
            var scales = tanks.Select(t => ((Node3D)t.FindChild("KIN_liquid", true, false)).Scale.Y).ToArray();
            Check(scales.All(s => Math.Abs(s - scales[0]) < 1e-5), "four_common_header_vessels_share_visual_fraction");
            runtime.ExecuteAction("toggle-treatment_ready");
            runtime.ExecuteAction("toggle-outlet_clear");
            Commands(true, true);
            Tick(10);
            Check(Point("actual_transfer_percent_per_second") > 0 && Point("outlet_valve_position_percent") > 0,
                "actual_installed_open_valve_permits_transfer");
            runtime.ExecuteAction("toggle-inflow_enabled");
            Tick(300);
            Check(!On("source_level_high") && Point("source_level_percent") <= 20, "drain_clears_hysteretic_high_feedback");
            var level = Point("source_level_percent");
            runtime.SetControllerPlaybackRunning(false);
            Tick(100);
            Check(Point("source_level_percent") == level && Point("actual_transfer_percent_per_second") == 0,
                "stop_freezes_quantity_and_zeroes_actual_flow");
            runtime.SetControllerPlaybackRunning(true);
            Tick(1);
            Check(Point("source_level_percent") < level, "resume_advances_retained_level");
            runtime.ExecuteAction("toggle-outlet_clear");
            level = Point("source_level_percent");
            Tick(100);
            Check(Point("source_level_percent") == level && On("transfer_pump_run") && On("transfer_inhibited"),
                "outlet_loss_inhibits_plant_without_fabricating_plc_command");
            runtime.ResetSimulation();
            Check(Point("source_level_percent") == 35 && !On("inflow_enabled") && !On("transfer_pump_run")
                && !On("outlet_valve_open"), "reset_restores_process_fixture_and_commands");
        }
        catch (Exception ex) { failures++; GD.PushError(ex.ToString()); }
        GD.Print($"WASTEWATER_PROCESS_VERIFY {(failures == 0 ? "PASS" : "FAIL")} checks={checks} failures={failures}; illustrative offline process, native review pending");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
