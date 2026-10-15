using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.Lessons;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyRepeatCycleLifecycle;
    // Explicit QA reference only; never called by scene factory or editor initialization.
    // Existing RepeatCycleReview's tested five-rung construction is reproduced here
    // because its local document is embedded in the geometry verifier, not a reusable helper.
    private static LadderEditorDocument RepeatCycleDiagnosticDocument(int periodMs=20)
    {
        var document=new LadderEditorDocument();
        document.ResetProject("qa-repeat-cycle-lifecycle","Repeat_Cycle_QA",TimeSpan.FromMilliseconds(periodMs));
        document.SourceSceneId=RepeatCycleSceneId;
        foreach(var name in new[]{"cycle_request","machine_enabled","machine_home","machine_busy","cycle_done"})
            document.AddTag(name,PlcVariableRole.Input,name);
        document.AddTag("head_position",PlcVariableRole.Input,"head_position",PlcVariableType.Real);
        foreach(var name in new[]{"cycle_active","cycle_complete"})document.AddTag(name,PlcVariableRole.Output,name);
        document.AddTag("batch_count",PlcVariableRole.Output,"batch_count",PlcVariableType.DInt);
        document.AddTag("completed_cycles",PlcVariableRole.Memory,type:PlcVariableType.Counter);
        document.AddTag("batch_active",PlcVariableRole.Memory);
        document.AddTag("publish_count",PlcVariableRole.Memory,initialValue:true);
        document.AddCounterRung("Count actual completed returns","completed_cycles",3);
        document.AddContact(0,0,"cycle_done",false);
        document.AddRung("Seal enabled batch below target","batch_active");
        document.AddContact(1,0,"cycle_request",false);document.AddContact(1,0,"machine_enabled",false);document.AddContact(1,0,"completed_cycles.DN",true);
        document.AddParallelBranch(1);document.AddContact(1,1,"batch_active",false);document.AddContact(1,1,"machine_enabled",false);document.AddContact(1,1,"completed_cycles.DN",true);
        document.AddRung("Low command acknowledges actual completion","cycle_active");
        document.AddContact(2,0,"batch_active",false);document.AddContact(2,0,"cycle_done",true);
        document.AddRung("PLC counter completion","cycle_complete");document.AddContact(3,0,"completed_cycles.DN",false);
        document.AddNumericOperationRung("Publish accumulated count",LadderNumericOperationKind.Move,"completed_cycles.ACC",string.Empty,"batch_count");
        document.AddContact(4,0,"publish_count",false);
        return document;
    }
    private async void VerifyRepeatCycleLifecycle()
    {
        var failures=0;
        var evidence=ProjectSettings.GlobalizePath("res://.tools/repeat-cycle-lifecycle-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(evidence);
        void Check(bool ok,string name){GD.Print($"REPEAT_CYCLE_LIFECYCLE {name}={ok}");if(!ok)failures++;}
        void Scan()=>_virtualController!.Advance(.02,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,AdvanceAcceptedScenePlant,
            SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs,CaptureScanInspectionPlantInputs);
        async Task Reopen(string path)
        {
            _simulatorShell!.DismissIntegratedLoadConfirmationForReview();MarkWorkspaceSaved();
            AddMigratedScene("lab-2-01-workstation-call",_candidateCatalog!,_mainCamera!,false,false);
            MarkWorkspaceSaved();LoadWorkspaceFromPath(path);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        try
        {
            SetPhysicsProcess(false); // QA controls exact accepted intervals; scene remains external-clock owned.
            AddMigratedScene(RepeatCycleSceneId,_candidateCatalog!,_mainCamera!,false,false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var factory=LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!;
            Check(factory.Rungs.Count==0,"fresh_factory_zero_rungs");
            var document=RepeatCycleDiagnosticDocument();
            var path=Path.Combine(evidence,"explicit-intended.rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(document));
            Check(_simulatorShell.OpenNativeMotionReviewProject(path,out var opened),"actual_disk_editor_open");GD.Print(opened);
            var draft=_simulatorShell.CaptureIntegratedLadderDraft();
            var layout=JsonSerializer.Serialize(CreateWorkspaceDocument());
            var joint=Path.Combine(evidence,"intended.rpworkspace.json");SaveWorkspaceToPath(joint);
            Check(File.Exists(joint),"actual_joint_saved");var epoch=_repeatCycleEpoch;
            await Reopen(joint);
            Check(_currentSceneId==RepeatCycleSceneId && _simulatorShell.CaptureIntegratedLadderDraft()==draft
                && JsonSerializer.Serialize(CreateWorkspaceDocument())==layout,"exact_joint_draft_layout_reopened");
            Check(_virtualController is null && _sceneRuntime!.CaptureRepeatCyclePlantState() is { Home:true, Done:false },"joint_actual_home_stopped");
            RunEditorDraftController();
            Check(_repeatCycleTrial?.View.Status==RepeatCycleTrialStatus.Ready && _repeatCycleIdentity?.Epoch>epoch,
                "joint_current_run_new_ready_epoch_without_extra_reset");
            Check(ExecuteSelectedControllerAction("toggle-machine_enabled")&&ExecuteSelectedControllerAction("pulse-cycle_request"),"real_enable_start_actions");
            Scan();
            Check(_repeatCycleTrial?.View.Status==RepeatCycleTrialStatus.Running && _repeatCycleTrial.View.TriggerScan==_virtualController!.Snapshot.ScanNumber,
                "joint_start_observed_running_without_extra_reset");
            Check(_sceneRuntime!.CaptureRepeatCyclePlantState() is { Busy:true, Home:false },"actual_first_accepted_stroke");
            StopActiveController();var historical=_repeatCycleTrial?.View;var loaded=_virtualController;
            RunEditorDraftController();Scan();
            Check(historical is { Status:RepeatCycleTrialStatus.Incomplete,Historical:true } && _repeatCycleTrial?.View==historical
                && ReferenceEquals(loaded,_virtualController),"unchanged_stop_run_preserves_historical_trial");
            document.AddTag("qa_changed_memory",PlcVariableRole.Memory);
            _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));RunEditorDraftController();Scan();
            Check(_repeatCycleTrial?.View.Status==RepeatCycleTrialStatus.Unavailable && _repeatCycleTrial.View.Historical,
                "ordinary_changed_program_not_fresh_ready");
            var invalid=RepeatCycleDiagnosticDocument();invalid.AddRung("Invalid unresolved output","qa_missing_output");
            path=Path.Combine(evidence,"invalid.rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(invalid));
            Check(_simulatorShell.OpenNativeMotionReviewProject(path,out opened),"actual_disk_invalid_editor_open");
            var invalidDraft=_simulatorShell.CaptureIntegratedLadderDraft();
            joint=Path.Combine(evidence,"invalid.rpworkspace.json");SaveWorkspaceToPath(joint);await Reopen(joint);
            var before=_sceneRuntime!.CaptureRepeatCyclePlantState();RunEditorDraftController();
            Check(_simulatorShell.CaptureIntegratedLadderDraft()==invalidDraft && _virtualController is null
                && _sceneRuntime.CaptureRepeatCyclePlantState()==before && (_repeatCycleTrial is null || _repeatCycleTrial.View.Status==RepeatCycleTrialStatus.Unavailable),
                "invalid_exact_reopen_unavailable_run_blocked");
            Check(_simulatorShell.RepeatCycleTrialLabelsForReview().Contains("Unavailable",StringComparison.Ordinal),"invalid_actual_card_unavailable");
            await VerifyRepeatCycleResetHandoff(Check,evidence,Reopen);
        }
        catch(Exception exception){GD.PushError("REPEAT_CYCLE_LIFECYCLE_EXCEPTION "+exception);failures++;}
        GD.Print($"REPEAT_CYCLE_LIFECYCLE_VERIFY {(failures==0?"PASS":"FAIL")} failures={failures} evidence={evidence}");
        GetTree().Quit(failures==0?0:1);
    }
    // Regression for valid A -> edited B -> actual Reset -> current editor Run.
    private async Task VerifyRepeatCycleResetHandoff(Action<bool,string> check,string evidence,Func<string,Task> reopen)
    {
        foreach(var ms in new[]{20,40,100})
        {
            void Tick()=>_virtualController!.Advance(ms/1000d,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
                CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,AdvanceAcceptedScenePlant,
                SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs,CaptureScanInspectionPlantInputs);
            AddMigratedScene(RepeatCycleSceneId,_candidateCatalog!,_mainCamera!,false,false);
            var a=RepeatCycleDiagnosticDocument(ms);
            var disk=Path.Combine(evidence,$"reset-handoff-A-{ms}.rpproj.json");File.WriteAllText(disk,LadderEditorProjectJson.Save(a));
            check(_simulatorShell!.OpenNativeMotionReviewProject(disk,out _),$"reset_handoff_disk_open_A_{ms}");
            var joint=Path.Combine(evidence,$"reset-handoff-A-{ms}.rpworkspace.json");SaveWorkspaceToPath(joint);
            async Task OpenA(){await reopen(joint);RunEditorDraftController();}
            var b=RepeatCycleDiagnosticDocument(ms);b.AddTag("qa_handoff_B_memory",PlcVariableRole.Memory);
            var bDraft=LadderEditorProjectJson.Save(b);
            var bFingerprint=WorkspaceTrialHash(LadderProgramJson.Save(b.BuildProgram() with {WatchVariables=null}));
            await OpenA();
            ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");Tick();
            check(_sceneRuntime!.CaptureRepeatCyclePlantState() is {Home:false,Busy:true},$"reset_handoff_A_actual_partial_stroke_{ms}");
            var epoch=_repeatCycleEpoch;
            _simulatorShell.RestoreIntegratedLadderDraft(bDraft);ResetActiveController();
            check(_sceneRuntime.CaptureRepeatCyclePlantState() is {Home:true,Done:false,Busy:false,HomeFraction:1,SpindleRadians:0}
                && _virtualController!.Snapshot.ScanNumber==0,$"reset_handoff_actual_measured_reset_{ms}");
            RunEditorDraftController();
            check(_repeatCycleTrial?.View is {Status:RepeatCycleTrialStatus.Ready,Historical:false}
                && _repeatCycleIdentity?.Epoch>epoch && _repeatCycleIdentity.ProgramFingerprint==bFingerprint
                && _simulatorShell.CaptureIntegratedLadderDraft()==bDraft,$"edited_B_reset_run_fresh_ready_exact_identity_{ms}");
            ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");Tick();
            check(_repeatCycleTrial?.View.Status==RepeatCycleTrialStatus.Running && _sceneRuntime.CaptureRepeatCyclePlantState() is {Busy:true,Home:false},
                $"edited_B_reset_run_start_observed_running_{ms}");
            await OpenA();_simulatorShell.RestoreIntegratedLadderDraft(bDraft);RunEditorDraftController();Tick();
            check(_repeatCycleTrial?.View is {Status:RepeatCycleTrialStatus.Unavailable,Historical:true},$"edited_B_run_without_reset_retired_{ms}");
            await OpenA();var invalid=RepeatCycleDiagnosticDocument(ms);invalid.AddRung("Unresolved output","qa_handoff_missing_output");
            var invalidDraft=LadderEditorProjectJson.Save(invalid);_simulatorShell.RestoreIntegratedLadderDraft(invalidDraft);ResetActiveController();
            var resetPlant=_sceneRuntime!.CaptureRepeatCyclePlantState();RunEditorDraftController();Tick();
            check(_virtualController?.Snapshot.State!=VirtualControllerState.Running && _sceneRuntime.CaptureRepeatCyclePlantState()==resetPlant
                && _simulatorShell.CaptureIntegratedLadderDraft()==invalidDraft && (_repeatCycleTrial is null || _repeatCycleTrial.View.Status==RepeatCycleTrialStatus.Unavailable),
                $"invalid_reset_run_still_blocked_{ms}");
            await OpenA();ApplyVirtualForce("machine_enabled",true);ClearVirtualForces();RunEditorDraftController();Tick();
            check(_repeatCycleTrial?.View is {Status:RepeatCycleTrialStatus.Unavailable,Historical:true},$"force_clear_without_reset_still_retired_{ms}");
            await OpenA();_simulatorShell.RestoreIntegratedLadderDraft(bDraft);ResetActiveController();
            var machine=_sceneCompositionRoot!.GetNode<Node3D>("machine_0");var original=machine.Position;machine.Position+=new Vector3(.1f,0,0);
            RunEditorDraftController();Tick();
            check(_repeatCycleTrial is null || _repeatCycleTrial.View.Status==RepeatCycleTrialStatus.Unavailable,$"layout_change_after_staged_reset_not_adopted_{ms}");
            machine.Position=original;
            await OpenA();_simulatorShell.RestoreIntegratedLadderDraft(bDraft);ResetActiveController();
            var c=RepeatCycleDiagnosticDocument(ms);c.AddTag("qa_handoff_C_memory",PlcVariableRole.Memory);
            _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(c));RunEditorDraftController();Tick();
            check(_repeatCycleTrial is null || _repeatCycleTrial.View.Status==RepeatCycleTrialStatus.Unavailable,$"additional_draft_after_staged_reset_not_adopted_{ms}");
            await OpenA();_simulatorShell.RestoreIntegratedLadderDraft(bDraft);ResetActiveController();StopActiveController();RunEditorDraftController();Tick();
            check(_repeatCycleTrial is null || _repeatCycleTrial.View.Status==RepeatCycleTrialStatus.Unavailable,$"stop_after_staged_reset_not_adopted_{ms}");
        }
    }
}
