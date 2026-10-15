using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private string? _workspaceRollerCaptureState;
    private bool _workspaceRollerCapturePending;
    private async void PrepareWorkspaceRollerCapture()
    {
        try
        {
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var state=_workspaceRollerCaptureState;
            if(_capturePath is null||state is not ("intended" or "coasting" or "wrong-terminal" or "invalid-current"))throw new InvalidOperationException("Finite roller capture requires supported QA state and capture path.");
            var preset=WorkspaceFiniteRollerPolicy.Create();var p=preset.FiniteRollerPreset!;
            AddMigratedScene(preset.SourceSceneId,_candidateCatalog!,_mainCamera!,false,false);
            MarkWorkspaceSaved();
            _simulatorShell!.GetNode<MenuButton>("Workspace/Toolbar/ToolbarMargin/ToolbarRow/FileMenu").GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed,5L);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(_sceneRuntime?.HasWorkspaceRollerPreset!=true)throw new InvalidOperationException("Actual File menu did not create finite preset.");
            var doc=new LadderEditorDocument();doc.ResetProject("roller-capture-"+state,"Finite_QA_"+state.Replace('-','_'),TimeSpan.FromMilliseconds(20));doc.SourceSceneId=preset.SourceSceneId;
            doc.AddTag("start",PlcVariableRole.Input,p.StartPoint);doc.AddTag("beam",PlcVariableRole.Input,p.BeamPoint);doc.AddTag("run",PlcVariableRole.Output,p.RunPoint);
            doc.AddRung("QA momentary seal in","run");doc.AddContact(0,0,"start",false);
            if(state is "intended" or "coasting")doc.AddContact(0,0,"beam",true);
            doc.AddParallelBranch(0);doc.AddContact(0,1,"run",false);
            if(state is "intended" or "coasting")doc.AddContact(0,1,"beam",true);
            var draft=LadderEditorProjectJson.Save(doc);
            var projectPath=ProjectSettings.GlobalizePath("res://.tools/roller-capture-"+state+".rpworkspace.json");
            WorkspaceProjectBundleRepository.Save(new(1,WorkspaceProjectBundleRepository.Kind,CreateWorkspaceDocument(),draft),projectPath,_candidateCatalog!,_sceneCatalog!);
            MarkWorkspaceSaved();LoadWorkspaceFromPath(projectPath);
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();
            if(_simulatorShell.CaptureIntegratedLadderDraft()!=draft||_virtualController is not null)throw new InvalidOperationException("Joint reopen did not preserve stopped custom draft.");
            var geometry=SceneComposer.MeasureWorkspaceRoller(_sceneCompositionRoot!);

            RunEditorDraftController();
            if(!ExecuteSelectedControllerAction("roller-workspace-press"))throw new InvalidOperationException("Actual momentary request action rejected.");
            var runtime=_sceneRuntime!;
            void Scan()=>_virtualController!.Advance(.020,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,runtime.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs);
            Scan();Scan();
            var count=0;
            if(state=="invalid-current")
            {
                for(var i=0;i<50;i++)Scan();
                var invalid=new LadderEditorDocument();invalid.ResetProject("finite-invalid-current","Finite_Invalid_Current",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=preset.SourceSceneId;
                var invalidPath=ProjectSettings.GlobalizePath("res://.tools/roller-capture-invalid-current.rpproj.json");File.WriteAllText(invalidPath,LadderEditorProjectJson.Save(invalid));
                if(!_simulatorShell.OpenNativeMotionReviewProject(invalidPath,out _))throw new InvalidOperationException("Actual invalid draft open failed.");
                RunEditorDraftController();
                if(_virtualController!.Snapshot.State==VirtualControllerState.Running)throw new InvalidOperationException("Invalid current ran previous program.");
                var before=runtime.CaptureWorkspaceRollerPreset();Scan();
                if(runtime.CaptureWorkspaceRollerPreset()!=before)throw new InvalidOperationException("Stopped invalid current advanced plant.");
            }
            else
            {
                while(count++<1500)
                {
                    Scan();var physical=runtime.CaptureWorkspaceRollerPreset();
                    if(state=="intended"&&_workspaceRollerObservedTrial?.State==FiniteRollerTrialState.Observed)break;
                    if(state=="coasting"&&_virtualController!.Snapshot.Variables["beam"]&&runtime.Points[p.RunPoint] is false&&physical.SpeedMps>0)break;
                    if(state=="wrong-terminal"&&physical.AtTerminal)break;
                }
                if(count>=1500)throw new InvalidOperationException("Finite QA state not reached.");

            }
            if(_shellView.Length>0)_simulatorShell.SetReviewState(_shellView,_shellSearch);
            PublishWorkspaceRollerObservedTrial();
            var detail=OS.GetCmdlineUserArgs().Contains("--workspace-roller-detail",StringComparer.Ordinal);
            var objective=OS.GetCmdlineUserArgs().Contains("--workspace-roller-objective",StringComparer.Ordinal);
            if(detail)
            {
                _shellView="split";_simulatorShell.SetReviewState("split",string.Empty);
                var tables=_simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
                if(tables.Visible)_simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle").EmitSignal(Button.SignalName.Pressed);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(tables.Visible)throw new InvalidOperationException("Finite QA SceneIO did not collapse.");
                GD.Print("WORKSPACE_ROLLER_CAPTURE_DETAIL ioCollapsed=true");
            }
            if(objective)
            {
                if(detail)throw new InvalidOperationException("Objective capture requires operator scene view, not split detail.");
                _shellView="scene";_simulatorShell.SetReviewState("scene",string.Empty);
                var tables=_simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
                if(tables.Visible)_simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle").EmitSignal(Button.SignalName.Pressed);

                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                _simulatorShell.ScrollWorkspaceRollerTrialForReview();
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if(tables.Visible||!_simulatorShell.WorkspaceRollerTrialCardFitsForReview())throw new InvalidOperationException("Actual objective card is clipped or SceneIO was not collapsed.");
                GD.Print("WORKSPACE_ROLLER_CAPTURE_OBJECTIVE ioCollapsed=true objective=true cardFits=true");
            }
            var aperture=_simulatorShell.SceneViewportRect();var layer=new CanvasLayer { Name="FiniteRollerDiagnosticCaption",Layer=100 };AddChild(layer);
            var caption=new Label { Position=aperture.Position+new Vector2(12,12),CustomMinimumSize=new Vector2(MathF.Min(520,aperture.Size.X-24),0),AutowrapMode=TextServer.AutowrapMode.WordSmart,
                Text="EPHEMERAL QA · CLOCK HELD · "+state+(state=="invalid-current"?" · CURRENT DRAFT INVALID":"")+"\n"+(_virtualController!.Snapshot.State==VirtualControllerState.Running?"Accepted scan ":"STOPPED · last accepted scan ")+_virtualController.Snapshot.ScanNumber+" · no physical PLC\nUnit-scale pallet. PLC OFF coasts; current wood beam differs from trigger history." };
            caption.AddThemeColorOverride("font_color",new Color("edf5f1"));caption.AddThemeColorOverride("font_shadow_color",new Color("15242a"));caption.AddThemeConstantOverride("shadow_offset_x",1);caption.AddThemeConstantOverride("shadow_offset_y",1);layer.AddChild(caption);
            static string Hash(string value)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
            var physicalState=runtime.CaptureWorkspaceRollerPreset();
            if(state=="intended"&&_workspaceRollerObservedTrial?.State!=FiniteRollerTrialState.Observed)throw new InvalidOperationException("Intended trial not observed.");
            if(state=="wrong-terminal"&&(!physicalState.AtTerminal||runtime.Points[p.RunPoint] is not true||_workspaceRollerObservedTrial?.State!=FiniteRollerTrialState.NeedsAttention))throw new InvalidOperationException("Wrong terminal evidence incomplete.");
            var assembly=ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/RungProof.Next.dll");
            if(!File.Exists(assembly))throw new InvalidOperationException("Current compiled assembly identity unavailable.");
            File.WriteAllText(_capturePath+".workspace-roller.json",JsonSerializer.Serialize(new{state,detail,objective,ephemeralQA=true,clockHeld=true,scene=_currentSceneId,sourceId=preset.SourceSceneId,
                dllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly))),draftSha256=Hash(_simulatorShell.CaptureIntegratedLadderDraft()),programSha256=Hash(LadderProgramJson.Save(_virtualProgram!)),
                acceptedScan=_virtualController!.Snapshot.ScanNumber,runtimeState=_virtualController.Snapshot.State.ToString(),currentDraftValid=state!="invalid-current",acceptedProgramHistorical=state=="invalid-current",
                loadedIdentityCurrent=_simulatorShell.LoadedDraftIdentityCurrent,observedTrialState=_workspaceRollerObservedTrial?.State.ToString(),observedTrialDetail=_workspaceRollerObservedTrial?.Detail,
                beamTriggerScan=_workspaceRollerObservedTrial?.BeamTriggerScanNumber,observerLastScan=_workspaceRollerObservedTrial?.LastScanNumber,
                acceptedVariables=_virtualController.Snapshot.Variables,acceptedOutputs=_virtualController.Snapshot.Outputs,simulatedTimeMs=_virtualController.Snapshot.SimulatedTime.TotalMilliseconds,
                points=runtime.Points,physical=physicalState,relation=p,geometry=new{geometry.CrownY,geometry.HomeCenter,geometry.TerminalCenter,rayFrom=geometry.RayFrom.ToString(),rayTo=geometry.RayTo.ToString()},projectPath}));
            _workspaceRollerCapturePending=false;GD.Print("WORKSPACE_ROLLER_CAPTURE_READY state="+state+" ephemeralQA=true clockHeld=true");
        }
        catch(Exception error){GD.PushError("WORKSPACE_ROLLER_CAPTURE_FAILED "+error);GetTree().Quit(2);}
    }
}
