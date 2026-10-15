using System;
using System.IO;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _deviceTraceCaptureHeld;
    private bool _deviceTraceCapturePending;
    private async void PrepareDeviceTraceCaptureReview()
    {
        try
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(_capturePath is null||_currentSceneId!="lab-4-01-press-count-lamp")throw new InvalidOperationException("Device trace QA capture requires exact Lab4.1 and capture path.");
            var document=new LadderEditorDocument();document.ResetProject("trace-capture-QA","Trace_QA",TimeSpan.FromMilliseconds(20));document.SourceSceneId=_currentSceneId;
            document.AddTag("pulse_received",PlcVariableRole.Input,"pulse_received");document.AddTag("threshold_lamp",PlcVariableRole.Output,"threshold_lamp");
            document.AddRung("QA lamp follows declared input","threshold_lamp");document.AddContact(0,0,"pulse_received",false);
            _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(document));
            RunEditorDraftController();
            _sceneRuntime!.ExecuteAction("toggle-pulse_received");_PhysicsProcess(.02);
            if(_virtualController!.Snapshot.ScanNumber!=1||!_virtualController.Snapshot.Variables["threshold_lamp"])throw new InvalidOperationException("Expected actual accepted QA scan not reached.");
            _deviceTraceCaptureHeld=true;
            _shellView="signals";
            _simulatorShell.SetIntegratedSceneIdentity();
            _simulatorShell.ShowDeviceTraceForReview("indicator_1");
            var aperture=_simulatorShell.SceneViewportRect();
            var layer=new CanvasLayer { Name="DeviceTraceDiagnosticCaption",Layer=100 };AddChild(layer);
            var caption=new PanelContainer { Position=aperture.Position+new Vector2(12,12),Size=new Vector2(MathF.Min(540,aperture.Size.X-24),0) };
            var style=new StyleBoxFlat { BgColor=new Color("18272eee"),BorderColor=new Color("93bdb0"),BorderWidthBottom=1,ContentMarginLeft=10,ContentMarginRight=10,ContentMarginTop=8,ContentMarginBottom=8 };
            caption.AddThemeStyleboxOverride("panel",style);
            var explanation=new Label { Text="EPHEMERAL QA · CLOCK HELD · accepted scan 1\nDEVICE TRACE values = accepted snapshot. Symbolic points = current plant image.",AutowrapMode=TextServer.AutowrapMode.WordSmart,CustomMinimumSize=new Vector2(MathF.Max(120,MathF.Min(520,aperture.Size.X-44)),0) };
            explanation.AddThemeColorOverride("font_color",new Color("edf5f1"));explanation.AddThemeFontSizeOverride("font_size",13);
            caption.AddChild(explanation);layer.AddChild(caption);            _simulatorShell.SetWorkspaceStatus("EPHEMERAL QA · accepted scan 1 held for diagnostics screenshot · actual scalar binding · not product draft");
            File.WriteAllText(_capturePath+".device-trace.json",JsonSerializer.Serialize(new{scene=_currentSceneId,provenance="explicit-ephemeral-QA-real-editor-Run-actual-scene-pulse",scan=_virtualController.Snapshot.ScanNumber,input=_virtualController.Snapshot.Variables["pulse_received"],output=_virtualController.Snapshot.Variables["threshold_lamp"],clockHeld=true,equipment="indicator_1"}));
            _deviceTraceCapturePending=false;GD.Print("DEVICE_TRACE_CAPTURE_READY acceptedScan=1 input=True output=True ephemeralQA=true");
        }
        catch(Exception error){GD.PushError("DEVICE_TRACE_CAPTURE_FAILED "+error);GetTree().Quit(2);}
    }
}
