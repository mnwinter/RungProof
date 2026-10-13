using System;
using System.Collections.Generic;
using System.Linq;
using RungProof.Next.Challenges;
using RungProof.Next.VirtualController;
namespace RungProof.Next.ChallengeTests;
internal static class ChallengeBoundaryTests
{
    private static void Check(bool condition, string detail) => ChallengeFixtures.Require(condition, detail);
    private static VirtualControllerSession Session(LadderProgram? program = null) => new(new VirtualControllerRuntime(LadderCompiler.Compile(program ?? ChallengeFixtures.OutputSealIn()).Program!));
    private static int Step(VirtualControllerSession session, Func<IReadOnlyDictionary<string,bool>>? sample = null, Action<IReadOnlyDictionary<string,bool>>? commit = null, Action<double>? plant = null) => session.Advance(.02, sample ?? (() => new Dictionary<string,bool>()), commit ?? (_ => {}), plant ?? (_ => {}));
    public static IEnumerable<(string, Action)> Tests()
    {
        yield return (nameof(ExpandedNumericTextImagesRemainPrivate), ExpandedNumericTextImagesRemainPrivate);
        yield return (nameof(LifecycleForceAndStoppedAdvancePublishNoCompletedScan), LifecycleForceAndStoppedAdvancePublishNoCompletedScan);
        yield return (nameof(AcceptedEventOccursAfterCommitAndPlant), AcceptedEventOccursAfterCommitAndPlant);
        yield return (nameof(SampledAndEffectiveImagesAreCopiedPrimitives), SampledAndEffectiveImagesAreCopiedPrimitives);
        yield return (nameof(ReturnedMapsCannotMutateRuntimeOrLaterRows), ReturnedMapsCannotMutateRuntimeOrLaterRows);
        yield return (nameof(OperatorPulseResolvesByBindingAndLastsOneScan), OperatorPulseResolvesByBindingAndLastsOneScan);
        yield return (nameof(DischargedPositionZeroRetainsAbsentAndCompletedState), DischargedPositionZeroRetainsAbsentAndCompletedState);
    }
    private static void ExpandedNumericTextImagesRemainPrivate()
    {
        var p = ChallengeFixtures.OutputSealIn();
        p = p with { Variables = p.Variables.Concat(new PlcVariable[] {
            new("Ports", PlcVariableType.Struct, PlcVariableRole.Input, new Dictionary<string,object> { ["Ready"] = false, ["Speed"] = 0.0 }, Aggregate: new([new("Ready", PlcVariableType.Bool), new("Speed", PlcVariableType.Real)])),
            new("Message", PlcVariableType.String, PlcVariableRole.Input, "initial"),
            new("StoredNumber", PlcVariableType.Real, PlcVariableRole.Memory, 0.0),
            new("StoredText", PlcVariableType.String, PlcVariableRole.Memory, "private") }).ToArray() };
        var compiled = LadderCompiler.Compile(p); Check(compiled.IsValid, string.Join(";", compiled.Issues.Select(i=>i.Message)));
        var s = new VirtualControllerSession(new(compiled.Program!)); VirtualControllerScanCompleted? row=null; s.ScanCompleted += e=>row=e; s.Run();
        var numbers = new Dictionary<string,double> { ["Ports.Speed"] = 2.5 };
        var text = new Dictionary<string,string> { ["Message"] = "sample" };
        s.Advance(.02, ()=>new Dictionary<string,bool>{{"Ports.Ready",true}}, ()=>numbers, _=>{}, _=>{}, _=>{}, ()=>text);
        numbers["Ports.Speed"]=99; text["Message"]="later";
        Check(row!.EffectiveInputs.Bools["Ports.Ready"] && !row.EffectiveInputs.Bools.ContainsKey("Ports"), "Aggregate root exposed");
        Check(row.EffectiveInputs.Numbers.Count==1 && row.EffectiveInputs.Numbers["Ports.Speed"]==2.5 && row.SampledInputs.Numbers["Ports.Speed"]==2.5,"Numeric copy/filter failed");
        Check(row.EffectiveInputs.Text.Count==1 && row.EffectiveInputs.Text["Message"]=="sample" && row.SampledInputs.Text["Message"]=="sample","Text copy/filter failed");
        bool immutable=false; try { ((IDictionary<string,double>)row.EffectiveInputs.Numbers)["Ports.Speed"]=0; } catch(NotSupportedException) { immutable=true; } Check(immutable,"Numeric map writable");
        immutable=false; try { ((IDictionary<string,string>)row.SampledInputs.Text)["Message"]="bad"; } catch(NotSupportedException) { immutable=true; } Check(immutable,"Text map writable");
    }
    private static void LifecycleForceAndStoppedAdvancePublishNoCompletedScan()
    {
        var s=Session(); int count=0, publications=0; s.ScanCompleted += _=>count++; s.SnapshotPublished += _=>publications++;
        s.Run(); s.SetBoolForce("Start",true); s.RemoveForce("Start"); s.ClearForces(); s.Stop(); s.Reset();
        Check(Step(s)==1, "Legacy stopped Advance count changed"); Check(count==0 && publications==7, "Phantom completed scan or changed publication");
        s.Run(); Step(s); Check(count==1,"Missing accepted event");
    }
    private static void AcceptedEventOccursAfterCommitAndPlant()
    {
        var s=Session(); var order=new List<string>(); s.ScanCompleted += e=> { order.Add("event"); Check(e.ScanNumber==1 && e.SimulatedTime==TimeSpan.FromMilliseconds(20),"Wrong scan identity"); }; s.Run();
        Step(s, ()=>{order.Add("sample");return new Dictionary<string,bool>();}, _=>order.Add("commit"), _=>order.Add("plant"));
        Check(string.Join(",",order)=="sample,commit,plant,event","Incorrect accepted phase");
    }
    private static void SampledAndEffectiveImagesAreCopiedPrimitives()
    {
        var s=Session(ChallengeFixtures.InternalSealIn()); VirtualControllerScanCompleted? row=null;
        var raw=new Dictionary<string,bool>{{"Start",false},{"Stop",false},{"Eye",false},{"Request",true},{"Run",true}};
        s.ScanCompleted += e=>row=e; s.Run(); s.PulseInput("Start"); s.SetBoolForce("Start",false); Step(s,()=>raw);
        Check(row!.SampledInputs.Bools["Start"] && !row.EffectiveInputs.Bools["Start"],"Operator/force distinction lost");
        Check(row.EffectiveInputs.Bools.Count==3 && !row.EffectiveInputs.Bools.ContainsKey("Request"),"Non-input leaked");
        raw["Eye"]=true; Check(!row.SampledInputs.Bools["Eye"],"Sample alias leaked");
    }
    private static void ReturnedMapsCannotMutateRuntimeOrLaterRows()
    {
        var s=Session(); var rows=new List<VirtualControllerScanCompleted>(); s.ScanCompleted += rows.Add; s.Run(); s.PulseInput("Start"); Step(s); s.PulseInput("Stop"); Step(s);
        Check(rows[0].Outputs.Bools["Run"] && !rows[1].Outputs.Bools["Run"],"Later row mutated old output");
        var rejected=false; try { ((IDictionary<string,bool>)rows[0].Outputs.Bools)["Run"]=false; } catch(NotSupportedException) { rejected=true; }
        Check(rejected && rows[0].Outputs.Bools["Run"],"Mutable event map");
        rejected=false; try { ((IList<string>)rows[0].Diagnostics).Add("corrupt"); } catch(NotSupportedException) { rejected=true; } Check(rejected,"Mutable diagnostics");
    }
    private static void OperatorPulseResolvesByBindingAndLastsOneScan()
    {
        var p=ChallengeFixtures.OutputSealIn(true); var s=Session(p); var rows=new List<VirtualControllerScanCompleted>(); s.ScanCompleted += rows.Add; s.Run();
        s.PulseInput(p.Variables.Single(v=>v.Binding=="operator.start").Name); Step(s); Step(s);
        Check(rows[0].EffectiveInputs.Bools["Begin"] && !rows[1].EffectiveInputs.Bools["Begin"] && rows[1].Outputs.Bools["Motor"],"Renamed pulse failed");
    }
    private static void DischargedPositionZeroRetainsAbsentAndCompletedState()
    {
        var a=new ConveyorChallengePlantAdapter(new(1,.5,.2,.5,.1)); var initial=a.CaptureState(); a.CommitBoolOutputs(new Dictionary<string,bool>{{"conveyor_running",true}});
        for(int i=0;i<150;i++) a.Advance(TimeSpan.FromMilliseconds(20)); var discharged=a.CaptureState();
        Check(discharged.Numbers["object_position"]==0 && !discharged.ObjectPresent && discharged.Completed==1,"Discharge became false origin success");
        Check(initial.ObjectPresent && initial.Completed==0 && initial.Numbers["object_position"]==0,"Captured state changed"); a.Reset(); Check(a.CaptureState().ObjectPresent && a.CaptureState().Completed==0,"Reset failed"); a.Advance(TimeSpan.FromMilliseconds(20)); Check(a.CaptureState().Numbers["object_position"]==0,"Reset retained command");
    }
}

