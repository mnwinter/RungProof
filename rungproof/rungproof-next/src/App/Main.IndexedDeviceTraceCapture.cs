using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private string? _indexedTraceCaptureState;
    private bool _indexedTraceCapturePending;
    private async void PrepareIndexedDeviceTraceCapture()
    {
        try
        {
            if(_capturePath is null||_indexedTraceCaptureState is not ("true" or "false"))throw new InvalidOperationException("Indexed capture requires path and true/false QA state.");
            _deviceTraceCaptureHeld=true;
            AddMigratedScene("lab-10-06-ten-motor-array-startup",_candidateCatalog!,_mainCamera!,false,false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var doc=CreateIndexedTraceQaDocument();var path=ProjectSettings.GlobalizePath("res://.tools/indexed-capture-qa.rpproj.json");
            File.WriteAllText(path,LadderEditorProjectJson.Save(doc));
            if(!_simulatorShell!.OpenNativeMotionReviewProject(path,out _))throw new InvalidOperationException("Actual indexed QA project open failed.");
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();RunEditorDraftController();
            void Scan()=>_virtualController!.Advance(.020,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,_sceneRuntime!.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
            if(!ExecuteSelectedControllerAction("toggle-group_start_request"))throw new InvalidOperationException("Request rejected.");Scan();
            if(_indexedTraceCaptureState=="false"){ExecuteSelectedControllerAction("toggle-group_start_request");Scan();}
            var accepted=_virtualController!.Snapshot;
            if(!_simulatorShell.VerifyIndexedDeviceTrace("motor_0","zones[0]",[LadderReferenceKind.Read,LadderReferenceKind.Write],accepted))throw new InvalidOperationException("Actual indexed diagnostic qualification failed.");
            _shellView="signals";_simulatorShell.SetReviewState("signals",string.Empty);_simulatorShell.ShowDeviceTraceForReview("motor_0");
            var tables=_simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
            if(tables.Visible)_simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle").EmitSignal(Button.SignalName.Pressed);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            _simulatorShell.ScrollDeviceTraceForReview();
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var aperture=_simulatorShell.SceneViewportRect();var layer=new CanvasLayer{Layer=100};AddChild(layer);
            var caption=new Label{Position=aperture.Position+new Vector2(12,12),Text=$"EPHEMERAL QA · CLOCK HELD · accepted scan {accepted.ScanNumber}\nIndexed PLC image {_indexedTraceCaptureState}; plant shown after that scan",CustomMinimumSize=new Vector2(420,0),AutowrapMode=TextServer.AutowrapMode.WordSmart};layer.AddChild(caption);
            static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            var refs=LadderProjectIndex.Build(_virtualProgram!).CrossReference("zones[0]").Where(r=>r.Symbol=="zones[0]"&&r.Kind is LadderReferenceKind.Read or LadderReferenceKind.Write).Select(r=>new{r.ElementId,r.BlockId,r.NetworkIndex,kind=r.Kind.ToString()});
            File.WriteAllText(_capturePath+".indexed-trace.json",JsonSerializer.Serialize(new{scene=_currentSceneId,state=_indexedTraceCaptureState,clockHeld=true,ephemeralQA=true,programSha256=Hash(LadderProgramJson.Save(_virtualProgram!)),draftSha256=Hash(_simulatorShell.CaptureIntegratedLadderDraft()),scan=accepted.ScanNumber,acceptedVariables=accepted.Variables,refs}));
            _indexedTraceCapturePending=false;GD.Print("INDEXED_TRACE_CAPTURE_READY state="+_indexedTraceCaptureState);
        }
        catch(Exception error){GD.PushError("INDEXED_TRACE_CAPTURE_FAILED "+error);GetTree().Quit(2);}
    }
}