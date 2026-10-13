using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyTypedLearnerWorkflow;
    private string? _typedLearnerCaptureState;
    private bool _typedLearnerCapturePending;
    private bool _typedLearnerCaptureHeld;

    // Ephemeral visual evidence follows actual editable disk Open/current Run.
    // This is not an alternate lesson execution path or persisted live state.
    private async void PrepareTypedLearnerCapture()
    {
        try
        {
            var scene=_currentSceneId!;
            var mode=_typedLearnerCaptureState!;
            if(_capturePath is null || mode is not ("intended" or "wrong" or "invalid" or "history"))
                throw new InvalidOperationException("An explicit typed learner capture state is required.");
            var document=TypedLearnerDocument(scene,40,mode=="wrong"?"wrong":"intended");
            var directory=ProjectSettings.GlobalizePath("res://.tools/typed-capture-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path=Path.Combine(directory,"current.rpproj.json");
            File.WriteAllText(path,LadderEditorProjectJson.Save(document));
            if(!_simulatorShell!.OpenNativeMotionReviewProject(path,out var result)) throw new InvalidOperationException(result);
            var draft=_simulatorShell.CaptureIntegratedLadderDraft();
            var joint=Path.Combine(directory,"current.rpworkspace.json");
            SaveWorkspaceToPath(joint);
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();
            AddMigratedScene("lab-2-01-workstation-call",_candidateCatalog!,_mainCamera!,false,false);
            MarkWorkspaceSaved();LoadWorkspaceFromPath(joint);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();
            if(_currentSceneId!=scene || _simulatorShell.CaptureIntegratedLadderDraft()!=draft)
                throw new InvalidOperationException("Typed capture joint draft did not roundtrip.");
            RunEditorDraftController();
            if(_virtualController?.Snapshot.State!=VirtualControllerState.Running)
                throw new InvalidOperationException("Typed capture current draft did not Run.");
            async Task Until(Func<bool> condition)
            {
                var timer=Stopwatch.StartNew();
                while(!condition())
                {
                    if(timer.Elapsed.TotalSeconds>10)throw new InvalidOperationException("Typed capture accepted target timed out.");
                    await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                }
            }
            void Action(string id)
            {
                if(!ExecuteSelectedControllerAction(id))throw new InvalidOperationException("Capture operator action rejected: "+id);
            }
            await Until(()=>_virtualController.Snapshot.ScanNumber>=2);
            if(scene=="lab-10-04-motor-enum-state")
            {
                Action("toggle-start_request");
                var scan=_virtualController.Snapshot.ScanNumber;
                await Until(()=>_virtualController.Snapshot.ScanNumber>=scan+5);
            }
            else if(scene=="lab-10-02-chicken-label-print")
            {
                Action("toggle-label_data_valid");Action("toggle-printer_ready");Action("toggle-product_weighed");
                await Until(()=>_sceneRuntime!.Points["label_applied"] is true);
                var expected=mode=="wrong"?"CHICKEN 1.24 kg":"CHICKEN 1.237 kg";
                if(_sceneCompositionRoot!.GetNode<MeshInstance3D>("ChickenLabelPaper").GetNode<Label3D>("PrintedInk").Text!=expected)
                    throw new InvalidOperationException("Actual printed ink differs from current learner format.");
            }
            else if(scene=="lab-10-01-drive-alarm-code-string")
            {
                Action("toggle-drive_alarm_string_valid");Action("next-alarm-message");
                var scan=_virtualController.Snapshot.ScanNumber;
                await Until(()=>_virtualController.Snapshot.ScanNumber>=scan+2);
            }
            else throw new InvalidOperationException("Capture scene is not an existing typed lesson.");
            if(mode=="invalid")
            {
                var invalid=LadderEditorProjectJson.Load(_simulatorShell.CaptureIntegratedLadderDraft()).Document!;
                invalid.AddRung("Unresolved current output","qa_missing_output");
                _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(invalid));
                RunEditorDraftController();
                if(_virtualController.Snapshot.State==VirtualControllerState.Running)
                    throw new InvalidOperationException("Invalid current capture continued execution.");
            }
            // Capture-only lease: do not enable unsupported product Pause.
            // Hold both accepted scans and autonomous motor descendants before
            // copying identity, so the eventual PNG shows that exact state.
            _typedLearnerCaptureHeld=true;
            void Hold(Node node)
            {
                node.ProcessMode=ProcessModeEnum.Disabled;
                foreach(var child in node.GetChildren())Hold(child);
            }
            Hold(_sceneCompositionRoot!);
            _demoEnvironment?.SetAmbientMotionEnabled(false);
            if(System.Linq.Enumerable.Contains(OS.GetCmdlineUserArgs(),"--verify-typed-label-presentation"))
            {
                var failures=0;
                await VerifyChickenLabelPresentation((ok,name)=>{GD.Print($"TYPED_LABEL_PRESENTATION_CHECK {name}={ok}");if(!ok)failures++;});
                if(failures>0)throw new InvalidOperationException($"Typed label presentation failures={failures}");
            }
            _simulatorShell.SetWorkspaceStatus("REVIEW CAPTURE · CLOCK HELD · "+mode+" current typed draft");
            if(mode=="history")
            {
                var before=JsonSerializer.Serialize(_virtualController.Snapshot);
                var points=JsonSerializer.Serialize(_sceneRuntime!.Points);
                var pose=FiveDemoPose();
                PublishScanInspectionState();
                if(!_simulatorShell.VerifyScanInspectionHistorySelection(0,out var historyText)
                    || JsonSerializer.Serialize(_virtualController.Snapshot)!=before
                    || JsonSerializer.Serialize(_sceneRuntime.Points)!=points || FiveDemoPose()!=pose)
                    throw new InvalidOperationException("Typed historical selection changed live state.");
                File.WriteAllText(_capturePath+".history.txt",historyText);
            }
            var dll=ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/RungProof.Next.dll");
            File.WriteAllText(_capturePath+".typed-learner.json",JsonSerializer.Serialize(new
            {
                scene,mode,ephemeralQA=true,diagnosticClockHeld=true,periodMs=_virtualController.ScanPeriod.TotalMilliseconds,
                scan=_virtualController.Snapshot.ScanNumber,state=_virtualController.Snapshot.State.ToString(),
                points=_sceneRuntime!.Points,currentDraft=_simulatorShell.CaptureIntegratedLadderDraft(),
                acceptedHistory=_virtualController.History,
                dllSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(dll)))
            }));
            _typedLearnerCapturePending=false;
            GD.Print($"TYPED_LEARNER_CAPTURE_READY scene={scene} mode={mode}");
        }
        catch(Exception error){GD.PushError("TYPED_LEARNER_CAPTURE_FAILED "+error);GetTree().Quit(1);}
    }

    private void ApplyTypedLearnerDetailCaptureView()
    {
        if(!System.Linq.Enumerable.Contains(OS.GetCmdlineUserArgs(),"--typed-learner-detail")
            || _typedLearnerCaptureState is null || _sceneCamera is null || _sceneCompositionRoot is null)return;
        Vector3 target,eye;
        if(_currentSceneId=="lab-10-02-chicken-label-print")
        {
            target=_sceneCompositionRoot.GetNode<MeshInstance3D>("ChickenLabelPaper").GlobalPosition;
            eye=target+new Vector3(.65f,1.1f,1.0f);
        }
        else
        {
            var equipment=_currentSceneId=="lab-10-04-motor-enum-state"?"training_accessory_7":"training_accessory_4";
            var readout=(Label3D)_sceneCompositionRoot.GetNode<Node3D>(equipment).FindChild("StaticReadout",true,false);
            target=readout.GlobalPosition;
            eye=target+readout.GlobalBasis*Vector3.Back*2.3f+Vector3.Up*.25f;
        }
        _sceneCamera.GlobalPosition=eye;
        _sceneCamera.Fov=32;
        _sceneCamera.LookAt(target,Vector3.Up);
        // Recompute aperture offsets for this distance, rather than retaining
        // the whole-room camera's much larger world-space offset.
        _cameraController?.CaptureCurrentView(target);
        _cameraController?.CaptureCurrentView(target);
        GD.Print("TYPED_LEARNER_DETAIL_VIEW diagnostic_camera_only "+_currentSceneId);
    }
}
