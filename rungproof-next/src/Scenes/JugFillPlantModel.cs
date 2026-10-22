using System;

namespace RungProof.Next.Scenes;

/// <summary>Command-driven conveyor and liquid accumulation, with no sequencing decisions.</summary>
public sealed class JugFillPlantModel
{
    private readonly double _home, _exit, _speed, _beam, _rear, _front, _rate;
    public double Position { get; private set; }
    public double Fraction { get; private set; }
    public bool Present => Position + _front >= _beam - 1e-9 && Position + _rear <= _beam + 1e-9;
    public bool High => Fraction >= 1;
    public bool AtExit => Position >= _exit - 1e-9;
    public JugFillPlantModel(double home, double exit, double speed, double beam, double rear, double front, double rate)
    {
        if (!double.IsFinite(home + exit + speed + beam + rear + front + rate)
            || exit <= home || speed <= 0 || rear >= front || rate <= 0)
            throw new ArgumentOutOfRangeException(nameof(exit));
        (_home, _exit, _speed, _beam, _rear, _front, _rate) = (home, exit, speed, beam, rear, front, rate);
        Reset();
    }
    public void Reset() { Position = _home; Fraction = 0; }
    public void Advance(double seconds, bool conveyor, bool valve, bool skid, bool nozzleInsideNeck)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (conveyor) Position = Math.Min(_exit, Position + _speed * seconds);
        else if (Present && valve && skid && nozzleInsideNeck)
            Fraction = Math.Min(1, Fraction + _rate * seconds);
    }
}
