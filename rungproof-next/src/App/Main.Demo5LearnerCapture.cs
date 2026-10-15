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
    private string? _demo5LearnerCaptureState;
    private bool _demo5LearnerCapturePending;
    private bool _demo5LearnerCaptureAdvancing;
    private bool Demo5LearnerCaptureClockHeld=>_demo5LearnerCaptureState is not null&&!_demo5LearnerCaptureAdvancing;
    private void AdvanceDemo5LearnerCapture(int count=1)
    {
        _demo5LearnerCaptureAdvancing=true;
        try{for(var i=0;i<count;i++)_PhysicsProcess(.020);}
        finally{_demo5LearnerCaptureAdvancing=false;}
    }
    private async void PrepareDemo5LearnerCapture()
    {
        const string scene="lab-11-13-xy-palletizing";
        try
        {
            var state=_demo5LearnerCaptureState;
            if(_capturePath is null||state is not ("initial" or "carry" or "completed" or "wrong-carry" or "invalid-current"))
                throw new InvalidOperationException("Demo5 capture requires an explicit supported state and capture path.");
            AddMigratedScene(scene,_candidateCatalog!,_mainCamera!,false,false);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            var disk=ProjectSettings.GlobalizePath("res://programs/demos/05-integrated-cell-multi-fb-fc.rpproj.json");
            if(!_simulatorShell!.OpenNativeMotionReviewProject(disk,out var opened))throw new InvalidOperationException(opened);
            var originalDraft=_simulatorShell.CaptureIntegratedLadderDraft();
            var projectPath=ProjectSettings.GlobalizePath("res://.tools/demo5-learner-capture-"+state+".rpworkspace.json");
            SaveWorkspaceToPath(projectPath);
            AddMigratedScene("lab-2-01-workstation-call",_candidateCatalog!,_mainCamera!,false,false);
            MarkWorkspaceSaved();LoadWorkspaceFromPath(projectPath);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            _simulatorShell.DismissIntegratedLoadConfirmationForReview();
            if(_currentSceneId!=scene||_simulatorShell.CaptureIntegratedLadderDraft()!=originalDraft
                ||_virtualController?.Snapshot.State==VirtualControllerState.Running)
                throw new InvalidOperationException("Joint Demo5 reopen did not retain exact stopped learner draft.");
            RunEditorDraftController();
            if(_virtualController?.Snapshot.State!=VirtualControllerState.Running)throw new InvalidOperationException("Actual current Demo5 draft did not Run.");
            var runtime=_sceneRuntime!;
            if(state=="initial")
            {
                StopActiveController();
                if(runtime.Points["gantry_home"] is not true||runtime.Points["carton_attached"] is not false
                    ||Convert.ToInt32(runtime.Points["placed_cartons"])!=0||_virtualController!.Snapshot.ScanNumber!=0)
                    throw new InvalidOperationException("Initial Demo5 home/empty placement/scan0 state is not genuine.");
            }
            else
            {
                if(!ExecuteSelectedControllerAction("start-palletizer"))throw new InvalidOperationException("Real Demo5 start action rejected.");
                if(state=="completed")
                {
                    var ticks=0;
                    do{AdvanceDemo5LearnerCapture();ticks++;}while(ticks<6000&&runtime.Points["layer_complete"] is not true);
                    if(ticks>=6000||Convert.ToInt32(runtime.Points["placed_cartons"])!=4||runtime.Points["gantry_home"] is not true)
                        throw new InvalidOperationException("Actual Demo5 four-carton scanned sequence did not complete.");
                    if(runtime.Points["layer_complete"] is not true||runtime.Points["carton_attached"] is not false
                        ||_sceneCompositionRoot!.GetChildren().OfType<Node3D>().Count(n=>n.Name.ToString().StartsWith("PlacedCarton_",StringComparison.Ordinal)&&n.Visible)!=4)
                        throw new InvalidOperationException("Four actual visible placed cartons/layer completion not reached.");
                }
                else
                {
                    var ticks=0;
                    while((runtime.Points["carton_attached"] is not true||runtime.Points["gantry_home"] is true)&&ticks++<1500)
                        AdvanceDemo5LearnerCapture();
                    if(ticks>=1500)throw new InvalidOperationException("Actual attached/away carry not reached.");
                    var carryPose=Demo5LearnerPoseIdentity();
                    if(state=="wrong-carry")
                    {
                        var load=LadderEditorProjectJson.Load(originalDraft);
                        var wrong=load.Document??throw new InvalidOperationException("Actual saved intended draft is not readable.");
                        wrong.AddTag("wrong_vacuum_inhibit",PlcVariableRole.Memory);
                        foreach(var branch in wrong.Rungs.Single(r=>r.CoilVariable=="vacuum_request").Branches)
                            branch.Contacts.Add(new("capture-inhibit-"+branch.Id,"wrong_vacuum_inhibit",false));
                        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(wrong));
                        if(!_simulatorShell.TryBuildCurrentLadderProgram(out _,out _)||Demo5LearnerPoseIdentity()!=carryPose)
                            throw new InvalidOperationException("Valid wrong draft-only change altered actual carry.");
                        RunEditorDraftController();
                        if(Demo5LearnerPoseIdentity()!=carryPose||!ExecuteSelectedControllerAction("start-palletizer"))
                            throw new InvalidOperationException("Current wrong Run reset carry or fresh start rejected.");
                        AdvanceDemo5LearnerCapture(2);
                        if(runtime.Points["vacuum_pick"] is not false||runtime.Points["palletizer_fault"] is not true
                            ||runtime.Points["carton_attached"] is not true||Convert.ToInt32(runtime.Points["placed_cartons"])!=0)
                            throw new InvalidOperationException("Wrong-current carry did not show actual vacuum-loss fault with retained attachment.");
                        // Persist this real wrong draft, but do not reopen/reset the carry being shown.
                        SaveWorkspaceToPath(projectPath);
                    }
                    if(state=="invalid-current")
                    {
                        var invalid=new LadderEditorDocument();invalid.ResetProject("demo5-capture-invalid","Invalid_Current",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=scene;
                        var invalidPath=ProjectSettings.GlobalizePath("res://.tools/demo5-capture-invalid.rpproj.json");
                        File.WriteAllText(invalidPath,LadderEditorProjectJson.Save(invalid));
                        if(!_simulatorShell.OpenNativeMotionReviewProject(invalidPath,out opened))throw new InvalidOperationException(opened);
                        RunEditorDraftController();
                        var scan=_virtualController!.Snapshot.ScanNumber;var time=_virtualController.Snapshot.SimulatedTime;
                        var invalidDraft=_simulatorShell.CaptureIntegratedLadderDraft();
                        AdvanceDemo5LearnerCapture(25);
                        if(_virtualController.Snapshot.State==VirtualControllerState.Running||_virtualController.Snapshot.Outputs.Values.Any(v=>v)
                            ||Demo5LearnerPoseIdentity()!=carryPose||_virtualController.Snapshot.ScanNumber!=scan
                            ||_virtualController.Snapshot.SimulatedTime!=time||_simulatorShell.CaptureIntegratedLadderDraft()!=invalidDraft)
                            throw new InvalidOperationException("Invalid current draft failed to freeze carry/scan/time or cleared-output state.");
                        SaveWorkspaceToPath(projectPath); // exact readable invalid draft, no reopen or implicit execution
                    }
                }
            }
            _simulatorShell.SetReviewState(_shellView.Length>0?_shellView:"scene",string.Empty);
            var tables=_simulatorShell.GetNode<Control>("Workspace/DiagnosticsDock/DiagnosticsBody/OperatorPointTables");
            if(tables.Visible)_simulatorShell.GetNode<Button>("Workspace/DiagnosticsDock/DiagnosticsBody/DiagnosticsHeader/DiagnosticsToggle").EmitSignal(Button.SignalName.Pressed);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
            if(tables.Visible)throw new InvalidOperationException("Actual Scene IO collapse failed.");
            var snapshot=_virtualController!.Snapshot;
            var aperture=_simulatorShell.SceneViewportRect();var layer=new CanvasLayer{Name="Demo5LearnerDiagnosticCaption",Layer=100};AddChild(layer);
            var caption=new Label{Position=aperture.Position+new Vector2(12,12),CustomMinimumSize=new Vector2(MathF.Min(550,aperture.Size.X-24),0),AutowrapMode=TextServer.AutowrapMode.WordSmart,
                Text=$"EPHEMERAL QA · CLOCK HELD · Demo 5 / Lab 11.13 · {state}\n{snapshot.State} · {(state=="invalid-current"?"LAST ACCEPTED":"accepted")} scan {snapshot.ScanNumber} · no physical PLC\nPLC step {snapshot.NumericVariables["sequence_step"]} · vacuum {runtime.Points["vacuum_pick"]}\nActual attached {runtime.Points["carton_attached"]} · placed {runtime.Points["placed_cartons"]}/4 · fault {runtime.Points["palletizer_fault"]}"};
            caption.AddThemeColorOverride("font_color",new Color("edf5f1"));caption.AddThemeColorOverride("font_shadow_color",new Color("15242a"));caption.AddThemeConstantOverride("shadow_offset_x",1);caption.AddThemeConstantOverride("shadow_offset_y",1);layer.AddChild(caption);
            var assembly=ProjectSettings.GlobalizePath("res://.godot/mono/temp/bin/Debug/RungProof.Next.dll");
            if(!File.Exists(assembly))throw new InvalidOperationException("Pinned current DLL is missing.");
            static string Hash(string s)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
            var root=_sceneCompositionRoot!;
            var physical=root.GetNode<Node3D>("training_accessory_4").FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
                .Where(m=>m.Name.ToString().StartsWith("KIN_",StringComparison.Ordinal)||m.Name=="GANTRY_GRIPPER")
                .Select(m=>new{name=m.Name.ToString(),pose=m.Transform.ToString(),bounds=ReviewBounds(m).ToString()}).ToArray();
            File.WriteAllText(_capturePath+".demo5-learner.json",JsonSerializer.Serialize(new{state,ephemeralQA=true,clockHeld=true,scene=_currentSceneId,sourceId=scene,
                dllSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly))),draftSha256=Hash(_simulatorShell.CaptureIntegratedLadderDraft()),programSha256=Hash(LadderProgramJson.Save(_virtualProgram!)),
                originalReopenedDraftSha256=Hash(originalDraft),currentDraftValid=state!="invalid-current",loadedIdentityCurrent=_simulatorShell.LoadedDraftIdentityCurrent,
                acceptedProgramHistorical=state=="invalid-current",acceptedScan=snapshot.ScanNumber,runtimeState=snapshot.State.ToString(),simulatedTimeMs=snapshot.SimulatedTime.TotalMilliseconds,
                acceptedVariables=snapshot.Variables,acceptedNumericVariables=snapshot.NumericVariables,acceptedOutputs=snapshot.Outputs,points=runtime.Points,
                physical,carton=new{pose=root.GetNode<Node3D>("box_2").Transform.ToString(),bounds=ReviewMeshes(root.GetNode<Node3D>("box_2")).Select(m=>ReviewBounds(m).ToString()).ToArray()},
                placed=root.GetChildren().OfType<Node3D>().Where(n=>n.Name.ToString().StartsWith("PlacedCarton_",StringComparison.Ordinal)).Select(n=>new{name=n.Name.ToString(),visible=n.Visible,pose=n.Transform.ToString(),bounds=ReviewMeshes(n).Select(m=>ReviewBounds(m).ToString()).ToArray()}).ToArray(),
                ioCollapsed=true,projectPath,jointReopenedBeforeRun=true,currentNegativeDraftSavedWithoutCarryReset=state is "wrong-carry" or "invalid-current"}));
            _demo5LearnerCapturePending=false;
            GD.Print("DEMO5_LEARNER_CAPTURE_READY state="+state+" ephemeralQA=true clockHeld=true ioCollapsed=true");
        }
        catch(Exception error){GD.PushError("DEMO5_LEARNER_CAPTURE_FAILED "+error);GetTree().Quit(2);}
    }
    private string Demo5LearnerPoseIdentity()=>string.Join("|",_sceneCompositionRoot!.FindChildren("*","Node3D",true,false).OfType<Node3D>().Select(n=>n.GetPath()+":"+n.Transform));
}
