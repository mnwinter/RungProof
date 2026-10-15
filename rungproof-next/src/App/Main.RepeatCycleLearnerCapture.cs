using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using RungProof.Next.Lessons;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private string? _repeatLearnerCaptureState;
    private bool _repeatLearnerCapturePending,_repeatLearnerCaptureHeld;
    private async void PrepareRepeatLearnerCapture()
    {
        try
        {
            if(_capturePath is null||_currentSceneId!=RepeatCycleSceneId||_repeatLearnerCaptureState is not ("intended" or "hint" or "history" or "invalid" or "request-count" or "preset-two" or "held-active"))throw new InvalidOperationException("Exact repeat lesson and explicit capture state required.");
            SetPhysicsProcess(false);
            var directory=ProjectSettings.GlobalizePath("res://.tools/repeat-learner-capture-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var wrong=_repeatLearnerCaptureState is "request-count" or "preset-two" or "held-active";
            var joint=await OpenRepeatLearnerFixture(directory,40,wrong?_repeatLearnerCaptureState!:"intended");
            ExecuteSelectedControllerAction("toggle-machine_enabled");ExecuteSelectedControllerAction("pulse-cycle_request");
            var physicalReturnsAtCapture=0;
            // Count physical edges directly around the existing accepted tick. A failed
            // assessment freezes its evidence while the learner's plant may continue.
            for(var i=0;i<500&&(wrong||_repeatCycleTrial!.View.Status!=RepeatCycleTrialStatus.Observed);i++)
            {
                var before=_sceneRuntime!.CaptureRepeatCyclePlantState();
                RepeatLearnerTick();
                var after=_sceneRuntime.CaptureRepeatCyclePlantState();
                if(before?.Phase==RungProof.Next.Scenes.RepeatCyclePlantModel.CyclePhase.Return
                    &&after is {Done:true,Phase:RungProof.Next.Scenes.RepeatCyclePlantModel.CyclePhase.Complete})physicalReturnsAtCapture++;
            }
            string PointEvidence(string name)=>_sceneRuntime!.Points.TryGetValue(name,out var value)
                ?$"{name}={value} ({value?.GetType().FullName??"null"})":name+"=missing";
            string CaptureEvidence()=>$"mode={_repeatLearnerCaptureState}; physicalReturns={physicalReturnsAtCapture}; status={_repeatCycleTrial?.View.Status}; acceptedTime={_virtualController?.Snapshot.SimulatedTime}; plant={_sceneRuntime!.CaptureRepeatCyclePlantState()}; {PointEvidence("batch_count")}; {PointEvidence("cycle_complete")}; {PointEvidence("cycle_active")}";
            var physicalReturnsValid=_repeatLearnerCaptureState switch
            {
                "request-count"=>physicalReturnsAtCapture>=4,
                "preset-two"=>physicalReturnsAtCapture==2,
                "held-active"=>physicalReturnsAtCapture==1,
                _=>physicalReturnsAtCapture==3
            };
            if(!physicalReturnsValid)throw new InvalidOperationException("Actual physical return total differs. "+CaptureEvidence());
            if(!wrong&&_repeatCycleTrial!.View.Status!=RepeatCycleTrialStatus.Observed)throw new InvalidOperationException("Current learner reference did not produce observed evidence. "+CaptureEvidence());
            if(wrong)
            {
                var expected=_repeatLearnerCaptureState=="held-active"?RepeatCycleTrialStatus.Incomplete:RepeatCycleTrialStatus.NeedsAttention;
                if(_repeatCycleTrial!.View.Status!=expected||_virtualController!.Snapshot.SimulatedTime!=TimeSpan.FromSeconds(20)||!RepeatLearnerExpectedDisplay())throw new InvalidOperationException("Actual wrong capture status/time/native displays differ. "+CaptureEvidence());
                // Scene DINT commits are boxed Int64; double is the controller image type.
                if(_repeatLearnerCaptureState=="preset-two"&&(_sceneRuntime!.Points["batch_count"] is not long count||count!=2||_sceneRuntime.Points["cycle_complete"] is not true))throw new InvalidOperationException("Preset-two wrong capture physical PLC consequence differs. "+CaptureEvidence());
                if(_repeatLearnerCaptureState=="request-count"&&(_sceneRuntime!.Points["batch_count"] is not long requestCount||requestCount!=1||_sceneRuntime.Points["cycle_complete"] is not false))throw new InvalidOperationException("Request-count wrong capture physical PLC consequence differs. "+CaptureEvidence());
                if(_repeatLearnerCaptureState=="held-active"&&_sceneRuntime!.CaptureRepeatCyclePlantState() is not {Done:true,Phase:RungProof.Next.Scenes.RepeatCyclePlantModel.CyclePhase.Complete})throw new InvalidOperationException("Held command did not retain actual Done. "+CaptureEvidence());
            }
            if(_repeatLearnerCaptureState=="invalid")
            {
                _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(RepeatLearnerFixture(40,"invalid")));RunEditorDraftController();
                if(_virtualController!.Snapshot.State==VirtualControllerState.Running)throw new InvalidOperationException("Invalid current draft still running. "+CaptureEvidence());
                SaveWorkspaceToPath(joint);
            }
            if(_repeatLearnerCaptureState=="hint")_simulatorShell!.ShowRepeatCycleHintForReview();
            _simulatorShell!.ScrollRepeatCycleTrialForReview();
            _repeatLearnerCaptureHeld=true;
            void Hold(Node node){node.ProcessMode=ProcessModeEnum.Disabled;foreach(var child in node.GetChildren())Hold(child);}
            Hold(_sceneCompositionRoot!);_demoEnvironment?.SetAmbientMotionEnabled(false);
            _simulatorShell!.SetWorkspaceStatus("REVIEW CAPTURE · CLOCK HELD · "+_repeatLearnerCaptureState);
            if(_repeatLearnerCaptureState=="history")
            {
                PublishScanInspectionState();var before=ScanInspectionNativePose();var snapshot=JsonSerializer.Serialize(_virtualController!.Snapshot);
                if(!_simulatorShell.VerifyScanInspectionHistorySelection(0,out var text)||before!=ScanInspectionNativePose()||snapshot!=JsonSerializer.Serialize(_virtualController.Snapshot))throw new InvalidOperationException("History capture mutated live state.");
                File.WriteAllText(_capturePath+".history.txt",text);
            }
            var dll=ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/RungProof.Next.dll");
            File.WriteAllText(_capturePath+".repeat-learner.json",JsonSerializer.Serialize(new { scene=_currentSceneId,mode=_repeatLearnerCaptureState,diagnosticClockHeld=true,learnerPauseUsed=false,physicalReturnsAtCapture,joint,currentDraft=_simulatorShell.CaptureIntegratedLadderDraft(),assessmentStatus=_repeatCycleTrial!.View.Status.ToString(),view=_repeatCycleTrial!.View,identity=_repeatCycleIdentity,snapshot=_virtualController!.Snapshot,plant=_sceneRuntime!.CaptureRepeatCyclePlantState(),points=_sceneRuntime.Points,pose=ScanInspectionNativePose(),display=RepeatLearnerDisplay(),dllSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(dll))) }));
            _repeatLearnerCapturePending=false;GD.Print("REPEAT_LEARNER_CAPTURE_READY "+_repeatLearnerCaptureState);
        }
        catch(Exception ex){GD.PushError("REPEAT_LEARNER_CAPTURE_FAILED "+ex);GetTree().Quit(1);}
    }
}
