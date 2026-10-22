using System;

namespace RungProof.Next.Scenes;

/// <summary>Bounded mechanical travel only. Direction remains entirely controller-owned.</summary>
public sealed class BidirectionalLiftPlantModel
{
    private readonly double _fullTravelSeconds;
    private bool _visitedTop;
    public double Position { get; private set; }
    public bool AtBottom => Position <= 0;
    public bool AtTop => Position >= 1;
    public bool CycleComplete => _visitedTop && AtBottom;
    public BidirectionalLiftPlantModel(double fullTravelSeconds)
    {
        if (!double.IsFinite(fullTravelSeconds) || fullTravelSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(fullTravelSeconds));
        _fullTravelSeconds = fullTravelSeconds;
    }
    public void Reset() { Position = 0; _visitedTop = false; }
    public void Advance(double seconds, bool up, bool down)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (up != down) Position = Math.Clamp(Position + (up ? 1 : -1) * seconds / _fullTravelSeconds, 0, 1);
        if (AtTop) _visitedTop = true;
    }
}
