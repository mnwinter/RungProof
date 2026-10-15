using System;

namespace RungProof.Next.Scenes;

/// <summary>Normalized illustrative fill quantity; no litre/flow calibration.</summary>
public sealed class ToteFillPlantModel
{
    public readonly record struct Snapshot(double Fraction, double FlowFractionPerSecond, bool Inhibited)
    {
        public bool Complete => Fraction >= 1;
    }
    private readonly double _rate;
    public Snapshot State { get; private set; }

    public ToteFillPlantModel(double rateFractionPerSecond)
    {
        if (!double.IsFinite(rateFractionPerSecond) || rateFractionPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(rateFractionPerSecond));
        _rate = rateFractionPerSecond;
        Reset();
    }

    public void Advance(double seconds, bool valveCommand, bool eligible)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Fill model accepts ticks up to 20 ms.");
        var old = State.Fraction;
        var inhibited = valveCommand && (!eligible || old >= 1);
        var next = valveCommand && !inhibited ? Math.Min(1, old + _rate * seconds) : old;
        State = new Snapshot(next, (next-old)/seconds, inhibited);
    }

    public void Pause() => State = State with { FlowFractionPerSecond = 0 };
    public void Reset() => State = new Snapshot(0, 0, false);
}
