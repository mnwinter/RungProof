using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization;
using RungProof.Next.VirtualController;
namespace RungProof.Next.Challenges;
/// <summary>
/// Pure offline worker. Original eligibility precedes serialization; each case owns fresh state.
/// The trusted caller validates actual packet/starter bytes and the bundled semantic manifest
/// before constructing identity. This method has no resource/file access and cannot authenticate
/// packet, engine or evaluator hashes from strings alone. It verifies the supplied program and
/// typed plant fingerprints; the coordinator owns trusted packet/manifest provenance.
/// </summary>
public static class ChallengeTestRunner
{
    public static ChallengeRunResult Run(ChallengeDefinition definition, LadderProgram program,
        ChallengeRunIdentity identity, Func<IChallengePlantAdapter> createPlant, CancellationToken cancellationToken)
    {
        var cases=new List<ChallengeCaseResult>(); var trace=new List<ChallengeObservation>(); var events=new List<ChallengeTraceEvent>();
        ChallengeRunResult Result(ChallengeOutcome outcome,string code,string diagnostic) => new(identity,outcome,cases,trace,events,code,diagnostic);
        string activeCase="";
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Typed callers receive the same strict packet policy as resource-loaded callers.
            if(definition.Hints.Count>8 || definition.Cases.Count!=7 || definition.Glossary.Count>32
                || definition.Title.Length>512 || definition.Objective.Length>512 || definition.Hints.Any(h=>h.Length>2048))
                return Result(ChallengeOutcome.InvalidChallenge,"CH_INVALID_CHALLENGE","Challenge content limits exceeded.");
            var jsonOptions=new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase,MaxDepth=32};
            jsonOptions.Converters.Add(new JsonStringEnumConverter());
            if(ChallengeDefinitionJson.Load(JsonSerializer.Serialize(definition,jsonOptions)).Definition is null)
                return Result(ChallengeOutcome.InvalidChallenge,"CH_INVALID_CHALLENGE","Typed definition fails strict conveyor packet validation.");
            if(definition.SchemaVersion!=1 || definition.Id!="scene-1-conveyor-stop" || definition.SceneId!=definition.Id || definition.ProtocolRevision!="conveyor-v1"
                || definition.EligibilityPolicy!="conveyor-bool-v1" || definition.ScanPeriodMs!=20 || definition.Plant!=new ConveyorPlantParameters(1,.5,.2,.5,.1)
                || definition.Cases.Count!=7 || definition.Cases.Select(c=>c.Kind).Distinct().Count()!=7
                || definition.Cases.Any(c=>!Enum.IsDefined(c.Kind) || c.Id!=$"CV{(int)c.Kind+1:00}" || c.MaximumScans!=250))
                return Result(ChallengeOutcome.InvalidChallenge,"CH_INVALID_CHALLENGE","Fixed conveyor-v1 definition required.");
            var issues=ChallengeEligibilityValidator.Validate(program);
            if(issues.Count!=0) return Result(ChallengeOutcome.InvalidProgram,"CH_INVALID_PROGRAM",issues[0].Message);
            if(identity.IdentityFormat!="RP-CANON-1" || identity.ChallengeId!=definition.Id || identity.ChallengeRevision!=definition.Revision || identity.ScanPeriodMs!=20
                || identity.ProgramSha256!=ChallengeProgramIdentity.ProgramSha256(program) || identity.EngineRevision!="conveyor-engine-v1" || identity.EvaluatorRevision!="conveyor-evaluator-v1"
                || identity.PlantSha256!=ChallengeContentIdentity.PlantSha256(definition.Plant)
                || new[]{identity.ProgramSha256,identity.ChallengeSha256,identity.PlantSha256,identity.EngineSha256,identity.EvaluatorSha256}.Any(h=>h.Length!=64 || h.Any(c=>!char.IsAsciiHexDigit(c) || char.IsUpper(c))))
                return Result(ChallengeOutcome.InvalidChallenge,"CH_INVALID_CHALLENGE","Identity does not match validated content/program revisions.");
            // Save/Load creates worker-owned lists and nested nodes; no caller graph is executed.
            var frozen=LadderProgramJson.Load(LadderProgramJson.Save(program)).Program!;
            issues=ChallengeEligibilityValidator.Validate(frozen);
            if(issues.Count!=0) return Result(ChallengeOutcome.InvalidProgram,"CH_INVALID_PROGRAM",issues[0].Message);
            foreach(var test in definition.Cases)
            {
                cancellationToken.ThrowIfCancellationRequested(); activeCase=test.Id;
                var compiled=LadderCompiler.Compile(frozen);
                if(!compiled.IsValid) return Result(ChallengeOutcome.InvalidProgram,"CH_INVALID_PROGRAM","Worker compile failed.");
                var session=new VirtualControllerSession(new VirtualControllerRuntime(compiled.Program!));
                var plant=createPlant() ?? throw new InvalidOperationException("Plant factory returned null.");
                var evaluator=new ChallengeEvaluator(); evaluator.BeginCase(identity,test);
                int cycle=0; long sequence=0; var protocol=new ConveyorChallengeProtocol(test.Kind);
                VirtualControllerScanCompleted? accepted=null; int eventCount=0;
                session.ScanCompleted += row => {accepted=row; eventCount++;};
                void Metadata(ChallengeTraceEventKind kind,string detail)
                {
                    if(events.Count>=32 || events.Count+trace.Count>=2048) throw new TraceLimitException();
                    var state=plant.CaptureState(); ValidateState(state);
                    events.Add(new(test.Id,cycle,sequence,kind,session.Snapshot.ScanNumber,session.Snapshot.SimulatedTime,state,detail));
                }
                void Reset()
                {
                    cancellationToken.ThrowIfCancellationRequested(); session.Reset(); plant.Reset();
                    var state=plant.CaptureState();
                    if(session.Snapshot.ScanNumber!=0 || session.Snapshot.SimulatedTime!=TimeSpan.Zero || session.Snapshot.Outputs.Values.Any(v=>v)
                        || session.Snapshot.Forces.Count!=0 || !state.ObjectPresent || state.Completed!=0 || !state.Numbers.TryGetValue("object_position",out var position) || Math.Abs(position)>1e-9
                        || !state.Feedback.TryGetValue("simulated_photoeye",out var occupied) || occupied)
                        throw new InvalidOperationException("Reset checkpoint did not restore loaded origin and controller zero state.");
                }
                Reset(); Metadata(ChallengeTraceEventKind.CaseStarted,"Fresh controller and plant."); session.Run();
                while(sequence<250)
                {
                    // Fixed seven protocols cannot exceed 7*250 accepted rows. At most two
                    // lifecycle events per case plus one CV07 reset => 15 metadata records.
                    // Guards still fail closed if a future protocol violates 1750/32/2048.
                    cancellationToken.ThrowIfCancellationRequested();
                    long next=session.Snapshot.ScanNumber+1;
                    foreach(var variable in frozen.Variables.Where(v=>v.Binding is "operator.start" or "operator.stop"))
                    { session.SetInput(variable.Name,false); if(variable.Binding=="operator.start"?protocol.Start(next):protocol.Stop(next)) session.PulseInput(variable.Name); }
                    accepted=null; eventCount=0;
                    session.Advance(.02,()=>SceneIoImageMapper.SampleBoolInputs(frozen,plant.SampleBoolInputs()),
                        ()=>SceneIoImageMapper.SampleNumericInputs(frozen,plant.SampleNumericInputs()),
                        output=>plant.CommitBoolOutputs(SceneIoImageMapper.CommitBoolOutputs(frozen,output)),
                        output=>plant.CommitNumericOutputs(SceneIoImageMapper.CommitNumericOutputs(frozen,output)),
                        seconds=>plant.Advance(TimeSpan.FromSeconds(seconds)),
                        ()=>SceneIoImageMapper.SampleTextInputs(frozen,plant.SampleTextInputs()),
                        output=>plant.CommitTextOutputs(SceneIoImageMapper.CommitTextOutputs(frozen,output)));
                    cancellationToken.ThrowIfCancellationRequested();
                    if(eventCount!=1 || accepted is null || accepted.ScanNumber!=next) throw new InvalidOperationException("Exactly one accepted-scan event required.");
                    if(trace.Count>=1750 || events.Count+trace.Count>=2048) throw new TraceLimitException();
                    var input=frozen.Variables.Where(v=>v.Role==PlcVariableRole.Input && v.Binding.Length>0).ToDictionary(v=>v.Binding,v=>accepted.EffectiveInputs.Bools[v.Name],StringComparer.Ordinal);
                    var sampled=frozen.Variables.Where(v=>v.Role==PlcVariableRole.Input && v.Binding.Length>0).ToDictionary(v=>v.Binding,v=>accepted.SampledInputs.Bools.GetValueOrDefault(v.Name),StringComparer.Ordinal);
                    if(input.Any(p=>p.Value!=sampled[p.Key])) throw new InvalidOperationException("Trial sampled/effective input mismatch.");
                    var stateAfter=plant.CaptureState(); ValidateState(stateAfter);
                    if(accepted.EffectiveInputs.Bools.Count>32 || accepted.Outputs.Bools.Count>32 || accepted.Diagnostics.Count>32 || accepted.Diagnostics.Any(d=>d.Length>256)) throw new TraceLimitException();
                    var observation=new ChallengeObservation(identity,test.Id,cycle,++sequence,accepted.ScanNumber,accepted.SimulatedTime,input,
                        SceneIoImageMapper.CommitBoolOutputs(frozen,accepted.Outputs.Bools),stateAfter,accepted.Diagnostics);
                    trace.Add(observation);
                    if(accepted.Diagnostics.Count>0) {cases.Add(evaluator.Observe(observation)!); return Result(ChallengeOutcome.InfrastructureError,"CH_RUNTIME_DIAGNOSTIC","Runtime semantic diagnostic stopped trial.");}
                    if(evaluator.Observe(observation) is not null) break;
                    protocol.Accept(next,observation.PlantAfter);
                    if(protocol.CycleFinished(next))
                    {
                        if(test.Kind==ChallengeCaseKind.ResetRepeat && cycle==0)
                        { cycle++; Reset(); protocol=new(test.Kind); Metadata(ChallengeTraceEventKind.ResetCheckpoint,"Controller/session/plant reset; accepted sequence retained."); session.Run(); continue; }
                        break;
                    }
                }
                cases.Add(evaluator.FinishCase()); Metadata(ChallengeTraceEventKind.CaseFinished,cases[^1].Code);
            }
            cancellationToken.ThrowIfCancellationRequested();
            bool passed=cases.Count==7 && cases.All(c=>c.Outcome==ChallengeOutcome.Passed);
            return Result(passed?ChallengeOutcome.Passed:ChallengeOutcome.Failed,passed?"CH_PASSED":"CH_FAILED","Seven independent cases executed.");
        }
        catch(OperationCanceledException) {return Result(ChallengeOutcome.Cancelled,"CH_CANCELLED","Cancellation stopped trial; unexecuted cases are Not Run.");}
        catch(TraceLimitException)
        {
            if(activeCase.Length>0 && !cases.Any(c=>c.CaseId==activeCase)) cases.Add(new(activeCase,ChallengeOutcome.InfrastructureError,"CH_TRACE_LIMIT","Bounded primitive accepted observations.","Retained observation or primitive image bound exceeded.",null));
            return Result(ChallengeOutcome.InfrastructureError,"CH_TRACE_LIMIT","Retained observation or primitive image bound exceeded.");
        }
        catch(Exception exception)
        {
            if(activeCase.Length>0 && !cases.Any(c=>c.CaseId==activeCase)) cases.Add(new(activeCase,ChallengeOutcome.InfrastructureError,"CH_ADAPTER_ERROR","Valid offline accepted-scan boundary.",exception.Message,null));
            return Result(ChallengeOutcome.InfrastructureError,"CH_ADAPTER_ERROR",exception.Message);
        }
    }
    private static void ValidateState(ChallengePlantState state)
    {
        if(state.Feedback.Count>32 || state.Numbers.Count>16 || state.Text.Count>16
            || state.Feedback.Keys.Concat(state.Numbers.Keys).Concat(state.Text.Keys).Any(k=>k.Length>256)
            || state.Text.Values.Any(v=>v.Length>256) || state.Numbers.Values.Any(v=>!double.IsFinite(v))) throw new TraceLimitException();
    }
    private sealed class TraceLimitException : Exception { }
}
