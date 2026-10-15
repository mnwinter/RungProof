using System;
using System.Linq;
using Godot;

namespace RungProof.Next.App;

public partial class Main
{
    private bool _auditEvLayout;
    private void AuditEvLayout()
    {
        var failures=0;
        void Check(bool ok,string name) { if(!ok)failures++; GD.Print($"EV_LAYOUT_CHECK {name}={ok}"); }
        try {
            AddMigratedScene("lab-9-12-ev-charging-manager",_candidateCatalog!,_mainCamera!,false,false);
            var root=_sceneCompositionRoot!;
            foreach(var ids in new[]{new[]{"training_accessory_3","training_accessory_4","training_accessory_5","training_accessory_6","training_accessory_7"},new[]{"ev_bay_b","ev_connector_b","ev_meter_b","ev_pulse_b","ev_reader_b"}}) {
                var bay=root.GetNode<Node3D>(ids[0]);
                MeshInstance3D Mesh(Node3D n,string name)=>(MeshInstance3D)n.FindChild(name,true,false);
                var pad=ReviewBounds(Mesh(bay,"EV_BAY_ground_pad"));
                var tires=bay.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>().Where(m=>m.Name.ToString().StartsWith("EV_tire_")).ToArray();
                Check(tires.Length==4 && tires.All(t=>Math.Abs(ReviewBounds(t).Position.Y-pad.End.Y)<.003),ids[0]+"_four_tires_bear_on_pad");
                var pedestal=ReviewBounds(Mesh(bay,"EVSE_base"));
                Check(Math.Abs(pedestal.Position.Y-pad.End.Y)<.003,ids[0]+"_charger_base_bears_on_pad");
                var plug=root.GetNode<Node3D>(ids[1]);
                var nose=ReviewBounds(Mesh(plug,"CONNECTOR_nose"));
                var inlet=ReviewBounds(Mesh(bay,"EV_INLET"));
                Check(Math.Abs(nose.GetCenter().X-inlet.GetCenter().X)<.003 && Math.Abs(nose.GetCenter().Y-inlet.GetCenter().Y)<.003 && nose.Intersects(inlet),ids[0]+"_plug_nose_engages_inlet");
                var cable=ReviewBounds(Mesh(bay,"EVSE_CABLE_4"));
                var gland=ReviewBounds(Mesh(plug,"CONNECTOR_cable_gland"));
                Check(cable.Intersects(gland),ids[0]+"_cable_reaches_plug_gland");
                var meter=root.GetNode<Node3D>(ids[2]);
                var meterBase=ReviewBounds(Mesh(meter,"METER_base"));
                Check(Math.Abs(meterBase.Position.Y)<.003,ids[0]+"_meter_grounded");
                var pulse=root.GetNode<Node3D>(ids[3]);
                var module=ReviewBounds(Mesh(pulse,"PULSE_module"));
                var housing=ReviewBounds(Mesh(meter,"METER_enclosure"));
                Check(Math.Abs(module.Position.Z-housing.End.Z)<.003 && module.Position.Y>=housing.Position.Y && module.End.Y<=housing.End.Y,ids[0]+"_pulse_module_backs_onto_meter");
                Check(Math.Abs(ReviewBounds(Mesh(root.GetNode<Node3D>(ids[4]),"READER_base")).Position.Y)<.003,ids[0]+"_reader_grounded");
            }
            Check(!ReviewMeshes(root).Any(m=>m.Name.ToString().StartsWith("KIN_slat_") || m.Name.ToString().StartsWith("PROCESS_flange")),"no_inherited_shutter_or_fluid_identity");
            Check(!root.HasNode("machine_0"),"unrelated_cabinet_removed");
            var runtime=_sceneRuntime!;
            runtime.UsesExternalClock=true;runtime.SetControllerPlaybackRunning(true);
            bool On(string n)=>Convert.ToBoolean(runtime.Points[n]);
            double Number(string n)=>Convert.ToDouble(runtime.Points[n]);
            void Tick(int count=1) {for(var i=0;i<count;i++)runtime.AdvanceSimulation(.02);}
            Check(!On("connector_connected") && !On("bay_b_connector_connected"),"empty_bays_have_no_connector_feedback");
            Check(!runtime.ExecuteAction("toggle-connector_inserted"),"cannot_insert_without_vehicle");
            runtime.ExecuteAction("toggle-bay_occupied");runtime.ExecuteAction("toggle-connector_inserted");
            runtime.ExecuteAction("toggle-customer_authorized");runtime.ExecuteAction("toggle-charger_ready");
            Check(On("connector_connected"),"occupied_inserted_actual_meshes_report_connected");
            Check(!runtime.ExecuteAction("toggle-bay_occupied"),"cannot_remove_connected_vehicle");
            runtime.CommitVirtualControllerOutputs(new System.Collections.Generic.Dictionary<string,bool>{{"charge_enable",true},{"bay_b_charge_enable",false}});
            runtime.CommitVirtualControllerNumericOutputs(new System.Collections.Generic.Dictionary<string,double>{{"allocated_kw",6},{"bay_b_allocated_kw",0}});
            Tick(30);
            Check(Number("delivered_kw")==6 && Number("meter_pulse_count")==1 && On("energy_pulse"),"accepted_ticks_generate_actual_fixture_meter_pulse");
            Check(!runtime.ExecuteAction("toggle-connector_inserted"),"connector_changes_rejected_with_charge_command");
            var energy=Number("meter_energy_kwh");runtime.SetControllerPlaybackRunning(false);Tick(30);
            Check(Number("meter_energy_kwh")==energy && !On("energy_pulse") && Number("delivered_kw")==0,"stopped_clock_retains_energy_without_pulse_or_delivery");
            runtime.SetControllerPlaybackRunning(true);Tick();
            Check(Number("meter_energy_kwh")>energy && Number("meter_pulse_count")==1,"resumed_tick_retains_meter_count");
            runtime.ExecuteAction("toggle-customer_authorized");Tick();
            Check(Number("delivered_kw")==0,"authorization_loss_stops_energy_delivery");
            runtime.ResetSimulation();runtime.SetControllerPlaybackRunning(false);
            Check(Number("meter_energy_kwh")==0 && Number("meter_pulse_count")==0 && !On("connector_connected"),"reset_clears_meter_and_fixture_state");
            AuditEvReference(Check);
        } catch(Exception ex) { failures++;GD.PushError(ex.ToString()); }
        GD.Print($"EV_LAYOUT_VERIFY {(failures==0?"PASS":"FAIL")} geometry, offline adapter and PLC reference; native runtime pending");
        GetTree().Quit(failures==0?0:1);
    }
}
