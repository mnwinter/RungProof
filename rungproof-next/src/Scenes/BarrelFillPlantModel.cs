using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// One prescribed, mechanically indexed barrel and a finite liquid inventory.
/// Constant flow is an exercise parameter, not a hydraulic calculation.
/// PLC commands are inputs; the plant does not rewrite the command image.
/// </summary>
public sealed class BarrelFillPlantModel
{
    public const double InitialX = -2.8, FillX = 0, ParkX = 3, Speed = .3;
    public const double DeckY = .9, BarrelRadius = .3, InnerRadius = .28;
    public const double LiquidBottom = .925, SourceBottom = 2.5, SourceRadius = .5;
    public const double SourceInitialLitres = 200, TargetLitres = 150, RateLitresPerSecond = 20;
    public double X { get; private set; } = InitialX;
    public double Litres { get; private set; }
    public double SourceLitres => SourceInitialLitres - Litres;
    public double FlowLitresPerSecond { get; private set; }
    public double BeltDistance => X - InitialX;
    public bool AtFill => Math.Abs(X - FillX) < 1e-8;
    public bool FillComplete => Litres >= TargetLitres - 1e-8;
    public bool Complete => FillComplete && X >= ParkX - 1e-8;
    public bool DownstreamClear => X + BarrelRadius < 2.7;
    // Sub-micron tolerance reconciles double integration with Godot's float
    // poses at exact silhouette boundaries (e.g. X=.3 or X=2.7).
    public bool FillBeamBlocked => Math.Abs(X) <= BarrelRadius + 1e-7;
    public bool ExitBeamBlocked => Math.Abs(X - ParkX) <= BarrelRadius + 1e-7;
    public bool Faulted { get; private set; }
    public string FaultReason { get; private set; } = string.Empty;
    public double LiquidHeight => Litres / (1000 * Math.PI * InnerRadius * InnerRadius);
    public double SourceHeight => SourceLitres / (1000 * Math.PI * SourceRadius * SourceRadius);

    public void Reset()
    {
        X = InitialX; Litres = FlowLitresPerSecond = 0;
        Faulted = false; FaultReason = string.Empty;
    }

    public void Pause() => FlowLitresPerSecond = 0;

    public void Step(double seconds, bool feed, bool valve)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Barrel fill time must be finite and non-negative.");
        FlowLitresPerSecond = 0;
        var remaining = seconds;
        while (remaining > 1e-10 && !Faulted)
        {
            var tick = Math.Min(.01, remaining);
            if (valve && (feed || !AtFill))
            { Faulted = true; FaultReason = "Fill requested with feed active or barrel away from indexed station"; break; }
            if (valve && !FillComplete)
            {
                var amount = Math.Min(TargetLitres - Litres, RateLitresPerSecond * tick);
                Litres += amount;
                FlowLitresPerSecond = amount / tick;
            }
            else if (feed && !Complete)
            {
                // The authored mechanical index blocks an unfilled barrel at
                // the station. Once filled, it may travel to the retained stop.
                X = Math.Min(FillComplete ? ParkX : FillX, X + Speed * tick);
            }
            remaining -= tick;
        }
        if (Faulted || FillComplete) FlowLitresPerSecond = 0;
    }
}
