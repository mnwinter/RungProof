using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using RungProof.Next.App;
using RungProof.Next.Challenges;
using RungProof.Next.ChallengeTests;
namespace RungProof.Next.CoordinatorTests;
internal static class Program
{
    private static readonly string SourceRoot = FindSourceRoot();
    private static readonly byte[] Starter = File.ReadAllBytes(Path.Combine(SourceRoot,"challenges/starters/conveyor-stop.rpproj.json"));
    private static readonly ChallengeDefinition Definition = ChallengeDefinitionJson.Load(File.ReadAllText(Path.Combine(SourceRoot,"challenges/conveyor-stop.challenge.json"))).Definition!;
    private static readonly ChallengeSemanticManifest Manifest = ChallengeSemanticManifest.Load(File.ReadAllText(Path.Combine(SourceRoot,"challenges/semantic-manifest.json"))).Manifest!;
    private static string FindSourceRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while(d is not null) { if(File.Exists(Path.Combine(d.FullName,"challenges/catalog.json"))) return d.FullName; d=d.Parent; } throw new InvalidOperationException("Source fixture resources missing"); }
    private static readonly RungProof.Next.VirtualController.LadderProgram Draft = ChallengeFixtures.OutputSealIn();
    private static ChallengeRunIdentity Identity(long epoch) => new("RP-CANON-1",Definition.Id,1,ChallengeProgramIdentity.ProgramSha256(Draft),ChallengeContentIdentity.PacketSha256(Definition,Starter),ChallengeContentIdentity.PlantSha256(Definition.Plant),Manifest.EngineRevision,Manifest.EngineSha256,Manifest.EvaluatorRevision,Manifest.EvaluatorSha256,20,epoch);
    private static ChallengeRunResult Passed(ChallengeRunIdentity id) => new(id,ChallengeOutcome.Passed,Definition.Cases.Select(c=>new ChallengeCaseResult(c.Id,ChallengeOutcome.Passed,"PASS","","",null)).ToArray(),[],[],"PASS","");
    private static void Require(bool value,string reason) { if(!value) throw new InvalidOperationException(reason); }
    private static void Drain(ChallengeSessionCoordinator c) { Require(SpinWait.SpinUntil(()=> { c.Poll(); return !c.State.IsWorkerOutstanding; },2000),"Worker did not exit"); }
    public static int Main()
    {
        var root=Path.Combine(Path.GetTempPath(),"RungProof-coordinator-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var failures=0;
        void Test(string name,Action test) { try { test(); Console.WriteLine("PASS "+name); } catch(Exception e) { failures++; Console.WriteLine("FAIL "+name+": "+e.Message); } }
        Test("Passing worker persists only matching current proof",()=> {
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"pass.json")),(_,_,id,_)=>Passed(id),()=>TimeSpan.Zero,Definition,Starter,Manifest);
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected eligible draft"); Drain(c);
            Require(c.State.IsCurrentProof && c.State.PersistenceStatus==ChallengePersistenceStatus.Saved && c.Progress.Count==1,"Passed proof not persisted");
            c.DraftChanged(); Require(!c.State.IsCurrentProof && c.State.LastResult?.Outcome==ChallengeOutcome.Passed,"Mutation lost historical result or retained proof");
        });
        Test("Cancellation after computed pass cannot award or start second worker",()=> {
            using var release=new ManualResetEventSlim(); using var entered=new ManualResetEventSlim();
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"cancel.json")),(_,_,id,_)=> { var r=Passed(id); entered.Set(); release.Wait(); return r; },()=>TimeSpan.Zero,Definition,Starter,Manifest);
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); Require(entered.Wait(2000),"Worker never entered"); c.Cancel();
            Require(c.State.MutationLocked && !c.Begin(Definition,Draft,Identity(2),[]),"Cancellation unlocked occupied worker"); release.Set(); Drain(c);
            Require(!c.State.IsCurrentProof && c.Progress.Count==0,"Retired worker awarded progress");
        });
        Test("Wrong completion identity never persists",()=> {
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"wrong.json")),(_,_,id,_)=>Passed(id with { RunEpoch=id.RunEpoch+1 }),()=>TimeSpan.Zero,Definition,Starter,Manifest);
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); Drain(c);
            Require(c.State.LastResult?.Outcome==ChallengeOutcome.InfrastructureError && c.Progress.Count==0,"Wrong identity accepted");
        });
        Test("Save failure retains Passed with explicit not-saved status",()=> {
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"fault.json"),_=>throw new IOException("Fixture failure")),(_,_,id,_)=>Passed(id),()=>TimeSpan.Zero,Definition,Starter,Manifest);
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); Drain(c);
            Require(c.State.IsCurrentProof && c.State.LastResult?.Outcome==ChallengeOutcome.Passed && c.State.PersistenceStatus==ChallengePersistenceStatus.NotSaved && c.State.StatusText.Contains("progress not saved",StringComparison.OrdinalIgnoreCase),"Save failure changed grade or was hidden");
        });
        Test("Expired pending Reset is dropped without automatic replay",()=> {
            var now=TimeSpan.Zero; using var release=new ManualResetEventSlim(); var actions=new List<ChallengePendingAction>();
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"reset.json")),(_,_,id,_)=> { release.Wait(); return Passed(id); },()=>now,Definition,Starter,Manifest);
            c.PendingActionReady+=actions.Add; Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); c.RequestAction(ChallengePendingAction.Reset);
            now=TimeSpan.FromSeconds(2); c.Poll(); Require(c.State.MutationLocked && c.State.PendingAction==ChallengePendingAction.None,"Timeout released worker or retained intent"); release.Set(); Drain(c);
            Require(actions.Count==0 && c.Progress.Count==0,"Expired intent replayed or awarded");
        });
        Test("Timely Navigate drains exactly once on polling caller",()=> {
            using var release=new ManualResetEventSlim(); var thread=Environment.CurrentManagedThreadId; var actions=0;
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"navigate.json")),(_,_,id,_)=> { release.Wait(); return Passed(id); },()=>TimeSpan.Zero,Definition,Starter,Manifest);
            c.PendingActionReady+=a=> { Require(a==ChallengePendingAction.Navigate && Environment.CurrentManagedThreadId==thread,"Callback crossed thread"); actions++; };
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); c.RequestAction(ChallengePendingAction.Navigate); release.Set(); Drain(c); c.Poll();
            Require(actions==1 && c.Progress.Count==0,"Intent did not drain once or stale pass awarded");
        });
        Test("Passed outcome with missing case cannot award",()=> {
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"incomplete.json")),(_,_,id,_)=>new ChallengeRunResult(id,ChallengeOutcome.Passed,[],[],[],"PASS",""),()=>TimeSpan.Zero,Definition,Starter,Manifest);
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); Drain(c); Require(!c.State.IsCurrentProof && c.Progress.Count==0,"Incomplete case ledger accepted");
        });
        Test("Full historical content identities survive same-program pass",()=> {
            var repository=new ChallengeProgressRepository(Path.Combine(root,"history.json"));
            var id=Identity(1);
            var older=new[]{id with { ChallengeRevision=2 },id with { ChallengeSha256=new string('e',64) },id with { PlantSha256=new string('f',64) },id with { EngineSha256=new string('1',64) },id with { EvaluatorSha256=new string('2',64) }};
            Require(repository.Save(older.Select(i=>new ChallengeProgressRecord(i,DateTimeOffset.UtcNow,[])).ToArray()).Success,"Seed save failed");
            using var c=new ChallengeSessionCoordinator(repository,(_,_,run,_)=>Passed(run),()=>TimeSpan.Zero,Definition,Starter,Manifest);
            Require(c.Begin(Definition,Draft,id,[]),"Begin rejected"); Drain(c); Require(c.Progress.Count==6,"Distinct content history erased");
            Require(c.Begin(Definition,Draft,Identity(2),[]),"Repeat Begin rejected"); Drain(c); Require(c.Progress.Count==6,"Epoch created duplicate content proof");
        });
        Test("Computed packet plant and manifest identity reject mismatched caller claims",()=> {
            var calls=0;
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"identity.json")),(_,_,id,_)=> { calls++; return Passed(id); },()=>TimeSpan.Zero,Definition,Starter,Manifest);
            var id=Identity(1);
            foreach(var wrong in new[]{id with { ChallengeSha256=new string('0',64) },id with { PlantSha256=new string('0',64) },id with { EngineSha256=new string('0',64) },id with { EvaluatorSha256=new string('0',64) }})
                Require(!c.Begin(Definition,Draft,wrong,[]) && !c.State.IsWorkerOutstanding,"Untrusted content claim reserved worker");
            Require(!c.Begin(Definition with { Objective="Altered packet" },Draft,id,[]) && calls==0,"Altered typed packet reached worker");
        });
        Test("Pending action drains before reentrant new test",()=> {
            using var release=new ManualResetEventSlim(); var calls=0; var started=false;
            using var c=new ChallengeSessionCoordinator(new(Path.Combine(root,"reentrant.json")),(_,_,id,_)=> { Interlocked.Increment(ref calls); if(id.RunEpoch==1) release.Wait(); return Passed(id); },()=>TimeSpan.Zero,Definition,Starter,Manifest);
            c.PendingActionReady+=_=>started=c.Begin(Definition,Draft,Identity(2),[]);
            Require(c.Begin(Definition,Draft,Identity(1),[]),"Begin rejected"); c.RequestAction(ChallengePendingAction.Navigate); release.Set(); Drain(c);
            Require(started && calls==2 && c.Progress.Count==1,"Intent was not drained before new worker");
        });
        Console.WriteLine(failures==0 ? "CHALLENGE_COORDINATOR_TESTS_PASS 10" : "CHALLENGE_COORDINATOR_TESTS_FAIL "+failures); return failures==0 ? 0 : 1;
    }
}
