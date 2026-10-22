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
            case "scene-2-conveyor-pusher":
                check(ExecuteSelectedControllerAction("toggle-system_enable"),"demo3_system_enable_action");
                check(ExecuteSelectedControllerAction("pulse-start_command"),"demo3_machine_start_action");FiveDemoTick(2);
                var routes=new System.Collections.Generic.List<(long Height,string Destination)>();long currentHeight=0;
                var ticks=0;
                while(routes.Count<3&&ticks++<6000)
                {
                    FiveDemoTick();
                    if(runtime.Points["part_at_pusher"] is true)
                        currentHeight=Convert.ToInt64(runtime.Points["carton_height_mm"]);
                    if(runtime.Points["main_line_exit"] is true&&!routes.Any(route=>route.Height==currentHeight&&route.Destination=="main"))
                        routes.Add((currentHeight,"main"));
                    if(runtime.Points["side_line_exit"] is true&&!routes.Any(route=>route.Height==currentHeight&&route.Destination=="side"))
                        routes.Add((currentHeight,"side"));
                }
                check(ticks<6000&&routes.Contains((420,"main"))&&routes.Contains((820,"side"))&&routes.Contains((600,"main")),
                    "demo3_measured_short_tall_boundary_cartons_take_correct_ladder_selected_route");
                var sorterDraft=LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!;
                check(sorterDraft.Blocks.Any(block=>block.Name=="FB_ConveyorDrive")
                    &&sorterDraft.Blocks.Any(block=>block.Name=="FB_SorterPusher")
                    &&sorterDraft.Instances.Count(instance=>instance.InstanceDataBlock?.Number is 101 or 102)==2,
                    "demo3_called_motor_and_pusher_fbs_have_distinct_instance_dbs");
                check(ExecuteSelectedControllerAction("toggle-safety_trip"),"demo3_simulated_trip_action");FiveDemoTick(2);
                check(_virtualController!.Snapshot.Variables["machine_run"] is false&&runtime.Points["conveyor_running"] is false&&runtime.Points["pusher_extend"] is false,
                    "demo3_trip_drops_machine_commands_in_ladder");
                ExecuteSelectedControllerAction("toggle-safety_trip");FiveDemoTick(2);
                check(_virtualController!.Snapshot.Variables["machine_run"] is false,"demo3_clear_trip_requires_fresh_start");
                check(ExecuteSelectedControllerAction("pulse-reset_command"),"demo3_machine_reset_action");FiveDemoTick(2);
                check(_virtualController!.Snapshot.Variables["machine_run"] is false
                    &&Convert.ToInt64(runtime.Points["parts_completed"])==0
                    &&Equals(runtime.Points["component_state"],"stopped_loaded"),
                    "demo3_reset_restores_repeatable_first_carton_without_app_reset");
                check(ExecuteSelectedControllerAction("pulse-start_command"),"demo3_repeat_machine_start");FiveDemoTick(2);
                check(_virtualController!.Snapshot.Variables["machine_run"] is true&&runtime.Points["conveyor_running"] is true,
                    "demo3_reset_state_runs_again_after_fresh_start");break;
            case "lab-9-11-pallet-counting":
                check(ExecuteSelectedControllerAction("pulse-start_command"),"demo4_actual_machine_start");FiveDemoTick(2);
                ExecuteSelectedControllerAction("toggle-count_request");ExecuteSelectedControllerAction("toggle-pallet_type_valid");FiveDemoTick(2);
                var palletCarton=_sceneCompositionRoot!.GetNode<Node3D>("box_1");
                var palletInfeedX=palletCarton.Position.X;
                for(var i=1;i<=5;i++)
                {
                    ExecuteSelectedControllerAction("pulse-pallet_detected");FiveDemoTick(2);
                    check(Equals(runtime.Points["pallet_count"],(long)i)&&_sceneCompositionRoot!.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout").Text==$"COUNT\n{i}"&&Equals(runtime.Points["pallet_count_valid"],i==5),"demo4_actual_same_scan_count_readout_validity_"+i);
                }
                check(palletCarton.Position.X>palletInfeedX,
                    "demo4_new_pallet_event_moves_carton_on_running_conveyor");
                check(Equals(runtime.Points["pallet_count"],5L)&&runtime.Points["pallet_count_valid"] is true,
                    "demo4_batch_valid_after_five_qualified_events");
                check(ExecuteSelectedControllerAction("pulse-reset_pressed"),"demo4_actual_machine_reset");FiveDemoTick(2);
                check(Equals(runtime.Points["pallet_count"],0L)&&runtime.Points["pallet_count_valid"] is false
                    &&_virtualController!.Snapshot.Counters.Values.Single().Accumulated==0
                    &&_virtualController.Snapshot.Variables["machine_running"] is false,"demo4_machine_reset_clears_batch_and_stops");
                check(ExecuteSelectedControllerAction("pulse-start_command"),"demo4_repeat_machine_start");FiveDemoTick(2);
                ExecuteSelectedControllerAction("pulse-pallet_detected");FiveDemoTick(2);
                check(Equals(runtime.Points["pallet_count"],1L)&&runtime.Points["pallet_count_valid"] is false,
                    "demo4_repeat_batch_begins_at_one_after_reset");
                break;
            case "lab-11-13-xy-palletizing":
                check(ExecuteSelectedControllerAction("start-palletizer"),"demo5_actual_start_one_four_carton_batch");
                var palletizerTicks=0;var placed=new System.Collections.Generic.HashSet<int>();var steps=new System.Collections.Generic.HashSet<long>();
                var commands=PalletizerAxisCommands.ToDictionary(command=>command,_=>false,StringComparer.Ordinal);
                do
                {
                    FiveDemoTick();palletizerTicks++;placed.Add(Convert.ToInt32(runtime.Points["placed_cartons"]));
                    steps.Add(Convert.ToInt64(_virtualController!.Snapshot.NumericVariables["sequence_step"]));
                    foreach(var command in commands.Keys.ToArray())commands[command]|=runtime.Points[command] is true;
                }while(palletizerTicks<6000&&runtime.Points["layer_complete"] is not true);
                check(palletizerTicks<6000&&placed.IsSupersetOf(new[]{0,1,2,3,4})&&steps.IsSupersetOf(new long[]{10,20,30,40,50,60,70,80,100}),"demo5_plc_scan_trace_places_four_cartons_through_every_state");
                check(commands.Values.All(value=>value)&&runtime.Points["gantry_home"] is true&&_virtualController!.Snapshot.Counters["cycle_count"].Accumulated==4,"demo5_all_axis_directions_and_four_return_edges_observed");
                check(runtime.Points["layer_complete"] is true&&runtime.Points["vacuum_pick"] is false&&PalletizerAxisCommands.All(command=>runtime.Points[command] is false)&&_sceneCompositionRoot!.GetNodeOrNull<Node3D>("PlacedCarton_3") is not null,"demo5_current_saved_draft_four_actual_cartons_layer_complete");break;
        }
    }
    private LadderEditorDocument FiveDemoWrong(string scene)
    {
        AuthoredDemoLadderPrograms.TryCreate(scene,out var doc);
        switch(scene)
        {
            case "lab-4-01-press-count-lamp":doc.Rungs.Single(r=>r.IsCounter).CounterPreset=1;break;
            case "lab-5-01-delayed-lamp":
                var timer=doc.Rungs.Single(r=>r.IsTimer);timer.TimerPreset=TimeSpan.FromMilliseconds(20);
                timer.TimerPresetAssigned=true;timer.TimerPresetOperand=LadderTimeLiteral.Format(timer.TimerPreset);break;
            case "scene-2-conveyor-pusher":
                doc.Rungs.Single(r=>r.CoilVariable=="divert_latched"&&r.CoilMode==LadderCoilMode.Set)
                    .Branches[0].Contacts.RemoveAll(contact=>contact.IsComparison);break;
            case "lab-9-11-pallet-counting":
                foreach(var branch in doc.Rungs.Single(r=>r.CoilVariable=="batch_ready").Branches)
                    branch.Contacts.RemoveAll(c=>c.Variable=="pallet_type_valid");break;
            case "lab-11-13-xy-palletizing":
                doc.AddTag("wrong_y_inhibit",PlcVariableRole.Memory);
                foreach(var branch in doc.Rungs.Single(r=>r.CoilVariable=="y_negative_request").Branches)
                    branch.Contacts.Add(new("wrong-inhibit-"+branch.Id,"wrong_y_inhibit",false));break;
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
            case "scene-2-conveyor-pusher":
                ExecuteSelectedControllerAction("toggle-system_enable");ExecuteSelectedControllerAction("pulse-start_command");FiveDemoTick(2);
                var wrongRoutes=0;var wrongTicks=0;
                while(wrongRoutes==0&&wrongTicks++<2500){FiveDemoTick();if(runtime.Points["side_line_exit"] is true)wrongRoutes++;}
                check(wrongTicks<2500&&wrongRoutes==1&&runtime.Points["carton_height_mm"] is 0L,
                    "valid_wrong_demo3_missing_height_compare_diverts_short_carton");break;
            case "lab-9-11-pallet-counting":
                ExecuteSelectedControllerAction("pulse-start_command");FiveDemoTick(2);
                ExecuteSelectedControllerAction("toggle-count_request");FiveDemoTick(2);ExecuteSelectedControllerAction("pulse-pallet_detected");FiveDemoTick(2);
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
        var wrong=FiveDemoWrong(scene);FiveDemoOpenDocument(wrong,"wrong",check);
        check(_simulatorShell!.TryBuildCurrentLadderProgram(out _,out _),"demo5_wrong_y_inhibit_compile_scene_valid");
        await FiveDemoJointRoundtrip(scene,"wrong",check);RunEditorDraftController();
        check(ExecuteSelectedControllerAction("start-palletizer"),"demo5_wrong_project_start");FiveDemoTick(100);
        check(_sceneRuntime!.Points["gantry_home"] is true&&Convert.ToInt32(_sceneRuntime.Points["placed_cartons"])==0
            &&_sceneRuntime.Points["gantry_y_negative"] is false,"demo5_valid_wrong_y_inhibit_prevents_hidden_scene_sequence");
        await FiveDemoInvalidCurrent(scene,check);
    }
}
