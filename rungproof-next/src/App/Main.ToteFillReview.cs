using System;
using System.Collections.Generic;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyToteFill(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = true; runtime.SetControllerPlaybackRunning(true);
        bool On(string name) => runtime.Points[name] is true;
        double Number(string name) => Convert.ToDouble(runtime.Points[name]);
        void Tick(int count) { for (var tick = 0; tick < count; tick++) runtime.AdvanceSimulation(.02); }
        void Command(bool conveyor, bool valve) => runtime.CommitVirtualControllerOutputs(
            new Dictionary<string, bool> { ["conveyor_run"] = conveyor, ["fill_valve_open"] = valve });
        var tote = _sceneCompositionRoot!.GetNode<Node3D>("finishing_tote");
        var cap = (MeshInstance3D)tote.FindChild("IBC_fill_cap", true, false);
        var witness = (MeshInstance3D)tote.FindChild("IBC_fill_witness", true, false);
        var stream = (Node3D)_sceneCompositionRoot.GetNode<Node3D>("finishing_fill_valve").FindChild("FINISHING_stream", true, false);
        Command(false, true); Tick(25);
        check(Number("fill_percent") == 0 && On("fill_inhibited") && On("fill_valve_open") && !stream.Visible,
            "tote_off_station_fill_is_inhibited_without_rewriting_plc_command");
        Command(true, true); Tick(214);
        check(Number("fill_percent") == 0 && On("tote_at_fill") && On("fill_inhibited"),
            "tote_conveyor_command_prevents_filling_while_crossing_station");
        Command(false, true); Tick(100);
        check(Math.Abs(Number("fill_percent") - 40) < .0001,
            "tote_fill_aligned_station_integrates_two_seconds_to_forty_percent");
        var forty = ReviewBounds(witness);
        check(witness.Visible && stream.Visible && Math.Abs(Number("actual_fill_percent_per_second") - 20) < .0001,
            "tote_actual_fill_projects_witness_and_stream_with_effective_rate");
        runtime.SetControllerPlaybackRunning(false); Tick(50);
        check(Math.Abs(Number("fill_percent") - 40) < .0001 && Number("actual_fill_percent_per_second") == 0 && !stream.Visible,
            "tote_stop_retains_quantity_and_hides_effective_stream");
        runtime.SetControllerPlaybackRunning(true); Tick(50);
        check(Math.Abs(Number("fill_percent") - 60) < .0001, "tote_run_resumes_retained_fill");
        Command(false, false); Tick(25);
        check(Math.Abs(Number("fill_percent") - 60) < .0001 && Number("actual_fill_percent_per_second") == 0 && !stream.Visible,
            "tote_valve_command_withdrawal_holds_quantity");
        cap.Visible = true; Command(false, true); Tick(25);
        check(Math.Abs(Number("fill_percent") - 60) < .0001 && On("fill_inhibited") && !stream.Visible,
            "tote_visible_cap_prevents_modeled_filling");
        cap.Visible = false; Tick(50);
        var homeZ = tote.Position.Z; tote.Position += new Vector3(0, 0, .3f); Tick(25);
        check(Math.Abs(Number("fill_percent") - 80) < .0001 && On("fill_inhibited") && !stream.Visible,
            "tote_fill_uses_actual_nozzle_bore_alignment_beyond_station_x_feedback");
        tote.Position = new Vector3(tote.Position.X, tote.Position.Y, homeZ); Tick(50); Tick(50);
        check(Number("fill_percent") == 100 && On("fill_complete") && On("fill_inhibited")
            && Number("actual_fill_percent_per_second") == 0 && On("fill_valve_open") && !stream.Visible,
            "tote_held_valve_caps_quantity_and_removes_effective_flow_without_rewriting_command");
        var full = ReviewBounds(witness);
        check(MathF.Abs(full.Position.Y - forty.Position.Y) < .00001f && MathF.Abs(full.Size.Y - forty.Size.Y*2.5f) < .0001f,
            "tote_witness_height_tracks_fraction_with_fixed_bottom");
        runtime.ResetSimulation();
        check(Number("fill_percent") == 0 && !On("fill_complete") && !witness.Visible && !cap.Visible,
            "tote_reset_empties_witness_and_restores_open_cap");
        Command(true, false); Tick(214); Command(false, true); Tick(100);
        check(Math.Abs(Number("fill_percent") - 40) < .0001 && MathF.Abs(ReviewBounds(witness).Size.Y - forty.Size.Y) < .0001f,
            "tote_repeated_reset_preserves_authored_witness_height");
        runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(false);
    }
}
