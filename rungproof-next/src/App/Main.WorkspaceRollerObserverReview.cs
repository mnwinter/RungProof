using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private async Task VerifyWorkspaceRollerObserver(Action<bool,string> check)
    {
        try
        {
            foreach(var ms in new[]{20,40,100})
            foreach(var wrong in new[]{false,true})
            {
                List<string>? baseline=null;
                foreach(var enabled in new[]{false,true})
                {
                    _workspaceRollerObservedTrialEnabled=enabled;
                    var preset=WorkspaceFiniteRollerPolicy.Create();var p=preset.FiniteRollerPreset!;
                    AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteRollerWorkspace:preset);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    // Reopen the actual joint drafts already created by the core audit.
                    var label=$"{ms}ms_{(wrong?"wrong":"intended")}";
                    var joint=ProjectSettings.GlobalizePath("res://.tools/workspace-roller-"+label+".rpworkspace.json");
                    // The invalid-midtravel test overwrites only wrong20's joint. Restore its valid editor draft.
                    if(wrong&&ms==20)
                    {
                        joint=ProjectSettings.GlobalizePath("res://.tools/workspace-roller-observer-wrong20.rpworkspace.json");
                        var doc=new LadderEditorDocument();doc.ResetProject("roller-qa-"+label,"Roller_QA",TimeSpan.FromMilliseconds(ms));doc.SourceSceneId=preset.SourceSceneId;
                        doc.AddTag("start",PlcVariableRole.Input,p.StartPoint);doc.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);doc.AddTag("run",PlcVariableRole.Output,p.RunPoint);
                        doc.AddRung("Momentary seal in","run");doc.AddContact(0,0,"start",false);doc.AddParallelBranch(0);doc.AddContact(0,1,"run",false);
                        WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,preset,LadderEditorProjectJson.Save(doc)),joint,_candidateCatalog!,_sceneCatalog!);
                    }
                    MarkWorkspaceSaved();LoadWorkspaceFromPath(joint);RunEditorDraftController();ResetActiveController();RunEditorDraftController();
                    var session=_virtualController!;var runtime=_sceneRuntime!;var root=_sceneCompositionRoot!;
                    var rollers=root.GetNode<Node3D>(p.RollerId).FindChildren("KIN_roller_*","Node3D",true,false).OfType<Node3D>().ToArray();
                    var trace=new List<string>();VirtualControllerScanCompleted? last=null;
                    void Accepted(VirtualControllerScanCompleted row)
                    {
                        last=row;
                        trace.Add(JsonSerializer.Serialize(row)+"|"+JsonSerializer.Serialize(runtime.CaptureWorkspaceRollerPreset())+"|"+string.Join(";",rollers.Select(n=>n.Transform.ToString())));
                    }
                    void Scan()=>session.Advance(ms/1000d,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,runtime.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
                    session.ScanCompleted+=Accepted;
                    try
                    {
                        check(ExecuteSelectedControllerAction("roller-workspace-press"),"observer_real_start_"+label);
                        for(var i=0;i<Math.Ceiling(16000d/ms);i++)Scan();
                    }
                    finally{session.ScanCompleted-=Accepted;}
                    if(baseline is null)baseline=trace;else check(baseline.SequenceEqual(trace),"observer_whole_accepted_payload_plant_31_transforms_equivalent_"+label);
                    if(!enabled)continue;
                    check(_workspaceRollerObservedTrial?.State==(wrong?FiniteRollerTrialState.NeedsAttention:FiniteRollerTrialState.Observed),"observer_native_result_"+label);
                    check(_workspaceRollerObservedTrial?.LastScanNumber==session.Snapshot.ScanNumber&&Math.Abs(_workspaceRollerObservedTrial.LastTimeSeconds-session.Snapshot.SimulatedTime.TotalSeconds)<1e-6&&_workspaceRollerObservedTrial.BeamTriggerScanNumber is not null,"observer_accepted_identity_history_"+label);
                    if(wrong)
                    {
                        ResetActiveController();RunEditorDraftController();session=_virtualController!;ExecuteSelectedControllerAction("roller-workspace-press");
                        var count=0;while(!runtime.CaptureWorkspaceRollerPreset().BeamBlocked&&count++<1000)Scan();
                        Scan();var established=_workspaceRollerObservedTrial;
                        check(established?.State==FiniteRollerTrialState.NeedsAttention&&!runtime.CaptureWorkspaceRollerPreset().AtTerminal,"wrong_next_scan_result_established_before_terminal_"+label);
                        StopActiveController();
                        check(established?.State==FiniteRollerTrialState.NeedsAttention&&_workspaceRollerObservedTrial?.State==established.State&&_workspaceRollerObservedTrial.Detail==established.Detail&&_workspaceRollerObservedTrial.BeamTriggerScanNumber==established.BeamTriggerScanNumber&&_workspaceRollerObservedTrial.LastScanNumber==established.LastScanNumber&&_workspaceRollerObservedTrial.LastTimeSeconds==established.LastTimeSeconds,"wrong_next_scan_stop_retains_outcome_detail_trigger_scan_time_"+label);
                    }
                    if(ms!=20)continue;
                    foreach(var force in new[]{"start","beam"})
                    {
                        ResetActiveController();RunEditorDraftController();session=_virtualController!;
                        var before=runtime.CaptureWorkspaceRollerPreset();session.SetBoolForce(force,true);
                        if(force=="beam")ExecuteSelectedControllerAction("roller-workspace-press");
                        session.ScanCompleted+=Accepted;try{Scan();}finally{session.ScanCompleted-=Accepted;}
                        var expectRun=force=="start"||wrong;var after=runtime.CaptureWorkspaceRollerPreset();
                        check(last is not null&&!last.SampledInputs.Bools[force]&&last.EffectiveInputs.Bools[force]&&Equals(runtime.Points[p.RunPoint],expectRun)&&(after.DriveSurfaceTravelM>before.DriveSurfaceTravelM)==expectRun&&_workspaceRollerObservedTrial?.State==FiniteRollerTrialState.Unavailable,"force_raw_false_effective_true_plant_obeys_observer_unavailable_"+force+"_"+label);
                        var epoch=_workspaceRollerResetEpoch;ResetActiveController();
                        check(session.Snapshot.Forces.Count==0&&_workspaceRollerResetEpoch>epoch&&_workspaceRollerObservedTrial?.State==FiniteRollerTrialState.Ready&&runtime.CaptureWorkspaceRollerPreset().ClampedLoadTravelM==0,"force_reset_clear_new_home_epoch_"+force+"_"+label);
                    }
                    RunEditorDraftController();session=_virtualController!;ExecuteSelectedControllerAction("roller-workspace-press");Scan();
                    var snapshot=JsonSerializer.Serialize(session.Snapshot);var loaded=_virtualProgram;var plant=runtime.CaptureWorkspaceRollerPreset();
                    var current=LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!;current.AddTag("roller_observer_changed_memory",PlcVariableRole.Memory);
                    var changed=LadderEditorProjectJson.Save(current);_simulatorShell.RestoreIntegratedLadderDraft(changed);
                    check(_workspaceRollerObservedTrial?.State==FiniteRollerTrialState.Unavailable&&ReferenceEquals(loaded,_virtualProgram)&&JsonSerializer.Serialize(session.Snapshot)==snapshot&&runtime.CaptureWorkspaceRollerPreset()==plant&&_simulatorShell.CaptureIntegratedLadderDraft()==changed,"valid_draft_only_change_no_implicit_load_scan_or_motion_"+label);
                    ResetActiveController();RunEditorDraftController();Scan();StopActiveController();
                    check(_workspaceRollerObservedTrial?.State==FiniteRollerTrialState.Incomplete,"manual_early_stop_incomplete_"+label);
                }
            }
        }
        finally{_workspaceRollerObservedTrialEnabled=true;}
    }
}
