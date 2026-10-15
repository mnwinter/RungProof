using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Renderer-neutral, illustrative two-bay energy fixture. PLC logic supplies
/// power allocations; this model never chooses a bay or rewrites a command.
/// It models neither an EV charging protocol nor electrical hardware.
/// </summary>
public sealed class EvChargingPlantModel
{
    public const double SharedBudgetKw = 6;
    public const double KwhPerPulse = .001;
    public readonly record struct BayInput(bool Occupied, bool Authorized, bool Ready, bool Connected, double AllocatedKw);
    public readonly record struct BaySnapshot(double DeliveredKw, double EnergyKwh, long PulseCount, bool EnergyPulse);
    public readonly record struct Snapshot(BaySnapshot A, BaySnapshot B, bool AllocationInhibited);
    private readonly double[] _energy = new double[2], _residual = new double[2];
    private readonly long[] _pulses = new long[2];
    public Snapshot Current { get; private set; }

    public void Reset()
    {
        Array.Clear(_energy); Array.Clear(_residual); Array.Clear(_pulses);
        Current = default;
    }

    /// <summary>Freeze integrated energy and fractional pulse state; remove transient feedback.</summary>
    public void Pause() => Current = new(new(0,_energy[0],_pulses[0],false),new(0,_energy[1],_pulses[1],false),false);

    /// <summary>
    /// One accepted offline plant tick, at most 20 ms. The 6 kW budget and
    /// .001 kWh pulse scale guarantee separate rising/falling scans at this rate.
    /// Long wall-clock gaps must be handled by the existing accepted-scan clock.
    /// </summary>
    public Snapshot Advance(double seconds, BayInput a, BayInput b)
    {
        if (!double.IsFinite(seconds) || seconds <= 0 || seconds > .020000001)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Use one accepted plant tick, up to 20 ms.");
        var invalid = !ValidAllocation(a.AllocatedKw) || !ValidAllocation(b.AllocatedKw)
            || a.AllocatedKw + b.AllocatedKw > SharedBudgetKw + 1e-9;
        BaySnapshot Tick(int index, BayInput input)
        {
            var power = !invalid && input.Occupied && input.Authorized && input.Ready && input.Connected ? input.AllocatedKw : 0;
            var increment = power * seconds / 3600;
            _energy[index] += increment;
            _residual[index] += increment;
            var pulse = _residual[index] >= KwhPerPulse - 1e-12;
            if (pulse) {
                _residual[index] = Math.Max(0,_residual[index] - KwhPerPulse);
                _pulses[index]++;
            }
            return new(power,_energy[index],_pulses[index],pulse);
        }
        Current = new(Tick(0,a),Tick(1,b),invalid);
        return Current;
    }
    private static bool ValidAllocation(double value) => double.IsFinite(value) && value >= 0 && value <= SharedBudgetKw;
}
