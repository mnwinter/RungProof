using System;
using System.Linq;

namespace RungProof.Next.Scenes;

/// <summary>
/// Original offline two-bay training plant. Positions are metres; a vehicle
/// faces -Z. The prescribed accepted PLC/plant clock owns all movement.
/// No controller count, transport, rendering or real barrier safety is here.
/// </summary>
public sealed class ParkingEntryPlantModel
{
    public enum VehicleState { Hidden, Entering, Parked, Exiting, Departed }
    public enum RoutePhase { Idle, Approach, EntryWait, EntryCross, ParkTurn, ExitTurn, ExitWait, ExitCross }
    public sealed class Vehicle
    {
        public VehicleState State { get; internal set; }
        public double X { get; internal set; }
        public double Z { get; internal set; } = 8;
        public double Yaw { get; internal set; }
        public double Travel { get; internal set; }
        public bool Visible => State != VehicleState.Hidden;
    }
    public const double TurnRadius = 3.5;
    public const double SpeedMps = 1;
    public const double BoomTimeS = 1;
    public Vehicle[] Vehicles { get; } = [new(), new()];
    public RoutePhase Phase { get; private set; }
    public double BoomFraction { get; private set; }
    public bool EntryPassed { get; private set; }
    public bool ExitPassed { get; private set; }
    public bool SpaceAvailable => Vehicles.Any(v => v.State is VehicleState.Hidden or VehicleState.Departed);
    public bool EntryDetected => Vehicles.Any(v => v.Visible && Math.Abs(v.X) < .01 && Math.Abs(v.Z - 2.7) <= 1.9);
    public bool PassageDetected => Vehicles.Any(v => v.Visible && Math.Abs(v.X) < .01 && Math.Abs(v.Z + .7) <= 1.9);
    // Larger than the boom's Z thickness: do not lower into a passing vehicle.
    public bool PassageOccupied => Vehicles.Any(v => v.Visible && Math.Abs(v.X) < 1.8 && Math.Abs(v.Z) < 2.2);
    public bool ExitRequested => Phase is RoutePhase.ExitTurn or RoutePhase.ExitWait or RoutePhase.ExitCross;
    public bool CanEnter => Phase == RoutePhase.Idle && SpaceAvailable;
    public bool CanExit => Phase == RoutePhase.Idle && Vehicles.Any(v => v.State == VehicleState.Parked)
        && Vehicles.All(v => v.State != VehicleState.Departed);
    public bool CanClearDeparted => Phase == RoutePhase.Idle && Vehicles.Any(v => v.State == VehicleState.Departed);
    private int _active = -1;
    private double _distance;

    public void Reset()
    {
        foreach (var v in Vehicles) { v.State = VehicleState.Hidden; v.X = v.Yaw = v.Travel = 0; v.Z = 8; }
        Phase = RoutePhase.Idle; _active = -1; _distance = BoomFraction = 0; EntryPassed = ExitPassed = false;
    }
    public bool RequestEntry()
    {
        if (!CanEnter) return false;
        // Reuse the car already outside before introducing another car into
        // that same approach position. Otherwise two visible cars can overlap.
        _active = Array.FindIndex(Vehicles, v => v.State == VehicleState.Departed);
        if (_active < 0) _active = Array.FindIndex(Vehicles, v => v.State == VehicleState.Hidden);
        var v = Vehicles[_active]; v.State = VehicleState.Entering; v.X = v.Yaw = 0; v.Z = 8;
        _distance = 0; Phase = RoutePhase.Approach; return true;
    }
    public bool RequestExit()
    {
        if (!CanExit) return false;
        _active = Array.FindIndex(Vehicles, v => v.State == VehicleState.Parked);
        Vehicles[_active].State = VehicleState.Exiting; _distance = 0; Phase = RoutePhase.ExitTurn; return true;
    }
    public bool ClearDeparted()
    {
        if (!CanClearDeparted) return false;
        foreach (var v in Vehicles.Where(v => v.State == VehicleState.Departed)) v.State = VehicleState.Hidden;
        return true;
    }
    public void Step(double seconds, bool enabled, bool boomOpen, bool vehicleRun)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (seconds == 0) return;
        // Completion feedback persists until the following accepted scan samples
        // it. Stop never calls Step, so an unsampled physical event is retained.
        EntryPassed = ExitPassed = false;
        if (!enabled) return;
        var target = boomOpen || PassageOccupied ? 1d : 0d;
        BoomFraction = MoveTowards(BoomFraction, target, seconds / BoomTimeS);
        if (_active < 0 || !vehicleRun) return;
        var v = Vehicles[_active];
        var travel = seconds * SpeedMps;
        if (Phase is RoutePhase.EntryWait or RoutePhase.ExitWait)
        {
            if (BoomFraction < 1 - 1e-9) return;
            Phase = Phase == RoutePhase.EntryWait ? RoutePhase.EntryCross : RoutePhase.ExitCross;
            _distance = 0;
        }
        var length = Phase switch
        {
            RoutePhase.Approach => 3.8,
            RoutePhase.EntryCross => 7.7,
            RoutePhase.ParkTurn or RoutePhase.ExitTurn => TurnRadius * Math.PI / 2,
            RoutePhase.ExitCross => 11.5,
            _ => 0,
        };
        var moved = Math.Min(travel, length - _distance); _distance += moved; v.Travel += moved;
        var side = _active == 0 ? -1d : 1d;
        switch (Phase)
        {
            case RoutePhase.Approach: v.Z = 8 - _distance; break;
            case RoutePhase.EntryCross: v.Z = 4.2 - _distance; break;
            case RoutePhase.ExitCross: v.Z = -3.5 + _distance; break;
            case RoutePhase.ParkTurn:
            case RoutePhase.ExitTurn:
                var angle = Phase == RoutePhase.ParkTurn ? _distance / TurnRadius : Math.PI / 2 - _distance / TurnRadius;
                v.X = side * TurnRadius * (1 - Math.Cos(angle)); v.Z = -3.5 - TurnRadius * Math.Sin(angle);
                v.Yaw = -side * angle; break;
        }
        if (_distance < length - 1e-9) return;
        _distance = 0;
        Phase = Phase switch
        {
            RoutePhase.Approach => RoutePhase.EntryWait,
            RoutePhase.EntryCross => RoutePhase.ParkTurn,
            RoutePhase.ExitTurn => RoutePhase.ExitWait,
            _ => Finish(v),
        };
    }
    private RoutePhase Finish(Vehicle v)
    {
        if (Phase == RoutePhase.ParkTurn) { v.State = VehicleState.Parked; EntryPassed = true; }
        if (Phase == RoutePhase.ExitCross) { v.State = VehicleState.Departed; ExitPassed = true; }
        _active = -1; return RoutePhase.Idle;
    }
    private static double MoveTowards(double value, double target, double distance) => value < target
        ? Math.Min(value + distance, target) : Math.Max(value - distance, target);
}
