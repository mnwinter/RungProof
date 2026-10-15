using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditRepeatCycle;
    private void AuditRepeatCycle()
    {
        var failures = 0;
        void Check(bool value, string label)
        { if (!value) failures++; GD.Print($"REPEAT_CYCLE_CHECK {label}={value}"); }
        try { VerifyRepeatCycleWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"REPEAT_CYCLE_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private void VerifyRepeatCycleWorkflow(Action<bool, string> check)
    {
        const string sceneId = "lab-4-03-repeat-cycle-counter";
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var machine = root.GetNode<Node3D>("machine_0");
        var head = machine.GetNode<Node3D>("KIN_repeat_cycle_head");
        var spindle = (Node3D)machine.FindChild("KIN_spindle", true, false);
        var initialHead = head.Transform;
        var stock = (MeshInstance3D)machine.FindChild("WORK_stock", true, false);
        var shoe = (MeshInstance3D)machine.FindChild("WORK_stock_bearing_shoe", true, false);
        check(MathF.Abs(ReviewBounds(stock).Position.Y - ReviewBounds(shoe).End.Y) < .001f,
            "repeat_stock_bears_on_installed_vise_shoe");
        var fixedParts = ReviewMeshes(machine).Where(mesh => !head.IsAncestorOf(mesh)).ToArray();
        var headParts = ReviewMeshes(head);
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= .001f || overlap.Y <= .001f || overlap.Z <= .001f || !OrientedBoxesPenetrate(a, b);
        }
        bool GuideMount(MeshInstance3D moving, MeshInstance3D part) =>
            (moving.Name.ToString().StartsWith("AXIS_Z_truck_") && part.Name.ToString().StartsWith("AXIS_Z_rail_"))
            || (moving.Name == "AXIS_Z_carriage" && part.Name == "AXIS_Z_ballscrew_cover");
        var movingClear = true;
        var samples = 0;
        var badPairs = new HashSet<string>();
        void GeometrySample()
        {
            samples++;
            foreach (var a in headParts)
                foreach (var b in fixedParts)
                    if (!GuideMount(a, b) && !Clear(a, b))
                    {
                        movingClear = false;
                        if (badPairs.Add($"{a.Name}/{b.Name}"))
                            GD.Print($"REPEAT_CYCLE_FIRST_OVERLAP {a.Name} {ReviewBounds(a)} / {b.Name} {ReviewBounds(b)} head={runtime.Points["head_position"]}");
                    }
            var actual = head.Position.Y - (initialHead.Origin.Y - (float)RepeatCyclePlantModel.TravelM);
            var expected = Convert.ToSingle(runtime.Points["head_position"]) / 100 * (float)RepeatCyclePlantModel.TravelM;
            movingClear &= MathF.Abs(actual - expected) < .0001f;
            movingClear &= Equals(runtime.Points["machine_home"], expected >= (float)RepeatCyclePlantModel.TravelM - .000001f);
        }
        var equipment = root.GetChildren().OfType<Node3D>().ToArray();
        check(equipment.All(a => equipment.Where(b => b != a).All(b => ReviewMeshes(a).All(am => ReviewMeshes(b).All(bm => Clear(am, bm))))),
            "repeat_operator_stations_and_displays_clear_machine_and_each_other");
        check(root.GetNode<Node3D>("switch_3").GetNode<Label3D>("OperatorFaceLabel").Text == "ENABLE"
            && runtime.Points["machine_home"] is true && runtime.Points["cycle_done"] is false
            && runtime.Points["machine_enabled"] is false && !runtime.Points.ContainsKey("cycle_count_complete"),
            "repeat_initial_home_and_actual_feedback_replace_manual_count_complete");
        var document = new LadderEditorDocument();
        document.ResetProject("review-repeat-cycle", "Repeat_Cycle_Reference", TimeSpan.FromMilliseconds(20));
        document.SourceSceneId = sceneId;
        foreach (var name in new[] { "cycle_request", "machine_enabled", "machine_home", "machine_busy", "cycle_done" })
            document.AddTag(name, PlcVariableRole.Input, name);
        document.AddTag("head_position", PlcVariableRole.Input, "head_position", PlcVariableType.Real);
        foreach (var name in new[] { "cycle_active", "cycle_complete" }) document.AddTag(name, PlcVariableRole.Output, name);
        document.AddTag("batch_count", PlcVariableRole.Output, "batch_count", PlcVariableType.DInt);
        document.AddTag("completed_cycles", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("batch_active", PlcVariableRole.Memory);
        document.AddTag("publish_count", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.AddRange(["cycle_request", "machine_enabled", "machine_home", "machine_busy", "cycle_done", "head_position", "completed_cycles", "batch_count", "cycle_active", "cycle_complete"]);
        document.AddCounterRung("Count actual completed returns", "completed_cycles", 3);
        document.AddContact(0, 0, "cycle_done", false);
        document.AddRung("Seal batch only while enabled and below target", "batch_active");
        document.AddContact(1, 0, "cycle_request", false);
        document.AddContact(1, 0, "machine_enabled", false);
        document.AddContact(1, 0, "completed_cycles.DN", true);
        document.AddParallelBranch(1);
        document.AddContact(1, 1, "batch_active", false);
        document.AddContact(1, 1, "machine_enabled", false);
        document.AddContact(1, 1, "completed_cycles.DN", true);
        document.AddRung("One low command acknowledges each actual completion", "cycle_active");
        document.AddContact(2, 0, "batch_active", false);
        document.AddContact(2, 0, "cycle_done", true);
        document.AddRung("Batch complete follows PLC counter", "cycle_complete");
        document.AddContact(3, 0, "completed_cycles.DN", false);
        document.AddNumericOperationRung("Publish PLC accumulated count", LadderNumericOperationKind.Move,
            "completed_cycles.ACC", string.Empty, "batch_count");
        document.AddContact(4, 0, "publish_count", false);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-repeat-cycle.rpproj.json"),
            LadderEditorProjectJson.Save(document));
        var program = document.BuildProgram();
        var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Repeat-cycle reference does not compile: " + string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        try
        {
            long Count() => _virtualController!.Snapshot.Counters.GetValueOrDefault("completed_cycles")?.Accumulated ?? 0;
            void Tick() { _PhysicsProcess(.02); GeometrySample(); }
            void Action(string name) => ExecuteSelectedControllerAction(name);
            RunActiveController(); Action("pulse-cycle_request"); Tick(); Tick();
            check(Count() == 0 && head.Transform.IsEqualApprox(initialHead) && runtime.Points["cycle_active"] is false,
                "repeat_request_without_enable_cannot_move_or_count");
            Action("toggle-machine_enabled"); Tick();
            check(runtime.Points["cycle_active"] is false, "repeat_enable_alone_does_not_start_batch");
            Action("pulse-cycle_request");
            for (var i = 0; i < 25; i++) Tick();
            check(runtime.Points["cycle_request"] is false && runtime.Points["machine_busy"] is true
                && Math.Abs(Convert.ToDouble(runtime.Points["head_position"]) - 50) < .001 && Count() == 0,
                "repeat_start_drives_actual_half_feed_with_no_premature_count");
            var pausedHead = head.Transform; var pausedSpin = spindle.Transform;
            StopActiveController(); var pausedScan = _virtualController!.Snapshot.ScanNumber;
            _PhysicsProcess(1); runtime.AdvanceSimulation(1);
            check(head.Transform.IsEqualApprox(pausedHead) && spindle.Transform.IsEqualApprox(pausedSpin)
                && _virtualController.Snapshot.ScanNumber == pausedScan && runtime.Points["cycle_active"] is false,
                "repeat_stop_holds_head_rotation_scan_and_count");
            RunActiveController(); for (var i = 0; i < 10; i++) Tick();
            check(Convert.ToDouble(runtime.Points["head_position"]) < 50 && Count() == 0,
                "repeat_run_resumes_partial_feed_without_counting_it");
            Action("toggle-machine_enabled"); Tick(); pausedHead = head.Transform; pausedSpin = spindle.Transform;
            for (var i = 0; i < 20; i++) Tick();
            check(head.Transform.IsEqualApprox(pausedHead) && spindle.Transform.IsEqualApprox(pausedSpin)
                && runtime.Points["cycle_active"] is false, "repeat_enable_loss_holds_partial_cycle_and_clears_batch_latch");
            Action("toggle-machine_enabled"); for (var i = 0; i < 5; i++) Tick();
            check(head.Transform.IsEqualApprox(pausedHead), "repeat_enable_restore_requires_fresh_start");
            Action("pulse-cycle_request");
            var ticks = 0; var priorCount = Count(); var actualReturns = 0; var previousDone = false; var countMatches = true;
            while (runtime.Points["cycle_complete"] is not true && ticks++ < 500)
            {
                var sampledDone = runtime.Points["cycle_done"] is true;
                Tick();
                var done = runtime.Points["cycle_done"] is true;
                if (done && !previousDone) actualReturns++;
                if (Count() != priorCount)
                    countMatches &= Count() == priorCount + 1 && sampledDone && runtime.Points["machine_home"] is true;
                previousDone = done; priorCount = Count();
            }
            check(ticks < 500 && Count() == 3 && actualReturns == 3 && countMatches
                && Equals(runtime.Points["batch_count"], 3L) && runtime.Points["cycle_active"] is false,
                "repeat_three_actual_returns_give_exactly_three_plc_counts_and_stop_batch");
            var finished = head.Transform;
            for (var i = 0; i < 200; i++) Tick();
            Action("pulse-cycle_request"); for (var i = 0; i < 10; i++) Tick();
            check(Count() == 3 && head.Transform.IsEqualApprox(finished) && runtime.Points["cycle_complete"] is true,
                "repeat_idle_or_new_start_cannot_add_fourth_cycle_before_reset");
            StopActiveController();
            check(Count() == 3 && Equals(runtime.Points["batch_count"], 0L) && runtime.Points["cycle_complete"] is false,
                "repeat_stop_clears_output_image_but_retains_counter_memory");
            check(_gantryReviewClockLabel is null || _gantryReviewClockLabel.Text.Contains("count 0 |"),
                "repeat_stop_refreshes_inspection_label_after_output_projection");
            RunActiveController(); Tick();
            check(Equals(runtime.Points["batch_count"], 3L) && runtime.Points["cycle_complete"] is true,
                "repeat_run_republishes_completed_batch_without_motion");
            ResetActiveController();
            check(Count() == 0 && runtime.Points["machine_enabled"] is false && runtime.Points["machine_home"] is true
                && runtime.Points["machine_busy"] is false && runtime.Points["cycle_done"] is false
                && runtime.Points["cycle_complete"] is false && Equals(runtime.Points["batch_count"], 0L)
                && head.Transform.IsEqualApprox(initialHead) && _virtualController.Snapshot.ScanNumber == 0,
                "repeat_application_reset_restores_disabled_home_and_zero_count");
            foreach (var pair in badPairs) GD.Print($"REPEAT_CYCLE_CLEARANCE_CANDIDATE {pair}");
            check(movingClear, "repeat_full_route_sampled_head_clearance_and_world_position_feedback");
            GD.Print($"REPEAT_CYCLE_ROUTE_SAMPLES {samples}");
            if (CanReviewGantryClock)
            {
                SetGantryReviewClockHeld(true); RunActiveController();
                Action("toggle-machine_enabled"); Action("pulse-cycle_request");
                _PhysicsProcess(1);
                check(_virtualController.Snapshot.ScanNumber == 0 && head.Transform.IsEqualApprox(initialHead),
                    "repeat_inspection_hold_freezes_both_accepted_scan_and_head");
                AdvanceGantryReviewClock(25);
                check(_virtualController.Snapshot.ScanNumber == 25 && Math.Abs(Convert.ToDouble(runtime.Points["head_position"]) - 50) < .001,
                    "repeat_inspection_step_advances_twenty_five_actual_scans_once");
                StopActiveController(); pausedHead = head.Transform; AdvanceGantryReviewClock(25);
                check(_virtualController.Snapshot.ScanNumber == 25 && head.Transform.IsEqualApprox(pausedHead),
                    "repeat_inspection_step_cannot_advance_stopped_controller");
                ReleaseGantryReviewClock(); ResetActiveController();
            }
        }
        finally { DisableVirtualController(); }
        var plant = new RepeatCyclePlantModel(); plant.Step(5, true); var angle = plant.SpindleRadians;
        plant.Step(5, true);
        check(plant.Done && plant.Home && plant.SpindleRadians == angle,
            "repeat_held_high_plant_command_cannot_retrigger_completed_stroke");
        plant.Step(.02, false); plant.Step(.5, true);
        check(plant.Busy && !plant.Done && Math.Abs(plant.HomeFraction - .5) < 1e-8,
            "repeat_sampled_low_acknowledgement_allows_one_new_stroke");
        var rejects = true;
        foreach (var invalid in new[] { -.01, double.NaN, double.PositiveInfinity })
        { try { plant.Step(invalid, true); rejects = false; } catch (ArgumentException) { } }
        check(rejects, "repeat_plant_rejects_invalid_time");
    }
}
