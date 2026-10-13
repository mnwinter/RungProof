using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.Lessons;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyRepeatCycleLearner;
    private bool RepeatLearnerCardBoundary(bool frozen)
    {
        var labels=_simulatorShell!.RepeatCycleTrialLabelsForReview();
        return labels.Contains("assessed",StringComparison.OrdinalIgnoreCase)&&labels.Contains("elapsed",StringComparison.OrdinalIgnoreCase)
            &&labels.Contains(frozen?"frozen accepted assessment":"updating accepted assessment",StringComparison.OrdinalIgnoreCase)
            &&!labels.Contains(frozen?"updating accepted assessment":"frozen accepted assessment",StringComparison.OrdinalIgnoreCase);
    }
    private bool RepeatLearnerExpectedDisplay()
    {
        var count=Convert.ToDouble(_sceneRuntime!.Points["batch_count"]);
        if(_sceneCompositionRoot!.GetNode<Label3D>("batch_count_display/NumericReadout").Text!="COUNT\n"+count.ToString("F0",System.Globalization.CultureInfo.InvariantCulture))return false;
        foreach(var (id,point,selected) in new[]{("indicator_2","cycle_active","amber"),("indicator_4","cycle_complete","green")})
        {
            var lenses=_sceneCompositionRoot.GetNode<Node3D>(id).FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().Where(n=>n.Name.ToString().Contains("LENS",StringComparison.OrdinalIgnoreCase)).ToArray();
            if(lenses.Length==0)return false;
            foreach(var lens in lenses)
            {
                var name=lens.Name.ToString().ToLowerInvariant();
                var colorName=new[]{"red","amber","green","blue","white","orange"}.FirstOrDefault(name.Contains)??selected;
                var active=_sceneRuntime.Points[point] is true&&(lenses.Length==1||colorName==selected);
                var color=new Color(colorName switch{"red"=>"e03c31","amber"=>"f2a900","blue"=>"2e8bd1","white"=>"e9f2f5","orange"=>"ff6400",_=>"21a366"});
                if(lens.MaterialOverride is not StandardMaterial3D m||m.EmissionEnabled!=active||Math.Abs(m.EmissionEnergyMultiplier-(active ? .65 : 0))>1e-6||!m.AlbedoColor.IsEqualApprox(active?color:color.Darkened(.72f)))return false;
            }
        }
        return true;
    }
    private async Task VerifyRepeatLearnerRetirement(string directory,Action<bool,string> check)
    {
        foreach(var ms in new[]{20,40,100})
        {
            await OpenRepeatLearnerFixture(directory,ms,"alias");
            check(_repeatCycleBindings is {Request:"qa_cycle_request",Count:"qa_batch_count"},$"actual_scene_alias_resolved_{ms}");
            ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");
            for(var i=0;i<12000/ms;i++)RepeatLearnerTick();
            check(_repeatCycleTrial!.View.Status==RepeatCycleTrialStatus.Observed&&_repeatCycleTrial.View.ActualReturns==3,$"alias_and_renamed_internalcounter_observed_{ms}");
            await OpenRepeatLearnerFixture(directory,ms,"intended");
            ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");RepeatLearnerTick();
            var scan=_virtualController!.Snapshot.ScanNumber;var pose=ScanInspectionNativePose();var plant=_sceneRuntime!.CaptureRepeatCyclePlantState();
            StopActiveController();RepeatLearnerTick();
            check(scan==_virtualController.Snapshot.ScanNumber&&pose==ScanInspectionNativePose()&&plant==_sceneRuntime.CaptureRepeatCyclePlantState(),$"partial_stop_no_accepted_clock_pose_drift_{ms}");
            ResetActiveController();var home=_sceneRuntime.CaptureRepeatCyclePlantState();
            check(home is {Home:true,Done:false,Busy:false,HomeFraction:1,SpindleRadians:0}&&_virtualController.Snapshot.ScanNumber==0&&_virtualController.History.Count==0&&RepeatLearnerExpectedDisplay(),$"reset_actual_home_spindle_readout_lamps_clock_history_{ms}");
            RunEditorDraftController();ApplyVirtualForce("cycle_request",true);RepeatLearnerTick();RemoveVirtualForce("cycle_request");
            check(_repeatCycleTrial!.View is {Status:RepeatCycleTrialStatus.Unavailable,Historical:true},$"force_retirement_survives_removal_{ms}");
            ResetActiveController();RunEditorDraftController();
            var old=_repeatCycleHandler;var session=_virtualController!;
            VirtualControllerScanCompleted? stale=null;void Keep(VirtualControllerScanCompleted row)=>stale=row;session.ScanCompleted+=Keep;RepeatLearnerTick();session.ScanCompleted-=Keep;
            var before=JsonSerializer.Serialize(_sceneRuntime.Points);var snapshot=JsonSerializer.Serialize(session.Snapshot);
            var edited=RepeatLearnerFixture(ms,"intended");edited.AddTag("qa_semantic_change",PlcVariableRole.Memory);
            _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(edited));RepeatCycleDraftChanged();
            check(_repeatCycleTrial!.View.Historical&&_repeatCycleTrial.View.Status==RepeatCycleTrialStatus.Unavailable&&before==JsonSerializer.Serialize(_sceneRuntime.Points)&&snapshot==JsonSerializer.Serialize(session.Snapshot),$"edit_retires_without_output_assignment_{ms}");
            // A retained callback is retired by its captured generation before observing.
            var view=_repeatCycleTrial.View;
            if(stale is not null)old?.Invoke(stale);
            check(_repeatCycleTrial.View==view,$"stale_callback_cannot_change_retired_view_{ms}");
            await OpenRepeatLearnerFixture(directory,ms,"intended");
            var authoredRoot=_sceneCompositionRoot!.GetNode<Node3D>("machine_0");
            var authoredBefore=authoredRoot.Transform;authoredRoot.Position+=new Vector3(.1f,0,0);
            GD.Print($"REPEAT_CYCLE_AUTHORED_LAYOUT root={authoredRoot.GetPath()} class={authoredRoot.GetClass()} before={authoredBefore} after={authoredRoot.Transform} kinematicChild=RepeatCycleHeadMotion");
            RepeatLearnerTick();
            check(_repeatCycleTrial!.View.Status==RepeatCycleTrialStatus.Unavailable&&_repeatCycleTrial.View.Historical,$"actual_layout_change_retires_identity_{ms}");
        }
    }
    private string RepeatLearnerDisplay() => string.Join("|",_sceneCompositionRoot!.FindChildren("*","Label3D",true,false).OfType<Label3D>().Select(n=>ScanInspectionStructuralPath(n)+":"+n.Text))
        +"|LAMPS:"+string.Join("|",_sceneCompositionRoot.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().Where(n=>n.Name.ToString().Contains("LENS",StringComparison.OrdinalIgnoreCase)).Select(n=>ScanInspectionStructuralPath(n)+":"+(n.MaterialOverride is StandardMaterial3D m?$"{m.EmissionEnabled}:{m.EmissionEnergyMultiplier}:{m.AlbedoColor}":"missing")));
    private static LadderEditorDocument RepeatLearnerFixture(int ms,string mode)
    {
        var d=RepeatCycleDiagnosticDocument(ms);
        if(mode=="request-count") d.Rungs[0].Branches[0].Contacts[0]=d.Rungs[0].Branches[0].Contacts[0] with { Variable="cycle_request" };
        if(mode=="preset-two") d.Rungs[0].CounterPreset=2;
        if(mode=="held-active") d.Rungs[2].Branches[0].Contacts.RemoveAt(1);
        if(mode=="alias") foreach(var tag in d.Tags.ToArray()) d.UpdateTag(tag.Name,"qa_"+tag.Name,tag.Type,tag.Role,tag.Binding,tag.InitialValue);
        if(mode=="invalid") d.AddRung("Unresolved QA output","qa_missing_output");
        return d;
    }
    private async Task<string> OpenRepeatLearnerFixture(string directory,int ms,string mode)
    {
        AddMigratedScene(RepeatCycleSceneId,_candidateCatalog!,_mainCamera!,false,false);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var path=Path.Combine(directory,$"{ms}-{mode}-{Guid.NewGuid():N}.rpproj.json");
        File.WriteAllText(path,LadderEditorProjectJson.Save(RepeatLearnerFixture(ms,mode)));
        if(!_simulatorShell!.OpenNativeMotionReviewProject(path,out var message))throw new InvalidOperationException(message);
        var draft=_simulatorShell.CaptureIntegratedLadderDraft();
        var layout=JsonSerializer.Serialize(CreateWorkspaceDocument());
        var joint=path.Replace(".rpproj.json",".rpworkspace.json");SaveWorkspaceToPath(joint);
        _simulatorShell.DismissIntegratedLoadConfirmationForReview();MarkWorkspaceSaved();
        AddMigratedScene("lab-2-01-workstation-call",_candidateCatalog!,_mainCamera!,false,false);
        MarkWorkspaceSaved();LoadWorkspaceFromPath(joint);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        _simulatorShell.DismissIntegratedLoadConfirmationForReview();
        if(_currentSceneId!=RepeatCycleSceneId||draft!=_simulatorShell.CaptureIntegratedLadderDraft()||layout!=JsonSerializer.Serialize(CreateWorkspaceDocument()))throw new InvalidOperationException("Exact joint draft/layout roundtrip failed.");
        RunEditorDraftController();return joint;
    }
    // Copy immediately around the SAME shared accepted callback. The observer consumes its
    // own pair before this QA subscriber; trace causality cannot depend on that private pair.
    private void RepeatLearnerTick()
    {
        _virtualController!.Advance(_virtualController.ScanPeriod.TotalSeconds,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
            CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,AdvanceAcceptedScenePlant,
            SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs,CaptureScanInspectionPlantInputs);
    }
    private async void VerifyRepeatCycleLearner()
    {
        var failures=0;var directory=ProjectSettings.GlobalizePath("res://.tools/repeat-cycle-learner-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        void Check(bool ok,string name){GD.Print($"REPEAT_CYCLE_LEARNER {name}={ok}");if(!ok)failures++;}
        try
        {
            SetPhysicsProcess(false);
            AddMigratedScene(RepeatCycleSceneId,_candidateCatalog!,_mainCamera!,false,false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            Check(LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!.Rungs.Count==0,"fresh_factory_zero_rungs");
            Check(_repeatCycleTrial is null&&_simulatorShell!.RepeatCycleTrialLabelsForReview().Contains("Assessed actual returns: unavailable",StringComparison.Ordinal)&&_simulatorShell.RepeatCycleTrialLabelsForReview().Contains("accepted PLC count: unavailable",StringComparison.Ordinal)&&_simulatorShell.RepeatCycleTrialLabelsForReview().Contains("Accepted elapsed: unavailable",StringComparison.Ordinal),"no_accepted_view_no_invented_zero_count_returns_elapsed");
            foreach(var ms in new[]{20,40,100})
            foreach(var mode in new[]{"intended","request-count","preset-two","held-active"})
            {
                List<string>? baseline=null;
                foreach(var observer in new[]{true,false})foreach(var history in new[]{true,false})
                {
                    _repeatCycleObserverEnabled=observer;await OpenRepeatLearnerFixture(directory,ms,mode);
                    var session=_virtualController!;session.HistoryRecordingEnabled=history;var trace=new List<string>();
                    RepeatCyclePlantImage? before=null,after=null;VirtualControllerSignalImage? post=null;
                    var returns=0;long edge=0;double previousCount=0;var lag=true;
                    void Completed(VirtualControllerScanCompleted s)
                    {
                        var count=s.Outputs.Numbers["batch_count"];
                        if(before!.Phase==RepeatCyclePlantModel.CyclePhase.Return&&after!.Phase==RepeatCyclePlantModel.CyclePhase.Complete){returns++;edge=s.ScanNumber;lag&=count==previousCount;}
                        if(s.ScanNumber==edge+1&&edge>0&&mode!="request-count")lag&=count==returns;
                        previousCount=count;
                        trace.Add(JsonSerializer.Serialize(new { snapshot=session.Snapshot,rootPose=_sceneCompositionRoot!.Transform.ToString(),rootVisible=_sceneCompositionRoot.Visible,s.ScanNumber,s.SimulatedTime,s.SampledInputs,s.EffectiveInputs,s.Outputs,post,before,after,points=_sceneRuntime!.Points,pose=ScanInspectionNativePose(),display=RepeatLearnerDisplay() }));
                    }
                    session.ScanCompleted+=Completed;
                    Check(ExecuteSelectedControllerAction("toggle-machine_enabled")&&ExecuteSelectedControllerAction("pulse-cycle_request"),$"mapped_start_{ms}_{mode}_{observer}_{history}");
                    for(var i=0;i<20000/ms;i++)session.Advance(ms/1000d,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,
                        dt=>{before=_sceneRuntime!.CaptureRepeatCyclePlantState();AdvanceAcceptedScenePlant(dt);after=_sceneRuntime.CaptureRepeatCyclePlantState();post=new(SampleVirtualControllerInputs(),SampleVirtualControllerNumericInputs(),SampleVirtualControllerTextInputs());},SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs,CaptureScanInspectionPlantInputs);
                    session.ScanCompleted-=Completed;
                    if(baseline is null)baseline=trace;else Check(baseline.SequenceEqual(trace),$"full_accepted_invariance_{ms}_{mode}_{observer}_{history}");
                    File.WriteAllLines(Path.Combine(directory,$"trace-{ms}-{mode}-observer{observer}-history{history}.jsonl"),trace);
                    Check(history?session.History.Count>0:session.History.Count==0,$"independent_history_{ms}_{mode}_{observer}_{history}");
                    if(observer)
                    {
                        var expected=mode=="intended"?RepeatCycleTrialStatus.Observed:mode=="held-active"?RepeatCycleTrialStatus.Incomplete:RepeatCycleTrialStatus.NeedsAttention;
                        Check(_repeatCycleTrial!.View.Status==expected,$"status_{ms}_{mode}_{history}_{expected}");
                        if(mode=="request-count")Check(returns>=4&&previousCount==1&&_sceneRuntime!.Points["cycle_complete"] is false,$"wrong_request_actual_repeated_strokes_count1_{ms}_{history}");
                        if(mode!="request-count")Check(returns==(mode=="preset-two"?2:mode=="held-active"?1:3),$"genuine_returns_{ms}_{mode}_{history}");
                        Check(RepeatLearnerExpectedDisplay(),$"expected_actual_readout_lamps_{ms}_{mode}_{history}");
                        if(mode=="request-count") Check(RepeatLearnerCardBoundary(true),$"bounded_assessed_elapsed_card_{ms}_{history}");
                        Check(RepeatLearnerCardBoundary(mode!="intended"),$"current_or_frozen_card_boundary_{ms}_{mode}_{history}");
                        if(mode=="intended")Check(lag&&_repeatCycleTrial.View.StationaryHold>=TimeSpan.FromSeconds(.5),$"N_Nplus1_subsequent_hold_{ms}_{history}");
                    }
                }
            }
            _repeatCycleObserverEnabled=true;
            await VerifyRepeatLearnerRetirement(directory,Check);
            foreach(var ms in new[]{20,40,100})
            {
                await OpenRepeatLearnerFixture(directory,ms,"intended");
                ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");RepeatLearnerTick();
                var sessionPartial=_virtualController!;var partial=_sceneRuntime!.CaptureRepeatCyclePlantState();
                PauseActiveController();StepActiveController();Check(sessionPartial.Snapshot.ScanNumber==1&&!sessionPartial.IsPaused,"unsupported_pause_step");
                StopActiveController();Check(RepeatLearnerCardBoundary(true),"historical_stop_frozen_card_boundary");var historical=_repeatCycleTrial!.View;RunEditorDraftController();RepeatLearnerTick();
                Check(historical.Historical&&historical.Status==RepeatCycleTrialStatus.Incomplete&&_repeatCycleTrial.View==historical&&_sceneRuntime.CaptureRepeatCyclePlantState()!=partial,"partial_stop_resume_historical");
                PublishScanInspectionState();var pose=ScanInspectionNativePose();var snapshot=JsonSerializer.Serialize(sessionPartial.Snapshot);
                Check(_simulatorShell!.VerifyScanInspectionHistorySelection(0,out _)&&pose==ScanInspectionNativePose()&&snapshot==JsonSerializer.Serialize(sessionPartial.Snapshot),"history_selection_read_only");
                var invalid=RepeatLearnerFixture(ms,"invalid");_simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(invalid));RunEditorDraftController();
                var stoppedScan=sessionPartial.Snapshot.ScanNumber;pose=ScanInspectionNativePose();RepeatLearnerTick();
                Check(sessionPartial.Snapshot.ScanNumber==stoppedScan&&pose==ScanInspectionNativePose(),"invalid_midfeed_blocks_stale_motion_clock");
                await OpenRepeatLearnerFixture(directory,ms,"invalid");Check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"exact_saved_invalid_reopen_blocked");
                await OpenRepeatLearnerFixture(directory,ms,"intended");ResetActiveController();Check(_sceneRuntime!.CaptureRepeatCyclePlantState() is {Home:true,Done:false,Busy:false},"reset_measured_home");
                RunEditorDraftController();ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");
                SetPhysicsProcess(true);var watch=Stopwatch.StartNew();while(_virtualController!.Snapshot.ScanNumber<3&&watch.Elapsed.TotalSeconds<5)await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                SetPhysicsProcess(false);Check(_virtualController!.Snapshot.ScanNumber>=3&&_sceneRuntime!.CaptureRepeatCyclePlantState() is {Busy:true,Home:false},"ordinary_real_frame_run_wall_watchdog");
            }
        }
        catch(Exception ex){GD.PushError("REPEAT_CYCLE_LEARNER_EXCEPTION "+ex);failures++;}
        GD.Print($"REPEAT_CYCLE_LEARNER_VERIFY {(failures==0?"PASS":"FAIL")} failures={failures} evidence={directory}");GetTree().Quit(failures==0?0:1);
    }
}
