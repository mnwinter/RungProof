using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private void VerifyServiceElevatorInstallation(Action<bool,string> check)
    {
        var cycle=new LadderEditorDocument();
        cycle.ResetProject("review-service-elevator-cycle","Service_Elevator_QA_Cycle",TimeSpan.FromMilliseconds(20));
        cycle.SourceSceneId="lab-11-11-service-elevator";
        foreach(var name in new[]{"call_valid","doors_closed","landing_clear","at_lower_landing","at_upper_landing","direction_conflict"})
            cycle.AddTag(name,PlcVariableRole.Input,name);
        cycle.AddTag("car_position_pct",PlcVariableRole.Input,"car_position_pct",type:PlcVariableType.Real);
        foreach(var name in new[]{"lift_up_cmd","lift_down_cmd"}) cycle.AddTag(name,PlcVariableRole.Output,name);
        cycle.AddTag("qa_upper_seen",PlcVariableRole.Memory);
        // Retain the return phase through permissive loss and Stop. Reset is
        // required for another round trip, preventing held-call oscillation.
        cycle.AddRung("Remember upper landing for a single return trip","qa_upper_seen").CoilMode=LadderCoilMode.Set;
        cycle.AddContact(0,0,"at_upper_landing",false);
        for(var direction=0;direction<2;direction++)
        {
            var index=cycle.Rungs.Count;
            cycle.AddRung(direction==0?"Ascend until upper landing":"Return and hold at lower landing",direction==0?"lift_up_cmd":"lift_down_cmd");
            foreach(var name in new[]{"call_valid","doors_closed","landing_clear"}) cycle.AddContact(index,0,name,false);
            cycle.AddContact(index,0,"qa_upper_seen",direction==0);
            cycle.AddContact(index,0,direction==0?"at_upper_landing":"at_lower_landing",true);
        }
        cycle.WatchVariables.AddRange(cycle.Tags.Select(tag=>tag.Name));
        var cycleCompiled=LadderCompiler.Compile(cycle.BuildProgram());
        check(cycleCompiled.IsValid,"elevator_native_round_trip_qa_program_compiles");
        if(!cycleCompiled.IsValid) throw new InvalidOperationException(string.Join(";",cycleCompiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-service-elevator-cycle-native-qa.rpproj.json"),LadderEditorProjectJson.Save(cycle));
        // Explicit QA files for normal editor Open / Verify+load review. They
        // never replace the exercise's blank project or connect to a PLC.
        foreach(var up in new[]{true,false})
        {
            var document=new LadderEditorDocument();
            var direction=up?"up":"down";
            document.ResetProject("review-service-elevator-"+direction,"Service_Elevator_QA_"+direction,TimeSpan.FromMilliseconds(20));
            document.SourceSceneId="lab-11-11-service-elevator";
            foreach(var name in new[]{"call_valid","doors_closed","landing_clear","at_lower_landing","at_upper_landing","direction_conflict"})
                document.AddTag(name,PlcVariableRole.Input,name);
            document.AddTag("car_position_pct",PlcVariableRole.Input,"car_position_pct",type:PlcVariableType.Real);
            foreach(var name in new[]{"lift_up_cmd","lift_down_cmd"}) document.AddTag(name,PlcVariableRole.Output,name);
            document.AddRung("QA single direction until destination landing",up?"lift_up_cmd":"lift_down_cmd");
            foreach(var name in new[]{"call_valid","doors_closed","landing_clear"}) document.AddContact(0,0,name,false);
            document.AddContact(0,0,up?"at_upper_landing":"at_lower_landing",true);
            document.WatchVariables.AddRange(document.Tags.Select(tag=>tag.Name));
            var compiled=LadderCompiler.Compile(document.BuildProgram());
            check(compiled.IsValid,"elevator_"+direction+"_native_qa_program_compiles");
            if(!compiled.IsValid) throw new InvalidOperationException(string.Join(";",compiled.Issues));
            System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/aaa-service-elevator-"+direction+"-native-qa.rpproj.json"),LadderEditorProjectJson.Save(document));
        }
        AddMigratedScene("lab-11-11-service-elevator", _candidateCatalog!, _mainCamera!, false, false);
        var scene = _sceneCompositionRoot!;
        var shaft = scene.GetNode<Node3D>("liftTable_0");
        var car = shaft.GetNode<Node3D>("KIN_service_elevator_car");
        var motion = shaft.GetNode<EquipmentMotionController>("ServiceElevatorPosition");
        var fixedParts = ReviewMeshes(shaft).Except(ReviewMeshes(car)).ToArray();
        bool Penetrates(Aabb a,Aabb b)
        {
            var overlap = a.Intersection(b).Size;
            return overlap.X > .001f && overlap.Y > .001f && overlap.Z > .001f;
        }
        check(LogBoundsCandidates(scene)==0,"elevator_separate_operator_equipment_clear_of_shaft");
        check(fixedParts.Count(p=>p.Name.ToString().StartsWith("ELEVATOR_landing_deck_"))==2,
            "elevator_has_two_distinct_landing_decks");
        var clear=true;
        for(var i=0;i<=100;i++)
        {
            motion.SetPositionNormalized(i/100f);
            SceneComposer.ProjectServiceElevatorHoist(shaft);
            foreach(var c in ReviewMeshes(car))
            foreach(var p in fixedParts)
            {
                if (!Penetrates(ReviewBounds(c),ReviewBounds(p))) continue;
                if(clear) GD.Print($"ELEVATOR_INTERFERENCE pose={i} car={c.Name} fixed={p.Name}");
                clear=false;
            }
        }
        check(clear,"elevator_101_car_poses_clear_fixed_shaft_and_landings");
        var leaves = fixedParts.Where(p=>p.Name.ToString().StartsWith("ELEVATOR_open_door_leaf_")).ToArray();
        foreach(var leaf in leaves)
            leaf.Position=new Vector3(MathF.Sign(leaf.Position.X)*.55f,leaf.Position.Y,leaf.Position.Z);
        clear=true;
        for(var i=0;i<=100;i++)
        {
            motion.SetPositionNormalized(i/100f);
            SceneComposer.ProjectServiceElevatorHoist(shaft);
            foreach(var c in ReviewMeshes(car))
            foreach(var p in fixedParts)
            {
                if(!Penetrates(ReviewBounds(c),ReviewBounds(p))) continue;
                if(clear) GD.Print($"ELEVATOR_CLOSED_DOOR_INTERFERENCE pose={i} car={c.Name} fixed={p.Name}");
                clear=false;
            }
        }
        check(clear,"elevator_101_car_poses_clear_with_closed_doors_and_hangers");
        // Before introducing door travel, screen intermediate leaf positions
        // against the car at every sampled elevation. Endpoint-only checks
        // cannot establish the clearance of the future sliding envelope.
        var doorTravelClear=true;
        for(var doorPose=0;doorPose<=10;doorPose++)
        {
            foreach(var leaf in leaves)
                leaf.Position=new Vector3(MathF.Sign(leaf.Position.X)
                    *(1.72f-(1.72f-.55f)*doorPose/10f),leaf.Position.Y,leaf.Position.Z);
            for(var carPose=0;carPose<=100;carPose++)
            {
                motion.SetPositionNormalized(carPose/100f);
                SceneComposer.ProjectServiceElevatorHoist(shaft);
                foreach(var c in ReviewMeshes(car))
                foreach(var p in fixedParts)
                {
                    if(!Penetrates(ReviewBounds(c),ReviewBounds(p))) continue;
                    if(doorTravelClear) GD.Print($"ELEVATOR_DOOR_ENVELOPE_INTERFERENCE door={doorPose} car={carPose} moving={c.Name} fixed={p.Name}");
                    doorTravelClear=false;
                }
            }
        }
        check(doorTravelClear,"elevator_11_door_by_101_car_poses_clear_sliding_envelope");
        // Check leaf bodies against permanent installation parts separately.
        // Attached hangers intentionally join their own leaf and track, so
        // their mounting contacts are not classified as body-path collisions.
        var permanentParts=fixedParts.Where(part=>!leaves.Any(leaf=>
            leaf==part || leaf.IsAncestorOf(part))).ToArray();
        var leafPathClear=true;
        for(var doorPose=0;doorPose<=100;doorPose++)
        {
            foreach(var leaf in leaves)
                leaf.Position=new Vector3(MathF.Sign(leaf.Position.X)
                    *(1.72f-(1.72f-.55f)*doorPose/100f),leaf.Position.Y,leaf.Position.Z);
            foreach(var leaf in leaves)
            foreach(var part in permanentParts.Concat(leaves.Where(other=>other!=leaf)))
            {
                if(!Penetrates(ReviewBounds(leaf),ReviewBounds(part))) continue;
                if(leafPathClear) GD.Print($"ELEVATOR_LEAF_PATH_INTERFERENCE pose={doorPose} leaf={leaf.Name} part={part.Name}");
                leafPathClear=false;
            }
        }
        check(leafPathClear,"elevator_101_leaf_poses_clear_fixed_installation_and_other_leaves");
        foreach(var leaf in leaves)
            leaf.Position=new Vector3(MathF.Sign(leaf.Position.X)*1.72f,leaf.Position.Y,leaf.Position.Z);
        var deck=car.GetNode<MeshInstance3D>("ELEVATOR_car_floor");
        check(MathF.Abs(ReviewBounds(deck).End.Y-2.7f)<.001f,"elevator_upper_car_floor_matches_upper_landing");
        motion.SetPositionNormalized(0);
        SceneComposer.ProjectServiceElevatorHoist(shaft);
        check(MathF.Abs(ReviewBounds(deck).End.Y-.6f)<.001f,"elevator_lower_car_floor_matches_lower_landing");
        motion.Run();motion._PhysicsProcess(1);
        check(motion.InputPositionNormalized==0,"elevator_generic_run_cannot_fabricate_direction_sequence");
        motion.ResetMotion();
        SceneComposer.ProjectServiceElevatorHoist(shaft);
        _sceneRuntime!.ResetSimulation();
        var runtime=_sceneRuntime;
        runtime.UsesExternalClock=true;
        runtime.SetControllerPlaybackRunning(true);
        void Command(bool up,bool down) => runtime.CommitVirtualControllerOutputs(new Dictionary<string,bool>
            { ["lift_up_cmd"]=up,["lift_down_cmd"]=down });
        void Tick(int n) { for(var i=0;i<n;i++) runtime.AdvanceSimulation(.02); }
        foreach(var name in new[]{"call_valid","doors_closed","landing_clear"}) runtime.ExecuteAction("toggle-"+name);
        Command(true,false);Tick(25);
        var partialDoor=leaves[0].Position;
        check(Math.Abs(partialDoor.X)> .55f && Math.Abs(partialDoor.X)<1.72f
            && Convert.ToDouble(runtime.Points["car_position_pct"])==0,
            "elevator_closing_doors_hold_car_despite_up_command");
        runtime.SetControllerPlaybackRunning(false);Tick(25);
        check(leaves[0].Position==partialDoor && Convert.ToDouble(runtime.Points["car_position_pct"])==0,
            "elevator_stop_freezes_partial_door_and_car");
        runtime.SetControllerPlaybackRunning(true);Tick(25);
        check(Math.Abs(Math.Abs(leaves[0].Position.X)-.55f)<.001
            && Convert.ToDouble(runtime.Points["car_position_pct"])==0,
            "elevator_completed_closure_precedes_first_car_displacement");
        Tick(150);
        check(Math.Abs(Convert.ToDouble(runtime.Points["car_position_pct"])-50)<.001
            && runtime.Points["at_lower_landing"] is false && runtime.Points["at_upper_landing"] is false,
            "elevator_single_up_command_reaches_mid_travel_with_matching_floor_flags");
        var held=car.Transform;
        Command(true,true);Tick(20);
        check(car.Transform==held && runtime.Points["direction_conflict"] is true
            && runtime.Points["lift_up_cmd"] is true && runtime.Points["lift_down_cmd"] is true,
            "elevator_opposing_commands_hold_and_remain_visible_for_diagnosis");
        Command(true,false);
        foreach(var name in new[]{"call_valid","doors_closed","landing_clear"})
        {
            runtime.ExecuteAction("toggle-"+name);Tick(20);
            check(car.Transform==held,"elevator_missing_"+name+"_holds_position");
            runtime.ExecuteAction("toggle-"+name);
        }
        runtime.SetControllerPlaybackRunning(false);Tick(20);
        check(car.Transform==held,"elevator_stopped_controller_clock_holds_position");
        runtime.SetControllerPlaybackRunning(true);Tick(200);
        check(runtime.Points["at_upper_landing"] is true && Math.Abs(Convert.ToDouble(runtime.Points["car_position_pct"])-100)<.001,
            "elevator_upper_endpoint_clamps_and_projects_feedback");
        Command(false,true);Tick(350);
        check(runtime.Points["at_lower_landing"] is true && Math.Abs(Convert.ToDouble(runtime.Points["car_position_pct"]))<.001,
            "elevator_down_command_returns_and_clamps_lower_endpoint");
        runtime.ResetSimulation();
        check(runtime.Points["at_lower_landing"] is true && runtime.Points["direction_conflict"] is false
            && runtime.Points["lift_up_cmd"] is false && runtime.Points["lift_down_cmd"] is false,
            "elevator_reset_restores_home_and_command_image");
        EnableVirtualControllerProgram(cycle.BuildProgram());
        try
        {
            RunActiveController();
            foreach(var name in new[]{"call_valid","doors_closed","landing_clear"}) ExecuteSelectedControllerAction("toggle-"+name);
            void Scans(int n) { for(var i=0;i<n;i++) _PhysicsProcess(.02); }
            Scans(350);
            check(runtime.Points["at_upper_landing"] is true,"elevator_qa_normal_controller_reaches_upper_landing");
            Scans(1);
            check(runtime.Points["lift_up_cmd"] is false && runtime.Points["lift_down_cmd"] is true,
                "elevator_qa_upper_feedback_selects_return_without_opposing_outputs");
            Scans(100); StopActiveController();
            var stopped=car.Transform; var scan=_virtualController!.Snapshot.ScanNumber;
            Scans(25);
            check(car.Transform==stopped && _virtualController.Snapshot.ScanNumber==scan,
                "elevator_qa_stop_retains_return_phase_and_car");
            RunActiveController(); Scans(205);
            check(runtime.Points["at_lower_landing"] is true && runtime.Points["lift_up_cmd"] is false
                && runtime.Points["lift_down_cmd"] is false,"elevator_qa_return_holds_home_with_call_still_valid");
            ResetActiveController();
            check(_virtualController.Snapshot.ScanNumber==0 && runtime.Points["at_lower_landing"] is true
                && runtime.Points["call_valid"] is false,"elevator_qa_reset_restores_inputs_and_home");
        }
        finally { DisableVirtualController(); }
    }
}
