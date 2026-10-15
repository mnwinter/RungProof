using System;

namespace RungProof.Next.Scenes;

/// <summary>Prescribed single-tote travel; no station sequence or PLC decisions.</summary>
public sealed class ToteTransferPlantModel
{
    public readonly record struct Snapshot(double Position, double Speed, bool AtExit);
    private readonly double _home, _exit, _speed;
    public Snapshot State { get; private set; }

    public ToteTransferPlantModel(double home, double exit, double speed)
    {
        if (!double.IsFinite(home) || !double.IsFinite(exit) || exit <= home
            || !double.IsFinite(speed) || speed <= 0)
            throw new ArgumentException("Tote travel requires finite home < exit and positive speed.");
        _home = home; _exit = exit; _speed = speed;
        Reset();
    }

    public void Advance(double seconds, bool conveyorRun)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Tote travel accepts ticks up to 20 ms.");
        var old = State.Position;
        var next = conveyorRun ? Math.Min(_exit, old + _speed * seconds) : old;
        State = new Snapshot(next, (next - old) / seconds, next >= _exit - 1e-9);
    }

    public void Pause() => State = State with { Speed = 0 };
    public void Reset() => State = new Snapshot(_home, 0, false);
}
