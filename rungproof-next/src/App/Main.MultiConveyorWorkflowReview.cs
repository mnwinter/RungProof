using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyMultiConveyorControllerWorkflow(Action<bool, string> check)
    {
        AddMigratedScene("lab-11-07-multi-conveyor-pallet-route", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!;
        var root = _sceneCompositionRoot!;
        var pallet = root.GetNode<Node3D>("route_pallet");
        var feed = root.GetNode<Node3D>("conveyor_0");
        var belt = ReviewBounds((MeshInstance3D)feed.FindChild("KIN_belt_surface", true, false));
        var tailX = ReviewBounds((MeshInstance3D)feed.FindChild("KIN_tail_drum", true, false)).GetCenter().X;
        var driveX = ReviewBounds((MeshInstance3D)feed.FindChild("KIN_drive_drum", true, false)).GetCenter().X;
        var runners = ReviewMeshes(pallet).Where(mesh => mesh.Name.ToString().StartsWith("PALLET_BOTTOM_", StringComparison.Ordinal)).ToArray();
        // The curved belt envelope is not the flat carrying span. Check the
        // three actual runner meshes against the drum axes independently.
        check(runners.Length == 3 && runners.All(mesh =>
        {
            var bounds = ReviewBounds(mesh);
            return MathF.Abs(bounds.Position.Y - belt.End.Y) < .001f
                && bounds.Position.X >= tailX && bounds.End.X <= driveX
                && bounds.Position.Z >= belt.Position.Z && bounds.End.Z <= belt.End.Z;
        }), "multi_conveyor_home_pallet_runners_seated_inside_flat_belt");
        GD.Print($"MULTI_CONVEYOR_HOME pallet={ReviewBounds(pallet)} belt={belt} flat={tailX}..{driveX}");
        VerifyMultiConveyorBearing(check, root, pallet);
        var document = new LadderEditorDocument();
        document.ResetProject("review-multi-conveyor", "Multi_Conveyor_QA", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = "lab-11-07-multi-conveyor-pallet-route";
        document.AddTag("common_stop_request", PlcVariableRole.Input, "common_stop_request");
        for (var zone = 1; zone <= 3; zone++)
        {
            var clear = $"zone_{zone}_clear";
            var run = $"zone_{zone}_run";
            document.AddTag(clear, PlcVariableRole.Input, clear);
            document.AddTag(run, PlcVariableRole.Output, run);
            var rung = zone - 1;
            document.AddRung($"Zone {zone}: fresh clear edge, common stop", run);
            // Sample the edge before permissives so releasing Stop with an
            // already-clear zone cannot generate a new start edge.
            document.InsertEdgeContact(rung, 0, 0, clear, LadderEdgeMode.Rising);
            document.AddContact(rung, 0, "common_stop_request", true);
            document.AddParallelBranch(rung);
            document.AddContact(rung, 1, clear, false);
            document.AddContact(rung, 1, "common_stop_request", true);
            document.AddContact(rung, 1, run, false);
        }
        var program = document.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        check(compiled.IsValid, "multi_conveyor_edge_qa_compiles");
        if (!compiled.IsValid) throw new InvalidOperationException("Multi-conveyor QA compile failed.");
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-multi-conveyor-native-qa.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(program);
        try
        {
            var crossedSensors = new HashSet<string>();
            var sensorPoints = new[] { "infeed_blocked", "handoff_1_blocked", "zone_2_blocked", "receiver_entry_blocked" };
            void Tick(int count)
            {
                for (var scan = 0; scan < count; scan++)
                {
                    _PhysicsProcess(.02);
                    foreach (var point in sensorPoints) if (runtime.Points[point] is true) crossedSensors.Add(point);
                }
            }
            bool Outputs(bool expected) => Enumerable.Range(1, 3).All(zone => Equals(runtime.Points[$"zone_{zone}_run"], expected));
            void Action(string id) { if (!ExecuteSelectedControllerAction(id)) throw new InvalidOperationException("Rejected action: " + id); }
            RunActiveController(); Tick(5);
            check(Outputs(false), "multi_conveyor_initial_outputs_off");
            for (var zone = 1; zone <= 3; zone++) { Action($"toggle-zone_{zone}_clear"); Tick(2); }
            check(Outputs(true), "multi_conveyor_fresh_edges_start_all_zones");
            Action("toggle-common-stop"); Tick(2);
            check(Outputs(false), "multi_conveyor_common_stop_drops_all_zones");
            Action("toggle-common-stop"); Tick(25);
            check(Outputs(false), "multi_conveyor_stop_release_does_not_restart_clear_zones");
            Action("toggle-zone_2_clear"); Tick(2); Action("toggle-zone_2_clear"); Tick(2);
            check(runtime.Points["zone_2_run"] is true && runtime.Points["zone_1_run"] is false && runtime.Points["zone_3_run"] is false,
                "multi_conveyor_only_fresh_zone_restarts");
            Action("toggle-zone_2_clear"); Tick(2);
            check(Outputs(false), "multi_conveyor_clear_loss_removes_zone_command");
            StopActiveController(); ResetActiveController();
            check(Outputs(false) && runtime.Points["common_stop_request"] is false,
                "multi_conveyor_reset_restores_inputs_and_outputs");
            RunActiveController(); Tick(2); Action("toggle-zone_1_clear"); Tick(100);
            check(pallet.Position.X > -8 && pallet.Position.X < -7.5,
                "multi_conveyor_zone_one_command_moves_load");
            StopActiveController(); var held = pallet.Position; Tick(50);
            check(pallet.Position == held, "multi_conveyor_stop_holds_actual_pallet_pose");
            ResetActiveController(); RunActiveController(); Tick(2); Action("toggle-zone_1_clear"); Tick(1000);
            var firstHandoff = pallet.Position;
            check(firstHandoff.X > -4 && firstHandoff.X < -3.5 && runtime.Points["zone_2_run"] is false,
                "multi_conveyor_stopped_zone_two_prevents_handoff");
            Tick(100); check(pallet.Position == firstHandoff, "multi_conveyor_first_handoff_holds");
            Action("toggle-zone_2_clear"); Tick(1000);
            var secondHandoff = pallet.Position;
            check(secondHandoff.X > 2.5 && secondHandoff.X < 3 && runtime.Points["zone_3_run"] is false,
                "multi_conveyor_stopped_zone_three_prevents_handoff");
            Action("toggle-zone_3_clear"); Tick(1500);
            check(MathF.Abs(pallet.Position.X - 12.3f) < .001f && runtime.Points["route_complete"] is true && Outputs(true),
                "multi_conveyor_arrives_on_receiver_without_overwriting_commands");
            check(sensorPoints.All(crossedSensors.Contains), "multi_conveyor_load_crosses_all_four_actual_optical_beams");
            var endpoint = pallet.Position; Tick(100);
            check(pallet.Position == endpoint, "multi_conveyor_endpoint_retains_pallet");
            StopActiveController(); ResetActiveController();
            check(MathF.Abs(pallet.Position.X + 9.3f) < .001f && runtime.Points["route_complete"] is false,
                "multi_conveyor_reset_restores_load_home");
            check(sensorPoints.All(point => runtime.Points[point] is false), "multi_conveyor_reset_restores_clear_geometric_sensors");
            _visualSceneReview = true;
            SetGantryReviewClockHeld(true); RunActiveController();
            var heldScans = _virtualController!.Snapshot.ScanNumber;
            _PhysicsProcess(5);
            check(GantryReviewClockHeld && _virtualController.Snapshot.ScanNumber == heldScans,
                "multi_conveyor_review_hold_freezes_controller_clock");
            StepGantryReviewClock();
            check(_virtualController.Snapshot.ScanNumber == heldScans + 25,
                "multi_conveyor_review_coarse_step_executes_25_scans");
            Action("toggle-zone_1_clear"); var beforeFine = pallet.Position.X;
            _visualReviewFocusId = "route_pallet";
            SetVisualReviewAngle(new Vector3(11, 7, 12), "verify-moving-focus");
            var cameraBeforeFine = _mainCamera!.Position;
            AdvanceGantryReviewClock(1);
            check(MathF.Abs(pallet.Position.X - beforeFine - .015f) < .0001f,
                "multi_conveyor_review_fine_step_moves_actual_pallet_one_scan");
            check((_mainCamera.Position - cameraBeforeFine).IsEqualApprox(new Vector3(.015f, 0, 0)),
                "multi_conveyor_review_step_refits_selected_moving_pallet");
            StopActiveController(); heldScans = _virtualController.Snapshot.ScanNumber;
            AdvanceGantryReviewClock(1);
            check(_virtualController.Snapshot.ScanNumber == heldScans,
                "multi_conveyor_review_stopped_clock_cannot_step");
            ResetActiveController(); ReleaseGantryReviewClock();
            _visualReviewFocusId = null;
        }
        finally { DisableVirtualController(); }
    }
}
