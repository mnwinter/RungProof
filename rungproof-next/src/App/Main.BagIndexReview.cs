using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditBagIndex;
    private void AuditBagIndex()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"BAG_INDEX_CHECK {name}={ok}"); }
        try { VerifyBagIndexWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"BAG_INDEX_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
    private static LadderEditorDocument CreateBagIndexReference()
    {
        var d = new LadderEditorDocument();
        d.ResetProject("review-bag-index", "Bag_Index_Reference", TimeSpan.FromMilliseconds(20));
        d.SourceSceneId = "lab-5-09-bag-indexing-conveyor";
        foreach (var p in new[] { "bag_at_entry", "bag_at_exit", "start_request", "pause_clear", "motion_inhibited", "travel_limited" })
            d.AddTag(p, PlcVariableRole.Input, p);
        foreach (var p in new[] { "bag_position", "belt_speed" }) d.AddTag(p, PlcVariableRole.Input, p, type: PlcVariableType.Real);
        foreach (var p in new[] { "conveyor_run", "conveyor_reverse" }) d.AddTag(p, PlcVariableRole.Output, p);
        d.AddTag("phase", PlcVariableRole.Memory, type: PlcVariableType.DInt);
        // Evaluate raw START in the existing phase before endpoint transitions.
        // A press during travel must not carry into the newly entered wait/idle.
        void Move(string label, int before, int after, params string[] permissives)
        {
            var r = d.Rungs.Count;
            d.AddNumericOperationRung(label, LadderNumericOperationKind.Move, after.ToString(), "", "phase");
            d.AddComparison(r, 0, "phase", LadderCompareOperator.Equal, before.ToString());
            foreach (var p in permissives) d.AddContact(r, 0, p, false);
        }
        Move("Fresh START at ENTRY selects forward", 0, 1, "bag_at_entry", "start_request");
        Move("Fresh START at EXIT after manual CLEAR selects return", 2, 3, "bag_at_exit", "pause_clear", "start_request");
        Move("Actual EXIT enters operator pause", 1, 2, "bag_at_exit");
        Move("Actual ENTRY ends the return", 3, 0, "bag_at_entry");
        d.AddRung("Run forward or permitted return; sensors stop the relevant leg", "conveyor_run");
        d.AddComparison(4, 0, "phase", LadderCompareOperator.Equal, "1"); d.AddContact(4, 0, "bag_at_exit", true);
        d.AddParallelBranch(4); d.AddComparison(4, 1, "phase", LadderCompareOperator.Equal, "3");
        d.AddContact(4, 1, "bag_at_entry", true); d.AddContact(4, 1, "pause_clear", false);
        d.AddRung("Reverse is a direction selection separate from run enable", "conveyor_reverse");
        d.AddComparison(5, 0, "phase", LadderCompareOperator.Equal, "3");
        d.WatchVariables.AddRange(d.Tags.Select(t => t.Name)); return d;
    }
    private void VerifyBagIndexWorkflow(Action<bool, string> check)
    {
        void Check(bool ok, string name) => check(ok, "bag_index_" + name);
        VerifyCartonStaticCableRoutes(Check, "lab-5-09-bag-indexing-conveyor", "installation");
        AddMigratedScene("lab-5-09-bag-indexing-conveyor", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!; var plant = runtime.BagIndexPlant!;
        var bag = root.GetNode<Node3D>("box_1"); var body = (MeshInstance3D)bag.FindChild("BAG_BODY", true, false);
        var belt = (MeshInstance3D)root.GetNode<Node3D>("conveyor_0").FindChild("KIN_belt_surface", true, false);
        var conveyor = root.GetNode<Node3D>("conveyor_0").FindChildren("*", "", true, false).OfType<ConveyorController>().Single();
        bool On(string p) => runtime.Points[p] is true;
        MeshInstance3D Lens(string id) => (MeshInstance3D)root.GetNode<Node3D>(id).FindChild("LENS_single", true, false);
        bool Lit(string id) => Lens(id).MaterialOverride is StandardMaterial3D { EmissionEnabled: true };
        Check(body.Mesh.GetFaces().Length > 0 && bag.FindChild("BAG_sealed_end*", true, false) is not null
            && bag.FindChild("KIN_pusher_plate", true, false) is null, "actual_sack_body_replaces_carton_and_copied_pusher");
        Check(root.FindChildren("BAG_BODY", "MeshInstance3D", true, false).Count == 1
            && root.GetNodeOrNull<Node3D>("training_accessory_5") is null
            && root.GetNodeOrNull<Node3D>("training_accessory_6") is null && root.GetNodeOrNull<Node3D>("training_accessory_7") is null,
            "one_product_with_integral_drive_and_real_operator_controls");
        var sensors = new[] { "photoeye_2", "photoeye_3" }.Select(id => root.GetNode<Node3D>(id)).ToArray();
        Check(sensors.All(n => {
            var tx = (MeshInstance3D)n.FindChild("TX_lens", true, false); var rx = (MeshInstance3D)n.FindChild("RX_lens", true, false);
            return Math.Abs(tx.GlobalPosition.Y - 1.1) < .001 && Math.Abs(rx.GlobalPosition.Y - 1.1) < .001
                && tx.GlobalPosition.Z > ReviewBounds(belt).End.Z && rx.GlobalPosition.Z < ReviewBounds(belt).Position.Z
                && Math.Abs(tx.GlobalPosition.X) < 2.1;
        }), "two_sensor_pairs_cross_the_supported_belt_at_product_height");
        Check(On("bag_at_entry") && !On("bag_at_exit") && !Lit("indicator_10") && !Lit("indicator_11")
            && !conveyor.IsPhysicsProcessing(), "initial_actual_entry_dark_commands_and_single_clock");
        Check(!ExecuteSelectedControllerAction("toggle-bag_at_entry") && !ExecuteSelectedControllerAction("toggle-bag_at_exit"),
            "manual_fake_sensor_toggles_removed");
        Check(root.GetNode<Node3D>("switch_4").GetNode<Label3D>("OperatorFaceLabel").Text == "START"
            && root.GetNode<Node3D>("switch_9").FindChild("KIN_selector_handle", true, false) is not null,
            "physical_start_and_manual_clear_selector");
        var document = CreateBagIndexReference(); var compiled = LadderCompiler.Compile(document.BuildProgram());
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-bag-index.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            var supported = true; var separated = true; var projected = true; var sampleCount = 0;
            var fixedSolids = root.GetChildren().OfType<Node3D>().Where(n => n != bag).SelectMany(ReviewMeshes)
                .Where(m => !m.Name.ToString().StartsWith("KIN_beam")).ToArray();
            void Tick(int count = 1)
            {
                for (var i = 0; i < count; i++)
                {
                    _PhysicsProcess(.02); sampleCount++;
                    var b = ReviewBounds(bag); var carrying = ReviewBounds(belt);
                    supported &= Math.Abs(b.Position.Y - carrying.End.Y) < .001 && b.Position.X >= carrying.Position.X
                        && b.End.X <= carrying.End.X && b.Position.Z >= carrying.Position.Z && b.End.Z <= carrying.End.Z;
                    separated &= ReviewMeshes(bag).All(m => fixedSolids.All(f => {
                        var overlap = ReviewBounds(m).Intersection(ReviewBounds(f)).Size;
                        return overlap.X <= .005 || overlap.Y <= .005 || overlap.Z <= .005 || !OrientedBoxesPenetrate(m, f);
                    }));
                    projected &= Math.Abs(plant.Position - bag.Position.X) < .0001
                        && Math.Abs(plant.Speed - conveyor.ActualSpeedMps) < .0001
                        && Lit("indicator_10") == On("conveyor_run") && Lit("indicator_11") == On("conveyor_reverse")
                        && !On("motion_inhibited") && !On("travel_limited");
                }
            }
            void Action(string p) { if (!ExecuteSelectedControllerAction(p)) throw new InvalidOperationException("Bag action rejected: " + p); }
            void Start() => Action("pulse-start_request");
            double Phase() => _virtualController!.Snapshot.NumericVariables["phase"];
            void Until(Func<bool> predicate, int bound = 1100) { for (var i = 0; i < bound && !predicate(); i++) Tick(); }
            RunActiveController(); Tick(10); Check(plant.Position == -2 && !On("conveyor_run") && Phase() == 0, "run_alone_does_not_start");
            Start(); Tick(100); Check(Math.Abs(plant.Position + 1) < .0001 && On("conveyor_run") && !On("conveyor_reverse")
                && !On("bag_at_entry") && !On("bag_at_exit"), "forward_two_seconds_moves_one_metre_and_clears_entry");
            var held = plant.Position; StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber;
            Tick(100); Check(plant.Position == held && plant.Speed == 0 && conveyor.ActualSpeedMps == 0 && !On("conveyor_run")
                && !On("conveyor_reverse") && _virtualController.Snapshot.ScanNumber == scan, "stop_holds_mid_forward_pose_and_clears_command_speed");
            RunActiveController(); Tick(); Check(plant.Position > held && Phase() == 1, "run_resumes_retained_forward_phase");
            Start(); Tick(2); Check(Phase() == 1 && !On("conveyor_reverse"), "active_start_does_not_queue_return");
            Until(() => Phase() == 2); Check(On("bag_at_exit") && !On("bag_at_entry") && !On("conveyor_run") && !On("conveyor_reverse"),
                "actual_exit_stops_forward_and_waits_for_operator");
            held = plant.Position; Tick(200); Check(plant.Position == held && Phase() == 2, "exit_wait_has_no_automatic_reversal");
            Start(); Tick(2); Check(Phase() == 2 && plant.Position == held, "start_without_clear_discarded");
            Action("toggle-pause_clear"); Tick(50); Check(Phase() == 2 && plant.Position == held, "clear_alone_does_not_resume_or_replay_start");
            Start(); Tick(100); Check(Phase() == 3 && plant.Position < held && On("conveyor_run") && On("conveyor_reverse"),
                "fresh_start_after_clear_runs_reverse_with_both_enable_and_direction_true");
            held = plant.Position; Action("toggle-pause_clear"); Tick(100);
            Check(plant.Position == held && !On("conveyor_run") && On("conveyor_reverse"), "lost_clear_holds_return_and_retains_direction");
            Action("toggle-pause_clear"); Tick(50); held = plant.Position; StopActiveController(); Tick(20);
            Check(plant.Position == held && plant.Speed == 0 && !On("conveyor_run") && !On("conveyor_reverse"), "stop_holds_mid_return_and_clears_both_outputs");
            RunActiveController(); Tick(); Check(plant.Position < held && Phase() == 3, "run_resumes_return_with_clear");
            Until(() => Phase() == 0); Check(On("bag_at_entry") && !On("bag_at_exit") && !On("conveyor_run") && !On("conveyor_reverse"),
                "actual_entry_stops_return_before_next_cycle");
            held = plant.Position; Tick(100); Check(plant.Position == held, "returned_bag_waits_for_fresh_start");
            Start(); Tick(5); Check(plant.Position > held && Phase() == 1, "next_outbound_cycle_uses_retained_supported_bag");
            Check(supported, "every_sample_keeps_bag_bottom_on_belt_and_footprint_inside");
            Check(separated, "sampled_bag_clear_of_fixed_mesh_solids"); Check(projected, "every_sample_projects_actual_pose_speed_command_lamps_and_no_fault");
            GD.Print($"BAG_INDEX_SAMPLES {sampleCount}");
            ResetActiveController(); Check(plant.Position == -2 && plant.Speed == 0 && On("bag_at_entry") && !On("bag_at_exit")
                && !On("start_request") && !On("pause_clear") && !On("conveyor_run") && !On("conveyor_reverse")
                && _virtualController.Snapshot.ScanNumber == 0, "reset_restores_supported_entry_raw_false_outputs_off_zero_scan");
            Start(); StopActiveController(); RunActiveController(); Tick(10);
            Check(!On("start_request") && !On("conveyor_run") && plant.Position == -2, "pending_start_discarded_by_stop");
        }
        finally { DisableVirtualController(); }
        // Advance an explicitly enabled offline plant clock for malformed PLC
        // image probes. Disabling the controller above correctly stopped it.
        runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_run"] = true, ["conveyor_reverse"] = true });
        runtime.AdvanceSimulation(.5);
        Check(plant.Position == -2 && On("motion_inhibited") && Lit("indicator_10") && Lit("indicator_11"),
            "forced_reverse_without_clear_inhibits_motion_but_exposes_command_image");
        runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["conveyor_run"] = true });
        runtime.AdvanceSimulation(30);
        Check(plant.Position == 2.6 && On("travel_limited") && On("conveyor_run") && !On("conveyor_reverse")
            && Math.Abs(ReviewBounds(bag).Position.Y - ReviewBounds(belt).End.Y) < .001,
            "missed_exit_limits_supported_travel_without_manufacturing_plc_stop");
        runtime.ResetSimulation();
        runtime.SetControllerPlaybackRunning(false);
    }
}
