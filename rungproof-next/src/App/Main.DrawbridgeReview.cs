using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditDrawbridge;
    private static readonly string[] BridgeOutputs = ["bridge_raise", "bridge_lower", "barrier_close", "barrier_open", "traffic_release", "traffic_stop"];

    private static void AddDrawbridgeFrameEnvelope(Node3D root, List<Vector3> points)
    {
        // Fit the entire known angular travel before Run. Calculate temporary
        // transforms only; never move the plant to obtain a camera envelope.
        foreach (var pivot in root.FindChildren("BridgeMotionPivot", "Node3D", true, false).OfType<Node3D>())
        {
            var parent = pivot.GetParent<Node3D>();
            var meshes = ReviewMeshes(pivot);
            var transforms = meshes.Select(m => pivot.GlobalTransform.AffineInverse() * m.GlobalTransform).ToArray();
            var maximum = meshes.Any(m => m.Name == "KIN_gate_arm") ? 90 : 70;
            for (var degrees = 0; degrees <= maximum; degrees++)
            {
                var pose = parent.GlobalTransform * new Transform3D(new Basis(Vector3.Back, Mathf.DegToRad(degrees)), pivot.Position);
                for (var m = 0; m < meshes.Length; m++)
                {
                    var box = meshes[m].GetAabb(); var transform = pose * transforms[m];
                    for (var corner = 0; corner < 8; corner++) points.Add(transform * (box.Position + box.Size * new Vector3(
                        (corner & 1) == 0 ? 0 : 1, (corner & 2) == 0 ? 0 : 1, (corner & 4) == 0 ? 0 : 1)));
                }
            }
        }
    }

    private void AuditDrawbridge()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"DRAWBRIDGE_CHECK {name}={ok}"); }
        try { VerifyDrawbridgeWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"DRAWBRIDGE_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private static LadderEditorDocument CreateDrawbridgeReference()
    {
        // Explicitly opened illustrative program, never the default exercise.
        // Raw limits come from the PC plant; PLC outputs own the sequence.
        var d = new LadderEditorDocument();
        d.ResetProject("review-drawbridge", "Drawbridge_Reference", TimeSpan.FromMilliseconds(20));
        d.SourceSceneId = "lab-5-08-drawbridge-control";
        foreach (var input in new[] { "traffic_stopped", "bridge_request", "bridge_home", "bridge_raised", "barriers_closed", "barriers_open", "motion_inhibited" })
            d.AddTag(input, PlcVariableRole.Input, input);
        foreach (var input in new[] { "bridge_angle", "barrier_angle" }) d.AddTag(input, PlcVariableRole.Input, input, type: PlcVariableType.Real);
        foreach (var output in BridgeOutputs) d.AddTag(output, PlcVariableRole.Output, output);
        void Rung(string label, string output, params (string Point, bool NC)[] contacts)
        {
            var r = d.Rungs.Count; d.AddRung(label, output);
            foreach (var (point, nc) in contacts) d.AddContact(r, 0, point, nc);
        }
        Rung("Close road barriers on request or whenever bridge off home", "barrier_close", ("bridge_request", false));
        d.AddParallelBranch(0); d.AddContact(0, 1, "bridge_home", true);
        Rung("Open barriers only after bridge home with no motion command", "barrier_open", ("bridge_home", false), ("bridge_request", true),
            ("barrier_close", true), ("bridge_raise", true), ("bridge_lower", true));
        Rung("Raise only after traffic stopped and both barriers closed", "bridge_raise", ("bridge_request", false), ("traffic_stopped", false),
            ("barriers_closed", false), ("bridge_raised", true), ("traffic_release", true), ("barrier_open", true));
        Rung("Lower only with traffic stopped and both barriers closed", "bridge_lower", ("bridge_request", true), ("traffic_stopped", false),
            ("barriers_closed", false), ("bridge_home", true), ("traffic_release", true), ("barrier_open", true));
        Rung("Release traffic only at home after barriers fully open", "traffic_release", ("bridge_home", false), ("barriers_open", false),
            ("bridge_request", true), ("bridge_raise", true), ("bridge_lower", true), ("barrier_close", true));
        Rung("Red whenever release absent", "traffic_stop", ("traffic_release", true));
        d.WatchVariables.AddRange(d.Tags.Select(t => t.Name));
        return d;
    }

    private void VerifyDrawbridgeWorkflow(Action<bool, string> check)
    {
        void Check(bool ok, string name) => check(ok, "drawbridge_" + name);
        AddMigratedScene("lab-5-08-drawbridge-control", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!; var plant = runtime.DrawbridgePlant!;
        var deck = root.GetNode<Node3D>("training_accessory_3"); var pivot = deck.GetNode<Node3D>("BridgeMotionPivot");
        var limits = root.GetNode<Node3D>("training_accessory_5"); var cam = limits.GetNode<Node3D>("BridgeMotionPivot");
        var gates = new[] { root.GetNode<Node3D>("training_accessory_4"), root.GetNode<Node3D>("barrier_east") };
        var signals = new[] { root.GetNode<Node3D>("indicator_1"), root.GetNode<Node3D>("indicator_8") };
        MeshInstance3D Mesh(Node3D node, string name) => node.FindChild(name, true, false) as MeshInstance3D ?? throw new InvalidOperationException("Missing " + name);
        bool Lit(Node3D n, string color) => ReviewMeshes(n).Where(m => m.Name.ToString().StartsWith("LENS_" + color)).All(m => m.MaterialOverride is StandardMaterial3D { EmissionEnabled: true });
        bool On(string p) => runtime.Points[p] is true;
        Check(ReviewMeshes(deck).Any(m => m.Name == "CHANNEL_water") && ReviewMeshes(deck).Count(m => m.Name.ToString().StartsWith("APPROACH_")) > 10
            && ReviewBounds(Mesh(deck, "KIN_deck_surface")).Size.X > 3.99, "bascule_deck_two_approaches_and_channel_replace_scissor_shutter");
        Check(pivot.GlobalPosition.DistanceTo(new Vector3(-2, 1.4f, 0)) < .001f
            && cam.GlobalPosition.DistanceTo(new Vector3(-2, 1.4f, 2.95f)) < .001f, "deck_and_cam_share_hinge_axis");
        Check(ReviewBounds(Mesh(deck, "HINGE_shaft")).Intersects(ReviewBounds(Mesh(limits, "KIN_cam_disc"))), "shaft_connects_cam_disc");
        Check(ReviewMeshes(limits).Where(m => m.Name.ToString().StartsWith("LIMIT_")).All(m =>
            ReviewMeshes(deck).Where(f => f.Name.ToString().StartsWith("HINGE_pier_")).All(f => !ReviewBounds(m).Intersects(ReviewBounds(f)))),
            "limit_switch_plate_and_stand_clear_concrete_hinge_piers");
        Check(gates.All(g => ReviewMeshes(g.GetNode<Node3D>("BridgeMotionPivot")).Any(m => m.Name == "KIN_gate_arm"))
            && gates.All(g => Math.Abs(ReviewBounds(g).Position.Y - 1.4) < .005), "two_opposed_barriers_grounded_on_supported_platforms");
        Check(On("bridge_home") && On("barriers_closed") && !On("bridge_raised") && !On("barriers_open")
            && BridgeOutputs.All(p => !On(p)) && signals.All(n => !Lit(n, "red") && !Lit(n, "amber") && !Lit(n, "green")), "blank_exercise_supported_home_closed_and_commands_dark");
        Check(!ExecuteSelectedControllerAction("toggle-bridge_home"), "home_limit_is_not_an_operator_toggle");
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["traffic_stop"] = true, ["traffic_release"] = true });
        Check(signals.All(n => Lit(n, "red") && Lit(n, "green") && !Lit(n, "amber")), "renderer_exposes_conflicting_red_green_without_amber");
        runtime.ResetSimulation();
        var document = CreateDrawbridgeReference(); var compiled = LadderCompiler.Compile(document.BuildProgram());
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-drawbridge.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var invariant = true; var sweep = true; var limitsAligned = true;
            // Compare individual rendered pieces, excluding intended shaft,
            // landing-seat contact and the broad water envelope. This is a
            // sampled AABB screen, supplemented by native visual inspection.
            var fixedRails = ReviewMeshes(deck).Where(m => m.Name.ToString().StartsWith("APPROACH_rail_") || m.Name.ToString().StartsWith("APPROACH_post_")).ToArray();
            var movingRails = ReviewMeshes(pivot).Where(m => m.Name.ToString().StartsWith("KIN_deck_post_") || m.Name.ToString().StartsWith("KIN_deck_rail_")).ToArray();
            void Tick(int count = 1)
            {
                for (var i = 0; i < count; i++)
                {
                    _PhysicsProcess(.02);
                    invariant &= !(On("bridge_raise") && On("bridge_lower")) && !(On("barrier_open") && On("barrier_close"))
                        && (!On("traffic_release") || (plant.Home && plant.GatesOpen && !On("bridge_request")))
                        && (!(On("bridge_raise") || On("bridge_lower")) || (On("traffic_stopped") && plant.GatesClosed && !On("traffic_release")))
                        && (!On("barrier_open") || plant.Home)
                        && On("bridge_home") == plant.Home && On("bridge_raised") == plant.Raised
                        && On("barriers_closed") == plant.GatesClosed && On("barriers_open") == plant.GatesOpen
                        && signals.All(n => Lit(n, "red") == On("traffic_stop") && Lit(n, "green") == On("traffic_release") && !Lit(n, "amber"));
                    sweep &= movingRails.All(m => fixedRails.All(f => !ReviewBounds(m).Intersects(ReviewBounds(f))));
                    limitsAligned &= Math.Abs(pivot.Rotation.Z - Mathf.DegToRad((float)plant.BridgeDegrees)) < .0001
                        && pivot.Rotation.DistanceTo(cam.Rotation) < .0001
                        && gates.All(g => Math.Abs(g.GetNode<Node3D>("BridgeMotionPivot").Rotation.Z - Mathf.DegToRad((float)plant.BarrierDegrees)) < .0001);
                }
            }
            void Action(string p) { if (!ExecuteSelectedControllerAction("toggle-" + p)) throw new InvalidOperationException("Drawbridge action rejected: " + p); }
            void Until(Func<bool> predicate, int bound = 600) { for (var i = 0; i < bound && !predicate(); i++) Tick(); }
            RunActiveController(); Tick(); Check(plant.Home && !plant.GatesOpen && On("barrier_open") && !On("traffic_release"), "home_first_opens_gates_without_release");
            Tick(98); Check(!plant.GatesOpen && !On("traffic_release"), "barriers_before_two_seconds_keep_traffic_red");
            Tick(); Check(plant.GatesOpen && !On("traffic_release"), "two_seconds_open_feedback_before_next_scan_release");
            Tick(); Check(On("traffic_release") && !On("traffic_stop"), "traffic_green_only_after_open_limit");
            Action("bridge_request"); Tick(); Check(On("barrier_close") && On("traffic_stop") && !On("traffic_release") && plant.Home, "request_removes_green_and_closes_barriers_first");
            Until(() => plant.GatesClosed); Tick(50); Check(plant.Home && !On("bridge_raise"), "traffic_stopped_ack_required_after_closed_barriers");
            Action("traffic_stopped"); Tick(); Check(On("bridge_raise") && !plant.Home && !On("bridge_home") && plant.BridgeDegrees > 0, "raise_motion_removes_actual_home_feedback");
            Tick(99); Check(Math.Abs(plant.BridgeDegrees - 35) < .001 && !plant.Raised, "half_travel_is_actual_35_degree_pose");
            var angle = plant.BridgeDegrees; Action("traffic_stopped"); Tick(100);
            Check(Math.Abs(plant.BridgeDegrees - angle) < .001 && !On("bridge_raise"), "lost_stopped_permissive_holds_partial_deck");
            Action("traffic_stopped"); Tick(100); Check(plant.Raised && On("bridge_raised") && !On("bridge_home"), "four_seconds_accumulated_raise_reaches_70_degree_limit");
            Tick(50); Check(!On("bridge_raise") && plant.Raised && plant.GatesClosed && !On("traffic_release"), "held_open_request_holds_raised_closed_barriers_and_red");
            Check(Mesh(limits, "KIN_cam_lobe").GlobalPosition.DistanceTo(Mesh(limits, "LIMIT_roller_raised").GlobalPosition) < .086
                && Lit(limits, "amber") && !Lit(limits, "green"), "raised_cam_reaches_roller_and_feedback_lens");
            Action("bridge_request"); Tick(100); Check(plant.BridgeDegrees > 0 && plant.BridgeDegrees < 70 && !On("barrier_open"), "close_request_lowers_before_any_gate_open");
            angle = plant.BridgeDegrees; StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; Tick(100);
            Check(BridgeOutputs.All(p => !On(p)) && Math.Abs(plant.BridgeDegrees - angle) < .001 && _virtualController.Snapshot.ScanNumber == scan,
                "stop_clears_commands_freezes_pose_and_clock");
            RunActiveController(); Until(() => plant.Home); Check(!On("traffic_release") && plant.GatesClosed, "resume_lowering_reaches_home_before_gate_or_traffic_release");
            Check(Mesh(limits, "KIN_cam_lobe").GlobalPosition.DistanceTo(Mesh(limits, "LIMIT_roller_home").GlobalPosition) < .086
                && Lit(limits, "green") && !Lit(limits, "amber"), "home_cam_reaches_roller_and_feedback_lens");
            Until(() => On("traffic_release")); Check(plant.Home && plant.GatesOpen && !On("traffic_stop"), "return_cycle_opens_gates_then_releases_green");
            Check(invariant, "every_scan_interlocks_commands_and_projects_actual_limits_and_lamps");
            Check(sweep, "sampled_moving_rails_clear_fixed_approach_rails"); Check(limitsAligned, "every_scan_deck_cam_and_two_gate_pivots_follow_actual_angles");
            ResetActiveController(); Check(plant.Home && plant.GatesClosed && !On("bridge_request") && !On("traffic_stopped")
                && BridgeOutputs.All(p => !On(p)) && _virtualController.Snapshot.ScanNumber == 0
                && _virtualController.Snapshot.State == VirtualControllerState.Stopped, "reset_restores_home_closed_raw_false_outputs_off_zero_stopped");
        }
        finally { DisableVirtualController(); }
        var faulty = new DrawbridgePlantModel();
        faulty.Step(1, true, true, true, false, false, false);
        Check(faulty.Home && faulty.MotionInhibited, "conflicting_bridge_commands_inhibit_motion");
        faulty.Step(1, true, false, false, true, true, false);
        Check(faulty.GatesClosed && faulty.MotionInhibited, "conflicting_gate_commands_inhibit_motion");
        faulty.Step(1, false, true, false, false, false, false);
        Check(faulty.Home && faulty.MotionInhibited, "raw_missing_stopped_inhibits_forced_raise");
        faulty.Step(1, true, true, false, false, false, true);
        Check(faulty.Home && faulty.MotionInhibited, "traffic_release_conflict_inhibits_forced_raise");
        faulty.Step(1, true, true, false, false, false, false); var offHome = faulty.BridgeDegrees;
        faulty.Step(1, true, false, false, false, true, false);
        Check(!faulty.Home && faulty.GatesClosed && faulty.BridgeDegrees == offHome && faulty.MotionInhibited, "off_home_gate_open_command_inhibited");
        faulty.Reset(); faulty.Step(1, true, false, false, false, true, false);
        faulty.Step(1, true, true, false, true, false, false);
        Check(faulty.Home && faulty.GatesClosed && faulty.MotionInhibited, "closed_feedback_sample_prevents_raise_on_last_gate_closing_scan");
    }
}
