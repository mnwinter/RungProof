using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using RungProof.Next.Challenges;
using RungProof.Next.Scenes;
using RungProof.Next.VirtualController;
namespace RungProof.Next.App;

public partial class Main
{
    /// <summary>Native test lane only: runs synchronously on Godot's main thread.</summary>
    private sealed class NativeChallengePlant(SceneSimulationRuntime runtime) : IChallengePlantAdapter
    {
        public void Reset() { runtime.UsesExternalClock = true; runtime.ResetSimulation(); runtime.SetControllerPlaybackRunning(true); }
        public IReadOnlyDictionary<string,bool> SampleBoolInputs() => runtime.CaptureConveyorChallengeState().Feedback;
        public IReadOnlyDictionary<string,double> SampleNumericInputs() => new Dictionary<string,double>();
        public IReadOnlyDictionary<string,string> SampleTextInputs() => new Dictionary<string,string>();
        public void CommitBoolOutputs(IReadOnlyDictionary<string,bool> outputs) => runtime.CommitVirtualControllerOutputs(outputs);
        public void CommitNumericOutputs(IReadOnlyDictionary<string,double> outputs) => runtime.CommitVirtualControllerNumericOutputs(outputs);
        public void CommitTextOutputs(IReadOnlyDictionary<string,string> outputs) => runtime.CommitVirtualControllerTextOutputs(outputs);
        public void Advance(TimeSpan period) => runtime.AdvanceSimulation(period.TotalSeconds);
        public ChallengePlantState CaptureState() => runtime.CaptureConveyorChallengeState();
    }
    private static LadderProgram ChallengeReviewProgram(string behavior = "hold", bool renamed = false, bool memory = false)
    {
        var start=renamed?"BeginRequest":"Start"; var stop=renamed?"HaltRequest":"Stop";
        var eye=renamed?"OccupiedBeam":"Eye"; var output=renamed?"BeltCommand":"Run"; var hold=memory?"Remember":output;
        LadderNode Contact(string id,string name,bool nc=false) => new(id,LadderNodeKind.Contact,name,nc);
        LadderNode Branch(params LadderNode[] children) => new("request",LadderNodeKind.Parallel,Children:children);
        LadderNode Series(params LadderNode[] children) => new("logic",LadderNodeKind.Series,Children:children);
        var variables=new List<PlcVariable> {
            new(start,PlcVariableType.Bool,PlcVariableRole.Input,false,"operator.start"),
            new(stop,PlcVariableType.Bool,PlcVariableRole.Input,false,"operator.stop"),
            new(eye,PlcVariableType.Bool,PlcVariableRole.Input,false,"simulated_photoeye"),
            new(output,PlcVariableType.Bool,PlcVariableRole.Output,false,"conveyor_running") };
        if(memory) variables.Add(new(hold,PlcVariableType.Bool,PlcVariableRole.Memory,false));
        var request=Branch(Contact("start",start),Contact("hold",hold));
        var logic=behavior switch {
            "missing-hold" => Series(Contact("stop",stop,true),Contact("eye",eye,true),Contact("start",start)),
            "missing-stop" => Series(Contact("eye",eye,true),request),
            "missing-eye" => Series(Contact("stop",stop,true),request),
            "always-on" => Branch(Contact("s",start),Contact("ns",start,true)),
            "always-off" => Series(Contact("s",start),Contact("ns",start,true)),
            "start-priority" => Branch(Contact("start",start),Series(Contact("stop",stop,true),Contact("eye",eye,true),Contact("hold",hold))),
            _ => Series(Contact("stop",stop,true),Contact("eye",eye,true),request),
        };
        var networks=new List<LadderNetwork> { new("r1","Review fixture",logic,new("coil",hold)) };
        if(memory) networks.Add(new("r2","Apply remembered request",Contact("remember",hold),new("output",output)));
        return new(1,"review","Native verifier fixture","LD",TimeSpan.FromMilliseconds(20),variables.ToArray(),networks.ToArray());
    }
    private void VerifyConveyorChallenge()
    {
        try
        {
            if(_challengeDefinition is null || _challengeManifest is null || _candidateCatalog is null || _mainCamera is null)
                throw new InvalidOperationException("Trusted challenge content unavailable");
            AddMigratedScene(_challengeDefinition.SceneId,_candidateCatalog,_mainCamera,false,false);
            if(_sceneRuntime is null) throw new InvalidOperationException("Native plant unavailable");
            var count=0;
            foreach(var fixture in new[]{("hold",false,false),("hold",true,false),("hold",false,true),("missing-hold",false,false),("missing-stop",false,false),("missing-eye",false,false),("always-on",false,false),("always-off",false,false),("start-priority",false,false)})
            {
                var program=ChallengeReviewProgram(fixture.Item1,fixture.Item2,fixture.Item3);
                var identity=CreateChallengeIdentity(program,++_challengeEpoch);
                var pure=ChallengeTestRunner.Run(_challengeDefinition,program,identity,()=>new ConveyorChallengePlantAdapter(_challengeDefinition.Plant),CancellationToken.None);
                var native=ChallengeTestRunner.Run(_challengeDefinition,program,identity,()=>new NativeChallengePlant(_sceneRuntime),CancellationToken.None);
                var expected=fixture.Item1=="hold"?ChallengeOutcome.Passed:ChallengeOutcome.Failed;
                if(pure.Outcome!=expected || native.Outcome!=expected || pure.Trace.Count!=native.Trace.Count || pure.Cases.Count!=native.Cases.Count || pure.Events.Count!=native.Events.Count)
                    throw new InvalidOperationException($"Outcome/ledger mismatch {fixture}: pure={pure.Outcome}/{pure.Diagnostic} native={native.Outcome}/{native.Diagnostic}");
                for(var i=0;i<pure.Cases.Count;i++)
                    if(pure.Cases[i].CaseId!=native.Cases[i].CaseId || pure.Cases[i].Code!=native.Cases[i].Code || pure.Cases[i].FirstFailure?.ScanNumber!=native.Cases[i].FirstFailure?.ScanNumber)
                        throw new InvalidOperationException("First-failure mismatch "+fixture);
                for(var i=0;i<pure.Trace.Count;i++)
                {
                    var a=pure.Trace[i]; var b=native.Trace[i];
                    if(a.CaseId!=b.CaseId || a.ScanNumber!=b.ScanNumber || a.Cycle!=b.Cycle || a.CaseSequence!=b.CaseSequence || a.SimulatedTime!=b.SimulatedTime
                        || a.PlantAfter.ObjectPresent!=b.PlantAfter.ObjectPresent || a.PlantAfter.Completed!=b.PlantAfter.Completed
                        || Math.Abs(a.PlantAfter.Numbers["object_position"]-b.PlantAfter.Numbers["object_position"])>1e-9
                        || !SameMap(a.EffectiveInputs,b.EffectiveInputs) || !SameMap(a.AppliedOutputs,b.AppliedOutputs)
                        || !SamePlant(a.PlantAfter,b.PlantAfter))
                        throw new InvalidOperationException($"Accepted scan projection mismatch {fixture} row={i}");
                }
                for(var i=0;i<pure.Events.Count;i++)
                {
                    var a=pure.Events[i]; var b=native.Events[i];
                    if(a.CaseId!=b.CaseId || a.Cycle!=b.Cycle || a.CaseSequence!=b.CaseSequence || a.Kind!=b.Kind
                        || a.ScanNumber!=b.ScanNumber || a.SimulatedTime!=b.SimulatedTime || a.Detail!=b.Detail || !SamePlant(a.Plant,b.Plant))
                        throw new InvalidOperationException($"Reset/lifecycle metadata mismatch {fixture} event={i}");
                }
                GD.Print($"CONVEYOR_CHALLENGE_CASE_PASS fixture={fixture} outcome={native.Outcome} acceptedRows={native.Trace.Count}"); count++;
            }
            // Captured native primitive maps must not alias the next live scan.
            var frozen=_sceneRuntime.CaptureConveyorChallengeState(); var position=frozen.Numbers["object_position"];
            _sceneRuntime.ResetSimulation();
            if(frozen.Numbers["object_position"]!=position) throw new InvalidOperationException("Native capture aliases live points");
            GD.Print($"CONVEYOR_CHALLENGE_VERIFY PASS fixtures={count} offline=true keyboard=false transport=none");
            GetTree().Quit();
        }
        catch(Exception error)
        { GD.PushError("CONVEYOR_CHALLENGE_VERIFY FAIL "+error); GetTree().Quit(1); }
    }
    private static bool SameMap<T>(IReadOnlyDictionary<string,T> a,IReadOnlyDictionary<string,T> b) =>
        a.Count==b.Count && a.All(p=>b.TryGetValue(p.Key,out var value)&&EqualityComparer<T>.Default.Equals(p.Value,value));
    private static bool SamePlant(ChallengePlantState a,ChallengePlantState b) => a.ObjectPresent==b.ObjectPresent && a.Completed==b.Completed
        && SameMap(a.Feedback,b.Feedback) && SameMap(a.Text,b.Text) && a.Numbers.Count==b.Numbers.Count
        && a.Numbers.All(p=>b.Numbers.TryGetValue(p.Key,out var value)&&Math.Abs(p.Value-value)<=1e-9);
}
