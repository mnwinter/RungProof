using System;
using RungProof.Next.Scenes;

internal static class ToteCapTests
{
    private static int _checks;
    private static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        Console.WriteLine("TOTE_CAP_MODEL_CHECK " + name + "=True"); _checks++;
    }
    public static void Main()
    {
        var model = new ToteCapPlantModel(.2, .1);
        for (var i=0;i<30;i++) model.Advance(.02,true,false);
        Check(model.State.Extension==0 && !model.State.Applied && model.State.Inhibited,"off_station_does_not_apply");
        for (var i=0;i<5;i++) model.Advance(.02,true,true);
        var halfway=model.State.Extension;
        model.Pause();
        Check(Math.Abs(halfway-.5)<1e-9 && model.State.Extension==halfway && !model.State.Moving,"stop_holds_partial_stroke");
        model.Advance(.02,false,true);
        Check(model.State.Extension<halfway && !model.State.Applied,"withdrawal_retracts_without_release");
        for(var i=0;i<20;i++) model.Advance(.02,false,true);
        Check(model.State.Stage==ToteCapPlantModel.Phase.Home && model.State.Extension==0,"interrupted_attempt_returns_home");
        model.Reset();
        for(var i=0;i<10;i++) model.Advance(.02,true,true);
        Check(!model.State.Applied && model.State.Stage==ToteCapPlantModel.Phase.Seating,"arrival_is_not_completion");
        model.Advance(.02,true,false);
        Check(!model.State.Applied && model.State.Stage==ToteCapPlantModel.Phase.Retracting,"eligibility_loss_before_release_aborts");
        model.Reset();
        for(var i=0;i<40;i++) model.Advance(.02,true,true);
        Check(model.State.Applied && model.State.Extension==0 && model.State.Stage==ToteCapPlantModel.Phase.Home,"accepted_cycle_applies_and_returns_home");
        for(var i=0;i<100;i++) model.Advance(.02,true,true);
        Check(model.State.Applied && model.State.Extension==0,"held_command_does_not_repeat");
        model.Advance(.02,false,false);model.Pause();
        Check(model.State.Applied,"withdrawal_and_stop_retain_applied_cap");
        foreach(var dt in new[]{0d,-.02,.04,double.NaN,double.PositiveInfinity})
        {
            var before=model.State;var rejected=false;
            try {model.Advance(dt,true,true);} catch(ArgumentOutOfRangeException){rejected=true;}
            Check(rejected && model.State==before,"invalid_tick_does_not_change_state");
        }
        model.Reset();
        Check(!model.State.Applied && model.State.Extension==0 && !model.State.Inhibited,"reset_restores_empty_home");
        var spinning=new ToteCapPlantModel(.2,.1,3);
        for(var i=0;i<10;i++) spinning.Advance(.02,true,true);
        Check(spinning.State.Turns==0,"descent_does_not_spin");
        spinning.Advance(.02,true,true);var turns=spinning.State.Turns;
        Check(Math.Abs(turns-.06)<1e-9,"seated_tick_integrates_declared_speed");
        spinning.Pause();Check(spinning.State.Turns==turns,"stop_holds_seated_angle");
        for(var i=0;i<30;i++) spinning.Advance(.02,true,true);
        Check(Math.Abs(spinning.State.Turns-.3)<1e-9 && spinning.State.Applied,"rotation_limited_to_accepted_seated_dwell");
        spinning.Reset();Check(spinning.State.Turns==0,"reset_restores_authored_angle");
        Console.WriteLine("TOTE_CAP_MODEL_PASS " + _checks);
    }
}
