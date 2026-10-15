using System;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditMobileTraffic;
    private void AuditMobileTraffic()
    {
        var failures = 0;
        void Check(bool ok, string name) { if (!ok) failures++; GD.Print($"MOBILE_TRAFFIC_CHECK {name}={ok}"); }
        try { VerifyMobileTraffic(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"MOBILE_TRAFFIC_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline educational model; native pending");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
    private static LadderEditorDocument CreateMobileTrafficReference()
    {
        var d = new LadderEditorDocument();
        d.ResetProject("review-mobile-traffic", "Mobile_Traffic_Training_Reference", TimeSpan.FromMilliseconds(20));
        d.SourceSceneId = "lab-11-12-mobile-traffic-lights";
        foreach (var input in new[] { "controller_ready", "road_a_clear", "road_b_clear", "crossing_clear", "road_a_occupied", "road_b_occupied", "signal_conflict" })
            d.AddTag(input, PlcVariableRole.Input, input);
        foreach (var output in new[] { "road_a_red", "road_a_amber", "road_a_green", "road_b_red", "road_b_amber", "road_b_green" })
            d.AddTag(output, PlcVariableRole.Output, output);
        d.AddTag("traffic_phase", PlcVariableRole.Memory, type: PlcVariableType.DInt);
        d.AddTag("reference_running", PlcVariableRole.Output);
        void MoveWhen(string title, int destination, Action<int> condition)
        {
            var r = d.Rungs.Count; d.AddNumericOperationRung(title, LadderNumericOperationKind.Move, destination.ToString(), "", "traffic_phase"); condition(r);
        }
        // Stop clears this unbound output. Each new Run restarts all-red;
        // fixture loss also returns to all-red and resets phase timers.
        MoveWhen("First Run restarts all-red clearance", 0, r => d.AddContact(r, 0, "reference_running", true));
        foreach (var input in new[] { "controller_ready", "road_a_clear", "road_b_clear" })
            MoveWhen("Missing " + input + " restarts all-red", 0, r => d.AddContact(r, 0, input, true));
        // These presets are declared original educational assumptions. No
        // manufacturer timing, road capacity, clearance calculation or radio protocol.
        (int Phase, string Timer, int Ms)[] stages = [(0, "startup_red", 1000), (1, "a_green", 6000), (2, "a_amber", 1000),
            (3, "a_clearance", 1000), (4, "b_green", 6000), (5, "b_amber", 1000), (6, "b_clearance", 1000)];
        foreach (var (phase, timer, ms) in stages)
        {
            d.AddTag(timer, PlcVariableRole.Memory, type: PlcVariableType.Timer);
            var r = d.Rungs.Count; d.AddTimerRung("Training phase " + phase, timer, TimeSpan.FromMilliseconds(ms));
            d.AddComparison(r, 0, "traffic_phase", LadderCompareOperator.Equal, phase.ToString());
            foreach (var input in new[] { "controller_ready", "road_a_clear", "road_b_clear" }) d.AddContact(r, 0, input, false);
        }
        foreach (var (phase, timer, _) in stages.Reverse())
            MoveWhen("Complete phase " + phase, phase == 6 ? 1 : phase + 1, r => {
                d.AddComparison(r, 0, "traffic_phase", LadderCompareOperator.Equal, phase.ToString());
                d.AddContact(r, 0, timer + ".Q", false);
                if (phase is 0 or 3 or 6) d.AddContact(r, 0, "crossing_clear", false);
            });
        foreach (var (output, phase) in new[] { ("road_a_green", 1), ("road_a_amber", 2), ("road_b_green", 4), ("road_b_amber", 5) })
        {
            var r = d.Rungs.Count; d.AddRung(output + " only its phase and valid entry fixtures", output);
            d.AddComparison(r, 0, "traffic_phase", LadderCompareOperator.Equal, phase.ToString());
            foreach (var input in new[] { "controller_ready", "road_a_clear", "road_b_clear" }) d.AddContact(r, 0, input, false);
        }
        foreach (var side in new[] { "a", "b" }) {
            var r = d.Rungs.Count; d.AddRung("Road " + side + " red whenever its green and amber absent", "road_" + side + "_red");
            d.AddContact(r, 0, "road_" + side + "_green", true); d.AddContact(r, 0, "road_" + side + "_amber", true);
        }
        var last = d.Rungs.Count; d.AddRung("Mark scanning until application Stop", "reference_running");
        d.AddComparison(last, 0, "traffic_phase", LadderCompareOperator.GreaterOrEqual, "0");
        d.WatchVariables.AddRange(d.Tags.Select(t => t.Name)); return d;
    }
    private void VerifyMobileTraffic(Action<bool, string> check)
    {
        AddMigratedScene("lab-11-12-mobile-traffic-lights", _candidateCatalog!, _mainCamera!, false, false);
        var runtime = _sceneRuntime!; var root = _sceneCompositionRoot!;
        var plant = runtime.MobileTrafficPlant ?? throw new InvalidOperationException("Traffic runtime hook missing.");
        var cars = new[] { root.GetNode<Node3D>("traffic_vehicle_a"), root.GetNode<Node3D>("traffic_vehicle_b") };
        var road = ReviewMeshes(root.GetNode<Node3D>("training_accessory_4")).Single(m => m.Name.ToString() == "ROAD_horizontal_surface");
        var deck = ReviewBounds(road);
        check(cars.All(c => !c.Visible) && runtime.Points["crossing_clear"] is true, "initial_hidden_clear");
        var document = CreateMobileTrafficReference();
        var compiled = LadderCompiler.Compile(document.BuildProgram());
        if (!compiled.IsValid) throw new InvalidOperationException(string.Join("; ", compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-mobile-traffic-cycle-native-qa.rpproj.json"), LadderEditorProjectJson.Save(document));
        EnableVirtualControllerProgram(document.BuildProgram());
        try {
            bool On(string p) => runtime.Points[p] is true;
            int Phase() => (int)_virtualController!.Snapshot.NumericVariables["traffic_phase"];
            var invariant = true; var supported = true; var separate = true;
            void Tick(int count = 1) { for (var i = 0; i < count; i++) {
                _PhysicsProcess(.02); invariant &= !(On("road_a_green") && On("road_b_green"));
                foreach (var car in cars.Where(c => c.Visible)) {
                    var bounds = ReviewBounds(car);
                    supported &= bounds.Position.X >= deck.Position.X - .001 && bounds.End.X <= deck.End.X + .001
                        && bounds.Position.Z >= deck.Position.Z && bounds.End.Z <= deck.End.Z
                        && Math.Abs(bounds.Position.Y - deck.End.Y) < .002;
                }
                if (cars.All(c => c.Visible)) separate &= !ReviewBounds(cars[0]).Intersects(ReviewBounds(cars[1]));
            } }
            void Action(string name) { if (!ExecuteSelectedControllerAction(name)) throw new InvalidOperationException("Rejected " + name); }
            void Until(int phase) { for (var i = 0; i < 1200 && Phase() != phase; i++) Tick(); }
            RunActiveController(); Tick(60); check(Phase() == 0 && On("road_a_red") && On("road_b_red"), "missing_ready_remains_all_red");
            foreach (var p in new[] { "controller_ready", "road_a_clear", "road_b_clear" }) Action("toggle-" + p);
            Action("request-road-a-vehicle"); Action("request-road-b-vehicle"); Tick(49);
            check(Phase() == 0 && plant.Vehicles.All(v => v.Progress == 0), "startup_before_one_second_blocks_entries");
            Tick(); check(Phase() == 1 && On("road_a_green") && On("road_b_red"), "startup_one_second_selects_a");
            Tick(80); check(On("road_a_occupied") && !On("crossing_clear") && plant.Vehicles[1].Progress == 0, "a_crossing_actual_feedback_b_queued");
            var before = plant.Vehicles[0].Progress; Action("toggle-controller_ready"); Tick();
            check(Phase() == 0 && On("road_a_red") && On("road_b_red") && plant.Vehicles[0].Progress > before, "ready_loss_removes_lamps_but_committed_vehicle_clears");
            Action("toggle-controller_ready"); Tick(60);
            check(Phase() == 0 && !On("crossing_clear") && !On("road_a_green") && !On("road_b_green"), "expired_startup_waits_for_occupied_crossing");
            Until(1); check(Phase() == 1 && On("crossing_clear"), "restart_release_only_after_crossing_clears");
            Until(2); check(Phase() == 2 && On("road_a_amber") && On("road_b_red"), "a_timed_amber");
            Until(3); check(Phase() == 3 && On("road_a_red") && On("road_b_red"), "a_all_red");
            Until(4); check(Phase() == 4 && On("road_b_green") && plant.Vehicles[0].Complete, "opposite_release_after_a_clearance");
            Tick(80); check(On("road_b_occupied") && !On("crossing_clear"), "b_moves_and_reports_occupancy");
            var held = plant.Vehicles[1].Progress; StopActiveController(); Tick(25);
            check(plant.Vehicles[1].Progress == held && !On("road_a_green") && !On("road_b_green"), "application_stop_retains_pose_clears_commands");
            RunActiveController(); Tick(1); check(Phase() == 0 && On("road_a_red") && On("road_b_red"), "resume_restarts_red");
            Tick(300); check(plant.Vehicles[1].Complete && cars[1].Visible && On("crossing_clear"), "b_retained_endpoint");
            check(invariant && supported && separate, "all_sampled_commands_exclusive_cars_supported_and_separate");
            ResetActiveController(); check(cars.All(c => !c.Visible) && plant.Vehicles.All(v => v.Progress == 0) && On("crossing_clear"), "reset_clears_requests_pose_and_feedback");
        } finally { StopActiveController(); DisableVirtualController(); }
    }
}
