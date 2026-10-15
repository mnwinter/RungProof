using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditPedestrianCrossing;
    private static readonly string[] CrossingOutputs = ["vehicle_stop", "vehicle_amber", "vehicle_ready", "pedestrian_stop", "pedestrian_walk", "sequence_running"];

    private void AuditPedestrianCrossing()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"CROSSING_CHECK {name}={ok}"); }
        try { VerifyPedestrianCrossingWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"PEDESTRIAN_CROSSING_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }

    private static LadderEditorDocument CreatePedestrianCrossingReference()
    {
        // Explicitly opened original training reference. These short presets
        // are not public-road clearance calculations or a standards implementation.
        var d = new LadderEditorDocument();
        d.ResetProject("review-pedestrian-crossing", "Pedestrian_Crossing_Reference", TimeSpan.FromMilliseconds(20));
        d.SourceSceneId = "lab-5-07-pedestrian-crossing";
        foreach (var input in new[] { "crossing_request", "clear_to_finish" }) d.AddTag(input, PlcVariableRole.Input, input);
        foreach (var output in CrossingOutputs) d.AddTag(output, PlcVariableRole.Output, output);
        d.AddTag("request_edge", PlcVariableRole.Memory);
        d.AddTag("crossing_phase", PlcVariableRole.Memory, type: PlcVariableType.DInt);
        // Unbound PLC output records that this controller is scanning. Stop
        // clears outputs; hence first Run always overrides a retained WALK phase.
        d.AddTag("reference_running", PlcVariableRole.Output);
        d.AddNumericOperationRung("First Run overrides retained phase with startup clearance", LadderNumericOperationKind.Move, "5", "", "crossing_phase");
        d.AddContact(0, 0, "reference_running", true);
        d.AddRung("Sample request edge even when unavailable; no deferred request", "request_edge");
        d.InsertEdgeContact(1, 0, 0, "crossing_request", LadderEdgeMode.Rising);
        (int Phase, string Timer, int Ms)[] stages = [(1, "amber_timer", 1000), (2, "all_red_timer", 500),
            (3, "walk_timer", 2000), (4, "clearance_timer", 3000), (5, "startup_timer", 3000) ];
        // Evaluate every timer before changing phase. A new phase starts timing
        // on the following accepted scan; no stale done bit can skip a phase.
        foreach (var (phase, timer, ms) in stages)
        {
            d.AddTag(timer, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var r = d.Rungs.Count; d.AddTimerRung("Time phase " + phase, timer, TimeSpan.FromMilliseconds(ms));
            d.AddComparison(r, 0, "crossing_phase", LadderCompareOperator.Equal, phase.ToString());
        }
        foreach (var (phase, timer, _) in stages.Reverse())
        {
            var destination = phase >= 4 ? 0 : phase + 1;
            var r = d.Rungs.Count;
            d.AddNumericOperationRung("Complete phase " + phase, LadderNumericOperationKind.Move, destination.ToString(), "", "crossing_phase");
            d.AddComparison(r, 0, "crossing_phase", LadderCompareOperator.Equal, phase.ToString());
            d.AddContact(r, 0, timer + ".Q", false);
            if (phase >= 4) d.AddContact(r, 0, "clear_to_finish", false);
        }
        var request = d.Rungs.Count;
        d.AddNumericOperationRung("Accept fresh idle request only with PATH CLEAR", LadderNumericOperationKind.Move, "1", "", "crossing_phase");
        d.AddComparison(request, 0, "crossing_phase", LadderCompareOperator.Equal, "0");
        d.AddContact(request, 0, "clear_to_finish", false); d.AddContact(request, 0, "request_edge", false);
        var green = d.Rungs.Count; d.AddRung("Vehicle green only idle and clear", "vehicle_ready");
        d.AddComparison(green, 0, "crossing_phase", LadderCompareOperator.Equal, "0"); d.AddContact(green, 0, "clear_to_finish", false);
        var amber = d.Rungs.Count; d.AddRung("Vehicle amber only phase one", "vehicle_amber");
        d.AddComparison(amber, 0, "crossing_phase", LadderCompareOperator.Equal, "1");
        var red = d.Rungs.Count; d.AddRung("Vehicle red whenever green and amber absent", "vehicle_stop");
        d.AddContact(red, 0, "vehicle_ready", true); d.AddContact(red, 0, "vehicle_amber", true);
        var walk = d.Rungs.Count; d.AddRung("White WALK only phase three", "pedestrian_walk");
        d.AddComparison(walk, 0, "crossing_phase", LadderCompareOperator.Equal, "3");
        var hand = d.Rungs.Count; d.AddRung("Steady orange hand whenever WALK absent", "pedestrian_stop"); d.AddContact(hand, 0, "pedestrian_walk", true);
        var active = d.Rungs.Count; d.AddRung("PLC sequence status, never an operator toggle", "sequence_running");
        d.AddComparison(active, 0, "crossing_phase", LadderCompareOperator.GreaterThan, "0");
        var running = d.Rungs.Count; d.AddRung("Mark controller scanning until Stop clears outputs", "reference_running");
        d.AddComparison(running, 0, "crossing_phase", LadderCompareOperator.GreaterOrEqual, "0");
        d.WatchVariables.AddRange(d.Tags.Select(t => t.Name));
        return d;
    }

    private void VerifyPedestrianCrossingWorkflow(Action<bool, string> check)
    {
        void Check(bool ok, string name) => check(ok, "crossing_" + name);
        AddMigratedScene("lab-5-07-pedestrian-crossing", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!;
        var road = root.GetNode<Node3D>("training_accessory_5");
        var roadBounds = ReviewBounds(road);
        var nodes = root.GetChildren().OfType<Node3D>().Where(n => n != road && ReviewMeshes(n).Length > 0).ToArray();
        Check(roadBounds.Size.X > 9.9 && roadBounds.Size.Z > 9.1 && roadBounds.Size.Y < .16
            && road.FindChild("ROAD_horizontal_surface", true, false) is MeshInstance3D, "horizontal_road_and_two_sidewalks_replace_vertical_wall");
        Check(ReviewMeshes(road).Count(m => m.Name.ToString().StartsWith("CROSSWALK_mark_")) == 9, "actual_crosswalk_marks_present");
        Check(nodes.All(n => Math.Abs(ReviewBounds(n).Position.Y - (n.Name == "switch_7" ? 0 : .15)) < .005), "heads_and_request_grounded_on_sidewalk_clear_selector_on_floor");
        var separated = true;
        for (var a = 0; a < nodes.Length; a++) for (var b = a + 1; b < nodes.Length; b++)
            separated &= !ReviewBounds(nodes[a]).Intersects(ReviewBounds(nodes[b]));
        Check(separated, "independent_equipment_envelopes_separated_excluding_intended_road_support");
        var vehicles = new[] { root.GetNode<Node3D>("training_accessory_3"), root.GetNode<Node3D>("traffic_west") };
        var pedestrians = new[] { root.GetNode<Node3D>("training_accessory_4"), root.GetNode<Node3D>("pedestrian_south") };
        bool Lit(Node3D node, string lens) => (node.FindChild(lens, true, false) as MeshInstance3D)?.MaterialOverride is StandardMaterial3D { EmissionEnabled: true };
        Check(vehicles.All(n => new[] { "red", "amber", "green" }.All(c => n.FindChild("LENS_" + c, true, false) is MeshInstance3D))
            && pedestrians.All(n => n.FindChild("LENS_orange_hand", true, false) is MeshInstance3D && n.FindChild("LENS_white_walk", true, false) is MeshInstance3D), "two_vehicle_and_two_symbol_signal_heads");
        Check(CrossingOutputs.All(p => runtime.Points[p] is false) && vehicles.All(n => !Lit(n, "LENS_red"))
            && pedestrians.All(n => !Lit(n, "LENS_white_walk")), "blank_exercise_initial_commands_and_lenses_dark");
        runtime.CommitVirtualControllerOutputs(CrossingOutputs.ToDictionary(p => p, _ => true));
        Check(vehicles.All(n => new[] { "red", "amber", "green" }.All(c => Lit(n, "LENS_" + c)))
            && pedestrians.All(n => Lit(n, "LENS_orange_hand") && Lit(n, "LENS_white_walk")), "renderer_exposes_conflicting_plc_commands");
        runtime.CommitVirtualControllerOutputs(new Dictionary<string, bool> { ["pedestrian_walk"] = false });
        Check(pedestrians.All(n => !Lit(n, "LENS_white_walk") && Lit(n, "LENS_orange_hand")), "clearing_walk_preserves_hand_channel");
        runtime.ResetSimulation();
        var document = CreatePedestrianCrossingReference();
        var compiled = LadderCompiler.Compile(document.BuildProgram());
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-pedestrian-crossing.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(document.BuildProgram());
        try
        {
            bool On(string p) => runtime.Points[p] is true;
            double Phase() => _virtualController!.Snapshot.NumericVariables["crossing_phase"];
            bool State(int phase, string vehicle, bool walk) => Phase() == phase && On("vehicle_" + vehicle)
                && new[] { "stop", "amber", "ready" }.Count(c => On("vehicle_" + c)) == 1
                && On("pedestrian_walk") == walk && On("pedestrian_stop") != walk
                && vehicles.All(n => Lit(n, "LENS_red") == On("vehicle_stop") && Lit(n, "LENS_amber") == On("vehicle_amber") && Lit(n, "LENS_green") == On("vehicle_ready"))
                && pedestrians.All(n => Lit(n, "LENS_white_walk") == walk && Lit(n, "LENS_orange_hand") != walk);
            var invariant = true;
            void Tick(int count = 1) { for (var i = 0; i < count; i++) { _PhysicsProcess(.02);
                invariant &= !(On("vehicle_ready") && On("pedestrian_walk")) && (!On("pedestrian_walk") || On("vehicle_stop")); } }
            void Action(string p) { if (!ExecuteSelectedControllerAction("toggle-" + p)) throw new InvalidOperationException("Crossing action rejected: " + p); }
            void Until(int phase) { for (var i = 0; i < 500 && Phase() != phase; i++) Tick(); }
            RunActiveController(); Tick(2); Check(State(5, "stop", false), "run_starts_red_hand_clearance");
            Action("crossing_request"); Tick(2); Action("clear_to_finish"); Tick(145);
            Check(State(5, "stop", false), "startup_before_three_seconds_and_early_request_discarded");
            Tick(); Check(State(0, "ready", false), "startup_at_scan_150_releases_only_green_with_path_clear");
            Tick(100); Check(State(0, "ready", false), "idle_has_no_automatic_crossing");
            Action("clear_to_finish"); Action("crossing_request"); Tick(2); Check(State(0, "stop", false), "blocked_request_discarded_and_green_removed");
            Action("clear_to_finish"); Tick(2); Check(State(0, "ready", false), "clearing_path_does_not_execute_discarded_request");
            Action("crossing_request"); Tick(); Check(State(1, "amber", false), "fresh_request_enters_amber_without_walk");
            Tick(49); Check(State(1, "amber", false), "amber_before_one_second");
            Tick(); Check(State(2, "stop", false), "one_second_amber_enters_all_red");
            Action("crossing_request"); Tick(24); Check(State(2, "stop", false), "active_request_does_not_restart_half_second_all_red");
            Tick(); Check(State(3, "stop", true), "half_second_all_red_then_white_walk");
            Action("clear_to_finish"); Tick(99); Check(State(3, "stop", true), "walk_before_two_seconds_even_with_path_occupied");
            Tick(); Check(State(4, "stop", false), "two_second_walk_enters_hand_red_clearance");
            Tick(200); Check(State(4, "stop", false), "clearance_expired_but_blocked_path_keeps_red_hand");
            Action("crossing_request"); Tick(2); Action("clear_to_finish"); Tick();
            Check(State(0, "ready", false), "path_clear_releases_green_without_deferred_request");
            Action("crossing_request"); Tick(2); Until(3); Check(State(3, "stop", true), "second_fresh_cycle_reaches_walk");
            Action("crossing_request"); StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; Tick(100);
            Check(CrossingOutputs.All(p => !On(p)) && runtime.Points["crossing_request"] is false && Phase() == 3
                && _virtualController.Snapshot.ScanNumber == scan, "stop_clears_commands_pending_request_and_freezes_scan");
            RunActiveController(); Tick(2); Check(State(5, "stop", false), "run_overrides_retained_walk_with_new_startup_clearance");
            Until(0); Check(State(0, "ready", false), "restart_clearance_completes_without_automatic_walk");
            _virtualController.SetBoolForce("crossing_request", true); Tick(); Until(0); Tick(100);
            Check(State(0, "ready", false), "held_request_produces_one_cycle_only");
            _virtualController.RemoveForce("crossing_request"); Tick(2);
            Check(invariant, "every_accepted_scan_keeps_walk_red_and_excludes_green_walk_overlap");
            ResetActiveController();
            Check(CrossingOutputs.All(p => !On(p)) && !On("crossing_request") && !On("clear_to_finish") && Phase() == 0
                && _virtualController.Snapshot.ScanNumber == 0 && _virtualController.Snapshot.State == VirtualControllerState.Stopped, "reset_all_false_zero_and_stopped");
        }
        finally { DisableVirtualController(); }
    }
}
