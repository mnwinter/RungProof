using System;
namespace RungProof.Next.Workspace;
public readonly record struct FiniteRollerTransportState(double SpeedMps,double DriveSurfaceTravelM,double ClampedLoadTravelM,bool AtTerminal);
/// <summary>Pure accepted-step kinematics. Positive distance means the preset's negative-X direction.</summary>
public sealed class FiniteRollerTransport
{
    public const double TargetSpeedMps=.45;
    public const double AccelerationMps2=.60;
    public const double RollerRadiusM=.038;
    public double SupportedTravelLimit { get; }
    public FiniteRollerTransportState InitialState=>new(0,0,0,false);
    public FiniteRollerTransport(double supportedTravelLimit)
    {
        if(!double.IsFinite(supportedTravelLimit)||supportedTravelLimit<=0)
            throw new ArgumentException("Supported travel limit must be finite and positive.");
        SupportedTravelLimit=supportedTravelLimit;
    }
    private void Validate(FiniteRollerTransportState s)
    {
        if(!double.IsFinite(s.SpeedMps)||!double.IsFinite(s.DriveSurfaceTravelM)||!double.IsFinite(s.ClampedLoadTravelM)
            ||s.SpeedMps<0||s.SpeedMps>TargetSpeedMps+1e-12||s.DriveSurfaceTravelM<0||s.ClampedLoadTravelM<0
            ||s.ClampedLoadTravelM>SupportedTravelLimit||s.DriveSurfaceTravelM+1e-9<s.ClampedLoadTravelM
            ||s.AtTerminal!=(s.ClampedLoadTravelM>=SupportedTravelLimit))
            throw new ArgumentException("Transport state does not represent supported finite forward travel.");
    }
    public FiniteRollerTransportState Advance(FiniteRollerTransportState state,double acceptedSeconds,bool run)
    {
        Validate(state);
        if(!double.IsFinite(acceptedSeconds)||acceptedSeconds<0)
            throw new ArgumentException("Accepted duration must be finite and non-negative.");
        var target=run?TargetSpeedMps:0;
        var change=target-state.SpeedMps;
        var rampTime=Math.Min(acceptedSeconds,Math.Abs(change)/AccelerationMps2);
        var acceleration=Math.Sign(change)*AccelerationMps2;
        var distance=state.SpeedMps*rampTime+.5*acceleration*rampTime*rampTime
            +target*(acceptedSeconds-rampTime);
        var speed=rampTime>=Math.Abs(change)/AccelerationMps2?target:state.SpeedMps+acceleration*rampTime;
        var surface=state.DriveSurfaceTravelM+distance;
        var load=Math.Min(SupportedTravelLimit,state.ClampedLoadTravelM+distance);
        if(!double.IsFinite(surface)||!double.IsFinite(load))throw new ArgumentException("Accepted duration overflows finite travel.");
        return new(speed,surface,load,load>=SupportedTravelLimit);
    }
    // Explicit simulation Stop freezes phase/load immediately; a false PLC run
    // command instead decelerates through Advance. Reset uses InitialState.
    public FiniteRollerTransportState Stop(FiniteRollerTransportState state)
    {Validate(state);return state with{SpeedMps=0};}
}