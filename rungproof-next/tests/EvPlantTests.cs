using System;
using RungProof.Next.Scenes;
using Bay = RungProof.Next.Scenes.EvChargingPlantModel.BayInput;

internal static class EvPlantTests
{
    private static int _checks;
    private static void Check(bool value,string name) { if(!value) throw new Exception(name); _checks++;Console.WriteLine("EV_PLANT_CHECK "+name+"=True"); }
    private static bool Near(double a,double b) => Math.Abs(a-b)<1e-9;
    public static int Main()
    {
        var model=new EvChargingPlantModel();
        var enabled=new Bay(true,true,true,true,6);var idle=new Bay(false,false,false,false,0);
        for(var i=0;i<29;i++) model.Advance(.02,enabled,idle);
        Check(model.Current.A.PulseCount==0 && !model.Current.A.EnergyPulse,"no_early_meter_pulse");
        model.Advance(.02,enabled,idle);
        Check(model.Current.A.PulseCount==1 && model.Current.A.EnergyPulse && Near(model.Current.A.EnergyKwh,.001),"six_kw_point_six_seconds_one_watt_hour");
        model.Advance(.02,enabled,idle);
        Check(!model.Current.A.EnergyPulse && model.Current.A.PulseCount==1,"pulse_falls_next_scan");
        model.Pause();var held=model.Current.A.EnergyKwh;
        Check(model.Current.A.DeliveredKw==0 && !model.Current.A.EnergyPulse && model.Current.A.PulseCount==1,"pause_retains_integrated_energy_and_removes_feedback");
        model.Advance(.02,enabled,idle);
        Check(Near(model.Current.A.EnergyKwh,held+6*.02/3600),"resume_adds_only_accepted_tick");
        foreach(var blocked in new[]{enabled with {Occupied=false}, enabled with {Authorized=false},enabled with {Ready=false},enabled with {Connected=false}}) {
            var before=model.Current.A.EnergyKwh;model.Advance(.02,blocked,idle);
            Check(model.Current.A.DeliveredKw==0 && Near(model.Current.A.EnergyKwh,before),"missing_permissive_stops_energy");
        }
        model.Reset();model.Advance(.02,enabled,enabled);
        Check(model.Current.AllocationInhibited && model.Current.A.DeliveredKw==0 && model.Current.B.DeliveredKw==0,"over_budget_inhibits_both_without_selecting_policy");
        model.Advance(.02,enabled with {AllocatedKw=3},enabled with {AllocatedKw=3});
        Check(model.Current.A.DeliveredKw==3 && model.Current.B.DeliveredKw==3 && !model.Current.AllocationInhibited,"independent_three_kw_allocations_share_budget");
        foreach(var value in new[]{-1d,double.NaN,double.PositiveInfinity,6.1}) {
            model.Advance(.02,enabled with {AllocatedKw=value},enabled with {AllocatedKw=0});
            Check(model.Current.AllocationInhibited && model.Current.A.DeliveredKw==0,"invalid_allocation_inhibits");
        }
        foreach(var seconds in new[]{0d,-.02,.04,double.NaN}) {
            var rejected=false;try {model.Advance(seconds,enabled,idle);}catch(ArgumentOutOfRangeException) {rejected=true;}
            Check(rejected,"invalid_tick_rejected");
        }
        model.Reset();
        for(var i=0;i<30000;i++) model.Advance(.02,enabled,idle);
        Check(Near(model.Current.A.EnergyKwh,1) && model.Current.A.PulseCount==1000 && model.Current.B.PulseCount==0,"ten_minutes_energy_and_pulses_agree_no_other_bay_events");
        model.Reset();Check(model.Current==default,"reset_clears_both_integrators_and_feedback");
        Console.WriteLine("EV_PLANT_VERIFY PASS checks="+_checks+" model only; renderer/controller integration pending");return 0;
    }
}
