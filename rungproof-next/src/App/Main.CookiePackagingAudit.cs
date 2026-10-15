using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditCookiePackaging;
    private void AuditCookiePackaging()
    {
        var failures = 0;
        void Check(bool condition, string label)
        { if (!condition) failures++; GD.Print($"COOKIE_AUDIT {label}={condition}"); }
        try
        {
            AddMigratedScene("lab-4-10-cookie-packaging", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
            var trays = Enumerable.Range(0, 6).Select(i => root.GetNode<Node3D>($"cookie_{i}")).ToArray();
            var films = trays.Select(tray => tray.GetNode<Node3D>("COOKIE_package")).ToArray();
            var head = root.GetNode<Node3D>("machine_1").GetNode<Node3D>("KIN_cookie_seal_head");
            var conveyor = root.GetNode<Node3D>("conveyor_0");
            var belt = ReviewMeshes(conveyor).Single(mesh => mesh.Name == "KIN_belt_surface");
            var drive = conveyor.FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
            var beams = ReviewMeshes(root.GetNode<Node3D>("photoeye_2")).Where(mesh => mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
            Check(beams.Length > 0 && Math.Abs(ReviewBounds(beams[0]).GetCenter().Y - .935) < .001, "actual_count_beam_intersects_biscuit_height");
            Check(!drive.IsPhysicsProcessing(), "single_plant_clock_disables_independent_belt_callback");
            Check(runtime.Points["packaging_ready"] is true && runtime.Points["cookie_count"] is 0L && films.All(film => !film.Visible), "initial_home_head_zero_counts_unwrapped_trays");
            Check(ReviewMeshes(root).All(mesh => ReviewBounds(mesh).Position.Y >= -.002f), "equipment_above_finished_floor");
            var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/cookie-packaging-reference.rpproj.json"));
            Check(loaded.IsReadable && loaded.Document is not null, "saved_editable_reference_loads");
            var program = loaded.Document!.BuildProgram(); EnableVirtualControllerProgram(program);
            RunActiveController(); _PhysicsProcess(.1);
            Check(trays[0].Position.X < -3.44f && runtime.Points["infeed_run"] is false, "run_without_fresh_start_holds_batch");
            runtime.ExecuteAction("start-cookie-batch");
            var supported = true; var clear = true; var optical = true; var counts = true; var visible = true;
            var oldCount = 0; var oldSealed = 0; var ticks = 0;
            var obstacles = root.GetChildren().OfType<Node3D>().Where(node => !trays.Contains(node)).SelectMany(ReviewMeshes)
                .Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
            var reported = new HashSet<string>();
            var trayParts = trays.ToDictionary(tray => tray, tray => ReviewMeshes(tray));
            var opticalCenter = beams.Select(ReviewBounds).Aggregate((left, right) => left.Merge(right)).GetCenter();
            var boundCache = new Dictionary<MeshInstance3D, (Transform3D Pose, Aabb Bounds)>();
            Aabb Bounds(MeshInstance3D mesh)
            {
                var pose = mesh.GlobalTransform;
                if (boundCache.TryGetValue(mesh, out var cached) && cached.Pose == pose) return cached.Bounds;
                var bounds = ReviewBounds(mesh); boundCache[mesh] = (pose, bounds); return bounds;
            }
            while (ticks++ < 1800 && runtime.Points["cycle_complete"] is not true)
            {
                _PhysicsProcess(.02);
                var beltBounds = Bounds(belt);
                var obstacleBounds = obstacles.Select(Bounds).ToArray();
                foreach (var tray in trays)
                {
                    var baseBounds = Bounds(tray.GetNode<MeshInstance3D>("COOKIE_tray"));
                    supported &= Math.Abs(baseBounds.Position.Y - beltBounds.End.Y) < .001
                        && baseBounds.Position.X >= beltBounds.Position.X && baseBounds.End.X <= beltBounds.End.X
                        && baseBounds.Position.Z >= beltBounds.Position.Z && baseBounds.End.Z <= beltBounds.End.Z;
                    visible &= tray.IsInsideTree() && tray.Visible;
                    foreach (var part in trayParts[tray].Where(mesh => mesh.IsVisibleInTree()))
                    for (var obstacle = 0; obstacle < obstacles.Length; obstacle++)
                    {
                        var other = obstacles[obstacle];
                        var intersection = Bounds(part).Intersection(obstacleBounds[obstacle]).Size;
                        if (intersection.X > .002f && intersection.Y > .002f && intersection.Z > .002f)
                        {
                            clear = false;
                            var key = part.Name + "/" + other.GetPath();
                            if (reported.Add(key)) GD.Print($"COOKIE_CONTACT tick={ticks} {key} load={ReviewBounds(part)} other={ReviewBounds(other)}");
                        }
                    }
                }
                var expectedBeam = trays.Any(tray => { var bounds = ReviewBounds(tray.GetNode<MeshInstance3D>("COOKIE_biscuit")); return bounds.HasPoint(opticalCenter); });
                // Tray is wider than the biscuit. Model the optical silhouette
                // at biscuit height, not the supporting tray's lower plane.
                optical &= expectedBeam == (runtime.Points["count_beam_blocked"] is true)
                    && beams.All(beam => beam.Visible == !expectedBeam);
                var count = Convert.ToInt32(runtime.Points["cookie_count"]); var sealedCount = Convert.ToInt32(runtime.Points["wrapped_count"]);
                counts &= count >= oldCount && count <= oldCount + 1 && sealedCount >= oldSealed && sealedCount <= oldSealed + 1
                    && films.Count(film => film.Visible) == sealedCount;
                oldCount = count; oldSealed = sealedCount;
            }
            Check(ticks < 1800 && runtime.Points["cycle_complete"] is true && runtime.Points["packaging_fault"] is false, "actual_twenty_ms_controller_completes_six_cookie_batch");
            Check(supported && visible, "every_tray_stays_supported_visible_through_sampled_cycle");
            Check(clear, "visible_cookie_tray_and_package_bounds_clear_station_and_equipment");
            Check(optical, "rendered_beam_and_feedback_match_actual_biscuit_crossings");
            Check(counts && oldCount == 6 && oldSealed == 6, "exactly_six_crossings_six_seals_no_duplicate_counts");
            Check(head.Position.IsEqualApprox(Vector3.Zero) && runtime.Points["infeed_run"] is false && runtime.Points["packaging_enable"] is false
                && trays[5].Position.X > 3.44f && trays[0].Position.X > .44f, "completed_packages_retained_with_head_home_commands_off");
            Check(root.GetNode<Node3D>("cookie_counter").GetNode<Label3D>("NumericReadout").Text == "COOKIES\n6"
                && root.GetNode<Node3D>("package_counter").GetNode<Label3D>("NumericReadout").Text == "SEALED\n6", "readouts_show_physical_numeric_feedback");
            var complete = trays.Select(tray => tray.Transform).ToArray();
            runtime.ExecuteAction("start-cookie-batch"); _PhysicsProcess(1);
            Check(trays.Select((tray, i) => tray.Transform == complete[i]).All(value => value) && oldSealed == Convert.ToInt32(runtime.Points["wrapped_count"]), "completed_batch_cannot_restart_recycle_or_disappear");
            var stopResume = true;
            foreach (var phase in new[] { "feed", "seal" })
            {
                ResetActiveController(); RunActiveController(); _PhysicsProcess(.04); runtime.ExecuteAction("start-cookie-batch");
                bool AtPhase() => phase == "feed" ? trays[5].Position.X > -.35f && trays[5].Position.X < -.1f : head.Position.Y < -.10f && head.Position.Y > -.20f;
                for (var tick = 0; tick < 200 && !AtPhase(); tick++) _PhysicsProcess(.02);
                stopResume &= AtPhase(); StopActiveController(); var poses = trays.Select(tray => tray.Transform).ToArray(); var heldHead = head.Transform;
                _PhysicsProcess(1); RunActiveController(); _PhysicsProcess(.2);
                stopResume &= head.Transform == heldHead && trays.Select((tray, i) => tray.Transform == poses[i]).All(value => value);
                runtime.ExecuteAction("start-cookie-batch"); _PhysicsProcess(.2);
                stopResume &= head.Transform != heldHead || trays[5].Transform != poses[5];
            }
            Check(stopResume, "stop_holds_feed_and_partial_head_run_requires_fresh_start");
            ResetActiveController();
            Check(trays[0].Position.X < -3.44f && films.All(film => !film.Visible) && head.Position == Vector3.Zero
                && runtime.Points["cookie_count"] is 0L && runtime.Points["wrapped_count"] is 0L, "reset_restores_all_trays_head_and_counts");
            DisableVirtualController(); runtime.UsesExternalClock = true; runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["infeed_run"] = true, ["packaging_enable"] = true }); runtime.AdvanceSimulation(.1);
            Check(runtime.Points["packaging_fault"] is true && runtime.Points["infeed_run"] is true && runtime.Points["packaging_enable"] is true
                && trays[0].Position.X < -3.44f, "conflict_fault_holds_without_rewriting_plc_command_image");
            runtime.SetExternalPlayback(true, false); var paused = trays[0].Transform; runtime.AdvanceSimulation(2);
            Check(trays[0].Transform == paused && runtime.Points["infeed_run"] is true, "external_pause_preserves_commands_and_pose");
            runtime.SetExternalPlayback(false, false); runtime.ResetSimulation();
            var model = new CookiePackagingPlantModel(); model.Step(10, false, true);
            Check(model.CookieCount == 0 && model.WrappedCount == 0 && !model.Busy, "empty_station_cannot_manufacture_counts_or_packages");
            var invalid = true;
            foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity })
            { try { model.Step(value, false, false); invalid = false; } catch (ArgumentException) { } }
            Check(invalid, "invalid_elapsed_time_rejected");
            EnableVirtualControllerProgram(program); RunActiveController(); _PhysicsProcess(.04);
            var wasReview = _visualSceneReview; _visualSceneReview = true; SetGantryReviewClockHeld(true);
            var heldScan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(3);
            Check(_virtualController.Snapshot.ScanNumber == heldScan && GantryReviewClockHeld, "native_review_hold_freezes_controller_clock");
            runtime.ExecuteAction("start-cookie-batch"); StepGantryReviewClock();
            Check(_virtualController.Snapshot.ScanNumber == heldScan + 100 && Math.Abs(trays[5].Position.X - .15f) < .001, "native_review_step_runs_two_seconds_actual_scans");
            StopActiveController(); heldScan = _virtualController.Snapshot.ScanNumber; StepGantryReviewClock();
            Check(_virtualController.Snapshot.ScanNumber == heldScan, "native_review_step_cannot_advance_stopped_controller");
            ReleaseGantryReviewClock(); _visualSceneReview = wasReview; ResetActiveController();
            var cablesClear = true;
            foreach (var cable in ReviewMeshes(conveyor).Where(mesh => mesh.Name.ToString().StartsWith("CTRL_", StringComparison.Ordinal) && mesh.Name.ToString().EndsWith("_cable", StringComparison.Ordinal)))
            foreach (var stationPart in ReviewMeshes(root.GetNode<Node3D>("machine_1")))
            {
                var overlap = ReviewBounds(cable).Intersection(ReviewBounds(stationPart)).Size;
                if (overlap.X <= .002f || overlap.Y <= .002f || overlap.Z <= .002f) continue;
                var vertices = cable.Mesh.GetFaces(); var stationBounds = ReviewBounds(stationPart); var candidate = false;
                for (var vertex = 0; vertex < vertices.Length; vertex += 3)
                {
                    var triangle = new Aabb(cable.GlobalTransform * vertices[vertex], Vector3.Zero)
                        .Expand(cable.GlobalTransform * vertices[vertex + 1]).Expand(cable.GlobalTransform * vertices[vertex + 2]);
                    if (triangle.Intersects(stationBounds)) { candidate = true; break; }
                }
                cablesClear &= !candidate;
                GD.Print($"COOKIE_CABLE_TRIANGLE_SCREEN {cable.Name}/{stationPart.Name} potential_surface_contact={candidate}");
            }
            Check(cablesClear, "curved_cable_triangle_bounds_clear_axis_aligned_sealer_parts");
            GD.Print($"COOKIE_CYCLE_SAMPLES {ticks}"); GD.Print($"COOKIE_STATIC_CANDIDATES {LogBoundsCandidates(root)}");
        }
        catch (Exception error) { failures++; GD.PushError($"COOKIE_AUDIT_EXCEPTION {error}"); }
        GD.Print($"COOKIE_AUDIT_RESULT failures={failures}; prescribed offline batch and bounds checks only");
        DisableVirtualController(); GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
