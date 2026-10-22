using System;
using System.Collections.Generic;
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
    private bool _verifyScanInspection;
    private bool _verifyScanInspectionDemo5;
    private async void VerifyDemo5ScanInspectionEquivalence()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"SCAN_INSPECTION_CHECK {name}={ok}");if(!ok)failures++;}
        try{foreach(var ms in new[]{20,40,100})await VerifyScanInspectionHistoryEquivalence("demo5",ms,Check);}
        catch(Exception error){failures++;GD.PushError("SCAN_INSPECTION_EXCEPTION "+error);}
        GD.Print("SCAN_INSPECTION_DEMO5_EQUIVALENCE_RESULT failures="+failures);GetTree().Quit(failures==0?0:1);
    }
    private async void VerifyScanInspection()
    {
        var failures=0;
        void Check(bool ok,string name){GD.Print($"SCAN_INSPECTION_CHECK {name}={ok}");if(!ok)failures++;}
        try
        {
            Check(_simulatorShell!.VerifyScanInspectionHistoryInitiallyHidden(out var initiallyHidden),"initial_history_popup_hidden_before_any_explicit_selection");
            GD.Print(initiallyHidden);
            foreach(var ms in new[]{20,40,100})
            foreach(var kind in new[]{"demo1","demo2","demo3","demo4","demo5","belt","roller"})
            {
                var label=kind+"_"+ms+"ms";
                await LoadScanInspectionFixture(kind,ms,false);
                var session=_virtualController!;var runtime=_sceneRuntime!;
                _PhysicsProcess(ms/1000d);
                Check(session.History.Count>0&&session.History.Last().PostPlantInputs.Bools.Count>0,"ordinary_main_physics_history_contains_actual_postplant_inputs_"+label);
                PauseActiveController();Check(session.IsPaused,"actual_pause_"+label);
                var before=JsonSerializer.Serialize(session.Snapshot);var inputs=JsonSerializer.Serialize(runtime.Points);var pose=ScanInspectionNativePose();
                var historyCount=session.History.Count;var lease=_sceneCompositionRoot!.ProcessMode;var ambient=ScanInspectionAmbientPose();
                await ScanInspectionWaitPhysics();
                Check(JsonSerializer.Serialize(session.Snapshot)==before&&JsonSerializer.Serialize(runtime.Points)==inputs&&ScanInspectionNativePose()==pose&&session.History.Count==historyCount&&lease==ProcessModeEnum.Disabled,"real_walltime_frames_freeze_snapshot_points_all_native_pose_"+label);
                Check(ScanInspectionAmbientPose()==ambient,"paused_actual_decorative_sibling_fan_pose_frozen_"+label);
                QueueScanInspectionStart(kind);
                var accepted=0;VirtualControllerScanCompleted? last=null;
                void Accept(VirtualControllerScanCompleted row){accepted++;last=row;}
                session.ScanCompleted+=Accept;
                var scan=session.Snapshot.ScanNumber;var time=session.Snapshot.SimulatedTime;
                try{StepActiveController();}finally{session.ScanCompleted-=Accept;}
                Check(accepted==1&&session.IsPaused&&session.Snapshot.ScanNumber==scan+1&&session.Snapshot.SimulatedTime==time+TimeSpan.FromMilliseconds(ms)&&session.History.Count==historyCount+1,"actual_step_exactly_one_accepted_scan_period_stays_paused_"+label);
                Check(last is not null&&ScanInspectionStartAccepted(kind,last),"actual_pc_input_queued_while_paused_sampled_once_"+label);
                Check(_simulatorShell!.VerifyScanInspectionControls(true,out var controls),"actual_pause_resume_step_ui_controls_"+label);GD.Print(controls);
                var pendingPose=ScanInspectionNativePose();var pendingSnapshot=JsonSerializer.Serialize(session.Snapshot);
                await ScanInspectionWaitPhysics();
                Check(ScanInspectionNativePose()==pendingPose&&JsonSerializer.Serialize(session.Snapshot)==pendingSnapshot,"after_step_no_autonomous_child_physics_drift_"+label);
                StepActiveController();
                Check(ScanInspectionPulseReleased(kind),"momentary_released_or_maintained_contract_"+label);
                await VerifyScanInspectionFixtureConsequence(kind,ms,Check);
                await VerifyScanInspectionHistoryAndDraftGate(kind,ms,Check);
                StopActiveController();Check(!session.IsPaused&&session.Snapshot.State==VirtualControllerState.Stopped,"stop_exits_pause_existing_stopped_contract_"+label);
                ResetActiveController();Check(!session.IsPaused&&session.Snapshot.ScanNumber==0&&session.Snapshot.Forces.Count==0,"reset_scan_zero_forces_clear_exits_pause_"+label);
            }
            foreach(var ms in new[]{20,40,100})
            foreach(var kind in new[]{"demo1","demo2","demo3","demo4","demo5","belt","roller"})
                await VerifyScanInspectionHistoryEquivalence(kind,ms,Check);
            foreach(var ms in new[]{20,40,100})await VerifyScanInspectionAccumulator(ms,Check);
            foreach(var ms in new[]{20,40,100})await VerifyScanInspectionImmediateTogglePause(ms,Check);
            AddMigratedScene("equipment-gallery",_candidateCatalog!,_mainCamera!,false,false);
            var unsupported=JsonSerializer.Serialize(_sceneRuntime!.Points);PauseActiveController();StepActiveController();
            Check(_virtualController is null&&JsonSerializer.Serialize(_sceneRuntime.Points)==unsupported,"unsupported_gallery_missing_controller_pause_step_rejects_no_plant_mutation");
            Check(_simulatorShell!.VerifyExternalProfileGuards(out var externalGuard),"existing_external_profile_guards_no_transport");GD.Print(externalGuard);
        }
        catch(Exception error){failures++;GD.PushError("SCAN_INSPECTION_EXCEPTION "+error);}
        GD.Print("SCAN_INSPECTION_RESULT failures="+failures+" scope=exact_five_demos_finite_belt_roller_20_40_100ms");GetTree().Quit(failures==0?0:1);
    }
    private async Task LoadScanInspectionFixture(string kind,int ms,bool wrong)
    {
        var scene=kind switch{"demo1"=>"lab-4-01-press-count-lamp","demo2"=>"lab-5-01-delayed-lamp","demo3"=>"scene-1-conveyor-stop","demo4"=>"lab-9-11-pallet-counting","demo5"=>"lab-11-13-xy-palletizing",_=>"scene-1-conveyor-stop"};
        var preset=kind=="belt"?WorkspaceFiniteBeltPolicy.Create():kind=="roller"?WorkspaceFiniteRollerPolicy.Create():null;
        AddMigratedScene(scene,_candidateCatalog!,_mainCamera!,false,false,finiteBeltWorkspace:kind=="belt"?preset:null,finiteRollerWorkspace:kind=="roller"?preset:null);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        LadderEditorDocument document;
        if(preset is null)
        {
            if(wrong)document=FiveDemoWrong(scene);else if(!AuthoredDemoLadderPrograms.TryCreate(scene,out document))throw new InvalidOperationException("Missing exact authored demo.");
            document.ScanPeriod=TimeSpan.FromMilliseconds(ms);
        }
        else
        {
            var start=kind=="belt"?preset.FiniteBeltPreset!.StartPoint:preset.FiniteRollerPreset!.StartPoint;
            var beam=kind=="belt"?preset.FiniteBeltPreset!.BeamPoint:preset.FiniteRollerPreset!.BeamPoint;
            var run=kind=="belt"?preset.FiniteBeltPreset!.RunPoint:preset.FiniteRollerPreset!.RunPoint;
            document=new();document.ResetProject("scan-inspection-"+kind,"Scan_Inspection",TimeSpan.FromMilliseconds(ms));document.SourceSceneId=scene;
            document.AddTag("start",PlcVariableRole.Input,start);document.AddTag("beam",PlcVariableRole.Input,beam);document.AddTag("run",PlcVariableRole.Output,run);
            document.AddRung("Current learner retained start","run");document.AddContact(0,0,"start",false);if(!wrong)document.AddContact(0,0,"beam",true);
            document.AddParallelBranch(0);document.AddContact(0,1,"run",false);if(!wrong)document.AddContact(0,1,"beam",true);
        }
        var path=ProjectSettings.GlobalizePath($"res://.tools/scan-inspection-{kind}-{ms}-{wrong}.rpproj.json");File.WriteAllText(path,LadderEditorProjectJson.Save(document));
        if(!_simulatorShell!.OpenNativeMotionReviewProject(path,out var opened)||!_simulatorShell.TryBuildCurrentLadderProgram(out _,out _))throw new InvalidOperationException("Actual fixture open/build failed: "+opened);
        RunEditorDraftController();
        if(_virtualController is null||_virtualController.ScanPeriod!=TimeSpan.FromMilliseconds(ms))throw new InvalidOperationException("Actual accepted scan period mismatch.");
    }
    private async Task ScanInspectionWaitPhysics()
    {
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree().CreateTimer(.5),SceneTreeTimer.SignalName.Timeout);
        await ToSignal(GetTree(),SceneTree.SignalName.PhysicsFrame);
    }
    private string ScanInspectionNativePose()
    {
        var poses=string.Join("|",_sceneCompositionRoot!.FindChildren("*","Node3D",true,false).OfType<Node3D>().Select(n=>ScanInspectionStructuralPath(n)+":"+n.GetClass()+":"+n.Transform+":"+n.Visible));
        var uv=string.Join("|",_sceneCompositionRoot.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().SelectMany(m=>Enumerable.Range(0,m.Mesh?.GetSurfaceCount()??0).Select(i=>m.GetSurfaceOverrideMaterial(i))).OfType<StandardMaterial3D>().Select(m=>m.Uv1Offset.ToString()));
        var physical=_sceneRuntime!.HasWorkspaceRollerPreset?JsonSerializer.Serialize(_sceneRuntime.CaptureWorkspaceRollerPreset()):_sceneRuntime.HasWorkspaceBeltPreset?JsonSerializer.Serialize(_sceneRuntime.CaptureWorkspaceBeltPreset()):string.Empty;
        return poses+"|UV:"+uv+"|PHYSICAL:"+physical;
    }
    private string ScanInspectionAmbientPose()=>_demoEnvironment is null?string.Empty:string.Join("|",_demoEnvironment.FindChildren("*","Node3D",true,false).OfType<Node3D>().Select(n=>n.GetPath()+":"+n.Transform));
    private void QueueScanInspectionStart(string kind)
    {
        var actions=kind switch{"demo1"=>new[]{"toggle-pulse_received"},"demo2"=>new[]{"toggle-timer_request"},"demo3"=>new[]{"scene-toggle"},"demo4"=>new[]{"toggle-count_request","toggle-pallet_type_valid","pulse-pallet_detected"},"demo5"=>new[]{"start-palletizer"},"belt"=>new[]{"workspace-press"},_=>new[]{"roller-workspace-press"}};
        foreach(var action in actions)if(!ExecuteSelectedControllerAction(action))throw new InvalidOperationException("Paused mapped PC action rejected: "+action);
    }
    private bool ScanInspectionStartAccepted(string kind,VirtualControllerScanCompleted row)
    {
        var name=kind switch{"demo1"=>"pulse_received","demo2"=>"timer_request","demo3"=>"start_command","demo4"=>"pallet_detected","demo5"=>"start_command",_=>"start"};
        return row.SampledInputs.Bools.GetValueOrDefault(name);
    }
    private bool ScanInspectionPulseReleased(string kind)
    {
        var name=kind switch{"demo1"=>"pulse_received","demo2"=>"timer_request","demo3"=>"start_command","demo4"=>"pallet_detected","demo5"=>"start_command",_=>"start"};
        return _virtualController!.Snapshot.Variables[name]==(kind is "demo2" or "demo4");
    }
    private async Task VerifyScanInspectionFixtureConsequence(string kind,int ms,Action<bool,string> check)
    {
        var label=kind+"_"+ms+"ms";var session=_virtualController!;var runtime=_sceneRuntime!;
        switch(kind)
        {
            case "demo1":check(session.Snapshot.Counters["press_count"].Accumulated==1&&runtime.Points["threshold_lamp"] is false&&FiveDemoLamp("indicator_1",false),"paused_demo1_one_pulse_count_actual_lamp_"+label);break;
            case "demo2":
                while(session.Snapshot.Timers["delay_timer"].Accumulated.TotalSeconds+ms/1000d<2-1e-9)StepActiveController();
                var before=session.Snapshot.Timers["delay_timer"].Accumulated;await ScanInspectionWaitPhysics();check(session.Snapshot.Timers["delay_timer"].Accumulated==before&&runtime.Points["delayed_lamp"] is false,"paused_ton_walltime_does_not_complete_"+label);
                StepActiveController();check(session.Snapshot.Timers["delay_timer"].Done&&runtime.Points["delayed_lamp"] is true&&FiveDemoLamp("indicator_1",true),"step_ton_exact_threshold_actual_lens_"+label);break;
            case "demo3":
            case "belt":
            case "roller":
                var beam=kind=="demo3"?"simulated_photoeye":kind=="belt"?WorkspaceFiniteBeltPolicy.Relation().BeamPoint:WorkspaceFiniteRollerPolicy.Relation().BeamPoint;
                var variable=kind=="demo3"?"simulated_photoeye":"beam";var run=kind=="demo3"?"conveyor_running":kind=="belt"?WorkspaceFiniteBeltPolicy.Relation().RunPoint:WorkspaceFiniteRollerPolicy.Relation().RunPoint;
                var steps=0;while(runtime.Points[beam] is not true&&steps++<1500)StepActiveController();
                check(steps<1500&&runtime.Points[beam] is true&&!session.Snapshot.Variables[variable],"paused_step_real_beam_postplant_old_sample_false_"+label);
                var trigger=session.History.Last();
                check(trigger.PostPlantInputs.Bools.GetValueOrDefault(beam)&&!trigger.CompletedScan.SampledInputs.Bools.GetValueOrDefault(variable),"actual_history_postplant_beam_separate_from_accepted_sample_"+label);
                StepActiveController();check(session.Snapshot.Variables[variable]&&runtime.Points[run] is false,"paused_next_step_samples_feedback_commands_off_"+label);
                check(session.History.Last().CompletedScan.ScanNumber==trigger.CompletedScan.ScanNumber+1&&session.History.Last().CompletedScan.SampledInputs.Bools.GetValueOrDefault(variable),"actual_history_exact_next_row_samples_prior_postplant_feedback_"+label);
                if(kind=="roller")
                {
                    var coast=runtime.CaptureWorkspaceRollerPreset();var pose=ScanInspectionNativePose();await ScanInspectionWaitPhysics();
                    check(coast.SpeedMps>0&&runtime.CaptureWorkspaceRollerPreset()==coast&&ScanInspectionNativePose()==pose,"pause_actual_positive_coast_speed_all31_phase_load_"+label);
                    StepActiveController();var after=runtime.CaptureWorkspaceRollerPreset();check(after.SpeedMps<coast.SpeedMps&&after.DriveSurfaceTravelM>coast.DriveSurfaceTravelM,"one_step_actual_analytical_deceleration_coast_"+label);
                }
                break;
            case "demo4":check(Equals(runtime.Points["pallet_count"],1L)&&_sceneCompositionRoot!.GetNode<Node3D>("training_accessory_6").GetNode<Label3D>("NumericReadout").Text=="COUNT\n1","paused_demo4_count_same_scan_actual_readout_"+label);break;
            case "demo5":
                var count=0;while(runtime.Points["carton_attached"] is not true&&count++<1500)StepActiveController();var held=ScanInspectionNativePose();var points=JsonSerializer.Serialize(runtime.Points);await ScanInspectionWaitPhysics();
                check(count<1500&&runtime.Points["carton_attached"] is true&&runtime.Points["palletizer_fault"] is false&&held==ScanInspectionNativePose()&&JsonSerializer.Serialize(runtime.Points)==points,"paused_demo5_actual_attachment_axes_carton_memory_frozen_"+label);
                check(!ExecuteSelectedControllerAction("load-carton")&&held==ScanInspectionNativePose()&&JsonSerializer.Serialize(runtime.Points)==points,"paused_demo5_direct_load_mutator_rejected_no_inventory_pose_change_"+label);
                var oldScan=session.Snapshot.ScanNumber;StepActiveController();check(session.Snapshot.ScanNumber==oldScan+1&&runtime.Points["carton_attached"] is true&&runtime.Points["palletizer_fault"] is false&&held!=ScanInspectionNativePose(),"paused_demo5_step_moves_actual_carried_carton_once_"+label);break;
        }
    }
    private async Task VerifyScanInspectionHistoryAndDraftGate(string kind,int ms,Action<bool,string> check)
    {
        var label=kind+"_"+ms+"ms";var session=_virtualController!;var first=session.History.First();var retained=JsonSerializer.Serialize(first);
        var mutationBlocked=false;
        try
        {
            if(first.CompletedScan.SampledInputs.Bools is IDictionary<string,bool> exposed)
            {var key=exposed.Keys.First();exposed[key]=!exposed[key];}
            else mutationBlocked=true;
        }
        catch(NotSupportedException){mutationBlocked=true;}
        check(mutationBlocked&&JsonSerializer.Serialize(first)==retained,"actual_history_exposed_sample_mutation_rejected_"+label);
        var elapsedScan=session.Snapshot.ScanNumber;var elapsedTime=session.Snapshot.SimulatedTime;
        // More than128 accepted steps proves real ring eviction without relying on a UI count.
        for(var i=0;i<140;i++)StepActiveController();
        check(session.History.Count==128&&session.HistoryDroppedCount>0&&JsonSerializer.Serialize(first)==retained&&session.Snapshot.ScanNumber==elapsedScan+140&&session.Snapshot.SimulatedTime==elapsedTime+TimeSpan.FromMilliseconds(ms*140),"bounded128_ring_old_record_immutable_exact_step_clock_"+label);
        var selectedBefore=JsonSerializer.Serialize(session.Snapshot);var selectedPlant=JsonSerializer.Serialize(_sceneRuntime!.Points);var selectedPose=ScanInspectionNativePose();
        check(_simulatorShell!.VerifyScanInspectionHistorySelection(0,out var historyUi),"actual_old_history_selection_historical_label_"+label);GD.Print(historyUi);
        check(JsonSerializer.Serialize(session.Snapshot)==selectedBefore&&JsonSerializer.Serialize(_sceneRuntime.Points)==selectedPlant&&ScanInspectionNativePose()==selectedPose,"actual_history_selection_no_live_snapshot_plant_pose_mutation_"+label);
        var forceSnapshot=JsonSerializer.Serialize(session.Snapshot);var forcePose=ScanInspectionNativePose();ApplyVirtualForce(kind switch{"demo1"=>"pulse_received","demo2"=>"timer_request","demo3"=>"start_command","demo4"=>"pallet_detected","demo5"=>"start_command",_=>"start"},true);
        check(JsonSerializer.Serialize(session.Snapshot)==forceSnapshot&&ScanInspectionNativePose()==forcePose,"force_edit_blocked_while_paused_no_history_or_live_mutation_"+label);
        var current=LadderEditorProjectJson.Load(_simulatorShell!.CaptureIntegratedLadderDraft()).Document!;
        if(current.WatchVariables.Count>0)current.WatchVariables.RemoveAt(0);else current.WatchVariables.Add(current.Tags[0].Name);
        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(current));var watchScan=session.Snapshot.ScanNumber;StepActiveController();
        check(session.Snapshot.ScanNumber==watchScan+1&&session.IsPaused,"watch_only_edit_remains_semantically_current_step_"+label);
        current.AddTag("scan_inspection_changed_memory",PlcVariableRole.Memory);
        var changed=LadderEditorProjectJson.Save(current);var old=JsonSerializer.Serialize(session.Snapshot);var pose=ScanInspectionNativePose();var rows=session.History.Count;
        _simulatorShell.RestoreIntegratedLadderDraft(changed);StepActiveController();
        check(JsonSerializer.Serialize(session.Snapshot)==old&&ScanInspectionNativePose()==pose&&session.History.Count==rows&&_simulatorShell.CaptureIntegratedLadderDraft()==changed,"changed_valid_current_step_blocks_stale_execution_"+label);
        var input=_virtualProgram!.Variables.First(v=>v.Role==PlcVariableRole.Input&&v.Type==PlcVariableType.Bool);var pending=JsonSerializer.Serialize(session.PendingInputs);
        PulseVirtualInput(input.Binding,input.Name);check(JsonSerializer.Serialize(session.PendingInputs)==pending,"monitor_input_mismatched_paused_draft_cannot_queue_"+label);
        var invalid=new LadderEditorDocument();invalid.ResetProject("scan-invalid-"+kind,"Invalid_Current",TimeSpan.FromMilliseconds(ms));invalid.SourceSceneId=_currentSceneId!;
        _simulatorShell.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(invalid));var before=session.Snapshot.ScanNumber;var invalidPose=ScanInspectionNativePose();StepActiveController();
        check(session.Snapshot.ScanNumber==before&&session.IsPaused&&ScanInspectionNativePose()==invalidPose,"invalid_current_step_rejected_no_stale_scan_or_motion_"+label);
        pending=JsonSerializer.Serialize(session.PendingInputs);PulseVirtualInput(input.Binding,input.Name);check(JsonSerializer.Serialize(session.PendingInputs)==pending,"monitor_input_invalid_paused_draft_cannot_queue_"+label);
        RunEditorDraftController();check(session.Snapshot.ScanNumber==before&&session.Snapshot.State==VirtualControllerState.Stopped&&session.Snapshot.Outputs.Values.All(v=>!v),"invalid_current_run_distinct_existing_stop_output_clear_"+label);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
    }
    private async Task VerifyScanInspectionHistoryEquivalence(string kind,int ms,Action<bool,string> check)
    {
        foreach(var wrong in new[]{false,true})
        {
            List<string>? baseline=null;
            List<ScanInspectionTraceDiagnostic>? baselineDiagnostic=null;
            foreach(var mode in new[]{"automatic","step-history-off","step-history-on"})
            {
                var recording=mode=="step-history-on";var stepped=mode!="automatic";
                await LoadScanInspectionFixture(kind,ms,wrong);var session=_virtualController!;session.HistoryRecordingEnabled=recording;PauseActiveController();QueueScanInspectionStart(kind);if(!stepped)RunEditorDraftController();
                var trace=new List<string>();var diagnostics=new List<ScanInspectionTraceDiagnostic>();
                void Accept(VirtualControllerScanCompleted row)
                {
                    var accepted=JsonSerializer.Serialize(row);var points=JsonSerializer.Serialize(_sceneRuntime!.Points);var pose=ScanInspectionNativePose();
                    trace.Add(accepted+"|"+points+"|"+pose);
                    if(kind=="demo5")diagnostics.Add(new(accepted,points,pose,ScanInspectionDiagnosticNodes()));
                }
                session.ScanCompleted+=Accept;
                try
                {
                    for(var i=0;i<Math.Ceiling(16_000d/ms);i++)
                        if(stepped)StepActiveController();
                        else session.Advance(ms/1000d,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,_sceneRuntime!.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs,CaptureScanInspectionPlantInputs);
                }
                finally{session.ScanCompleted-=Accept;}
                if(baseline is null){baseline=trace;baselineDiagnostic=diagnostics;}
                else
                {
                    if(kind=="demo5"&&!baseline.SequenceEqual(trace))WriteScanInspectionDifference(ms,wrong,mode,baseline,trace,baselineDiagnostic!,diagnostics);
                    check(baseline.SequenceEqual(trace),$"automatic_step_history_modes_entire_accepted_payload_actual_plant_pose_equivalent_{kind}_{ms}ms_wrong{wrong}_{mode}");
                }
                check(!recording?session.History.Count==0:session.History.Count==128,$"history_toggle_real_bounded_collection_{kind}_{ms}ms_wrong{wrong}_{mode}");
                StopActiveController();
            }
        }
    }
    private sealed record ScanInspectionTraceDiagnostic(string AcceptedScanJson,string PlantPointsJson,string NativePose,ScanInspectionNodeDiagnostic[] Nodes);
    private sealed record ScanInspectionNodeDiagnostic(string Path,string OriginalPath,string Type,int ChildIndex,string Transform,bool Visible);
    private ScanInspectionNodeDiagnostic[] ScanInspectionDiagnosticNodes()=>_sceneCompositionRoot!.FindChildren("*","Node3D",true,false).OfType<Node3D>()
        .Select(n=>new ScanInspectionNodeDiagnostic(ScanInspectionStructuralPath(n),n.GetPath().ToString(),n.GetClass(),n.GetIndex(),n.Transform.ToString(),n.Visible)).ToArray();
    // Godot's unnamed procedural nodes receive process-wide numeric suffixes. These identify
    // instances, not positions in the authored scene. Preserve authored names; identify only
    // generated segments by their exact sibling ordinal and node type across fresh fixtures.
    private string ScanInspectionStructuralPath(Node node)
    {
        var segments=new Stack<string>();
        for(Node? current=node;current is not null;current=current.GetParent())
        {
            var name=current.Name.ToString();var finalAt=name.LastIndexOf('@');
            var generated=name.StartsWith('@')&&finalAt>0&&long.TryParse(name[(finalAt+1)..],out _);
            generated|=ScanInspectionIsProvenDuplicatedGeneratedName(current);
            segments.Push(generated?$"[child={current.GetIndex()};type={current.GetClass()}]":name);
        }
        return "/"+string.Join("/",segments);
    }
    private bool ScanInspectionIsProvenDuplicatedGeneratedName(Node node)
    {
        // Runtime ResetPalletizerPlant duplicates box_2 into exactly PlacedCarton_0..3.
        // Duplicate sanitizes @ to _. Normalize only a matching source node at the
        // same child-index ancestry, never arbitrary authored names ending in digits.
        var indices=new Stack<int>();Node? ancestor=node;
        while(ancestor is not null&&ancestor.GetParent()!=_sceneCompositionRoot)
        {indices.Push(ancestor.GetIndex());ancestor=ancestor.GetParent();}
        if(ancestor is null||!new[]{"PlacedCarton_0","PlacedCarton_1","PlacedCarton_2","PlacedCarton_3"}.Contains(ancestor.Name.ToString()))return false;
        Node? original=_sceneCompositionRoot!.GetNodeOrNull<Node>("box_2");
        foreach(var index in indices)
        {if(original is null||index>=original.GetChildCount())return false;original=original.GetChild(index);}
        if(original is null||original.GetClass()!=node.GetClass())return false;
        var originalName=original.Name.ToString();var finalAt=originalName.LastIndexOf('@');
        return originalName.StartsWith('@')&&finalAt>0&&long.TryParse(originalName[(finalAt+1)..],out _)&&node.Name.ToString()==originalName.Replace('@','_');
    }
    private readonly string _scanInspectionDiagnosticRun=DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")+"-"+Guid.NewGuid().ToString("N")[..8];
    private void WriteScanInspectionDifference(int ms,bool wrong,string mode,List<string> baseline,List<string> actual,List<ScanInspectionTraceDiagnostic> expectedRows,List<ScanInspectionTraceDiagnostic> actualRows)
    {
        var first=Enumerable.Range(0,Math.Min(baseline.Count,actual.Count)).FirstOrDefault(i=>baseline[i]!=actual[i],-1);
        if(first<0)first=Math.Min(baseline.Count,actual.Count);
        var directory=Path.Combine(ProjectSettings.GlobalizePath("res://"),".tools","studio-verification","scan-inspection-demo5-differences",_scanInspectionDiagnosticRun);Directory.CreateDirectory(directory);
        var stem=$"demo5-{ms}ms-wrong{wrong}-{mode}";
        var options=new JsonSerializerOptions{WriteIndented=true};
        var start=Math.Max(0,first-1);var count=3;
        File.WriteAllText(Path.Combine(directory,stem+"-automatic.json"),JsonSerializer.Serialize(expectedRows.Skip(start).Take(count),options));
        File.WriteAllText(Path.Combine(directory,stem+"-actual.json"),JsonSerializer.Serialize(actualRows.Skip(start).Take(count),options));
        var expected=first<expectedRows.Count?expectedRows[first]:null;var current=first<actualRows.Count?actualRows[first]:null;
        var nodeIndex=expected is not null&&current is not null?Enumerable.Range(0,Math.Min(expected.Nodes.Length,current.Nodes.Length)).FirstOrDefault(i=>
            (expected.Nodes[i] with{OriginalPath=""})!=(current.Nodes[i] with{OriginalPath=""}),-1):-1;
        var summary=new{FirstDifferentRowZeroBased=first,WindowStartZeroBased=start,ExpectedCount=baseline.Count,ActualCount=actual.Count,
            AcceptedScanEqual=expected?.AcceptedScanJson==current?.AcceptedScanJson,PlantPointsEqual=expected?.PlantPointsJson==current?.PlantPointsJson,NativePoseEqual=expected?.NativePose==current?.NativePose,
            FirstDifferentNodeIndex=nodeIndex,ExpectedNode=nodeIndex>=0?expected!.Nodes[nodeIndex]:null,ActualNode=nodeIndex>=0?current!.Nodes[nodeIndex]:null,
            CurrentDraftJson=_simulatorShell!.CaptureIntegratedLadderDraft()};
        File.WriteAllText(Path.Combine(directory,stem+"-summary.json"),JsonSerializer.Serialize(summary,options));
        GD.Print("SCAN_INSPECTION_FIRST_DIFFERENCE "+JsonSerializer.Serialize(new{first,summary.AcceptedScanEqual,summary.PlantPointsEqual,summary.NativePoseEqual,nodeIndex,summary.ExpectedNode,summary.ActualNode})+" diagnosticStem="+Path.Combine(directory,stem));
    }
    private async Task VerifyScanInspectionAccumulator(int ms,Action<bool,string> check)
    {
        await LoadScanInspectionFixture("demo1",ms,false);var session=_virtualController!;var period=ms/1000d;
        int Advance(double elapsed)=>session.Advance(elapsed,SampleVirtualControllerInputs,SampleVirtualControllerNumericInputs,CommitVirtualControllerOutputs,CommitVirtualControllerNumericOutputs,_sceneRuntime!.AdvanceSimulation,SampleVirtualControllerTextInputs,CommitVirtualControllerTextOutputs,CaptureScanInspectionPlantInputs);
        var modes=_sceneCompositionRoot!.FindChildren("*","Node",true,false).Prepend(_sceneCompositionRoot!).ToDictionary(n=>n,n=>n.ProcessMode);
        CheckAccumulator(Advance(period*.75)==0,"fraction_initial_no_scan");PauseActiveController();var scan=session.Snapshot.ScanNumber;StepActiveController();
        CheckAccumulator(session.Snapshot.ScanNumber==scan+1&&Advance(10000)==0,"step_bypasses_fraction_and_paused_huge_walltime");RunEditorDraftController();
        CheckAccumulator(Advance(period*.25)==1&&modes.All(v=>v.Key.ProcessMode==v.Value),"resume_preserves_fraction_restores_every_original_process_mode");
        var before=session.Snapshot.ScanNumber;var batch=Advance(period*(VirtualControllerSession.MaximumScansPerAdvance+3.5));
        CheckAccumulator(batch==VirtualControllerSession.MaximumScansPerAdvance,"accepted_max_batch_retains_backlog");PauseActiveController();StepActiveController();RunEditorDraftController();
        CheckAccumulator(Advance(0)==3&&session.Snapshot.ScanNumber==before+VirtualControllerSession.MaximumScansPerAdvance+4,"step_does_not_consume_preexisting_backlog_resume_only_prior_time");
        PauseActiveController();session.PulseInput("pulse_received");StopActiveController();RunEditorDraftController();Advance(period);
        CheckAccumulator(session.Snapshot.Counters["press_count"].Accumulated==0,"stop_discards_pending_pulse_and_backlog_distinct_from_pause");
        void CheckAccumulator(bool ok,string name)=>check(ok,$"native_accumulator_{ms}ms_{name}");
    }
    private async Task VerifyScanInspectionImmediateTogglePause(int ms,Action<bool,string> check)
    {
        await LoadScanInspectionFixture("demo2",ms,false);
        var session=_virtualController!;var runtime=_sceneRuntime!;var scan=session.Snapshot.ScanNumber;
        var toggled=ExecuteSelectedControllerAction("toggle-timer_request");
        check(toggled&&runtime.Points["timer_request"] is true&&session.Snapshot.ScanNumber==scan,$"running_toggle_changes_actual_input_before_any_scan_{ms}ms");
        PauseActiveController();var held=JsonSerializer.Serialize(session.Snapshot);var pose=ScanInspectionNativePose();
        var queued=ExecuteSelectedControllerAction("toggle-timer_request");
        check(queued&&session.TryGetPendingInput("timer_request",out var pending)&&!pending&&runtime.Points["timer_request"] is true&&JsonSerializer.Serialize(session.Snapshot)==held&&ScanInspectionNativePose()==pose,$"paused_toggle_uses_latest_actual_input_queues_false_no_live_mutation_{ms}ms");
        // Repeated clicks must toggle the already queued image, not reread the held plant.
        ExecuteSelectedControllerAction("toggle-timer_request");
        check(session.TryGetPendingInput("timer_request",out pending)&&pending&&runtime.Points["timer_request"] is true,$"paused_repeated_toggle_prefers_pending_image_{ms}ms");
        ExecuteSelectedControllerAction("toggle-timer_request");
        VirtualControllerScanCompleted? accepted=null;var events=0;
        void Accept(VirtualControllerScanCompleted row){accepted=row;events++;}
        session.ScanCompleted+=Accept;var time=session.Snapshot.SimulatedTime;
        try{StepActiveController();}finally{session.ScanCompleted-=Accept;}
        check(events==1&&session.IsPaused&&session.Snapshot.ScanNumber==scan+1&&session.Snapshot.SimulatedTime==time+TimeSpan.FromMilliseconds(ms)&&accepted is not null&&accepted.SampledInputs.Bools["timer_request"]==false&&accepted.EffectiveInputs.Bools["timer_request"]==false&&session.Snapshot.Timers["delay_timer"].Accumulated==TimeSpan.Zero&&runtime.Points["delayed_lamp"] is false&&FiveDemoLamp("indicator_1",false),$"queued_false_step_exact_scan_sample_effective_ton_zero_actual_lamp_off_{ms}ms");
        StopActiveController();
    }
    // Root's capture wrapper may await this before its normal same-binary screenshot path.
    // It prepares real accepted state and leaves the product Pause lease active.
    private async Task PrepareScanInspectionReviewCapture(string state)
    {
        var kind=state switch{"timer"=>"demo2","carry" or "invalid"=>"demo5","coast"=>"roller","history"=>"demo3",_=>throw new ArgumentException("Unknown scan inspection capture state.")};
        await LoadScanInspectionFixture(kind,20,false);PauseActiveController();QueueScanInspectionStart(kind);StepActiveController();
        if(state=="timer")for(var i=0;i<49;i++)StepActiveController();
        else if(state is "carry" or "invalid")
        {
            var count=0;while(_sceneRuntime!.Points["carton_attached"] is not true&&count++<1500)StepActiveController();
            if(count>=1500)throw new InvalidOperationException("Real Demo5 capture never attached.");
            for(var i=0;i<30;i++)StepActiveController();
            if(state=="invalid")
            {
                var invalid=new LadderEditorDocument();invalid.ResetProject("scan-capture-invalid","Invalid_Current",TimeSpan.FromMilliseconds(20));invalid.SourceSceneId=_currentSceneId!;
                _simulatorShell!.RestoreIntegratedLadderDraft(LadderEditorProjectJson.Save(invalid));
                _simulatorShell.TryBuildCurrentLadderProgram(out _,out var issues);_simulatorShell.SetVirtualControllerValidation(issues);StepActiveController();
            }
        }
        else if(state=="coast")
        {
            var beam=WorkspaceFiniteRollerPolicy.Relation().BeamPoint;var count=0;while(_sceneRuntime!.Points[beam] is not true&&count++<1500)StepActiveController();StepActiveController();
            if(count>=1500||_sceneRuntime!.CaptureWorkspaceRollerPreset().SpeedMps<=0)throw new InvalidOperationException("Real roller capture never reached genuine coast.");
        }
        else
        {
            for(var i=0;i<30;i++)StepActiveController();PublishScanInspectionState();
            if(!_simulatorShell!.VerifyScanInspectionHistorySelection(0,out var detail))throw new InvalidOperationException("Actual historic row capture failed: "+detail);
        }
        PublishScanInspectionState();GD.Print($"SCAN_INSPECTION_CAPTURE_PREPARED state={state} scan={_virtualController!.Snapshot.ScanNumber} paused={_virtualController.IsPaused} periodMs=20 transport=none");
    }
}
