using System;
using System.Collections.Generic;
using RungProof.Next.Lessons;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
internal static class Program
{
    private static int checks;
    private static void Check(bool condition,string name) { checks++; if(!condition) throw new Exception(name); }
    private static readonly RepeatCycleTrialIdentity Id=new(1,"lab-4-03-repeat-cycle-counter","cnc-dry-stroke-v1","p","b","l");
    private static readonly RepeatCycleTrialBindings Bind=new("startAlias","enableAlias","homeAlias","busyAlias","doneAlias","positionAlias","activeAlias","completeAlias","countAlias");
    private sealed class Rig
    {
        public readonly RepeatCyclePlantModel Plant=new();
        public readonly RepeatCycleObservedTrial Trial=new(Id,Bind);
        public long Scan; public TimeSpan Time; public int Count; public bool Complete; public readonly TimeSpan Period;
        public Rig(int ms) { Period=TimeSpan.FromMilliseconds(ms); }
        private RepeatCyclePlantImage Image()=>new(Plant.Phase,Plant.HomeFraction,Plant.SpindleRadians,Plant.Home,Plant.Busy,Plant.Done);
        private VirtualControllerSignalImage Inputs(bool request)=>new(new Dictionary<string,bool>{{Bind.Request,request},{Bind.Enabled,true},{Bind.Home,Plant.Home},{Bind.Busy,Plant.Busy},{Bind.Done,Plant.Done}},new Dictionary<string,double>{{Bind.HeadPosition,Plant.HomeFraction*100}},new Dictionary<string,string>());
        public RepeatCycleAcceptedObservation Tick(bool request,bool active,bool increment=true, bool force=false)
        {
            var before=Image(); var inputs=Inputs(request);
            if(Plant.Done&&increment) Count++;
            Complete=Count==3;
            var outputs=new VirtualControllerSignalImage(new Dictionary<string,bool>{{Bind.Active,active},{Bind.Complete,Complete}},new Dictionary<string,double>{{Bind.Count,Count}},new Dictionary<string,string>());
            Plant.Step(Period.TotalSeconds,active); Time+=Period;
            var o=new RepeatCycleAcceptedObservation(Id,++Scan,Time,Period,inputs,inputs,outputs,Inputs(request),before,Image(),true,true,force);
            Trial.Observe(o); return o;
        }
        public void Return(bool first=false)
        {
            while(!Plant.Done) Tick(first&&Scan==0,true);
        }
        public void Three(bool release=true)
        {
            for(int i=0;i<3;i++) { Return(i==0); Tick(!release,false); }
        }
    }
    private static VirtualControllerSignalImage Outputs(double count,bool active=false,bool complete=false)=>new(new Dictionary<string,bool>{{Bind.Active,active},{Bind.Complete,complete}},new Dictionary<string,double>{{Bind.Count,count}},new Dictionary<string,string>());
    private static void InvalidObservation(Func<RepeatCycleAcceptedObservation,RepeatCycleAcceptedObservation> mutate,string name)
    {
        var source=new Rig(20); var o=source.Tick(false,false);
        var trial=new RepeatCycleObservedTrial(Id,Bind); trial.Observe(mutate(o));
        Check(trial.View.Status==RepeatCycleTrialStatus.Unavailable,name);
    }
    public static void Main()
    {
        // Three separately requested single cycles are not a repeat batch from one START.
        foreach(int ms in new[]{20,40,100})
        {
            var separateStarts=new Rig(ms);
            bool rejectedSecondStart=false;
            for(int cycle=0;cycle<3;cycle++)
            {
                separateStarts.Tick(true,true);
                if(cycle==1)
                    rejectedSecondStart=separateStarts.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention;
                separateStarts.Return();
                separateStarts.Tick(false,false);
            }
            for(int hold=0;hold<(int)Math.Ceiling(500.0/ms);hold++) separateStarts.Tick(false,false);
            Check(rejectedSecondStart,"second START must reject separately requested cycles at "+ms+" ms");
            Check(separateStarts.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"three individual STARTs cannot earn Observed at "+ms+" ms");
            Check(separateStarts.Plant.Phase==RepeatCyclePlantModel.CyclePhase.Idle&&separateStarts.Count==3,
                "rejection does not interfere with real-model independently requested trace at "+ms+" ms");
            var midstrokeStart=new Rig(ms); midstrokeStart.Tick(true,true); midstrokeStart.Tick(false,true); midstrokeStart.Tick(true,true);
            Check(midstrokeStart.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,
                "additional START during a partial stroke rejects at "+ms+" ms");
        }
        foreach(int ms in new[]{20,40,100})
        {
            var r=new Rig(ms); r.Tick(false,false);
            Check(r.Trial.View.Status==RepeatCycleTrialStatus.Ready,"waiting stays Ready");
            r.Tick(true,true); Check(r.Trial.View.AcceptedElapsed==r.Period,"trigger interval counts");
            for(int i=0;i<3;i++) { r.Return(); Check(r.Trial.View.ActualReturns==i+1,"actual complete edge"); r.Tick(false,false); }
            Check(r.Trial.View.Status==RepeatCycleTrialStatus.Running,"ack interval excluded");
            int intervals=(int)Math.Ceiling(500.0/ms);
            for(int i=0;i<intervals-1;i++) r.Tick(false,false);
            Check(r.Trial.View.Status==RepeatCycleTrialStatus.Running,"short hold"); r.Tick(false,false);
            Check(r.Trial.View.Status==RepeatCycleTrialStatus.Observed,"three chains success");
            while(r.Time<TimeSpan.FromSeconds(21)) r.Tick(false,false);
            Check(r.Trial.View.Status==RepeatCycleTrialStatus.Observed,"observed success survives later stationary scans past deadline");
            r.Trial.Stop(); var old=r.Trial.View; r.Tick(true,true); Check(r.Trial.View==old,"stopped success sticky historical");
        }
        foreach(int ms in new[]{20,40,100})
        {
            var early=new Rig(ms); early.Count=1; early.Tick(true,true); Check(early.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"early count");
            var missing=new Rig(ms); missing.Return(true); missing.Tick(false,false,false); Check(missing.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"missing N+1 count");
            var twice=new Rig(ms); twice.Return(true); twice.Count=1; twice.Tick(false,false); Check(twice.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"double N+1 count");
            var pending=new Rig(ms); pending.Return(true); pending.Tick(false,true); for(int i=0;i<10;i++) pending.Tick(false,true,false);
            Check(pending.Trial.View.Status==RepeatCycleTrialStatus.Running&&pending.Trial.View.ActualReturns==1,"repeated Done pending"); pending.Tick(false,false,false); pending.Return();
            Check(pending.Trial.View.ActualReturns==2,"later acknowledgement admits chain");
            var timeout=new Rig(ms); timeout.Return(true); timeout.Tick(false,true); while(timeout.Time<TimeSpan.FromSeconds(20)) timeout.Tick(false,true,false);
            Check(timeout.Trial.View.Status==RepeatCycleTrialStatus.Incomplete,"pending times out");
            var force=new Rig(ms); force.Tick(true,true,force:true); Check(force.Trial.View.Status==RepeatCycleTrialStatus.Unavailable&&force.Trial.View.ClaimedCount is null,"force unavailable no fabricated count");
            var stale=new Rig(ms); var observation=stale.Tick(false,false); stale.Trial.Observe(observation with{Identity=Id with{Epoch=2}}); Check(stale.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"epoch retires");
            var duplicate=new Rig(ms); observation=duplicate.Tick(false,false); duplicate.Trial.Observe(observation); Check(duplicate.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"duplicate retires");
            var gap=new Rig(ms); observation=gap.Tick(false,false); gap.Trial.Observe(observation with{ScanNumber=3,SimulatedTime=TimeSpan.FromMilliseconds(60)}); Check(gap.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"gap retires");
            var draft=new Rig(ms); observation=draft.Tick(false,false); draft.Trial.Observe(observation with{CurrentDraftMatches=false}); Check(draft.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"draft retires");
            var nonhome=new Rig(ms); nonhome.Plant.Step(.2,true); nonhome.Tick(true,true); Check(nonhome.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"nonhome initial");
            var stop=new Rig(ms); stop.Tick(true,true); stop.Trial.Stop(); Check(stop.Trial.View.Status==RepeatCycleTrialStatus.Incomplete&&stop.Trial.View.Historical,"stop incomplete historical");
            early.Trial.Stop(); Check(early.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention&&early.Trial.View.Historical,"stop preserves failure");
            var unreleased=new Rig(ms); unreleased.Tick(true,true);
            for(int i=0;i<3;i++)
            {
                while(!unreleased.Plant.Done) unreleased.Tick(true,true);
                unreleased.Tick(true,false);
            }
            Check(unreleased.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"START held through third acknowledgement");
            // Fresh replay with a concrete premature Complete output at N+1.
            var replay=new Rig(ms); replay.Return(true); replay.Tick(false,false); replay.Return();
            var next=replay.Tick(false,false);
            var separate=new Rig(ms); separate.Return(true); separate.Tick(false,false); separate.Return();
            separate.Trial.Observe(next with{AppliedOutputs=Outputs(2,false,true)});
            Check(separate.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"completion after two");
            var finalHigh=new Rig(ms); finalHigh.Three(); finalHigh.Tick(false,true);
            Check(finalHigh.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"active after final acknowledgement");
            var fourth=new Rig(ms); fourth.Three(); while(fourth.Trial.View.Status!=RepeatCycleTrialStatus.Observed) fourth.Tick(false,false);
            fourth.Tick(false,true); Check(fourth.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"fourth departure after observed");
            var holdCount=new Rig(ms); holdCount.Three(); holdCount.Count=4; holdCount.Tick(false,false);
            Check(holdCount.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"hold count change");
            var holdPose=new Rig(ms); holdPose.Three(); var held=holdPose.Tick(false,false);
            holdPose.Trial.Observe(held with{ScanNumber=held.ScanNumber+1,SimulatedTime=held.SimulatedTime+held.ScanPeriod,AfterPlant=held.AfterPlant with{SpindleRadians=held.AfterPlant.SpindleRadians+.1}});
            Check(holdPose.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"stationary spindle");
        }
        foreach(double n in new[]{double.NaN,double.PositiveInfinity,1.5,(double)int.MaxValue+1,(double)int.MinValue-1})
            InvalidObservation(o=>o with{AppliedOutputs=Outputs(n)},"DINT invalid "+n);
        InvalidObservation(o=>o with{BindingsValid=false},"mapping invalid");
        InvalidObservation(o=>o with{AppliedOutputs=new(new Dictionary<string,bool>(),new Dictionary<string,double>(),new Dictionary<string,string>())},"missing outputs");
        InvalidObservation(o=>o with{AfterPlant=o.AfterPlant with{HomeFraction=double.NaN}},"nonfinite plant");
        InvalidObservation(o=>o with{ScanPeriod=TimeSpan.Zero},"zero period");
        var mismatchedInput=new Rig(20); var matching=mismatchedInput.Tick(false,false);
        var altered=new Dictionary<string,bool>(matching.EffectiveInputs.Bools){{"unrelated",false}}; altered[Bind.Request]=true;
        InvalidObservation(o=>o with{EffectiveInputs=new(altered,o.EffectiveInputs.Numbers,o.EffectiveInputs.Text)},"sampled effective mismatch");
        var timing=new Rig(20); var timed=timing.Tick(false,false); timing.Trial.Observe(timed with{ScanNumber=2,SimulatedTime=TimeSpan.FromMilliseconds(41)});
        Check(timing.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"exact timestamp retirement");
        var period=new Rig(20); timed=period.Tick(false,false); period.Trial.Observe(timed with{ScanNumber=2,ScanPeriod=TimeSpan.FromMilliseconds(40),SimulatedTime=TimeSpan.FromMilliseconds(60)});
        Check(period.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"period changed");
        var baseScan=new Rig(20); baseScan.Scan=500; baseScan.Tick(true,true);
        Check(baseScan.Trial.View.TriggerScan==501&&baseScan.Trial.View.Status==RepeatCycleTrialStatus.Running,"nonzero session scan baseline");
        var duplicateAliases=new RepeatCycleObservedTrial(Id,Bind with{Count=Bind.Active}); duplicateAliases.Observe(matching);
        Check(duplicateAliases.View.Status==RepeatCycleTrialStatus.Unavailable,"ambiguous duplicate alias");
        var wrongBool=new Dictionary<string,bool>(matching.AppliedOutputs.Bools){{Bind.Count,false}};
        InvalidObservation(o=>o with{AppliedOutputs=new(wrongBool,o.AppliedOutputs.Numbers,o.AppliedOutputs.Text)},"wrong signal type");
        var historical=new Rig(20); historical.Tick(true,true); historical.Trial.Invalidate("changed identity");
        Check(historical.Trial.View.Historical&&historical.Trial.View.ClaimedCount==0,"historical accepted value retained"); historical.Tick(false,true);
        Check(historical.Trial.View.Status==RepeatCycleTrialStatus.Unavailable,"invalidation sticky");
        Check(new RepeatCycleObservedTrial(Id,Bind).View.ClaimedCount is null,"count absent before valid observation");
        var invalidScanCount=new Rig(20); var counted=invalidScanCount.Tick(false,false);
        invalidScanCount.Trial.Observe(counted with{ScanNumber=9,AppliedOutputs=Outputs(99)});
        Check(invalidScanCount.Trial.View.ClaimedCount==0&&invalidScanCount.Trial.View.Historical,"retired scan cannot replace last accepted count");
        foreach(int ms in new[]{20,40,100})
        {
            var late=new Rig(ms); late.Return(true); late.Tick(false,false); late.Return(); late.Tick(false,false);
            while(late.Plant.Phase!=RepeatCyclePlantModel.CyclePhase.Return||late.Plant.HomeFraction<.8) late.Tick(false,true);
            while(late.Time<TimeSpan.FromSeconds(19.6)) late.Tick(false,false);
            late.Return(); late.Tick(false,false);
            while(late.Time<TimeSpan.FromSeconds(20)) late.Tick(false,false);
            Check(late.Trial.View.Status==RepeatCycleTrialStatus.Incomplete&&late.Trial.View.StationaryHold<TimeSpan.FromSeconds(.5),"deadline includes final hold");
            var delayed=new Rig(ms); while(delayed.Time<TimeSpan.FromSeconds(25)) delayed.Tick(false,false);
            Check(delayed.Trial.View.Status==RepeatCycleTrialStatus.Ready,"no deadline before enabled edge"); delayed.Tick(true,true);
            Check(delayed.Trial.View.AcceptedElapsed==delayed.Period,"deadline uses trigger preinterval");
            var lateCount=new Rig(ms); lateCount.Return(true); lateCount.Tick(false,false,false); lateCount.Count=1; lateCount.Tick(false,false,false);
            Check(lateCount.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"N+2 late count cannot repair N+1");
            var countWithoutAck=new Rig(ms); countWithoutAck.Return(true); countWithoutAck.Tick(false,true,false);
            Check(countWithoutAck.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"N+1 count failure independent of acknowledgement");
            var initialPreset=new Rig(ms); initialPreset.Count=2; initialPreset.Tick(true,true);
            Check(initialPreset.Trial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"preset two fails concrete early count");
        }
        var missingAlias=new Dictionary<string,bool>(matching.SampledInputs.Bools); missingAlias.Remove(Bind.Done);
        InvalidObservation(o=>o with{SampledInputs=new(missingAlias,o.SampledInputs.Numbers,o.SampledInputs.Text)},"missing exact input alias");
        var bogusDone=new Dictionary<string,bool>(matching.PostPlantInputs.Bools); bogusDone[Bind.Done]=true;
        InvalidObservation(o=>o with{PostPlantInputs=new(bogusDone,o.PostPlantInputs.Numbers,o.PostPlantInputs.Text)},"manually toggled Done cannot substitute for actual phase");
        foreach(var identity in new[]{Id with{SceneId="other"},Id with{PlantVersion="other"},Id with{ProgramFingerprint="other"},Id with{BindingFingerprint="other"},Id with{LayoutFingerprint="other"}})
            InvalidObservation(o=>o with{Identity=identity},"semantic identity retirement");
        var mutableBools=new Dictionary<string,bool>{{"alias",true}}; var mutableNumbers=new Dictionary<string,double>{{"number",1}};
        var copy=new VirtualControllerSignalImage(mutableBools,mutableNumbers,new Dictionary<string,string>());
        mutableBools["alias"]=false; mutableNumbers["number"]=99;
        Check(copy.Bools["alias"]&&copy.Numbers["number"]==1,"accepted images are primitive copies");
        foreach(int endpoint in new[]{int.MinValue,int.MaxValue})
        {
            var endpointTrial=new RepeatCycleObservedTrial(Id,Bind); endpointTrial.Observe(matching with{AppliedOutputs=Outputs(endpoint)});
            Check(endpointTrial.View.ClaimedCount==endpoint&&endpointTrial.View.Status==RepeatCycleTrialStatus.NeedsAttention,"DINT endpoints validated before semantic count failure");
        }
        Console.WriteLine($"PASS {checks} checks");
    }
}



