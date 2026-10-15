using System;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyBottleShuttlePlacement(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-20-bottle-shuttle", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var conveyor = root.GetNode<Node3D>("shuttle_conveyor");
        var bottle = root.GetNode<Node3D>("shuttle_bottle");
        var body = (MeshInstance3D)bottle.FindChild("BOTTLE_BODY", true, false);
        var belt = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_belt_surface", true, false));
        GD.Print($"BOTTLE_SHUTTLE_DATUM body={ReviewBounds(body)} belt={belt} root={bottle.Position}");
        check(MathF.Abs(ReviewBounds(body).Position.Y - belt.End.Y) < 0.0001f,
            "bottle_shuttle_body_seated_on_belt");
        var tail = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_tail_drum", true, false)).GetCenter().X;
        var drive = ReviewBounds((MeshInstance3D)conveyor.FindChild("KIN_drive_drum", true, false)).GetCenter().X;
        var start = bottle.Position;
        var supported = true;
        // Authored route samples screen support; they do not prove runtime
        // reversal, sensor timing, acceleration, slip or dynamic stability.
        for (var index = 0; index <= 120; index++)
        {
            bottle.Position = new Vector3(-3 + index * 0.05f, start.Y, start.Z);
            var bounds = ReviewBounds(body);
            supported &= MathF.Abs(bounds.Position.Y - belt.End.Y) < 0.0001f
                && bounds.Position.X > MathF.Min(tail, drive) && bounds.End.X < MathF.Max(tail, drive)
                && bounds.Position.Z > belt.Position.Z && bounds.End.Z < belt.End.Z;
        }
        bottle.Position = start;
        check(supported, "bottle_shuttle_body_supported_through_121_authored_route_samples");

        var label = ReviewBounds((MeshInstance3D)bottle.FindChild("BOTTLE_LABEL", true, false));
        var band = ReviewBounds((MeshInstance3D)bottle.FindChild("BOTTLE_LABEL_BAND", true, false));
        var process = bottle.FindChild("BOTTLE_PRINT_PROCESS", true, false) as MeshInstance3D;
        var capacity = bottle.FindChild("BOTTLE_PRINT_CAPACITY", true, false) as MeshInstance3D;
        bool WithinPanel(MeshInstance3D? text, Aabb panel)
        {
            if (text is null) return false;
            var bounds = ReviewBounds(text);
            GD.Print($"BOTTLE_LABEL_DATUM {text.Name}={bounds} panel={panel}");
            return bounds.Position.X > panel.Position.X + 0.005f && bounds.End.X < panel.End.X - 0.005f
                && bounds.Position.Y > panel.Position.Y + 0.005f && bounds.End.Y < panel.End.Y - 0.005f;
        }
        check(WithinPanel(process, label) && process is not null && ReviewBounds(process).Position.Y > band.End.Y + 0.005f,
            "bottle_shuttle_process_legend_fits_label_above_color_band");
        check(WithinPanel(capacity, band), "bottle_shuttle_capacity_legend_fits_color_band");

        var sensors = new[] { root.GetNode<Node3D>("left_sensor"), root.GetNode<Node3D>("right_sensor") };
        check(sensors.All(sensor => ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString().EndsWith("_foot", StringComparison.Ordinal))
            .All(mesh => MathF.Abs(ReviewBounds(mesh).Position.Y) < 0.001f)), "bottle_shuttle_sensor_feet_grounded");
        var clear = true;
        foreach (var sensor in sensors)
        foreach (var part in ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)))
        foreach (var fixedPart in ReviewMeshes(conveyor))
        {
            var overlap = ReviewBounds(part).Intersection(ReviewBounds(fixedPart)).Size;
            if (overlap.X > 0.005f && overlap.Y > 0.005f && overlap.Z > 0.005f)
            {
                clear = false;
                GD.Print($"BOTTLE_SENSOR_CLEARANCE {sensor.Name}/{part.Name} vs {fixedPart.Name} overlap={overlap}");
            }
        }
        check(clear, "bottle_shuttle_sensor_mounts_and_cable_bounds_clear_conveyor");
        var neighborsClear = true;
        foreach (var sensor in sensors)
        foreach (var part in ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)))
        foreach (var neighbor in new[] { root.GetNode<Node3D>("shuttle_start"), root.GetNode<Node3D>("shuttle_status") })
        foreach (var fixedPart in ReviewMeshes(neighbor))
        {
            var overlap = ReviewBounds(part).Intersection(ReviewBounds(fixedPart)).Size;
            if (overlap.X <= 0.005f || overlap.Y <= 0.005f || overlap.Z <= 0.005f) continue;
            neighborsClear = false;
            GD.Print($"BOTTLE_SENSOR_NEIGHBOR {sensor.Name}/{part.Name} vs {neighbor.Name}/{fixedPart.Name} overlap={overlap}");
        }
        check(neighborsClear, "bottle_shuttle_sensor_bounds_clear_start_station_and_status");
        var optical = true;
        for (var index = 0; index < sensors.Length; index++)
        {
            bottle.Position = new Vector3(index == 0 ? -3 : 3, start.Y, start.Z);
            for (var side = 0; side < sensors.Length; side++)
            {
                var from = ReviewBounds((MeshInstance3D)sensors[side].FindChild("RX_lens", true, false)).GetCenter();
                var to = ReviewBounds((MeshInstance3D)sensors[side].FindChild("TX_lens", true, false)).GetCenter();
                // The existing double-sided segment/triangle helper applies
                // to any mesh, including this cylindrical bottle body.
                optical &= LineHitsCableSurface(from, to, body) == (side == index);
            }
        }
        bottle.Position = start;
        check(optical, "bottle_shuttle_endpoint_rays_cross_only_the_corresponding_body_mesh");
        VerifyBottleShuttleMotion(check, bottle, body, sensors);
        VerifyBottleShuttleController(check);
    }

    private void VerifyBottleShuttleMotion(Action<bool, string> check, Node3D bottle, MeshInstance3D body, Node3D[] sensors)
    {
        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = false;
        runtime.ResetSimulation();
        var home = bottle.Position;
        var beltController = _sceneCompositionRoot!.GetNode<Node3D>("shuttle_conveyor")
            .FindChildren("*", string.Empty, true, false).OfType<ConveyorController>().Single();
        bool OpticalMatch() => sensors.Select((sensor, index) =>
        {
            var tx = ReviewBounds((MeshInstance3D)sensor.FindChild("TX_lens", true, false)).GetCenter();
            var rx = ReviewBounds((MeshInstance3D)sensor.FindChild("RX_lens", true, false)).GetCenter();
            return LineHitsCableSurface(tx, rx, body) == (runtime.Points[index == 0 ? "left_sensor_active" : "right_sensor_active"] is true);
        }).All(value => value);
        runtime.RunDefault();
        check(OpticalMatch() && runtime.Points["left_sensor_active"] is true,
            "bottle_shuttle_start_keeps_actual_left_feedback");
        runtime.AdvanceSimulation(0.5);
        check(MathF.Abs(bottle.Position.X - home.X - 0.375f) < 0.001f && OpticalMatch()
            && MathF.Abs(beltController.ActualSpeedMps - 0.75f) < 0.001f && !beltController.IsPhysicsProcessing(),
            "bottle_shuttle_half_second_uses_configured_speed");
        check(!runtime.RunDefault(), "bottle_shuttle_active_start_rejected");
        runtime.StopSimulation(); var held = bottle.Position;
        runtime.AdvanceSimulation(0.5);
        check(bottle.Position.IsEqualApprox(held) && runtime.Points["motor_run"] is false && OpticalMatch()
            && beltController.ActualSpeedMps == 0,
            "bottle_shuttle_stop_holds_pose_and_actual_feedback");
        check(runtime.RunDefault() && bottle.Position.IsEqualApprox(held), "bottle_shuttle_resume_does_not_teleport");
        runtime.AdvanceSimulation(0.5);
        check(MathF.Abs(bottle.Position.X - held.X - 0.375f) < 0.001f,
            "bottle_shuttle_resume_advances_from_held_pose_at_configured_speed");
        var allOptical = true; var reversed = false; var firstContact = false; var reversalX = 0f;
        for (var tick = 0; tick < 2400 && runtime.IsRunning; tick++)
        {
            var direction = runtime.Points["motor_direction"];
            var beforeX = bottle.Position.X;
            runtime.AdvanceSimulation(1.0 / 120);
            allOptical &= OpticalMatch();
            if (Equals(direction, 1L) && Equals(runtime.Points["motor_direction"], -1L))
            {
                // A tick may contain the first contact and a little return
                // travel. Final feedback must match that final pose, not latch
                // a contact that has already cleared during the same tick.
                reversed = true;
                reversalX = bottle.Position.X;
                // Infer the reversal position from outward + return travel in
                // this tick, then independently probe either side with the
                // review's segment/triangle helper. A near-endpoint reversal
                // alone would not prove first contact.
                var inferredTrip = (beforeX + 0.75f / 120 + reversalX) / 2;
                var returnPose = bottle.Position;
                var tx = ReviewBounds((MeshInstance3D)sensors[1].FindChild("TX_lens", true, false)).GetCenter();
                var rx = ReviewBounds((MeshInstance3D)sensors[1].FindChild("RX_lens", true, false)).GetCenter();
                bottle.Position = new Vector3(inferredTrip - 0.0001f, returnPose.Y, returnPose.Z);
                var outside = !LineHitsCableSurface(tx, rx, body);
                bottle.Position = new Vector3(inferredTrip + 0.0001f, returnPose.Y, returnPose.Z);
                firstContact = outside && LineHitsCableSurface(tx, rx, body);
                bottle.Position = returnPose;
                runtime.StopSimulation(); var returnHeld = bottle.Position;
                runtime.AdvanceSimulation(0.5);
                var stationary = bottle.Position.IsEqualApprox(returnHeld);
                runtime.RunDefault(); runtime.AdvanceSimulation(0.1);
                check(stationary && MathF.Abs(bottle.Position.X - returnHeld.X + 0.075f) < 0.001f
                    && MathF.Abs(beltController.ActualSpeedMps + 0.75f) < 0.001f,
                    "bottle_shuttle_return_leg_stop_resumes_left");
            }
        }
        check(reversed && firstContact && reversalX > 2.7f && reversalX < 3 && allOptical,
            "bottle_shuttle_reverses_at_actual_right_body_crossing_and_feedback_matches_every_tick");
        check(!runtime.IsRunning && runtime.Points["motor_run"] is false && runtime.Points["cycle_complete"] is true
            && runtime.Points["left_sensor_active"] is true && OpticalMatch(), "bottle_shuttle_completes_on_actual_left_crossing");
        runtime.ResetSimulation(); runtime.RunDefault(); runtime.AdvanceSimulation(30);
        check(!runtime.IsRunning && runtime.Points["cycle_complete"] is true && OpticalMatch()
            && bottle.Position.X > -3 && bottle.Position.X < -2.7,
            "bottle_shuttle_large_step_consumes_both_sensor_transitions_without_overshoot");
        runtime.ResetSimulation();
        check(bottle.Position.IsEqualApprox(home) && runtime.Points["cycle_complete"] is false && OpticalMatch(),
            "bottle_shuttle_reset_restores_supported_home_and_actual_feedback");
        // A run command without a direction must hold position. The reference
        // cannot manufacture a direction for the selected controller.
        runtime.UsesExternalClock = true;
        runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["motor_run"] = true });
        var startBlocked = !runtime.RunDefault(); runtime.AdvanceSimulation(1);
        check(startBlocked && bottle.Position.IsEqualApprox(home) && runtime.Points["motor_run"] is true
            && Equals(runtime.Points["motor_direction"], 0L) && OpticalMatch(),
            "bottle_shuttle_reference_does_not_replace_selected_controller_commands_or_pose");
        runtime.UsesExternalClock = false; runtime.SetExternalPlayback(true, true);
        check(!runtime.RunDefault(), "bottle_shuttle_reference_blocked_when_external_playback_selected");
        runtime.SetExternalPlayback(false, false); runtime.ResetSimulation();
    }

    private void VerifyBottleShuttleController(Action<bool, string> check)
    {
        const string sceneId = "lab-2-20-bottle-shuttle";
        var catalog = SceneCatalogLoader.LoadCatalog("res://scenes/catalog/original-scenes.catalog.json");
        var scene = SceneCatalogLoader.LoadScene(catalog.Scenes.Single(item => item.Id == sceneId));
        var reference = LadderEditorProjectJson.Load(FileAccess.GetFileAsString(
            "res://programs/examples/02-bottle-shuttle-reference.rpproj.json"));
        check(reference.IsReadable && reference.Document is not null, "bottle_shuttle_controller_reference_readable");
        if (reference.Document is null) return;
        var program = reference.Document.BuildProgram();
        var issues = SceneIoBindingValidator.Validate(program, ReadScenePoints(scene));
        check(issues.Count == 0 && LadderCompiler.Compile(program).IsValid,
            "bottle_shuttle_controller_reference_matches_scene_io");
        foreach (var issue in issues) GD.Print($"BOTTLE_CONTROLLER_BINDING {issue.Message}");
        if (issues.Count > 0) return;
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var bottle = root.GetNode<Node3D>("shuttle_bottle");
        var body = (MeshInstance3D)bottle.FindChild("BOTTLE_BODY", true, false);
        var belt = ReviewBounds((MeshInstance3D)root.GetNode("shuttle_conveyor").FindChild("KIN_belt_surface", true, false));
        var beltController = root.GetNode("shuttle_conveyor").FindChildren("*", string.Empty, true, false)
            .OfType<ConveyorController>().Single();
        bool OpticalMatch() => new[] { "left_sensor", "right_sensor" }.Select((id, index) =>
        {
            var sensor = root.GetNode(id);
            var from = ReviewBounds((MeshInstance3D)sensor.FindChild("TX_lens", true, false)).GetCenter();
            var to = ReviewBounds((MeshInstance3D)sensor.FindChild("RX_lens", true, false)).GetCenter();
            return LineHitsCableSurface(from, to, body)
                == (runtime.Points[index == 0 ? "left_sensor_active" : "right_sensor_active"] is true);
        }).All(value => value);
        bool Supported()
        {
            var load = ReviewBounds(body);
            return MathF.Abs(load.Position.Y - belt.End.Y) < 0.0001f
                && load.Position.X > belt.Position.X && load.End.X < belt.End.X
                && load.Position.Z > belt.Position.Z && load.End.Z < belt.End.Z;
        }
        EnableVirtualControllerProgram(program);
        try
        {
            check(_virtualController is not null && !ExecuteSelectedControllerAction("start-shuttle"),
                "bottle_shuttle_machine_start_requires_running_controller");
            RunActiveController(); _PhysicsProcess(0.02);
            var home = bottle.Position;
            check(runtime.Points["motor_run"] is false && Equals(runtime.Points["motor_direction"], 0L)
                && bottle.Position.IsEqualApprox(home), "bottle_shuttle_controller_run_alone_does_not_start_machine");
            var accepted = ExecuteSelectedControllerAction("start-shuttle"); _PhysicsProcess(0.5);
            check(accepted && MathF.Abs(bottle.Position.X - home.X - 0.375f) < 0.001f
                && Equals(runtime.Points["motor_direction"], 1L) && runtime.Points["motor_run"] is true
                && OpticalMatch() && Supported() && !beltController.IsPhysicsProcessing(),
                "bottle_shuttle_operator_start_drives_actual_loaded_ladder_scan_and_plant");
            var beforeRepeat = bottle.Position;
            ExecuteSelectedControllerAction("start-shuttle"); _PhysicsProcess(0.5);
            check(MathF.Abs(bottle.Position.X - beforeRepeat.X - 0.375f) < 0.001f,
                "bottle_shuttle_repeated_machine_start_does_not_restart_loaded_controller_travel");
            ExecuteSelectedControllerAction("stop-shuttle"); var held = bottle.Position; _PhysicsProcess(0.5);
            check(bottle.Position.IsEqualApprox(held) && runtime.Points["motor_run"] is false
                && Equals(runtime.Points["motor_direction"], 0L) && beltController.ActualSpeedMps == 0
                && Equals(runtime.Points["status_color"], "amber"),
                "bottle_shuttle_normal_playback_stop_holds_supported_pose_and_clears_outputs");
            RunActiveController(); _PhysicsProcess(0.5);
            check(MathF.Abs(bottle.Position.X - held.X - 0.375f) < 0.001f
                && Equals(runtime.Points["motor_direction"], 1L),
                "bottle_shuttle_normal_playback_run_resumes_outward_step");
            var optical = true; var supported = true; var reached = false;
            for (var scan = 0; scan < 500; scan++)
            {
                _PhysicsProcess(0.02); optical &= OpticalMatch(); supported &= Supported();
                if (runtime.Points["right_sensor_active"] is not true) continue;
                reached = Equals(runtime.Points["motor_direction"], 1L);
                GD.Print($"BOTTLE_CONTROLLER_RIGHT_SENSOR x={bottle.Position.X} direction={runtime.Points["motor_direction"]}");
                break;
            }
            check(reached && optical && supported,
                "bottle_shuttle_actual_right_contact_is_feedback_before_controller_reversal");
            _PhysicsProcess(0.02);
            check(Equals(runtime.Points["motor_direction"], -1L) && _virtualSnapshot!.Variables["right_sensor_active"],
                "bottle_shuttle_loaded_ladder_reverses_on_next_scan_of_right_feedback");
            ExecuteSelectedControllerAction("stop-shuttle"); held = bottle.Position; _PhysicsProcess(0.5);
            var returnStopped = bottle.Position.IsEqualApprox(held) && beltController.ActualSpeedMps == 0;
            RunActiveController(); _PhysicsProcess(0.5);
            check(returnStopped && MathF.Abs(bottle.Position.X - held.X + 0.375f) < 0.001f
                && Equals(runtime.Points["motor_direction"], -1L), "bottle_shuttle_normal_stop_run_resumes_return_step");
            var completed = false;
            for (var scan = 0; scan < 500; scan++)
            {
                _PhysicsProcess(0.02); optical &= OpticalMatch(); supported &= Supported();
                if (runtime.Points["motor_run"] is not false) continue;
                completed = runtime.Points["left_sensor_active"] is true && runtime.Points["cycle_complete"] is true
                    && Equals(runtime.Points["motor_direction"], 0L);
                GD.Print($"BOTTLE_CONTROLLER_LEFT_COMPLETE x={bottle.Position.X} complete={completed}");
                break;
            }
            check(completed && optical && supported && bottle.Position.X > -3 && bottle.Position.X < -2.7f,
                "bottle_shuttle_loaded_controller_completes_supported_round_trip_with_actual_optics");
            held = bottle.Position; _PhysicsProcess(1);
            check(bottle.Position.IsEqualApprox(held) && runtime.Points["motor_run"] is false,
                "bottle_shuttle_completed_controller_does_not_automatically_repeat");
            ExecuteSelectedControllerAction("start-shuttle"); _PhysicsProcess(0.5);
            check(MathF.Abs(bottle.Position.X - held.X - 0.375f) < 0.001f && runtime.Points["cycle_complete"] is false,
                "bottle_shuttle_fresh_machine_start_uses_completed_pose_without_teleport");
            ExecuteSelectedControllerAction("reset-shuttle"); _PhysicsProcess(1);
            check(bottle.Position.IsEqualApprox(home) && runtime.Points["motor_run"] is false
                && Equals(runtime.Points["motor_direction"], 0L) && runtime.Points["cycle_complete"] is false
                && OpticalMatch() && _virtualSnapshot!.NumericVariables["shuttle_step"] == 0,
                "bottle_shuttle_normal_reset_restores_plant_and_controller_memory");

            // Independently exercise the plant command boundary with a faulty
            // program image. Beam feedback cannot repair PLC output commands.
            runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string, bool> { ["motor_run"] = true });
            runtime.CommitVirtualControllerNumericOutputs(new System.Collections.Generic.Dictionary<string, double> { ["motor_direction"] = 2 });
            runtime.AdvanceSimulation(1);
            check(bottle.Position.IsEqualApprox(home) && Equals(runtime.Points["motor_direction"], 2L)
                && runtime.Points["motor_run"] is true && beltController.ActualSpeedMps == 0,
                "bottle_shuttle_invalid_direction_holds_travel_without_rewriting_plc_image");
            runtime.CommitVirtualControllerNumericOutputs(new System.Collections.Generic.Dictionary<string, double> { ["motor_direction"] = 1 });
            runtime.AdvanceSimulation(20);
            check(MathF.Abs(bottle.Position.X - 3) < 0.001f && runtime.Points["right_sensor_active"] is true
                && Equals(runtime.Points["motor_direction"], 1L) && runtime.Points["motor_run"] is true
                && runtime.Points["cycle_complete"] is false && Supported() && beltController.ActualSpeedMps == 0,
                "bottle_shuttle_route_limit_does_not_auto_reverse_or_claim_controller_completion");
            runtime.SetExternalPlayback(true, false); held = bottle.Position;
            runtime.AdvanceSimulation(1);
            check(bottle.Position.IsEqualApprox(held) && beltController.ActualSpeedMps == 0
                && runtime.Points["motor_run"] is true && Equals(runtime.Points["motor_direction"], 1L)
                && Equals(runtime.Points["status_color"], "amber"),
                "bottle_shuttle_external_pause_holds_pose_without_erasing_commands");
            runtime.ResetExternalPlant();
            check(runtime.Points["motor_run"] is true && Equals(runtime.Points["motor_direction"], 1L)
                && bottle.Position.IsEqualApprox(home) && beltController.ActualSpeedMps == 0,
                "bottle_shuttle_external_pause_reset_preserves_controller_commands");
            runtime.SetExternalPlayback(true, true); runtime.AdvanceSimulation(0.5);
            check(MathF.Abs(bottle.Position.X - home.X - 0.375f) < 0.001f && OpticalMatch() && Supported()
                && runtime.Points["motor_run"] is true && Equals(runtime.Points["motor_direction"], 1L),
                "bottle_shuttle_selected_external_image_drives_same_bounded_plant_without_reference_commands");
            runtime.SetExternalPlayback(false, false);
            ResetActiveController();
        }
        finally { DisableVirtualController(); }
    }
}
