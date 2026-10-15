using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditDualSpindle;

    // Deliberately separate from the accepted geometry regression suite:
    // this audit checks the delivered shared-stock/feed/transfer installation
    // and exits nonzero on a regression. No PLC is contacted.
    private void AuditDualSpindle()
    {
        var failures = 0;
        void Check(bool condition, string requirement)
        {
            if (!condition) failures++;
            GD.Print($"DUAL_SPINDLE_AUDIT {requirement}={condition}");
        }
        try
        {
            AddMigratedScene("lab-2-22-dual-spindle", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var runtime = _sceneRuntime!;
            var probe = new LadderEditorDocument();
            probe.ResetProject("review-dual-start", "Dual_Start_QA", TimeSpan.FromMilliseconds(20));
            probe.SourceSceneId = "lab-2-22-dual-spindle";
            probe.AddTag("start_request", PlcVariableRole.Input, "operator.start");
            probe.AddTag("start_seen", PlcVariableRole.Memory);
            var startRung = probe.Rungs.Count;
            probe.AddRung("Observe Start without commanding spindle motion", "start_seen").CoilMode = LadderCoilMode.Set;
            probe.AddContact(startRung, 0, "start_request", false);
            EnableVirtualControllerProgram(probe.BuildProgram());
            try
            {
                Check(!ExecuteSelectedControllerAction("start-dual-drill"), "stopped_controller_rejects_start");
                RunActiveController();
                _PhysicsProcess(.02);
                Check(!_virtualController!.Snapshot.Variables.GetValueOrDefault("start_seen"), "run_alone_does_not_start");
                var accepted = ExecuteSelectedControllerAction("start-dual-drill");
                _PhysicsProcess(.02);
                Check(accepted && _virtualController.Snapshot.Variables.GetValueOrDefault("start_seen"), "start_action_reaches_ladder_input");
                _PhysicsProcess(.02);
                Check(!_virtualController.Snapshot.Variables.GetValueOrDefault("start_request"), "start_input_is_single_scan_pulse");
                ResetActiveController();
                Check(!_virtualController.Snapshot.Variables.GetValueOrDefault("start_seen"), "reset_clears_start_probe");
                var previousReview = _visualSceneReview;
                try
                {
                    _visualSceneReview = true;
                    SetGantryReviewClockHeld(true);
                    RunActiveController();
                    var before = _virtualController.Snapshot.ScanNumber;
                    _PhysicsProcess(.5);
                    Check(GantryReviewClockHeld && _virtualController.Snapshot.ScanNumber == before,
                        "dual_review_hold_prevents_unaccepted_scans");
                    AdvanceGantryReviewClock(25);
                    Check(_virtualController.Snapshot.ScanNumber == before + 25,
                        "dual_review_step_accepts_exactly_25_scans");
                    ResetActiveController();
                }
                finally { ReleaseGantryReviewClock(); _visualSceneReview = previousReview; }
            }
            finally { DisableVirtualController(); }
            runtime.ResetSimulation(); runtime.UsesExternalClock = true;
            runtime.SetControllerPlaybackRunning(true);
            var feedA = root.GetNode<Node3D>("drill_a").FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["drill_a_run"] = true });
            for (var tick = 0; tick < 25; tick++) runtime.AdvanceSimulation(.02);
            Check(feedA.PositionPercent == 0, "rotation_command_alone_does_not_feed");
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["drill_a_feed"] = true });
            for (var tick = 0; tick < 25; tick++) runtime.AdvanceSimulation(.02);
            Check(feedA.PositionPercent > 25 && feedA.PositionPercent < 35 && runtime.Points["drill_a_home"] is false
                && runtime.Points["drill_b_home"] is true, "controller_feed_moves_only_requested_head_and_updates_home");
            var feedB = root.GetNode<Node3D>("drill_b").FindChildren("*", "", true, false).OfType<EquipmentMotionController>().Single();
            var movingPlate = root.GetNode<Node3D>("metal_plate");
            var plateOrigin = movingPlate.Position;
            void Commands(params string[] requested)
            {
                var names = new[] { "drill_a_run", "drill_b_run", "drill_a_feed", "drill_b_feed", "drill_a_retract", "drill_b_retract", "transfer_extend", "transfer_retract" };
                runtime.CommitVirtualControllerOutputs(names.ToDictionary(name => name, name => requested.Contains(name)));
            }
            void Ticks(int count) { for (var tick = 0; tick < count; tick++) runtime.AdvanceSimulation(.02); }
            runtime.SetControllerPlaybackRunning(false);
            var pausedHead = root.GetNode<Node3D>("drill_a").FindChild("KIN_spindle", true, false) as Node3D;
            var pausedHeadPose = pausedHead!.Transform; Ticks(25);
            Check(pausedHead.Transform == pausedHeadPose, "stopped_clock_holds_partial_axial_feed_and_rotation");
            runtime.SetControllerPlaybackRunning(true);
            Commands(); var retained = feedA.PositionPercent; Ticks(25);
            Check(feedA.PositionPercent == retained, "withdrawn_feed_holds_actual_position");
            Commands("drill_a_run", "drill_a_feed", "drill_a_retract"); Ticks(25);
            Check(feedA.PositionPercent == retained, "opposed_feed_retract_requests_hold");
            Commands("transfer_extend"); Ticks(25);
            Check(movingPlate.Position == plateOrigin && runtime.Points["transfer_inhibited"] is true,
                "partial_head_inhibits_transfer_without_rewriting_request");
            Check(runtime.Points["transfer_extend"] is true, "transfer_inhibition_preserves_plc_command_image");
            Commands("drill_a_run", "drill_b_run", "drill_a_feed", "drill_b_feed"); Ticks(100);
            Check(feedA.PositionPercent == 100 && feedB.PositionPercent == 100
                && runtime.Points["drill_a_at_depth"] is true && runtime.Points["drill_b_at_depth"] is true,
                "both_heads_reach_actual_depth_limits");
            Commands("drill_a_retract"); Ticks(80);
            Check(feedA.PositionPercent == 0 && feedB.PositionPercent == 100 && runtime.Points["drill_b_home"] is false,
                "first_head_retracts_independently_without_rotation");
            Commands("transfer_extend"); Ticks(25);
            Check(movingPlate.Position == plateOrigin, "second_head_not_home_blocks_transfer");
            Commands("drill_b_retract"); Ticks(81);
            Check(runtime.Points["drill_a_home"] is true && runtime.Points["drill_b_home"] is true,
                "both_homes_follow_independent_retractions");
            Commands("drill_a_run", "transfer_extend"); Ticks(25);
            Check(movingPlate.Position == plateOrigin && runtime.Points["transfer_inhibited"] is true,
                "rotating_head_inhibits_transfer_even_when_home");
            Commands("transfer_extend", "transfer_retract"); Ticks(25);
            Check(movingPlate.Position == plateOrigin && runtime.Points["transfer_inhibited"] is true,
                "opposed_transfer_requests_hold_fixture");
            Commands("transfer_extend");
            var supportTop = ReviewBounds((MeshInstance3D)root.GetNode<Node3D>("plate_bed").FindChild("BENCH_top", true, false));
            var supportedTransfer = true;
            void SupportedTicks(int count)
            {
                for (var tick = 0; tick < count; tick++)
                {
                    runtime.AdvanceSimulation(.02);
                    var underside = ReviewBounds((MeshInstance3D)movingPlate.FindChild("FIXTURE_SUBPLATE", true, false));
                    supportedTransfer &= MathF.Abs(underside.Position.Y - supportTop.End.Y) < .002f
                        && underside.Position.X >= supportTop.Position.X && underside.End.X <= supportTop.End.X
                        && underside.Position.Z >= supportTop.Position.Z && underside.End.Z <= supportTop.End.Z;
                }
            }
            SupportedTicks(30);
            Check(MathF.Abs(movingPlate.Position.X - plateOrigin.X - 1.1f) < .001f
                && runtime.Points["transfer_home"] is false, "controller_slide_moves_shared_fixture_at_declared_speed");
            var stoppedPlate = movingPlate.Transform;
            runtime.SetControllerPlaybackRunning(false); Ticks(25);
            Check(movingPlate.Transform == stoppedPlate, "stopped_clock_holds_mid_transfer");
            runtime.SetControllerPlaybackRunning(true); SupportedTicks(31);
            Check(MathF.Abs(movingPlate.Position.X - plateOrigin.X - 2.2f) < .001f
                && runtime.Points["transfer_at_end"] is true, "controller_transfer_retains_supported_endpoint");
            Check(supportedTransfer, "controller_transfer_fixture_supported_at_all_20ms_samples");
            Commands("drill_a_run", "drill_a_feed"); Ticks(25);
            Check(feedA.PositionPercent == 0, "displaced_fixture_blocks_axial_feed");
            Commands("transfer_retract"); Ticks(61);
            Check(movingPlate.Position.IsEqualApprox(plateOrigin) && runtime.Points["transfer_home"] is true,
                "explicit_transfer_retract_restores_fixture_home");
            runtime.ResetSimulation();
            Check(feedA.PositionPercent == 0 && feedB.PositionPercent == 0 && runtime.Points["transfer_inhibited"] is false,
                "controller_reset_restores_heads_fixture_and_feedback");
            runtime.UsesExternalClock = false; // Explicit reference preview only.
            var fixture = root.GetNode<Node3D>("metal_plate");
            var slide = root.GetNode<Node3D>("plate_transfer");
            var bed = root.GetNode<Node3D>("plate_bed");
            var drills = new[] { root.GetNode<Node3D>("drill_a"), root.GetNode<Node3D>("drill_b") };
            MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
            var stock = Part(fixture, "STEEL_WORKPIECE");
            var subplate = Part(fixture, "FIXTURE_SUBPLATE");
            var bits = drills.Select(drill => Part(drill, "DRILL_bit")).ToArray();
            var spindles = drills.Select(drill => (Node3D)drill.FindChild("KIN_spindle", true, false)).ToArray();
            var home = spindles.Select(spindle => spindle.GlobalPosition).ToArray();
            var fixtureHome = fixture.Transform;
            var bedTop = Part(bed, "BENCH_top");
            var bedBounds = ReviewBounds(bedTop);
            foreach (var equipment in root.GetChildren().OfType<Node3D>())
            foreach (var mesh in ReviewMeshes(equipment))
                GD.Print($"DUAL_SPINDLE_MESH {equipment.Name}/{mesh.Name} {ReviewBounds(mesh)}");
            var stockBounds = ReviewBounds(stock);
            bool AxesOverStock() => bits.All(bit =>
            {
                var center = ReviewBounds(bit).GetCenter();
                return center.X >= stockBounds.Position.X && center.X <= stockBounds.End.X
                    && center.Z >= stockBounds.Position.Z && center.Z <= stockBounds.End.Z;
            });
            Check(AxesOverStock(), "both_drill_axes_over_the_shared_steel_workpiece");
            Check(drills.All(drill => ReviewMeshes(drill).All(mesh => mesh.Name != "DRILL_workpiece")),
                "one_shared_workpiece_without_separate_drill_coupons");
            // Necessary mounting contact, not a proof of load capacity. The
            // fixture's actual bottom must bear on an existing table/slide.
            bool HasBearingContact()
            {
                var bottom = ReviewBounds(subplate);
                return root.GetChildren().OfType<Node3D>().Where(node => node != fixture)
                    .SelectMany(ReviewMeshes).Any(mesh =>
                    {
                        var support = ReviewBounds(mesh);
                        return MathF.Abs(support.End.Y - bottom.Position.Y) <= 0.002f
                            && support.Position.X < bottom.End.X && support.End.X > bottom.Position.X
                            && support.Position.Z < bottom.End.Z && support.End.Z > bottom.Position.Z;
                    });
            }
            Check(HasBearingContact(), "fixture_has_actual_bearing_surface_at_home");
            var feet = ReviewMeshes(bed).Where(mesh => mesh.Name.ToString().StartsWith("BENCH_leg", StringComparison.Ordinal)).ToArray();
            Check(feet.Length == 4 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f
                && ReviewMeshes(bed).Where(other => other != foot).Any(other => ReviewBounds(foot).Grow(0.002f).Intersects(ReviewBounds(other)))),
                "bed_has_four_grounded_connected_legs");
            var cache = new Dictionary<MeshInstance3D, Aabb>();
            Aabb Bounds(MeshInstance3D mesh)
            {
                if (!cache.TryGetValue(mesh, out var bounds)) cache[mesh] = bounds = ReviewBounds(mesh);
                return bounds;
            }
            bool Clear(MeshInstance3D a, MeshInstance3D b)
            {
                var overlap = Bounds(a).Intersection(Bounds(b)).Size;
                if (overlap.X <= 0.002f || overlap.Y <= 0.002f || overlap.Z <= 0.002f || !OrientedBoxesPenetrate(a, b)) return true;
                var cable = a.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase) ? a
                    : b.Name.ToString().Contains("cable", StringComparison.OrdinalIgnoreCase) ? b : null;
                if (cable is not null)
                {
                    var other = cable == a ? b : a;
                    var transform = other.GlobalTransform.AffineInverse() * cable.GlobalTransform;
                    var faces = cable.Mesh.GetFaces();
                    var otherBounds = other.GetAabb();
                    var candidate = false;
                    for (var i = 0; i < faces.Length && !candidate; i += 3)
                        candidate = new Aabb(transform * faces[i], Vector3.Zero).Expand(transform * faces[i + 1])
                            .Expand(transform * faces[i + 2]).Grow(0.0001f).Intersects(otherBounds);
                    if (!candidate) return true;
                }
                GD.Print($"DUAL_SPINDLE_CLEARANCE {a.Name}/{b.Name} {Bounds(a)} {Bounds(b)}");
                return false;
            }
            var stationaryClear = true;
            var groups = root.GetChildren().OfType<Node3D>().Where(node => node != fixture).ToArray();
            for (var a = 0; a < groups.Length; a++)
            for (var b = a + 1; b < groups.Length; b++)
            foreach (var left in ReviewMeshes(groups[a]))
            foreach (var right in ReviewMeshes(groups[b])) stationaryClear &= Clear(left, right);
            Check(stationaryClear, "all_separate_equipment_clear_at_home");
            var controllers = root.FindChildren("*", "", true, false).OfType<EquipmentMotionController>().ToArray();
            var slidePlate = Part(slide, "KIN_pusher_plate");
            var plateHome = slidePlate.GlobalPosition;
            var maximumFeed = new float[2];
            var maximumSlide = 0.0f;
            var supported = true;
            var contactAtTransfer = true;
            var transferObserved = false;
            var fullFootprint = true;
            var cutObserved = new bool[2];
            var toolBearings = true;
            var feedbackMatches = true;
            var motionClear = true;
            var movingParts = ReviewMeshes(fixture);
            var surrounding = groups.SelectMany(ReviewMeshes).ToArray();
            Check(runtime.ExecuteAction("start-dual-drill") && !runtime.ExecuteAction("start-dual-drill"),
                "active_preview_cannot_restart_from_authored_coordinates");
            for (var tick = 0; tick < 3000; tick++)
            {
                runtime.AdvanceSimulation(0.002);
                foreach (var controller in controllers) controller._PhysicsProcess(0.002);
                for (var index = 0; index < drills.Length; index++)
                    maximumFeed[index] = MathF.Max(maximumFeed[index], home[index].Y - spindles[index].GlobalPosition.Y);
                maximumSlide = MathF.Max(maximumSlide, slidePlate.GlobalPosition.DistanceTo(plateHome));
                supported &= HasBearingContact();
                var loadBounds = ReviewBounds(fixture);
                fullFootprint &= loadBounds.Position.X >= bedBounds.Position.X - 0.002f && loadBounds.End.X <= bedBounds.End.X + 0.002f
                    && loadBounds.Position.Z >= bedBounds.Position.Z - 0.002f && loadBounds.End.Z <= bedBounds.End.Z + 0.002f;
                for (var index = 0; index < drills.Length; index++)
                {
                    var tip = ReviewBounds(bits[index]);
                    cutObserved[index] |= tip.Position.Y > stockBounds.Position.Y && tip.Position.Y < stockBounds.End.Y;
                    toolBearings &= ReviewBounds(Part(drills[index], "DRILL_quill"))
                        .Intersects(ReviewBounds(Part(drills[index], "DRILL_spindle_bearing_housing")));
                    feedbackMatches &= (runtime.Points[$"drill_{(index == 0 ? "a" : "b")}_home"] is true)
                        == (MathF.Abs(spindles[index].GlobalPosition.Y - home[index].Y) < 0.00001f);
                }
                // Ten-ms clearance samples supplement the two-ms support/
                // feed/contact checks. Cutting the intended stock is allowed;
                // hitting clamps, supports or another machine is not.
                if (tick % 5 == 0)
                {
                    cache.Clear();
                    foreach (var part in movingParts)
                    foreach (var other in surrounding)
                        if (!(part == stock && other.Name == "DRILL_bit")) motionClear &= Clear(part, other);
                    foreach (var part in ReviewMeshes(slide).Where(mesh => mesh.Name.ToString().StartsWith("KIN_pusher_", StringComparison.Ordinal)))
                    foreach (var other in groups.Where(node => node != slide).SelectMany(ReviewMeshes)) motionClear &= Clear(part, other);
                }
                if (runtime.Points["transfer_extend"] is true)
                {
                    transferObserved = true;
                    contactAtTransfer &= ReviewBounds(slidePlate).Grow(0.002f).Intersects(ReviewBounds(subplate));
                }
            }
            var travel = fixture.GlobalPosition.X - fixtureHome.Origin.X;
            GD.Print($"DUAL_SPINDLE_TRAVEL fixtureX={travel} slide={maximumSlide} feedA={maximumFeed[0]} feedB={maximumFeed[1]}");
            Check(maximumFeed.All(feed => feed > 0.001f), "both_declared_position_motions_produce_axial_feed");
            Check(supported, "fixture_has_bearing_contact_through_entire_preview");
            Check(fullFootprint, "entire_fixture_footprint_supported_through_transfer");
            Check(cutObserved.All(value => value) && toolBearings, "bits_enter_shared_stock_with_quills_retained_in_bearings");
            Check(feedbackMatches, "reference_home_feedback_matches_actual_axial_position");
            Check(motionClear, "moving_fixture_and_slide_clear_clamps_heads_and_bed_through_600_samples");
            Check(transferObserved && contactAtTransfer, "slide_contacts_fixture_through_transfer");
            Check(MathF.Abs(travel - maximumSlide) <= 0.002f, "fixture_transfer_matches_actual_slide_stroke");
            Check(runtime.Points["cycle_complete"] is true, "timed_reference_reports_completion");
            Check(!runtime.ExecuteAction("start-dual-drill"), "transferred_fixture_requires_reset_before_repeat");
            runtime.ResetSimulation();
            Check(fixture.Transform.IsEqualApprox(fixtureHome) && spindles.Select((spindle, i) => spindle.GlobalPosition.IsEqualApprox(home[i])).All(value => value),
                "reset_restores_fixture_and_both_spindle_homes");
            runtime.ExecuteAction("start-dual-drill");
            for (var tick = 0; tick < 250; tick++) runtime.AdvanceSimulation(0.002);
            runtime.StopSimulation();
            var stopped = spindles.Select(spindle => spindle.Transform).ToArray();
            var stoppedFixture = fixture.Transform;
            for (var tick = 0; tick < 250; tick++)
            {
                runtime.AdvanceSimulation(0.002);
                foreach (var controller in controllers) controller._PhysicsProcess(0.002);
            }
            Check(spindles.Select((spindle, i) => spindle.Transform.IsEqualApprox(stopped[i])).All(value => value)
                && fixture.Transform.IsEqualApprox(stoppedFixture) && !runtime.ExecuteAction("start-dual-drill"),
                "stop_holds_partial_feed_and_blocks_unsafe_restart");
            runtime.ResetSimulation();
        }
        catch (Exception error)
        {
            failures++;
            GD.PushError($"DUAL_SPINDLE_AUDIT_EXCEPTION {error}");
        }
        GD.Print($"DUAL_SPINDLE_AUDIT_RESULT failures={failures}; bounds/contact screen only, no mechanical or controller acceptance");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
