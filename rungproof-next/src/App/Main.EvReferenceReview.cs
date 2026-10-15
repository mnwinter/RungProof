using System;
using System.Linq;
using Godot;
using RungProof.Next.VirtualController;

namespace RungProof.Next.App;

public partial class Main
{
    private static LadderEditorDocument CreateEvReference()
    {
        var d=new LadderEditorDocument();
        d.ResetProject("review-ev-energy","EV_Allocation_Reference",TimeSpan.FromMilliseconds(20));
        d.SourceSceneId="lab-9-12-ev-charging-manager";
        d.AddTag("reference_enabled",PlcVariableRole.Memory,initialValue:true);
        foreach(var prefix in new[]{"","bay_b_"}) {
            var occupied=prefix.Length==0?"bay_occupied":prefix+"occupied";
            foreach(var n in new[]{occupied,prefix+"customer_authorized",prefix+"charger_ready",prefix+"connector_inserted",prefix+"connector_connected",prefix+"energy_pulse"})d.AddTag(n,PlcVariableRole.Input,n);
            foreach(var n in new[]{prefix+"delivered_kw",prefix+"meter_energy_kwh"})d.AddTag(n,PlcVariableRole.Input,n,type:PlcVariableType.Real);
            d.AddTag(prefix+"meter_pulse_count",PlcVariableRole.Input,prefix+"meter_pulse_count",type:PlcVariableType.DInt);
            foreach(var n in new[]{prefix+"charge_enable",prefix+"energy_session_active"})d.AddTag(n,PlcVariableRole.Output,n);
            foreach(var n in new[]{prefix+"allocated_kw",prefix+"energy_kwh"})d.AddTag(n,PlcVariableRole.Output,n,type:PlcVariableType.Real);
            d.AddTag(prefix+"energy_count",PlcVariableRole.Output,prefix+"energy_count",type:PlcVariableType.DInt);
            d.AddTag(prefix+"eligible",PlcVariableRole.Memory);
            foreach(var n in new[]{"seen_pulses","retained_count","new_pulses"})d.AddTag(prefix+n,PlcVariableRole.Memory,type:PlcVariableType.DInt);
        }
        d.AddTag("allocation_inhibited",PlcVariableRole.Input,"allocation_inhibited");
        // Illustrative policy: a single eligible bay gets 6 kW; two share 3+3.
        // This is explicit ladder policy, not arbitration hidden in the plant.
        foreach(var prefix in new[]{"","bay_b_"}) {
            var r=d.Rungs.Count;d.AddRung("Eligible occupied, authorized, ready, connected bay",prefix+"eligible");
            foreach(var n in new[]{prefix.Length==0?"bay_occupied":prefix+"occupied",prefix+"customer_authorized",prefix+"charger_ready",prefix+"connector_connected"})d.AddContact(r,0,n,false);
        }
        foreach(var prefix in new[]{"","bay_b_"}) {
            var other=prefix.Length==0?"bay_b_":"";
            foreach(var command in new[]{"charge_enable","energy_session_active"}) {
                var r=d.Rungs.Count;d.AddRung("Eligible bay command",prefix+command);d.AddContact(r,0,prefix+"eligible",false);
            }
            var clear=d.Rungs.Count;d.AddNumericOperationRung("Clear allocation before selecting policy",LadderNumericOperationKind.Move,"0","",prefix+"allocated_kw");d.AddContact(clear,0,"reference_enabled",false);
            foreach(var sharing in new[]{false,true}) {
                var r=d.Rungs.Count;d.AddNumericOperationRung(sharing?"Two eligible bays share three kW each":"One eligible bay receives six kW",LadderNumericOperationKind.Move,sharing?"3":"6","",prefix+"allocated_kw");
                d.AddContact(r,0,prefix+"eligible",false);d.AddContact(r,0,other+"eligible",!sharing);
            }
            // A monotonic meter event ledger prevents losing an emitted pulse
            // when Stop occurs between plant publication and the next PLC scan.
            // The PLC owns its acknowledged event count and output energy.
            var delta=d.Rungs.Count;d.AddNumericOperationRung("Unacknowledged meter events",LadderNumericOperationKind.Subtract,prefix+"meter_pulse_count",prefix+"seen_pulses",prefix+"new_pulses");d.AddContact(delta,0,"reference_enabled",false);
            var accumulate=d.Rungs.Count;d.AddNumericOperationRung("Accumulate each unacknowledged event once",LadderNumericOperationKind.Add,prefix+"retained_count",prefix+"new_pulses",prefix+"retained_count");d.AddComparison(accumulate,0,prefix+"new_pulses",LadderCompareOperator.GreaterThan,"0");
            var ack=d.Rungs.Count;d.AddNumericOperationRung("Acknowledge sampled meter ledger",LadderNumericOperationKind.Move,prefix+"meter_pulse_count","",prefix+"seen_pulses");d.AddContact(ack,0,"reference_enabled",false);
            var publish=d.Rungs.Count;d.AddNumericOperationRung("Publish retained PLC event count",LadderNumericOperationKind.Move,prefix+"retained_count","",prefix+"energy_count");d.AddContact(publish,0,"reference_enabled",false);
            var scale=d.Rungs.Count;d.AddNumericOperationRung("One thousand pulses per illustrative kWh",LadderNumericOperationKind.Divide,prefix+"retained_count","1000",prefix+"energy_kwh");d.AddContact(scale,0,"reference_enabled",false);
        }
        d.WatchVariables.AddRange(d.Tags.Select(t=>t.Name));return d;
    }
    private void AuditEvReference(Action<bool,string> check)
    {
        var document=CreateEvReference();var compiled=LadderCompiler.Compile(document.BuildProgram());
        if(!compiled.IsValid)throw new InvalidOperationException(string.Join(";",compiled.Issues));
        System.IO.File.WriteAllText(ProjectSettings.GlobalizePath("res://.tools/plant-review-ev.rpproj.json"),LadderEditorProjectJson.Save(document));
        _sceneRuntime!.ResetSimulation();EnableVirtualControllerProgram(document.BuildProgram());
        try {
            var runtime=_sceneRuntime!;
            double N(string n)=>Convert.ToDouble(runtime.Points[n]);
            bool On(string n)=>runtime.Points[n] is true;
            void Tick(int count=1) {for(var i=0;i<count;i++)_PhysicsProcess(.02);}
            RunActiveController();Tick(10);
            check(N("delivered_kw")==0 && N("bay_b_delivered_kw")==0,"reference_empty_bays_do_not_charge");
            foreach(var action in new[]{"toggle-bay_occupied","toggle-connector_inserted","toggle-customer_authorized","toggle-charger_ready","toggle-bay_b_occupied","toggle-bay_b_connector_inserted","toggle-bay_b_customer_authorized","toggle-bay_b_charger_ready"})
                check(ExecuteSelectedControllerAction(action),"reference_fixture_"+action);
            Tick(61);
            check(N("allocated_kw")==3 && N("bay_b_allocated_kw")==3 && N("delivered_kw")==3 && N("bay_b_delivered_kw")==3,"reference_two_bays_share_six_kw");
            check(N("energy_count")==1 && N("bay_b_energy_count")==1 && Math.Abs(N("energy_kwh")-.001)<1e-9 && Math.Abs(N("bay_b_energy_kwh")-.001)<1e-9,"reference_independent_meter_events_and_scaled_energy");
            check(new[]{"training_accessory_5","ev_meter_b"}.All(id=>_sceneCompositionRoot!.GetNode<Node3D>(id).GetNode<Label3D>("NumericReadout").Text=="kWh\n0.001"),"reference_both_readouts_publish_plc_energy");
            check(new[]{"toggle-bay_occupied","toggle-connector_inserted","toggle-customer_authorized","toggle-charger_ready","toggle-bay_b_occupied","toggle-bay_b_connector_inserted","toggle-bay_b_customer_authorized","toggle-bay_b_charger_ready"}.All(id=>_sceneControlInteractor!.Bindings.Any(b=>b.ActionId==id)),"reference_eight_three_dimensional_controls_wired");
            ExecuteSelectedControllerAction("toggle-bay_b_charger_ready");Tick(31);
            check(N("allocated_kw")==6 && N("bay_b_allocated_kw")==0 && N("bay_b_delivered_kw")==0 && N("bay_b_energy_count")==1,"reference_readiness_loss_reallocates_without_other_bay_count");
            for(var i=0;i<100 && !On("energy_pulse");i++)Tick();
            var ledger=N("meter_pulse_count");var energy=N("meter_energy_kwh");
            check(On("energy_pulse"),"reference_can_stop_at_published_pulse_boundary");
            StopActiveController();Tick(30);
            check(N("meter_energy_kwh")==energy && N("delivered_kw")==0 && !On("energy_pulse"),"reference_stop_retains_meter_without_more_energy");
            RunActiveController();Tick();
            check(N("energy_count")==ledger && N("bay_b_energy_count")==1,"reference_resume_acknowledges_pending_pulse_once");
            Tick();check(N("energy_count")==ledger,"reference_retained_ledger_cannot_double_count");
            ResetActiveController();check(N("energy_count")==0 && N("meter_pulse_count")==0 && N("bay_b_energy_count")==0,"reference_reset_clears_plc_and_meter_ledgers");
        } finally {DisableVirtualController();}
    }
}
