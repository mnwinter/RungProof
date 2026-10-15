using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditBarrelFill;
    private void AuditBarrelFill()
    {
        var failures = 0;
        void Check(bool condition, string label)
        { if (!condition) failures++; GD.Print($"BARREL_AUDIT {label}={condition}"); }
        try
        {
            AddMigratedScene("lab-4-11-barrel-fill-station", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
            var barrel = root.GetNode<Node3D>("training_accessory_5");
            var liquid = barrel.GetNode<MeshInstance3D>("KIN_barrel_liquid");
            var source = root.GetNode<Node3D>("tank_1").GetNode<MeshInstance3D>("KIN_supply_liquid");
            var nozzle = root.GetNode<Node3D>("training_accessory_7");
            var stream = nozzle.GetNode<MeshInstance3D>("KIN_fill_stream");
            var drive = root.GetNode<Node3D>("conveyor_0").FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
            var belt = ReviewMeshes(root.GetNode<Node3D>("conveyor_0")).Single(mesh => mesh.Name == "KIN_belt_surface");
            var fillBeams = ReviewMeshes(root.GetNode<Node3D>("photoeye_3")).Where(mesh => mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
            var exitBeams = ReviewMeshes(root.GetNode<Node3D>("exit_photoeye")).Where(mesh => mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
            double Value(string key) => Convert.ToDouble(runtime.Points[key]);
            double Volume(MeshInstance3D mesh)
            {
                var bounds = ReviewBounds(mesh);
                return Math.PI * bounds.Size.X * bounds.Size.Z / 4 * bounds.Size.Y * 1000;
            }
            Check(!drive.IsPhysicsProcessing(), "single_plant_clock_disables_independent_belt_callback");
            Check(!liquid.Visible && !stream.Visible && Math.Abs(Value("source_litres") - 200) < .001 && runtime.Points["downstream_clear"] is true, "initial_empty_barrel_source_inventory_and_clear_outfeed");
            Check(ReviewMeshes(root).All(mesh => ReviewBounds(mesh).Position.Y >= -.002f), "equipment_above_finished_floor");
            // Measured pipe-face datums, not equipment-root distances. Paths
            // include intended socket/pipe joins; no pressure rating is claimed.
            Aabb Bound(string id, string name) => ReviewBounds(root.GetNode<Node3D>(id).GetNode<MeshInstance3D>(name));
            var nozzlePath = Bound("training_accessory_7", "FILL_nozzle");
            Vector3 Face(string id, string name) => Bound(id, name).GetCenter();
            Check(Face("tank_1", "SUPPLY_outlet_end_ring").DistanceTo(Face("valve_2", "VALVE_bore_start_ring")) < .001
                && Face("valve_2", "VALVE_bore_end_ring").DistanceTo(Face("training_accessory_6", "METER_inlet_start_ring")) < .001
                && Face("training_accessory_6", "METER_outlet_end_ring").DistanceTo(Face("training_accessory_7", "FILL_nozzle_start_ring")) < .001, "source_valve_meter_nozzle_pipe_faces_contiguous");
            Check(Math.Abs(nozzlePath.Position.Y - 2.1) < .001 && nozzlePath.Position.Y > ReviewBounds(barrel).End.Y + .25f, "fixed_nozzle_clears_barrel_rim_through_transfer");
            var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/barrel-fill-reference.rpproj.json"));
            Check(loaded.IsReadable && loaded.Document is not null, "saved_editable_reference_loads");
            var program = loaded.Document!.BuildProgram(); EnableVirtualControllerProgram(program);
            RunActiveController(); _PhysicsProcess(.1);
            Check(barrel.Position.X < -2.79f && Value("barrel_litres") == 0 && runtime.Points["infeed_run"] is false, "run_alone_holds_position_and_volume");
            runtime.ExecuteAction("start-barrel-batch");
            var support = true; var clearance = true; var optical = true; var inventory = true; var flow = true; var visible = true; var readoutCorrect = true;
            var oldLitres = 0.0; var samples = 0;
            var body = barrel.GetNode<MeshInstance3D>("BARREL_bottom");
            var parts = ReviewMeshes(barrel);
            var obstacles = root.GetChildren().OfType<Node3D>().Where(node => node != barrel).SelectMany(ReviewMeshes)
                .Where(mesh => !mesh.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal) && mesh != stream).ToArray();
            var cache = new Dictionary<MeshInstance3D, (Transform3D Pose, Aabb Bounds)>();
            Aabb Bounds(MeshInstance3D mesh)
            {
                if (cache.TryGetValue(mesh, out var saved) && saved.Pose == mesh.GlobalTransform) return saved.Bounds;
                var bound = ReviewBounds(mesh); cache[mesh] = (mesh.GlobalTransform, bound); return bound;
            }
            var reported = new HashSet<string>();
            while (samples++ < 2000 && runtime.Points["cycle_complete"] is not true)
            {
                _PhysicsProcess(.02);
                var baseBounds = Bounds(body); var deck = Bounds(belt);
                support &= Math.Abs(baseBounds.Position.Y - deck.End.Y) < .001
                    && baseBounds.Position.X >= deck.Position.X && baseBounds.End.X <= deck.End.X
                    && baseBounds.Position.Z >= deck.Position.Z && baseBounds.End.Z <= deck.End.Z;
                visible &= barrel.IsInsideTree() && barrel.Visible;
                foreach (var part in parts.Where(mesh => mesh.IsVisibleInTree()))
                foreach (var other in obstacles)
                {
                    var overlap = Bounds(part).Intersection(Bounds(other)).Size;
                    if (overlap.X > .002f && overlap.Y > .002f && overlap.Z > .002f)
                    {
                        clearance = false;
                        var key = part.Name + "/" + other.GetPath();
                        if (reported.Add(key)) GD.Print($"BARREL_CONTACT sample={samples} {key} load={Bounds(part)} other={Bounds(other)}");
                    }
                }
                bool Blocked(MeshInstance3D[] beams)
                {
                    var center = beams.Select(Bounds).Aggregate((left, right) => left.Merge(right)).GetCenter();
                    return Bounds(barrel.GetNode<MeshInstance3D>("BARREL_wall")).Grow(.0000001f).HasPoint(center);
                }
                var fillBlocked = Blocked(fillBeams); var exitBlocked = Blocked(exitBeams);
                if ((runtime.Points["fill_beam_blocked"] is true) != fillBlocked || (runtime.Points["exit_beam_blocked"] is true) != exitBlocked)
                    GD.Print($"BARREL_OPTICAL_MISMATCH sample={samples} x={barrel.Position.X:R} fill={fillBlocked}/{runtime.Points["fill_beam_blocked"]} exit={exitBlocked}/{runtime.Points["exit_beam_blocked"]}");
                optical &= (runtime.Points["fill_beam_blocked"] is true) == fillBlocked && (runtime.Points["exit_beam_blocked"] is true) == exitBlocked
                    && fillBeams.All(beam => beam.Visible == !fillBlocked) && exitBeams.All(beam => beam.Visible == !exitBlocked);
                var litres = Value("barrel_litres");
                readoutCorrect &= root.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout").Text
                    == "LITRES\n" + litres.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                inventory &= Math.Abs(litres + Value("source_litres") - 200) < .001
                    && litres >= oldLitres && litres <= 150.001 && Value("source_litres") >= 49.999
                    && Math.Abs(Volume(source) - Value("source_litres")) < .02
                    && (!liquid.Visible || Math.Abs(Volume(liquid) - litres) < .02);
                if (stream.Visible)
                {
                    var streamBounds = Bounds(stream); var surface = Bounds(liquid).End.Y;
                    flow &= runtime.Points["barrel_at_fill"] is true && runtime.Points["infeed_run"] is false
                        && Math.Abs(streamBounds.End.Y - 2.1f) < .001 && Math.Abs(streamBounds.Position.Y - surface) < .001;
                }
                flow &= stream.Visible == (Value("flow_lps") > 0);
                oldLitres = litres;
            }
            Check(samples < 2000 && runtime.Points["cycle_complete"] is true && runtime.Points["fill_fault"] is false, "actual_twenty_ms_controller_completes_fill_and_discharge");
            Check(support && visible, "barrel_remains_supported_and_visible_through_sampled_cycle");
            Check(clearance, "visible_barrel_bounds_clear_other_equipment_through_cycle");
            Check(optical, "both_actual_optical_centerlines_and_rendered_beams_match_barrel");
            Check(inventory, "liquid_inventory_conserved_and_both_rendered_volumes_match_feedback");
            Check(flow, "flow_only_into_stationary_barrel_and_stream_ends_at_liquid_surface");
            Check(readoutCorrect, "fractional_meter_readout_stays_one_decimal_through_cycle");
            Check(Math.Abs(Value("barrel_litres") - 150) < .001 && Math.Abs(Value("source_litres") - 50) < .001
                && Math.Abs(barrel.Position.X - 3) < .001 && !stream.Visible && runtime.Points["infeed_run"] is false
                && runtime.Points["fill_valve_open"] is false && runtime.Points["downstream_clear"] is false, "filled_barrel_retained_with_commands_off_and_outfeed_occupied");
            Check(root.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout").Text == "LITRES\n150.0", "meter_displays_actual_transferred_volume");
            var parked = barrel.Transform; runtime.ExecuteAction("start-barrel-batch"); _PhysicsProcess(1);
            Check(barrel.Transform == parked && Value("barrel_litres") == 150, "completed_batch_cannot_restart_recycle_or_disappear");
            var stopResume = true;
            foreach (var phase in new[] { "infeed", "fill", "outfeed", "occupied-outfeed" })
            {
                ResetActiveController(); RunActiveController(); _PhysicsProcess(.04); runtime.ExecuteAction("start-barrel-batch");
                bool AtPhase() => phase switch
                {
                    "infeed" => barrel.Position.X > -2 && barrel.Position.X < -1,
                    "fill" => Value("barrel_litres") > 40 && Value("barrel_litres") < 80,
                    "outfeed" => barrel.Position.X > .8 && barrel.Position.X < 1.2,
                    _ => barrel.Position.X > 2.6 && barrel.Position.X < 2.8,
                };
                for (var tick = 0; tick < 2000 && !AtPhase(); tick++) _PhysicsProcess(.02);
                stopResume &= AtPhase(); StopActiveController(); var pose = barrel.Transform; var heldLitres = Value("barrel_litres");
                _PhysicsProcess(1); RunActiveController(); _PhysicsProcess(.2);
                stopResume &= barrel.Transform == pose && Value("barrel_litres") == heldLitres && !stream.Visible && Value("flow_lps") == 0;
                runtime.ExecuteAction("start-barrel-batch"); _PhysicsProcess(.2);
                stopResume &= barrel.Transform != pose || Value("barrel_litres") > heldLitres;
            }
            Check(stopResume, "stop_run_fresh_start_holds_and_resumes_all_four_phases_including_owned_outfeed");
            ResetActiveController();
            Check(barrel.Position.X < -2.79f && !liquid.Visible && !stream.Visible && Value("barrel_litres") == 0 && Value("source_litres") == 200, "reset_restores_empty_barrel_source_and_flow");
            DisableVirtualController(); runtime.UsesExternalClock = true; runtime.SetControllerPlaybackRunning(true);
            runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["infeed_run"] = true, ["fill_valve_open"] = true }); runtime.AdvanceSimulation(.1);
            Check(runtime.Points["fill_fault"] is true && runtime.Points["infeed_run"] is true && runtime.Points["fill_valve_open"] is true
                && barrel.Position.X < -2.79f && Value("barrel_litres") == 0, "invalid_fill_fault_holds_without_rewriting_plc_commands");
            runtime.SetExternalPlayback(true, false); runtime.AdvanceSimulation(2);
            Check(barrel.Position.X < -2.79f && runtime.Points["fill_valve_open"] is true && !stream.Visible, "external_pause_preserves_commands_and_inventory");
            runtime.SetExternalPlayback(false, false); runtime.ResetSimulation();
            var large = new BarrelFillPlantModel(); var small = new BarrelFillPlantModel();
            large.Step(10, true, false); for (var i = 0; i < 500; i++) small.Step(.02, true, false);
            large.Step(8, false, true); for (var i = 0; i < 400; i++) small.Step(.02, false, true);
            large.Step(11, true, false); for (var i = 0; i < 550; i++) small.Step(.02, true, false);
            Check(large.Complete && small.Complete && large.Litres == small.Litres && Math.Abs(large.X - small.X) < 1e-8, "large_and_small_timesteps_reach_identical_retained_inventory");
            var empty = new BarrelFillPlantModel(); empty.Step(.1, false, true);
            Check(empty.Faulted && empty.Litres == 0 && empty.SourceLitres == 200, "valve_without_indexed_barrel_faults_without_phantom_fill");
            var invalid = true;
            foreach (var value in new[] { -1.0, double.NaN, double.PositiveInfinity })
            { try { empty.Step(value, false, false); invalid = false; } catch (ArgumentException) { } }
            Check(invalid, "invalid_elapsed_time_rejected");
            EnableVirtualControllerProgram(program); RunActiveController(); _PhysicsProcess(.04);
            var wasReview = _visualSceneReview; _visualSceneReview = true; SetGantryReviewClockHeld(true);
            var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(3);
            Check(_virtualController.Snapshot.ScanNumber == scan && GantryReviewClockHeld, "native_review_hold_freezes_controller_clock");
            runtime.ExecuteAction("start-barrel-batch"); StepGantryReviewClock();
            Check(_virtualController.Snapshot.ScanNumber == scan + 100 && Math.Abs(barrel.Position.X + 2.2f) < .001, "native_review_step_runs_two_seconds_actual_scans");
            StopActiveController(); scan = _virtualController.Snapshot.ScanNumber; StepGantryReviewClock();
            Check(_virtualController.Snapshot.ScanNumber == scan, "native_review_step_cannot_advance_stopped_controller");
            ReleaseGantryReviewClock(); _visualSceneReview = wasReview; ResetActiveController();
            // Ray/triangle test excludes false cable-envelope hits while
            // proving that fixed props do not obstruct either optical path.
            bool HitsTriangle(Vector3 origin, Vector3 ray, Vector3 a, Vector3 b, Vector3 c)
            {
                var e1 = b - a; var e2 = c - a; var p = ray.Cross(e2); var determinant = e1.Dot(p);
                if (Math.Abs(determinant) < 1e-9f) return false;
                var inverse = 1 / determinant; var t = origin - a; var u = t.Dot(p) * inverse;
                if (u < 0 || u > 1) return false;
                var q = t.Cross(e1); var v = ray.Dot(q) * inverse;
                if (v < 0 || u + v > 1) return false;
                var distance = e2.Dot(q) * inverse; return distance > 1e-6 && distance < 1 - 1e-6;
            }
            var opticalClear = true;
            foreach (var beams in new[] { fillBeams, exitBeams })
            {
                var bounds = beams.Select(ReviewBounds).Aggregate((left, right) => left.Merge(right));
                var start = bounds.GetCenter() with { Z = bounds.Position.Z }; var ray = Vector3.Back * bounds.Size.Z;
                foreach (var obstacle in obstacles)
                {
                    var faces = obstacle.Mesh.GetFaces();
                    for (var i = 0; i < faces.Length; i += 3)
                        if (HitsTriangle(start, ray, obstacle.GlobalTransform * faces[i], obstacle.GlobalTransform * faces[i + 1], obstacle.GlobalTransform * faces[i + 2]))
                        { opticalClear = false; GD.Print($"BARREL_FIXED_OPTICAL_CONTACT {obstacle.GetPath()}"); break; }
                }
            }
            Check(opticalClear, "both_optical_rays_clear_fixed_equipment_surface_triangles");
            var cablesClear = true;
            foreach (var cable in ReviewMeshes(root.GetNode<Node3D>("conveyor_0")).Where(mesh => mesh.Name.ToString().StartsWith("CTRL_", StringComparison.Ordinal) && mesh.Name.ToString().EndsWith("_cable", StringComparison.Ordinal)))
            foreach (var id in new[] { "valve_2", "training_accessory_7" })
            foreach (var part in ReviewMeshes(root.GetNode<Node3D>(id)))
            {
                var overlap = ReviewBounds(cable).Intersection(ReviewBounds(part)).Size;
                if (overlap.X <= .002f || overlap.Y <= .002f || overlap.Z <= .002f) continue;
                var faces = cable.Mesh.GetFaces(); var candidate = false;
                for (var i = 0; i < faces.Length; i += 3)
                {
                    var triangle = new Aabb(cable.GlobalTransform * faces[i], Vector3.Zero)
                        .Expand(cable.GlobalTransform * faces[i + 1]).Expand(cable.GlobalTransform * faces[i + 2]);
                    if (triangle.Intersects(ReviewBounds(part))) { candidate = true; break; }
                }
                cablesClear &= !candidate;
                GD.Print($"BARREL_CABLE_TRIANGLE_SCREEN {cable.Name}/{part.Name} potential_surface_contact={candidate}");
            }
            Check(cablesClear, "curved_cable_triangle_bounds_clear_valve_and_portal_parts");
            GD.Print($"BARREL_CYCLE_SAMPLES {samples}"); GD.Print($"BARREL_STATIC_CANDIDATES {LogBoundsCandidates(root)}");
        }
        catch (Exception error) { failures++; GD.PushError($"BARREL_AUDIT_EXCEPTION {error}"); }
        GD.Print($"BARREL_AUDIT_RESULT failures={failures}; prescribed offline flow and sampled bounds only");
        DisableVirtualController(); GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
