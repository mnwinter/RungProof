using System;
namespace RungProof.Next.Workspace;
public enum FiniteBeltTrialState { Ready, Travelling, Holding, Observed, NeedsAttention, Incomplete, Unavailable }
public sealed record FiniteBeltTrialEpoch(string ProgramIdentity,string LayoutIdentity,long ResetEpoch);
public readonly record struct FiniteBeltTrialObservation(FiniteBeltTrialEpoch Epoch,long ScanNumber,
    double AcceptedTimeSeconds,double CycleSeconds,bool SampledStart,bool EffectiveStart,
    bool SampledBeam,bool EffectiveBeam,bool AppliedRun,double Center,bool PhysicalBeam,bool AtTerminal,
    double InitialCenter,bool Supported,bool CurrentProgramMatches,bool Forced=false,bool BindingsValid=true,bool Running=true);
/// <summary>Passive immutable accepted-trace observer. It cannot command or advance a plant.</summary>
public sealed record FiniteBeltObservedTrial
{
    public FiniteBeltTrialEpoch Epoch { get; init; }=null!;
    public double InitialCenter { get; init; }
    public FiniteBeltTrialState State { get; init; }
    public string Detail { get; init; }="Awaiting a real momentary start from measured Reset home.";
    private long? LastScan { get; init; }
    private double LastTime { get; init; }
    private bool LastStart { get; init; }
    private bool LastPhysicalBeam { get; init; }
    private bool Started { get; init; }
    private bool ReleasedTravel { get; init; }
    private long? FirstBeamScan { get; init; }
    private double FirstBeamCenter { get; init; }
    private double HoldStart { get; init; }
    private double HoldCenter { get; init; }
    private double StartedTime { get; init; }
    public static FiniteBeltObservedTrial Begin(FiniteBeltTrialEpoch epoch,double initialCenter)
        =>new(){Epoch=epoch,InitialCenter=initialCenter,State=double.IsFinite(initialCenter)?FiniteBeltTrialState.Ready:FiniteBeltTrialState.Unavailable};
    public FiniteBeltObservedTrial Invalidate(string reason)=>this with{State=FiniteBeltTrialState.Unavailable,Detail=reason};
    public FiniteBeltObservedTrial Stop()=>State==FiniteBeltTrialState.Observed?this:this with{State=FiniteBeltTrialState.Incomplete,Detail="Stopped before a complete observed trial."};
    public FiniteBeltObservedTrial Observe(FiniteBeltTrialObservation o)
    {
        // Every lifecycle mismatch invalidates even a previously completed result.
        if(o.Epoch!=Epoch||!o.CurrentProgramMatches||o.Forced||!o.BindingsValid
            ||o.SampledStart!=o.EffectiveStart||o.SampledBeam!=o.EffectiveBeam)
            return Invalidate("Program/layout/reset identity, force, or binding image is unavailable.");
        if(!double.IsFinite(o.Center)||!double.IsFinite(o.InitialCenter)||!double.IsFinite(o.AcceptedTimeSeconds)
            ||!double.IsFinite(o.CycleSeconds)||o.CycleSeconds<=0||o.ScanNumber<=0||o.AcceptedTimeSeconds<=0
            ||Math.Abs(o.InitialCenter-InitialCenter)>1e-6||!o.Supported)
            return Invalidate("Measured geometry or accepted time is unavailable.");
        if(!o.Running)return Stop();
        if(LastScan is { } prior&&(o.ScanNumber!=prior+1||Math.Abs(o.AcceptedTimeSeconds-LastTime-o.CycleSeconds)>1e-6))
            return Invalidate("Accepted scan/time sequence is discontinuous.");
        if(State is FiniteBeltTrialState.Unavailable or FiniteBeltTrialState.Incomplete or FiniteBeltTrialState.NeedsAttention)
            return this with{LastScan=o.ScanNumber,LastTime=o.AcceptedTimeSeconds,LastStart=o.SampledStart,LastPhysicalBeam=o.PhysicalBeam};
        if(o.SampledBeam!=LastPhysicalBeam)return Invalidate("Sampled beam does not match the preceding physical observation.");
        var next=this with{LastScan=o.ScanNumber,LastTime=o.AcceptedTimeSeconds,LastStart=o.SampledStart,LastPhysicalBeam=o.PhysicalBeam};
        if(State==FiniteBeltTrialState.Observed)
            return !o.AppliedRun&&o.PhysicalBeam&&Math.Abs(o.Center-HoldCenter)<=1e-6?next:
                next with{State=FiniteBeltTrialState.NeedsAttention,Detail="The previously observed stop is no longer current."};
        if(!Started)
        {
            if(o.PhysicalBeam||o.SampledBeam||o.Center<InitialCenter-1e-6)return Invalidate("Trial did not begin from an unblocked measured home.");
            if(!o.SampledStart){if(Math.Abs(o.Center-InitialCenter)>1e-6)return Invalidate("Movement preceded a real start.");return next;}
            if(o.Center>InitialCenter+.45*o.CycleSeconds+1e-6)
                return Invalidate("Start was not observed within one accepted step of measured Reset home.");
            if(LastStart||!o.AppliedRun)return next;
            next=next with{Started=true,StartedTime=o.AcceptedTimeSeconds,State=FiniteBeltTrialState.Travelling,Detail="Started; observing travel after release."};
        }
        if(o.AcceptedTimeSeconds-next.StartedTime>60)return next with{State=FiniteBeltTrialState.Incomplete,Detail="No complete observed trial within 60 accepted seconds."};
        if(o.AtTerminal&&o.AppliedRun)return next with{State=FiniteBeltTrialState.NeedsAttention,Detail="Carton reached the finite endpoint while the PLC command remained on."};
        if(!o.SampledStart&&o.AppliedRun&&o.Center>InitialCenter+1e-6)next=next with{ReleasedTravel=true};
        if(!LastPhysicalBeam&&o.PhysicalBeam&&next.FirstBeamScan is null)
            next=next with{FirstBeamScan=o.ScanNumber,FirstBeamCenter=o.Center};
        if(next.FirstBeamScan is { } beamScan&&o.ScanNumber==beamScan+1)
        {
            if(!next.ReleasedTravel||!o.SampledBeam||o.AppliedRun||!o.PhysicalBeam
                ||o.Center-next.FirstBeamCenter>.45*o.CycleSeconds+1e-6||o.Center<next.FirstBeamCenter-1e-6)
                return next with{State=FiniteBeltTrialState.NeedsAttention,Detail="The next accepted scan did not stop on physical beam feedback after released travel."};
            return next with{State=FiniteBeltTrialState.Holding,HoldStart=o.AcceptedTimeSeconds,HoldCenter=o.Center,Detail="Stopped at the beam; observing accepted-time hold."};
        }
        if(State==FiniteBeltTrialState.Holding)
        {
            if(o.AppliedRun||!o.PhysicalBeam||!o.SampledBeam||Math.Abs(o.Center-HoldCenter)>1e-6)
                return next with{State=FiniteBeltTrialState.NeedsAttention,Detail="Supported beam stop did not remain stable."};
            if(o.AcceptedTimeSeconds-HoldStart>=.5-1e-9)
                return next with{State=FiniteBeltTrialState.Observed,Detail="Observed one released-start trial stopping at the beam and holding for 0.5 accepted seconds."};
        }
        return next;
    }
}