using System;
namespace RungProof.Next.Workspace;
public enum FiniteRollerTrialState { Ready,Running,Observed,NeedsAttention,Incomplete,Unavailable }
public sealed record FiniteRollerTrialEpoch(string ProgramIdentity,string LayoutIdentity,long ResetEpoch);
public readonly record struct FiniteRollerTrialObservation(FiniteRollerTrialEpoch Epoch,long ScanNumber,
    double AcceptedTimeSeconds,double CycleSeconds,bool SampledStart,bool EffectiveStart,bool SampledBeam,bool EffectiveBeam,
    bool AppliedRun,FiniteRollerTransportState Transport,bool PhysicalBeam,bool Supported,bool CurrentProgramMatches,
    bool Forced=false,bool BindingsValid=true,bool Running=true);
/// <summary>Passive immutable observer of a reset-home roller trial; it owns no clock or command writer.</summary>
public sealed record FiniteRollerObservedTrial
{
    private const double Epsilon=1e-6;
    public FiniteRollerTrialEpoch Epoch { get; init; }=null!;
    public FiniteRollerTrialState State { get; init; }
    public string Detail { get; init; }="Awaiting a momentary start from measured Reset home.";
    public long? LastScanNumber { get; init; }
    public double LastTimeSeconds { get; init; }
    private double SupportedLimit { get; init; }
    private FiniteRollerTransportState PreviousTransport { get; init; }
    private bool PreviousStart { get; init; }
    private bool PreviousBeam { get; init; }
    private bool Started { get; init; }
    private bool ReleasedTravel { get; init; }
    private double StartTime { get; init; }
    private long? TriggerScan { get; init; }
    public long? BeamTriggerScanNumber => TriggerScan;
    private double TriggerDriveTravel { get; init; }
    private double TriggerSpeed { get; init; }
    private bool CommandOffObserved { get; init; }
    private double? HoldStart { get; init; }
    private double HoldLoad { get; init; }
    private double HoldDrive { get; init; }
    public static FiniteRollerObservedTrial Begin(FiniteRollerTrialEpoch epoch,double supportedTravelLimit)
    {
        _=new FiniteRollerTransport(supportedTravelLimit);
        return new(){Epoch=epoch,SupportedLimit=supportedTravelLimit,State=FiniteRollerTrialState.Ready};
    }
    public FiniteRollerObservedTrial Invalidate(string reason)=>this with{State=FiniteRollerTrialState.Unavailable,Detail=reason};
    public FiniteRollerObservedTrial Stop()=>State is FiniteRollerTrialState.Observed or FiniteRollerTrialState.NeedsAttention or FiniteRollerTrialState.Unavailable?this:
        this with{State=FiniteRollerTrialState.Incomplete,Detail="Stopped before a complete observed deceleration trial."};
    public FiniteRollerObservedTrial Observe(FiniteRollerTrialObservation o)
    {
        if(o.Epoch!=Epoch||!o.CurrentProgramMatches||o.Forced||!o.BindingsValid
            ||o.SampledStart!=o.EffectiveStart||o.SampledBeam!=o.EffectiveBeam)
            return Invalidate("Current program, layout, reset, forcing or binding image is unavailable.");
        if(!o.Supported||o.ScanNumber<=0||!double.IsFinite(o.AcceptedTimeSeconds)||o.AcceptedTimeSeconds<=0
            ||!double.IsFinite(o.CycleSeconds)||o.CycleSeconds<=0)
            return Invalidate("Supported geometry or accepted time is unavailable.");
        if(!o.Running)return Stop();
        if(LastScanNumber is { } scan&&(o.ScanNumber!=scan+1||Math.Abs(o.AcceptedTimeSeconds-LastTimeSeconds-o.CycleSeconds)>Epsilon))
            return Invalidate("Accepted scan/time sequence is discontinuous.");
        FiniteRollerTransportState expected;
        try{expected=new FiniteRollerTransport(SupportedLimit).Advance(PreviousTransport,o.CycleSeconds,o.AppliedRun);}
        catch(ArgumentException){return Invalidate("Transport state is not finite supported kinematics.");}
        var t=o.Transport;
        if(!double.IsFinite(t.SpeedMps)||!double.IsFinite(t.DriveSurfaceTravelM)||!double.IsFinite(t.ClampedLoadTravelM)
            ||Math.Abs(t.SpeedMps-expected.SpeedMps)>Epsilon||Math.Abs(t.DriveSurfaceTravelM-expected.DriveSurfaceTravelM)>Epsilon
            ||Math.Abs(t.ClampedLoadTravelM-expected.ClampedLoadTravelM)>Epsilon||t.AtTerminal!=expected.AtTerminal)
            return Invalidate("Observed motion does not match the accepted ramp/coast step.");
        if(o.SampledBeam!=PreviousBeam)return Invalidate("Sampled beam differs from preceding actual solid-wood detection.");
        var next=this with{LastScanNumber=o.ScanNumber,LastTimeSeconds=o.AcceptedTimeSeconds,PreviousTransport=t,PreviousStart=o.SampledStart,PreviousBeam=o.PhysicalBeam};
        if(State is FiniteRollerTrialState.Unavailable or FiniteRollerTrialState.Incomplete or FiniteRollerTrialState.NeedsAttention)return next;
        if(State==FiniteRollerTrialState.Observed)
            return !o.AppliedRun&&t.SpeedMps<=Epsilon&&Math.Abs(t.ClampedLoadTravelM-HoldLoad)<=Epsilon
                &&Math.Abs(t.DriveSurfaceTravelM-HoldDrive)<=Epsilon?next:
                next with{State=FiniteRollerTrialState.NeedsAttention,Detail="The previously observed settled stop is no longer current."};
        if(!Started)
        {
            if(o.PhysicalBeam||o.SampledBeam)return Invalidate("Pre-existing wood detection cannot start a fresh trial.");
            if(!o.SampledStart)
            {
                if(t.ClampedLoadTravelM>Epsilon||t.SpeedMps>Epsilon)return Invalidate("Motion preceded a real start request.");
                return o.AcceptedTimeSeconds>60?next with{State=FiniteRollerTrialState.Incomplete,Detail="No start trial within 60 accepted seconds."}:next;
            }
            if(!o.AppliedRun)return next;
            if(PreviousStart)return Invalidate("A held request is not a newly observed rising start.");
            var firstStep=new FiniteRollerTransport(SupportedLimit).Advance(default,o.CycleSeconds,true);
            if(t.ClampedLoadTravelM>firstStep.ClampedLoadTravelM+Epsilon)return Invalidate("Start was observed away from measured Reset home.");
            next=next with{Started=true,StartTime=o.AcceptedTimeSeconds,State=FiniteRollerTrialState.Running,Detail="Observing released travel and an actual wood-beam event."};
        }
        if(o.AcceptedTimeSeconds-next.StartTime>60)return next with{State=FiniteRollerTrialState.Incomplete,Detail="No completed trial within 60 accepted seconds."};
        if(t.AtTerminal&&o.AppliedRun)return next with{State=FiniteRollerTrialState.NeedsAttention,Detail="Finite load boundary reached while the roller command remains on."};
        if(!o.SampledStart&&o.AppliedRun&&t.ClampedLoadTravelM>Epsilon)next=next with{ReleasedTravel=true};
        if(!PreviousBeam&&o.PhysicalBeam&&next.TriggerScan is null)
            next=next with{TriggerScan=o.ScanNumber,TriggerDriveTravel=t.DriveSurfaceTravelM,TriggerSpeed=t.SpeedMps};
        if(next.TriggerScan is { } trigger&&o.ScanNumber==trigger+1)
        {
            if(!next.ReleasedTravel||!o.SampledBeam||o.AppliedRun)
                return next with{State=FiniteRollerTrialState.NeedsAttention,Detail="The next accepted scan did not command off after the actual beam event."};
            next=next with{CommandOffObserved=true,Detail="Command off after wood detection; observing deceleration and settling."};
        }
        if(next.CommandOffObserved)
        {
            if(o.AppliedRun||t.DriveSurfaceTravelM-next.TriggerDriveTravel>next.TriggerSpeed*next.TriggerSpeed/(2*FiniteRollerTransport.AccelerationMps2)+Epsilon)
                return next with{State=FiniteRollerTrialState.NeedsAttention,Detail="Command restarted or coast exceeded the observed-speed stopping bound."};
            if(t.SpeedMps<=Epsilon)
            {
                if(next.HoldStart is null)return next with{HoldStart=o.AcceptedTimeSeconds,HoldLoad=t.ClampedLoadTravelM,HoldDrive=t.DriveSurfaceTravelM,Detail="Settled; observing supported stationary hold (current beam may be clear)."};
                if(Math.Abs(t.ClampedLoadTravelM-next.HoldLoad)>Epsilon||Math.Abs(t.DriveSurfaceTravelM-next.HoldDrive)>Epsilon)
                    return next with{State=FiniteRollerTrialState.NeedsAttention,Detail="The settled load or roller phase moved during hold."};
                if(o.AcceptedTimeSeconds-next.HoldStart.Value>=.5-1e-9)
                    return next with{State=FiniteRollerTrialState.Observed,Detail="Observed a released-start trial decelerating after a wood-beam event and holding supported for 0.5 accepted seconds."};
            }
        }
        return next;
    }
}