using System;
using System.Linq;

namespace RungProof.Next.Scenes;

/// <summary>
/// Original finite three-carton accumulation exercise. The front stop and
/// preceding carton bound each feed stroke; prescribed travel excludes roller
/// slip, contact forces and collision dynamics. PLC owns count and commands.
/// Only accepted controller/plant scans advance these positions.
/// </summary>
public sealed class PackageGroupingPlantModel
{
    public const int Capacity = 3;
    public const double SurfaceY = .9, HalfCarton = .425, GateX = 3;
    public const double EntryX = -4.4, SensorX = -3.5, Speed = .4;
    public const double FrontTarget = GateX - .04 - HalfCarton - .005;
    public const double FinalFront = 6.4;
    public double[] X { get; } = new double[Capacity];
    public int Loaded { get; private set; }
    public int ActiveFeed { get; private set; } = -1;
    public bool Releasing { get; private set; }
    public bool Complete { get; private set; }
    public double GateFraction { get; private set; }
    public double RollerTravel { get; private set; }
    public bool CanLoad => ActiveFeed < 0 && !Releasing && !Complete && Loaded < Capacity && GateFraction <= 1e-9;
    public bool PackageDetected => Enumerable.Range(0, Loaded).Any(i => Math.Abs(X[i] - SensorX) <= HalfCarton);
    public bool GroupStaged => Loaded == Capacity && ActiveFeed < 0 && !Releasing && !Complete;
    public bool ReceiverOccupied => Enumerable.Range(0, Loaded).Any(i => X[i] + HalfCarton > 3.2);
    public bool ReceiverDetected => Enumerable.Range(0, Loaded).Any(i => Math.Abs(X[i] - 3.7) <= HalfCarton);
    public bool StopPlaneOccupied => Enumerable.Range(0, Loaded).Any(i => Math.Abs(X[i] - GateX) < HalfCarton + .05);
    public double Target(int index) => FrontTarget - index * 2 * HalfCarton;

    public void Reset()
    {
        Array.Fill(X, EntryX); Loaded = 0; ActiveFeed = -1;
        Releasing = Complete = false; GateFraction = RollerTravel = 0;
    }
    public bool Load()
    {
        if (!CanLoad || GateFraction > 1e-9) return false;
        ActiveFeed = Loaded++; X[ActiveFeed] = EntryX; return true;
    }
    public void Step(double seconds, bool enabled, bool pathClear, bool conveyorRun, bool release)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (seconds == 0 || !enabled) return;
        // An incomplete group cannot retract its physical accumulation stop.
        // Once crossing starts, hold the raised gate until all cartons clear it.
        if (release && GroupStaged && pathClear) Releasing = true;
        if (Releasing && !Complete)
        {
            if (!pathClear || !conveyorRun || !release) return;
            GateFraction = Math.Min(1, GateFraction + seconds); // declared 1 s stroke
            if (GateFraction < 1 - 1e-9) return;
            GateFraction = 1; // canonical raised datum, avoid accumulated float drift
            var travel = Math.Min(Speed * seconds, FinalFront - X[0]);
            for (var i = 0; i < Loaded; i++) X[i] += travel;
            RollerTravel += travel;
            Complete = X[0] >= FinalFront - 1e-9;
            return;
        }
        // After completion the clear stop can return without hitting the group.
        if (!StopPlaneOccupied) GateFraction = Math.Max(0, GateFraction - seconds);
        if (ActiveFeed < 0 || !pathClear || !conveyorRun || GateFraction > 1e-9) return;
        var advance = Math.Min(Speed * seconds, Target(ActiveFeed) - X[ActiveFeed]);
        X[ActiveFeed] += advance; RollerTravel += advance;
        if (X[ActiveFeed] >= Target(ActiveFeed) - 1e-9) ActiveFeed = -1;
    }
}
