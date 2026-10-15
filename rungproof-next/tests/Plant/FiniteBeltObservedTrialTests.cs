using System;
using RungProof.Next.Workspace;
internal static class FiniteBeltObservedTrialTests
{
    private static readonly FiniteBeltTrialEpoch Epoch=new("programA","layoutA",1);
    private static int _checks;
    private static void Check(bool condition,string name){if(!condition)throw new Exception(name);_checks++;Console.WriteLine("PASS "+name);}
    private static FiniteBeltTrialObservation Frame(long scan,double period,bool start,bool sampledBeam,bool run,double center,bool physical=false,bool terminal=false)
        =>new(Epoch,scan,scan*period,period,start,start,sampledBeam,sampledBeam,run,center,physical,terminal,.3,true,true);
    private static FiniteBeltObservedTrial ToHold(double period)
    {
        var trial=FiniteBeltObservedTrial.Begin(Epoch,.3);
        var first=Frame(1,period,true,false,true,.3+.45*period);
        var immutable=first;trial=trial.Observe(first);Check(first==immutable,"readonly observation unchanged");
        trial=trial.Observe(Frame(2,period,false,false,true,.3+.9*period));
        trial=trial.Observe(Frame(3,period,false,false,true,1,true));
        trial=trial.Observe(Frame(4,period,false,true,false,1,true));
        Check(trial.State==FiniteBeltTrialState.Holding,"exact next scan enters hold period "+period);
        return trial;
    }
    private static void Main()
    {
        foreach(var period in new[]{.02,.05,.1})
        {
            var trial=ToHold(period);var old=trial;
            var holdScans=(int)Math.Ceiling(.5/period);
            for(var i=1;i<holdScans;i++)trial=trial.Observe(Frame(4+i,period,false,true,false,1,true));
            Check(trial.State==FiniteBeltTrialState.Holding,"no premature accepted-time result "+period);
            trial=trial.Observe(Frame(4+holdScans,period,false,true,false,1,true));
            Check(trial.State==FiniteBeltTrialState.Observed,"half-second accepted hold "+period);
            Check(trial.Observe(Frame(5+holdScans,period,false,true,true,1,true)).State==FiniteBeltTrialState.NeedsAttention,"completed result loses current status on restart");
            Check(trial.Observe(Frame(5+holdScans,period,false,true,false,1.01,true)).State==FiniteBeltTrialState.NeedsAttention,"completed result loses current status on movement");
            Check(trial.Observe(Frame(5+holdScans,period,false,true,false,1,true) with{CurrentProgramMatches=false}).State==FiniteBeltTrialState.Unavailable,"completed result mismatch invalidation");
            Check(trial.Stop().State==FiniteBeltTrialState.Observed,"manual stop retains completed historical observation");
            Check(old.State==FiniteBeltTrialState.Holding,"immutable observer prior state remains");
            Check(trial.Observe(Frame(5+holdScans,period,false,true,false,1,true) with{Epoch=Epoch with{ResetEpoch=2}}).State==FiniteBeltTrialState.Unavailable,"reset invalidates previous completion");
        }
        var begun=FiniteBeltObservedTrial.Begin(Epoch,.3);
        Check(begun.Observe(Frame(1,.02,true,false,true,2)).State==FiniteBeltTrialState.Unavailable,"midtravel initial start cannot qualify");
        Check(begun.Observe(Frame(0,.02,true,false,true,.3)).State==FiniteBeltTrialState.Unavailable,"nonpositive accepted scan rejected");
        Check(begun.Observe(Frame(1,.02,false,false,false,1,true)).State==FiniteBeltTrialState.Unavailable,"pre-existing beam stop cannot satisfy trial");
        Check(begun.Observe(Frame(1,.02,false,false,true,.5)).State==FiniteBeltTrialState.Unavailable,"movement without request rejected");
        var travel=begun.Observe(Frame(1,.02,true,false,true,.309)).Observe(Frame(2,.02,false,false,true,.318));
        Check(travel.Stop().State==FiniteBeltTrialState.Incomplete,"manual stop unfinished trial incomplete");
        Check(travel.Observe(Frame(3,.02,false,false,true,5,true,true)).State==FiniteBeltTrialState.NeedsAttention,"finite endpoint with command on needs attention");
        var beam=travel.Observe(Frame(3,.02,false,false,true,1,true));
        Check(beam.Observe(Frame(4,.02,false,true,true,1,true)).State==FiniteBeltTrialState.NeedsAttention,"wrong command after beam not corrected");
        var wrongResult=beam.Observe(Frame(4,.02,false,true,true,1,true));
        for(var scan=5;scan<15;scan++)wrongResult=wrongResult.Observe(Frame(scan,.02,false,true,true,1,true));
        Check(wrongResult.State==FiniteBeltTrialState.NeedsAttention,"continued accepted scans preserve wrong-beam result");
        Check(wrongResult.Observe(Frame(15,.02,false,true,true,1,true) with{CurrentProgramMatches=false}).State==FiniteBeltTrialState.Unavailable,"wrong result still invalidated by changed identity");
        var terminalResult=travel.Observe(Frame(3,.02,false,false,true,5,false,true));
        for(var scan=4;scan<14;scan++)terminalResult=terminalResult.Observe(Frame(scan,.02,false,false,true,5,false,true));
        Check(terminalResult.State==FiniteBeltTrialState.NeedsAttention,"continued accepted scans preserve finite-terminal result");
        Check(beam.Observe(Frame(4,.02,false,true,false,1.02,true)).State==FiniteBeltTrialState.NeedsAttention,"next-scan displacement exceeds actual period bound");
        Check(beam.Observe(Frame(5,.02,false,true,false,1,true)).State==FiniteBeltTrialState.Unavailable,"skipped next scan cannot pass");
        Check(travel.Observe(Frame(3,.02,false,true,true,.327)).State==FiniteBeltTrialState.Unavailable,"invented feedback rejected");
        foreach(var bad in new[]{Frame(3,.02,false,false,true,.327) with{Forced=true},Frame(3,.02,false,false,true,.327) with{BindingsValid=false},Frame(3,.02,false,false,true,.327) with{CurrentProgramMatches=false},Frame(3,.02,false,false,true,.327) with{EffectiveStart=true},Frame(3,.02,false,false,true,.327) with{Supported=false},Frame(3,.02,false,false,true,.327) with{CycleSeconds=double.NaN}})
            Check(travel.Observe(bad).State==FiniteBeltTrialState.Unavailable,"invalid identity/image/geometry unavailable");
        var held=ToHold(.02);Check(held.Observe(Frame(5,.02,false,true,false,1.01,true)).State==FiniteBeltTrialState.NeedsAttention,"hold actual movement fails");
        var timeout=begun.Observe(Frame(1,31,true,false,true,.31)).Observe(Frame(2,31,false,false,true,.32)).Observe(Frame(3,31,false,false,true,.33));
        Check(timeout.State==FiniteBeltTrialState.Incomplete,"accepted-time timeout incomplete rather than wrong code");
        Check(FiniteBeltObservedTrial.Begin(Epoch with{LayoutIdentity="new"},.3).State==FiniteBeltTrialState.Ready,"new layout epoch starts fresh");
        Console.WriteLine("FINITE_BELT_OBSERVER_PASS checks="+_checks+" scope=pure-passive-observer-not-native-plant-trace-equivalence");
    }
}