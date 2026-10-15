using System;
using RungProof.Next.Scenes;

internal static class WastewaterPlantChecks
{
    public static int Main()
    {
        var checks = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new InvalidOperationException(name);
            checks++;
            Console.WriteLine($"WASTEWATER_MODEL_CHECK {name}=PASS");
        }
        bool Near(double a, double b) => Math.Abs(a - b) < 1e-8;
        var model = new WastewaterPlantModel();
        Check(Near(model.State.Level, .35) && Near(model.State.TransmitterMa, 9.6) && !model.State.High, "reset_level_and_analog");
        for (var i = 0; i < 100; i++) model.Advance(.02, false, true, 0, true, true);
        Check(Near(model.State.Level, .35) && model.State.TransferInhibited, "closed_valve_blocks_bad_pump_command");
        for (var i = 0; i < 100; i++) model.Advance(.02, false, true, 1, false, true);
        Check(Near(model.State.Level, .35) && model.State.TransferInhibited, "treatment_loss_blocks_transfer");
        for (var i = 0; i < 100; i++) model.Advance(.02, false, true, 1, true, false);
        Check(Near(model.State.Level, .35) && model.State.TransferInhibited, "outlet_loss_blocks_transfer");
        for (var i = 0; i < 376; i++) model.Advance(.02, true, false, 0, true, true);
        Check(model.State.Level >= .65 && model.State.High && Near(model.State.Inflow, .04), "inflow_reaches_high_threshold");
        model.Advance(.02, false, true, .5, true, true);
        Check(Near(model.State.TransferFlow, .04), "half_open_delivered_valve_halves_illustrative_flow");
        while (model.State.Level > .3) model.Advance(.02, false, true, 1, true, true);
        Check(model.State.High, "high_feedback_remains_latched_in_deadband");
        while (model.State.Level > .2) model.Advance(.02, false, true, 1, true, true);
        Check(!model.State.High, "high_feedback_clears_at_low_threshold");
        for (var i = 0; i < 1000; i++) model.Advance(.02, false, true, 1, true, true);
        Check(model.State.Empty && Near(model.State.TransmitterMa, 4) && model.State.TransferFlow == 0, "empty_clamp_and_zero_flow");
        model.Reset();
        var before = model.State.Level;
        model.Advance(.02, true, true, 1, true, true);
        Check(Near(model.State.Level - before, (.04 - .08) * .02), "simultaneous_mass_balance");
        model.Pause();
        Check(model.State.TransferFlow == 0 && model.State.Inflow == 0 && Near(model.State.Level, before - .0008), "pause_retains_quantity_and_zeroes_rates");
        for (var i = 0; i < 2000; i++) model.Advance(.02, true, false, 0, true, true);
        Check(model.State.Full && Near(model.State.TransmitterMa, 20) && Near(model.State.Overflow, .04), "full_clamp_and_rejected_inflow_overflow");
        model.Reset();
        Check(Near(model.State.Level, .35) && !model.State.High && model.State.Overflow == 0, "reset_restores_initial_state");
        foreach (var seconds in new[] { 0.0, -.02, .021, double.NaN, double.PositiveInfinity })
        {
            var rejected = false;
            try { model.Advance(seconds, true, false, 0, true, true); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "reject_invalid_tick_" + seconds);
        }
        foreach (var valve in new[] { -.01, 1.01, double.NaN })
        {
            var rejected = false;
            try { model.Advance(.02, true, false, valve, true, true); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "reject_invalid_valve_" + valve);
        }
        Console.WriteLine($"WASTEWATER_MODEL_RESULT PASS checks={checks}");
        return 0;
    }
}
