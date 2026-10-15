using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private string? _tenZoneCaptureState;
    private bool _tenZoneCapturePending;
    private bool _tenZoneCaptureDetail;
    private PanelContainer? _tenZoneCaptureCaption;
    private Label? _tenZoneCaptureCaptionText;
    private bool TenZoneCaptureClockHeld=>_tenZoneCaptureState is not null;
    private void UpdateTenZoneCaptureCaptionLayout()
    {
        if(_tenZoneCaptureCaption is null || _tenZoneCaptureCaptionText is null)return;
        var aperture=_simulatorShell?.SceneViewportRect()??new Rect2();
        var width=MathF.Min(640,aperture.Size.X-24);
        _tenZoneCaptureCaption.Visible=width>48 && aperture.Size.Y>96;
        if(!_tenZoneCaptureCaption.Visible)return;
        _tenZoneCaptureCaption.Position=aperture.Position+new Vector2(12,12);
        _tenZoneCaptureCaptionText.CustomMinimumSize=new Vector2(width-24,0);
        _tenZoneCaptureCaption.Size=new Vector2(width,0);
    }
    private void InitializeTenZoneCaptureOptions(string[] arguments)
    {
        _tenZoneCaptureDetail=arguments.Contains("--capture-tenzone-detail",StringComparer.Ordinal);
        _tenZoneCaptureState=arguments.FirstOrDefault(a=>a.StartsWith("--capture-tenzone-state=",StringComparison.Ordinal))?
            .Substring("--capture-tenzone-state=".Length);
        if(_tenZoneCaptureState is null)
        {
            if(_tenZoneCaptureDetail)throw new InvalidOperationException("Ten-zone detail requires a diagnostic ten-zone capture state.");
            return;
        }
        if(_capturePath is null || _tenZoneCaptureState is not ("all-running" or "blocked-downstream" or "final-beam-stop" or "invalid-current"))
            throw new InvalidOperationException("Ten-zone diagnostic state requires a capture path and a supported state.");
        _tenZoneCapturePending=true;
    }
    private async void PrepareTenZoneCaptureReview()
    {
        if(_tenZoneCaptureState is null)return;
        try
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(_currentSceneId!="lab-10-06-ten-motor-array-startup" || _sceneRuntime?.HasMotorConveyorZones!=true)
                throw new InvalidOperationException("Ten-zone diagnostic capture is restricted to actual Lab10.6.");
            var shell=_simulatorShell!;var runtime=_sceneRuntime!;var state=_tenZoneCaptureState;
            var document=new LadderEditorDocument();
            document.ResetProject("capture-tenzone-"+state,"Diagnostic_TenZone_"+state.Replace('-','_'),TimeSpan.FromMilliseconds(20));
            document.SourceSceneId="lab-10-06-ten-motor-array-startup";
            document.AddTag("request",PlcVariableRole.Input,"group_start_request");
            for(var i=0;i<10;i++)document.AddTag("sense_"+i,PlcVariableRole.Input,"zone_occupied_"+i);
            document.AddAggregateTag("zones",PlcVariableRole.Output,PlcVariableType.Array,
                new(ElementType:PlcVariableType.Bool,LowerBound:0,Length:10),new bool[10],"motor_commands");
            for(var i=0;i<(state=="blocked-downstream"?1:10);i++)
            {
                document.AddRung("Zone "+i+" accepted command",$"zones[{i}]");document.AddContact(i,0,"request",false);
                if(state=="final-beam-stop")document.AddContact(i,0,"sense_9",true);
            }
            string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
            var projectPath=ProjectSettings.GlobalizePath("res://.tools/tenzone-capture-"+state+".rpproj.json");
            File.WriteAllText(projectPath,LadderEditorProjectJson.Save(document),new UTF8Encoding(false));
            if(!shell.OpenNativeMotionReviewProject(projectPath,out var openMessage))throw new InvalidOperationException(openMessage);
            GD.Print("TENZONE_CAPTURE_PROJECT_OPEN "+openMessage);
            RunEditorDraftController();
            if(_virtualController?.Snapshot.State!=VirtualControllerState.Running)throw new InvalidOperationException("Actual current shell draft did not Run.");
            if(!ExecuteSelectedControllerAction("toggle-group_start_request"))throw new InvalidOperationException("Actual request action rejected.");
            void Scan()=>_virtualController!.Advance(.020,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,
                CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,runtime.AdvanceSimulation,
                SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
            for(var scan=0;scan<200;scan++)Scan();
            if(state=="final-beam-stop")
            {
                var limit=0;while(runtime.Points["zone_occupied_9"] is not true && limit++<2000)Scan();
                if(runtime.Points["zone_occupied_9"] is not true)throw new InvalidOperationException("Physical final sensing line not reached.");
                Scan();
                if(Enumerable.Range(0,10).Any(i=>runtime.Points[$"motor_commands[{i}]"] is not false))throw new InvalidOperationException("Feedback-controlled next scan did not stop.");
            }
            if(state=="invalid-current")
            {
                document.ResetProject("capture-invalid-current","Diagnostic_Invalid_Current",TimeSpan.FromMilliseconds(20));
                document.SourceSceneId="lab-10-06-ten-motor-array-startup";
                File.WriteAllText(projectPath,LadderEditorProjectJson.Save(document),new UTF8Encoding(false));
                if(!shell.OpenNativeMotionReviewProject(projectPath,out openMessage))throw new InvalidOperationException(openMessage);
                GD.Print("TENZONE_CAPTURE_INVALID_PROJECT_OPEN "+openMessage);
                RunEditorDraftController();
                if(_virtualController?.Snapshot.State!=VirtualControllerState.Stopped)throw new InvalidOperationException("Invalid current draft did not stop old session.");
            }
            var monitor=typeof(SimulatorShell).GetField("_ladderMonitorMatchesLoadedProgram",BindingFlags.Instance|BindingFlags.NonPublic)
                ??throw new InvalidOperationException("Actual editor monitor identity unavailable.");
            var matches=monitor.GetValue(shell) is true;
            if(matches!=(state!="invalid-current"))throw new InvalidOperationException("Editor monitor/current-program identity mismatch.");
            if(state!="invalid-current" && LadderProgramJson.Save(_virtualProgram! with{WatchVariables=null})!=LadderProgramJson.Save(document.BuildProgram() with{WatchVariables=null}))
                throw new InvalidOperationException("Loaded execution differs from opened current draft.");
            if(_shellView.Length>0)shell.SetReviewState(_shellView,_shellSearch);
            if(_tenZoneCaptureDetail)
            {
                shell.SetReviewState("split",_shellSearch);
                var tables=shell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
                if(tables.Visible)
                    shell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle")
                        .EmitSignal(BaseButton.SignalName.Pressed);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(tables.Visible)throw new InvalidOperationException("Actual SceneIO did not collapse for diagnostic detail.");
                GD.Print("TENZONE_CAPTURE_DETAIL ioCollapsed=true");
            }
            shell.UpdateVirtualController(_virtualController!.Snapshot);
            var model=runtime.CaptureMotorConveyorZones();var installation=_sceneCompositionRoot!.GetNode<Node3D>("MotorConveyorInstallation");
            var captionLayer=new CanvasLayer{Name="TenZoneDiagnosticCaptureCaption",Layer=100};AddChild(captionLayer);
            var panel=new PanelContainer{MouseFilter=Control.MouseFilterEnum.Ignore};
            _tenZoneCaptureCaption=panel;
            panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=new Color("#182B35"),ContentMarginLeft=12,ContentMarginRight=12,ContentMarginTop=6,ContentMarginBottom=6});
            captionLayer.AddChild(panel);
            _tenZoneCaptureCaptionText=new Label{AutowrapMode=TextServer.AutowrapMode.WordSmart,
                MouseFilter=Control.MouseFilterEnum.Ignore,
                Text=$"DIAGNOSTIC CAPTURE · CLOCK HELD · {(matches?"accepted":"last accepted")} scan {_virtualController.Snapshot.ScanNumber}\n{state} · current editor {(matches?"matches loaded program":"INVALID / STOPPED / MISMATCH")} · ephemeral QA"};
            panel.AddChild(_tenZoneCaptureCaptionText);
            UpdateTenZoneCaptureCaptionLayout();
            GD.Print("TENZONE_CAPTURE_IDENTITY "+JsonSerializer.Serialize(new
            {
                state,clockHeld=true,provenance="real-shell-project-open-validation-load-and-current-draft-Run",projectPath,
                projectSha256=Hash(File.ReadAllText(projectPath)),sourceSha256=Hash(File.ReadAllText(ProjectSettings.GlobalizePath("res://scenes/migrated/lab-10-06-ten-motor-array-startup.scene.json"))),
                loadedProgramSha256=Hash(LadderProgramJson.Save(_virtualProgram!)),monitorMatches=matches,
                scan=_virtualController.Snapshot.ScanNumber,runtimeState=_virtualController.Snapshot.State.ToString(),
                inputs=runtime.SampleVirtualControllerInputs(),outputs=_virtualController.Snapshot.Outputs,
                modelCenter=model.Center,modelZone=model.Zone,atDischarge=model.AtDischarge,
                actualCarton=installation.GetNode<Node3D>("TeachingCarton").Position.ToString(),
                zones=Enumerable.Range(0,10).Select(i=>new{index=i,occupiedLine=runtime.Points["zone_occupied_"+i],
                    acceptedCommand=runtime.Points[$"motor_commands[{i}]"],shaft= _sceneCompositionRoot!.GetNode<Node3D>("motor_"+i).FindChild("KIN_motor_shaft",true,false) is Node3D shaft?shaft.Transform.ToString():"missing",
                    drum=installation.GetNode<Node3D>($"Zone_{i}/KIN_zone_drive").Transform.ToString(),
                    beltUv=((StandardMaterial3D)installation.GetNode<MeshInstance3D>($"Zone_{i}/ZoneBeltSurface").MaterialOverride).Uv1Offset.ToString()})
            }));
            _tenZoneCapturePending=false;
            GD.Print("TENZONE_CAPTURE_READY state="+state);
        }
        catch(Exception error){GD.PushError("TENZONE_CAPTURE_PREPARATION_FAILED "+error);GetTree().Quit(2);}
    }
}
