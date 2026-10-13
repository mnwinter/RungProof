using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RungProof.Next.VirtualController;
using RungProof.Next.Workspace;
namespace RungProof.Next.App;
public partial class Main
{
    private bool _verifyFiveDemoLearner;
    private async void VerifyFiveDemoLearner()
    {
        var failures=0;
        void Check(bool value,string name){GD.Print($"FIVE_DEMO_LEARNER_CHECK {name}={value}");if(!value)failures++;}
        try
        {
            // Existing detailed plant geometry/sequence audit remains separate from current-draft tests.
            VerifyPalletizerWorkflow(Check);
            foreach(var (scene,relative) in AuthoredDemoLadderPrograms.ProjectFiles)
            {
                AddMigratedScene(scene,_candidateCatalog!,_mainCamera!,false,false);
                await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                Check(_simulatorShell!.OpenNativeMotionReviewProject(ProjectSettings.GlobalizePath("res://"+relative),out var opened),"actual_disk_project_open_"+scene);GD.Print(opened);
                Check(_simulatorShell.TryBuildCurrentLadderProgram(out _,out _),"actual_current_draft_compiles_and_scene_binds_"+scene);
                await FiveDemoJointRoundtrip(scene,"intended",Check);
                RunEditorDraftController();Check(_virtualController!.Snapshot.State==VirtualControllerState.Running,"actual_current_draft_run_"+scene);
                FiveDemoAssertIntended(scene,Check);
                StopActiveController();ResetActiveController();
                if(scene=="lab-11-13-xy-palletizing")
                    await FiveDemoPalletizerNegativeCases(Check);
                else
                    await FiveDemoNegativeCases(scene,Check);
            }
        }
        catch(Exception error){failures++;GD.PushError("FIVE_DEMO_LEARNER_EXCEPTION "+error);}
        GD.Print("FIVE_DEMO_LEARNER_RESULT failures="+failures+" scope=20ms_actual_editor_joint_offline");
        GetTree().Quit(failures==0?0:1);
    }
    private void FiveDemoTick(int scans=1){for(var i=0;i<scans;i++)_PhysicsProcess(.020);}
    private bool FiveDemoLamp(string id,bool on)
    {
        var lenses=_sceneCompositionRoot!.GetNode<Node3D>(id).FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
            .Where(m=>m.Name.ToString().Contains("LENS",StringComparison.OrdinalIgnoreCase)).ToArray();
        return lenses.Length>0&&(on?lenses.Any(m=>m.MaterialOverride is StandardMaterial3D{EmissionEnabled:true}):lenses.All(m=>m.MaterialOverride is StandardMaterial3D{EmissionEnabled:false}));
    }
    private string FiveDemoPose()=>string.Join("|",_sceneCompositionRoot!.FindChildren("*","Node3D",true,false).OfType<Node3D>().Select(n=>n.GetPath()+":"+n.Transform));
    private async Task FiveDemoJointRoundtrip(string scene,string mode,Action<bool,string> check)
    {
        var draft=_simulatorShell!.CaptureIntegratedLadderDraft();var layout=JsonSerializer.Serialize(CreateWorkspaceDocument());
        var path=ProjectSettings.GlobalizePath($"res://.tools/five-demo-{scene}-{mode}.rpworkspace.json");
        SaveWorkspaceToPath(path);check(File.Exists(path),"actual_joint_save_"+scene+"_"+mode);
        _simulatorShell.DismissIntegratedLoadConfirmationForReview();
        // Navigate away so reopen cannot pass simply by leaving the old draft resident.
        AddMigratedScene("lab-2-01-workstation-call",_candidateCatalog!,_mainCamera!,false,false);
        MarkWorkspaceSaved();LoadWorkspaceFromPath(path);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        check(_currentSceneId==scene&&_simulatorShell.CaptureIntegratedLadderDraft()==draft&&JsonSerializer.Serialize(CreateWorkspaceDocument())==layout,"joint_reopen_exact_layout_actual_draft_"+scene+"_"+mode);
        check(_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running,"joint_reopen_no_implicit_run_"+scene+"_"+mode);
    }
    private void FiveDemoAssertIntended(string scene,Action<bool,string> check)
    {
        var runtime=_sceneRuntime!;
        switch(scene)
        {
            case "lab-4-01-press-count-lamp":
                for(var i=1;i<=3;i++)
                {
                    check(ExecuteSelectedControllerAction("toggle-pulse_received"),"demo1_actual_pulse_"+i);FiveDemoTick(2);
                    check(_virtualController!.Snapshot.Counters["press_count"].Accumulated==i&&Equals(runtime.Points["threshold_lamp"],i==3)&&FiveDemoLamp("indicator_1",i==3),"demo1_current_draft_count_output_actual_lens_"+i);
                }
                FiveDemoTick(20);check(_virtualController!.Snapshot.Counters["press_count"].Accumulated==3,"demo1_idle_no_recount");break;
            case "lab-5-01-delayed-lamp":
                check(ExecuteSelectedControllerAction("toggle-timer_request"),"demo2_actual_maintained_request");FiveDemoTick(99);
                check(runtime.Points["delayed_lamp"] is false&&FiveDemoLamp("indicator_1",false)&&Math.Abs(_virtualController!.Snapshot.Timers["delay_timer"].Accumulated.TotalSeconds-1.98)<1e-9,"demo2_99_scans_timer_output_actual_lens_off");FiveDemoTick();
                check(runtime.Points["delayed_lamp"] is true&&FiveDemoLamp("indicator_1",true),"demo2_100th_scan_actual_lens_on");
                ExecuteSelectedControllerAction("toggle-timer_request");FiveDemoTick();check(runtime.Points["delayed_lamp"] is false&&FiveDemoLamp("indicator_1",false),"demo2_request_off_resets_lamp");break;
            case "scene-1-conveyor-stop":
                var carton=_sceneCompositionRoot!.GetNode<Node3D>("scene1_product");var home=carton.Transform;
                check(ExecuteSelectedControllerAction("scene-toggle"),"demo3_actual_start");FiveDemoTick(2);
                check(runtime.Points["conveyor_running"] is true&&carton.Transform!=home&&!_virtualController!.Snapshot.Variables["start_command"],"demo3_release_seal_actual_motion");
                var count=0;while(runtime.Points["simulated_photoeye"] is not true&&count++<300)FiveDemoTick();
                check(runtime.Points["simulated_photoeye"] is true&&!_virtualController!.Snapshot.Variables["simulated_photoeye"],"demo3_physical_feedback_before_sample");FiveDemoTick();var held=carton.Transform;
                check(runtime.Points["conveyor_running"] is false&&_virtualController!.Snapshot.Variables["simulated_photoeye"],"demo3_exact_next_scan_off");FiveDemoTick(20);check(carton.Transform==held,"demo3_actual_carton_hold");break;
            case "lab-9-11-pallet-counting":
                ExecuteSelectedControllerAction("toggle-count_request");ExecuteSelectedControllerAction("toggle-pallet_type_valid");FiveDemoTick(2);
                for(var i=1;i<=5;i++)
                {
                    ExecuteSelectedControllerAction("toggle-pallet_detected");FiveDemoTick(2);
                    check(Equals(runtime.Points["pallet_count"],(long)i)&&_sceneCompositionRoot!.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout").Text==$"COUNT\n{i}"&&Equals(runtime.Points["pallet_count_valid"],i==5),"demo4_actual_same_scan_count_readout_validity_"+i);
                    ExecuteSelectedControllerAction("toggle-pallet_detected");FiveDemoTick(2);
                }
                break;
            case "lab-11-13-xy-palletizing":
                for(var i=0;i<4;i++)
                {
                    if(i>0)check(ExecuteSelectedControllerAction("load-carton"),"demo5_actual_load_"+i);
                    check(ExecuteSelectedControllerAction("start-palletizer"),"demo5_actual_start_"+i);var ticks=0;bool attached=false,place=false;
                    do{FiveDemoTick();ticks++;attached|=runtime.Points["carton_attached"] is true;place|=runtime.Points["at_place"] is true;}while(ticks<1500&&runtime.Points["gantry_cycle"] is not false);
                    check(ticks<1500&&attached&&place&&runtime.Points["gantry_home"] is true&&Convert.ToInt32(runtime.Points["placed_cartons"])==i+1&&_virtualController!.Snapshot.Counters["cycle_count"].Accumulated==i+1,"demo5_current_saved_draft_actual_place_home_count_"+i);
                }
                check(runtime.Points["layer_complete"] is true&&runtime.Points["vacuum_pick"] is false&&runtime.Points["gantry_cycle"] is false&&_sceneCompositionRoot!.GetNodeOrNull<Node3D>("PlacedCarton_3") is not null,"demo5_current_saved_draft_four_actual_cartons_layer_complete");break;
        }
    }
    private LadderEditorDocument FiveDemoWrong(string scene)
    {
        AuthoredDemoLadderPrograms.TryCreate(scene,out var doc);
        switch(scene)
        {
            case "lab-4-01-press-count-lamp":doc.Rungs.Single(r=>r.IsCounter).CounterPreset=1;break;
            case "lab-5-01-delayed-lamp":doc.Rungs.Single(r=>r.IsTimer).TimerPreset=TimeSpan.FromMilliseconds(20);break;
            case "scene-1-conveyor-stop":
                foreach(var branch in doc.Rungs.SelectMany(r=>r.Branches))branch.Contacts.RemoveAll(c=>c.Variable=="simulated_photoeye");break;
            case "lab-9-11-pallet-counting":
                foreach(var branch in doc.Blocks.SelectMany(b=>b.Rungs).Where(r=>r.IsCounter).SelectMany(r=>r.Branches))branch.Contacts.RemoveAll(c=>c.Variable=="pallet_type_valid");break;
            case "lab-11-13-xy-palletizing":
                doc.AddTag("wrong_vacuum_inhibit",PlcVariableRole.Memory);
                foreach(var branch in doc.Rungs.Single(r=>r.CoilVariable=="vacuum_pick").Branches)
                    branch.Contacts.Add(new("wrong-inhibit-"+branch.Id,"wrong_vacuum_inhibit",false));break;
        }
        return doc;
    }
    private void FiveDemoOpenDocument(LadderEditorDocument doc,string mode,Action<bool,string> check)
    {
        var path=ProjectSettings.GlobalizePath($"res://.tools/five-demo-{doc.SourceSceneId}-{mode}.rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(doc));
        check(_simulatorShell!.OpenNativeMotionReviewProject(path,out var result),"actual_project_open_"+doc.SourceSceneId+"_"+mode);GD.Print(result);
    }
    private async Task FiveDemoNegativeCases(string scene,Action<bool,string> check)
    {
        FiveDemoOpenDocument(FiveDemoWrong(scene),"wrong",check);
        check(_simulatorShell!.TryBuildCurrentLadderProgram(out _,out _),"wrong_actual_scene_contract_compile_valid_"+scene);
        await FiveDemoJointRoundtrip(scene,"wrong",check);RunEditorDraftController();var runtime=_sceneRuntime!;
        switch(scene)
        {
            case "lab-4-01-press-count-lamp":ExecuteSelectedControllerAction("toggle-pulse_received");FiveDemoTick(2);check(_virtualController!.Snapshot.Counters["press_count"].Accumulated==1&&runtime.Points["threshold_lamp"] is true&&FiveDemoLamp("indicator_1",true),"valid_wrong_demo1_actual_lamp_premature_one_press");break;
            case "lab-5-01-delayed-lamp":ExecuteSelectedControllerAction("toggle-timer_request");FiveDemoTick();check(runtime.Points["delayed_lamp"] is true&&FiveDemoLamp("indicator_1",true)&&_virtualController!.Snapshot.Timers["delay_timer"].Accumulated.TotalSeconds==.02,"valid_wrong_demo2_actual_lamp_premature_20ms");break;
            case "scene-1-conveyor-stop":
                ExecuteSelectedControllerAction("scene-toggle");var ticks=0;while(runtime.Points["simulated_photoeye"] is not true&&ticks++<300)FiveDemoTick();var pose=_sceneCompositionRoot!.GetNode<Node3D>("scene1_product").Transform;FiveDemoTick(2);
                check(ticks<300&&runtime.Points["conveyor_running"] is true&&_virtualController!.Snapshot.Variables["simulated_photoeye"]&&_sceneCompositionRoot.GetNode<Node3D>("scene1_product").Transform!=pose,"valid_wrong_demo3_ignores_real_feedback_continues_actual_motion");break;
            case "lab-9-11-pallet-counting":
                ExecuteSelectedControllerAction("toggle-count_request");FiveDemoTick(2);ExecuteSelectedControllerAction("toggle-pallet_detected");FiveDemoTick(2);
                check(runtime.Points["pallet_type_valid"] is false&&Equals(runtime.Points["pallet_count"],1L)&&_sceneCompositionRoot!.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout").Text=="COUNT\n1","valid_wrong_demo4_counts_invalid_type_actual_readout");break;
        }
        await FiveDemoInvalidCurrent(scene,check);
    }
    private async Task FiveDemoInvalidCurrent(string scene,Action<bool,string> check)
    {
        var invalid=new LadderEditorDocument();invalid.ResetProject("five-invalid-"+scene,"Invalid_Current",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=scene;
        var runtime=_sceneRuntime!;var held=FiveDemoPose();FiveDemoOpenDocument(invalid,"invalid",check);var draft=_simulatorShell!.CaptureIntegratedLadderDraft();
        RunEditorDraftController();check(_virtualController!.Snapshot.State!=VirtualControllerState.Running&&_virtualController.Snapshot.Outputs.Values.All(v=>!v),"invalid_current_blocks_old_program_clears_bool_outputs_"+scene);
        var scan=_virtualController.Snapshot.ScanNumber;var time=_virtualController.Snapshot.SimulatedTime;FiveDemoTick(25);runtime.AdvanceSimulation(.5);
        check(FiveDemoPose()==held&&_virtualController.Snapshot.ScanNumber==scan&&_virtualController.Snapshot.SimulatedTime==time&&_simulatorShell.CaptureIntegratedLadderDraft()==draft,"invalid_current_freezes_actual_pose_scan_time_exact_draft_"+scene);
        await FiveDemoJointRoundtrip(scene,"invalid",check);RunEditorDraftController();
        check((_virtualController is null||_virtualController.Snapshot.State!=VirtualControllerState.Running)&&_simulatorShell.CaptureIntegratedLadderDraft()==draft,"reopened_invalid_no_stale_program_"+scene);
    }
    private async Task FiveDemoPalletizerNegativeCases(Action<bool,string> check)
    {
        const string scene="lab-11-13-xy-palletizing";
        // A real valid project Open intentionally resets the plant. A draft-only change
        // followed by learner Run is the separate seam that preserves a carried carton.
        var tracePath=ProjectSettings.GlobalizePath("res://.tools/five-demo-carry-native-trace.jsonl");File.WriteAllText(tracePath,string.Empty);
        void Trace(string stage,VirtualControllerScanCompleted? accepted=null)
        {
            var root=_sceneCompositionRoot!;
            var poses=root.GetNode<Node3D>("training_accessory_4").FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
                .Where(m=>m.Name.ToString().StartsWith("KIN_",StringComparison.Ordinal)||m.Name=="GANTRY_GRIPPER").ToDictionary(m=>m.Name.ToString(),m=>m.Transform.ToString());
            poses["box_2"]=root.GetNode<Node3D>("box_2").Transform.ToString();
            var program=_virtualProgram is null?null:LadderProgramJson.Save(_virtualProgram);
            var row=JsonSerializer.Serialize(new{stage,program,accepted,snapshot=_virtualController?.Snapshot,points=_sceneRuntime!.Points,poses});
            File.AppendAllText(tracePath,row+System.Environment.NewLine);
            GD.Print($"FIVE_DEMO_CARRY_TRACE stage={stage} scan={_virtualController?.Snapshot.ScanNumber} state={_virtualController?.Snapshot.State} cycle={_sceneRuntime.Points["gantry_cycle"]} vacuum={_sceneRuntime.Points["vacuum_pick"]} attached={_sceneRuntime.Points["carton_attached"]} fault={_sceneRuntime.Points["palletizer_fault"]} placed={_sceneRuntime.Points["placed_cartons"]} file={tracePath}");
        }
        RunEditorDraftController();ExecuteSelectedControllerAction("start-palletizer");var ticks=0;
        while((_sceneRuntime!.Points["carton_attached"] is not true||_sceneRuntime.Points["gantry_home"] is true)&&ticks++<1500)FiveDemoTick();
        check(ticks<1500&&_sceneRuntime!.Points["carton_attached"] is true&&Convert.ToInt32(_sceneRuntime.Points["placed_cartons"])==0,"demo5_wrong_fixture_actual_attached_before_place");
        Trace("carry_before_valid_wrong_open");var wrong=FiveDemoWrong(scene);
        FiveDemoOpenDocument(wrong,"wrong",check);Trace("after_valid_wrong_open");check(_simulatorShell!.TryBuildCurrentLadderProgram(out _,out _),"demo5_wrong_vacuum_inhibit_compile_scene_valid");
        check(_sceneRuntime!.Points["carton_attached"] is false&&_sceneRuntime.Points["gantry_home"] is true&&_sceneRuntime.Points["palletizer_fault"] is false&&_virtualController!.Snapshot.State==VirtualControllerState.Stopped&&_virtualController.Snapshot.ScanNumber==0,"demo5_actual_valid_project_open_resets_transient_carry_stopped_home");
        RunEditorDraftController();Trace("after_valid_wrong_run");
        void OpenScanTrace(VirtualControllerScanCompleted scan)=>Trace("valid_wrong_open_accepted_scan",scan);
        _virtualController!.ScanCompleted+=OpenScanTrace;try{FiveDemoTick(2);}finally{_virtualController.ScanCompleted-=OpenScanTrace;}
        Trace("after_valid_wrong_open_two_scans");
        check(_sceneRuntime.Points["gantry_cycle"] is false&&_sceneRuntime.Points["vacuum_pick"] is false&&_sceneRuntime.Points["carton_attached"] is false&&_sceneRuntime.Points["palletizer_fault"] is false,"demo5_valid_wrong_open_run_alone_has_no_carry_or_fabricated_fault");
        // Reestablish carry with the real intended project; then retain that exact
        // physical state while loading the different valid current draft through Run.
        AuthoredDemoLadderPrograms.TryCreate(scene,out var carryCorrect);FiveDemoOpenDocument(carryCorrect,"carry-intended",check);
        RunEditorDraftController();ExecuteSelectedControllerAction("start-palletizer");ticks=0;
        while((_sceneRuntime!.Points["carton_attached"] is not true||_sceneRuntime.Points["gantry_home"] is true)&&ticks++<1500)FiveDemoTick();
        check(ticks<1500&&_sceneRuntime.Points["carton_attached"] is true,"demo5_actual_carry_reestablished_before_draft_only_change");
        var carryPose=FiveDemoPose();Trace("carry_before_wrong_draft_restore");
        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(wrong));Trace("after_wrong_draft_restore");
        check(FiveDemoPose()==carryPose&&_sceneRuntime.Points["carton_attached"] is true,"demo5_draft_only_change_preserves_actual_carry");
        RunEditorDraftController();Trace("after_wrong_draft_run");
        check(FiveDemoPose()==carryPose&&_sceneRuntime.Points["carton_attached"] is true,"demo5_current_wrong_run_load_preserves_actual_carry");
        check(ExecuteSelectedControllerAction("start-palletizer"),"demo5_fresh_start_resumes_current_wrong_carry_command");
        void CarryScanTrace(VirtualControllerScanCompleted scan)=>Trace("current_wrong_carry_accepted_scan",scan);
        _virtualController!.ScanCompleted+=CarryScanTrace;try{FiveDemoTick(2);}finally{_virtualController.ScanCompleted-=CarryScanTrace;}
        Trace("after_wrong_carry_two_scans");
        check(_sceneRuntime!.Points["vacuum_pick"] is false&&_sceneRuntime.Points["palletizer_fault"] is true&&_sceneRuntime.Points["carton_attached"] is true&&Convert.ToInt32(_sceneRuntime.Points["placed_cartons"])==0,"demo5_valid_wrong_actual_carry_vacuum_loss_fault_attachment_no_false_place");
        await FiveDemoJointRoundtrip(scene,"wrong",check);RunEditorDraftController();ExecuteSelectedControllerAction("start-palletizer");FiveDemoTick(50);
        check(_sceneRuntime!.Points["vacuum_pick"] is false&&_sceneRuntime.Points["carton_attached"] is false&&Convert.ToInt32(_sceneRuntime.Points["placed_cartons"])==0,"demo5_reopened_wrong_inhibit_obeys_current_program_no_transfer");
        StopActiveController();ResetActiveController();AuthoredDemoLadderPrograms.TryCreate(scene,out var correct);FiveDemoOpenDocument(correct,"intended-restored",check);RunEditorDraftController();ExecuteSelectedControllerAction("start-palletizer");ticks=0;
        while((_sceneRuntime!.Points["carton_attached"] is not true||_sceneRuntime.Points["gantry_home"] is true)&&ticks++<1500)FiveDemoTick();
        check(ticks<1500&&_sceneRuntime!.Points["carton_attached"] is true,"demo5_invalid_fixture_actual_carry");
        await FiveDemoInvalidCurrent(scene,check);
    }
}
