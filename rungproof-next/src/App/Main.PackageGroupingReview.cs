using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditPackageGrouping;
    private void AuditPackageGrouping()
    {
        var failures = 0;
        void Check(bool value, string name) { if (!value) failures++; GD.Print($"GROUPING_CHECK {name}={value}"); }
        try { VerifyPackageGroupingWorkflow(Check); }
        catch (Exception error) { failures++; GD.PushError(error.ToString()); }
        GD.Print($"GROUPING_VERIFY {(failures == 0 ? "PASS" : "FAIL")} offline-only");
        GetTree().Quit(failures == 0 ? 0 : 1);
    }
    private void VerifyPackageGroupingWorkflow(Action<bool, string> check)
    {
        const string sceneId = "lab-4-08-package-grouping";
        AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!; var runtime = _sceneRuntime!; var plant = runtime.PackageGroupingPlant!;
        var cartons = Enumerable.Range(0, 3).Select(i => root.GetNode<Node3D>($"carton_{i}")).ToArray();
        // Hidden initial cartons must still contribute their real meshes to later
        // pose checks. Visibility is tested per pose, never used for discovery.
        var parts = cartons.Select(c => c.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().Where(m => m.Mesh is not null).ToArray()).ToArray();
        var line = root.GetNode<Node3D>("group_line");
        var rollers = ReviewMeshes(line).Where(m => m.Name.ToString().StartsWith("KIN_group_roller", StringComparison.Ordinal)).ToArray();
        var gate = (MeshInstance3D)root.GetNode<Node3D>("group_stop").FindChild("KIN_group_gate", true, false);
        var rod = (MeshInstance3D)root.GetNode<Node3D>("group_stop").FindChild("STOP_telescoping_rod", true, false);
        var initialGate = gate.Transform;
        var opticalStations = new[] { ("entry_photoeye", "package_detected"), ("receiver_photoeye", "receiver_detected") }
            .Select(pair => (Point: pair.Item2, Beams: ReviewMeshes(root.GetNode<Node3D>(pair.Item1))
                .Where(m => m.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray())).ToArray();
        var readout = root.GetNode<Node3D>("group_count_display").GetNode<Label3D>("NumericReadout");
        var stationRoots = root.GetChildren().OfType<Node3D>().Where(n => !cartons.Contains(n)).ToArray();
        var fixtures = stationRoots.SelectMany(ReviewMeshes).Where(m => !m.Name.ToString().StartsWith("KIN_beam", StringComparison.Ordinal)).ToArray();
        bool Clear(MeshInstance3D a, MeshInstance3D b)
        {
            var overlap = ReviewBounds(a).Intersection(ReviewBounds(b)).Size;
            return overlap.X <= .001f || overlap.Y <= .001f || overlap.Z <= .001f || !OrientedBoxesPenetrate(a, b);
        }
        var stationaryClear = true;
        foreach (var a in stationRoots) foreach (var b in stationRoots.Where(n => n != a))
            foreach (var am in ReviewMeshes(a)) foreach (var bm in ReviewMeshes(b))
                if (!am.Name.ToString().StartsWith("KIN_beam") && !bm.Name.ToString().StartsWith("KIN_beam") && !Clear(am,bm))
                { stationaryClear = false; GD.Print($"GROUPING_STATIC_OVERLAP {a.Name}/{am.Name} {b.Name}/{bm.Name}"); }
        check(stationaryClear, "static_line_gate_sensor_feet_controls_and_readout_are_separated");
        check(rollers.Length > 50 && ReviewBounds(rollers[0]).End.Y > .899 && ReviewMeshes(line).Any(m => m.Name == "TRANSFER_bridge"), "connected_powered_receiver_and_transfer_bridge_are_real_meshes");
        check(readout.Text == "GROUP\n0" && !runtime.Points.ContainsKey("group_count_reached") && cartons.All(c => !c.Visible), "empty_scene_has_raw_feedback_and_plc_count_no_manual_ready_toggle");
        var supported = true; var clear = true; var projection = true; var samples = 0;
        var reported = new HashSet<string>();
        var fixedBounds = fixtures.Except(rollers).Where(m => m != gate && m != rod).ToDictionary(m => m, ReviewBounds);
        // Rotating a cylinder's box enlarges its apparent radius. Inspect its
        // actual mesh vertices instead, preserving the real support envelope.
        var rollerVertices = rollers.ToDictionary(r => r, r => r.Mesh.GetFaces().Distinct().ToArray());
        var rollerBoundsCache = new Dictionary<MeshInstance3D, (Transform3D Pose, Aabb Bounds)>();
        Aabb RollerBounds(MeshInstance3D mesh)
        {
            var pose = mesh.GlobalTransform;
            if (rollerBoundsCache.TryGetValue(mesh, out var prior) && prior.Pose == pose) return prior.Bounds;
            var vertices = rollerVertices[mesh];
            var bounds = new Aabb(pose * vertices[0], Vector3.Zero);
            foreach (var vertex in vertices) bounds = bounds.Expand(pose * vertex);
            rollerBoundsCache[mesh] = (pose, bounds); return bounds;
        }
        void Sample()
        {
            samples++;
            var bounds = new Dictionary<MeshInstance3D, Aabb>(fixedBounds);
            foreach (var m in rollers) bounds[m] = RollerBounds(m);
            bounds[gate] = ReviewBounds(gate); bounds[rod] = ReviewBounds(rod);
            var surfaces = rollers.Select(m => bounds[m]).Append(bounds[fixtures.Single(m => m.Name == "TRANSFER_bridge")]).ToArray();
            for (var i = 0; i < 3; i++) if (cartons[i].Visible)
            {
                var envelope = ReviewBounds(cartons[i]);
                var contacts = surfaces.Where(b => Math.Abs(b.End.Y - envelope.Position.Y) < .001 && b.End.X > envelope.Position.X && b.Position.X < envelope.End.X).ToArray();
                var prior = supported;
                supported &= contacts.Length >= 2 && envelope.Position.X >= -5-.001 && envelope.End.X <= 7+.001
                    && envelope.Position.Z >= -.72-.001 && envelope.End.Z <= .72+.001
                    && contacts.Min(b => b.Position.X) <= envelope.GetCenter().X && contacts.Max(b => b.End.X) >= envelope.GetCenter().X;
                if (prior && !supported) GD.Print($"GROUPING_FIRST_SUPPORT_FAILURE {cartons[i].Name} envelope={envelope} contacts={contacts.Length}");
                foreach (var part in parts[i])
                {
                    var pb = ReviewBounds(part);
                    foreach (var f in fixtures)
                    {
                        var overlap = pb.Intersection(bounds[f]).Size;
                        if (overlap.X > .001 && overlap.Y > .001 && overlap.Z > .001 && OrientedBoxesPenetrate(part,f))
                        { clear = false; if (reported.Add($"{part.Name}/{f.Name}")) GD.Print($"GROUPING_FIRST_CONTACT {cartons[i].Name}/{part.Name} {pb} / {f.Name} {bounds[f]}"); }
                    }
                    for (var j = i+1; j < 3; j++) if (cartons[j].Visible)
                        foreach (var other in parts[j]) if (!Clear(part,other))
                        { clear = false; if (reported.Add($"{i}/{j}/{part.Name}/{other.Name}")) GD.Print($"GROUPING_CARTON_CONTACT {i}/{j} {part.Name}/{other.Name}"); }
                }
                projection &= cartons[i].Position.IsEqualApprox(new((float)plant.X[i], .9f, 0));
            }
            projection &= readout.Text == "GROUP\n" + Convert.ToInt64(runtime.Points["group_count"])
                && Math.Abs(ReviewBounds(gate).Position.Y - (.95 + plant.GateFraction)) < .001
                && Math.Abs(ReviewBounds(rod).End.Y - 2.56) < .001
                && Math.Abs(ReviewBounds(rod).Position.Y - ReviewBounds(gate).End.Y) < .001
                && Equals(runtime.Points["package_detected"], plant.PackageDetected)
                && Equals(runtime.Points["receiver_detected"], plant.ReceiverDetected);
            foreach (var station in opticalStations)
            {
                var center = station.Beams.Select(ReviewBounds).Aggregate((a,b) => a.Merge(b)).GetCenter();
                var blocked = cartons.Where(c => c.Visible).Any(c => ReviewBounds(c).HasPoint(center));
                projection &= Equals(runtime.Points[station.Point], blocked) && station.Beams.All(b => b.Visible == !blocked);
            }
        }
        var document = new LadderEditorDocument();
        document.ResetProject("review-package-grouping", "Package_Grouping_Reference", TimeSpan.FromMilliseconds(20)); document.SourceSceneId = sceneId;
        foreach (var name in new[] { "machine_enabled", "release_clear", "package_detected", "group_staged", "stop_raised", "receiver_occupied", "transfer_complete" }) document.AddTag(name, PlcVariableRole.Input, name);
        foreach (var name in new[] { "group_conveyor_run", "group_release", "group_ready" }) document.AddTag(name, PlcVariableRole.Output, name);
        document.AddTag("group_count", PlcVariableRole.Output, "group_count", PlcVariableType.DInt);
        document.AddTag("arrivals", PlcVariableRole.Memory, type: PlcVariableType.Counter);
        document.AddTag("release_active", PlcVariableRole.Memory); document.AddTag("publish", PlcVariableRole.Memory, initialValue: true);
        document.WatchVariables.AddRange(["package_detected","arrivals","group_count","group_staged","stop_raised","group_release","transfer_complete"]);
        document.AddCounterRung("Count actual incoming beam rising edges", "arrivals", 3); document.AddContact(0,0,"package_detected",false);
        document.AddNumericOperationRung("Publish PLC count", LadderNumericOperationKind.Move,"arrivals.ACC","","group_count"); document.AddContact(1,0,"publish",false);
        document.AddRung("Latch release only after count and physical accumulation", "release_active").CoilMode = LadderCoilMode.Set;
        foreach (var name in new[] {"machine_enabled","release_clear","arrivals.DN","group_staged"}) document.AddContact(2,0,name,false);
        document.AddRung("Complete transfer clears release latch with priority", "release_active").CoilMode = LadderCoilMode.Reset; document.AddContact(3,0,"transfer_complete",false);
        document.AddRung("Drive rollers only with enable and downstream clear", "group_conveyor_run"); document.AddContact(4,0,"machine_enabled",false); document.AddContact(4,0,"release_clear",false); document.AddContact(4,0,"transfer_complete",true);
        document.AddRung("Permissive release remains commanded through crossing", "group_release"); foreach(var name in new[]{"machine_enabled","release_clear","release_active"}) document.AddContact(5,0,name,false);
        document.AddRung("PLC count reached indication", "group_ready"); document.AddContact(6,0,"arrivals.DN",false); document.AddContact(6,0,"transfer_complete",true);
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-package-grouping.rpproj.json"), LadderEditorProjectJson.Save(document));
        var program = document.BuildProgram(); var compiled = LadderCompiler.Compile(program);
        if (!compiled.IsValid) throw new InvalidOperationException("Grouping reference does not compile: " + string.Join("; ", compiled.Issues));
        EnableVirtualControllerProgram(program);
        var edgeCounts = true; var previousSampled = false; long expectedCount = 0;
        try
        {
            long Count() => Convert.ToInt64(runtime.Points["group_count"]);
            void Tick()
            {
                var sampled = runtime.Points["package_detected"] is true;
                if (sampled && !previousSampled) expectedCount++;
                _PhysicsProcess(.02); previousSampled = sampled;
                edgeCounts &= Count() == expectedCount; Sample();
            }
            void Ticks(int n) { for(var i=0;i<n;i++) Tick(); }
            void Action(string id) { if(!ExecuteSelectedControllerAction(id)) throw new InvalidOperationException("Rejected grouping action " + id); }
            void FinishFeed() { var n=0; while(plant.ActiveFeed>=0 && n++<1100) Tick(); if(n>=1100) throw new InvalidOperationException("Feed did not finish"); Tick(); }
            RunActiveController(); Action("load-carton"); Ticks(10);
            check(cartons[0].Visible && cartons[0].Position.X == -4.4f && Count()==0 && !runtime.ExecuteAction("load-carton"), "load_stages_one_carton_no_unpermitted_motion_or_double_load");
            Action("toggle-machine_enabled"); Ticks(10); check(cartons[0].Position.X == -4.4f, "enable_without_path_clear_cannot_feed");
            Action("toggle-release_clear"); Ticks(75);
            check(plant.PackageDetected && Count()==1 && plant.GateFraction==0, "actual_first_beam_crossing_counts_once_before_accumulation");
            var car = cartons[0].Transform; var roller = rollers[0].Transform; var stoppedGate=gate.Transform;
            StopActiveController(); var scan=_virtualController!.Snapshot.ScanNumber; _PhysicsProcess(1); runtime.AdvanceSimulation(1);
            check(cartons[0].Transform==car && rollers[0].Transform==roller && gate.Transform==stoppedGate && _virtualController.Snapshot.ScanNumber==scan && Count()==0, "stop_holds_carton_roller_gate_scan_and_clears_output_image");
            RunActiveController(); Tick(); check(Count()==1 && _virtualController.Snapshot.ScanNumber==scan+1, "run_republishes_retained_count_without_new_beam_edge");
            Action("toggle-machine_enabled"); Tick(); car=cartons[0].Transform; roller=rollers[0].Transform; Ticks(20);
            check(cartons[0].Transform==car && rollers[0].Transform==roller, "enable_loss_holds_partial_feed"); Action("toggle-machine_enabled"); FinishFeed();
            check(Count()==1 && Math.Abs(plant.X[0]-plant.Target(0))<1e-8 && !plant.GroupStaged && plant.GateFraction==0, "first_carton_stops_at_guided_gate_without_release");
            Action("load-carton"); FinishFeed(); check(Count()==2 && !plant.GroupStaged && plant.GateFraction==0 && cartons.Take(2).All(c=>c.Visible), "second_real_arrival_retained_behind_first_with_no_release");
            Action("load-carton"); FinishFeed();
            check(Count()==3 && plant.Loaded==3 && cartons.All(c=>c.Visible) && !runtime.ExecuteAction("load-carton"), "third_actual_arrival_forms_finite_group_and_blocks_fourth_load");
            Ticks(20); check(plant.GateFraction is >0 and <1 && !plant.ReceiverOccupied, "release_retracts_actual_stop_before_cartons_cross");
            car=cartons[0].Transform; stoppedGate=gate.Transform; Action("toggle-release_clear"); Tick(); Ticks(20);
            check(cartons[0].Transform==car && gate.Transform==stoppedGate && runtime.Points["group_release"] is false, "path_loss_holds_partial_stop_retraction_and_group"); Action("toggle-release_clear");
            var n=0; while(plant.X[0] + PackageGroupingPlantModel.HalfCarton < PackageGroupingPlantModel.GateX + .01 && n++<300) Tick();
            check(plant.GateFraction==1 && plant.StopPlaneOccupied && Count()==3, "group_crosses_only_fully_retracted_stop_and_count_remains_three");
            StopActiveController(); car=cartons[0].Transform; stoppedGate=gate.Transform; roller=rollers[0].Transform; scan=_virtualController.Snapshot.ScanNumber; _PhysicsProcess(1);
            check(cartons[0].Transform==car && gate.Transform==stoppedGate && rollers[0].Transform==roller && _virtualController.Snapshot.ScanNumber==scan, "stop_during_transfer_holds_group_gate_and_rollers");
            RunActiveController(); Tick(); check(Count()==3 && runtime.Points["group_release"] is true, "run_restores_retained_release_latch_and_count");
            n=0; while(!plant.Complete && n++<700) Tick(); Tick();
            check(plant.Complete && plant.ReceiverOccupied && cartons.All(c=>c.Visible) && Count()==3 && runtime.Points["group_conveyor_run"] is false && runtime.Points["group_release"] is false, "actual_complete_group_retained_on_receiver_commands_drop");
            car=cartons[0].Transform; Ticks(100); check(cartons[0].Transform==car && Count()==3 && plant.GateFraction==0, "completed_group_does_not_disappear_or_recount_and_clear_stop_returns");
            ResetActiveController(); expectedCount=0;previousSampled=false;
            check(cartons.All(c=>!c.Visible) && gate.Transform==initialGate && Count()==0 && plant.GateFraction==0 && _virtualController.Snapshot.ScanNumber==0 && _virtualController.Snapshot.State==VirtualControllerState.Stopped, "application_reset_clears_group_count_gate_and_stays_stopped");
            RunActiveController(); Action("toggle-machine_enabled"); Action("toggle-release_clear"); Action("load-carton"); FinishFeed();
            check(Count()==1 && plant.Loaded==1 && !plant.Complete, "new_batch_after_reset_counts_fresh_real_arrival"); ResetActiveController();
        }
        finally { DisableVirtualController(); }
        var isolated = new PackageGroupingPlantModel(); isolated.Reset(); isolated.Load(); isolated.Step(30,true,true,true,true);
        check(isolated.GateFraction==0 && !isolated.Releasing && !isolated.Complete, "premature_release_command_cannot_open_incomplete_group");
        var invalid=true; foreach(var time in new[]{-1d,double.NaN,double.PositiveInfinity}) { try{ isolated.Step(time,true,true,true,true); invalid=false;}catch(ArgumentOutOfRangeException){} }
        check(invalid,"invalid_elapsed_time_rejected");
        check(edgeCounts,"plc_count_matches_sampled_actual_beam_edges_each_scan");
        check(clear,"full_carton_routes_clear_line_frames_gate_sensors_and_other_cartons");
        check(supported,"each_carton_bears_on_multiple_rollers_across_supported_transfer");
        check(projection,"actual_carton_gate_rod_readout_and_optical_feedback_match_model");
        GD.Print($"GROUPING_GEOMETRY_SAMPLES {samples}");
    }
}
