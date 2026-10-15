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
    private bool _verifyWorkspaceRoller;
    // Native audit: uses the actual editor, saved joint project and accepted controller clock.
    // Geometry is measured at reset/home only; post-travel bearing follows that measured interval.
    private async void VerifyWorkspaceRoller()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"WORKSPACE_ROLLER_AUDIT {name}={ok}");if(!ok)failures++;}
        try
        {
            var core=new FiniteRollerTransport(2.525);var accelerated=core.Advance(core.InitialState,.75,true);
            Check(Math.Abs(accelerated.SpeedMps-.45)<1e-12&&Math.Abs(accelerated.DriveSurfaceTravelM-.16875)<1e-12,"core_analytic_ramp");
            var coast=core.Advance(accelerated,.75,false);
            Check(coast.SpeedMps==0&&Math.Abs(coast.DriveSurfaceTravelM-.3375)<1e-12,"core_analytic_coast");
            var large=core.Advance(core.InitialState,1000,true);
            Check(large.AtTerminal&&large.ClampedLoadTravelM==2.525&&large.DriveSurfaceTravelM>large.ClampedLoadTravelM&&large.SpeedMps==.45,"core_finite_load_only_clamp");
            foreach(var dt in new[]{.020,.040,.100})
            {
                var split=core.InitialState;for(var i=0;i<(int)Math.Round(2/dt);i++)split=core.Advance(split,dt,true);
                var whole=core.Advance(core.InitialState,2,true);
                Check(Math.Abs(split.DriveSurfaceTravelM-whole.DriveSurfaceTravelM)<1e-10&&Math.Abs(split.SpeedMps-whole.SpeedMps)<1e-10,"core_subdivision_"+dt);
            }
            var preset=WorkspaceFiniteRollerPolicy.Create();var p=preset.FiniteRollerPreset!;
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false);
            MarkWorkspaceSaved();
            _simulatorShell!.GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/FileMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,5L);
            Check(_workspaceFiniteRollerDocument?.FiniteRollerPreset==p&&_sceneRuntime!.HasWorkspaceRollerPreset,"actual_file_menu_creates_preset");
            Check(_activeSceneDefinition!.PlcTestProfile is null,"no_external_plc_profile");
            Check(_simulatorShell.VerifyExternalProfileGuards(out var external),"external_profile_guard_before_transport");GD.Print(external);
            foreach(var periodMs in new[]{20,40,100})
            foreach(var wrong in new[]{false,true})
            {
                var label=$"{periodMs}ms_{(wrong?"wrong":"intended")}";
                AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteRollerWorkspace:preset);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                var document=new LadderEditorDocument();document.ResetProject("roller-qa-"+label,"Roller_QA",TimeSpan.FromMilliseconds(periodMs));document.SourceSceneId=preset.SourceSceneId;
                document.AddTag("start",PlcVariableRole.Input,p.StartPoint);document.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);document.AddTag("run",PlcVariableRole.Output,p.RunPoint);
                document.AddRung("Momentary seal in","run");document.AddContact(0,0,"start",false);
                if(!wrong)document.AddContact(0,0,"beam",true);
                document.AddParallelBranch(0);document.AddContact(0,1,"run",false);
                if(!wrong)document.AddContact(0,1,"beam",true);
                var path=ProjectSettings.GlobalizePath("res://.tools/workspace-roller-"+label+".rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(document));
                Check(_simulatorShell!.OpenNativeMotionReviewProject(path,out var opened),"actual_editor_open_"+label);GD.Print(opened);
                var draft=_simulatorShell.CaptureIntegratedLadderDraft();
                var joint=ProjectSettings.GlobalizePath("res://.tools/workspace-roller-"+label+".rpworkspace.json");
                WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,CreateWorkspaceDocument(),draft),joint,_candidateCatalog!,_sceneCatalog!);
                MarkWorkspaceSaved();LoadWorkspaceFromPath(joint);
                Check(_simulatorShell.CaptureIntegratedLadderDraft()==draft&&CreateWorkspaceDocument().FiniteRollerPreset==p,"saved_actual_joint_reopens_"+label);
                var home=Vector(preset.Placements.Single(v=>v.InstanceId==p.RollerId).Position);
                TryTransformFiniteRollerWorkspace(p.RollerId,home+new Vector3(.4f,0,.3f),Vector3.Zero,Vector3.One);
                Check(preset.Placements.All(v=>_workspaceNodes[v.InstanceId].Node.Position.IsEqualApprox(Vector(v.Position)+new Vector3(.4f,0,.3f)))&&_simulatorShell.CaptureIntegratedLadderDraft()==draft,"whole_cell_translation_preserves_draft_"+label);
                UndoWorkspace();Check(_workspaceNodes[p.RollerId].Item2.Position.IsEqualApprox(home),"translation_undo_"+label);
                RedoWorkspace();Check(_workspaceNodes[p.RollerId].Item2.Position.IsEqualApprox(home+new Vector3(.4f,0,.3f)),"translation_redo_"+label);UndoWorkspace();
                var root=_sceneCompositionRoot!;var runtime=_sceneRuntime!;var geometry=SceneComposer.MeasureWorkspaceRoller(root);
                var adapter=root.GetNode<Node3D>(p.RollerId).GetNode<RollerConveyorController>("WorkspaceRuntimeAdapter");
                var pallet=root.GetNode<Node3D>(p.PalletId);
                var rollers=root.GetNode<Node3D>(p.RollerId).FindChildren("KIN_roller_*","Node3D",true,false).OfType<Node3D>().ToArray();
                var rollerHomes=rollers.Select(n=>n.Transform).ToArray();var palletHome=pallet.Position;
                Check(rollers.Length==31&&!adapter.IsPhysicsProcessing()&&runtime.Points.Count==3,"31_rollers_three_points_one_clock_"+label);
                Check(_simulatorShell.VerifyDeviceTraceEquipmentInventory(p.RollerId,p.SensorId),"actual_device_inventory_"+label);
                RunEditorDraftController();Check(_virtualController!.Snapshot.State==VirtualControllerState.Running,"current_draft_runs_"+label);
                var head=(MeshInstance3D)_sceneControlInteractor!.Bindings.Single().Node;var center=head.GlobalTransform*head.GetAabb().GetCenter();
                Check(_sceneControlInteractor.TryPick(_mainCamera!,_mainCamera!.UnprojectPosition(center),out var picked)&&picked?.ActionId=="roller-workspace-press","physical_head_pick_"+label);
                Check(ExecuteSelectedControllerAction("roller-workspace-press"),"actual_start_action_"+label);
                var seconds=periodMs/1000.0;
                void Scan(double elapsed)=>_virtualController!.Advance(elapsed,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,runtime.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
                Scan(seconds);Check(_virtualController.Snapshot.Variables["start"]&&runtime.Points[p.RunPoint] is true,"physical_start_sample_"+label);
                Scan(seconds);Check(!_virtualController.Snapshot.Variables["start"]&&runtime.Points[p.RunPoint] is true,"released_start_seal_in_"+label);
                var count=0;while(!runtime.CaptureWorkspaceRollerPreset().BeamBlocked&&count++<1000)Scan(seconds);
                var trigger=runtime.CaptureWorkspaceRollerPreset();var triggerScan=_virtualController.Snapshot.ScanNumber;
                Check(trigger.BeamBlocked&&!_virtualController.Snapshot.Variables["beam"]&&trigger.SpeedMps>0,"real_wood_poststep_old_sample_false_"+label);
                Scan(seconds);var after=runtime.CaptureWorkspaceRollerPreset();
                Check(_virtualController.Snapshot.ScanNumber==triggerScan+1&&_virtualController.Snapshot.Variables["beam"]&&Equals(runtime.Points[p.RunPoint],wrong)&&after.SpeedMps>0,"exact_next_sample_command_then_coast_"+label);
                Check(_simulatorShell.VerifyDeviceTraceScalar(p.SensorId,"beam",_virtualController.Snapshot,!wrong)&&_simulatorShell.VerifyDeviceTraceScalar(p.RollerId,"run",_virtualController.Snapshot,true)&&_simulatorShell.VerifyDeviceTraceSelector(p.RollerId,true),"accepted_reader_writer_navigation_"+label);
                var scanBefore=_virtualController.Snapshot.ScanNumber;Scan(.5);
                Check(_virtualController.Snapshot.ScanNumber==scanBefore+500/periodMs,"elapsed_subdivides_accepted_scans_"+label);
                for(var i=0;i<Math.Ceiling(12/seconds);i++)Scan(seconds);
                var settled=runtime.CaptureWorkspaceRollerPreset();
                Check(_workspaceRollerObservedTrial?.State==(wrong?FiniteRollerTrialState.NeedsAttention:FiniteRollerTrialState.Observed),"dedicated_roller_observer_result_"+label);
                var signedAngle=(float)(settled.DriveSurfaceTravelM/geometry.RollerRadius)*geometry.PhaseSign;
                var phaseRotation=new Basis(Vector3.Up,signedAngle);
                var phaseError=rollers.Select((n,i)=>{
                    var expected=rollerHomes[i].Basis*phaseRotation;var actual=n.Transform.Basis;
                    return Math.Max((expected.X-actual.X).Length(),Math.Max((expected.Y-actual.Y).Length(),(expected.Z-actual.Z).Length()));
                }).Max();
                Check(phaseError<.001&&Math.Abs(settled.Center-(geometry.HomeCenter-settled.ClampedLoadTravelM))<1e-5,"all_31_actual_phases_drive_distance_radius_and_load_projection_"+label);
                Check(settled.Center>=geometry.TerminalCenter-1e-5&&settled.Center<=geometry.HomeCenter+1e-5&&Math.Abs(pallet.Position.Y-geometry.CrownY)<.001,"measured_supported_position_"+label);
                if(wrong)
                {
                    var phases=rollers.Select(n=>n.Transform).ToArray();var distance=settled.DriveSurfaceTravelM;var load=settled.ClampedLoadTravelM;Scan(.5);var endpoint=runtime.CaptureWorkspaceRollerPreset();
                    Check(settled.AtTerminal&&runtime.Points[p.RunPoint] is true&&Math.Abs(endpoint.SpeedMps-.45)<1e-8&&endpoint.DriveSurfaceTravelM>distance&&endpoint.ClampedLoadTravelM==load&&!rollers.Select(n=>n.Transform).SequenceEqual(phases),"wrong_endpoint_retains_drive_advancing_phase_fixed_load_"+label);
                }
                else
                {
                    Check(!settled.AtTerminal&&settled.SpeedMps==0&&runtime.Points[p.RunPoint] is false,"intended_coast_then_supported_settle_"+label);
                    Check(trigger.Center-settled.Center<=trigger.SpeedMps*trigger.SpeedMps/(2*.60)+1e-5,"coast_distance_no_extra_command_scan_"+label);
                    var stationary=pallet.Position;Scan(.5);Check(pallet.Position==stationary,"supported_half_second_hold_"+label);
                    GD.Print($"WORKSPACE_ROLLER_BEAM_HISTORY {label} trigger=True current={settled.BeamBlocked} center={settled.Center:R} speed={settled.SpeedMps:R}");
                }
                var historicalTrial=_workspaceRollerObservedTrial;
                StopActiveController();var frozen=_virtualController.Snapshot;var frozenLoad=pallet.Position;var frozenRollers=rollers.Select(n=>n.Transform).ToArray();
                if(wrong)Check(historicalTrial?.State==FiniteRollerTrialState.NeedsAttention&&_workspaceRollerObservedTrial?.State==historicalTrial.State&&_workspaceRollerObservedTrial.Detail==historicalTrial.Detail&&_workspaceRollerObservedTrial.BeamTriggerScanNumber==historicalTrial.BeamTriggerScanNumber&&_workspaceRollerObservedTrial.LastScanNumber==historicalTrial.LastScanNumber&&_workspaceRollerObservedTrial.LastTimeSeconds==historicalTrial.LastTimeSeconds,"wrong_terminal_stop_retains_outcome_detail_trigger_scan_time_"+label);
                Check(adapter.ActualSpeedMps==0&&!adapter.RunCommand,"manual_stop_immediate_speed_zero_"+label);
                Scan(.5);runtime.AdvanceSimulation(.5);
                Check(_virtualController.Snapshot.ScanNumber==frozen.ScanNumber&&_virtualController.Snapshot.SimulatedTime==frozen.SimulatedTime&&pallet.Position==frozenLoad&&rollers.Select(n=>n.Transform).SequenceEqual(frozenRollers),"stop_freezes_scan_time_actual_load_phase_"+label);
                ResetActiveController();Check(pallet.Position==palletHome&&rollers.Select(n=>n.Transform).SequenceEqual(rollerHomes)&&adapter.ActualSpeedMps==0&&!runtime.CaptureWorkspaceRollerPreset().BeamBlocked&&runtime.Points[p.RunPoint] is false&&!adapter.IsPhysicsProcessing(),"reset_exact_home_31_transforms_beam_command_clock_"+label);
                if(wrong&&periodMs==20)
                {
                    RunEditorDraftController();ExecuteSelectedControllerAction("roller-workspace-press");for(var i=0;i<20;i++)Scan(seconds);
                    Check(adapter.ActualSpeedMps>0&&pallet.Position!=palletHome,"invalid_fixture_actually_midtravel");
                    var invalid=new LadderEditorDocument();invalid.ResetProject("roller-invalid","Invalid_Current",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=preset.SourceSceneId;
                    File.WriteAllText(path,LadderEditorProjectJson.Save(invalid));var invalidPosition=pallet.Position;
                    Check(_simulatorShell.OpenNativeMotionReviewProject(path,out opened)&&pallet.Position==invalidPosition,"invalid_current_open_preserves_midtravel");
                    RunEditorDraftController();Check(_virtualController.Snapshot.State!=VirtualControllerState.Running&&adapter.ActualSpeedMps==0&&runtime.Points[p.RunPoint] is false,"invalid_current_no_old_program_fallback");
                    var invalidPhase=rollers.Select(n=>n.Transform).ToArray();var invalidSnapshot=_virtualController.Snapshot;Scan(.5);runtime.AdvanceSimulation(.5);
                    Check(pallet.Position==invalidPosition&&rollers.Select(n=>n.Transform).SequenceEqual(invalidPhase)&&_virtualController.Snapshot.ScanNumber==invalidSnapshot.ScanNumber,"invalid_freezes_actual_load_phase_clock");
                    var invalidDraft=_simulatorShell.CaptureIntegratedLadderDraft();WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,CreateWorkspaceDocument(),invalidDraft),joint,_candidateCatalog!,_sceneCatalog!);MarkWorkspaceSaved();LoadWorkspaceFromPath(joint);
                    Check(_simulatorShell.CaptureIntegratedLadderDraft()==invalidDraft&&CreateWorkspaceDocument().FiniteRollerPreset==p,"joint_invalid_exact_json_reopen");RunEditorDraftController();Check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"reopened_invalid_stays_stopped");
                }
            }
            VerifyWorkspaceRollerBindings(Check);
            await VerifyWorkspaceRollerRollback(Check);
            await VerifyWorkspaceRollerDraftCache(Check);
            await VerifyWorkspaceRollerObserver(Check);
            var layout=System.Text.Json.JsonSerializer.Serialize(CreateWorkspaceDocument());var retainedDraft=_simulatorShell!.CaptureIntegratedLadderDraft();
            foreach(var bad in new[]{
                preset with{FiniteRollerPreset=p with{BeamPoint=p.RunPoint}},
                preset with{Placements=preset.Placements.Select(v=>v.InstanceId==p.PalletId?v with{AssetId=WorkspaceFiniteRollerPolicy.RollerAsset}:v).ToArray()},
                preset with{Placements=preset.Placements.Select(v=>v.InstanceId==p.SensorId?v with{Position=[20,.398,0]}:v).ToArray()},
                preset with{SignalLinks=(preset.SignalLinks??[]).Take(2).ToArray()},
                preset with{SignalLinks=(preset.SignalLinks??[]).Concat([(preset.SignalLinks??[])[0]]).ToArray()}})
            {
                var rejected=false;try{WorkspaceRepository.ValidateDocument(bad,_candidateCatalog!,_sceneCatalog!);}catch(Exception error)when(error is InvalidOperationException or ArgumentException){rejected=true;}
                Check(rejected&&layout==System.Text.Json.JsonSerializer.Serialize(CreateWorkspaceDocument())&&retainedDraft==_simulatorShell.CaptureIntegratedLadderDraft(),"malformed_asset_pose_owner_missing_duplicate_rejected_without_mutation");
            }
        }
        catch(Exception error){GD.PushError("WORKSPACE_ROLLER_AUDIT_EXCEPTION "+error);failures++;}
        GD.Print("WORKSPACE_ROLLER_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
}
