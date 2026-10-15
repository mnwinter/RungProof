using System;

namespace RungProof.Next.Scenes;

/// <summary>Prescribed supported workpiece travel. PLC owns every phase/timer;
/// this model only applies commands and exposes malformed-image diagnostics.</summary>
public sealed class CoatingPlantModel
{
    private readonly double _minimum, _maximum, _home, _speed;
    public double Position { get; private set; }
    public double Speed { get; private set; }
    public bool MotionInhibited { get; private set; }
    public bool SprayInhibited { get; private set; }
    public bool Spraying { get; private set; }
    public bool TravelLimited { get; private set; }
    public CoatingPlantModel(double minimum, double maximum, double home, double speed)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || !double.IsFinite(home)
            || !double.IsFinite(speed) || minimum >= maximum || home < minimum || home > maximum || speed <= 0 || speed > 2)
            throw new ArgumentOutOfRangeException(nameof(speed));
        (_minimum, _maximum, _home, _speed) = (minimum, maximum, home, speed); Reset();
    }
    public void Reset() { Position = _home; Pause(); MotionInhibited = SprayInhibited = TravelLimited = false; }
    public void Pause() { Speed = 0; Spraying = false; }
    public void Step(double seconds, bool index, bool spray, bool vent, bool station, bool sprayReady, bool ventReady)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        MotionInhibited = index && (spray || !sprayReady || !ventReady);
        Spraying = spray && !index && vent && station && sprayReady && ventReady;
        SprayInhibited = spray && !Spraying;
        var speed = index && !MotionInhibited ? _speed : 0;
        var next = Math.Clamp(Position + speed * seconds, _minimum, _maximum);
        TravelLimited = Math.Abs(next - Position - speed * seconds) > 1e-9;
        Position = next; Speed = TravelLimited ? 0 : speed;
    }
}
