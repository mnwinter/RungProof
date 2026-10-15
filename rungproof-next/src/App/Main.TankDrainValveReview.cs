using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyTankDrainValve(string sceneId, Node3D root, Node3D outlet, Action<bool, string> check)
    {
        var valve = root.GetNodeOrNull<Node3D>(sceneId == "tank-high-low" ? "hl_drain_valve" : sceneId == "tank-radar" ? "radar_drain_valve" : "drain_valve");
        check(valve is not null, $"{sceneId}_drain_command_has_visible_valve");
        if (valve is null) return;
        MeshInstance3D Part(string name) => (MeshInstance3D)valve.FindChild(name, true, false);
        Vector3 Face(MeshInstance3D mesh, float sign) => mesh.GlobalTransform *
            (mesh.GetAabb().GetCenter() + Vector3.Up * sign * mesh.GetAabb().Size.Y / 2);
        var liner = Part("PROCESS_bore_liner");
        check(Face(liner, -1).DistanceTo(Face((MeshInstance3D)outlet.FindChild("FLANGE_1_72", true, false), 1)) < 0.001f
            && valve.FindChild("TANK_DRAIN_mating_flange", true, false) is MeshInstance3D
            && root.GetNode<Label3D>("TANK_drain_boundary").Position.DistanceTo(Face(liner, 1) + Vector3.Up * 0.40f) < 0.001f,
            $"{sceneId}_drain_valve_bore_meets_spool_and_boundary_is_at_new_end");
        var meshes = ReviewMeshes(valve).ToArray();
        var feet = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PIPE_foot", StringComparison.Ordinal)).ToArray();
        var posts = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PIPE_shoe_post", StringComparison.Ordinal)).ToArray();
        var saddles = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PIPE_saddle", StringComparison.Ordinal)).ToArray();
        var anchors = meshes.Where(mesh => mesh.Name.ToString().StartsWith("PIPE_anchor_washer", StringComparison.Ordinal)).ToArray();
        check(feet.Length == 2 && feet.All(foot => MathF.Abs(ReviewBounds(foot).Position.Y) < 0.001f)
            && posts.Length == 2 && posts.All(post => feet.Any(foot => ReviewBounds(post).Intersects(ReviewBounds(foot))))
            && saddles.Length == 4 && saddles.All(saddle => posts.Any(post => ReviewBounds(saddle).Intersects(ReviewBounds(post))))
            && anchors.Length == 8 && anchors.All(anchor => feet.Any(foot => ReviewBounds(anchor).Intersects(ReviewBounds(foot).Grow(0.003f)))),
            $"{sceneId}_drain_valve_shoes_grounded_posts_and_anchors_attached");
        var conflicts = root.GetChildren().OfType<Node3D>().Where(equipment => equipment != valve && equipment != outlet)
            .SelectMany(ReviewMeshes).SelectMany(other => meshes.Where(part => ReviewBounds(part).Intersects(ReviewBounds(other))
                && OrientedBoxesPenetrate(part, other)).Select(part => $"{part.Name}/{other.Name}")).ToArray();
        GD.Print($"TANK_DRAIN_CLEARANCE {sceneId} {string.Join(';', conflicts)}");
        check(conflicts.Length == 0, $"{sceneId}_drain_valve_clear_of_other_equipment");

        var controller = valve.GetNode<EquipmentMotionController>("PositionRotationController");
        var pointer = Part("POSITION_pointer");
        // The pointer's longest local box dimension defines its physical axis;
        // compare it to the installed bore, not just the adapter's percentage.
        Vector3 PointerAxis()
        {
            var size = pointer.GetAabb().Size;
            var axis = size.X > size.Y && size.X > size.Z ? Vector3.Right : size.Y > size.Z ? Vector3.Up : Vector3.Back;
            return (pointer.GlobalBasis * axis).Normalized();
        }
        var boreAxis = (Face(liner, 1) - Face(liner, -1)).Normalized();
        bool PointerMatches(bool open)
        {
            var dot = MathF.Abs(PointerAxis().Dot(boreAxis));
            return open ? dot > 0.999f : dot < 0.001f;
        }
        check(!controller.AutonomousPositionTravel && controller.PositionInputInverted && PointerMatches(false),
            $"{sceneId}_false_command_initializes_pointer_perpendicular_closed");
        var pointerClear = true;
        var fixedParts = meshes.Where(mesh => mesh != pointer && mesh.Name != "POSITION_indicator_disc" && mesh.Name != "VALVE_stem").ToArray();
        for (var sample = 0; sample <= 100; sample++)
        {
            controller.SetPositionNormalized(sample / 100.0f);
            pointerClear &= fixedParts.All(other => !ReviewBounds(pointer).Intersects(ReviewBounds(other)) || !OrientedBoxesPenetrate(pointer, other));
        }
        controller.SetPositionNormalized(0);
        check(pointerClear, $"{sceneId}_pointer_full_quarter_turn_sweep_clear_of_fixed_valve_parts");
        // Radar has REAL feedback, rather than these lessons' two level switches.
        // Its own reference must verify the REAL thresholds and controller path.
        if (sceneId == "tank-radar") return;

        // A real offline scan session cycles on the scene's actual switch
        // feedback. This catches missing/wrong bindings, inversion, creeping
        // motion and integration with Stop/Reset; no scene inputs are forged.
        var document = new LadderEditorDocument();
        document.ResetProject("qa-tank-drain", "Tank_Drain_QA", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        foreach (var point in new[] { "low_level_switch", "high_level_switch" }) document.AddTag(point, PlcVariableRole.Input, point);
        foreach (var point in new[] { "inlet_pump_run", "drain_valve_open" }) document.AddTag(point, PlcVariableRole.Output, point);
        document.AddTag("drain_phase", PlcVariableRole.Memory);
        document.AddRung("High level starts draining", "drain_phase").CoilMode = LadderCoilMode.Set;
        document.AddContact(0, 0, "high_level_switch", false);
        document.AddRung("Low level resumes filling", "drain_phase").CoilMode = LadderCoilMode.Reset;
        document.AddContact(1, 0, "low_level_switch", false);
        document.AddRung("Fill command", "inlet_pump_run"); document.AddContact(2, 0, "drain_phase", true);
        document.AddRung("Drain command", "drain_valve_open"); document.AddContact(3, 0, "drain_phase", false);
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var runtime = _sceneRuntime!;
            _virtualController!.Run();
            // The inspection clock must preserve the real scan -> plant path.
            // Gauge transforms are read after the accepted scan, not forged.
            var reviewWasEnabled = _visualSceneReview;
            _visualSceneReview = true;
            try
            {
                var tankId = sceneId == "tank-high-low" ? "water_tank_hl" : "process_tank";
                var liquid = (Node3D)root.GetNode<Node3D>(tankId).FindChild("KIN_liquid", true, false);
                var beforeLevel = Convert.ToDouble(runtime.Points["tank_level"]);
                var beforeTransform = liquid.Transform;
                var beforeScan = _virtualController.Snapshot.ScanNumber;
                SetGantryReviewClockHeld(true);
                _PhysicsProcess(.02);
                check(_virtualController.Snapshot.ScanNumber == beforeScan
                    && liquid.Transform == beforeTransform, $"{sceneId}_held_clock_freezes_scan_and_gauge");
                AdvanceGantryReviewClock(1);
                check(_virtualController.Snapshot.ScanNumber == beforeScan + 1
                    && Convert.ToDouble(runtime.Points["tank_level"]) > beforeLevel
                    && liquid.Transform != beforeTransform, $"{sceneId}_held_step_advances_one_scan_level_and_gauge");
            }
            finally { ReleaseGantryReviewClock(); _visualSceneReview = reviewWasEnabled; }
            var consistent = true; var transitions = 0; var previous = false;
            for (var tick = 0; tick < 1000 && transitions < 3; tick++)
            {
                _PhysicsProcess(0.04);
                controller._PhysicsProcess(0.04); // screen autonomous drift too
                var open = runtime.Points["drain_valve_open"] is true;
                consistent &= PointerMatches(open) && MathF.Abs(controller.InputPositionNormalized - (open ? 1 : 0)) < 0.001f;
                if (open != previous) { transitions++; previous = open; }
            }
            check(consistent && transitions == 3, $"{sceneId}_actual_ladder_scans_cycle_fill_drain_fill_drain_with_correct_pointer");
            CommitVirtualControllerSnapshot(_virtualController.Stop());
            check(runtime.Points["drain_valve_open"] is false && PointerMatches(false), $"{sceneId}_controller_stop_closes_drain_pointer");
            runtime.ResetSimulation(); CommitVirtualControllerSnapshot(_virtualController.Reset());
            check(runtime.Points["drain_valve_open"] is false && PointerMatches(false), $"{sceneId}_reset_restores_closed_drain_pointer");
        }
        finally { DisableVirtualController(); }
        GD.Print($"TANK_DRAIN_DATUM {sceneId} root={valve.Position} inlet={Face(liner, -1)} outlet={Face(liner, 1)}");
    }
}
