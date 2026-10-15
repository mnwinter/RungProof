using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Prescribed single-carton sorter motion. The controller owns conveyor and
/// diverter commands; simulated vision/permissive fixtures gate travel.
/// This model does not recognize images or simulate contact forces.
/// </summary>
public sealed class VisionSorterPlantModel
{
    public enum SortPhase { Awaiting, Infeed, Indexing, Discharge, Complete }
    public const double HomeX = -1.1, TableX = 3.3;
    public const double TravelSpeedMps = .45, IndexSpeedDegrees = 45;
    // The receiving belt ends at radius 6.6 m. Keeping the carton centre at
    // 6.1 leaves its 0.425 m half-length and a 75 mm end margin supported.
    public const double OutfeedEndRadius = 6.1;
    public SortPhase Phase { get; private set; }
    public int LatchedClass { get; private set; }
    public double InfeedX { get; private set; } = HomeX;
    public double YawDegrees { get; private set; }
    public double OutfeedRadius { get; private set; }
    public bool Done => Phase == SortPhase.Complete;
    public bool AtTable => Phase is SortPhase.Indexing or SortPhase.Discharge;
    public double TargetYaw => LatchedClass switch { 1 => 67.5, 2 => 22.5, 3 => -22.5, 4 => -67.5, _ => 0 };
    public double X => Phase is SortPhase.Discharge or SortPhase.Complete
        ? TableX + OutfeedRadius * Math.Cos(YawDegrees * Math.PI / 180) : InfeedX;
    public double Z => Phase is SortPhase.Discharge or SortPhase.Complete
        ? -OutfeedRadius * Math.Sin(YawDegrees * Math.PI / 180) : 0;

    public void Reset()
    {
        Phase = SortPhase.Awaiting; LatchedClass = 0; InfeedX = HomeX;
        YawDegrees = OutfeedRadius = 0;
    }

    public void Step(double seconds, bool conveyor, bool diverter,
        bool packagePresent, bool resultValid, bool destinationClear, int visionClass)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds));
        if (seconds == 0 || Done || !packagePresent || !resultValid || !destinationClear) return;
        if (Phase == SortPhase.Awaiting)
        {
            if (!conveyor || visionClass is < 1 or > 4) return;
            // A later fixture change must never redirect a moving carton.
            LatchedClass = visionClass; Phase = SortPhase.Infeed;
        }
        var remaining = seconds;
        while (remaining > 1e-10 && !Done)
        {
            var tick = Math.Min(.01, remaining);
            switch (Phase)
            {
                case SortPhase.Infeed:
                    if (!conveyor) return;
                    InfeedX = Math.Min(TableX, InfeedX + TravelSpeedMps * tick);
                    if (InfeedX >= TableX - 1e-10) { InfeedX = TableX; Phase = SortPhase.Indexing; }
                    break;
                case SortPhase.Indexing:
                    if (!diverter) return;
                    var error = TargetYaw - YawDegrees;
                    YawDegrees += Math.Sign(error) * Math.Min(Math.Abs(error), IndexSpeedDegrees * tick);
                    if (Math.Abs(TargetYaw - YawDegrees) <= 1e-10)
                    { YawDegrees = TargetYaw; Phase = SortPhase.Discharge; }
                    break;
                case SortPhase.Discharge:
                    if (!conveyor || !diverter) return;
                    OutfeedRadius = Math.Min(OutfeedEndRadius, OutfeedRadius + TravelSpeedMps * tick);
                    if (OutfeedRadius >= OutfeedEndRadius - 1e-10)
                    { OutfeedRadius = OutfeedEndRadius; Phase = SortPhase.Complete; }
                    break;
            }
            remaining -= tick;
        }
    }
}
