using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// Fixed four-carton kinematic plant. The scanned PLC owns every sequence
/// transition and sends independent X/Y/Z and vacuum commands. This model only
/// integrates those actuator commands and publishes measured process feedback.
/// It intentionally has no palletizing phase/state machine and no force physics.
/// </summary>
public sealed class PalletizerPlantModel
{
    public const double PickX = -.95, PickZ = -.4, DeckY = 1.055, PalletY = .4505,
        CartonHeight = .4, CarryY = 1.66;
    public const double PickupY = DeckY + CartonHeight;
    public const double PlaceY = PalletY + CartonHeight;
    // Matches the authored ±5 mm encoder-window comparisons. The prescribed
    // 10 ms integration increments do not necessarily land on every integer-mm
    // target, so process-position feedback uses the same declared window.
    private const double PositionTolerance = .005;
    private const double HorizontalSpeed = .4;
    private const double VerticalSpeed = .3;

    public double X { get; private set; }
    public double Z { get; private set; }
    public double ToolY { get; private set; }
    public int Placed { get; private set; }
    public bool CartonAtPick { get; private set; }
    public bool Attached { get; private set; }
    public bool PickComplete { get; private set; }
    public bool Faulted { get; private set; }
    public string FaultReason { get; private set; } = "";

    public bool XHome => Near(X, PickX);
    public bool ZHome => Near(Z, PickZ);
    public bool YHome => Near(ToolY, CarryY);
    public bool Home => XHome && ZHome && YHome && !Attached;
    public bool AtPickup => XHome && ZHome && Near(ToolY, PickupY);
    public bool AtPlace
    {
        get
        {
            if (Placed >= 4) return false;
            var slot = Slot(Placed);
            return Near(X, slot.X) && Near(Z, slot.Z) && Near(ToolY, PlaceY);
        }
    }
    public bool InProgress => !Home || Attached;

    public static (double X, double Z) Slot(int index) =>
        ((index % 2 == 0 ? -.26 : .26), (index < 2 ? -.23 : .23));

    public PalletizerPlantModel() => Reset();

    public void Reset()
    {
        X = PickX;
        Z = PickZ;
        ToolY = CarryY;
        Placed = 0;
        CartonAtPick = true;
        Attached = false;
        PickComplete = false;
        Faulted = false;
        FaultReason = "";
    }

    // Optional operator supply action. The normal four-carton lesson models an
    // automatic magazine, so a carton is also staged after each completed place.
    public bool CanLoadCarton => Home && !CartonAtPick && !Attached && Placed < 4 && !Faulted;
    public bool LoadCarton()
    {
        if (!CanLoadCarton) return false;
        CartonAtPick = true;
        return true;
    }

    public void Step(
        double seconds,
        bool xPositive,
        bool xNegative,
        bool yPositive,
        bool yNegative,
        bool zPositive,
        bool zNegative,
        bool vacuum,
        bool palletValid,
        bool reset)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Elapsed time must be finite and nonnegative.");
        if (reset) { Reset(); return; }
        if (seconds == 0 || Faulted) return;
        if (Conflict(xPositive, xNegative) || Conflict(yPositive, yNegative) || Conflict(zPositive, zNegative))
        { Fault("Opposed axis commands were energized together."); return; }
        if (!palletValid && (xPositive || xNegative || yPositive || yNegative || zPositive || zNegative || vacuum))
        { Fault("Actuator command without pallet permissive."); return; }

        PickComplete = false;
        for (var remaining = seconds; remaining > 1e-9;)
        {
            var dt = Math.Min(.01, remaining);
            remaining -= dt;

            X = IntegrateAxis(X, xPositive, xNegative, PickX, Slot(3).X, HorizontalSpeed, dt);
            Z = IntegrateAxis(Z, zPositive, zNegative, PickZ, Slot(3).Z, HorizontalSpeed, dt);

            // The lower physical stop depends on whether the carriage is over
            // the pickup table or a pallet slot. The PLC still decides when to
            // move and when to stop from the measured position feedback.
            var lowerY = AtHorizontalPickup() ? PickupY : PlaceY;
            ToolY = IntegrateAxis(ToolY, yPositive, yNegative, lowerY, CarryY, VerticalSpeed, dt);

            if (!Attached && vacuum && AtPickup && CartonAtPick)
            {
                Attached = true;
                CartonAtPick = false;
            }
            else if (Attached && !vacuum)
            {
                // Stop/trip removes every command. The bounded offline lesson
                // retains the carton at the held pose instead of simulating a
                // gravity drop. Vacuum loss is a fault only when the PLC still
                // commands axis travel away from a supported place.
                if (!AtPlace)
                {
                    if (xPositive || xNegative || yPositive || yNegative || zPositive || zNegative)
                        Fault("Vacuum was removed during commanded travel.");
                    return;
                }
                Attached = false;
                Placed++;
                // The magazine is device-local material handling, not a motion
                // sequencer. It presents the next carton without choosing a PLC state.
                CartonAtPick = Placed < 4;
            }
        }

        // A completion indication is physical feedback: a carton has been
        // placed and all measured axes have returned home. It clears as soon as
        // the next commanded move leaves home.
        PickComplete = Placed > 0 && Home;
    }

    private bool AtHorizontalPickup() => XHome && ZHome;
    private static bool Conflict(bool positive, bool negative) => positive && negative;
    private static bool Near(double value, double target) => Math.Abs(value - target) <= PositionTolerance;
    private static double IntegrateAxis(double value, bool positive, bool negative,
        double minimum, double maximum, double speed, double dt)
    {
        var direction = positive ? 1 : negative ? -1 : 0;
        return Math.Clamp(value + direction * speed * dt, minimum, maximum);
    }
    private void Fault(string reason) { Faulted = true; FaultReason = reason; }
}
