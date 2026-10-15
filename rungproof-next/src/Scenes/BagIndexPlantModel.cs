using System;

namespace RungProof.Next.Scenes;

/// <summary>Prescribed single-bag travel. PLC run enable and reverse direction
/// stay distinct; sensor transitions and sequence decisions belong to callers.</summary>
public sealed class BagIndexPlantModel
{
    private readonly double _minimum, _maximum, _speed, _home;
    public double Position { get; private set; }
    public double Speed { get; private set; }
    public bool MotionInhibited { get; private set; }
    public bool TravelLimited { get; private set; }
    public BagIndexPlantModel(double minimum, double maximum, double home, double speed)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || !double.IsFinite(home)
            || !double.IsFinite(speed) || minimum >= maximum || home < minimum || home > maximum || speed <= 0 || speed > 2)
            throw new ArgumentOutOfRangeException(nameof(speed), "Bag travel requires finite supported bounds/home and speed in (0,2].");
        (_minimum, _maximum, _home, _speed) = (minimum, maximum, home, speed); Reset();
    }
    public void Reset() { Position = _home; Speed = 0; MotionInhibited = TravelLimited = false; }
    public void Pause() => Speed = 0;
    public void Step(double seconds, bool run, bool reverse, bool pauseClear)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        MotionInhibited = run && reverse && !pauseClear;
        var speed = run && !MotionInhibited ? (reverse ? -_speed : _speed) : 0;
        var requested = speed * seconds;
        var next = Math.Clamp(Position + requested, _minimum, _maximum);
        TravelLimited = Math.Abs(next - Position - requested) > 1e-9;
        Position = next; Speed = TravelLimited ? 0 : speed;
    }
}
