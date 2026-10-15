using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditParkingEntry;
    private void AuditParkingEntry()
    {
        var failures = 0;
        void Check(bool value, string label) { if (!value) failures++; GD.Print($"PARKING_ENTRY_CHECK {label}={value}"); }
        try { VerifyParkingEntryWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"PARKING_ENTRY_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
    private void VerifyParkingEntryWorkflow(Action<bool, string> check)
    {
        const string sceneId = "lab-4-07-parking-garage-entry";
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!; var plant = runtime.ParkingEntryPlant!;
        var cars = new[] { root.GetNode<Node3D>("vehicle_0"), root.GetNode<Node3D>("vehicle_1") };
        var pivot = root.GetNode<Node3D>("barrier").GetNode<Node3D>("BoomPivot");
        var pad = (MeshInstance3D)root.GetNode<Node3D>("parking_pad").FindChild("ROAD_supported_slab", true, false);
        var display = root.GetNode<Node3D>("occupancy_display");
        var readout = display.GetNode<Label3D>("NumericReadout");
        var displayBase = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_base", true, false);
        var displayMast = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_mast", true, false);
        var displayHousing = (MeshInstance3D)display.FindChild("COUNT_DISPLAY_housing", true, false);
        check(readout.Text == "OCCUPANCY\n0" && display.FindChild("COUNT_DISPLAY_static_legend", true, false) is MeshInstance3D { Visible: false },
            "parking_numeric_readout_replaces_static_legend");
        check(Math.Abs(ReviewBounds(displayBase).Position.Y) < .001 && ReviewBounds(displayMast).Intersects(ReviewBounds(displayBase).Grow(.001f))
            && ReviewBounds(displayMast).Intersects(ReviewBounds(displayHousing)), "parking_readout_base_mast_and_housing_are_supported");
        var fixtures = root.GetChildren().OfType<Node3D>().Where(n => n.Name != "parking_pad" && !cars.Contains(n)).SelectMany(ReviewMeshes)
            .Where(m => !m.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= .001f || overlap.Y <= .001f || overlap.Z <= .001f || !OrientedBoxesPenetrate(a, b);
        }
        var clear = true; var supported = true; var feedbackMatches = true; var samples = 0; var bad = new HashSet<string>();
        // Cars start hidden. Discover their meshes independently of visibility;
        // visibility decides whether to inspect a pose, not which meshes exist.
        var carParts = cars.Select(c => c.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Where(m => m.Mesh is not null).ToArray()).ToArray();
        var fixedBounds = fixtures.Where(m => !pivot.IsAncestorOf(m)).ToDictionary(m => m, ReviewBounds);
        var stationRoots = root.GetChildren().OfType<Node3D>().Where(n => n.Name != "parking_pad" && !cars.Contains(n)).ToArray();
        check(stationRoots.All(a => stationRoots.Where(b => b != a).All(b => ReviewMeshes(a).All(am => ReviewMeshes(b).All(bm => Clear(am, bm))))),
            "parking_static_stations_sensor_feet_and_barrier_are_separated");
        void GeometrySample()
        {
            samples++; var slab = ReviewBounds(pad);
            // Recompute moving mesh bounds once per accepted pose; fixed bounds
            // are cached. Avoid millions of repeated native tree enumerations.
            var bounds = new Dictionary<MeshInstance3D, Aabb>(fixedBounds);
            foreach (var part in fixtures.Where(m => pivot.IsAncestorOf(m))) bounds[part] = ReviewBounds(part);
            for (var i = 0; i < 2; i++) if (cars[i].Visible)
                foreach (var part in carParts[i]) bounds[part] = ReviewBounds(part);
            bool PoseClear(MeshInstance3D a, MeshInstance3D b)
            {
                var overlap = bounds[a].Intersection(bounds[b]).Size;
                return overlap.X <= .001f || overlap.Y <= .001f || overlap.Z <= .001f || !OrientedBoxesPenetrate(a, b);
            }
            for (var i = 0; i < 2; i++)
            {
                var car = cars[i]; if (!car.Visible) continue;
                var body = ReviewBounds(car);
                var previouslySupported = supported;
                supported &= body.Position.X >= slab.Position.X - .001 && body.End.X <= slab.End.X + .001
                    && body.Position.Z >= slab.Position.Z - .001 && body.End.Z <= slab.End.Z + .001;
                var wheels = carParts[i].Where(m => m.Name.ToString().StartsWith("WHEEL_", StringComparison.Ordinal)).ToArray();
                supported &= wheels.Length == 4 && wheels.All(m => Math.Abs(bounds[m].Position.Y - slab.End.Y) < .001);
                if (previouslySupported && !supported)
                    GD.Print($"PARKING_FIRST_SUPPORT_FAILURE car={car.Name} body={body} slab={slab} wheels={string.Join(";", wheels.Select(m => $"{m.Name}:{bounds[m]}"))}");
                foreach (var part in carParts[i])
                    foreach (var other in fixtures.Concat(cars[1-i].Visible ? carParts[1-i] : []))
                        if (!PoseClear(part, other))
                        {
                            clear = false;
                            if (bad.Add($"{part.Name}/{other.Name}")) GD.Print($"PARKING_FIRST_OVERLAP {car.Name}/{part.Name} {ReviewBounds(part)} / {other.Name} {ReviewBounds(other)} phase={plant.Phase}");
                        }
                feedbackMatches &= car.Position.IsEqualApprox(new Vector3((float)plant.Vehicles[i].X, .04f, (float)plant.Vehicles[i].Z));
            }
            feedbackMatches &= Math.Abs(pivot.Rotation.Z - plant.BoomFraction * Math.PI / 2) < .00001
                && readout.Text == "OCCUPANCY\n" + Convert.ToInt64(runtime.Points["occupancy_count"])
                && Equals(runtime.Points["barrier_raised"], plant.BoomFraction >= 1 - 1e-9)
                && Equals(runtime.Points["entry_detected"], plant.EntryDetected)
                && Equals(runtime.Points["passage_detected"], plant.PassageDetected);
        }
        check(cars.All(c => !c.Visible) && plant.Phase == ParkingEntryPlantModel.RoutePhase.Idle
            && runtime.Points["space_available"] is true && runtime.Points["barrier_closed"] is true, "parking_initial_empty_closed_actual_feedback");
        check(carParts[0].Any(m => m.Name == "VEHICLE_body") && ReviewMeshes(pivot).Any(m => m.Name == "BOOM_arm"), "parking_real_vehicle_and_hinged_boom_replace_substitutes");
        var document = new LadderEditorDocument();
        document.ResetProject("review-parking-entry", "Parking_Entry_Reference", TimeSpan.FromMilliseconds(20)); document.SourceSceneId = sceneId;
        foreach (var name in new[] { "machine_enabled", "exit_clear", "entry_detected", "space_available", "exit_requested", "passage_occupied", "entry_passed", "exit_passed", "barrier_raised" }) document.AddTag(name, PlcVariableRole.Input, name);
        foreach (var name in new[] { "barrier_open", "vehicle_run", "garage_available" }) document.AddTag(name, PlcVariableRole.Output, name);
        document.AddTag("occupancy_count", PlcVariableRole.Output, "occupancy_count", PlcVariableType.DInt);
        foreach (var name in new[] { "arrivals", "departures" }) document.AddTag(name, PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("publish", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.AddRange(["entry_detected", "passage_occupied", "barrier_raised", "entry_passed", "exit_passed", "arrivals", "departures", "occupancy_count", "barrier_open", "vehicle_run"]);
        document.AddCounterRung("Count actual parked arrival edge", "arrivals", 2); document.AddContact(0, 0, "entry_passed", false);
        document.AddCounterRung("Count actual complete departure edge", "departures", 2); document.AddContact(1, 0, "exit_passed", false);
        document.AddNumericOperationRung("Occupancy = actual arrivals minus departures", LadderNumericOperationKind.Subtract, "arrivals.ACC", "departures.ACC", "occupancy_count"); document.AddContact(2, 0, "publish", false);
        document.AddRung("Barrier: permissive arrival, requested exit, or occupied passage", "barrier_open");
        document.AddContact(3, 0, "machine_enabled", false); document.AddContact(3, 0, "exit_clear", false); document.AddContact(3, 0, "entry_detected", false); document.AddComparison(3, 0, "occupancy_count", LadderCompareOperator.LessThan, "2");
        document.AddParallelBranch(3); document.AddContact(3, 1, "machine_enabled", false); document.AddContact(3, 1, "exit_clear", false); document.AddContact(3, 1, "exit_requested", false);
        document.AddParallelBranch(3); document.AddContact(3, 2, "machine_enabled", false); document.AddContact(3, 2, "passage_occupied", false);
        document.AddRung("Driver permitted only while enabled and path clear", "vehicle_run"); document.AddContact(4, 0, "machine_enabled", false); document.AddContact(4, 0, "exit_clear", false);
        document.AddRung("Available from PLC count and actual free space", "garage_available"); document.AddContact(5, 0, "machine_enabled", false); document.AddContact(5, 0, "space_available", false); document.AddComparison(5, 0, "occupancy_count", LadderCompareOperator.LessThan, "2");
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-parking-entry.rpproj.json"), LadderEditorProjectJson.Save(document));
        var program = document.BuildProgram(); var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Parking reference does not compile: " + string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        try
        {
            long Count() => Convert.ToInt64(runtime.Points["occupancy_count"]);
            void Tick() { _PhysicsProcess(.02); GeometrySample(); }
            void Ticks(int n) { for (var i = 0; i < n; i++) Tick(); }
            void Action(string id) { if (!ExecuteSelectedControllerAction(id)) throw new InvalidOperationException($"Rejected parking action: {id}"); }
            void Finish() { var ticks = 0; while (plant.Phase != ParkingEntryPlantModel.RoutePhase.Idle && ticks++ < 1400) Tick(); Tick(); if (ticks >= 1400) throw new InvalidOperationException("Parking route did not finish."); }
            RunActiveController(); Action("enter-vehicle"); Ticks(10);
            check(cars[0].Visible && cars[0].Position.Z == 8 && Count() == 0 && plant.BoomFraction == 0, "parking_entry_stages_but_cannot_move_without_enable_or_path");
            Action("toggle-machine_enabled"); Ticks(10); check(cars[0].Position.Z == 8, "parking_enable_alone_cannot_drive_vehicle");
            Action("toggle-exit_clear"); Ticks(200);
            check(plant.EntryDetected && plant.BoomFraction is > 0 and < 1 && Count() == 0 && plant.Phase == ParkingEntryPlantModel.RoutePhase.EntryWait, "parking_actual_approach_waits_at_partial_boom_with_no_count");
            var pausedCar = cars[0].Transform; var pausedBoom = pivot.Transform;
            StopActiveController(); var scan = _virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1); runtime.AdvanceSimulation(1);
            check(cars[0].Transform == pausedCar && pivot.Transform == pausedBoom && _virtualController.Snapshot.ScanNumber == scan && runtime.Points["barrier_open"] is false, "parking_stop_holds_car_boom_and_scan_clears_commands");
            RunActiveController(); Ticks(40);
            check(plant.BoomFraction == 1 && cars[0].Position.Z < 4.2 && Count() == 0, "parking_run_resumes_then_crosses_only_actual_raised_boom");
            Action("toggle-machine_enabled"); Tick(); pausedCar = cars[0].Transform; pausedBoom = pivot.Transform; Ticks(10);
            check(cars[0].Transform == pausedCar && pivot.Transform == pausedBoom, "parking_enable_loss_holds_partial_motion");
            Action("toggle-machine_enabled");
            var n = 0; while (!plant.PassageOccupied && n++ < 300) Tick();
            Action("toggle-exit_clear"); Tick(); pausedCar = cars[0].Transform; Ticks(50);
            check(cars[0].Transform == pausedCar && plant.BoomFraction == 1 && runtime.Points["barrier_open"] is true && Count() == 0, "parking_obstructed_path_holds_vehicle_and_occupied_passage_keeps_boom_raised");
            Action("toggle-exit_clear"); Finish();
            check(Count() == 1 && plant.Vehicles[0].State == ParkingEntryPlantModel.VehicleState.Parked && cars[0].Visible && cars[0].Position.IsEqualApprox(new Vector3(-3.5f,.04f,-7)), "parking_first_actual_park_counts_once_and_retains_car");
            Ticks(100); check(Count() == 1 && plant.BoomFraction == 0, "parking_idle_does_not_recount_and_clear_boom_closes");
            Action("enter-vehicle"); Finish();
            check(Count() == 2 && cars.All(c => c.Visible) && plant.Vehicles.All(v => v.State == ParkingEntryPlantModel.VehicleState.Parked)
                && runtime.Points["garage_available"] is false && runtime.Points["space_available"] is false && !runtime.ExecuteAction("enter-vehicle"), "parking_two_visible_bays_count_two_and_reject_third_vehicle");
            Action("exit-vehicle"); Finish();
            check(Count() == 1 && cars[0].Visible && cars[0].Position.Z == 8 && plant.Vehicles[0].State == ParkingEntryPlantModel.VehicleState.Departed
                && !runtime.ExecuteAction("exit-vehicle"), "parking_complete_departure_decrements_once_retains_outbound_car_and_blocks_following_exit");
            Ticks(100); check(Count() == 1 && cars[0].Visible, "parking_departed_vehicle_never_disappears_at_threshold");
            Action("enter-vehicle"); Finish(); check(Count() == 2 && cars.All(c => c.Visible), "parking_reentry_reuses_outbound_car_and_counts_third_real_arrival");
            Action("exit-vehicle"); Finish(); Action("clear-departed");
            check(Count() == 1 && !cars[0].Visible && cars[1].Visible, "parking_explicit_remove_only_clears_fully_departed_car");
            Action("exit-vehicle"); Finish(); check(Count() == 0 && plant.Vehicles[1].State == ParkingEntryPlantModel.VehicleState.Departed, "parking_three_departures_balance_three_arrivals_to_zero");
            Action("clear-departed"); Ticks(10); check(cars.All(c => !c.Visible) && Count() == 0 && !runtime.ExecuteAction("exit-vehicle"), "parking_empty_lot_cannot_generate_negative_departure");
            Action("enter-vehicle"); Finish(); StopActiveController(); scan = _virtualController!.Snapshot.ScanNumber; var parked = cars[0].Transform;
            check(Count() == 0 && _virtualController.Snapshot.Counters["arrivals"].Accumulated == 4 && _virtualController.Snapshot.Counters["departures"].Accumulated == 3 && cars[0].Transform == parked, "parking_stop_clears_readout_image_but_retains_physical_lot_and_counter_memory");
            RunActiveController(); Tick(); check(Count() == 1 && _virtualController.Snapshot.ScanNumber == scan + 1 && cars[0].Transform == parked, "parking_run_republishes_retained_occupancy_without_new_event");
            ResetActiveController(); check(cars.All(c => !c.Visible) && plant.BoomFraction == 0 && Count() == 0
                && _virtualController.Snapshot.ScanNumber == 0 && _virtualController.Snapshot.State == VirtualControllerState.Stopped, "parking_application_reset_empties_lot_and_controller_stopped");
        }
        finally { DisableVirtualController(); }
        check(clear, "parking_full_routes_clear_boom_sensor_stands_controls_and_other_vehicle");
        check(supported, "parking_all_four_wheels_bear_on_pad_and_full_vehicle_stays_within_support");
        check(feedbackMatches, "parking_projected_vehicle_boom_and_actual_sensors_match_model");
        GD.Print($"PARKING_GEOMETRY_SAMPLES {samples}");
    }
}
