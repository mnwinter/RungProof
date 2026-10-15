using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditChainLiftInstallation;
    private void AuditChainLiftInstallation()
    {
        var failures = 0;
        void Check(bool condition, string label)
        { if (!condition) failures++; GD.Print($"CHAIN_LIFT_INSTALLATION {label}={condition}"); }
        try
        {
            AddMigratedScene("lab-4-09-chain-drive-lift", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
            var lift = root.GetNode<Node3D>("liftTable_1");
            var motion = lift.GetNode<ChainLiftDriveVisual>("ChainLiftMotion");
            var feed = root.GetNode<Node3D>("conveyor_0").FindChildren("*", "", true, false).OfType<ChainFeedMotion>().Single();
            var box = root.GetNode<Node3D>("box_2");
            var carriage = lift.GetNode<Node3D>("KIN_chain_lift_carriage");
            var deck = carriage.GetNode<MeshInstance3D>("LIFT_carrying_deck");
            var carryingBelt = carriage.GetNode<MeshInstance3D>("KIN_belt_surface");
            Check(ReviewBounds(carryingBelt).End.Y - ReviewBounds(deck).End.Y > .0025f
                && MathF.Abs(ReviewBounds(carryingBelt).Position.Y - ReviewBounds(deck).End.Y) < .0001f,
                "carrying_belt_bears_on_recessed_deck_without_coplanar_top_faces");
            foreach (var equipment in root.GetChildren().OfType<Node3D>().Where(node => ReviewMeshes(node).Length > 0))
                GD.Print($"CHAIN_LIFT_BOUNDS {equipment.Name} {ReviewBounds(equipment)}");
            // Chain slats have gaps. Bearing contacts must surround the carton
            // COM; this is a bounds screen, not a load/stability calculation.
            var carriers = ReviewMeshes(root).Where(mesh => mesh.Name.ToString().StartsWith("KIN_CHAIN_SLAT_", StringComparison.Ordinal)
                || mesh.Name.ToString().StartsWith("CHAIN_end_wear_plate_", StringComparison.Ordinal)
                || mesh.Name == "TRANSFER_bridge" || mesh.Name.ToString().StartsWith("LIFT_deck_margin_", StringComparison.Ordinal)
                || mesh.Name.ToString().Contains("belt_surface", StringComparison.OrdinalIgnoreCase)).ToArray();
            bool Supported()
            {
                var load = ReviewBounds(box); var center = load.GetCenter();
                var contacts = carriers.Select(ReviewBounds).Where(surface => MathF.Abs(surface.End.Y - load.Position.Y) < .002f
                    && surface.Position.X < load.End.X && surface.End.X > load.Position.X
                    && surface.Position.Z < load.End.Z && surface.End.Z > load.Position.Z).ToArray();
                return contacts.Length > 0 && contacts.Min(contact => contact.Position.X) <= center.X
                    && contacts.Max(contact => contact.End.X) >= center.X
                    && contacts.Min(contact => contact.Position.Z) <= center.Z && contacts.Max(contact => contact.End.Z) >= center.Z;
            }
            Check(Supported() && Math.Abs(box.Position.X - ChainLiftPlantModel.InitialX) < .001, "initial_carton_bears_on_chain_infeed");
            var supports = ReviewMeshes(carriage).Where(mesh => mesh.Name.ToString().StartsWith("LIFT_deck_support_", StringComparison.Ordinal)).ToArray();
            Check(supports.Length == 2 && supports.All(part => ReviewBounds(part).Intersects(ReviewBounds(deck))), "deck_bears_on_two_carriage_crossmembers");
            var limits = root.GetNode<Node3D>("training_accessory_6");
            var mounts = ReviewMeshes(limits).Where(mesh => mesh.Name.ToString().StartsWith("LIMIT_mount_", StringComparison.Ordinal)).ToArray();
            var guides = ReviewMeshes(lift).Where(mesh => mesh.Name.ToString().StartsWith("LIFT_guide_", StringComparison.Ordinal)).ToArray();
            Check(mounts.Length == 2 && mounts.All(mount => guides.Any(guide => ReviewBounds(mount).Intersects(ReviewBounds(guide)))), "both_limit_brackets_bear_on_fixed_guide_post");
            var rollers = ReviewMeshes(limits).Where(mesh => mesh.Name.ToString().StartsWith("LIMIT_roller_", StringComparison.Ordinal)).OrderBy(mesh => mesh.GlobalPosition.Y).ToArray();
            var shoe = ReviewMeshes(carriage).Single(mesh => mesh.Name.ToString().StartsWith("LIFT_guide_shoe_-", StringComparison.Ordinal) && mesh.GlobalPosition.Z > 0);
            var limitContact = true;
            for (var endpoint = 0; endpoint <= 1; endpoint++)
            { motion.SetPositionNormalized(endpoint); motion.ProjectDrive(); limitContact &= ReviewBounds(rollers[endpoint]).Intersects(ReviewBounds(shoe)); }
            Check(limitContact, "moving_guide_shoe_meets_both_endpoint_rollers");
            runtime.ResetSimulation();
            Check(ReviewMeshes(root).All(mesh => ReviewBounds(mesh).Position.Y >= -.002f), "equipment_clears_finished_floor");
            Check(runtime.Points["lift_home"] is true && runtime.Points["box_present"] is true && runtime.Points["destination_clear"] is true,
                "initial_feedback_comes_from_home_and_infeed_positions");
            var driveHomes = ReviewMeshes(root).Where(mesh => mesh.Name.ToString().StartsWith("SPROCKET", StringComparison.Ordinal)
                || mesh.Name.ToString().StartsWith("KIN_CHAIN_SLAT_", StringComparison.Ordinal)
                || mesh.Name.ToString().Contains("drum", StringComparison.OrdinalIgnoreCase)).ToDictionary(mesh => mesh, mesh => mesh.Transform);
            Check(!motion.IsPhysicsProcessing() && !feed.IsPhysicsProcessing() && !motion.AutonomousPositionTravel
                && lift.FindChildren("*", "", true, false).OfType<ConveyorController>().All(adapter => !adapter.IsPhysicsProcessing()),
                "plant_clock_disables_independent_carriage_chain_and_carrying_belt_callbacks");
            var feedBridge = root.GetNode<Node3D>("training_accessory_4").GetNode<MeshInstance3D>("TRANSFER_bridge");
            var upperBridge = root.GetNode<Node3D>("training_accessory_5").GetNode<MeshInstance3D>("TRANSFER_bridge");
            Check(MathF.Abs(ReviewBounds(feedBridge).End.Y - ReviewBounds(carryingBelt).End.Y) < .001f
                && MathF.Abs(ReviewBounds(feedBridge).End.X - ReviewBounds(deck).Position.X) < .001f, "lower_bridge_matches_platform_edge_and_height");
            var obstacles = root.GetChildren().OfType<Node3D>().Where(node => node != lift && node != box && node.Name != "training_accessory_6")
                .SelectMany(ReviewMeshes).Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
            var fixedParts = ReviewMeshes(lift).Where(part => !carriage.IsAncestorOf(part)
                && !part.Name.ToString().StartsWith("LIFT_guide_", StringComparison.Ordinal)
                && !part.Name.ToString().StartsWith("LIFT_chain_link_", StringComparison.Ordinal)).ToArray();
            // Rotating cylinders/sprockets must use transformed surface
            // vertices: transforming the corners of their local bounding
            // cubes falsely expands a circular drum into the carton.
            var surfaceVertices = ReviewMeshes(root).ToDictionary(mesh => mesh, mesh => mesh.Mesh.GetFaces());
            var surfaceBoundsCache = new Dictionary<MeshInstance3D, (Transform3D Pose, Aabb Bounds)>();
            Aabb SurfaceBounds(MeshInstance3D mesh)
            {
                var pose = mesh.GlobalTransform;
                if (surfaceBoundsCache.TryGetValue(mesh, out var cached) && cached.Pose == pose) return cached.Bounds;
                var vertices = surfaceVertices[mesh];
                var bounds = new Aabb(mesh.GlobalTransform * vertices[0], Vector3.Zero);
                foreach (var vertex in vertices) bounds = bounds.Expand(mesh.GlobalTransform * vertex);
                surfaceBoundsCache[mesh] = (pose, bounds);
                return bounds;
            }
            bool Penetrates(MeshInstance3D part, MeshInstance3D other)
            {
                var coarse = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
                if (coarse.X <= .002f || coarse.Y <= .002f || coarse.Z <= .002f) return false;
                var overlap = SurfaceBounds(part).Intersection(SurfaceBounds(other)).Size;
                return overlap.X > .002f && overlap.Y > .002f && overlap.Z > .002f && OrientedBoxesPenetrate(part, other);
            }
            var clear = true;
            for (var sample = 0; sample <= 210; sample++)
            {
                motion.SetPositionNormalized(sample / 210f); motion.ProjectDrive();
                foreach (var part in ReviewMeshes(carriage))
                foreach (var other in obstacles.Concat(fixedParts))
                    if (Penetrates(part, other)) { clear = false; GD.Print($"CHAIN_LIFT_CONTACT {sample} {part.Name}/{other.Name}"); }
            }
            Check(clear, "carriage_clears_selected_non_guide_obstacles_at_211_heights");
            Check(MathF.Abs(ReviewBounds(upperBridge).End.Y - ReviewBounds(carryingBelt).End.Y) < .001f
                && MathF.Abs(ReviewBounds(upperBridge).Position.X - ReviewBounds(deck).End.X) < .001f, "upper_bridge_matches_raised_platform_edge_and_height");
            var receiving = ReviewMeshes(root.GetNode<Node3D>("receiving_conveyor")).Single(mesh => mesh.Name.ToString().Contains("belt_surface", StringComparison.OrdinalIgnoreCase));
            Check(MathF.Abs(ReviewBounds(receiving).End.Y - ReviewBounds(carryingBelt).End.Y) < .002f
                && MathF.Abs(ReviewBounds(receiving).Position.X - ReviewBounds(upperBridge).End.X) < .002f, "upper_belt_joins_receiving_bridge");
            var document = CreateChainLiftInstallationReference();
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/chain-lift-installation-reference.rpproj.json"), LadderEditorProjectJson.Save(document));
            var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/chain-lift-installation-reference.rpproj.json"));
            Check(loaded.IsReadable && loaded.Document is not null && LadderEditorProjectJson.Save(loaded.Document) == LadderEditorProjectJson.Save(document), "saved_reference_matches_reviewed_cycle_program");
            EnableVirtualControllerProgram(document.BuildProgram());
            RunActiveController(); _PhysicsProcess(.04);
            Check(!motion.RunCommand && runtime.Points["chain_run"] is false && box.Position.X < -2.69f, "run_alone_cannot_start_loading");
            runtime.ExecuteAction("start-lift-installation");
            var supported = true; var cartonClear = true; var feedback = true; var visible = true;
            var beamFeedback = true; var sawReceiverBeam = false;
            var entryBeams = new[] { "infeed_photoeye", "receiver_photoeye" }.Select(id =>
                root.GetNode<Node3D>(id).FindChildren("KIN_beam*", "", true, false).OfType<MeshInstance3D>().ToArray()).ToArray();
            var sawLoad = false; var sawRise = false; var sawDischarge = false; var sawReturn = false;
            // Imported child order is not semantic: the first mesh can be a
            // barcode stroke. Select the solid carton body by volume, then
            // prove its bounds cover the modeled footprint before using it.
            var ticks = 0; var body = ReviewMeshes(box).OrderByDescending(mesh =>
                { var size = ReviewBounds(mesh).Size; return size.X * size.Y * size.Z; }).First();
            var bodySize = ReviewBounds(body).Size;
            GD.Print($"CHAIN_LIFT_CARTON_BODY {body.Name} {ReviewBounds(body)}");
            Check(MathF.Abs(bodySize.X - (float)ChainLiftPlantModel.HalfLoad * 2) < .001f
                && bodySize.Z > .7f && bodySize.Z < ReviewBounds(carryingBelt).Size.Z
                && MathF.Abs(bodySize.Y - .72f) < .001f,
                "solid_carton_body_matches_modeled_footprint_before_collision_and_optical_checks");
            var loadObstacles = ReviewMeshes(root).Where(mesh => !box.IsAncestorOf(mesh) && !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
            var reportedLoadContacts = new HashSet<MeshInstance3D>();
            while (ticks++ < 3500 && runtime.Points["cycle_complete"] is not true)
            {
                _PhysicsProcess(.01); supported &= Supported(); visible &= box.Visible && box.IsInsideTree();
                foreach (var other in loadObstacles) if (Penetrates(body, other)) { cartonClear = false; if (reportedLoadContacts.Add(other)) GD.Print($"CHAIN_LIFT_LOAD_CONTACT tick={ticks} {other.GetPath()} body={SurfaceBounds(body)} other={SurfaceBounds(other)}"); }
                feedback &= (bool)runtime.Points["lift_home"]! == (motion.PositionPercent < .001f)
                    && (bool)runtime.Points["lift_upper"]! == (motion.PositionPercent > 99.999f);
                var bodyBounds = ReviewBounds(body);
                foreach (var (beams, index) in entryBeams.Select((beams, index) => (beams, index)))
                {
                    // The rendered beam is dashed; its first segment is near
                    // a sensor head, outside the centered carton. Merge all
                    // segments to recover the actual optical centerline.
                    var opticalBounds = beams.Select(ReviewBounds).Aggregate((left, right) => left.Merge(right));
                    var expected = bodyBounds.HasPoint(opticalBounds.GetCenter());
                    var actual = (bool)runtime.Points[index == 0 ? "box_present" : "receiver_beam_blocked"]!;
                    if (expected != actual && ticks % 100 == 0) GD.Print($"CHAIN_LIFT_BEAM_MISMATCH tick={ticks} sensor={index} expected={expected} actual={actual} body={bodyBounds} beam={opticalBounds}");
                    beamFeedback &= expected == actual && beams.All(beam => beam.Visible == !actual);
                }
                sawReceiverBeam |= runtime.Points["receiver_beam_blocked"] is true;
                sawLoad |= motion.PositionPercent < .001f && box.Position.X > -2 && box.Position.X < -1;
                sawRise |= motion.PositionPercent > 5 && motion.PositionPercent < 95 && runtime.Points["lift_enable"] is true;
                sawDischarge |= motion.PositionPercent > 99.999f && box.Position.X > 1.25f;
                sawReturn |= motion.PositionPercent > 5 && motion.PositionPercent < 95 && runtime.Points["lift_lower"] is true;
            }
            Check(runtime.Points["cycle_complete"] is true && ticks < 3500 && runtime.Points["transfer_fault"] is false, "actual_twenty_ms_controller_completes_load_raise_discharge_return_cycle");
            Check(sawLoad && sawRise && sawDischarge && sawReturn, "all_four_physical_route_legs_observed");
            Check(supported, "carton_com_inside_bearing_contact_envelope_through_entire_sampled_cycle");
            Check(cartonClear && visible, "carton_stays_visible_and_clears_equipment_through_entire_sampled_cycle");
            Check(feedback, "home_and_upper_feedback_match_carriage_position_every_sample");
            Check(beamFeedback && sawReceiverBeam && runtime.Points["receiver_beam_blocked"] is false
                && runtime.Points["receiver_occupied"] is true,
                "both_actual_entry_beams_follow_carton_body_and_clear_after_it_passes");
            Check(runtime.Points["carton_at_receiver"] is true && runtime.Points["receiver_occupied"] is true && runtime.Points["destination_clear"] is false
                && runtime.Points["lift_home"] is true && runtime.Points["carton_on_lift"] is false && runtime.Points["cycle_active"] is false,
                "carton_stays_on_receiver_after_empty_carriage_returns_home");
            Check(new[] { "chain_run", "lift_enable", "lift_lower" }.All(point => runtime.Points[point] is false), "completion_clears_all_drive_commands");
            var completePose = box.Transform; _PhysicsProcess(1); runtime.ExecuteAction("start-lift-installation"); _PhysicsProcess(.04);
            Check(box.Transform.IsEqualApprox(completePose) && runtime.Points["cycle_active"] is false, "occupied_completed_receiver_cannot_restart_or_recycle_carton");
            var stopResume = true;
            foreach (var leg in new[] { "load", "up", "discharge", "down" })
            {
                ResetActiveController(); RunActiveController(); _PhysicsProcess(.04); runtime.ExecuteAction("start-lift-installation");
                bool InLeg() => leg switch {
                    "load" => box.Position.X > -2.4f && box.Position.X < -1,
                    "up" => motion.PositionPercent > 20 && runtime.Points["lift_enable"] is true,
                    "discharge" => box.Position.X > 1.8f && runtime.Points["chain_run"] is true,
                    _ => motion.PositionPercent < 80 && runtime.Points["lift_lower"] is true };
                for (var tick = 0; tick < 3400 && !InLeg(); tick++) _PhysicsProcess(.01);
                stopResume &= InLeg(); StopActiveController(); var heldBox = box.Transform; var heldDeck = carriage.Transform;
                _PhysicsProcess(1); stopResume &= box.Transform.IsEqualApprox(heldBox) && carriage.Transform.IsEqualApprox(heldDeck);
                RunActiveController(); _PhysicsProcess(.1); stopResume &= box.Transform.IsEqualApprox(heldBox) && carriage.Transform.IsEqualApprox(heldDeck);
                runtime.ExecuteAction("start-lift-installation"); _PhysicsProcess(.2); stopResume &= box.Transform != heldBox || carriage.Transform != heldDeck;
            }
            Check(stopResume, "stop_holds_and_run_requires_fresh_start_in_every_route_leg");
            ResetActiveController();
            Check(Supported() && runtime.Points["lift_home"] is true && runtime.Points["box_present"] is true && runtime.Points["destination_clear"] is true
                && box.Position.X < -2.69f && new[] { "chain_run", "lift_enable", "lift_lower", "cycle_complete", "transfer_fault" }.All(point => runtime.Points[point] is false),
                "reset_restores_infeed_carton_home_feedback_and_clears_status");
            Check(driveHomes.Count > 0 && driveHomes.All(pair => pair.Key.Transform.IsEqualApprox(pair.Value)), "reset_restores_chain_and_both_carrying_belt_drums_without_another_command");
            DisableVirtualController(); runtime.UsesExternalClock = true; runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["lift_enable"] = true, ["lift_lower"] = true }); runtime.AdvanceSimulation(.2);
            Check(runtime.Points["transfer_fault"] is true && runtime.Points["lift_enable"] is true && runtime.Points["lift_lower"] is true
                && motion.PositionPercent == 0, "opposing_commands_report_fault_without_rewriting_plc_image");
            runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["chain_run"] = true, ["lift_enable"] = true }); runtime.AdvanceSimulation(.2);
            Check(runtime.Points["transfer_fault"] is true, "simultaneous_horizontal_vertical_commands_report_fault");
            runtime.SetExternalPlayback(true, false); var paused = box.Transform; runtime.AdvanceSimulation(5);
            Check(box.Transform == paused && runtime.Points["chain_run"] is true, "paused_external_playback_holds_pose_and_preserves_command_image");
            runtime.SetExternalPlayback(false, false); runtime.ResetSimulation();
            var a = new ChainLiftPlantModel(); var b = new ChainLiftPlantModel(); a.Step(7, true, false, false);
            for (var tick = 0; tick < 700; tick++) b.Step(.01, true, false, false);
            Check(Math.Abs(a.LoadX - b.LoadX) < 1e-8 && a.TransferFault == b.TransferFault, "large_steps_cannot_skip_transfer_edges");
            var rejectedTime = true;
            foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity })
            { try { a.Step(value, false, false, false); rejectedTime = false; } catch (ArgumentException) { } }
            Check(rejectedTime, "invalid_elapsed_time_rejected");
            EnableVirtualControllerProgram(document.BuildProgram()); RunActiveController(); _PhysicsProcess(.04);
            var wasReview = _visualSceneReview; _visualSceneReview = true;
            var rootMode = root.ProcessMode; var runtimeMode = runtime.ProcessMode;
            SetGantryReviewClockHeld(true); var heldScan = _virtualController!.Snapshot.ScanNumber; var heldLoad = box.Transform;
            _PhysicsProcess(3);
            Check(GantryReviewClockHeld && _virtualController.Snapshot.ScanNumber == heldScan && box.Transform == heldLoad,
                "native_review_hold_freezes_actual_controller_and_single_plant_clock");
            runtime.ExecuteAction("start-lift-installation"); StepGantryReviewClock();
            Check(_virtualController.Snapshot.ScanNumber == heldScan + 100 && Math.Abs(box.Position.X + 2.1f) < .01f,
                "native_review_step_executes_two_seconds_of_actual_twenty_ms_scans");
            StopActiveController(); var heldAfterStop = box.Transform; heldScan = _virtualController.Snapshot.ScanNumber;
            StepGantryReviewClock();
            Check(box.Transform == heldAfterStop && _virtualController.Snapshot.ScanNumber == heldScan, "native_review_step_cannot_advance_stopped_controller");
            ReleaseGantryReviewClock(); _visualSceneReview = wasReview;
            Check(root.ProcessMode == rootMode && runtime.ProcessMode == runtimeMode && !GantryReviewClockHeld,
                "native_review_release_restores_original_processing_modes");
            ResetActiveController();
            GD.Print($"CHAIN_LIFT_STATIC_CANDIDATES {LogBoundsCandidates(root)}"); GD.Print($"CHAIN_LIFT_CYCLE_SAMPLES {ticks}");
        }
        catch (Exception error) { failures++; GD.PushError($"CHAIN_LIFT_INSTALLATION_EXCEPTION {error}"); }
        GD.Print($"CHAIN_LIFT_INSTALLATION_RESULT failures={failures}; sampled offline cycle/geometry checks only");
        DisableVirtualController(); GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private static LadderEditorDocument CreateChainLiftInstallationReference()
    {
        var d = new LadderEditorDocument();
        d.ResetProject("reference-chain-lift-cycle", "Chain_Lift_Carton_Cycle", TimeSpan.FromMilliseconds(20)); d.SourceSceneId = "lab-4-09-chain-drive-lift";
        foreach (var input in new[] { "carton_on_infeed", "lift_home", "destination_clear", "carton_on_lift", "lift_upper", "carton_at_receiver", "transfer_fault", "lift_start" })
            d.AddTag(input, PlcVariableRole.Input, input);
        d.AddTag("stop_command", PlcVariableRole.Input, "operator.stop");
        foreach (var memory in new[] { "start_edge", "start_accepted" }) d.AddTag(memory, PlcVariableRole.Memory);
        foreach (var output in new[] { "chain_run", "lift_enable", "lift_lower", "cycle_active", "cycle_complete" }) d.AddTag(output, PlcVariableRole.Output, output);
        d.AddRung("Sample fresh Start edge", "start_edge"); d.InsertEdgeContact(0, 0, 0, "lift_start", LadderEdgeMode.Rising);
        d.AddRung("Accept initial or held-route Start", "start_accepted");
        for (var branch = 0; branch < 4; branch++)
        {
            if (branch > 0) d.AddParallelBranch(1);
            d.AddContact(1, branch, "start_edge", false); d.AddContact(1, branch, "cycle_active", true);
            d.AddContact(1, branch, "stop_command", true); d.AddContact(1, branch, "transfer_fault", true);
            if (branch == 0) { d.AddContact(1, branch, "carton_on_infeed", false); d.AddContact(1, branch, "lift_home", false); d.AddContact(1, branch, "destination_clear", false); }
            else if (branch == 1) { d.AddContact(1, branch, "carton_on_lift", false); d.AddContact(1, branch, "destination_clear", false); }
            else if (branch == 2) { d.AddContact(1, branch, "lift_upper", false); d.AddContact(1, branch, "carton_at_receiver", true); }
            else { d.AddContact(1, branch, "carton_at_receiver", false); d.AddContact(1, branch, "lift_home", true); }
        }
        d.AddRung("Complete after discharge and empty-carriage return", "cycle_complete");
        d.AddContact(2, 0, "cycle_active", false); d.AddContact(2, 0, "carton_at_receiver", false); d.AddContact(2, 0, "lift_home", false);
        d.AddParallelBranch(2); d.AddContact(2, 1, "cycle_complete", false);
        foreach (var branch in new[] { 0, 1 }) { d.AddContact(2, branch, "start_accepted", true); d.AddContact(2, branch, "stop_command", true); d.AddContact(2, branch, "transfer_fault", true); }
        d.AddRung("Authorization seal cleared by Stop fault completion", "cycle_active");
        d.AddContact(3, 0, "start_accepted", false); d.AddParallelBranch(3); d.AddContact(3, 1, "cycle_active", false);
        foreach (var branch in new[] { 0, 1 }) { d.AddContact(3, branch, "cycle_complete", true); d.AddContact(3, branch, "stop_command", true); d.AddContact(3, branch, "transfer_fault", true); }
        d.AddRung("Horizontal loading at HOME or discharge at UPPER", "chain_run");
        d.AddContact(4, 0, "cycle_active", false); d.AddContact(4, 0, "lift_home", false); d.AddContact(4, 0, "carton_on_infeed", false);
        d.AddParallelBranch(4); d.AddContact(4, 1, "cycle_active", false); d.AddContact(4, 1, "lift_upper", false); d.AddContact(4, 1, "carton_at_receiver", true);
        d.AddRung("Raise carried carton stopping at UPPER", "lift_enable");
        d.AddContact(5, 0, "cycle_active", false); d.AddContact(5, 0, "carton_on_lift", false); d.AddContact(5, 0, "lift_upper", true);
        d.AddRung("Return empty carriage after carton reaches stop", "lift_lower");
        d.AddContact(6, 0, "cycle_active", false); d.AddContact(6, 0, "carton_at_receiver", false); d.AddContact(6, 0, "lift_home", true);
        return d;
    }
}
