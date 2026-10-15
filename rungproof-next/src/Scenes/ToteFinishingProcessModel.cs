using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Scene-configured label contact/dwell and inspection acquisition. Inspection
/// checks modeled fill, cap, label and visibility facts; it is not image analysis,
/// barcode decoding, adhesive mechanics or a real quality-control instrument.
/// </summary>
public sealed class ToteFinishingProcessModel
{
    public enum LabelPhase { Home, Extending, Applying, Retracting }
    [Flags] public enum Defect { None = 0, Underfilled = 1, MissingCap = 2, MissingLabel = 4 }
    public readonly record struct Input(bool LabelCommand, bool LabelEligible,
        bool InspectionCommand, bool InspectionEligible, bool FillComplete, bool CapApplied);
    public readonly record struct Snapshot(LabelPhase LabelStage, double Extension,
        bool LabelApplied, bool LabelMoving, bool LabelInhibited, bool InspectionBusy,
        bool InspectionDone, bool InspectionOk, bool InspectionInhibited, Defect Defects);

    private readonly double _travelSeconds, _contactSeconds, _inspectionSeconds;
    private double _contactElapsed, _inspectionElapsed;
    private bool _previousInspectionCommand;
    public Snapshot State { get; private set; }

    public ToteFinishingProcessModel(double travelSeconds = .35, double contactSeconds = .4,
        double inspectionSeconds = .6)
    {
        if (!double.IsFinite(travelSeconds) || travelSeconds <= 0
            || !double.IsFinite(contactSeconds) || contactSeconds <= 0
            || !double.IsFinite(inspectionSeconds) || inspectionSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(travelSeconds));
        (_travelSeconds, _contactSeconds, _inspectionSeconds) = (travelSeconds, contactSeconds, inspectionSeconds);
        Reset();
    }

    public void Advance(double seconds, Input input)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        var stage = State.LabelStage;
        var extension = State.Extension;
        var applied = State.LabelApplied;
        var moving = false;
        if ((stage is LabelPhase.Extending or LabelPhase.Applying) && (!input.LabelCommand || !input.LabelEligible))
        {
            stage = LabelPhase.Retracting;
            _contactElapsed = 0;
        }
        if (stage == LabelPhase.Home && input.LabelCommand && input.LabelEligible && !applied)
            stage = LabelPhase.Extending;
        switch (stage)
        {
            case LabelPhase.Extending:
                extension = Math.Min(1, extension + seconds / _travelSeconds);
                moving = true;
                if (extension >= 1 - 1e-12) { extension = 1; stage = LabelPhase.Applying; _contactElapsed = 0; }
                break;
            case LabelPhase.Applying:
                _contactElapsed += seconds;
                if (_contactElapsed + 1e-12 >= _contactSeconds) { applied = true; stage = LabelPhase.Retracting; }
                break;
            case LabelPhase.Retracting:
                extension = Math.Max(0, extension - seconds / _travelSeconds);
                moving = true;
                if (extension <= 1e-12) { extension = 0; stage = LabelPhase.Home; moving = false; }
                break;
        }

        var busy = State.InspectionBusy;
        var done = State.InspectionDone;
        var ok = State.InspectionOk;
        var defects = State.Defects;
        // A held command cannot repeatedly inspect or replace a retained result.
        // A new request clears the old result; only accepted stationary/viewable
        // acquisition time can complete a replacement result.
        if (input.InspectionCommand && !_previousInspectionCommand)
        {
            busy = input.InspectionEligible;
            done = false; ok = false; defects = Defect.None; _inspectionElapsed = 0;
        }
        if (busy && (!input.InspectionCommand || !input.InspectionEligible))
        {
            busy = false;
            _inspectionElapsed = 0;
        }
        if (busy)
        {
            _inspectionElapsed += seconds;
            if (_inspectionElapsed + 1e-12 >= _inspectionSeconds)
            {
                defects = (input.FillComplete ? Defect.None : Defect.Underfilled)
                    | (input.CapApplied ? Defect.None : Defect.MissingCap)
                    | (applied ? Defect.None : Defect.MissingLabel);
                done = true; ok = defects == Defect.None; busy = false;
            }
        }
        _previousInspectionCommand = input.InspectionCommand;
        State = new Snapshot(stage, extension, applied, moving,
            input.LabelCommand && (!input.LabelEligible || applied), busy, done, ok,
            input.InspectionCommand && !input.InspectionEligible, defects);
    }

    // Stop freezes progress and pose. The caller must not advance while paused;
    // the accepted acquisition may resume without fabricating a new rising edge.
    public void Pause() => State = State with { LabelMoving = false };
    public void Reset()
    {
        _contactElapsed = 0; _inspectionElapsed = 0; _previousInspectionCommand = false;
        State = new Snapshot(LabelPhase.Home, 0, false, false, false, false, false, false, false, Defect.None);
    }
}
