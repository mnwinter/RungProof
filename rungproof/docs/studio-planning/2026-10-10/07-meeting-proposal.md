# Studio meeting proposal — Dot, 2026-10-10

This document follows completed departmental reports 01–06. It is the common agenda for actual asynchronous cross-department critique, not a claim that a meeting has already occurred or implementation is authorized. Root and Dot coordinate; six distinct agents represent six departments. Four concurrent slots require review waves, not a simultaneous six-agent call. Findings remain static source inspection; human screen is reserved.

## Proposed scope and hierarchy

Recommend a controls-learners-first Windows offline machine-puzzle mode alongside retained demos/sandbox. First implementation tranche is one complete conveyor challenge using `scene-1-conveyor-stop`. Audience, tranche, support scope and labor budget require human approval. Full roadmap: baseline/contracts → pure behavioral harness → native playable slice → internal exact package/environment qualification → observed learner pilot/revision → separately authorized small campaign → separately authorized public release. Campaign proposal: contact/coil primer, conveyor hold/feedback, delayed lamp, then edge counting/batch, then sequence/palletizer after its recovery contract. No wholesale 77-scene conversion, second language, cloud/accounts, scoring, telemetry, sound, asset rebuild, orthographic rewrite or live challenge execution.

Existing source is authoritative; current release/handoff evidence is attributed historical evidence. Old Qt sections and generated help are not current proof. Reports are investigations containing alternatives. Final spec and meeting resolutions supersede their contradictory recommendations; retain reports unchanged for provenance.

## Decisions proposed for ratification

| ID | Proposed resolution | Reason / displaced alternative |
| --- | --- | --- |
| D01 | Test freezes learner executable document, runs a fresh pure model/controller worker, leaves interactive plant/document/history/path/unsaved baseline untouched; no replay MVP | Supersedes Architecture visible-reset trial option; UI labels historical isolated trace |
| D02 | Worker calls existing Session.Advance with exactly one 20 ms period per iteration; dedicated copied ScanCompleted event emitted only when ScanNumber increments and after commit+plant step | SnapshotPublished includes lifecycle/forces; no second clock or stopped-loop false samples |
| D03 | Application Stop cancels test and performs normal interactive Stop; ordinary process Stop is an input stimulus; Cancel only cancels isolated test | Toolbar stop cannot prove learner stop logic |
| D04 | BOOL contact/series/parallel and ordinary Assign coils; BOOL input/memory/output; supported binding aliases; fixed20ms; initial memory/output false; no timer/counter/numeric/jump/call/task grading in slice | Narrow disclosed eligibility enforced on imported programs, not just palette; sandbox remains unrestricted |
| D05 | Behavioral acceptance, two structurally different valid programs plus renamed tags; canonical CV01–07 below; CV08 lifecycle ungraded | Supersedes Architecture six-case subset; no hash/topology oracle |
| D06 | Retain perspective camera and existing meshes/plain floor; challenge-only focus/labels and optional nondecorative beam explanation | Art reuse before rebuild; existing demo framing preserved |
| D07 | Separate atomic local progress; test identity contains program/challenge/plant/evaluator content SHA256 plus revisions, period and monotonically increasing run epoch | No raw editor selection hash; no existing project-schema hijack |
| D08 | Minimum target1280×720 actual logical content at OS100/125/150/200%; baseline1920×1080 logical100%; actual viewport/bounds recorded | Supersedes differing departmental matrices. Physical monitor must fit scaled content+chrome (at200%, ≥2560×1440 plus chrome for minimum). If actual renderer scaling differs, record it and fail/revise target rather than claim unsupported coverage |
| D09 | Full first-challenge keyboard workflow, text+shape state, visible focus, UI text≥16logical and ladder text≥14logical at declared zoom; screen-reader semantic feasibility gate before any support claim | Custom drawing cannot be declared assistive-accessible from source or screenshots |
| D10 | Strict slice bounds below; no generalized script predicate system or unbounded trace | Generic report budgets superseded |
| D11 | Pilot6 people:4 target learners +2 experienced reviewers;3/4 learners complete≤30min uncoached,3/4 explain command/feedback/Stop,2/2 reviewers no unresolved semantic issue | Production exact formative protocol chosen; no market statistical claim, external recruitment separately authorized |
| D12 | Recoverable byte-verified WIP snapshot before authorized source edits; HEAD-only worktree insufficient; one source writer per Main/Shell/runtime/art/release file family | Existing103 modified/untracked files inventory is not backup; preserve concurrent work |

## Canonical conveyor behavior ledger

Freeze from current scene: length1m, speed0.5m/s, carton0.2m, beam0.5m, minimum beam hold0.1s, no pusher/reload. Scan20ms, epsilon1e-9m, commanded travel0.01m/scan. Inputs are resolved by `operator.start`/`operator.stop` bindings, not fixed learner tag names. Start/Stop pulses last one accepted scan. Held-Start rearming is not a promised requirement. Stop priority applies while Stop is sampled true. Photoeye feedback inhibits movement even if Start is sampled true while blocked.

| ID | Exact stimulus and acceptance |
| --- | --- |
| CV01 cold | Fresh Reset; Run;25scans without Start. Output false; present carton edge0±epsilon; completed0 |
| CV02 holding | Fresh;Start pulse scan1;release thereafter;advance through scan10. Applied output true every scan1–10;edge0.1±epsilon,present,completed0. Always-off/nohold fail |
| CV03 beam | Fresh;Start scan1; advance until first PlantFeedbackAfter.photoeye true, at scan n≤75. On scan n+1 SampledInputs.photoeye true and AppliedOutputs.conveyor false. Edge within[0.5−epsilon,0.51+epsilon];present;completed0. Hold25additional accepted scans with no Start:output false,edge unchanged≤epsilon,present,completed0. Require witnessed travel and sensor transition; no initial-position or absent-carton pass |
| CV04 processStop | Fresh;Start scan1;Stop pulse scan11;Start false. Output false scan11 and next25scans;edge0.1±epsilon,present,completed0. Runtime.Stop is never substituted |
| CV05 simultaneous | Fresh;Start+Stop pulses scan1;25scans. No output or movement;Stop dominates |
| CV06 blockedStart | Fresh ordinary run to CV03 stopped state;newStart pulse next scan while sampled beam true;next25scans no output/movement;present/completed0 |
| CV07 resetRepeat | Complete ordinary run/hold; explicit controller+plant Reset in same case, clear pulses/forces/backlog;verify scan0/time0/outputfalse/edge0/present/completed0;Run+newStart;second CV03 outcome required. Learner program unchanged |
| CV08 lifecycle only | Application Stop during live exploration stops normal runtime; during Test cancels worker and stops live source. Cancel alone preserves exploration. No completion awarded on cancellation;Reset preserves draft and cancels pending test before normal reset. Not a graded learner case |

Every graded case≤250accepted scans;7cases,total≤1750. Timeout is Failed. Runtime diagnostics are Failed; invalid compile/binding/eligibility is InvalidProgram; invalid content InvalidChallenge; bad adapter/internal exceptions InfrastructureError; Cancelled never updates completion. Completion demands all seven pass.

## Bounds and persistence

Challenge JSON≤256KiB,depth32,7cases for this definition(max8 allowed schema),32stimuli/case,IDs≤64characters,text≤512,hints≤2048,≤8hints. Draft eligibility≤32variables/64networks/256logic nodes/branchdepth16; count across all blocks and reject hidden unsupported instructions. No reinterpretation of old sandbox project limits. Trace≤2048rows across run;retain full1750max+case reset metadata;exceeding bound is explicit error,no silent truncation/pass. Per row≤32bool inputs/outputs,16plant numbers/text,strings≤256;copied primitives. Progress≤1MiB/1000records,JSONdepth32,exact schema version,unknown future version readable error and original preserved. IDs never become file paths. Trusted authored data uses enums+arguments,no code/shell/eval/remote predicates.

Program digest proposal: current `LadderProgramJson.Save(program)` → parse → recursively ordinal-sort object properties,preserve array order,normalize finite numbers invariant roundtrip →UTF8 SHA256. Exclude root display `name`,watchVariables,id only;retain node/network IDs/labels,variables/bindings/initials/order,blocks/tasks/period. Full challenge content and frozen plant parameter JSON independently canonical-hashed; evaluator revision and assembly/content hash included. Editor selection/timestamps absent compiled IR. Any draft mutation invalidates displayed current proof immediately; freshTest only reinstates it. Historical pass remains historical. Save failure says passed/progress not saved;no false persisted claim. Existing `.rpproj.json` unchanged. User data fixed relativefilename `challenge-progress.json` under existing per-build user data path,serialized writes,tmp+replace plus lastgood backup,corrupt originals quarantined without deletion.

## Proposed exact new shared contracts

Namespace `RungProof.Next.Challenges`;all listed types are NEW proposals,not existing APIs.

```csharp
public enum ChallengeOutcome { Passed, Failed, InvalidProgram, InvalidChallenge, Cancelled, InfrastructureError }
public enum ChallengeCaseKind { ColdStart, Holding, BeamStop, ProcessStop, SimultaneousStartStop, BlockedStart, ResetRepeat }
public enum ChallengeUiState { Idle, Validating, Running, Cancelling, Finished }
public sealed record ConveyorPlantParameters(double Length, double Speed, double ObjectLength, double PhotoeyePosition, double MinimumPhotoeyeOn);
public sealed record ChallengeDefinition(string Id,int Revision,string SceneId,string Title,string Objective,IReadOnlyList<string> Hints,ConveyorPlantParameters Plant,IReadOnlyList<ChallengeTestCase> Cases);
public sealed record ChallengeTestCase(string Id,ChallengeCaseKind Kind,int MaximumScans);
public sealed record ChallengeRunIdentity(string ChallengeId,int ChallengeRevision,string ProgramSha256,string ChallengeSha256,string PlantSha256,string EvaluatorRevision,string EvaluatorSha256,int ScanPeriodMs,long RunEpoch);
public sealed record ChallengePlantState(IReadOnlyDictionary<string,bool> Feedback,IReadOnlyDictionary<string,double> Numbers,IReadOnlyDictionary<string,string> Text,bool ObjectPresent,long Completed);
public sealed record ChallengeObservation(ChallengeRunIdentity Identity,string CaseId,int Cycle,long ScanNumber,TimeSpan SimulatedTime,IReadOnlyDictionary<string,bool> SampledInputs,IReadOnlyDictionary<string,bool> AppliedOutputs,ChallengePlantState PlantAfter,IReadOnlyList<string> RuntimeDiagnostics);
public sealed record ChallengeCaseResult(string CaseId,ChallengeOutcome Outcome,string Code,string Expected,string Observed,ChallengeObservation? FirstFailure);
public sealed record ChallengeRunResult(ChallengeRunIdentity Identity,ChallengeOutcome Outcome,IReadOnlyList<ChallengeCaseResult> Cases,IReadOnlyList<ChallengeObservation> Trace,string Diagnostic);
public interface IChallengePlantAdapter { void Reset(); IReadOnlyDictionary<string,bool> SampleBoolInputs(); IReadOnlyDictionary<string,double> SampleNumericInputs(); IReadOnlyDictionary<string,string> SampleTextInputs(); void CommitBoolOutputs(IReadOnlyDictionary<string,bool> outputs); void CommitNumericOutputs(IReadOnlyDictionary<string,double> outputs); void CommitTextOutputs(IReadOnlyDictionary<string,string> outputs); void Advance(TimeSpan period); ChallengePlantState CaptureState(); }
public sealed class ChallengeTestRunner { public ChallengeRunResult Run(ChallengeDefinition definition,LadderProgram program,ChallengeRunIdentity identity,Func<IChallengePlantAdapter> createPlant,System.Threading.CancellationToken cancellationToken); }
```

Session boundary NEW `VirtualControllerScanCompleted` copies actual sampled bool/numeric/text images after operator overlays plus current snapshot. `VirtualControllerSession.ScanCompleted` fires after accepted scan's commits+plantadvance only,not lifecycle/force/idle. Runner callback copies adapterstate. Native verifier captures narrow scene observation after same acceptedboundary;no worker Godot calls. Main integration owns coordinator:freeze snapshot→compile/binding/eligibility→hash→Task.Run(pure runner)→mainthread result dispatch(epochguard). Cancel token checked each acceptedscan;worker must exit/cancel and delivery cannot award after cancel or changedepoch. UI lock only while test;Cancel always keyboardreachable;navigation/close waits bounded worker cleanup. Proposed250ms cancel responsiveness measured on declared referencehardware;countbound alone is not timeproof.

## Task/interface skeleton for meeting critique

T0 recovery/baseline(internal release owner);T1 contracts/content/digest/eligibility(challenge owner);T2 completedscan/runtime+adapter(runtime owner);T3 runner/evaluator+independent fixtures(challenge+QA);T4 separate progress(challenge persistence owner);T5 coordinator/Main(native integration owner);T6 presenter/Shell/editor keyboard(UI/editor owners);T7 challenge camera/labels reuse(technicalart,Main changes requested through owner);T8 native fixture parity/visual/DPI/keyboard/offline regressions(QA);T9 exact local package/contentmanifest/recovery/support/rights disposition(release);T10 separately authorized observed pilot/revision(product). Dependencies:T1→T2/T3/T4→T5→T6/T7→T8→T9→T10;content/rights review parallel,sharedsource edits serialized.

Exact first milestone new files:`src/Challenges/ChallengeContracts.cs`,`ChallengeDefinitionJson.cs`,`ChallengeProgramIdentity.cs`,`ChallengeEligibilityValidator.cs`,`ConveyorChallengePlantAdapter.cs`,`ChallengeTestRunner.cs`,`ChallengeEvaluator.cs`;existing `src/VirtualController/VirtualControllerSession.cs`;new `challenges/conveyor-stop.challenge.json`;new `tests/Challenges/RungProof.Next.Challenges.Tests.csproj`,`Program.cs`,`ChallengeFixtures.cs`. Tests source-link current pure `src/VirtualController/*.cs`,`src/Scenes/ConveyorPlantModel.cs`,new`src/Challenges/*.cs`;no Godot/app reference. net10.0,warnings-as-errors,defaultcompile disabled consistent with existing console projects. No arbitrary predicate abstraction needed:caseenum implements seven fixed disclosed protocols.

Tests must prove twovaliddifferent ladders+renamedbindings;alwaysoff/unconditional/nohold/nostop/nosensor/priority/restart/discharge negatives;exactphase and eventcount;deepcopy;freshcaseorder;20ms+batch equivalence;250/1750budget;cancel;malformeddata/limits;currentresultinvalidation. Existing app/plant/controller/connection/offlinepolicy lanes remain scoped regressions. New native verifier proposed `--verify-challenge-conveyor` not existing. Final plan must provide exact isolatedMSBuild commands and cleanexit+PASSmarker requirements. Native screenshots/realkeyboard and packagecleanmachine remain separate,no run in planning.

## Scope/effort and human choices

Production disjoint40–67focused person-days covers baseline,engine,UI,onecontent,requiredreuseart,independentQA,internalpackage andonepilotcorrection;not summeddepartment estimates,not calendar promise. Excludes full screenreader remediation,assetrebuild,audio,hardwareprocurement/legal/signing acquisition,storefront/fullcampaign/livePLC. Contract/cancel/native feasibility issues can require reestimate. Choose one-slice approval versus largercampaign;Windowsminimums/accessibility target;controls-first audience;WIPintegration strategy;budget tolerance;externalpilot/release authority. Default recommendation internal one-slice only. Human approval required before any implementation;Dot plan verdict is no runtime/legal/safety certification.

## Department response requested

Read all report summaries and cross-dependency sections plus this proposal. Respond with specific defects/contradictions/omissions, exact amendment, dependencyowner, acceptancerisk and department disposition. Critique another department's interface,not only endorse yours. Write a NEW `meeting-<department>-response.md`. No source/runtime actions. Explicitly say whether any structural planning issue remains;do not manufacture unanimous agreement. Final spec/plan and Dot review follow actual recorded responses.
