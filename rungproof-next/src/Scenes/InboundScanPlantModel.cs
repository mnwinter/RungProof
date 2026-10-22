using System;

namespace RungProof.Next.Scenes;

/// <summary>One conveyor and an automatic scanner; contains no controller sequence.</summary>
public sealed class InboundScanPlantModel
{
    private readonly double _home, _exit, _speed, _beam, _halfLength, _scanSeconds;
    private double _scanElapsed;
    public double Position { get; private set; }
    public bool AtScanner => Math.Abs(Position - _beam) <= _halfLength + 1e-9;
    public bool ScanComplete { get; private set; }
    public bool AtExit => Position >= _exit - 1e-9;

    public InboundScanPlantModel(double home, double exit, double speed, double beam, double halfLength, double scanSeconds)
    {
        if (!double.IsFinite(home + exit + speed + beam + halfLength + scanSeconds)
            || exit <= home || speed <= 0 || halfLength <= 0 || scanSeconds <= 0
            || beam - halfLength <= home || beam + halfLength >= exit)
            throw new ArgumentOutOfRangeException(nameof(exit), "Invalid supported scanner route.");
        (_home, _exit, _speed, _beam, _halfLength, _scanSeconds) = (home, exit, speed, beam, halfLength, scanSeconds);
        Reset();
    }
    public void Reset() { Position = _home; _scanElapsed = 0; ScanComplete = false; }
    public void Advance(double seconds, bool conveyorRun)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (conveyorRun)
        {
            Position = Math.Min(_exit, Position + _speed * seconds);
            if (!ScanComplete) _scanElapsed = 0; // A moving tote cannot finish identification.
        }
        else if (AtScanner && !ScanComplete)
        {
            _scanElapsed += seconds;
            ScanComplete = _scanElapsed + 1e-9 >= _scanSeconds;
        }
    }
}
