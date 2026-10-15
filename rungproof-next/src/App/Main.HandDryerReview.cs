using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditHandDryer;
    private static LadderEditorDocument CreateHandDryerReference()
    {
        var d = new LadderEditorDocument();
        d.ResetProject("review-hand-dryer", "Hand_Dryer_Reference", TimeSpan.FromMilliseconds(20));
        d.SourceSceneId = "lab-6-08-hand-dryer";
        foreach (var n in new[] { "hands_inserted", "hands_present", "heater_inhibited" }) d.AddTag(n, PlcVariableRole.Input, n);
        foreach (var n in new[] { "blower_run", "heater_enable" }) d.AddTag(n, PlcVariableRole.Output, n);
        d.AddTag("remaining_seconds", PlcVariableRole.Output, "remaining_seconds", type: PlcVariableType.Real);
        d.AddTag("remaining_ms", PlcVariableRole.Memory, type: PlcVariableType.Real);
        d.AddTag("phase", PlcVariableRole.Memory, type: PlcVariableType.DInt);
        d.AddTag("dryer_timer", PlcVariableRole.Memory, type: PlcVariableType.Timer);
        void Phase(int r, int value) => d.AddComparison(r, 0, "phase", LadderCompareOperator.Equal, value.ToString());
        // Timer reads the previous output image so the eligible actuator command
        // lasts exactly 500 accepted 20 ms ticks. Stop clears that image; Run
        // therefore starts a fresh nonretentive interval in the retained phase.
        d.AddTimerRung("Ten-second illustrated interval", "dryer_timer", TimeSpan.FromSeconds(10));
        Phase(0, 1); d.AddContact(0, 0, "hands_present", false); d.AddContact(0, 0, "blower_run", false);
        d.AddNumericOperationRung("Withdrawal rearms the cycle", LadderNumericOperationKind.Move, "0", "", "phase");
        d.AddContact(1, 0, "hands_present", true);
        d.AddNumericOperationRung("Presence starts a new interval", LadderNumericOperationKind.Move, "1", "", "phase");
        Phase(2, 0); d.AddContact(2, 0, "hands_present", false);
        d.AddNumericOperationRung("Completion waits for withdrawal", LadderNumericOperationKind.Move, "2", "", "phase");
        Phase(3, 1); d.AddContact(3, 0, "dryer_timer.Q", false);
        foreach (var n in new[] { "blower_run", "heater_enable" }) {
            var r=d.Rungs.Count; d.AddRung("Eligible " + n, n); Phase(r,1); d.AddContact(r,0,"hands_present",false);
        }
        d.AddNumericOperationRung("Default stopped countdown", LadderNumericOperationKind.Move, "0", "", "remaining_seconds");
        d.AddComparison(6,0,"phase",LadderCompareOperator.GreaterOrEqual,"0");
        var subtract=d.Rungs.Count;
        d.AddNumericOperationRung("Remaining milliseconds", LadderNumericOperationKind.Subtract, "10000", "dryer_timer.ET", "remaining_ms");
        Phase(subtract,1); d.AddContact(subtract,0,"hands_present",false);
        var divide=d.Rungs.Count;
        d.AddNumericOperationRung("Remaining seconds", LadderNumericOperationKind.Divide, "remaining_ms", "1000", "remaining_seconds");
        Phase(divide,1); d.AddContact(divide,0,"hands_present",false);
        d.WatchVariables.AddRange(d.Tags.Select(t=>t.Name)); return d;
    }
    private void AuditHandDryer()
    {
        var failures=0;
        void Check(bool ok,string name) { if(!ok)failures++; GD.Print($"HAND_DRYER_CHECK {name}={ok}"); }
        try { VerifyHandDryerWorkflow(Check); } catch(Exception e) {failures++;GD.PushError(e.ToString());}
        GD.Print($"HAND_DRYER_VERIFY {(failures==0?"PASS":"FAIL")} offline-only");GetTree().Quit(failures==0?0:1);
    }
    private void VerifyHandDryerWorkflow(Action<bool,string> report)
    {
        void Check(bool ok,string name) => report(ok,"hand_dryer_"+name);
        AddMigratedScene("lab-6-08-hand-dryer",_candidateCatalog!,_mainCamera!,false,false);
        var runtime=_sceneRuntime!;
        var root=_sceneCompositionRoot!;
        var heater=root.GetNode<Node3D>("training_accessory_5");
        var enclosure=root.GetNode<Node3D>("training_accessory_4");
        var brackets=ReviewMeshes(enclosure).Where(m=>m.Name.ToString().StartsWith("DRYER_heater_bracket_")).Select(ReviewBounds).ToArray();
        var rails=ReviewMeshes(heater).Where(m=>m.Name.ToString().StartsWith("HEATER_rail_")).Select(ReviewBounds).ToArray();
        Check(brackets.Length==2 && rails.Length==2 && rails.All(rail=>brackets.All(bracket=>
            Math.Abs(rail.Position.Y-bracket.End.Y)<.005 && rail.Intersection(bracket).Size.X>.01f)),"heater_rails_bear_on_two_enclosure_brackets");
        bool On(string n)=>runtime.Points[n] is true;
        double Remaining()=>Convert.ToDouble(runtime.Points["remaining_seconds"]);
        var d=CreateHandDryerReference();var compiled=LadderCompiler.Compile(d.BuildProgram());
        if(!compiled.IsValid)throw new InvalidOperationException(string.Join(";",compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-hand-dryer.rpproj.json"),LadderEditorProjectJson.Save(d));
        EnableVirtualControllerProgram(d.BuildProgram());
        try {
            void Tick(int count=1) {for(var i=0;i<count;i++)_PhysicsProcess(.02);}
            void Hands() {if(!ExecuteSelectedControllerAction("toggle-hands_inserted"))throw new InvalidOperationException("Hands action rejected");}
            RunActiveController(); Tick(10); Check(!On("hands_present")&&!On("blower_run")&&Remaining()==0,"initial_idle");
            Check(!ExecuteSelectedControllerAction("toggle-dryer_timer_active"),"manual_timer_removed");
            Hands();Check(On("hands_present"),"actual_hand_mesh_intersects_sensor_segment");Tick();
            var handsActor=root.GetNode<Node3D>("training_accessory_3/HandPair");
            var handMeshes=ReviewMeshes(handsActor);
            var fixedMeshes=ReviewMeshes(root).Where(m=>!handMeshes.Contains(m)&&m.Name!="DryerAirCommand").ToArray();
            Check(handMeshes.Length==12 && handMeshes.All(m=>fixedMeshes.All(f=> {
                var overlap=ReviewBounds(m).Intersection(ReviewBounds(f)).Size;
                return overlap.X<=.005 || overlap.Y<=.005 || overlap.Z<=.005 || !OrientedBoxesPenetrate(m,f);
            })),"inserted_hands_clear_fixed_enclosure_heater_tray_and_sensor_meshes");
            Check(On("blower_run")&&On("heater_enable")&&Remaining()==10,"start_full_interval");
            Tick(250);Check(Math.Abs(Remaining()-5)<.001&&On("blower_run"),"half_interval_countdown");
            StopActiveController();var scan=_virtualController!.Snapshot.ScanNumber;Tick(20);
            Check(_virtualController.Snapshot.ScanNumber==scan&&!On("blower_run"),"stop_freezes_scans_and_clears_output");
            RunActiveController();Tick();Check(Remaining()==10&&On("blower_run"),"resume_fresh_nonretentive_interval");
            Tick(499);Check(On("blower_run")&&Math.Abs(Remaining()-.02)<.001,"last_active_tick");
            Tick();Check(!On("blower_run")&&!On("heater_enable")&&Remaining()==0,"completion_outputs_off");
            Tick(100);Check(!On("blower_run"),"held_hands_cannot_restart");
            Hands();Tick();Hands();Tick();Check(On("blower_run")&&Remaining()==10,"withdrawal_reinsertion_rearms");
            Tick(25);Hands();Tick();Check(!On("blower_run")&&!On("hands_present")&&Remaining()==0,"early_withdrawal_cancels");
            ResetActiveController();Check(!On("hands_inserted")&&!On("hands_present")&&Remaining()==0&&_virtualController.Snapshot.ScanNumber==0,"reset_restores_idle");
        } finally { DisableVirtualController(); }
        // Malformed PLC images remain visible for diagnosis. The plant refuses
        // illustrative heater activation without its hand/blower permissives.
        runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(true);
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string,bool> { ["heater_enable"] = true });
        runtime.AdvanceSimulation(.02);
        Check(On("heater_enable") && On("heater_inhibited") && !_sceneCompositionRoot!.GetNode<MeshInstance3D>("DryerAirCommand").Visible,"heater_without_hands_and_blower_is_inhibited_not_rewritten");
        runtime.ExecuteAction("toggle-hands_inserted"); runtime.AdvanceSimulation(.02);
        Check(On("hands_present") && On("heater_inhibited"),"hands_alone_cannot_enable_heat");
        runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string,bool> { ["blower_run"] = true, ["heater_enable"] = true });
        runtime.AdvanceSimulation(.02);
        Check(!On("heater_inhibited") && _sceneCompositionRoot!.GetNode<MeshInstance3D>("DryerAirCommand").Visible,"eligible_heat_and_air_projection");
        var hands=_sceneCompositionRoot!.GetNode<Node3D>("training_accessory_3/HandPair");
        var home=hands.Position; hands.Position += new Vector3(0,0,1);
        runtime.AdvanceSimulation(.02);
        Check(On("hands_inserted") && !On("hands_present") && On("heater_inhibited"),"sensor_uses_actual_mesh_pose_not_raw_toggle");
        hands.Position=home; runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(false);
    }
}
