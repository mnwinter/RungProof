using System;
using System.Linq;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
namespace RungProof.Next.Lessons;
public enum RepeatCycleTrialStatus { Ready, Running, Observed, NeedsAttention, Incomplete, Unavailable }
public sealed record RepeatCycleTrialIdentity(long Epoch, string SceneId, string PlantVersion, string ProgramFingerprint, string BindingFingerprint, string LayoutFingerprint);
public sealed record RepeatCycleTrialBindings(string Request, string Enabled, string Home, string Busy, string Done, string HeadPosition, string Active, string Complete, string Count);
public sealed record RepeatCyclePlantImage(RepeatCyclePlantModel.CyclePhase Phase, double HomeFraction, double SpindleRadians, bool Home, bool Busy, bool Done);
public sealed record RepeatCycleAcceptedObservation(RepeatCycleTrialIdentity Identity, long ScanNumber, TimeSpan SimulatedTime, TimeSpan ScanPeriod, VirtualControllerSignalImage SampledInputs, VirtualControllerSignalImage EffectiveInputs, VirtualControllerSignalImage AppliedOutputs, VirtualControllerSignalImage PostPlantInputs, RepeatCyclePlantImage BeforePlant, RepeatCyclePlantImage AfterPlant, bool CurrentDraftMatches, bool BindingsValid, bool ForcesPresent);
public sealed record RepeatCycleTrialView(RepeatCycleTrialStatus Status, string Detail, int ActualReturns, long? ClaimedCount, long? TriggerScan, TimeSpan AcceptedElapsed, TimeSpan StationaryHold, bool Historical);
public sealed class RepeatCycleObservedTrial
{
    private const double Tolerance = 1e-8;
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20), HoldRequired = TimeSpan.FromSeconds(.5);
    private readonly RepeatCycleTrialIdentity _identity;
    private readonly RepeatCycleTrialBindings _bindings;
    private readonly bool _aliasesValid;
    private long? _lastScan, _completeScan;
    private TimeSpan _lastTime, _period, _triggerTime;
    private RepeatCyclePlantImage? _lastPlant, _holdPlant;
    private bool _lastEnabledRequest, _released, _pendingAcknowledgement, _finalAcknowledged, _departed;
    private RepeatCyclePlantModel.CyclePhase _chainPhase = RepeatCyclePlantModel.CyclePhase.Idle;
    public RepeatCycleTrialView View { get; private set; } = new(RepeatCycleTrialStatus.Ready,"Awaiting an enabled START from measured Reset home.",0,null,null,TimeSpan.Zero,TimeSpan.Zero,false);
    public RepeatCycleObservedTrial(RepeatCycleTrialIdentity identity, RepeatCycleTrialBindings bindings)
    {
        _identity = identity;
        _bindings = bindings;
        var names = new[] { bindings.Request, bindings.Enabled, bindings.Home, bindings.Busy, bindings.Done,
            bindings.HeadPosition, bindings.Active, bindings.Complete, bindings.Count };
        _aliasesValid = names.All(n => !string.IsNullOrWhiteSpace(n)) && names.Distinct(StringComparer.Ordinal).Count() == names.Length;
    }
    /// <summary>Consumes copied accepted values only; never steps or modifies the controller or plant.</summary>
    public RepeatCycleTrialView Observe(RepeatCycleAcceptedObservation o)
    {
        // Retirement is sticky. A new Reset epoch requires a new observer instance.
        if (View.Historical || View.Status is RepeatCycleTrialStatus.Unavailable or RepeatCycleTrialStatus.NeedsAttention or RepeatCycleTrialStatus.Incomplete)
            return View;
        if (o.Identity != _identity || !o.CurrentDraftMatches)
            return Invalidate("Accepted identity or current draft changed; Reset is required.");
        if (!_aliasesValid || !o.BindingsValid || o.ForcesPresent)
            return UnavailableValue("Exact unforced bindings are unavailable.");
        if (!ReadInputs(o.SampledInputs, o.BeforePlant, out var request, out var enabled)
            || !ReadInputs(o.EffectiveInputs, o.BeforePlant, out var effectiveRequest, out var effectiveEnabled)
            || request != effectiveRequest || enabled != effectiveEnabled
            || !ReadInputs(o.PostPlantInputs, o.AfterPlant, out _, out _)
            || !ReadBool(o.AppliedOutputs, _bindings.Active, out var active)
            || !ReadBool(o.AppliedOutputs, _bindings.Complete, out var complete)
            || !ReadCount(o.AppliedOutputs, out var count))
            return UnavailableValue("Accepted signal types, values or actual feedback agreement are invalid.");
        if (o.ScanPeriod <= TimeSpan.Zero || o.ScanNumber < 0 || o.SimulatedTime < o.ScanPeriod
            || (_lastScan.HasValue && (o.ScanNumber != _lastScan.Value + 1 || o.ScanPeriod != _period || o.SimulatedTime - _lastTime != _period))
            || (_lastPlant is not null && !SamePlant(_lastPlant, o.BeforePlant)))
            return Invalidate("Accepted scan, period, timestamp or plant continuity changed.");
        if (!_lastScan.HasValue && !ResetHome(o.BeforePlant))
            return Invalidate("Trial did not begin at measured Reset home.");
        View = View with { ClaimedCount = count };
        _lastScan = o.ScanNumber; _lastTime = o.SimulatedTime; _period = o.ScanPeriod; _lastPlant = o.AfterPlant;
        bool enabledRequest = request && enabled;
        bool rising = enabledRequest && !_lastEnabledRequest;
        _lastEnabledRequest = enabledRequest;
        if (!View.TriggerScan.HasValue)
        {
            if (!rising)
            {
                if (!ResetHome(o.AfterPlant) || active || complete || count != 0)
                    return Attention("Motion, completion or count preceded the enabled START.");
                return View;
            }
            if (!ResetHome(o.BeforePlant)) return Attention("START did not occur at measured home.");
            _triggerTime = o.SimulatedTime - o.ScanPeriod;
            View = View with { TriggerScan = o.ScanNumber, Status = RepeatCycleTrialStatus.Running, Detail = "Observing actual Feed, Dwell and Return chains." };
        }
        View = View with { AcceptedElapsed = o.SimulatedTime - _triggerTime };
        // The batch must arise from one momentary request. A later request must not
        // make separately started single cycles look like autonomous repetition.
        if (_released && effectiveRequest)
            return Attention("Additional START after release: the three-cycle batch must use one momentary START.");
        if (!request) _released = true;
        if (View.AcceptedElapsed > Deadline && View.Status != RepeatCycleTrialStatus.Observed) return Timeout();
        if (complete && View.ActualReturns < 3) return Attention("PLC completion preceded three actual returns.");
        // A new actual Complete at N is counted here. PLC count is checked at exactly N+1.
        if (_completeScan.HasValue && o.ScanNumber == _completeScan.Value + 1)
        {
            if (!o.BeforePlant.Done || count != View.ActualReturns)
                return Attention("Mapped count must increase once at the first accepted Done sample (N+1).");
            _pendingAcknowledgement = true;
            if (View.ActualReturns == 3)
            {
                if (active || !complete || request || !_released)
                    return Attention("After the third Done sample, release START, stop command and retain completion/count 3.");
                if (!ResetHome(o.AfterPlant)) return Attention("Third completion was not acknowledged at actual home.");
                _pendingAcknowledgement = false; _finalAcknowledged = true; _holdPlant = o.AfterPlant;
                return AtDeadlineOrView(); // acknowledgement interval earns no stationary hold
            }
        }
        else
        {
            // At N the actual return is counted below, after checking the preceding PLC count.
            if (count != View.ActualReturns) return Attention("Count changed without the required accepted completion edge.");
        }
        if (_finalAcknowledged)
        {
            if (active || !complete || count != 3 || request || !SamePlant(_holdPlant!, o.AfterPlant))
                return Attention("Final stationary hold changed command, completion, count, START or actual pose.");
            View = View with { StationaryHold = View.StationaryHold + o.ScanPeriod };
            if (View.StationaryHold >= HoldRequired)
                return View = View with { Status = RepeatCycleTrialStatus.Observed, Detail = "Three actual returns, count 3 and accepted stationary hold observed." };
            return AtDeadlineOrView();
        }
        if (_pendingAcknowledgement)
        {
            if (!active && ResetHome(o.AfterPlant))
            { _pendingAcknowledgement = false; _chainPhase = RepeatCyclePlantModel.CyclePhase.Idle; _departed = false; }
            else if (!o.AfterPlant.Done || o.AfterPlant.Phase != RepeatCyclePlantModel.CyclePhase.Complete)
                return Attention("Completion changed before actual low-command acknowledgement.");
            return AtDeadlineOrView();
        }
        var before = o.BeforePlant.Phase; var after = o.AfterPlant.Phase;
        if (before == RepeatCyclePlantModel.CyclePhase.Idle && after == RepeatCyclePlantModel.CyclePhase.Feed)
        {
            if (View.ActualReturns >= 3) return Attention("A fourth departure occurred.");
            _chainPhase = after; _departed = !o.AfterPlant.Home;
        }
        else if (before == after)
        {
            if (after == RepeatCyclePlantModel.CyclePhase.Feed && !o.AfterPlant.Home) _departed = true;
        }
        else if (before == _chainPhase &&
            ((before == RepeatCyclePlantModel.CyclePhase.Feed && after == RepeatCyclePlantModel.CyclePhase.Dwell)
            || (before == RepeatCyclePlantModel.CyclePhase.Dwell && after == RepeatCyclePlantModel.CyclePhase.Return)))
            _chainPhase = after;
        else if (before == RepeatCyclePlantModel.CyclePhase.Return && _chainPhase == before
            && after == RepeatCyclePlantModel.CyclePhase.Complete && _departed && o.AfterPlant.Home && !o.BeforePlant.Done)
        {
            View = View with { ActualReturns = View.ActualReturns + 1 };
            _completeScan = o.ScanNumber; _chainPhase = after;
        }
        else return Attention("Actual phase order or acknowledgement is invalid.");
        return AtDeadlineOrView();
    }
    private RepeatCycleTrialView AtDeadlineOrView() => View.AcceptedElapsed >= Deadline && View.Status != RepeatCycleTrialStatus.Observed ? Timeout() : View;
    private RepeatCycleTrialView Timeout() => View = View with { Status = RepeatCycleTrialStatus.Incomplete, Detail = "20 accepted seconds elapsed before the full trial and stationary hold completed." };
    private RepeatCycleTrialView Attention(string detail) => View = View with { Status = RepeatCycleTrialStatus.NeedsAttention, Detail = detail };
    private RepeatCycleTrialView UnavailableValue(string reason)
    { View = View with { ClaimedCount = null }; return Invalidate(reason); }
    public RepeatCycleTrialView Invalidate(string reason) => View = View with { Status = RepeatCycleTrialStatus.Unavailable, Detail = reason, Historical = true };
    public RepeatCycleTrialView Stop() => View = View with { Status = View.Status is RepeatCycleTrialStatus.Ready or RepeatCycleTrialStatus.Running ? RepeatCycleTrialStatus.Incomplete : View.Status, Detail = View.Status is RepeatCycleTrialStatus.Ready or RepeatCycleTrialStatus.Running ? "Stopped before observed success; Reset is required for a fresh trial." : View.Detail, Historical = true };
    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) && Math.Abs(a - b) <= Tolerance;
    private static bool ResetHome(RepeatCyclePlantImage p) => p.Phase == RepeatCyclePlantModel.CyclePhase.Idle && p.Home && !p.Busy && !p.Done && Near(p.HomeFraction, 1);
    private static bool SamePlant(RepeatCyclePlantImage a, RepeatCyclePlantImage b) => a.Phase == b.Phase && a.Home == b.Home && a.Busy == b.Busy && a.Done == b.Done && Near(a.HomeFraction,b.HomeFraction) && Near(a.SpindleRadians,b.SpindleRadians);
    private static bool ReadBool(VirtualControllerSignalImage image, string name, out bool value) => image.Bools.TryGetValue(name,out value) && !image.Numbers.ContainsKey(name) && !image.Text.ContainsKey(name);
    private bool ReadCount(VirtualControllerSignalImage image, out long count)
    {
        count = 0;
        if (!image.Numbers.TryGetValue(_bindings.Count,out var n) || image.Bools.ContainsKey(_bindings.Count) || image.Text.ContainsKey(_bindings.Count)
            || !double.IsFinite(n) || n != Math.Truncate(n) || n < int.MinValue || n > int.MaxValue) return false;
        count = (long)n; return true;
    }
    private bool ReadInputs(VirtualControllerSignalImage image, RepeatCyclePlantImage plant, out bool request, out bool enabled)
    {
        request = enabled = false;
        bool phaseValid = Enum.IsDefined(plant.Phase) && double.IsFinite(plant.SpindleRadians)
            && double.IsFinite(plant.HomeFraction) && plant.HomeFraction >= 0 && plant.HomeFraction <= 1
            && plant.Home == (plant.HomeFraction >= 1 - Tolerance)
            && plant.Busy == (plant.Phase is RepeatCyclePlantModel.CyclePhase.Feed or RepeatCyclePlantModel.CyclePhase.Dwell or RepeatCyclePlantModel.CyclePhase.Return)
            && plant.Done == (plant.Phase == RepeatCyclePlantModel.CyclePhase.Complete);
        return phaseValid && ReadBool(image,_bindings.Request,out request) && ReadBool(image,_bindings.Enabled,out enabled)
            && ReadBool(image,_bindings.Home,out var home) && home == plant.Home
            && ReadBool(image,_bindings.Busy,out var busy) && busy == plant.Busy
            && ReadBool(image,_bindings.Done,out var done) && done == plant.Done
            && image.Numbers.TryGetValue(_bindings.HeadPosition,out var position) && !image.Bools.ContainsKey(_bindings.HeadPosition)
            && !image.Text.ContainsKey(_bindings.HeadPosition) && Near(position,plant.HomeFraction * 100);
    }
}

