using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Original offline traffic training model, not traffic engineering or vehicle physics.
/// One scaled vehicle per direction uses a separate straight lane; the central
/// crossing is reported occupied until the entire vehicle has cleared it.
/// Accepted plant time alone moves vehicles. The PLC owns all six signal lamps.
/// </summary>
public sealed class MobileTrafficPlantModel
{
    public const double HomeX = 3.6, LaneZ = .9, SpeedMps = 1.3;
    // Conservative envelope of the reused 50%-scale parking vehicle GLB.
    public const double HalfLength = 1, HalfWidth = .55, CrossingHalfLength = 1.2;
    public sealed class Vehicle
    {
        public bool Visible { get; internal set; }
        public bool Requested { get; internal set; }
        public bool Committed { get; internal set; }
        public double Progress { get; internal set; }
        public bool Complete => Progress >= HomeX * 2 - 1e-9;
    }
    public Vehicle[] Vehicles { get; } = [new(), new()];
    public bool SignalConflict { get; private set; }
    public double X(int index) => index == 0 ? -HomeX + Vehicles[index].Progress : HomeX - Vehicles[index].Progress;
    public bool Occupied(int index) => Vehicles[index].Visible && Math.Abs(X(index)) < CrossingHalfLength + HalfLength;
    public bool CrossingClear => !Occupied(0) && !Occupied(1);
    public bool CanRequest(int index) => !Vehicles[index].Requested || Vehicles[index].Complete;
    public bool Request(int index)
    {
        if (index is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(index));
        if (!CanRequest(index)) return false;
        var vehicle = Vehicles[index];
        vehicle.Visible = vehicle.Requested = true;
        vehicle.Progress = 0; vehicle.Committed = false;
        return true;
    }
    public void Reset()
    {
        foreach (var vehicle in Vehicles) { vehicle.Visible = vehicle.Requested = vehicle.Committed = false; vehicle.Progress = 0; }
        SignalConflict = false;
    }
    public void Step(double seconds, bool ready, bool aClear, bool bClear, bool aGreen, bool bGreen)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        SignalConflict = aGreen && bGreen;
        if (seconds == 0) return;
        for (var i = 0; i < 2; i++)
        {
            var vehicle = Vehicles[i];
            if (!vehicle.Requested || vehicle.Complete) continue;
            var green = i == 0 ? aGreen : bGreen;
            // An accepted green lets this vehicle commit to its straight route.
            // After commitment it clears the crossing even under amber/all-red;
            // Entry faults block new entries; they do not strand a committed
            // vehicle in the crossing. Application Stop freezes the plant clock.
            if (!vehicle.Committed && (!green || !ready || !aClear || !bClear || SignalConflict
                || (Vehicles[1 - i].Committed && !Vehicles[1 - i].Complete))) continue;
            vehicle.Committed = true;
            vehicle.Progress = Math.Min(HomeX * 2, vehicle.Progress + seconds * SpeedMps);
        }
    }
}
