using System;

namespace RungProof.Next.Scenes;

/// <summary>Command-driven shutter travel: zero is open, one is closed. No sequence or command generation.</summary>
public sealed class ServiceDoorPlantModel
{
    private readonly double _fullTravelSeconds;
    public double Position { get; private set; } = 1;
    public bool OpenLimitNc => Position > 0;
    public bool ClosedLimitNc => Position < 1;
    public bool CycleComplete { get; private set; }

    public ServiceDoorPlantModel(double fullTravelSeconds)
    {
        if (!double.IsFinite(fullTravelSeconds) || fullTravelSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(fullTravelSeconds));
        _fullTravelSeconds = fullTravelSeconds;
    }

    public void Reset() { Position = 1; CycleComplete = false; }

    public void Advance(double seconds, bool open, bool close)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        // Both asserted is an invalid direction image, so hold rather than
        // choose a direction or secretly correct the controller's outputs.
        if (seconds == 0 || open == close) return;
        var previous = Position;
        Position = Math.Clamp(Position + (close ? 1 : -1) * seconds / _fullTravelSeconds, 0, 1);
        if (Position != previous) CycleComplete = Position is 0 or 1;
    }
}
