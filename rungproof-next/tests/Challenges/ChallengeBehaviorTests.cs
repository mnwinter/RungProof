using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RungProof.Next.Challenges;
using RungProof.Next.VirtualController;
namespace RungProof.Next.ChallengeTests;
internal static class ChallengeBehaviorTests
{
    internal static ChallengeDefinition Definition => ChallengeDefinitionJson.Load(File.ReadAllText("challenges/conveyor-stop.challenge.json")).Definition
        ?? throw new InvalidOperationException("Actual production challenge packet is invalid.");
    internal static ChallengeRunIdentity Identity(LadderProgram p,ChallengeDefinition? content=null)
    {
        var fixture=ChallengeSemanticManifest.Load(File.ReadAllText(".tools/studio-verification/challenge-semantic-manifest.json")).Manifest
            ?? throw new InvalidOperationException("Actual complete fixture semantic manifest required.");
        var production=ChallengeSemanticManifest.Load(File.ReadAllText("challenges/semantic-manifest.json")).Manifest
            ?? throw new InvalidOperationException("Actual complete production semantic manifest required.");
        Check(production.EngineSha256==fixture.EngineSha256 && production.EvaluatorSha256==fixture.EvaluatorSha256,"Fixture/production manifest drift");
        var generator=Encoding.UTF8.GetBytes(File.ReadAllText("tools/build_challenge_semantic_manifest.ps1").Replace("\r\n","\n").Replace("\r","\n"));
        Check(Convert.ToHexStringLower(SHA256.HashData(generator))==fixture.GeneratorSha256,"Stale manifest producer digest");
        foreach(var entry in fixture.EngineInputs.Concat(fixture.EvaluatorInputs))
        {
            var bytes=Encoding.UTF8.GetBytes(File.ReadAllText(entry.RelativePath).Replace("\r\n","\n").Replace("\r","\n"));
            Check(bytes.LongLength==entry.ByteLength && Convert.ToHexStringLower(SHA256.HashData(bytes))==entry.Sha256,"Stale semantic source entry: "+entry.RelativePath);
        }
        var definition=content??Definition;
        var actualStarter=File.ReadAllBytes("challenges/starters/conveyor-stop.rpproj.json");
        return new("RP-CANON-1",definition.Id,definition.Revision,ChallengeProgramIdentity.ProgramSha256(p),ChallengeContentIdentity.PacketSha256(definition,actualStarter),ChallengeContentIdentity.PlantSha256(definition.Plant),fixture.EngineRevision,fixture.EngineSha256,fixture.EvaluatorRevision,fixture.EvaluatorSha256,20,1);
    }
    internal static ChallengeRunResult Run(LadderProgram p,Func<IChallengePlantAdapter>? plant=null,CancellationToken token=default) => ChallengeTestRunner.Run(Definition,p,Identity(p),plant??(()=>new ConveyorChallengePlantAdapter(Definition.Plant)),token);
    private static void Check(bool ok,string message)=>ChallengeFixtures.Require(ok,message);
    public static IEnumerable<(string,Action)> Tests()
    {
        yield return("OutputSealInAndInternalAssignSealInBothPassAllCases",()=>{foreach(var p in new[]{ChallengeFixtures.OutputSealIn(),ChallengeFixtures.InternalSealIn()}) {var r=Run(p);Check(r.Outcome==ChallengeOutcome.Passed,string.Join(";",r.Cases.Select(c=>c.CaseId+":"+c.Code+":"+c.Observed))+r.Diagnostic);Check(r.Cases.Count==7,"Seven cases required");}});
        yield return("RenamedBindingVariantsPass",()=>{foreach(var p in new[]{ChallengeFixtures.OutputSealIn(true),ChallengeFixtures.InternalSealIn(true)}) Check(Run(p).Outcome==ChallengeOutcome.Passed,"Renamed end-to-end mapper failed");});
        yield return("MissingHoldFailsCV02AfterStartRelease",()=>Failure(Remove("hold"),"CV02",2));
        yield return("MissingStopFailsCV04AtScan11",()=>Failure(Remove("stop"),"CV04",11));
        yield return("MissingSensorFailsCV03AtFirstEffectiveTrueScan",()=>{var c=Run(Remove("eye")).Cases.Single(c=>c.CaseId=="CV03");Check(c.Outcome==ChallengeOutcome.Failed && c.FirstFailure!.EffectiveInputs["simulated_photoeye"],"Sensor failure phase incorrect");});
        yield return("UnconditionalFailsCV01",()=>{var p=ChallengeFixtures.OutputSealIn();p=p with{Networks=[p.Networks[0] with{Logic=new("always",LadderNodeKind.Parallel,Children:[new("a",LadderNodeKind.Contact,"Start"),new("not-a",LadderNodeKind.Contact,"Start",true)]) }]};Failure(p,"CV01",1);});
        yield return("AlwaysOffCannotPassStoppedOrigin",()=>{var p=ChallengeFixtures.OutputSealIn();p=p with{Networks=[p.Networks[0] with{Logic=new("never",LadderNodeKind.Contact,"Stop")}]};Failure(p,"CV02",1);});
        yield return("BlockedStartBypassFailsCV06",()=>{var p=ChallengeFixtures.OutputSealIn();p=p with{Networks=[p.Networks[0] with{Logic=new("bypass",LadderNodeKind.Parallel,Children:[p.Networks[0].Logic,new("bypass-start",LadderNodeKind.Contact,"Start")])}]};Check(ChallengeEligibilityValidator.Validate(p).Count==0,"Bypass fixture ineligible");var c=Run(p).Cases.Single(c=>c.CaseId=="CV06");Check(c.Outcome==ChallengeOutcome.Failed && c.FirstFailure!.EffectiveInputs["operator.start"] && c.FirstFailure.EffectiveInputs["simulated_photoeye"],"Occupied Start bypass not discriminated");});
        yield return("EligibleRestartFailsHold",()=>{var p=ChallengeFixtures.OutputSealIn();p=p with{Networks=[p.Networks[0] with{Logic=new("restart",LadderNodeKind.Parallel,Children:[p.Networks[0].Logic,new("oscillator",LadderNodeKind.Series,Children:[new("occupied",LadderNodeKind.Contact,"Eye"),new("off",LadderNodeKind.Contact,"Run",true)])])}]};Check(ChallengeEligibilityValidator.Validate(p).Count==0,"Restart must be eligible BOOL logic");var c=Run(p).Cases.Single(c=>c.CaseId=="CV03");Check(c.Outcome==ChallengeOutcome.Failed && c.FirstFailure!.ScanNumber>51 && c.FirstFailure.AppliedOutputs["conveyor_running"],"Restart after initial inhibition passed");});
        yield return("SimultaneousStartPriorityFailsCV05",()=>{var p=ChallengeFixtures.OutputSealIn();p=p with{Networks=[p.Networks[0] with{Logic=new("start-priority",LadderNodeKind.Parallel,Children:[p.Networks[0].Logic,new("priority-start",LadderNodeKind.Contact,"Start")])}]};Failure(p,"CV05",1);});
        yield return("SimultaneousInputsRequirePulsePlus25SubsequentScans",()=>{var rows=Run(ChallengeFixtures.OutputSealIn()).Trace.Where(o=>o.CaseId=="CV05").ToArray();Check(rows.Length==26 && rows.Select(o=>o.ScanNumber).SequenceEqual(Enumerable.Range(1,26).Select(n=>(long)n)),"CV05 must retain exactly pulse scan1 plus25 subsequent accepted scans");Check(rows[0].EffectiveInputs["operator.start"] && rows[0].EffectiveInputs["operator.stop"] && rows.Skip(1).All(o=>!o.EffectiveInputs["operator.start"] && !o.EffectiveInputs["operator.stop"]),"Both pulses must release after scan1");});
        yield return("ResetRepeatUsesMonotonicCaseSequence",()=>{var r=Run(ChallengeFixtures.OutputSealIn());var rows=r.Trace.Where(o=>o.CaseId=="CV07").ToArray();Check(rows.Select(o=>o.CaseSequence).SequenceEqual(Enumerable.Range(1,rows.Length).Select(i=>(long)i)),"Sequence reset");Check(rows.Count(o=>o.Cycle==1)>0 && rows.First(o=>o.Cycle==1).ScanNumber==1 && rows.Length<=250,"Second cycle missing/budget wrong");Check(r.Events.Count(e=>e.Kind==ChallengeTraceEventKind.ResetCheckpoint)==1,"Reset metadata missing");});
        yield return("BeamStopWitnessesActualFeedbackAnd25HeldScans",()=>{var rows=Run(ChallengeFixtures.OutputSealIn()).Trace.Where(o=>o.CaseId=="CV03").ToArray();var n=rows.First(o=>o.PlantAfter.Feedback["simulated_photoeye"]).ScanNumber;Check(n<=75 && rows.Last().ScanNumber==n+26,"Beam witness/hold wrong");var stopped=rows.Single(o=>o.ScanNumber==n+1);Check(stopped.EffectiveInputs["simulated_photoeye"] && !stopped.AppliedOutputs["conveyor_running"],"Next effective inhibition missing");});
    }
    internal static LadderProgram Remove(string nodeId)
    {
        var p=ChallengeFixtures.OutputSealIn();
        LadderNode Edit(LadderNode n)
        {
            var children=n.Children?.Where(c=>c.Id!=nodeId).Select(Edit).ToArray();
            return children?.Length==1 && n.Kind==LadderNodeKind.Parallel ? children[0] : n with{Children=children};
        }
        return p with{Networks=[p.Networks[0] with{Logic=Edit(p.Networks[0].Logic)}]};
    }
    private static void Failure(LadderProgram p,string id,long scan)
    {Check(ChallengeEligibilityValidator.Validate(p).Count==0,"Negative fixture must be eligible");var r=Run(p);var c=r.Cases.Single(c=>c.CaseId==id);Check(c.Outcome==ChallengeOutcome.Failed && c.FirstFailure?.ScanNumber==scan,$"Expected {id}/{scan}, got {c.Code}/{c.FirstFailure?.ScanNumber}");Check(r.Cases.Count==7,"Behavior failure must continue fresh cases");}
}
