using System;

namespace RungProof.Next.Scenes;

/// <summary>Original symbolic/offline bascule plant; angle-driven raw limits.
/// Prescribed travel times illustrate motion, not physical loads or road design.</summary>
public sealed class DrawbridgePlantModel
{
    public const double RaisedDegrees = 70;
    public const double BridgeTravelSeconds = 4;
    public const double BarrierTravelSeconds = 2;
    public double BridgeDegrees { get; private set; }
    public double BarrierDegrees { get; private set; }
    public bool MotionInhibited { get; private set; }
    public bool Home => BridgeDegrees <= 1e-8;
    public bool Raised => BridgeDegrees >= RaisedDegrees - 1e-8;
    public bool GatesClosed => BarrierDegrees <= 1e-8;
    public bool GatesOpen => BarrierDegrees >= 90 - 1e-8;

    public void Reset() { BridgeDegrees = BarrierDegrees = 0; MotionInhibited = false; }

    public void Step(double seconds, bool stopped, bool raise, bool lower, bool closeGate, bool openGate, bool release)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        // Reject conflicting/impossible motion without rewriting the user's
        // PLC output image. An observable PC feedback flags the inhibited drive.
        MotionInhibited = (raise && lower) || (closeGate && openGate)
            || ((raise || lower) && (!stopped || !GatesClosed || release || openGate))
            || (openGate && (!Home || raise || lower)) || (release && (!Home || !GatesOpen || raise || lower));
        if (seconds == 0) return;
        if (closeGate && !openGate) BarrierDegrees = Math.Max(0, BarrierDegrees - 90 / BarrierTravelSeconds * seconds);
        else if (openGate && !closeGate && Home && !raise && !lower)
            BarrierDegrees = Math.Min(90, BarrierDegrees + 90 / BarrierTravelSeconds * seconds);
        // Use closed feedback at the start of a scan, matching a real sampled
        // limit. PLC sequencing normally commands motion on the next scan.
        if (!MotionInhibited && stopped && GatesClosed && !release && !openGate)
        {
            if (raise && !lower) BridgeDegrees = Math.Min(RaisedDegrees, BridgeDegrees + RaisedDegrees / BridgeTravelSeconds * seconds);
            else if (lower && !raise) BridgeDegrees = Math.Max(0, BridgeDegrees - RaisedDegrees / BridgeTravelSeconds * seconds);
        }
    }
}
