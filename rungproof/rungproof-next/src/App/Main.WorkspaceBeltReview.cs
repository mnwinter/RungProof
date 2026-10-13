using System;
using System.IO;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;

namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyWorkspaceBelt;
    private async void VerifyWorkspaceBelt()
    {
        var failures=0;
        void Check(bool value,string name){GD.Print($"WORKSPACE_BELT_AUDIT {name}={value}");if(!value)failures++;}
        try
        {
            var preset=WorkspaceFiniteBeltPolicy.Create();var p=preset.FiniteBeltPreset!;
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false);
            MarkWorkspaceSaved();
            _simulatorShell!.GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/FileMenu").GetPopup()
                .EmitSignal(PopupMenu.SignalName.IdPressed,4L);
            Check(_workspaceFiniteBeltDocument?.FiniteBeltPreset==p&&_sceneRuntime!.HasWorkspaceBeltPreset,
                "real_file_menu_creates_user_accessible_preset");
            VerifyWorkspaceBeltBindings(Check);
            foreach(var wrong in new[]{false,true})
            {
                AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:preset);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var root=_sceneCompositionRoot!;var runtime=_sceneRuntime!;
                var geometry=SceneComposer.MeasureWorkspaceBelt(root);
                Check(Math.Abs(geometry.Carton.Position.Y-geometry.Belt.End.Y)<.015,"actual_initial_carton_bears_on_belt_deck");
                Check(_simulatorShell!.VerifyDeviceTraceEquipmentInventory(p.BeltId,p.SensorId),"actual_device_inventory_replaces_same_source_scene_cache");
                var adapter=root.GetNode<Node3D>(p.BeltId).GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                Check(!adapter.IsPhysicsProcessing(),"belt_autonomy_disabled_after_ready");
                Check(runtime.RuntimeType=="workspaceFiniteBelt"&&runtime.Points.Count==3
                    &&root.GetNodeOrNull("scene1_conveyor") is null,"original_source_model_points_nodes_replaced");
                var doc=new LadderEditorDocument();doc.ResetProject(wrong?"workspace-wrong-qa":"workspace-intended-qa","FiniteBelt_QA",TimeSpan.FromMilliseconds(20));
                doc.SourceSceneId=preset.SourceSceneId;
                doc.AddTag("start",PlcVariableRole.Input,p.StartPoint);doc.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);
                doc.AddTag("run",PlcVariableRole.Output,p.RunPoint);
                doc.AddRung("Momentary seal in","run");doc.AddContact(0,0,"start",false);
                if(!wrong)doc.AddContact(0,0,"beam",true);
                doc.AddParallelBranch(0);doc.AddContact(0,1,"run",false);
                if(!wrong)doc.AddContact(0,1,"beam",true);
                var path=ProjectSettings.GlobalizePath("res://.tools/workspace-belt-qa.rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(doc));
                Check(_simulatorShell!.OpenNativeMotionReviewProject(path,out var opened),"real_shell_open_current_draft");GD.Print(opened);
                var validDraft=_simulatorShell.CaptureIntegratedLadderDraft();
                var validBundlePath=ProjectSettings.GlobalizePath("res://.tools/workspace-belt-valid-qa.rpworkspace.json");
                WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,CreateWorkspaceDocument(),validDraft),validBundlePath,_candidateCatalog!,_sceneCatalog!);
                MarkWorkspaceSaved();LoadWorkspaceFromPath(validBundlePath);
                Check(_workspaceFiniteBeltDocument?.FiniteBeltPreset==p&&_workspaceNodes.Count==4
                    &&_simulatorShell.CaptureIntegratedLadderDraft()==validDraft,"valid_joint_reopen_before_behavior_preserves_actual_draft");
                var beltHome=Vector(preset.Placements.Single(v=>v.InstanceId==p.BeltId).Position);
                TryTransformFiniteBeltWorkspace(p.BeltId,beltHome+new Vector3(.4f,0,.3f),Vector3.Zero,Vector3.One);
                Check(preset.Placements.All(v=>_workspaceNodes[v.InstanceId].Node.Position.IsEqualApprox(Vector(v.Position)+new Vector3(.4f,0,.3f)))
                    &&_simulatorShell.CaptureIntegratedLadderDraft()==validDraft,"whole_cell_translate_preserves_custom_draft");
                UndoWorkspace();Check(_workspaceNodes[p.BeltId].Item2.Position.IsEqualApprox(beltHome),"whole_cell_translation_undo");
                RedoWorkspace();Check(_workspaceNodes[p.BeltId].Item2.Position.IsEqualApprox(beltHome+new Vector3(.4f,0,.3f)),"whole_cell_translation_redo");
                UndoWorkspace();
                root=_sceneCompositionRoot!;runtime=_sceneRuntime!;geometry=SceneComposer.MeasureWorkspaceBelt(root);
                adapter=root.GetNode<Node3D>(p.BeltId).GetNode<ConveyorController>("WorkspaceRuntimeAdapter");
                var initialDrumHomes=root.GetNode<Node3D>(p.BeltId).FindChildren("KIN_*drum*","Node3D",true,false).OfType<Node3D>().Select(n=>n.Transform).ToArray();
                RunEditorDraftController();Check(_virtualController!.Snapshot.State==VirtualControllerState.Running,"valid_current_draft_runs");
                var binding=_sceneControlInteractor!.Bindings.Single();
                var head=(MeshInstance3D)binding.Node;var center=head.GlobalTransform*head.GetAabb().GetCenter();
                Check(_sceneControlInteractor.TryPick(_mainCamera!,_mainCamera!.UnprojectPosition(center),out var picked)
                    &&picked?.ActionId=="workspace-press","actual_momentary_head_ray_pick");
                Check(ExecuteSelectedControllerAction("workspace-press")&&runtime.Points[p.StartPoint] is true,"real_momentary_action_accepted");
                void Scan(double seconds=.020)=>_virtualController!.Advance(seconds,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
                    CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,runtime.AdvanceSimulation,
                    SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
                Scan();Check(_virtualController.Snapshot.Variables["start"]&&runtime.Points[p.RunPoint] is true,"first_scan_consumes_physical_start");
                Scan();Check(!_virtualController.Snapshot.Variables["start"]&&runtime.Points[p.RunPoint] is true,"momentary_released_seal_in_retains_run");
                var count=0;while(runtime.Points[p.BeamPoint] is not true&&count++<900)Scan();
                Check(runtime.Points[p.BeamPoint] is true&&!_virtualController.Snapshot.Variables["beam"],"actual_intersection_published_after_scan_old_input_false");
                Scan();Check(_virtualController.Snapshot.Variables["beam"]&&Equals(runtime.Points[p.RunPoint],wrong),"next_scan_feedback_changes_only_intended_program");
                var traceScan=_virtualController.Snapshot.ScanNumber;
                Check(_simulatorShell.VerifyDeviceTraceScalar(p.SensorId,"beam",_virtualController.Snapshot,!wrong)
                    &&(wrong||_simulatorShell.VerifyDeviceTraceSelector(p.SensorId,true))
                    &&_simulatorShell.VerifyDeviceTraceScalar(p.BeltId,"run",_virtualController.Snapshot,true)
                    &&_simulatorShell.VerifyDeviceTraceSelector(p.BeltId,true)
                    &&_virtualController.Snapshot.ScanNumber==traceScan,"real_reader_writer_navigation_preserves_accepted_snapshot");
                var largeBefore=_virtualController.Snapshot.ScanNumber;
                Scan(.5);
                Check(_virtualController.Snapshot.ScanNumber==largeBefore+25,"elapsed_half_second_subdivides_into_25_accepted_scans");
                for(var i=0;i<900;i++)Scan();
                var state=runtime.CaptureWorkspaceBeltPreset();
                Check(wrong?state.AtTerminal&&runtime.Points[p.RunPoint] is true:!state.AtTerminal&&state.BeamBlocked,
                    wrong?"valid_wrong_runs_to_finite_endpoint":"intended_stops_on_real_beam");
                var carton=root.GetNode<Node3D>(p.CartonId);
                var shifted=geometry.Carton with{Position=geometry.Carton.Position+carton.Position-Vector(preset.Placements.Single(v=>v.InstanceId==p.CartonId).Position)};
                Check(shifted.Position.X>=geometry.Belt.Position.X&&shifted.End.X<=geometry.Belt.End.X
                    &&shifted.Position.Z>=geometry.Belt.Position.Z&&shifted.End.Z<=geometry.Belt.End.Z
                    &&Math.Abs(shifted.Position.Y-geometry.Belt.End.Y)<.015,"actual_load_fully_supported_terminal_or_stop");
                var belt=root.GetNode<Node3D>(p.BeltId);
                var drums=belt.FindChildren("KIN_*drum*","Node3D",true,false).OfType<Node3D>().ToArray();
                var surfaces=belt.FindChildren("KIN_belt_surface*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
                    .SelectMany(m=>Enumerable.Range(0,m.Mesh.GetSurfaceCount()).Select(i=>m.GetSurfaceOverrideMaterial(i)))
                    .OfType<StandardMaterial3D>().ToArray();
                Check(drums.Length>0&&surfaces.Length>0,"actual_drum_and_instance_uv_surfaces_bound");
                var before=carton.Position;var commandBeforeStop=runtime.Points[p.RunPoint];var snapshotRunBeforeStop=_virtualController.Snapshot.Variables["run"];StopActiveController();var stopped=adapter.ActualSpeedMps;
                GD.Print($"WORKSPACE_BELT_STOP speed={stopped:R} adapterRun={adapter.RunCommand} pointBefore={commandBeforeStop} pointAfter={runtime.Points[p.RunPoint]} snapshotRunBefore={snapshotRunBeforeStop} snapshotRunAfter={_virtualController.Snapshot.Variables["run"]}");
                Check(stopped==0&&!adapter.RunCommand&&runtime.Points[p.RunPoint] is false&&!_virtualController.Snapshot.Variables["run"],"stop_visual_speed_zero_and_existing_controller_output_clear_contract");
                var stoppedSnapshot=_virtualController.Snapshot;
                var stoppedDrums=drums.Select(n=>n.Transform).ToArray();var stoppedUvs=surfaces.Select(m=>m.Uv1Offset).ToArray();
                Scan(.5);runtime.AdvanceSimulation(.5);
                Check(carton.Position==before&&adapter.ActualSpeedMps==stopped,"stop_rejects_clock_and_freezes_actual_plant");
                Check(_virtualController.Snapshot.ScanNumber==stoppedSnapshot.ScanNumber
                    &&_virtualController.Snapshot.SimulatedTime==stoppedSnapshot.SimulatedTime
                    &&drums.Select(n=>n.Transform).SequenceEqual(stoppedDrums)
                    &&surfaces.Select(m=>m.Uv1Offset).SequenceEqual(stoppedUvs),"stop_freezes_scan_time_actual_drum_and_uv");
                ResetActiveController();
                Check(surfaces.All(m=>m.Uv1Offset==Vector3.Zero)&&adapter.ActualSpeedMps==0&&drums.Select(n=>n.Transform).SequenceEqual(initialDrumHomes),"reset_actual_belt_uv_and_speed");
                Check(carton.Position.IsEqualApprox(Vector(preset.Placements.Single(v=>v.InstanceId==p.CartonId).Position))
                    &&runtime.Points[p.StartPoint] is false&&runtime.Points[p.RunPoint] is false&&!runtime.CaptureWorkspaceBeltPreset().BeamBlocked,
                    "reset_actual_home_command_and_sensor_image");
                Check(!adapter.IsPhysicsProcessing(),"run_stop_reset_never_enables_second_clock");
                if(wrong)
                {
                    RunEditorDraftController();
                    Check(ExecuteSelectedControllerAction("workspace-press"),"restart_wrong_with_real_momentary_action_before_invalid_edit");
                    for(var i=0;i<20;i++)Scan();
                    Check(_virtualController.Snapshot.State==VirtualControllerState.Running&&adapter.ActualSpeedMps>0
                        &&!carton.Position.IsEqualApprox(Vector(preset.Placements.Single(v=>v.InstanceId==p.CartonId).Position)),
                        "invalid_edit_fixture_begins_running_midtravel");
                    var runningPosition=carton.Position;
                    var invalid=new LadderEditorDocument();invalid.ResetProject("invalid-belt-qa","Invalid_Current",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=preset.SourceSceneId;
                    File.WriteAllText(path,LadderEditorProjectJson.Save(invalid));
                    Check(_simulatorShell.OpenNativeMotionReviewProject(path,out opened),"readable_invalid_current_open_preserved");
                    var invalidHome=carton.Position;
                    Check(invalidHome==runningPosition,"invalid_draft_open_preserves_actual_midtravel_carton");
                    var invalidDrums=drums.Select(n=>n.Transform).ToArray();var invalidUvs=surfaces.Select(m=>m.Uv1Offset).ToArray();
                    RunEditorDraftController();Check(_virtualController.Snapshot.State!=VirtualControllerState.Running,"invalid_current_cannot_run_old_program");
                    Check(runtime.Points[p.RunPoint] is false,"invalid_run_clears_previous_plant_command");
                    var invalidSnapshot=_virtualController.Snapshot;
                    Scan(.5);runtime.AdvanceSimulation(.5);
                    Check(_virtualController.Snapshot.ScanNumber==invalidSnapshot.ScanNumber&&_virtualController.Snapshot.SimulatedTime==invalidSnapshot.SimulatedTime
                        &&drums.Select(n=>n.Transform).SequenceEqual(invalidDrums)&&surfaces.Select(m=>m.Uv1Offset).SequenceEqual(invalidUvs),"invalid_freezes_scan_time_drum_uv");
                    Check(carton.Position==invalidHome,"invalid_current_freezes_actual_carton_without_old_program_fallback");
                    var bundlePath=ProjectSettings.GlobalizePath("res://.tools/workspace-belt-qa.rpworkspace.json");
                    var draft=_simulatorShell.CaptureIntegratedLadderDraft();
                    WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,CreateWorkspaceDocument(),draft),bundlePath,_candidateCatalog!,_sceneCatalog!);
                    MarkWorkspaceSaved();LoadWorkspaceFromPath(bundlePath);
                    Check(_workspaceFiniteBeltDocument?.FiniteBeltPreset==p&&_workspaceNodes.Count==4
                        &&_simulatorShell.CaptureIntegratedLadderDraft()==draft,"joint_bundle_preserves_ids_relation_and_invalid_draft");
                    RunEditorDraftController();Check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"reopened_invalid_draft_stays_invalid_stopped");
                }
            }
            await VerifyWorkspaceBeltRollback(Check);
            await VerifyWorkspaceObservedTrial(Check);
            await VerifyWorkspaceBeltDraftCache(Check);
            var bad=preset with{Placements=preset.Placements.Select(v=>v.InstanceId==p.SensorId?v with{Position=[20,.25,0]}:v).ToArray()};
            try{WorkspaceRepository.ValidateDocument(bad,_candidateCatalog!,_sceneCatalog!);Check(false,"unsupported_sensor_pose_rejected_before_replace");}
            catch(InvalidOperationException){Check(true,"unsupported_sensor_pose_rejected_before_replace");}
            var preservedWorkspace=System.Text.Json.JsonSerializer.Serialize(CreateWorkspaceDocument());
            var preservedDraft=_simulatorShell!.CaptureIntegratedLadderDraft();
            foreach(var malformed in new[]{
                preset with{FiniteBeltPreset=p with{BeamPoint=p.RunPoint}},
                preset with{Placements=preset.Placements.Take(3).ToArray()},
                preset with{Placements=preset.Placements.Select(v=>v.InstanceId==p.SensorId?v with{InstanceId=p.BeltId}:v).ToArray()},
                preset with{SignalLinks=(preset.SignalLinks??[]).Where(v=>v.InstanceId!=p.SensorId).ToArray()}})
            {
                var rejected=false;try{WorkspaceRepository.ValidateDocument(malformed,_candidateCatalog!,_sceneCatalog!);}
                catch(Exception error)when(error is InvalidOperationException or ArgumentException){rejected=true;}
                Check(rejected&&System.Text.Json.JsonSerializer.Serialize(CreateWorkspaceDocument())==preservedWorkspace
                    &&_simulatorShell.CaptureIntegratedLadderDraft()==preservedDraft,"malformed_relation_rejected_without_workspace_or_draft_mutation");
            }
            var pure=new WorkspaceBeltPlantModel(6,.5525,.18);
            Check(pure.BeamBlocked,"nonzero_initial_beam_occupancy_not_forced_false");pure.Step(1000,true);
            Check(pure.AtTerminal&&pure.TrailingEdge>=0&&pure.LeadingEdge<=6,"analytic_large_step_finite_endpoint_no_drop");
        }
        catch(Exception error){GD.PushError("WORKSPACE_BELT_AUDIT_EXCEPTION "+error);failures++;}
        GD.Print("WORKSPACE_BELT_AUDIT_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
}
