using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private static LadderEditorDocument CreateRadarReference()
    {
        var d=new LadderEditorDocument();d.ResetProject("review-radar-tank","Radar_Tank_Reference",TimeSpan.FromMilliseconds(20));
        d.SourceSceneId="tank-radar";
        foreach(var n in new[]{"cycle_request","manual_drain_request","radar_echo_ok"})d.AddTag(n,PlcVariableRole.Input,n);
        foreach(var n in new[]{"radar_level","radar_distance","radar_signal"})d.AddTag(n,PlcVariableRole.Input,n,type:PlcVariableType.Real);
        foreach(var n in new[]{"inlet_pump_run","drain_valve_open"})d.AddTag(n,PlcVariableRole.Output,n);
        d.AddTag("drain_phase",PlcVariableRole.Memory);
        // Illustrative 20/80% hysteresis: PLC owns the phase and comparisons.
        var high=d.Rungs.Count;d.AddRung("Eighty percent selects draining","drain_phase").CoilMode=LadderCoilMode.Set;
        d.AddComparison(high,0,"radar_level",LadderCompareOperator.GreaterOrEqual,"80");
        var low=d.Rungs.Count;d.AddRung("Twenty percent selects filling","drain_phase").CoilMode=LadderCoilMode.Reset;
        d.AddComparison(low,0,"radar_level",LadderCompareOperator.LessOrEqual,"20");
        var fill=d.Rungs.Count;d.AddRung("Requested healthy cycle fills below high threshold","inlet_pump_run");
        foreach(var n in new[]{"cycle_request","radar_echo_ok"})d.AddContact(fill,0,n,false);
        foreach(var n in new[]{"drain_phase","manual_drain_request"})d.AddContact(fill,0,n,true);
        var drain=d.Rungs.Count;d.AddRung("Requested healthy cycle drains in drain phase or manual request","drain_valve_open");
        foreach(var n in new[]{"drain_phase","manual_drain_request"}) {
            var branch=n=="drain_phase"?0:1;
            if(branch==1)d.AddParallelBranch(drain);
            foreach(var input in new[]{"cycle_request","radar_echo_ok",n})d.AddContact(drain,branch,input,false);
        }
        d.WatchVariables.AddRange(d.Tags.Select(t=>t.Name));return d;
    }
    private void AuditRadarReference(Action<bool,string> check)
    {
        var document=CreateRadarReference();var compiled=LadderCompiler.Compile(document.BuildProgram());
        if(!compiled.IsValid)throw new InvalidOperationException(string.Join(";",compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-radar.rpproj.json"),LadderEditorProjectJson.Save(document));
        _sceneRuntime!.ResetSimulation();EnableVirtualControllerProgram(document.BuildProgram());
        try {
            var runtime=_sceneRuntime!;
            bool On(string n)=>runtime.Points[n] is true;
            double N(string n)=>Convert.ToDouble(runtime.Points[n]);
            void Tick(int count=1) {for(var i=0;i<count;i++)_PhysicsProcess(.02);}
            var valve=_sceneCompositionRoot!.GetNode<Node3D>("radar_drain_valve");
            var pointer=(MeshInstance3D)valve.FindChild("POSITION_pointer",true,false);
            var liner=(MeshInstance3D)valve.FindChild("PROCESS_bore_liner",true,false);
            var bore=(liner.GlobalBasis*Vector3.Up).Normalized();
            bool PointerMatches(bool open) {
                var size=pointer.GetAabb().Size;
                var local=size.X>size.Y&&size.X>size.Z?Vector3.Right:size.Y>size.Z?Vector3.Up:Vector3.Back;
                var dot=MathF.Abs((pointer.GlobalBasis*local).Normalized().Dot(bore));return open?dot>.999f:dot<.001f;
            }
            RunActiveController();Tick(10);
            check(!On("inlet_pump_run")&&!On("drain_valve_open")&&Math.Abs(N("radar_level")-35)<.001,"reference_idle_without_cycle_request");
            check(ExecuteSelectedControllerAction("toggle-pump"),"reference_local_cycle_request_accepted");
            var transitions=0;var previous=false;var consistent=true;var surfaceConsistent=true;
            for(var i=0;i<2200 && transitions<3;i++) {
                Tick();var draining=On("drain_valve_open");if(draining!=previous){transitions++;previous=draining;}
                consistent &= PointerMatches(draining) && !(draining&&On("inlet_pump_run")) && Math.Abs(N("radar_signal")-(4+.16*N("radar_level")))<1e-6;
                var tank=_sceneCompositionRoot!.GetNode<Node3D>("water_tank_radar");
                var surface=tank.GetNode<MeshInstance3D>("REVIEW_liquid_surface");
                var liquid=(MeshInstance3D)tank.FindChild("KIN_liquid",true,false);
                surfaceConsistent &= Math.Abs(ReviewBounds(surface).End.Y-ReviewBounds(liquid).End.Y)<.001f;
            }
            check(transitions==3 && consistent,"reference_real_thresholds_cycle_fill_drain_fill_drain_with_matching_pointer");
            check(surfaceConsistent,"reference_inspection_surface_tracks_liquid_datum_through_three_transitions");
            var level=N("radar_level");var distance=N("radar_distance");StopActiveController();var scans=_virtualController!.Snapshot.ScanNumber;Tick(30);
            check(N("radar_level")==level&&N("radar_distance")==distance&&_virtualController.Snapshot.ScanNumber==scans&&!On("drain_valve_open")&&PointerMatches(false),"reference_stop_holds_surface_and_range_and_closes_pointer");
            RunActiveController();Tick();
            check(On("drain_valve_open")&&!On("inlet_pump_run")&&PointerMatches(true),"reference_resume_retains_drain_phase");
            ExecuteSelectedControllerAction("toggle-pump");Tick();
            check(!On("drain_valve_open")&&!On("inlet_pump_run")&&PointerMatches(false),"reference_cycle_request_removal_stops_both_commands");
            ResetActiveController();RunActiveController();
            ExecuteSelectedControllerAction("toggle-pump");ExecuteSelectedControllerAction("toggle-drain");Tick();
            check(On("drain_valve_open")&&!On("inlet_pump_run")&&PointerMatches(true),"reference_manual_drain_request_excludes_fill");
            ResetActiveController();
            check(!On("cycle_request")&&!On("manual_drain_request")&&!On("inlet_pump_run")&&!On("drain_valve_open")&&Math.Abs(N("radar_level")-35)<.001&&PointerMatches(false),"reference_reset_restores_initial_level_and_requests");
        } finally {DisableVirtualController();}
    }
}
