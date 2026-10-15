using System;

namespace RungProof.Next.Scenes;

/// <summary>
/// One bounded carton route, with PLC-owned horizontal/up/down commands.
/// Positions and feedback share this clock. No timers manufacture commands or
/// completion. Invalid transfer requests latch a diagnostic and hold the plant;
/// this model does not attempt falling-load or collision dynamics.
/// </summary>
public sealed class ChainLiftPlantModel
{
    public const double HomeSurfaceY = .9675, Stroke = 2.1, HalfLoad = .425;
    public const double DeckLeft = -1.25, DeckRight = 1.25, ReceiverLeft = 1.4;
    public const double InitialX = -2.7, ReceivingStopX = 3.5775;
    public double LoadX { get; private set; }
    public double Height { get; private set; }
    public double LoadY => HomeSurfaceY + (_carried ? Height : _received ? Stroke : 0);
    public bool Home => Height <= 1e-8;
    public bool Upper => Height >= Stroke - 1e-8;
    public bool BoxPresent => !_received && LoadX - HalfLoad <= InitialX && LoadX + HalfLoad >= InitialX;
    public bool CartonOnInfeed => !_received && !_carried;
    public bool CartonOnLift => _carried && LoadX - HalfLoad >= DeckLeft - 1e-8 && LoadX + HalfLoad <= DeckRight + 1e-8;
    public bool ReceiverOccupied => (_received || Upper) && LoadX + HalfLoad >= ReceiverLeft;
    // Entry beam is a footprint intersection, not the occupied receiver zone.
    // It clears once the carton's rear passes it, even while the receiver holds a load.
    public bool ReceiverBeamBlocked => (_received || Upper) && LoadX - HalfLoad <= ReceiverLeft && LoadX + HalfLoad >= ReceiverLeft;
    public bool DestinationClear => !ReceiverOccupied;
    public bool AtReceiver => _received && LoadX >= ReceivingStopX - 1e-8;
    public bool TransferFault { get; private set; }
    public string FaultReason { get; private set; } = string.Empty;
    private bool _carried, _received;

    public ChainLiftPlantModel() => Reset();
    public void Reset()
    {
        LoadX = InitialX; Height = 0; _carried = _received = false;
        TransferFault = false; FaultReason = string.Empty;
    }

    public void Step(double seconds, bool chainRun, bool up, bool down)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
            throw new ArgumentException("Chain lift time must be finite and non-negative.");
        if (seconds == 0 || TransferFault) return;
        var remaining = seconds;
        while (remaining > 1e-10 && !TransferFault)
        {
            var tick = Math.Min(remaining, .01);
            StepTick(tick, chainRun, up, down);
            remaining -= tick;
        }
    }

    private void StepTick(double seconds, bool chainRun, bool up, bool down)
    {
        if (up && down) { Fault("Opposing lift commands"); return; }
        if ((up || down) && chainRun) { Fault("Horizontal and vertical drives commanded together"); return; }
        if (up || down)
        {
            // A carton may rise while wholly carried, or the empty carriage
            // may return after discharge. A straddling load cannot be teleported.
            var straddles = !_received && !CartonOnLift && LoadX + HalfLoad > DeckLeft;
            if (straddles) { Fault("Lift motion requested with a carton straddling a transfer edge"); return; }
            Height = Math.Clamp(Height + (up ? 1 : -1) * seconds * Stroke / 4, 0, Stroke);
        }
        if (!chainRun || AtReceiver) return;
        var next = LoadX + seconds * .3;
        if (!_carried && !_received)
        {
            if (!Home && next + HalfLoad > DeckLeft)
            { Fault("Infeed requested across an absent home platform"); return; }
            LoadX = next;
            if (LoadX - HalfLoad >= DeckLeft) _carried = true;
        }
        else if (_carried)
        {
            if (!Upper && next + HalfLoad > DeckRight)
            { Fault("Discharge requested across an absent upper receiving surface"); return; }
            if (!Home && !Upper) { Fault("Horizontal transfer requested between lift endpoints"); return; }
            LoadX = next;
            if (Upper && LoadX - HalfLoad >= DeckRight) { _carried = false; _received = true; }
        }
        else LoadX = next;
        LoadX = Math.Min(LoadX, ReceivingStopX);
    }

    private void Fault(string reason) { TransferFault = true; FaultReason = reason; }
}
