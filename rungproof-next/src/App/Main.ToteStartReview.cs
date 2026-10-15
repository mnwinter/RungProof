using System;
using System.Collections.Generic;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyToteStartCommand(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-21-tote-finishing", _candidateCatalog!, _mainCamera!, false, false);
        var document = new LadderEditorDocument();
        document.ResetProject("review-tote-start-binding", "Tote_Start_Binding_Probe", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-2-21-tote-finishing";
        document.AddTag("start_request", PlcVariableRole.Input, "operator.start");
        document.AddTag("start_seen", PlcVariableRole.Memory);
        foreach (var name in new[] { "conveyor_run", "fill_valve_open", "capper_run", "labeler_run", "inspection_run" })
            document.AddTag(name, PlcVariableRole.Output, name);
        var rung = document.Rungs.Count;
        document.AddRung("Capture the operator Start pulse without commanding actuators", "start_seen").CoilMode = LadderCoilMode.Set;
        document.AddContact(rung, 0, "start_request", false);
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            RunActiveController();
            _PhysicsProcess(.02);
            check(!_virtualController!.Snapshot.Variables.GetValueOrDefault("start_seen"), "tote_start_run_alone_does_not_pulse_start");
            var accepted = ExecuteSelectedControllerAction("start-line");
            _PhysicsProcess(.02);
            check(accepted && _virtualController.Snapshot.Variables.GetValueOrDefault("start_seen"),
                "tote_start_3d_action_reaches_declared_ladder_start_input");
            _PhysicsProcess(.02);
            check(!_virtualController.Snapshot.Variables.GetValueOrDefault("start_request"), "tote_start_command_is_a_single_scan_pulse");
            ResetActiveController();
            check(!_virtualController.Snapshot.Variables.GetValueOrDefault("start_seen"), "tote_start_reset_clears_probe_state");
            check(!ExecuteSelectedControllerAction("start-line"), "tote_start_stopped_controller_rejects_command");
        }
        finally { DisableVirtualController(); }
        var runtime = _sceneRuntime!;
        runtime.ResetSimulation(); runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        var tote = _sceneCompositionRoot!.GetNode<Node3D>("finishing_tote");
        var home = tote.Position.X;
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_run"] = true });
        for (var tick = 0; tick < 200; tick++) runtime.AdvanceSimulation(.02);
        check(MathF.Abs(tote.Position.X - (home + 3f)) < .001f,
            "tote_plc_conveyor_command_moves_actual_tote_at_declared_speed");
        var held = tote.Transform;
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_run"] = false });
        for (var tick = 0; tick < 25; tick++) runtime.AdvanceSimulation(.02);
        check(tote.Transform == held, "tote_withdrawn_conveyor_command_retains_position");
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_run"] = true });
        runtime.SetControllerPlaybackRunning(false);
        for (var tick = 0; tick < 25; tick++) runtime.AdvanceSimulation(.02);
        check(tote.Transform == held, "tote_stopped_controller_clock_retains_position");
        runtime.SetControllerPlaybackRunning(true);
        var seen = new HashSet<string>();
        var supported = true;
        var beltMesh = (MeshInstance3D)_sceneCompositionRoot.GetNode<Node3D>("finishing_conveyor").FindChild("KIN_belt_surface", true, false);
        var belt = ReviewBounds(beltMesh);
        for (var tick = 0; tick < 1000; tick++)
        {
            runtime.AdvanceSimulation(.02);
            foreach (var point in new[] { "tote_at_fill", "tote_at_cap", "tote_at_label", "tote_at_inspection" })
                if (runtime.Points[point] is true) seen.Add(point);
            var load = ReviewBounds(tote);
            supported &= tote.Visible && MathF.Abs(load.Position.Y - belt.End.Y) < .002f
                && load.Position.X >= belt.Position.X && load.End.X <= belt.End.X
                && load.Position.Z >= belt.Position.Z && load.End.Z <= belt.End.Z;
        }
        check(seen.Count == 4, "tote_controller_travel_reports_each_installed_station_window");
        check(supported && runtime.Points["tote_at_exit"] is true && MathF.Abs(tote.Position.X - 6.4f) < .001f,
            "tote_controller_travel_retains_supported_exit_without_wraparound");
        var exit = tote.Transform;
        for (var tick = 0; tick < 25; tick++) runtime.AdvanceSimulation(.02);
        check(tote.Transform == exit, "tote_held_run_at_exit_retains_visible_tote");
        var reviewEnabled = _visualSceneReview;
        var previousFocus = _visualReviewFocusId;
        var previousClose = _visualReviewClose;
        var direction = new Vector3(8, 6, 8);
        try
        {
            _visualSceneReview = true;
            _visualReviewFocusId = "finishing_tote";
            _visualReviewClose = true;
            SetVisualReviewAngle(direction, "tote-exit-reset-probe");
            var exitTarget = _cameraController!.ViewTarget;
            ResetActiveController();
            var expectedTravel = new Vector3(home - 6.4f, 0, 0);
            check((_cameraController.ViewTarget - exitTarget).IsEqualApprox(expectedTravel)
                && _sceneCameraDirection == direction && _visualReviewClose
                && _visualReviewFocusId == "finishing_tote",
                "tote_reset_refits_selected_close_focus_to_infeed_preserving_angle");
        }
        finally
        {
            _visualSceneReview = reviewEnabled;
            _visualReviewFocusId = previousFocus;
            _visualReviewClose = previousClose;
        }
        runtime.SetControllerPlaybackRunning(false);
        check(MathF.Abs(tote.Position.X - home) < .001f && runtime.Points["tote_at_exit"] is false,
            "tote_controller_reset_restores_infeed_and_clears_exit_feedback");
    }
}
