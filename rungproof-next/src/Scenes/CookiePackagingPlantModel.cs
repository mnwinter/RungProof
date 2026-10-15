using System;
using System.Linq;

namespace RungProof.Next.Scenes;

/// <summary>
/// Finite six-cookie indexing exercise. One prescribed belt displacement moves
/// every tray; a mechanical index stops the next unwrapped tray at the sealer.
/// No feed replenishment, slip, heat transfer, film mechanics or collision
/// dynamics are claimed. Commands belong to the controller; position, optical
/// crossings, head travel and completion belong to this offline plant.
/// </summary>
public sealed class CookiePackagingPlantModel
{
    public const int Capacity = 6;
    public const double SurfaceY = .9, Pitch = .6, StationX = .3, SensorX = 0;
    public const double InitialLeft = -3.45, FinalDisplacement = 3.9, Speed = .3;
    public const double HalfTray = .16, HalfBiscuit = .115, SealTime = 2, HeadStroke = .22;
    private readonly bool[] _wrapped = new bool[Capacity], _counted = new bool[Capacity];
    private int _activeTray = -1;
    private double _sealElapsed;
    public double Displacement { get; private set; }
    public double BeltDistance { get; private set; }
    public int CookieCount => _counted.Count(value => value);
    public int WrappedCount => _wrapped.Count(value => value);
    public bool Faulted { get; private set; }
    public string FaultReason { get; private set; } = string.Empty;
    public bool Busy => _activeTray >= 0;
    public bool Ready => !Faulted && !Busy;
    public bool Complete => WrappedCount == Capacity && Displacement >= FinalDisplacement - 1e-8;
    public bool ProductPresent => Enumerable.Range(0, Capacity).Any(i => !_wrapped[i] && Math.Abs(X(i) - StationX) < 1e-8);
    public bool BeamBlocked => Enumerable.Range(0, Capacity).Any(i => X(i) - HalfBiscuit <= SensorX && X(i) + HalfBiscuit >= SensorX);
    public double HeadFraction => !Busy ? 0 : _sealElapsed < .5 ? _sealElapsed / .5 : _sealElapsed <= 1.5 ? 1 : (SealTime - _sealElapsed) / .5;
    public double X(int index) => InitialLeft + Pitch * index + Displacement;
    public bool IsWrapped(int index) => _wrapped[index];

    public void Reset()
    {
        Array.Clear(_wrapped); Array.Clear(_counted);
        Displacement = BeltDistance = _sealElapsed = 0; _activeTray = -1;
        Faulted = false; FaultReason = string.Empty;
    }

    public void Step(double seconds, bool infeed, bool package)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Cookie packaging time must be finite and non-negative.");
        var remaining = seconds;
        while (remaining > 1e-10 && !Faulted)
        {
            var tick = Math.Min(.01, remaining);
            Tick(tick, infeed, package); remaining -= tick;
        }
    }

    private void Tick(double seconds, bool infeed, bool package)
    {
        if (infeed && (package || Busy)) { Fault("Feed requested while the sealer is active"); return; }
        if (package)
        {
            if (!Busy)
            {
                _activeTray = Enumerable.Range(0, Capacity).FirstOrDefault(i => !_wrapped[i] && Math.Abs(X(i) - StationX) < 1e-8, -1);
                if (_activeTray < 0) return; // Empty station cannot manufacture a package/count.
                _sealElapsed = 0;
            }
            _sealElapsed = Math.Min(SealTime, _sealElapsed + seconds);
            if (_sealElapsed >= SealTime - 1e-8)
            { _wrapped[_activeTray] = true; _activeTray = -1; _sealElapsed = 0; }
            // The held command cannot start another tray until feed indexes it.
            return;
        }
        if (!infeed || Complete) return;
        var limit = FinalDisplacement;
        for (var i = 0; i < Capacity; i++)
            if (!_wrapped[i]) limit = Math.Min(limit, StationX - (InitialLeft + Pitch * i));
        var next = Math.Min(limit, Displacement + Speed * seconds);
        BeltDistance += next - Displacement; Displacement = next;
        for (var i = 0; i < Capacity; i++)
            if (X(i) + HalfBiscuit >= SensorX - 1e-8) _counted[i] = true;
    }

    private void Fault(string reason) { Faulted = true; FaultReason = reason; }
}
