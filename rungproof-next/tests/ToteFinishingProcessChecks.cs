using System;
using RungProof.Next.Scenes;

internal static class ToteFinishingProcessChecks
{
    public static int Main()
    {
        var checks = 0;
        void Check(bool ok, string name)
        {
            if (!ok) throw new InvalidOperationException(name);
            checks++; Console.WriteLine($"TOTE_FINISHING_MODEL_CHECK {name}=PASS");
        }
        var model = new ToteFinishingProcessModel();
        var label = new ToteFinishingProcessModel.Input(true, true, false, false, true, true);
        void Tick(int count, ToteFinishingProcessModel.Input input)
        { for (var i = 0; i < count; i++) model.Advance(.02, input); }
        Check(!model.State.LabelApplied && !model.State.InspectionDone && model.State.Extension == 0, "initial_no_label_or_result");
        Tick(100, label with { LabelEligible = false });
        Check(!model.State.LabelApplied && model.State.Extension == 0 && model.State.LabelInhibited, "off_station_command_cannot_apply_label");
        Tick(10, label);
        Check(model.State.Extension > 0 && model.State.Extension < 1 && !model.State.LabelApplied, "pad_extends_without_early_label");
        var extension = model.State.Extension; model.Pause();
        Check(model.State.Extension == extension && !model.State.LabelMoving && !model.State.LabelApplied, "pause_retains_partial_stroke");
        Tick(30, label with { LabelEligible = false });
        Check(model.State.Extension == 0 && !model.State.LabelApplied, "lost_eligibility_retracts_without_label");
        Tick(19, label);
        Check(model.State.Extension == 1 && !model.State.LabelApplied, "full_contact_requires_dwell");
        Tick(5, label with { LabelCommand = false });
        Check(!model.State.LabelApplied && model.State.Extension < 1, "command_loss_aborts_contact_dwell");
        Tick(100, label with { LabelCommand = false });
        Tick(60, label);
        Check(model.State.LabelApplied && model.State.Extension == 0 && model.State.LabelStage == ToteFinishingProcessModel.LabelPhase.Home,
            "accepted_contact_dwell_applies_label_then_returns_home");
        Tick(100, label);
        Check(model.State.Extension == 0 && model.State.LabelApplied, "held_label_command_does_not_reapply");
        var inspect = label with { LabelCommand = false, LabelEligible = false, InspectionCommand = true, InspectionEligible = true };
        Tick(10, inspect);
        Check(model.State.InspectionBusy && !model.State.InspectionDone && !model.State.InspectionOk, "inspection_requires_acquisition_time");
        Tick(5, inspect with { InspectionEligible = false });
        Check(!model.State.InspectionBusy && !model.State.InspectionDone && model.State.InspectionInhibited, "visibility_loss_aborts_acquisition");
        Tick(100, inspect);
        Check(!model.State.InspectionDone, "held_request_does_not_restart_aborted_acquisition");
        Tick(1, inspect with { InspectionCommand = false });
        Tick(30, inspect);
        Check(model.State.InspectionDone && model.State.InspectionOk && model.State.Defects == ToteFinishingProcessModel.Defect.None,
            "fresh_complete_fixture_passes_modeled_inspection");
        Tick(40, inspect with { FillComplete = false, CapApplied = false });
        Check(model.State.InspectionOk, "held_request_retains_acquired_result");
        Tick(1, inspect with { InspectionCommand = false });
        Tick(30, inspect with { FillComplete = false, CapApplied = false });
        Check(model.State.InspectionDone && !model.State.InspectionOk
            && model.State.Defects == (ToteFinishingProcessModel.Defect.Underfilled | ToteFinishingProcessModel.Defect.MissingCap),
            "new_request_measures_underfill_and_missing_cap_defects");
        model.Reset();
        Tick(30, inspect with { FillComplete = true, CapApplied = true });
        Check(model.State.InspectionDone && !model.State.InspectionOk && model.State.Defects == ToteFinishingProcessModel.Defect.MissingLabel,
            "missing_label_does_not_pass_inspection");
        model.Reset();
        Check(!model.State.LabelApplied && !model.State.InspectionDone && !model.State.InspectionOk, "reset_clears_mark_and_results");
        foreach (var dt in new[] { 0.0, -.01, .021, double.NaN })
        {
            var rejected = false;
            try { model.Advance(dt, label); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "reject_invalid_tick_" + dt);
        }
        Console.WriteLine($"TOTE_FINISHING_MODEL_RESULT PASS checks={checks}");
        return 0;
    }
}
