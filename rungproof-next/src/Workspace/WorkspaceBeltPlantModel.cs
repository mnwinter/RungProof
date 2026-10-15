using System;

namespace RungProof.Next.Workspace;

/// <summary>
/// One finite, fully supported teaching carton on one measured belt. The
/// accepted controller clock is the only caller: this model owns no timer,
/// evaluator, hidden permissive, queue or automatic reload.
/// </summary>
public sealed class WorkspaceBeltPlantModel
{
    public const double SpeedMps = .45;
    private const double EndMarginM = .05;
    public double BeltLength { get; }
    public double LoadLength { get; }
    public double BeamPosition { get; }
    public double InitialCenter { get; }
    public double Center { get; private set; }
    public double LeadingEdge => Center + LoadLength / 2;
    public double TrailingEdge => Center - LoadLength / 2;
    public bool BeamBlocked => TrailingEdge <= BeamPosition + 1e-9
        && LeadingEdge >= BeamPosition - 1e-9;
    public bool AtTerminal => LeadingEdge >= BeltLength - EndMarginM - 1e-9;

    public WorkspaceBeltPlantModel(double beltLength, double loadLength, double beamPosition, double? measuredInitialCenter = null)
    {
        if (!double.IsFinite(beltLength) || !double.IsFinite(loadLength)
            || !double.IsFinite(beamPosition) || loadLength <= 0
            || beltLength <= loadLength + 2 * EndMarginM
            || beamPosition < 0 || beamPosition > beltLength)
            throw new ArgumentException("Measured belt/load/beam geometry cannot support a finite carton.");
        BeltLength = beltLength;
        LoadLength = loadLength;
        BeamPosition = beamPosition;
        InitialCenter = measuredInitialCenter ?? loadLength / 2 + EndMarginM;
        if (!double.IsFinite(InitialCenter) || InitialCenter - loadLength / 2 < 0
            || InitialCenter + loadLength / 2 > beltLength - EndMarginM)
            throw new ArgumentException("Measured initial carton must be fully supported.");
        Reset();
    }

    public void Reset() => Center = InitialCenter;

    /// <returns>Actual carton translation; belt travel may continue at a
    /// terminal under a true command, an explicit teaching simplification.</returns>
    public double Step(double seconds, bool acceptedRunCommand)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Accepted plant delta must be finite and non-negative.");
        var previous = Center;
        if (acceptedRunCommand)
            Center = Math.Min(BeltLength - EndMarginM - LoadLength / 2,
                Center + SpeedMps * seconds);
        return Center - previous;
    }
}
