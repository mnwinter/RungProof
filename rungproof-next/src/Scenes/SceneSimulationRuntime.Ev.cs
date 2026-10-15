using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Godot;

namespace RungProof.Next.Scenes;

public partial class SceneSimulationRuntime
{
    private bool HasEvPlant => RuntimeType == "evCharging";
    private EvChargingPlantModel? _evPlant;
    private Node3D[] _evBays=[], _evPlugs=[];
    private MeshInstance3D[][] _evVehicles=[];
    private MeshInstance3D[] _evInlets=[], _evNoses=[], _evPulseLeds=[];
    private static string EvPoint(int index,string a,string b) => index==0?a:b;
    private bool EvActionAvailable(JsonElement action)
    {
        if(!HasEvPlant)return true;
        var point=Text(action,"point","");
        for(var i=0;i<2;i++) {
            var occupied=EvPoint(i,"bay_occupied","bay_b_occupied");
            var inserted=EvPoint(i,"connector_inserted","bay_b_connector_inserted");
            if(point==occupied && AsBool(_points[occupied]) && AsBool(_points[inserted]))return false;
            if(point==inserted) {
                if(!AsBool(_points[inserted]) && !AsBool(_points[occupied]))return false;
                if(AsBool(_points[EvPoint(i,"charge_enable","bay_b_charge_enable")])
                    || Convert.ToDouble(_points[EvPoint(i,"allocated_kw","bay_b_allocated_kw")],CultureInfo.InvariantCulture)!=0)return false;
            }
        }
        return true;
    }
    private void ResetEvPlant()
    {
        if(!HasEvPlant)return;
        _evPlant=new EvChargingPlantModel();
        _evBays=new[]{_sceneRoot.GetNode<Node3D>("training_accessory_3"),_sceneRoot.GetNode<Node3D>("ev_bay_b")};
        _evPlugs=new[]{_sceneRoot.GetNode<Node3D>("training_accessory_4"),_sceneRoot.GetNode<Node3D>("ev_connector_b")};
        _evVehicles=_evBays.Select(b=>b.FindChildren("*","MeshInstance3D",true,false).OfType<MeshInstance3D>()
            .Where(m=>m.Name.ToString().StartsWith("EV_")&&!m.Name.ToString().StartsWith("EV_BAY_")).ToArray()).ToArray();
        _evInlets=_evBays.Select(b=>(MeshInstance3D)b.FindChild("EV_INLET",true,false)).ToArray();
        _evNoses=_evPlugs.Select(b=>(MeshInstance3D)b.FindChild("CONNECTOR_nose",true,false)).ToArray();
        _evPulseLeds=new[]{"training_accessory_6","ev_pulse_b"}.Select(id=>(MeshInstance3D)_sceneRoot.GetNode<Node3D>(id).FindChild("PULSE_LED",true,false)).ToArray();
        ProjectEvGeometry();PublishEvSnapshot();
    }
    private void ProjectEvGeometry()
    {
        if(_evPlant is null)return;
        for(var i=0;i<2;i++) {
            var occupied=AsBool(_points[EvPoint(i,"bay_occupied","bay_b_occupied")]);
            foreach(var part in _evVehicles[i])part.Visible=occupied;
            _evPlugs[i].Visible=occupied && AsBool(_points[EvPoint(i,"connector_inserted","bay_b_connector_inserted")]);
            // Feedback requires the actual installed nose/socket geometry, as
            // well as the fixture's occupied/inserted pose; no fake PLC echo.
            var nose=_evNoses[i].GlobalTransform*_evNoses[i].GetAabb();
            var inlet=_evInlets[i].GlobalTransform*_evInlets[i].GetAabb();
            SetPoint(EvPoint(i,"connector_connected","bay_b_connector_connected"),occupied && _evPlugs[i].Visible
                && nose.Intersects(inlet) && Math.Abs(nose.GetCenter().X-inlet.GetCenter().X)<.003 && Math.Abs(nose.GetCenter().Y-inlet.GetCenter().Y)<.003);
        }
    }
    private EvChargingPlantModel.BayInput EvInput(int i) => new(
        AsBool(_points[EvPoint(i,"bay_occupied","bay_b_occupied")]),
        AsBool(_points[EvPoint(i,"customer_authorized","bay_b_customer_authorized")]),
        AsBool(_points[EvPoint(i,"charger_ready","bay_b_charger_ready")]),
        AsBool(_points[EvPoint(i,"connector_connected","bay_b_connector_connected")]),
        AsBool(_points[EvPoint(i,"charge_enable","bay_b_charge_enable")]) ? Convert.ToDouble(_points[EvPoint(i,"allocated_kw","bay_b_allocated_kw")],CultureInfo.InvariantCulture) : 0);
    private void AdvanceEvPlant(double seconds)
    {
        ProjectEvGeometry();_evPlant!.Advance(seconds,EvInput(0),EvInput(1));PublishEvSnapshot();ApplyBindings();StateChanged?.Invoke();
    }
    private void PauseEvPlant()
    {
        if(_evPlant is null)return;
        _evPlant.Pause();PublishEvSnapshot();ApplyBindings();StateChanged?.Invoke();
    }
    private void PublishEvSnapshot()
    {
        var snapshot=_evPlant!.Current;
        for(var i=0;i<2;i++) {
            var bay=i==0?snapshot.A:snapshot.B;
            SetPoint(EvPoint(i,"delivered_kw","bay_b_delivered_kw"),bay.DeliveredKw);
            SetPoint(EvPoint(i,"meter_energy_kwh","bay_b_meter_energy_kwh"),bay.EnergyKwh);
            SetPoint(EvPoint(i,"meter_pulse_count","bay_b_meter_pulse_count"),(long)checked((int)bay.PulseCount));
            SetPoint(EvPoint(i,"energy_pulse","bay_b_energy_pulse"),bay.EnergyPulse);
            _evPulseLeds[i].Visible=bay.EnergyPulse;
        }
        SetPoint("allocation_inhibited",snapshot.AllocationInhibited);
    }
}
