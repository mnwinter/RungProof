using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    // Runs the real editor, physical request action, accepted controller bridge, and rendered plant.
    // Only observer processing is toggled; command/plant rows must match byte-for-byte.
    private async Task VerifyWorkspaceObservedTrial(Action<bool,string> check)
    {
        try
        {
            foreach(var milliseconds in new[]{20,40})
            foreach(var wrong in new[]{false,true})
            {
                List<string>? baseline=null;
                foreach(var enabled in new[]{false,true})
                {
                    _workspaceObservedTrialEnabled=enabled;
                    var preset=WorkspaceFiniteBeltPolicy.Create();var p=preset.FiniteBeltPreset!;
                    AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:preset);
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                    check(_simulatorShell!.VerifyWorkspaceTrialHintHidden(),"observer_new_scene_hint_hidden");
                    var document=new LadderEditorDocument();document.ResetProject("observer-qa","Observer_QA",TimeSpan.FromMilliseconds(milliseconds));document.SourceSceneId=preset.SourceSceneId;
                    document.AddTag("start",PlcVariableRole.Input,p.StartPoint);document.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);document.AddTag("run",PlcVariableRole.Output,p.RunPoint);
                    document.AddRung("QA retained request","run");document.AddContact(0,0,"start",false);if(!wrong)document.AddContact(0,0,"beam",true);
                    document.AddParallelBranch(0);document.AddContact(0,1,"run",false);if(!wrong)document.AddContact(0,1,"beam",true);
                    var path=ProjectSettings.GlobalizePath("res://.tools/observer-qa.rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(document));
                    check(_simulatorShell!.OpenNativeMotionReviewProject(path,out _),"observer_actual_editor_open");
                    var learnerDraft=_simulatorShell.CaptureIntegratedLadderDraft();
                    RunEditorDraftController();ResetActiveController();RunEditorDraftController();
                    check(ExecuteSelectedControllerAction("workspace-press"),"observer_real_start");
                    var trace=new List<string>();
                    void Scan()
                    {
                        _virtualController!.Advance(milliseconds/1000d,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,_sceneRuntime!.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
                        var physical=_sceneRuntime!.CaptureWorkspaceBeltPreset();
                        trace.Add($"{_virtualController.Snapshot.ScanNumber}:{_virtualController.Snapshot.SimulatedTime.Ticks}:{physical.Center:R}:{physical.BeamBlocked}:{physical.AtTerminal}:{_sceneRuntime.Points[p.RunPoint]}");
                    }
                    for(var i=0;i<Math.Ceiling(16_000d/milliseconds);i++)Scan();
                    check(_simulatorShell.CaptureIntegratedLadderDraft()==learnerDraft,"observer_running_does_not_edit_learner_draft");
                    if(baseline is null)baseline=trace;else check(System.Linq.Enumerable.SequenceEqual(baseline,trace),$"observer_enabled_disabled_identical_trace_{milliseconds}ms_wrong{wrong}");
                    if(enabled)
                    {
                        GD.Print($"WORKSPACE_OBSERVER_ACTUAL periodMs={milliseconds} wrong={wrong} state={_workspaceObservedTrial?.State} detail={_workspaceObservedTrial?.Detail}");
                        check(_workspaceObservedTrial?.State==(wrong?FiniteBeltTrialState.NeedsAttention:FiniteBeltTrialState.Observed),$"observer_actual_result_{milliseconds}ms_wrong{wrong}");
                        StopActiveController();check(wrong||_workspaceObservedTrial?.State==FiniteBeltTrialState.Observed,"observer_stop_completed_retains_historical_result");
                        if(!wrong)
                        {
                            var completedEpoch=_workspaceTrialResetEpoch;
                            var jointPath=ProjectSettings.GlobalizePath("res://.tools/observer-completed-layout.rpworkspace.json");
                            WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,CreateWorkspaceDocument(),_simulatorShell.CaptureIntegratedLadderDraft()),jointPath,_candidateCatalog!,_sceneCatalog!);
                            var home=Vector(System.Linq.Enumerable.Single(preset.Placements,v=>v.InstanceId==p.BeltId).Position);
                            TryTransformFiniteBeltWorkspace(p.BeltId,home+new Vector3(.4f,0,.3f),Vector3.Zero,Vector3.One);
                            check(_workspaceObservedTrial?.State!=FiniteBeltTrialState.Observed&&_workspaceTrialResetEpoch>completedEpoch
                                &&(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running),"observer_completed_translation_invalidates_stopped_epoch");
                            var translatedEpoch=_workspaceTrialResetEpoch;
                            UndoWorkspace();
                            check(_workspaceObservedTrial?.State!=FiniteBeltTrialState.Observed&&_workspaceTrialResetEpoch>translatedEpoch
                                &&(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running),"observer_completed_layout_undo_no_inherited_success");
                            var undoneEpoch=_workspaceTrialResetEpoch;
                            MarkWorkspaceSaved();LoadWorkspaceFromPath(jointPath);
                            check(_workspaceObservedTrial?.State!=FiniteBeltTrialState.Observed&&_workspaceTrialResetEpoch>undoneEpoch
                                &&(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running),"observer_completed_joint_reopen_no_inherited_success");
                            RunEditorDraftController();
                        }
                        ResetActiveController();check(_workspaceObservedTrial?.State==FiniteBeltTrialState.Ready,"observer_reset_new_home_epoch");
                        RunEditorDraftController();Scan();StopActiveController();check(_workspaceObservedTrial?.State==FiniteBeltTrialState.Incomplete,"observer_manual_stop_before_completion_incomplete");
                        var stoppedSnapshot=_virtualController!.Snapshot;var stoppedPhysical=_sceneRuntime!.CaptureWorkspaceBeltPreset();
                        Scan();Scan();
                        check(_virtualController.Snapshot.ScanNumber==stoppedSnapshot.ScanNumber&&_virtualController.Snapshot.SimulatedTime==stoppedSnapshot.SimulatedTime
                            &&_sceneRuntime.CaptureWorkspaceBeltPreset()==stoppedPhysical,"observer_stop_no_scan_no_clock_or_displacement");
                        foreach(var forcedVariable in new[]{"start","beam"})
                        {
                            ResetActiveController();RunEditorDraftController();
                            var beforeForce=_sceneRuntime.CaptureWorkspaceBeltPreset();
                            _virtualController.SetBoolForce(forcedVariable,true);
                            if(forcedVariable=="beam")check(ExecuteSelectedControllerAction("workspace-press"),"observer_forced_beam_actual_start");
                            Scan();
                            var forceScan=_challengeLiveScan!;
                            var expectedRun=forcedVariable=="start"||wrong;
                            check(_workspaceObservedTrial?.State==FiniteBeltTrialState.Unavailable
                                &&forceScan.EffectiveInputs.Bools[forcedVariable]&&!forceScan.SampledInputs.Bools[forcedVariable]
                                &&_virtualController.Snapshot.Variables[forcedVariable]
                                &&Equals(_sceneRuntime.Points[p.RunPoint],expectedRun)
                                &&(_sceneRuntime.CaptureWorkspaceBeltPreset().Center>beforeForce.Center)==expectedRun,
                                "observer_force_unavailable_plant_obeys_effective_accepted_image_"+forcedVariable);
                            var forcedEpoch=_workspaceTrialResetEpoch;
                            ResetActiveController();
                            check(_virtualController.Snapshot.Forces.Count==0&&_workspaceTrialResetEpoch>forcedEpoch
                                &&_workspaceObservedTrial?.State==FiniteBeltTrialState.Ready,"observer_reset_clears_force_restores_home_eligibility");
                        }
                        check(_simulatorShell.CaptureIntegratedLadderDraft()==learnerDraft,"observer_forces_stop_reset_do_not_edit_learner_draft");
                        if(_simulatorShell.VerifyWorkspaceTrialHintHidden())check(_simulatorShell.ShowWorkspaceTrialHintForReview(),"observer_actual_hint_toggle");
                        check(_simulatorShell.CaptureIntegratedLadderDraft()==learnerDraft,"observer_hint_does_not_edit_learner_draft");
                        RunEditorDraftController();check(ExecuteSelectedControllerAction("workspace-press"),"observer_valid_edit_actual_start");Scan();
                        var loadedBeforeEdit=_virtualProgram;var acceptedBeforeEdit=_virtualController.Snapshot;
                        document.AddTag("observer_extra_memory",PlcVariableRole.Memory,string.Empty);
                        var explicitlyChangedDraft=LadderEditorProjectJson.Save(document);File.WriteAllText(path,explicitlyChangedDraft);
                        _simulatorShell.RestoreIntegratedLadderDraft(explicitlyChangedDraft);
                        var acceptedAfterEdit=_virtualController.Snapshot;
                        GD.Print($"WORKSPACE_OBSERVER_EDIT state={_workspaceObservedTrial?.State} detail={_workspaceObservedTrial?.Detail} sameProgram={ReferenceEquals(_virtualProgram,loadedBeforeEdit)} beforeScan={acceptedBeforeEdit.ScanNumber} afterScan={acceptedAfterEdit.ScanNumber} beforeTime={acceptedBeforeEdit.SimulatedTime} afterTime={acceptedAfterEdit.SimulatedTime} beforeState={acceptedBeforeEdit.State} afterState={acceptedAfterEdit.State} beforeOutputs={System.Text.Json.JsonSerializer.Serialize(acceptedBeforeEdit.Outputs)} afterOutputs={System.Text.Json.JsonSerializer.Serialize(acceptedAfterEdit.Outputs)} beforeSnapshot={System.Text.Json.JsonSerializer.Serialize(acceptedBeforeEdit)} afterSnapshot={System.Text.Json.JsonSerializer.Serialize(acceptedAfterEdit)}");
                        check(_workspaceObservedTrial?.State==FiniteBeltTrialState.Unavailable
                            &&ReferenceEquals(_virtualProgram,loadedBeforeEdit)&&System.Text.Json.JsonSerializer.Serialize(_virtualController.Snapshot)==System.Text.Json.JsonSerializer.Serialize(acceptedBeforeEdit)
                            &&_simulatorShell.CaptureIntegratedLadderDraft()==explicitlyChangedDraft,
                            "observer_valid_changed_draft_invalidates_without_implicit_load_or_scan");
                        var invalid=new LadderEditorDocument();invalid.ResetProject("observer-invalid","Invalid",TimeSpan.FromMilliseconds(milliseconds));invalid.SourceSceneId=preset.SourceSceneId;
                        var explicitlyInvalidDraft=LadderEditorProjectJson.Save(invalid);
                        File.WriteAllText(path,explicitlyInvalidDraft);_simulatorShell.OpenNativeMotionReviewProject(path,out _);RunEditorDraftController();
                        check(_workspaceObservedTrial?.State==FiniteBeltTrialState.Unavailable&&_virtualController!.Snapshot.State!=VirtualControllerState.Running,"observer_invalid_draft_unavailable_stopped");
                        check(_simulatorShell.CaptureIntegratedLadderDraft()==explicitlyInvalidDraft,"observer_invalid_run_does_not_repair_draft");
                    }
                }
            }
        }
        finally{_workspaceObservedTrialEnabled=true;}
    }
}