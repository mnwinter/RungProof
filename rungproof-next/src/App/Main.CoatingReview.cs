using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditCoating;
    private void AuditCoating()
    {
        var failures=0;
        void Check(bool ok,string n) { if(!ok)failures++;GD.Print($"COATING_CHECK {n}={ok}"); }
        try { VerifyCoatingWorkflow(Check); } catch(Exception e) {failures++;GD.PushError(e.ToString());}
        GD.Print($"COATING_VERIFY {(failures==0?"PASS":"FAIL")} offline-only");GetTree().Quit(failures==0?0:1);
    }
    private static LadderEditorDocument CreateCoatingReference()
    {
        var d=new LadderEditorDocument();d.ResetProject("review-coating-line","Coating_Line_Reference",TimeSpan.FromMilliseconds(20));
        d.SourceSceneId="lab-5-10-coating-line";
        foreach(var n in new[]{"workpiece_at_station","workpiece_at_exit","start_request","spray_ready","ventilation_ready","motion_inhibited","spray_inhibited","travel_limited"})d.AddTag(n,PlcVariableRole.Input,n);
        foreach(var n in new[]{"workpiece_position","belt_speed"})d.AddTag(n,PlcVariableRole.Input,n,type:PlcVariableType.Real);
        foreach(var n in new[]{"index_run","spray_enable","vent_run"})d.AddTag(n,PlcVariableRole.Output,n);
        d.AddTag("phase",PlcVariableRole.Memory,type:PlcVariableType.DInt);
        foreach(var n in new[]{"spray_timer","purge_timer"})d.AddTag(n,PlcVariableRole.Memory,type:PlcVariableType.Timer);
        void Phase(int r,int b,int phase) => d.AddComparison(r,b,"phase",LadderCompareOperator.Equal,phase.ToString());
        void Ready(int r,int b) {d.AddContact(r,b,"spray_ready",false);d.AddContact(r,b,"ventilation_ready",false);}
        // TON reads the preceding command image; transition scan then removes
        // the command after the full number of accepted spray/purge ticks.
        d.AddTimerRung("Time actual eligible spray command", "spray_timer",TimeSpan.FromSeconds(2));
        Phase(0,0,2);Ready(0,0);d.AddContact(0,0,"workpiece_at_station",false);d.AddContact(0,0,"spray_enable",false);
        d.AddTimerRung("Post-spray ventilation purge", "purge_timer",TimeSpan.FromSeconds(1));
        Phase(1,0,3);Ready(1,0);d.AddContact(1,0,"vent_run",false);d.AddContact(1,0,"spray_enable",true);
        void Move(string label,int from,int to,params string[] inputs) {
            var r=d.Rungs.Count;d.AddNumericOperationRung(label,LadderNumericOperationKind.Move,to.ToString(),"","phase");
            Phase(r,0,from);foreach(var n in inputs)d.AddContact(r,0,n,false);
        }
        Move("Fresh START with both manual permissives",0,1,"start_request","spray_ready","ventilation_ready");
        Move("Actual STATION stops indexing",1,2,"workpiece_at_station");
        Move("Completed two-second spray enters purge",2,3,"spray_timer.Q");
        Move("Completed one-second purge permits discharge",3,4,"purge_timer.Q");
        Move("Actual EXIT ends the single-load cycle",4,5,"workpiece_at_exit");
        var run=d.Rungs.Count;d.AddRung("Index only before station or during discharge", "index_run");
        Phase(run,0,1);Ready(run,0);d.AddContact(run,0,"workpiece_at_station",true);
        d.AddParallelBranch(run);Phase(run,1,4);Ready(run,1);d.AddContact(run,1,"workpiece_at_exit",true);
        var spray=d.Rungs.Count;d.AddRung("Spray only over positioned part with ventilation", "spray_enable");Phase(spray,0,2);Ready(spray,0);d.AddContact(spray,0,"workpiece_at_station",false);
        var vent=d.Rungs.Count;d.AddRung("Ventilate spray, purge and discharge", "vent_run");
        d.AddComparison(vent,0,"phase",LadderCompareOperator.GreaterOrEqual,"2");d.AddComparison(vent,0,"phase",LadderCompareOperator.LessOrEqual,"4");d.AddContact(vent,0,"ventilation_ready",false);
        d.WatchVariables.AddRange(d.Tags.Select(t=>t.Name));return d;
    }
    private void VerifyCoatingWorkflow(Action<bool,string> check)
    {
        void Check(bool ok,string n)=>check(ok,"coating_"+n);
        AddMigratedScene("lab-5-10-coating-line",_candidateCatalog!,_mainCamera!,false,false);
        var root=_sceneCompositionRoot!;var runtime=_sceneRuntime!;var plant=runtime.CoatingPlant!;
        var part=root.GetNode<Node3D>("training_accessory_7");
        var belt=(MeshInstance3D)root.GetNode<Node3D>("conveyor_0").FindChild("KIN_belt_surface",true,false);
        var spray=root.GetNode<MeshInstance3D>("CoatingSpray");
        var conveyor=root.GetNode<Node3D>("conveyor_0").FindChildren("*","",true,false).OfType<ConveyorController>().Single();
        bool On(string n)=>runtime.Points[n] is true;
        Check(root.GetNodeOrNull<Node3D>("machine_1") is null && part.FindChild("WORKPIECE_BODY",true,false) is MeshInstance3D
            && root.GetNode<Node3D>("training_accessory_5").FindChild("TUNNEL_roof",true,false) is not null
            && root.GetNode<Node3D>("training_accessory_8").FindChild("DAMPER_vane",true,false) is not null,"correct_model_identities_and_no_cnc_on_belt");
        Check(!On("workpiece_at_station")&&!On("workpiece_at_exit")&&!spray.Visible&&plant.Position==-2&&!conveyor.IsPhysicsProcessing(),"initial_supported_home_and_single_clock");
        Check(!ExecuteSelectedControllerAction("toggle-workpiece_at_station"),"fake_station_toggle_removed");
        var d=CreateCoatingReference();var compiled=LadderCompiler.Compile(d.BuildProgram());
        if(!compiled.IsValid)throw new InvalidOperationException(string.Join(";",compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-coating-line.rpproj.json"),LadderEditorProjectJson.Save(d));
        EnableVirtualControllerProgram(d.BuildProgram());
        try {
            var samples=0;var supported=true;var separated=true;var projections=true;var spraySamples=0;var purgeSamples=0;
            var solids=root.GetChildren().OfType<Node3D>().Where(n=>n!=part).SelectMany(ReviewMeshes)
                .Where(m=>m!=spray&&!m.Name.ToString().StartsWith("KIN_beam")).ToArray();
            double Phase()=>_virtualController!.Snapshot.NumericVariables["phase"];
            void Tick(int count=1) {for(var i=0;i<count;i++) {
                _PhysicsProcess(.02);samples++;var b=ReviewBounds(part);var deck=ReviewBounds(belt);
                supported &= Math.Abs(b.Position.Y-deck.End.Y)<.001&&b.Position.X>=deck.Position.X&&b.End.X<=deck.End.X&&b.Position.Z>=deck.Position.Z&&b.End.Z<=deck.End.Z;
                separated &= ReviewMeshes(part).All(m=>solids.All(f=>{var o=ReviewBounds(m).Intersection(ReviewBounds(f)).Size;return o.X<=.005||o.Y<=.005||o.Z<=.005||!OrientedBoxesPenetrate(m,f);}));
                projections &= Math.Abs(plant.Position-part.Position.X)<.0001&&Math.Abs(plant.Speed-conveyor.ActualSpeedMps)<.0001
                    &&spray.Visible==plant.Spraying&&!On("motion_inhibited")&&!On("spray_inhibited")&&!On("travel_limited");
                if(plant.Spraying)spraySamples++;if(Phase()==3&&On("vent_run"))purgeSamples++;
            }}
            void Action(string n) {if(!ExecuteSelectedControllerAction(n))throw new InvalidOperationException("Coating action rejected: "+n);}
            void Until(Func<bool> pred,int bound=1500) {for(var i=0;i<bound&&!pred();i++)Tick();}
            RunActiveController();Action("pulse-start_request");Tick(10);Check(Phase()==0&&plant.Position==-2,"blocked_start_discarded");
            Action("toggle-spray_ready");Action("toggle-ventilation_ready");Tick(10);Check(Phase()==0&&plant.Position==-2,"readiness_alone_does_not_replay_start");
            Action("pulse-start_request");Tick(100);Check(Phase()==1&&Math.Abs(plant.Position+1)<.0001&&On("index_run"),"two_seconds_indexes_one_metre");
            var held=plant.Position;StopActiveController();var scan=_virtualController!.Snapshot.ScanNumber;Tick(25);
            Check(plant.Position==held&&plant.Speed==0&&!On("index_run")&&!spray.Visible&&scan==_virtualController.Snapshot.ScanNumber,"stop_freezes_motion_scan_and_clears_commands");
            RunActiveController();Until(()=>Phase()==2);Check(On("workpiece_at_station")&&!On("index_run")&&On("spray_enable")&&On("vent_run")&&spray.Visible&&Math.Abs(plant.Position)<.04,"actual_station_stops_under_mounted_nozzle");
            Tick(30);held=plant.Position;Action("toggle-ventilation_ready");Tick(10);
            Check(Phase()==2&&plant.Position==held&&!spray.Visible&&!On("spray_enable")&&!On("vent_run")&&_virtualController!.Snapshot.Timers["spray_timer"].Accumulated==TimeSpan.Zero,"lost_vent_inhibits_and_resets_nonretentive_spray_timer");
            Action("toggle-ventilation_ready");Tick(30);StopActiveController();var timer=_virtualController!.Snapshot.Timers["spray_timer"].Accumulated;Tick(20);
            Check(plant.Position==held&&!spray.Visible&&_virtualController.Snapshot.Timers["spray_timer"].Accumulated==timer,"stop_freezes_mid_spray_timer_and_pose");
            RunActiveController();Tick();Check(Phase()==2 && spray.Visible && _virtualController!.Snapshot.Timers["spray_timer"].Accumulated==TimeSpan.Zero,"run_resumes_spray_phase_with_fresh_full_nonretentive_interval");
            Until(()=>Phase()==3);Check(!On("spray_enable")&&!spray.Visible&&On("vent_run")&&!On("index_run")&&plant.Position==held,"spray_complete_enters_stationary_ventilation_purge");
            Tick(20);Check(Phase()==3&&plant.Position==held,"purge_keeps_load_stationary");
            Until(()=>Phase()==4);Check(On("index_run")&&!On("spray_enable")&&On("vent_run")&&purgeSamples>=50,"purge_time_precedes_discharge");
            Until(()=>Phase()==5);Check(On("workpiece_at_exit")&&!On("workpiece_at_station")&&!On("index_run")&&!On("vent_run")&&!spray.Visible&&part.Visible,"actual_exit_stops_retains_supported_visible_part");
            held=plant.Position;Action("pulse-start_request");Tick(100);Check(Phase()==5&&plant.Position==held,"completed_single_load_waits_for_reset");
            Check(supported,"every_sample_fixture_bears_on_belt_with_supported_footprint");Check(separated,"sampled_load_clears_fixed_visible_mesh_solids");Check(projections,"actual_pose_speed_spray_and_no_diagnostic_each_sample");
            GD.Print($"COATING_SAMPLES {samples} sprayTicks={spraySamples} purgeTicks={purgeSamples}");
            ResetActiveController();Check(plant.Position==-2&&!On("start_request")&&!On("spray_ready")&&!On("ventilation_ready")&&!On("index_run")&&!On("vent_run")&&!spray.Visible&&_virtualController!.Snapshot.ScanNumber==0,"reset_restores_home_raw_false_and_zero_scan");
        } finally {DisableVirtualController();}
        // A separate uninterrupted cycle measures the actual enabled spray and
        // purge samples, independently of the Stop/permissive probes above.
        EnableVirtualControllerProgram(d.BuildProgram());
        try {
            var enabled=0;var purging=0;var count=0;
            RunActiveController();ExecuteSelectedControllerAction("toggle-spray_ready");ExecuteSelectedControllerAction("toggle-ventilation_ready");ExecuteSelectedControllerAction("pulse-start_request");
            while (_virtualController!.Snapshot.NumericVariables["phase"] != 5 && count++ < 1200) {
                _PhysicsProcess(.02);
                if(plant.Spraying) enabled++;
                if(_virtualController.Snapshot.NumericVariables["phase"]==3 && On("vent_run")) purging++;
            }
            Check(enabled==100 && purging==50 && On("workpiece_at_exit"),"uninterrupted_two_second_spray_and_one_second_purge_at_20ms");
            GD.Print($"COATING_HEALTHY_SAMPLES {count} sprayTicks={enabled} purgeTicks={purging}");
        } finally {DisableVirtualController();}
        runtime.ResetSimulation();runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string,bool>{{"index_run",true},{"spray_enable",true},{"vent_run",true}});runtime.AdvanceSimulation(.5);
        Check(plant.Position==-2&&On("motion_inhibited")&&On("spray_inhibited")&&On("index_run")&&On("spray_enable")&&!spray.Visible,"malformed_outputs_inhibit_without_rewriting_plc_image");
        runtime.ResetSimulation();runtime.ExecuteAction("toggle-spray_ready");runtime.ExecuteAction("toggle-ventilation_ready");runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new Dictionary<string,bool>{{"index_run",true}});runtime.AdvanceSimulation(30);
        Check(plant.Position==2.6&&On("travel_limited")&&On("index_run")&&part.Visible,"missed_exit_clamps_supported_load_without_fake_plc_stop");
        runtime.ResetSimulation();runtime.SetControllerPlaybackRunning(false);
    }
}
