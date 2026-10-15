using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditLuggageLayout;
    private void AuditLuggageLayout()
    {
        var failures=0;
        void Check(bool ok,string name) {if(!ok)failures++;GD.Print($"LUGGAGE_LAYOUT_CHECK {name}={ok}");}
        try {
            AddMigratedScene("lab-6-07-luggage-weight-sort",_candidateCatalog!,_mainCamera!,false,false);
            var root=_sceneCompositionRoot!;
            var bag=root.GetNode<Node3D>("training_accessory_5");
            var belt=(MeshInstance3D)root.GetNode<Node3D>("conveyor_0").FindChild("KIN_belt_surface",true,false);
            var receiver=(MeshInstance3D)root.GetNode<Node3D>("reject_outfeed").FindChild("KIN_belt_surface",true,false);
            var scale=root.GetNode<Node3D>("training_accessory_4");
            var deck=(MeshInstance3D)scale.FindChild("SCALE_DECK_surface",true,false);
            var baseMesh=(MeshInstance3D)bag.FindChild("LUGGAGE_base",true,false);
            Check(bag.FindChild("LUGGAGE_BODY",true,false) is MeshInstance3D && root.GetNodeOrNull<Node3D>("machine_2") is null && root.GetNodeOrNull<Node3D>("box_1") is null,"actual_suitcase_and_no_cnc_or_extra_carton");
            bool Borne(Aabb load,Aabb surface)=>Math.Abs(load.Position.Y-surface.End.Y)<.005 && load.Position.X>=surface.Position.X-.001 && load.End.X<=surface.End.X+.001 && load.Position.Z>=surface.Position.Z-.001 && load.End.Z<=surface.End.Z+.001;
            Check(Borne(ReviewBounds(baseMesh),ReviewBounds(belt)),"home_base_bears_within_infeed_belt");
            var home=bag.Position;bag.Position=new(0,.9f,0);
            Check(Borne(ReviewBounds(baseMesh),ReviewBounds(deck)),"station_pose_base_bears_within_actual_weigh_deck");bag.Position=home;
            var cells=ReviewMeshes(scale).Where(m=>m.Name.ToString().StartsWith("LOAD_CELL_body_")).Select(ReviewBounds).ToArray();
            Check(cells.Length==4 && cells.All(cell=>Math.Abs(cell.End.Y-ReviewBounds(deck).Position.Y)<.005),"weigh_deck_bears_on_four_cell_bodies");
            var diverter=root.GetNode<Node3D>("training_accessory_7");
            var rollers=ReviewMeshes(diverter).Where(m=>m.Name.ToString().StartsWith("ROLLER_")).Select(ReviewBounds).ToArray();
            Check(rollers.Length==12 && rollers.All(roller=>Math.Abs(roller.End.Y-.9)<.005) && Math.Abs(ReviewBounds(receiver).End.Y-.9)<.005,"retained_roller_crowns_and_receiver_match_point_nine_plane");
            var readout=root.GetNode<Node3D>("training_accessory_6");
            var readoutParts=ReviewMeshes(readout);
            var readoutOthers=ReviewMeshes(root).Where(m=>!readoutParts.Contains(m)&&!m.Name.ToString().StartsWith("KIN_beam")).ToArray();
            Check(readoutParts.All(m=>readoutOthers.All(f=>{
                var overlap=ReviewBounds(m).Intersection(ReviewBounds(f)).Size;
                return overlap.X<=.002 || overlap.Y<=.002 || overlap.Z<=.002 || !OrientedBoxesPenetrate(m,f);
            })),"weight_readout_clears_sensor_feet_and_other_equipment");
            var bridge=ReviewBounds((MeshInstance3D)root.GetNode<Node3D>("luggage_transfer_bridge").FindChild("LUGGAGE_reject_bridge_surface",true,false));
            Check(Math.Abs(bridge.End.Y-.9)<.005 && bridge.Position.Z<=ReviewBounds(receiver).End.Z && bridge.End.Z<=-.43,"reject_receiving_bridge_reaches_outfeed_at_matching_height");
            var arm=(MeshInstance3D)diverter.FindChild("KIN_DIVERTER_ARM",true,false);
            var gate=diverter.GetNode<Node3D>("LuggageGatePivot");
            var solids=ReviewMeshes(root).Where(m=>!ReviewMeshes(bag).Contains(m) && !m.Name.ToString().StartsWith("KIN_beam")).ToArray();
            bool Clear() => ReviewMeshes(bag).All(m=>solids.All(f=> {
                var overlap=ReviewBounds(m).Intersection(ReviewBounds(f)).Size;
                return overlap.X<=.002 || overlap.Y<=.002 || overlap.Z<=.002 || !OrientedBoxesPenetrate(m,f);
            }));
            bool directClear=true;
            for(var i=0;i<=300;i++) {
                bag.Position=new(-3.2f+5.85f*i/300,.9f,0);directClear &= Clear();
            }
            Check(directClear,"301_prescribed_normal_route_poses_clear_visible_fixed_solids");
            gate.Rotation=new(0,Mathf.DegToRad(55),0);
            // Conservatively place the entire suitcase behind the arm plane.
            // Use actual delivered mesh corners, including the tag and handles,
            // rather than only the nominal rectangular body dimensions.
            var normal=gate.GlobalBasis.Z.Normalized();
            var pivot=gate.GlobalPosition;
            bag.Position=Vector3.Zero;
            var localProjection=ReviewMeshes(bag).SelectMany(m=> {
                var bounds=m.GetAabb();return Enumerable.Range(0,8).Select(i=>m.GlobalTransform*new Vector3(
                    (i&1)==0?bounds.Position.X:bounds.End.X,
                    (i&2)==0?bounds.Position.Y:bounds.End.Y,
                    (i&4)==0?bounds.Position.Z:bounds.End.Z));
            }).Max(v=>v.Dot(normal));
            float GuidedZ(float x)=>Math.Min(0,(pivot.Dot(normal)-.055f-localProjection-x*normal.X)/normal.Z);
            bool rejectClear=true;
            for(var i=0;i<=300;i++) {
                var x=-3.2f+5.5f*i/300;
                bag.Position=new(x,.9f,GuidedZ(x));rejectClear &= Clear();
            }
            var branchStart=GuidedZ(2.3f);
            for(var i=0;i<=150;i++) {
                bag.Position=new(2.3f,.9f,branchStart+(-3.03f-branchStart)*i/150);rejectClear &= Clear();
            }
            Check(rejectClear,"452_prescribed_reject_route_poses_clear_visible_fixed_solids");
            Check(Borne(ReviewBounds(baseMesh),ReviewBounds(receiver)),"reject_end_pose_is_retained_on_actual_receiving_belt");
            GD.Print($"LUGGAGE_GUIDE_GEOMETRY branchStartZ={branchStart} marginM=.005 prescribed-only");
            bag.Position=home;gate.Rotation=Vector3.Zero;
            AuditLuggageAdapter(Check);
            AuditLuggageReference(Check);

        } catch(Exception e) {failures++;GD.PushError(e.ToString());}
        GD.Print($"LUGGAGE_LAYOUT_VERIFY {(failures==0?"PASS":"FAIL")} geometry, offline adapter and controller reference; native moving-cycle pending");GetTree().Quit(failures==0?0:1);
    }
    private static LadderEditorDocument CreateLuggageReference()
    {
        var d=new LadderEditorDocument();d.ResetProject("review-luggage","Luggage_Weight_Reference",TimeSpan.FromMilliseconds(20));
        d.SourceSceneId="lab-6-07-luggage-weight-sort";
        foreach(var n in new[]{"bag_at_entry","bag_at_scale","bag_at_normal_exit","bag_at_reject_exit","weight_valid","scale_ready","motion_inhibited","travel_limited","diverter_ready"})d.AddTag(n,PlcVariableRole.Input,n);
        foreach(var n in new[]{"fixture_mass_kg","weight_kg","bag_x","bag_z","bag_speed"})d.AddTag(n,PlcVariableRole.Input,n,type:PlcVariableType.Real);
        foreach(var n in new[]{"infeed_run","discharge_run","weigh_cycle","reject_select"})d.AddTag(n,PlcVariableRole.Output,n);
        foreach(var n in new[]{"class_result","class_1_count","class_2_count","class_3_count"})d.AddTag(n,PlcVariableRole.Output,n,type:PlcVariableType.DInt);
        foreach(var n in new[]{"category","count_1","count_2","count_3"})d.AddTag(n,PlcVariableRole.Memory,type:PlcVariableType.DInt);
        d.AddTag("phase",PlcVariableRole.Memory,type:PlcVariableType.DInt);d.AddTag("weigh_timer",PlcVariableRole.Memory,type:PlcVariableType.Timer);
        void Phase(int r,int value)=>d.AddComparison(r,0,"phase",LadderCompareOperator.Equal,value.ToString());
        void Valid(int r){Phase(r,2);d.AddContact(r,0,"weight_valid",false);d.AddContact(r,0,"weigh_timer.Q",false);}
        // Half-second dwell and 15/25 kg category limits are illustrative, not
        // source/OEM baggage limits or scale accuracy specifications.
        d.AddTimerRung("Illustrative half-second valid weighing dwell","weigh_timer",TimeSpan.FromSeconds(.5));
        Phase(0,2);d.AddContact(0,0,"weigh_cycle",false);d.AddContact(0,0,"weight_valid",false);
        foreach(var from in new[]{0,4}){
            var r=d.Rungs.Count;d.AddNumericOperationRung("Fresh entry starts indexing",LadderNumericOperationKind.Move,"1","","phase");
            Phase(r,from);d.AddContact(r,0,"bag_at_entry",false);
        }
        var station=d.Rungs.Count;d.AddNumericOperationRung("Actual station stops indexing",LadderNumericOperationKind.Move,"2","","phase");
        Phase(station,1);d.AddContact(station,0,"bag_at_scale",false);
        var clear=d.Rungs.Count;d.AddNumericOperationRung("Clear result for next entering bag",LadderNumericOperationKind.Move,"0","","category");Phase(clear,1);
        foreach(var category in new[]{1,2,3}) {
            var r=d.Rungs.Count;d.AddNumericOperationRung("Classify once: category "+category,LadderNumericOperationKind.Move,category.ToString(),"","category");Valid(r);
            if(category==1)d.AddComparison(r,0,"weight_kg",LadderCompareOperator.LessOrEqual,"15");
            if(category==2){d.AddComparison(r,0,"weight_kg",LadderCompareOperator.GreaterThan,"15");d.AddComparison(r,0,"weight_kg",LadderCompareOperator.LessOrEqual,"25");}
            if(category==3)d.AddComparison(r,0,"weight_kg",LadderCompareOperator.GreaterThan,"25");
            r=d.Rungs.Count;var count="count_"+category;
            d.AddNumericOperationRung("Increment only selected category",LadderNumericOperationKind.Add,count,"1",count);Valid(r);
            d.AddComparison(r,0,"category",LadderCompareOperator.Equal,category.ToString());
        }
        var done=d.Rungs.Count;d.AddNumericOperationRung("Completed valid dwell permits discharge",LadderNumericOperationKind.Move,"3","","phase");Valid(done);
        foreach(var (beam,category) in new[]{("bag_at_normal_exit",false),("bag_at_reject_exit",true)}) {
            var r=d.Rungs.Count;d.AddNumericOperationRung("Actual exit finishes cycle",LadderNumericOperationKind.Move,"4","","phase");Phase(r,3);
            d.AddContact(r,0,beam,false);d.AddComparison(r,0,"category",category?LadderCompareOperator.Equal:LadderCompareOperator.LessThan,"3");
        }
        foreach(var (name,phase) in new[]{("infeed_run",1),("weigh_cycle",2),("discharge_run",3)}) {
            var r=d.Rungs.Count;d.AddRung("PLC command "+name,name);Phase(r,phase);
            if(phase==2)d.AddContact(r,0,"scale_ready",false);
        }
        var route=d.Rungs.Count;d.AddRung("Third category selects reject route","reject_select");Phase(route,3);d.AddComparison(route,0,"category",LadderCompareOperator.Equal,"3");
        // Stop clears public numeric output images. Keep the counted result in
        // PLC memory so Run restores it without counting the same bag again.
        var publish=d.Rungs.Count;d.AddNumericOperationRung("Publish retained category",LadderNumericOperationKind.Move,"category","","class_result");d.AddComparison(publish,0,"phase",LadderCompareOperator.GreaterOrEqual,"0");
        foreach(var c in new[]{1,2,3}){var r=d.Rungs.Count;d.AddNumericOperationRung("Publish retained class counter",LadderNumericOperationKind.Move,"count_"+c,"","class_"+c+"_count");d.AddComparison(r,0,"phase",LadderCompareOperator.GreaterOrEqual,"0");}
        d.WatchVariables.AddRange(d.Tags.Select(t=>t.Name));return d;
    }
    private void AuditLuggageAdapter(Action<bool,string> check)
    {
        var runtime=_sceneRuntime!;runtime.ResetSimulation();runtime.UsesExternalClock=true;runtime.SetControllerPlaybackRunning(true);
        bool On(string n)=>runtime.Points[n] is true;
        double N(string n)=>Convert.ToDouble(runtime.Points[n]);
        void Image(bool feed=false,bool weigh=false,bool discharge=false,bool reject=false)=>runtime.CommitVirtualControllerOutputs(
            new System.Collections.Generic.Dictionary<string,bool> { ["infeed_run"]=feed,["weigh_cycle"]=weigh,["discharge_run"]=discharge,["reject_select"]=reject });
        void Tick(int count=1) {for(var i=0;i<count;i++)runtime.AdvanceSimulation(.02);}
        check(new[]{"load-next-bag","toggle-scale_ready","cycle-fixture_mass_kg"}.All(id=>_sceneControlInteractor!.Bindings.Any(b=>b.ActionId==id)),"declared_three_dimensional_operator_actions_are_wired");
        check(On("bag_at_entry")&&!On("bag_at_scale")&&!On("weight_valid"),"adapter_home_mesh_entry_feedback");
        check(!runtime.ExecuteAction("load-next-bag"),"new_bag_rejected_before_exit");
        runtime.ExecuteAction("cycle-fixture_mass_kg");Image(feed:true);Tick(50);
        check(!runtime.ExecuteAction("cycle-fixture_mass_kg"),"mass_selection_rejected_during_travel");
        var x=N("bag_x");runtime.SetControllerPlaybackRunning(false);Tick(20);
        check(N("bag_x")==x && N("bag_speed")==0,"paused_adapter_does_not_integrate");
        runtime.SetControllerPlaybackRunning(true);Tick(300);
        check(Math.Abs(N("bag_x"))<.001&&On("bag_at_scale"),"infeed_stop_pose_intersects_actual_station_beam");
        Image(weigh:true);Tick();check(On("weight_valid")&&N("weight_kg")==22,"live_weight_uses_latched_fixture_on_scale");
        runtime.ExecuteAction("toggle-scale_ready");Tick();check(!On("weight_valid")&&N("weight_kg")==0,"scale_not_ready_invalidates_weight");
        runtime.ExecuteAction("toggle-scale_ready");Image(discharge:true);Tick(300);
        check(On("bag_at_normal_exit")&&N("bag_x")<=2.6501&&!On("weight_valid"),"normal_exit_feedback_and_retained_travel_limit");
        check(ReviewMeshes(_sceneCompositionRoot!.GetNode<Node3D>("training_accessory_7")).Where(m=>m.Name.ToString().StartsWith("ROLLER_")).All(m=>Math.Abs(m.Mesh.GetFaces().Max(v=>(m.GlobalTransform*v).Y)-.9)<.001),"animated_roller_mesh_crowns_preserve_receiving_height");
        Image();check(runtime.ExecuteAction("load-next-bag")&&On("bag_at_entry"),"explicit_next_bag_reloads_after_stopped_exit");
        Image(feed:true);Tick(350);Image(discharge:true,reject:true);Tick(100);
        x=N("bag_x");var z=N("bag_z");Image(discharge:true,reject:false);Tick(20);
        check(On("motion_inhibited")&&On("discharge_run")&&N("bag_x")==x&&N("bag_z")==z,"route_change_inhibited_without_rewriting_command_image");
        Image(discharge:true,reject:true);Tick(550);
        check(On("bag_at_reject_exit")&&N("bag_z")>=-3.0301,"reject_exit_feedback_and_retained_travel_limit");
        runtime.ResetSimulation();runtime.SetControllerPlaybackRunning(false);
        check(Math.Abs(N("bag_x")+3.2)<.001&&N("bag_z")==0&&N("class_1_count")==0,"reset_restores_initial_bag_and_counters");
    }

    private void AuditLuggageReference(Action<bool,string> check)
    {
        var d=CreateLuggageReference();var compiled=LadderCompiler.Compile(d.BuildProgram());
        if(!compiled.IsValid)throw new InvalidOperationException(string.Join(";",compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-luggage.rpproj.json"),LadderEditorProjectJson.Save(d));
        _sceneRuntime!.ResetSimulation();EnableVirtualControllerProgram(d.BuildProgram());
        try {
            var runtime=_sceneRuntime!;
            bool On(string n)=>runtime.Points[n] is true;
            double N(string n)=>Convert.ToDouble(runtime.Points[n]);
            void Tick(int count=1){for(var i=0;i<count;i++)_PhysicsProcess(.02);}
            RunActiveController();
            for(var category=1;category<=3;category++) {
                if(category>1) {
                    check(ExecuteSelectedControllerAction("cycle-fixture_mass_kg"),"reference_next_fixture_mass_"+category);
                    check(ExecuteSelectedControllerAction("load-next-bag"),"reference_load_next_bag_"+category);
                }
                var sawWeight=false;
                for(var i=0;i<1600;i++){Tick();sawWeight|=On("weight_valid")&&N("weight_kg")==category*10+2;
                    if(N("class_result")==category&&!On("discharge_run")&&(On("bag_at_normal_exit")||On("bag_at_reject_exit")))break;}
                check(sawWeight&&N("class_result")==category&&!On("discharge_run")&&!On("infeed_run"),"reference_category_"+category+"_weighed_classified_and_stopped");
                check(Enumerable.Range(1,3).All(c=>N("class_"+c+"_count")== (c<=category?1:0)),"reference_only_selected_counter_incremented_once_"+category);
                var before=N("class_"+category+"_count");Tick(200);
                check(N("class_"+category+"_count")==before,"reference_held_exit_cannot_recount_"+category);
            }
            StopActiveController();RunActiveController();Tick();
            check(N("class_result")==3&&Enumerable.Range(1,3).All(c=>N("class_"+c+"_count")==1),"reference_stop_run_restores_retained_result_and_counters");
            ResetActiveController();RunActiveController();
            for(var i=0;i<1000 && !On("weigh_cycle");i++)Tick();
            Tick(10);StopActiveController();var x=N("bag_x");var scans=_virtualController!.Snapshot.ScanNumber;Tick(20);
            check(N("bag_x")==x&&_virtualController.Snapshot.ScanNumber==scans&&!On("weigh_cycle")&&!On("weight_valid"),"reference_stop_freezes_clock_and_invalidates_weighing");
            RunActiveController();Tick();check(On("weigh_cycle")&&N("class_1_count")==0,"reference_resume_restarts_nonretentive_dwell");
            ResetActiveController();check(N("class_result")==0&&N("class_1_count")==0&&On("bag_at_entry"),"reference_reset_restores_bag_and_counters");
        } finally {DisableVirtualController();}
    }

}
