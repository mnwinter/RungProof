using System;
using RungProof.Next.Workspace;
internal static class Program
{
    static int checks;
    static void Check(bool condition,string name){checks++;if(!condition)throw new Exception(name);}
    static readonly FiniteRollerTrialEpoch Epoch=new("program","layout",1);
    // Protocol fixtures use actual pure ramp integration; the synthetic wood interval is not native optics proof.
    sealed class Trial
    {
        public readonly FiniteRollerTransport Model=new(2.525);
        public FiniteRollerTransportState Plant;
        public FiniteRollerObservedTrial Observer=FiniteRollerObservedTrial.Begin(Epoch,2.525);
        public long Scan;public double Time;public bool Beam;
        public FiniteRollerTrialObservation Tick(double dt,bool start,bool run)
        {
            bool sampled=Beam;Plant=Model.Advance(Plant,dt,run);Scan++;Time+=dt;
            Beam=Plant.ClampedLoadTravelM>=.7&&Plant.ClampedLoadTravelM<.78;
            var o=new FiniteRollerTrialObservation(Epoch,Scan,Time,dt,start,start,sampled,sampled,run,Plant,Beam,true,true);
            Observer=Observer.Observe(o);return o;
        }
    }
    static void Main()
    {
        foreach(double dt in new[]{.02,.04,.10})
        {
            var t=new Trial();t.Tick(dt,true,true);
            while(!t.Beam)t.Tick(dt,false,true);
            var trigger=t.Observer.BeamTriggerScanNumber;
            t.Tick(dt,false,false);
            Check(t.Observer.State==FiniteRollerTrialState.Running,"command off precedes settled result");
            while(t.Plant.SpeedMps>1e-9)t.Tick(dt,false,false);
            Check(!t.Beam,"realistic short solid interval clears during coast");
            double settled=t.Time;
            while(t.Time-settled<.5-1e-9){t.Tick(dt,false,false);if(t.Time-settled<.5-1e-9)Check(t.Observer.State!=FiniteRollerTrialState.Observed,"hold duration uses accepted time");}
            Check(t.Observer.State==FiniteRollerTrialState.Observed,"intended observed at period "+dt);
            Check(t.Observer.BeamTriggerScanNumber==trigger,"trigger history independent of current clear beam");
            var stopped=t.Observer.Stop();Check(stopped.State==FiniteRollerTrialState.Observed,"completed stop historical");
            var observation=t.Tick(dt,false,false);
            Check(t.Observer.State==FiniteRollerTrialState.Observed,"completed history continues");
            Check(t.Observer.Observe(observation with{ScanNumber=observation.ScanNumber+1,AcceptedTimeSeconds=observation.AcceptedTimeSeconds+dt,Forced=true}).State==FiniteRollerTrialState.Unavailable,"force retires completed result");
            t.Tick(dt,false,true);Check(t.Observer.State==FiniteRollerTrialState.NeedsAttention,"completed restart changes result");
            var wrong=new Trial();wrong.Tick(dt,true,true);while(!wrong.Beam)wrong.Tick(dt,false,true);wrong.Tick(dt,false,true);
            Check(wrong.Observer.State==FiniteRollerTrialState.NeedsAttention,"wrong next scan command detected");
            var failedNext=wrong.Observer;Check(failedNext.Stop()==failedNext,"Stop preserves next-scan failure detail trigger and immutable history");
            for(int i=0;i<5;i++)wrong.Tick(dt,false,true);
            Check(wrong.Observer.State==FiniteRollerTrialState.NeedsAttention&&wrong.Observer.LastScanNumber==wrong.Scan,"wrong result updates accepted history");
            while(!wrong.Plant.AtTerminal)wrong.Tick(dt,false,true);
            Check(wrong.Observer.State==FiniteRollerTrialState.NeedsAttention,"wrong endpoint never success");
            Check(wrong.Observer.Stop()==wrong.Observer,"Stop preserves wrong-terminal failure evidence");
        }
        var first=new Trial();var f=first.Tick(.02,true,true);
        var fresh=FiniteRollerObservedTrial.Begin(Epoch,2.525);
        Check(fresh.State==FiniteRollerTrialState.Ready&&fresh.LastScanNumber is null,"immutable begin remains untouched");
        Check(fresh.Stop().State==FiniteRollerTrialState.Incomplete,"ready stop incomplete");
        var unavailable=fresh.Invalidate("source mismatch");Check(unavailable.Stop()==unavailable,"Stop preserves unavailable provenance");
        Check(fresh.Observe(f with{Transport=new(.45,1,1,false)}).State==FiniteRollerTrialState.Unavailable,"midtravel start rejected");
        Check(fresh.Observe(f with{PhysicalBeam=true}).State==FiniteRollerTrialState.Unavailable,"preblocked start rejected");
        Check(fresh.Observe(f with{ScanNumber=0}).State==FiniteRollerTrialState.Unavailable,"zero scan rejected");
        Check(fresh.Observe(f with{AcceptedTimeSeconds=0}).State==FiniteRollerTrialState.Unavailable,"zero accepted time rejected");
        Check(fresh.Observe(f with{Supported=false}).State==FiniteRollerTrialState.Unavailable,"unsupported load rejected");
        Check(fresh.Observe(f with{CurrentProgramMatches=false}).State==FiniteRollerTrialState.Unavailable,"changed draft rejected");
        Check(fresh.Observe(f with{Epoch=Epoch with{ResetEpoch=2}}).State==FiniteRollerTrialState.Unavailable,"reset epoch retires");
        Check(fresh.Observe(f with{BindingsValid=false}).State==FiniteRollerTrialState.Unavailable,"invalid bindings retire");
        Check(fresh.Observe(f with{EffectiveStart=false}).State==FiniteRollerTrialState.Unavailable,"raw effective mismatch retires");
        var held=new Trial();held.Tick(.02,true,false);held.Tick(.02,true,true);Check(held.Observer.State==FiniteRollerTrialState.Unavailable,"held request cannot become new rising start");
        var gap=new Trial();var g=gap.Tick(.02,true,true);Check(gap.Observer.Observe(g with{ScanNumber=3,AcceptedTimeSeconds=.06}).State==FiniteRollerTrialState.Unavailable,"skipped scan rejected");
        Check(first.Observer.Stop().State==FiniteRollerTrialState.Incomplete,"early manual stop incomplete");
        Check(fresh.Observe(f with{SampledBeam=true,EffectiveBeam=true}).State==FiniteRollerTrialState.Unavailable,"invented sampled beam rejected");
        Console.WriteLine("FINITE_ROLLER_OBSERVER_PASS checks="+checks+" periods=20/40/100ms pureProtocolOnly=true");
    }
}

