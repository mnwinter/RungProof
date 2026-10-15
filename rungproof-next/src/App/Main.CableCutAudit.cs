using System;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditCableCut;
    private void AuditCableCut()
    {
        var failures = 0;
        void Check(bool condition, string label)
        { if (!condition) failures++; GD.Print($"CABLE_AUDIT {label}={condition}"); }
        try
        {
            AddMigratedScene("lab-4-12-cable-cut-length", _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
            var piece = root.GetNode<MeshInstance3D>("cable_strand/KIN_cable_piece");
            var blade = root.GetNode<Node3D>("training_accessory_7/KIN_cutter_blade");
            var reel = root.GetNode<Node3D>("training_accessory_4/KIN_payoff");
            var encoder = root.GetNode<Node3D>("training_accessory_6/KIN_lower_roll");
            var readout = root.GetNode<Node3D>("training_accessory_8").GetNode<Label3D>("NumericReadout");
            double Value(string key) => Convert.ToDouble(runtime.Points[key]);
            Check(!piece.Visible && runtime.Points["cable_present"] is true && runtime.Points["cutter_home"] is true
                && Value("stock_remaining_m") == 10 && readout.Text == "LENGTH m\n0.00", "initial_prethreaded_stock_home_and_live_measurement");
            Check(!ReviewMeshes(root).Any(mesh => mesh.Name.ToString().StartsWith("KIN_bottom_bar", StringComparison.Ordinal)), "scene_contains_no_substitute_roller_shutter");
            Check(ReviewMeshes(root).All(mesh => ReviewBounds(mesh).Position.Y >= -.002f), "all_equipment_above_floor");
            var loaded = LadderEditorProjectJson.Load(FileAccess.GetFileAsString("res://programs/examples/cable-cut-reference.rpproj.json"));
            Check(loaded.IsReadable && loaded.Document is not null, "saved_editable_reference_loads");
            var program = loaded.Document!.BuildProgram(); EnableVirtualControllerProgram(program);
            RunActiveController(); _PhysicsProcess(.1);
            Check(Value("measured_length_m") == 0 && runtime.Points["feed_run"] is false, "run_alone_leaves_initial_cable_waiting");
            runtime.ExecuteAction("start-cable-cut");
            var reported = new System.Collections.Generic.HashSet<string>();
            var knifeClear = true;
            var knifeBody = blade.GetNode<MeshInstance3D>("KNIFE_blade");
            var knifeObstacles = ReviewMeshes(root).Where(mesh => mesh != knifeBody && !mesh.Name.ToString().StartsWith("CABLE_", StringComparison.Ordinal) && mesh != piece && mesh.Name != "KNIFE_rod").ToArray();
            var geometry = true; var inventory = true; var numeric = true; var motion = true; var order = true; var samples = 0;
            var receiver = root.GetNode<Node3D>("cable_receiver").GetNode<MeshInstance3D>("RECEIVER_surface");
            var anvils = ReviewMeshes(root.GetNode<Node3D>("training_accessory_7")).Where(mesh => mesh.Name.ToString().StartsWith("KNIFE_slotted_anvil", StringComparison.Ordinal)).ToArray();
            var rightAnvil = anvils.OrderBy(mesh => ReviewBounds(mesh).Position.X).Last();
            var others = root.GetChildren().OfType<Node3D>().Where(node => node.Name != "cable_strand" && node.Name != "training_accessory_7")
                .SelectMany(ReviewMeshes).ToArray();
            while (samples < 1000 && runtime.Points["cycle_complete"] is not true)
            {
                samples++;
                _PhysicsProcess(.02);
                var length = Value("measured_length_m");
                foreach (var other in knifeObstacles)
                {
                    var overlap = ReviewBounds(knifeBody).Intersection(ReviewBounds(other)).Size;
                    if (overlap.X > .002f && overlap.Y > .002f && overlap.Z > .002f)
                    { knifeClear = false; if (reported.Add(other.GetPath())) GD.Print($"CABLE_KNIFE_CONTACT sample={samples} {other.GetPath()}"); }
                }
                inventory &= Math.Abs(length + Value("stock_remaining_m") - 10) < 1e-8 && length <= 3.000001;
                numeric &= readout.Text == "LENGTH m\n" + length.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                motion &= Math.Abs(blade.Position.Y + Value("cutter_position") * .35) < .0001
                    && Math.Abs(reel.Rotation.Z + length / .35) < .0001 && Math.Abs(encoder.Rotation.Z + length / .08) < .0001;
                order &= !(runtime.Points["feed_run"] is true && runtime.Points["cutter_fire"] is true)
                    && (Value("feed_speed_mps") == 0 || runtime.Points["cutter_home"] is true)
                    && (runtime.Points["cutter_fire"] is not true || runtime.Points["length_reached"] is true);
                if (!piece.Visible) continue;
                var bounds = ReviewBounds(piece); var surface = ReviewBounds(receiver); var anvil = ReviewBounds(rightAnvil);
                // Intentional knife/cable cutting interface and narrow slot are
                // excluded. Once cut, the full piece bears on anvil plus table.
                geometry &= Math.Abs(bounds.Position.Y - surface.End.Y) < .001
                    && Math.Abs(bounds.Size.X - length) < .001 && bounds.End.X <= surface.End.X + .001
                    && Math.Abs(anvil.End.X - surface.Position.X) < .001;
                foreach (var other in others)
                {
                    var overlap = bounds.Intersection(ReviewBounds(other)).Size;
                    if (overlap.X > .002f && overlap.Y > .002f && overlap.Z > .002f)
                    { geometry = false; if (reported.Add(other.GetPath())) GD.Print($"CABLE_PIECE_CONTACT sample={samples} {other.GetPath()}"); }
                }
            }
            Check(samples < 1000 && runtime.Points["cycle_complete"] is true && runtime.Points["cut_fault"] is false, "actual_twenty_ms_controller_finishes_cut_and_home_return");
            Check(geometry, "sampled_piece_dimensions_support_surfaces_and_selected_clearance");
            Check(knifeClear, "sampled_knife_body_clears_slotted_anvil_and_fixed_equipment");
            Check(inventory, "measured_piece_and_remaining_stock_conserve_available_length");
            Check(numeric, "fractional_and_completed_display_matches_measured_length");
            Check(motion, "reel_encoder_and_blade_follow_actual_plant_state");
            Check(order, "feed_and_cut_interlocks_hold_at_every_sample");
            Check(piece.Visible && Math.Abs(Value("measured_length_m") - 3) < .000001 && Value("stock_remaining_m") == 7
                && runtime.Points["feed_run"] is false && runtime.Points["cutter_fire"] is false
                && Math.Abs(ReviewBounds(piece).Position.X - .06) < .001, "completion_retains_supported_three_metre_piece_and_cut_gap");
            var retained = piece.Transform; runtime.ExecuteAction("start-cable-cut"); _PhysicsProcess(2);
            Check(piece.Transform == retained && Value("stock_remaining_m") == 7, "completed_start_cannot_recycle_or_feed_again");
            // Exercise retained feed, partial downstroke and partial upstroke.
            foreach (var phase in new[] { "feed", "down", "return" })
            {
                ResetActiveController(); RunActiveController(); _PhysicsProcess(.04); runtime.ExecuteAction("start-cable-cut");
                var ticks = 0;
                bool Reached() => phase == "feed" ? Value("measured_length_m") > 1
                    : phase == "down" ? runtime.Points["cut_done"] is false && Value("cutter_position") > .4
                    : runtime.Points["cut_done"] is true && Value("cutter_position") is > .3 and < .8;
                while (!Reached() && ticks++ < 900) _PhysicsProcess(.02);
                var pose = (piece.Transform, blade.Transform, reel.Transform); var amount = Value("stock_remaining_m");
                StopActiveController(); _PhysicsProcess(.5);
                GD.Print($"CABLE_STOP_PROBE phase={phase} ticks={ticks} piece_same={pose.Item1 == piece.Transform} blade_same={pose.Item2 == blade.Transform} reel_same={pose.Item3 == reel.Transform} stock_same={amount == Value("stock_remaining_m")} feed={runtime.Points["feed_run"]} fire={runtime.Points["cutter_fire"]}");
                Check(ticks < 900 && pose == (piece.Transform, blade.Transform, reel.Transform) && Value("stock_remaining_m") == amount
                    && runtime.Points["feed_run"] is false && runtime.Points["cutter_fire"] is false, $"stop_holds_{phase}_pose_inventory_and_clears_commands");
                RunActiveController(); _PhysicsProcess(.2);
                Check(pose == (piece.Transform, blade.Transform, reel.Transform) && Value("stock_remaining_m") == amount, $"run_without_fresh_start_holds_{phase}");
                runtime.ExecuteAction("start-cable-cut"); ticks = 0;
                while (runtime.Points["cycle_complete"] is not true && ticks++ < 1000) _PhysicsProcess(.02);
                Check(ticks < 1000 && runtime.Points["cut_fault"] is false && runtime.Points["cutter_home"] is true, $"fresh_start_resumes_{phase}_to_retained_completion");
            }
            ResetActiveController();
            Check(!piece.Visible && Value("measured_length_m") == 0 && Value("stock_remaining_m") == 10
                && blade.Position == Vector3.Zero && reel.Rotation == Vector3.Zero
                && _virtualController!.Snapshot.State == VirtualControllerState.Stopped, "reset_restores_stock_zero_length_home_axes_and_stopped_scans");
            foreach (var conflict in new[] { (Feed: true, Cut: true), (Feed: false, Cut: true) })
            {
                var plant = new CableCutPlantModel(); plant.Step(.1, true, conflict.Feed, conflict.Cut);
                Check(plant.Faulted && plant.LengthM == 0 && plant.Stroke == 0, conflict.Feed ? "simultaneous_feed_cut_faults_before_motion" : "cut_before_target_faults_before_motion");
            }
            var coarse = new CableCutPlantModel(); var fine = new CableCutPlantModel();
            coarse.Step(10, true, true, false); coarse.Step(.8, true, false, true); coarse.Step(.8, true, false, false);
            for (var tick = 0; tick < 1000; tick++) fine.Step(.01, true, true, false);
            for (var tick = 0; tick < 80; tick++) fine.Step(.01, true, false, true);
            for (var tick = 0; tick < 80; tick++) fine.Step(.01, true, false, false);
            Check(coarse.Complete && fine.Complete && Math.Abs(coarse.LengthM - fine.LengthM) < .000001, "coarse_and_fine_clock_partitions_conserve_same_cut");
            var invalid = true;
            foreach (var time in new[] { -1.0, double.NaN, double.PositiveInfinity })
                try { coarse.Step(time, false, false, false); invalid = false; } catch (ArgumentException) { }
            Check(invalid, "invalid_elapsed_time_rejected_even_when_idle");
            _visualSceneReview = true; RunActiveController(); SetGantryReviewClockHeld(true);
            var scans = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(2);
            Check(_virtualController.Snapshot.ScanNumber == scans, "native_hold_freezes_controller_clock");
            StepGantryReviewClock(); Check(_virtualController.Snapshot.ScanNumber == scans + 25, "native_step_runs_twenty_five_actual_twenty_ms_scans");
            StopActiveController(); scans = _virtualController.Snapshot.ScanNumber; StepGantryReviewClock();
            Check(_virtualController.Snapshot.ScanNumber == scans, "native_step_cannot_advance_stopped_controller"); ReleaseGantryReviewClock();
            GD.Print($"CABLE_CYCLE_SAMPLES {samples}"); GD.Print($"CABLE_STATIC_CANDIDATES {LogBoundsCandidates(root)}");
        }
        catch (Exception error) { failures++; GD.PushError($"CABLE_AUDIT_EXCEPTION {error}"); }
        GD.Print($"CABLE_AUDIT_RESULT failures={failures}; prescribed no-slip kinematics and sampled bounds only");
        DisableVirtualController(); GetTree().Quit(failures == 0 ? 0 : 1);
    }
}
