using System;
using System.Linq;
using RungProof.Next.Workspace;
internal static class FiniteRollerTransportTests
{
    private static int _count;
    private static void Check(bool value,string name){if(!value)throw new Exception(name);_count++;Console.WriteLine("PASS "+name);}
    private static bool Near(double a,double b)=>Math.Abs(a-b)<1e-9;
    private static void Reject(Action action,string name){var rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,name);}
    private static void Main()
    {
        var model=new FiniteRollerTransport(2.525);var initial=model.InitialState;
        var ramp=model.Advance(initial,.75,true);
        Check(Near(ramp.SpeedMps,.45)&&Near(ramp.DriveSurfaceTravelM,.16875),"exact cruise arrival acceleration integral");
        var cruise=model.Advance(initial,1,true);
        Check(Near(cruise.SpeedMps,.45)&&Near(cruise.DriveSurfaceTravelM,.28125),"within-step target arrival splits remaining cruise");
        var decel=model.Advance(ramp,.75,false);
        Check(decel.SpeedMps==0&&Near(decel.DriveSurfaceTravelM-ramp.DriveSurfaceTravelM,.16875),"exact stop arrival braking integral");
        var partial=model.Advance(initial,.2,true);var partialStop=model.Advance(partial,1,false);
        Check(Near(partial.SpeedMps,.12)&&partialStop.SpeedMps==0&&Near(partialStop.DriveSurfaceTravelM,.024),"partly accelerated braking exact");
        foreach(var period in new[]{.02,.04,.1})
        {
            var state=initial;var elapsed=0.0;
            while(elapsed<1-1e-12){var step=Math.Min(period,1-elapsed);state=model.Advance(state,step,true);elapsed+=step;}
            Check(Near(state.SpeedMps,cruise.SpeedMps)&&Near(state.DriveSurfaceTravelM,cruise.DriveSurfaceTravelM),"partition equivalent run "+period);
            elapsed=0;while(elapsed<1-1e-12){var step=Math.Min(period,1-elapsed);state=model.Advance(state,step,false);elapsed+=step;}
            Check(state.SpeedMps==0&&Near(state.DriveSurfaceTravelM,.45),"partition equivalent coast "+period);
        }
        var terminal=model.Advance(initial,100,true);
        Check(terminal.AtTerminal&&Near(terminal.ClampedLoadTravelM,2.525)&&terminal.SpeedMps==.45,"load clamps fully supported while drive command continues");
        var later=model.Advance(terminal,1,true);
        Check(Near(later.DriveSurfaceTravelM-terminal.DriveSurfaceTravelM,.45)&&later.ClampedLoadTravelM==terminal.ClampedLoadTravelM,"drive surface and load travel distinct at endpoint");
        var stopped=model.Stop(later);
        Check(stopped.SpeedMps==0&&stopped.DriveSurfaceTravelM==later.DriveSurfaceTravelM&&stopped.ClampedLoadTravelM==later.ClampedLoadTravelM,"manual Stop zeros speed without phase or load displacement");
        Check(model.Advance(stopped,1,false)==stopped,"paused accepted false image does not move");
        Check(initial==model.InitialState&&later.SpeedMps==.45,"immutable prior states and Reset initial home");
        Check(model.Advance(initial,0,true)==initial,"zero accepted elapsed has no effects");
        foreach(var value in new[]{-1.0,double.NaN,double.PositiveInfinity})Reject(()=>model.Advance(initial,value,true),"reject invalid accepted duration");
        foreach(var value in new[]{0.0,-1,double.NaN,double.PositiveInfinity})Reject(()=>new FiniteRollerTransport(value),"reject invalid supported limit");
        foreach(var bad in new[]{new FiniteRollerTransportState(-.1,0,0,false),new(.46,0,0,false),new(0,double.NaN,0,false),new(0,0,1,false),new(0,3,3,true),new(0,0,0,true)})Reject(()=>model.Advance(bad,.02,true),"reject invalid immutable state");
        var preset=WorkspaceFiniteRollerPolicy.Create();WorkspaceFiniteRollerPolicy.Validate(preset);
        Check(preset.Version==8&&preset.Placements.Count==4&&preset.FiniteRollerPreset==WorkspaceFiniteRollerPolicy.Relation(),"versioned four-asset exact relation");
        var translated=preset with{Placements=preset.Placements.Select(v=>v with{Position=[v.Position[0]+.4,v.Position[1],v.Position[2]+.3]}).ToArray()};
        WorkspaceFiniteRollerPolicy.Validate(translated);Check(true,"complete cell translation valid");
        foreach(var bad in new[]{preset with{Version=7},preset with{FiniteBeltPreset=WorkspaceFiniteBeltPolicy.Relation()},preset with{FiniteRollerPreset=preset.FiniteRollerPreset! with{BeamPoint="wrong"}},preset with{Placements=preset.Placements.Take(3).ToArray()},preset with{SignalLinks=[]},preset with{Placements=preset.Placements.Select((v,i)=>i==2?v with{Position=[0,.398,0]}:v).ToArray()},preset with{Placements=preset.Placements.Select((v,i)=>i==1?v with{Scale=[.5,.5,.5]}:v).ToArray()}})
        {
            var rejected=false;try{WorkspaceFiniteRollerPolicy.Validate(bad);}catch(InvalidOperationException){rejected=true;}
            Check(rejected,"malformed locked relation rejected without changing original");
        }
        Check(preset.Placements[1].Scale.SequenceEqual(new[]{1.0,1,1})&&preset.Placements[2].Position[0]==-1.20,"rejected validation leaves original pallet/sensor unchanged");
        Console.WriteLine("FINITE_ROLLER_TRANSPORT_PASS checks="+_count+" scope=pure-analytic-not-native-bearing-or-optics");
    }
}