using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// One prethreaded cable, one prescribed 3 m cut. Feed length drives the
/// encoder and reel; no slip, tension, reel layering or cutting force solver.
/// The finite available stock excludes cable already threaded to the knife.
/// </summary>
public sealed class CableCutPlantModel
{
    public const double TargetM = 3, InitialStockM = 10, FeedSpeed = .3;
    public const double CableY = 1.1, CableRadius = .014, StrokeM = .35;
    public double LengthM { get; private set; }
    public double StockM => InitialStockM - LengthM;
    public double Stroke { get; private set; }
    public bool LengthReached => LengthM >= TargetM - 1e-8;
    public bool Home => Stroke <= 1e-8;
    public bool CutDone { get; private set; }
    public bool Complete => CutDone && Home;
    public bool Faulted { get; private set; }
    public string FaultReason { get; private set; } = string.Empty;
    public double FeedRate { get; private set; }
    public void Reset()
    { LengthM = Stroke = FeedRate = 0; CutDone = Faulted = false; FaultReason = string.Empty; }
    public void Pause() => FeedRate = 0;
    public void Step(double seconds, bool active, bool feed, bool cut)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Cable cut time must be finite and non-negative.");
        FeedRate = 0;
        if (!active || Faulted || Complete) return;
        var remaining = seconds;
        while (remaining > 1e-10 && !Faulted && !Complete)
        {
            var tick = Math.Min(.01, remaining);
            // Reject incompatible commands before moving either axis.
            if ((feed && cut) || (feed && !Home) || (cut && !LengthReached) || (feed && CutDone))
            { Faulted = true; FaultReason = "Feed/cutter conflict, blade away from home or target not measured"; break; }
            if (feed && !LengthReached)
            {
                var amount = Math.Min(TargetM - LengthM, FeedSpeed * tick);
                LengthM += amount; FeedRate = amount / tick;
            }
            if (cut && !CutDone)
            {
                Stroke = Math.Min(1, Stroke + tick / .8);
                if (Stroke >= 1 - 1e-8) { Stroke = 1; CutDone = true; }
            }
            else if (!cut && Stroke > 0) Stroke = Math.Max(0, Stroke - tick / .8);
            remaining -= tick;
        }
        if (Faulted || LengthReached) FeedRate = 0;
    }
}
