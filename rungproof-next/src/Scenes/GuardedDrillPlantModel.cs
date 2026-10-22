using System;

namespace RungProof.Next.Scenes;

/// <summary>Explicit motorized feed/retract mechanics; never generates a drill cycle or spring return.</summary>
public sealed class GuardedDrillPlantModel
{
    private readonly double _fullTravelSeconds;
    public double Position { get; private set; }
    public bool AtTop => Position <= 0;
    public bool AtBottom => Position >= 1;
    public bool MotionInhibited { get; private set; }
    public bool CycleComplete { get; private set; }
    private bool _visitedBottom;

    public GuardedDrillPlantModel(double fullTravelSeconds)
    {
        if (!double.IsFinite(fullTravelSeconds) || fullTravelSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(fullTravelSeconds));
        _fullTravelSeconds = fullTravelSeconds;
    }
    public void Reset() { Position = 0; MotionInhibited = CycleComplete = _visitedBottom = false; }
    public void Advance(double seconds, bool spindle, bool feed, bool retract, bool leftHand, bool rightHand, bool workpiece)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        var feedPermitted = spindle && leftHand && rightHand && workpiece;
        MotionInhibited = (feed && retract) || (feed && !feedPermitted);
        if (seconds == 0 || feed == retract || (feed && !feedPermitted)) return;
        // Explicit retract can withdraw the tool without a workpiece or spindle.
        // This is an educational actuator contract, not a safety-rated circuit.
        Position = Math.Clamp(Position + (feed ? 1 : -1) * seconds / _fullTravelSeconds, 0, 1);
        if (AtBottom) _visitedBottom = true;
        CycleComplete = _visitedBottom && AtTop;
    }
}
