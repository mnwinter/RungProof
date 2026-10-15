using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Illustrative axial cap cycle. Timing is supplied by the scene; this does
/// not model threads, torque, sealing or real cap-feed machinery.
/// </summary>
public sealed class ToteCapPlantModel
{
    public enum Phase { Home, Lowering, Seating, Retracting }
    public readonly record struct Snapshot(Phase Stage, double Extension, bool Applied,
        bool Moving, bool Inhibited, double Turns);
    private readonly double _travelSeconds, _seatingSeconds, _turnsPerSecond;
    private double _seatElapsed;
    public Snapshot State { get; private set; }

    public ToteCapPlantModel(double travelSeconds, double seatingSeconds, double seatingTurnsPerSecond = 0)
    {
        if (!double.IsFinite(travelSeconds) || travelSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(travelSeconds));
        if (!double.IsFinite(seatingSeconds) || seatingSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(seatingSeconds));
        if (!double.IsFinite(seatingTurnsPerSecond) || seatingTurnsPerSecond < 0)
            throw new ArgumentOutOfRangeException(nameof(seatingTurnsPerSecond));
        _travelSeconds = travelSeconds; _seatingSeconds = seatingSeconds; _turnsPerSecond = seatingTurnsPerSecond;
        Reset();
    }

    public void Advance(double seconds, bool command, bool eligible)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        var stage = State.Stage;
        var extension = State.Extension;
        var applied = State.Applied;
        var turns = State.Turns;
        var inhibited = command && (!eligible || applied);
        // Loss before release returns the retained cap to home. No partial
        // attempt can turn into completion merely because a timer expired.
        if ((stage == Phase.Lowering || stage == Phase.Seating) && (!command || !eligible))
        {
            stage = Phase.Retracting; _seatElapsed = 0;
        }
        if (stage == Phase.Home && command && eligible && !applied)
            stage = Phase.Lowering;
        var moving = false;
        switch (stage)
        {
            case Phase.Lowering:
                extension = Math.Min(1, extension + seconds / _travelSeconds);
                moving = true;
                if (extension >= 1 - 1e-12) { extension = 1; stage = Phase.Seating; _seatElapsed = 0; }
                break;
            case Phase.Seating:
                var accepted = Math.Min(seconds, Math.Max(0, _seatingSeconds - _seatElapsed));
                turns += accepted * _turnsPerSecond;
                _seatElapsed += seconds;
                if (_seatElapsed + 1e-12 >= _seatingSeconds)
                {
                    applied = true; stage = Phase.Retracting;
                }
                break;
            case Phase.Retracting:
                extension = Math.Max(0, extension - seconds / _travelSeconds);
                moving = true;
                if (extension <= 1e-12) { extension = 0; stage = Phase.Home; moving = false; }
                break;
        }
        State = new Snapshot(stage, extension, applied, moving, inhibited, turns);
    }

    public void Pause() => State = State with { Moving = false };
    public void Reset() { _seatElapsed = 0; State = new Snapshot(Phase.Home, 0, false, false, false, 0); }
}
