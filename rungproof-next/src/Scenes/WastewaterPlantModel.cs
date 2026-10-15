using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Illustrative four-vessel common-header process. Equal vessels share one
/// normalized level; rates describe fractions of total bank capacity per second.
/// This is neither a hydraulic design nor a calibrated litre/4-20 mA instrument.
/// </summary>
public sealed class WastewaterPlantModel
{
    public readonly record struct Snapshot(double Level, double Inflow, double TransferFlow,
        double Overflow, bool High, bool TransferInhibited)
    {
        public bool Empty => Level <= 0;
        public bool Full => Level >= 1;
        public double LevelPercent => Level * 100;
        public double TransmitterMa => 4 + 16 * Level;
    }

    private readonly double _initial, _inflowRate, _transferRate, _highOn, _highOff;
    public Snapshot State { get; private set; }

    public WastewaterPlantModel(double initial = .35, double inflowRate = .04,
        double transferRate = .08, double highOn = .65, double highOff = .20)
    {
        if (!double.IsFinite(initial) || initial < 0 || initial > 1
            || !double.IsFinite(inflowRate) || inflowRate <= 0
            || !double.IsFinite(transferRate) || transferRate <= 0
            || !double.IsFinite(highOn) || !double.IsFinite(highOff)
            || highOff < 0 || highOn > 1 || highOff >= highOn)
            throw new ArgumentOutOfRangeException(nameof(initial), "Invalid wastewater process configuration.");
        (_initial, _inflowRate, _transferRate, _highOn, _highOff) =
            (initial, inflowRate, transferRate, highOn, highOff);
        Reset();
    }

    public void Advance(double seconds, bool inflowEnabled, bool pumpCommand,
        double valveFraction, bool treatmentReady, bool outletClear)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Process ticks must be at most 20 ms.");
        if (!double.IsFinite(valveFraction) || valveFraction < 0 || valveFraction > 1)
            throw new ArgumentOutOfRangeException(nameof(valveFraction));
        var inflow = inflowEnabled ? _inflowRate : 0;
        var available = State.Level + inflow * seconds;
        // The plant never supplies the PLC command. Loss of either downstream
        // permissive prevents modeled transfer even if a bad program holds Run.
        var eligible = pumpCommand && treatmentReady && outletClear && valveFraction > 0;
        var transferred = eligible ? Math.Min(available, _transferRate * valveFraction * seconds) : 0;
        var afterTransfer = available - transferred;
        var overflow = Math.Max(0, afterTransfer - 1) / seconds;
        var level = Math.Clamp(afterTransfer, 0, 1);
        var high = State.High ? level > _highOff : level >= _highOn;
        State = new Snapshot(level, inflow, transferred / seconds, overflow, high,
            pumpCommand && (!eligible || transferred == 0));
    }

    public void Pause() => State = State with { Inflow = 0, TransferFlow = 0, Overflow = 0, TransferInhibited = false };
    public void Reset() => State = new Snapshot(_initial, 0, 0, 0, _initial >= _highOn, false);
}
