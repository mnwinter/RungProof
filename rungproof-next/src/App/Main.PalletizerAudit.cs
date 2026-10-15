using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditPalletizer;
    private static readonly string[] PalletizerAxisCommands =
    [
        "gantry_x_positive", "gantry_x_negative", "gantry_y_positive",
        "gantry_y_negative", "gantry_z_positive", "gantry_z_negative",
    ];

    private void AuditPalletizer()
    {
        var failures = 0;
        void Check(bool condition, string label)
        { if (!condition) failures++; GD.Print($"PALLETIZER_CHECK {label}={condition}"); }
        try { VerifyPalletizerWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError($"PALLETIZER_EXCEPTION {error}"); }
        GD.Print($"PALLETIZER_RESULT failures={failures}; scanned PLC sequence with prescribed kinematics and sampled bounds only");
        DisableVirtualController(); GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyPalletizerWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-11-13-xy-palletizing", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var runtime = _sceneRuntime!;
        var gantry = root.GetNode<Node3D>("training_accessory_4");
        var carton = root.GetNode<Node3D>("box_2");
        var pallet = root.GetNode<Node3D>("training_accessory_6");
        var table = root.GetNode<Node3D>("training_accessory_8");
        var moving = ReviewMeshes(gantry).Where(mesh => mesh.Name.ToString().StartsWith("KIN_", StringComparison.Ordinal)
            || mesh.Name == "GANTRY_GRIPPER").ToArray();
        var posts = ReviewMeshes(gantry).Where(mesh => mesh.Name.ToString().StartsWith("GANTRY_POST", StringComparison.Ordinal)).ToArray();
        var tool = moving.Single(mesh => mesh.Name == "GANTRY_GRIPPER");
        var rod = moving.Single(mesh => mesh.Name.ToString().StartsWith("KIN_Z_AXIS", StringComparison.Ordinal));
        var bridge = moving.Single(mesh => mesh.Name.ToString().StartsWith("KIN_X_BRIDGE", StringComparison.Ordinal));
        var carriage = moving.Single(mesh => mesh.Name.ToString().StartsWith("KIN_Y_CARRIAGE", StringComparison.Ordinal));
        var home = moving.Select(part => part.Transform).ToArray();
        var palletBounds = ReviewBounds(pallet);
        var surface = ReviewBounds(table.FindChild("PICK_surface", true, false) as MeshInstance3D
            ?? throw new InvalidOperationException("Missing pickup surface."));

        bool BearsOn(Aabb box, Aabb support) => Math.Abs(box.Position.Y - support.End.Y) < .005f
            && box.Position.X >= support.Position.X - .001f && box.End.X <= support.End.X + .001f
            && box.Position.Z >= support.Position.Z - .001f && box.End.Z <= support.End.Z + .001f;
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        { var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size; return overlap.X <= .002f || overlap.Y <= .002f || overlap.Z <= .002f || !OrientedBoxesPenetrate(a, b); }
        bool CommandsOff() => PalletizerAxisCommands.All(command => runtime.Points[command] is false)
            && runtime.Points["vacuum_pick"] is false;
        long Step() => Convert.ToInt64(_virtualController!.Snapshot.NumericVariables["sequence_step"]);

        check(moving.Length == 4 && posts.Length == 4 && posts.All(post => !ReviewBounds(post).Intersects(palletBounds)),
            "pallet_inside_four_posts_without_intersection");
        check(BearsOn(ReviewBounds(carton), surface), "initial_carton_bears_on_reachable_pick_table");
        check(new[] { ("switch_10", "ENABLE"), ("switch_9", "START"), ("switch_14", "STOP"),
                ("switch_15", "RESET"), ("switch_16", "SIM TRIP"), ("switch_11", "PALLET") }
            .All(item => root.GetNode<Node3D>(item.Item1).GetNode<Label3D>("OperatorFaceLabel").Text == item.Item2),
            "learner_controls_identify_enable_start_stop_reset_trip_and_pallet_permissive");
        check(!runtime.TryGetAction("toggle-gantry_home", out _) && !runtime.TryGetAction("toggle-carton_at_pick", out _),
            "measured_home_and_carton_feedback_have_no_manual_toggle_actions");

        var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString(
            "res://programs/demos/05-integrated-cell-multi-fb-fc.rpproj.json"));
        check(loaded.IsReadable && loaded.Document is not null, "normal_saved_demo_document_is_editable");
        check(LadderEditorProjectJson.Save(loaded.Document!) == LadderEditorProjectJson.Save(
            AuthoredDemoLadderPrograms.TryCreate("lab-11-13-xy-palletizing", out var authored)
                ? authored : throw new InvalidOperationException("Missing Demo 5")),
            "saved_file_matches_demo_menu_document");
        check(!authored.Tags.Any(tag => tag.Name == "gantry_cycle")
            && authored.Instances.Count == 4
            && authored.Instances.Select(instance => instance.InstanceDataBlock!.Number).Order().SequenceEqual(new[] { 101, 102, 103, 104 }),
            "no_aggregate_cycle_and_four_distinct_axis_gripper_instance_dbs");

        RunActiveController();
        _PhysicsProcess(.1);
        check(CommandsOff() && Step() == 0 && moving.Select((part, index) => part.Transform == home[index]).All(equal => equal),
            "app_run_alone_does_not_start_machine");
        check(ExecuteSelectedControllerAction("start-palletizer"), "operator_start_accepted_before_first_controller_scan");

        var observedSteps = new HashSet<long>();
        var observedCommands = PalletizerAxisCommands.ToDictionary(command => command, _ => false, StringComparer.Ordinal);
        var observedPlaced = new HashSet<int>();
        var contacts = new HashSet<string>();
        var samples = 0;
        var geometry = true;
        var bearing = true;
        var rodSeated = true;
        var bridgeSupported = true;
        var attachment = true;
        var feedback = true;
        while (runtime.Points["layer_complete"] is not true && samples++ < 6000)
        {
            _PhysicsProcess(.02);
            var currentStep = Step();
            if (observedSteps.Add(currentStep))
                GD.Print($"PALLETIZER_SCAN_TRACE step={currentStep} scan={_virtualController!.Snapshot.ScanNumber} placed={runtime.Points["placed_cartons"]} x={runtime.Points["gantry_x_encoder_counts"]} y={runtime.Points["gantry_y_encoder_counts"]} z={runtime.Points["gantry_z_encoder_counts"]}");
            foreach (var command in PalletizerAxisCommands) observedCommands[command] |= runtime.Points[command] is true;
            observedPlaced.Add(Convert.ToInt32(runtime.Points["placed_cartons"]));
            var carriageBounds = ReviewBounds(carriage); var bridgeBounds = ReviewBounds(bridge);
            bridgeSupported &= carriageBounds.Position.Z >= bridgeBounds.Position.Z - .001f
                && carriageBounds.End.Z <= bridgeBounds.End.Z + .001f
                && carriageBounds.GetCenter().X >= bridgeBounds.Position.X
                && carriageBounds.GetCenter().X <= bridgeBounds.End.X && carriageBounds.Intersects(bridgeBounds);
            rodSeated &= ReviewBounds(rod).Intersects(carriageBounds) && ReviewBounds(rod).Intersects(ReviewBounds(tool));
            foreach (var part in moving)
            foreach (var post in posts) geometry &= Clear(part, post);
            var cartons = root.GetChildren().OfType<Node3D>()
                .Where(node => (node == carton || node.Name.ToString().StartsWith("PlacedCarton_", StringComparison.Ordinal)) && node.Visible).ToArray();
            var peers = root.GetChildren().OfType<Node3D>().Where(node => node != gantry && node != carton && node.Visible)
                .SelectMany(ReviewMeshes).ToArray();
            foreach (var part in moving)
            foreach (var peer in peers)
                if (!Clear(part, peer)) { geometry = false; var key = $"{part.Name}/{peer.GetPath()}"; if (contacts.Add(key)) GD.Print($"PALLETIZER_CONTACT sample={samples} {key}"); }
            foreach (var placed in cartons.Where(node => node != carton)) bearing &= BearsOn(ReviewBounds(placed), palletBounds);
            if (runtime.Points["carton_attached"] is true)
                attachment &= Math.Abs(ReviewBounds(carton).End.Y - ReviewBounds(tool).Position.Y) < .005f
                    && Math.Abs(ReviewBounds(carton).GetCenter().X - ReviewBounds(tool).GetCenter().X) < .001f
                    && Math.Abs(ReviewBounds(carton).GetCenter().Z - ReviewBounds(tool).GetCenter().Z) < .001f;
            feedback &= runtime.Points["palletizer_fault"] is false
                && (runtime.Points["gantry_home"] is not true || runtime.Points["cycle_in_progress"] is false);
        }
        GD.Print($"PALLETIZER_SCAN_FINAL step={Step()} placed={runtime.Points["placed_cartons"]} attached={runtime.Points["carton_attached"]} at_pick={runtime.Points["at_pickup"]} at_place={runtime.Points["at_place"]} fault={runtime.Points["palletizer_fault"]} x={runtime.Points["gantry_x_encoder_counts"]} y={runtime.Points["gantry_y_encoder_counts"]} z={runtime.Points["gantry_z_encoder_counts"]}");
        check(samples < 6000 && Convert.ToInt32(runtime.Points["placed_cartons"]) == 4,
            "one_start_causes_four_feedback_confirmed_placements");
        check(new long[] { 10, 20, 30, 40, 50, 60, 70, 80, 100 }.All(observedSteps.Contains),
            "scan_trace_visits_every_visible_plc_sequence_step");
        check(observedCommands.Values.All(value => value), "all_six_independent_axis_direction_commands_are_observed");
        check(observedPlaced.IsSupersetOf(new[] { 0, 1, 2, 3, 4 }), "placement_feedback_advances_zero_through_four");
        check(runtime.Points["gantry_home"] is true && CommandsOff() && Step() == 100
            && _virtualController!.Snapshot.Counters["cycle_count"].Accumulated == 4,
            "layer_completes_home_with_commands_off_and_four_physical_return_edges");
        check(geometry, "sampled_cartons_and_four_moving_parts_clear_fixed_solids_and_posts");
        check(bearing, "all_four_placed_cartons_bear_inside_pallet_footprint");
        check(attachment, "carried_carton_top_and_center_follow_tool_contact");
        check(rodSeated && bridgeSupported, "telescoping_rod_seated_and_carriage_supported_through_all_routes");
        check(feedback, "home_and_progress_feedback_follow_measured_plant_state");

        // Machine Reset is authored ladder/I/O, not the app lifecycle Reset.
        check(runtime.ExecuteAction("reset-palletizer"), "machine_reset_action_is_learner_visible");
        _PhysicsProcess(.04);
        check(Step() == 0 && Convert.ToInt32(runtime.Points["placed_cartons"]) == 0
            && _virtualController!.Snapshot.Counters["cycle_count"].Accumulated == 0
            && runtime.Points["gantry_home"] is true && CommandsOff(),
            "machine_reset_clears_plc_sequence_count_and_physical_layer");

        runtime.ExecuteAction("start-palletizer");
        var ticks = 0;
        while (runtime.Points["carton_attached"] is not true && ticks++ < 1000) _PhysicsProcess(.02);
        check(ticks < 1000, "fresh_cycle_reaches_attached_carton");
        runtime.ExecuteAction("stop-palletizer"); _PhysicsProcess(.02);
        var held = moving.Select(part => part.Transform).ToArray(); var heldCarton = carton.Transform;
        _PhysicsProcess(.5);
        check(CommandsOff() && moving.Select((part, index) => part.Transform == held[index]).All(equal => equal)
            && carton.Transform == heldCarton, "machine_stop_removes_commands_and_holds_pose_and_attachment");
        _PhysicsProcess(.2);
        check(moving.Select((part, index) => part.Transform == held[index]).All(equal => equal),
            "machine_stop_does_not_auto_restart");
        runtime.ExecuteAction("start-palletizer"); _PhysicsProcess(.2);
        check(!moving.Select((part, index) => part.Transform == held[index]).All(equal => equal),
            "fresh_start_resumes_retained_sequence");

        runtime.ExecuteAction("toggle-safety_trip"); _PhysicsProcess(.02);
        held = moving.Select(part => part.Transform).ToArray(); _PhysicsProcess(.3);
        check(CommandsOff() && moving.Select((part, index) => part.Transform == held[index]).All(equal => equal),
            "simulated_trip_removes_axis_commands_and_holds_pose");
        runtime.ExecuteAction("toggle-safety_trip"); _PhysicsProcess(.2);
        check(CommandsOff() && moving.Select((part, index) => part.Transform == held[index]).All(equal => equal),
            "clearing_simulated_trip_does_not_restart_without_fresh_start");
        runtime.ExecuteAction("start-palletizer"); _PhysicsProcess(.1);
        check(!CommandsOff(), "fresh_start_after_trip_clear_reenables_scanned_motion");

        runtime.ExecuteAction("toggle-pallet_position_valid"); _PhysicsProcess(.02);
        held = moving.Select(part => part.Transform).ToArray(); _PhysicsProcess(.3);
        check(CommandsOff() && moving.Select((part, index) => part.Transform == held[index]).All(equal => equal),
            "pallet_permissive_loss_removes_commands_and_holds_pose");
        runtime.ExecuteAction("toggle-pallet_position_valid"); _PhysicsProcess(.2);
        check(CommandsOff(), "restoring_pallet_permissive_does_not_restart_without_fresh_start");

        runtime.ExecuteAction("reset-palletizer"); _PhysicsProcess(.04);
        check(runtime.Points["gantry_home"] is true && runtime.Points["carton_at_pick"] is true
            && Convert.ToInt32(runtime.Points["placed_cartons"]) == 0 && Step() == 0 && CommandsOff(),
            "machine_reset_recovers_from_interrupted_sequence_to_safe_home");

        var faultPlant = new PalletizerPlantModel();
        faultPlant.Step(1, false, false, false, true, false, false, false, true, false);
        faultPlant.Step(.02, false, false, false, false, false, false, true, true, false);
        faultPlant.Step(.02, false, false, true, false, false, false, true, true, false);
        faultPlant.Step(.02, false, false, true, false, false, false, false, true, false);
        check(faultPlant.Faulted && faultPlant.Attached && faultPlant.Placed == 0,
            "vacuum_loss_during_commanded_carry_latches_fault_without_false_placement");
        var opposed = new PalletizerPlantModel();
        opposed.Step(.02, true, true, false, false, false, false, false, true, false);
        check(opposed.Faulted, "plant_rejects_opposed_axis_commands_without_selecting_a_sequence_state");
        var invalid = true;
        foreach (var time in new[] { -1.0, double.NaN, double.PositiveInfinity })
            try { opposed.Step(time, false, false, false, false, false, false, false, true, false); invalid = false; }
            catch (ArgumentException) { }
        check(invalid, "invalid_elapsed_time_rejected_even_when_faulted");
        if (_visualSceneReview) VerifyGantryReviewClock(check);
        GD.Print($"PALLETIZER_CYCLE_SAMPLES {samples}");
        GD.Print($"PALLETIZER_STATIC_CANDIDATES {LogBoundsCandidates(root)}");
    }
}
