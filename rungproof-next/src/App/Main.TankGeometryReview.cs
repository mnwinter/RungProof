using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifySumpLevelMotionGeometry(Action<bool, string> check)
    {
        AddMigratedScene("lab-2-14-sump-pump", _candidateCatalog!, _mainCamera!, false, false);
        var tank = _sceneCompositionRoot!.GetNode<Node3D>("sump_tank");
        var liquid = (MeshInstance3D)tank.FindChild("KIN_liquid", true, false);
        var shell = ReviewBounds((MeshInstance3D)tank.FindChild("TANK_shell", true, false));
        var runtime = _sceneRuntime!;
        runtime.UsesExternalClock = false;
        runtime.ResetSimulation();
        var lowBody = _sceneCompositionRoot.GetNode<MeshInstance3D>("low_float/SUMP_FLOAT_body");
        var highBody = _sceneCompositionRoot.GetNode<MeshInstance3D>("high_float/SUMP_FLOAT_body");
        var initialLow = lowBody.Position;
        var initialHigh = highBody.Position;
        var initial = ReviewBounds(liquid);
        check(MathF.Abs(initial.Position.Y-.12f)<.001f && MathF.Abs(initial.End.Y-(.12f+1.65f*.22f))<.001f,
            "sump_liquid_initial_22_percent_matches_open_basin_datum");
        check(runtime.ExecuteAction("start-sump"), "sump_reference_level_motion_starts_for_geometry_audit");
        runtime.AdvanceSimulation(1);
        var actualFraction = (ReviewBounds(liquid).End.Y-.12f)/1.65f;
        var reportedPercent = Convert.ToDouble(runtime.Points["sump_level"]);
        check(lowBody.Position.Y > initialLow.Y && highBody.Position == initialHigh,
            "sump_rising_water_lifts_low_float_with_high_float_on_lower_stop");
        GD.Print($"SUMP_LEVEL_COHERENCE after=1s renderedPercent={actualFraction*100} reportedPercent={reportedPercent}");
        check(Math.Abs(reportedPercent-actualFraction*100)<.01,
            "sump_mid_rise_reported_level_matches_actual_liquid_surface");
        var contained = true;
        var feedbackCoherent = true;
        var floatTravelCoherent = true;
        var peak = initial.End.Y;
        for (var tick=0;tick<350;tick++)
        {
            runtime.AdvanceSimulation(.02);
            var bounds=ReviewBounds(liquid);
            contained &= MathF.Abs(bounds.Position.Y-.12f)<.001f
                && bounds.End.Y<shell.End.Y
                && bounds.Position.X>shell.Position.X+.14f && bounds.End.X<shell.End.X-.14f
                && bounds.Position.Z>shell.Position.Z+.14f && bounds.End.Z<shell.End.Z-.14f;
            foreach (var body in new[] { lowBody, highBody })
            {
                var datum = ((Node3D)body.GetParent()).GlobalPosition.Y;
                floatTravelCoherent &= MathF.Abs(body.Position.Y - Mathf.Clamp(bounds.End.Y - datum, -.08f, .08f)) < .001f
                    && body.Position.X == 0 && body.Position.Z == 0;
            }
            peak=MathF.Max(peak,bounds.End.Y);
            var fraction=(bounds.End.Y-.12f)/1.65f;
            feedbackCoherent &= Math.Abs(Convert.ToDouble(runtime.Points["sump_level"])-fraction*100)<.01
                && Equals(runtime.Points["low_float_active"],fraction<=.200001f)
                && Equals(runtime.Points["high_float_active"],fraction>=.779999f);
        }
        check(floatTravelCoherent, "sump_350_samples_float_bodies_follow_level_on_bounded_stems");
        check(contained,"sump_350_reference_level_samples_remain_inside_open_basin");
        check(feedbackCoherent,"sump_350_samples_percentage_and_float_inputs_match_liquid_surface");
        check(MathF.Abs(peak-(.12f+1.65f*.78f))<.001f,
            "sump_reference_motion_reaches_78_percent_high_level");
        check(MathF.Abs(ReviewBounds(liquid).End.Y-(.12f+1.65f*.20f))<.001f,
            "sump_reference_motion_finishes_at_20_percent_low_level");
        runtime.ResetSimulation();
        check(MathF.Abs(ReviewBounds(liquid).End.Y-initial.End.Y)<.001f,
            "sump_reset_restores_initial_liquid_surface");
        check(Math.Abs(Convert.ToDouble(runtime.Points["sump_level"])-22)<.001
            && runtime.Points["low_float_active"] is false && runtime.Points["high_float_active"] is false,
            "sump_reset_restores_level_and_float_input_image");
        check(lowBody.Position.IsEqualApprox(initialLow) && highBody.Position.IsEqualApprox(initialHigh),
            "sump_reset_restores_both_float_poses");
        // This drives the existing reference trajectory for geometry only.
        // It does not establish a PLC-owned pump latch or float mechanics.
        VerifySumpControllerCycle(check);
    }

    private void VerifySumpControllerCycle(Action<bool,string> check)
    {
        var qa=new LadderEditorDocument();
        qa.ResetProject("review-sump-latch","Sump_Latch_QA",TimeSpan.FromMilliseconds(20));
        qa.SourceSceneId="lab-2-14-sump-pump";
        foreach(var name in new[]{"start_sump","high_float_active","low_float_active"}) qa.AddTag(name,PlcVariableRole.Input,name);
        foreach(var name in new[]{"cycle_enable","pump_run"}) qa.AddTag(name,PlcVariableRole.Output,name);
        foreach(var name in new[]{"qa_cycle_requested","qa_pump_latched"}) qa.AddTag(name,PlcVariableRole.Memory);
        qa.AddRung("QA: remember Start until Reset","qa_cycle_requested").CoilMode=LadderCoilMode.Set;
        qa.AddContact(0,0,"start_sump",false);
        qa.AddRung("QA: latch pump at high float","qa_pump_latched").CoilMode=LadderCoilMode.Set;
        qa.AddContact(1,0,"high_float_active",false);
        qa.AddRung("QA: reset latch at low float","qa_pump_latched").CoilMode=LadderCoilMode.Reset;
        qa.AddContact(2,0,"low_float_active",false);
        qa.AddRung("QA: project cycle enable","cycle_enable");qa.AddContact(3,0,"qa_cycle_requested",false);
        qa.AddRung("QA: project pump command","pump_run");qa.AddContact(4,0,"qa_pump_latched",false);
        qa.WatchVariables.AddRange(qa.Tags.Select(tag=>tag.Name));
        var compiled=LadderCompiler.Compile(qa.BuildProgram());
        check(compiled.IsValid,"sump_native_qa_latch_program_compiles");
        if(!compiled.IsValid) throw new InvalidOperationException(string.Join(";",compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-a-a-sump-latch-native-qa.rpproj.json"),LadderEditorProjectJson.Save(qa));
        var runtime=_sceneRuntime!;
        EnableVirtualControllerProgram(qa.BuildProgram());
        try
        {
            void Scans(int n) { for(var i=0;i<n;i++) _PhysicsProcess(.02); }
            RunActiveController();Scans(10);
            check(Math.Abs(Convert.ToDouble(runtime.Points["sump_level"])-22)<.001 && runtime.Points["pump_run"] is false,
                "sump_normal_Run_without_Start_holds_initial_level");
            check(ExecuteSelectedControllerAction("start-sump"),"sump_normal_Start_pulses_loaded_ladder_input");
            Scans(150);
            check(runtime.Points["pump_run"] is true && runtime.Points["high_float_active"] is false
                && Convert.ToDouble(runtime.Points["sump_level"])>20 && Convert.ToDouble(runtime.Points["sump_level"])<78,
                "sump_normal_controller_latches_pump_through_deadband");
            StopActiveController();var held=Convert.ToDouble(runtime.Points["sump_level"]);var scan=_virtualController!.Snapshot.ScanNumber;
            check(Equals(runtime.Points["status_color"], "red"), "sump_Stop_tower_reports_stopped_controller");
            Scans(25);
            check(Convert.ToDouble(runtime.Points["sump_level"])==held && runtime.Points["pump_run"] is false
                && _virtualController.Snapshot.ScanNumber==scan,"sump_Stop_holds_water_and_clears_pump_command");
            RunActiveController();Scans(200);
            check(Math.Abs(Convert.ToDouble(runtime.Points["sump_level"])-20)<.001 && runtime.Points["low_float_active"] is true
                && runtime.Points["pump_run"] is false,"sump_resume_finishes_at_low_float_with_pump_off");
            check(Equals(runtime.Points["status_color"], "amber"), "sump_completed_tower_reports_pump_off");
            Scans(30);
            check(Math.Abs(Convert.ToDouble(runtime.Points["sump_level"])-20)<.001,"sump_held_cycle_enable_does_not_repeat_without_Reset");
            ResetActiveController();
            check(_virtualController.Snapshot.ScanNumber==0 && Math.Abs(Convert.ToDouble(runtime.Points["sump_level"])-22)<.001
                && runtime.Points["cycle_enable"] is false && runtime.Points["pump_run"] is false,"sump_controller_Reset_restores_water_and_commands");
        }
        finally { DisableVirtualController(); }
    }

    private void VerifyTankAnalogMountGeometry(Action<bool, string> check)
    {
        AddMigratedScene("tank-level", _candidateCatalog!, _mainCamera!, false, false);
        var root = _sceneCompositionRoot!;
        var tank = root.GetNode<Node3D>("process_tank");
        var sensor = root.GetNode<Node3D>("level_transmitter");
        MeshInstance3D Part(Node node, string name) => (MeshInstance3D)node.FindChild(name, true, false);
        var roof = ReviewBounds(Part(tank, "TANK_roof"));
        var liquid = ReviewBounds(Part(tank, "KIN_liquid"));
        var rod = ReviewBounds(Part(sensor, "PROBE_sensing_rod"));
        var tip = ReviewBounds(Part(sensor, "PROBE_tip_weight"));
        var flange = ReviewBounds(Part(sensor, "PROCESS_flange"));
        var nozzle = tank.FindChild("TANK_ANALOG_socket", true, false) as MeshInstance3D;
        check(nozzle is not null && MathF.Abs(ReviewBounds(nozzle).End.Y - flange.Position.Y) < 0.001f
            && ReviewBounds(nozzle).Position.Y < roof.End.Y,
            "tank_analog_process_flange_seated_on_roof_socket");
        var shell = ReviewBounds(Part(tank, "TANK_shell"));
        bool Inside(Aabb bounds) => new Vector2(bounds.GetCenter().X - shell.GetCenter().X,
            bounds.GetCenter().Z - shell.GetCenter().Z).Length() + MathF.Max(bounds.Size.X, bounds.Size.Z) / 2 < shell.Size.X / 2;
        check(Inside(rod) && Inside(tip) && MathF.Abs(tip.Position.Y - liquid.Position.Y) < 0.001f
            && rod.End.Y > liquid.Position.Y + liquid.Size.Y / 0.42f,
            "tank_analog_probe_inside_vessel_and_covers_full_rendered_level_range");
        check(rod.Intersects(ReviewBounds(Part(sensor, "PROBE_insulator"))) && rod.Intersects(tip),
            "tank_analog_probe_seated_in_insulator_and_tip_weight");
        check(MathF.Abs(rod.Size.X - 0.05f) < 0.001f && MathF.Abs(rod.Size.Z - 0.05f) < 0.001f
            && MathF.Abs(tip.Size.Y - 0.12f) < 0.001f,
            "tank_analog_installation_preserves_rod_diameter_and_tip_weight_size");
        var headParts = ReviewMeshes(sensor).Where(mesh => !mesh.Name.ToString().StartsWith("PROBE_", StringComparison.Ordinal)).ToArray();
        var tankObstacles = ReviewMeshes(tank).Where(mesh => mesh.Name.ToString().StartsWith("RAIL_", StringComparison.Ordinal)
            || mesh.Name.ToString().StartsWith("MANWAY_", StringComparison.Ordinal)).ToArray();
        bool Clear(MeshInstance3D part, MeshInstance3D other)
        {
            var box = ReviewBounds(part);
            if (!box.Intersects(ReviewBounds(other))) return true;
            // Ring bounds include empty space. Narrow the screen to delivered
            // triangle bounds instead of calling the whole ring a solid disk.
            var faces = other.Mesh.GetFaces();
            for (var index = 0; index < faces.Length; index += 3)
            {
                var triangle = new Aabb(other.GlobalTransform * faces[index], Vector3.Zero)
                    .Expand(other.GlobalTransform * faces[index + 1]).Expand(other.GlobalTransform * faces[index + 2]);
                // Include planar faces whose unexpanded bounds have a zero
                // dimension; those must still catch a part crossing the face.
                var overlap = box.Intersection(triangle.Grow(0.001f)).Size;
                if (overlap.X > 0.001f && overlap.Y > 0.001f && overlap.Z > 0.001f) return false;
            }
            return true;
        }
        check(headParts.All(part => ReviewBounds(part).Position.Y >= roof.End.Y
            && tankObstacles.All(other => Clear(part, other))),
            "tank_analog_head_above_roof_and_clear_of_manway_and_guardrails");
        GD.Print($"TANK_ANALOG_MOUNT roof={roof} flange={flange} rod={rod} tip={tip}");
    }

    private void VerifyTankSwitchMountGeometry(Action<bool, string> check)
    {
        foreach (var (sceneId, tankId, lowId, highId, initial, low, high) in new[]
        {
            ("tank-high-low", "water_tank_hl", "hl_low_switch", "hl_high_switch", 0.5f, 0.25f, 0.75f),
            ("tank-level", "process_tank", "low_level_switch", "high_level_switch", 0.42f, 0.2f, 0.8f),
            ("lab-2-14-sump-pump", "sump_tank", "low_float", "high_float", 0.22f, 0.2f, 0.78f),
        })
        {
            AddMigratedScene(sceneId, _candidateCatalog!, _mainCamera!, false, false);
            var root = _sceneCompositionRoot!;
            var tank = root.GetNode<Node3D>(tankId);
            var shell = ReviewBounds((MeshInstance3D)tank.FindChild("TANK_shell", true, false));
            var liquid = ReviewBounds((MeshInstance3D)tank.FindChild("KIN_liquid", true, false));
            foreach (var (id, threshold) in new[] { (lowId, low), (highId, high) })
            {
                var sensor = root.GetNode<Node3D>(id);
                if (sceneId == "lab-2-14-sump-pump")
                {
                    var body = ReviewBounds((MeshInstance3D)sensor.FindChild("SUMP_FLOAT_body", true, false));
                    var elevationFloat = liquid.Position.Y + liquid.Size.Y / initial * threshold;
                    check(MathF.Abs(sensor.GlobalPosition.Y-elevationFloat)<.001f
                        && MathF.Abs(body.GetCenter().Y-(elevationFloat+Mathf.Clamp(liquid.End.Y-elevationFloat,-.08f,.08f)))<.001f
                        && body.Position.X>shell.Position.X+.14f && body.End.X<shell.End.X-.14f
                        && body.Position.Z>shell.Position.Z+.14f && body.End.Z<shell.End.Z-.14f,
                        $"{sceneId}_{id}_float_datum_at_threshold_and_moving_body_inside_open_basin");
                    var bracket = ReviewBounds((MeshInstance3D)sensor.FindChild("SUMP_FLOAT_bracket",true,false));
                    check(bracket.Position.Y<shell.End.Y && bracket.End.Y>shell.End.Y
                        && bracket.Position.X<shell.End.X && bracket.End.X>shell.End.X,
                        $"{sceneId}_{id}_bracket_seated_on_basin_rim");
                    var stem = ReviewBounds((MeshInstance3D)sensor.FindChild("SUMP_FLOAT_stem",true,false));
                    check(stem.Intersects(body) && stem.Intersects(bracket) && stem.Position.Y>.12f,
                        $"{sceneId}_{id}_float_stem_connected_to_bracket_above_floor");
                    check(tank.FindChild("TANK_roof",true,false) is null
                        && sensor.FindChild("FORK_tip",true,false) is null,
                        $"{sceneId}_{id}_open_basin_float_replaces_closed_vessel_fork");
                    continue;
                }
                var tips = ReviewMeshes(sensor).Where(mesh => mesh.Name.ToString().StartsWith("FORK_tip", StringComparison.Ordinal)).ToArray();
                var elevation = liquid.Position.Y + liquid.Size.Y / initial * threshold;
                check(tips.Length == 2 && tips.All(tip =>
                {
                    var center = ReviewBounds(tip).GetCenter() - shell.GetCenter();
                    return MathF.Sqrt(center.X * center.X + center.Z * center.Z) < shell.Size.X / 2
                        && MathF.Abs(ReviewBounds(tip).GetCenter().Y - elevation) < 0.001f;
                }), $"{sceneId}_{id}_tips_inside_tank_at_actual_level_threshold");
                var nozzle = tank.FindChild($"TANK_SWITCH_{id}", true, false) as MeshInstance3D;
                var seal = ReviewBounds((MeshInstance3D)sensor.FindChild("PROCESS_seal", true, false));
                var alongX = sceneId == "lab-2-14-sump-pump";
                check(nozzle is not null && (alongX
                    ? MathF.Abs(ReviewBounds(nozzle).End.X - seal.Position.X) < 0.001f
                        && ReviewBounds(nozzle).Position.X < shell.End.X
                    : MathF.Abs(ReviewBounds(nozzle).End.Z - seal.Position.Z) < 0.001f
                        && ReviewBounds(nozzle).Position.Z < shell.End.Z),
                    $"{sceneId}_{id}_process_seal_seated_on_tank_nozzle");
                var tankObstacles = ReviewMeshes(tank).Where(mesh =>
                    mesh.Name.ToString().StartsWith("LADDER", StringComparison.Ordinal)
                    || mesh.Name.ToString().StartsWith("SIGHT", StringComparison.Ordinal)).ToArray();
                check(ReviewMeshes(sensor).All(part => ReviewBounds(part).Position.Y >= 0
                    && tankObstacles.All(other => !OrientedBoxesPenetrate(part, other))),
                    $"{sceneId}_{id}_above_floor_and_clear_of_ladder_and_sight_glass");
                var separateSolids = root.GetChildren().OfType<Node3D>().Where(node => node != tank && node != sensor)
                    .SelectMany(ReviewMeshes).ToArray();
                check(ReviewMeshes(sensor).All(part => separateSolids.All(other =>
                {
                    var overlap = ReviewBounds(part).Intersection(ReviewBounds(other)).Size;
                    return overlap.X <= 0.001f || overlap.Y <= 0.001f || overlap.Z <= 0.001f
                        || !OrientedBoxesPenetrate(part, other);
                })), $"{sceneId}_{id}_clear_of_separate_equipment");
                GD.Print($"TANK_SWITCH_MOUNT {sceneId} {id} elevation={elevation} tips={string.Join(';', tips.Select(ReviewBounds))}");
            }
        }
    }
}
