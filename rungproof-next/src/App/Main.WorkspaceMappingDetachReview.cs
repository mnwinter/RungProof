using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyWorkspaceMappingDetach;
    // Red reproducer only: real generic assets and Main graph edits. Output projection is a
    // declared-point QA fixture, not a compiled learner-program or finite-preset acceptance.
    private async void VerifyWorkspaceMappingDetach()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"WORKSPACE_MAPPING_DETACH {name}={ok}");if(!ok)failures++;}
        try
        {
            foreach(var assetId in new[]{"material-handling.belt-conveyor.600x6000.v1","material-handling.pallet-roller-conveyor.1000x4000.v1"})
            {
                AddMigratedScene("lab-2-23-parcel-sorter",_candidateCatalog!,_mainCamera!,false,false);
                PlaceAsset(_candidateCatalog!.Assets.Single(a=>a.Id==assetId));
                var id=_workspaceNodes.Keys.Single();
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Node Adapter()=>_workspaceNodes[id].Node.GetNode<Node>("WorkspaceRuntimeAdapter");
                bool Run()=>Adapter() is ConveyorController b?b.RunCommand:((RollerConveyorController)Adapter()).RunCommand;
                bool Estop()=>Adapter() is ConveyorController b?b.EstopOk:((RollerConveyorController)Adapter()).EstopOk;
                float Speed()=>Adapter() is ConveyorController b?b.SpeedSetpointMps:((RollerConveyorController)Adapter()).SpeedSetpointMps;
                float Actual()=>Adapter() is ConveyorController b?b.ActualSpeedMps:((RollerConveyorController)Adapter()).ActualSpeedMps;
                var label=assetId.Contains("pallet")?"roller":"belt";
                var defaultSpeed=Speed();Check(!Run()&&Estop()&&defaultSpeed>0,"never_mapped_defaults_"+label);
                void Project(){_sceneRuntime!.CommitVirtualControllerOutputs(new Dictionary<string,bool>{{"conveyors_run",true}});_sceneRuntime.CommitVirtualControllerNumericOutputs(new Dictionary<string,double>{{"route_position",.4}});ApplySignalMappings();}
                CreateSignalLink(id,"run_command","conveyors_run");CreateSignalLink(id,"estop_ok","conveyors_run");CreateSignalLink(id,"speed_setpoint","route_position");Project();
                Check(Run()&&Estop()&&Math.Abs(Speed()-.4)<1e-5,"three_inputs_live_"+label);
                var count=_signalLinks.Count;CreateSignalLink(id,"run_command","large_detected");Check(_signalLinks.Count==count,"competing_target_mapping_rejected_"+label);
                var runLink=_signalLinks.Single(l=>l.SignalId=="run_command").Id;
                for(var i=0;i<35;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                var before=Actual();Check(before>0,"actual_generic_physics_driven_"+label);
                DeleteSignalLink(runLink);
                Check(!Run()&&Estop()&&Math.Abs(Speed()-.4)<1e-5,"detach_run_clears_only_former_live_input_"+label);
                for(var i=0;i<60;i++)await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
                Check(Actual()<before&&Actual()<.01,"detach_run_actual_coast_to_stop_"+label);
                UndoWorkspace();Project();Check(Run()&&_signalLinks.Any(l=>l.Id==runLink),"undo_restores_exact_link_and_current_value_"+label);
                RedoWorkspace();Check(!Run()&&!_signalLinks.Any(l=>l.Id==runLink),"redo_detaches_again_"+label);
                CreateSignalLink(id,"run_command","conveyors_run");Project();Check(Run(),"rebind_current_command_"+label);
                DeleteSignalLink(_signalLinks.Single(l=>l.SignalId=="speed_setpoint").Id);
                Check(Speed()==0&&Run()&&Estop(),"detach_speed_zero_other_live_values_retained_"+label);
                UndoWorkspace();Project();Check(Math.Abs(Speed()-.4)<1e-5,"undo_speed_restores_current_map_"+label);
                var mappedRun=_signalLinks.Single(l=>l.SignalId=="run_command");var index=_signalLinks.IndexOf(mappedRun);
                _signalLinks[index]=mappedRun with{PointName="__qa_missing_point"};
                var invalidStatus=ApplySignalMappings()[mappedRun.Id];
                Check(!Run()&&invalidStatus.State==SignalMappingRuntimeState.AuthoredOnly,"formerly_live_source_unavailable_clears_command_"+label);
                _signalLinks[index]=mappedRun;Project();
                DeleteSignalLink(_signalLinks.Single(l=>l.SignalId=="estop_ok").Id);
                Check(!Estop()&&Run(),"detach_permissive_false_run_mapping_retained_"+label);
                _sceneRuntime!.StopSimulation();ApplySignalMappings();Check(!Run(),"scene_stop_point_projection_separate_"+label);
                _sceneRuntime.ResetSimulation();ApplySignalMappings();Check(!Run(),"scene_reset_point_projection_separate_"+label);
                DeletePlacement(id);ApplySignalMappings();Check(!_workspaceNodes.ContainsKey(id),"removed_target_safely_evicted_"+label);
                PlaceAsset(_candidateCatalog.Assets.Single(a=>a.Id==assetId));
                var untouched=_workspaceNodes.Single().Value.Node;
                CreateSignalLink(_workspaceNodes.Keys.Single(),"running","large_detected");
                var authored=ApplySignalMappings().Values.Single();
                var adapter=untouched.GetNode<Node>("WorkspaceRuntimeAdapter");
                var defaults=adapter is ConveyorController b?b.EstopOk&&b.SpeedSetpointMps==.65f:((RollerConveyorController)adapter).EstopOk&&((RollerConveyorController)adapter).SpeedSetpointMps==.45f;
                Check(authored.State==SignalMappingRuntimeState.AuthoredOnly&&defaults&&!_genericLiveInputs.Keys.Any(k=>k.NodeId==untouched.GetInstanceId()),"metadata_only_never_acquires_live_ownership_"+label);
            }
        }
        catch(Exception error){failures++;GD.PushError("WORKSPACE_MAPPING_DETACH_FAILED "+error);}
        GD.Print("WORKSPACE_MAPPING_DETACH_DONE failures="+failures);GetTree().Quit(failures==0?0:1);
    }
}
