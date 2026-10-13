# Controller, plant, and software architecture department

Date: 2026-10-10. Status: proposed design submitted to Dot; implementation requires the user's final authorization. Static source inspection only. No build, application launch, installation, PLC contact, commit, or push occurred.

## Executive decision

Build an offline challenge layer around the current Godot/C# application. Preserve the ladder language, controller semantics, existing demos, sandbox scenes, and guarded external PLC path. Start with one conveyor challenge and a renderer-free executable grading harness; do not rewrite the plant framework or ship a second programming language. Make grading a separate repeatable operation from exploratory playback.

The authoritative inspection target was `Siemens-PLC-PC-Interface/rungproof`, including current dirty working files. Root and RungProof AGENTS instructions were read. `docs/AI_HANDOFF.md`, README, PROJECT_INFORMATION, and CONTEXT were consulted; CONTEXT describes intended renderer-neutral ownership more broadly than the present Godot implementation achieves. Existing historical verification counts are background evidence, not newly reproduced results.

## Source evidence and seams

All paths below are relative to `rungproof-next/`.

| Existing source | Verified behavior | Consequence |
| --- | --- | --- |
| `src/VirtualController/VirtualControllerSession.cs` | `Advance` samples bool/numeric/text inputs, runs `_runtime.Scan`, commits outputs, advances plant by ScanPeriod, clears one-scan pulses, then publishes snapshot. Batches cap at 1000 scans and retain backlog. | Use accepted scans rather than frame time or dropped backlog for grading. |
| Same session | `Run`, `Stop`, `Reset`, and force methods also publish `SnapshotPublished`. | Publication is not a reliable completed-plant-scan event. |
| `src/VirtualController/VirtualControllerRuntime.cs` | Owns ladder memory, timers, counters, edges, forces, task execution; Snapshot includes ScanNumber and SimulatedTime. Stop clears outputs and nonretentive timers; Reset clears memories, scan time and forces but restores declared initial values. | Preserve simulator semantics; grader must reject initially energized challenge outputs separately rather than change Reset globally. |
| `src/App/Main.cs`, EnableVirtualControllerProgram, OnVirtualSnapshot, CommitVirtualControllerSnapshot | Compiles program, creates session, attaches observer, selects external clock, resets scene optionally; lifecycle output projection follows snapshot callbacks. | Main owns integration; add a focused coordinator rather than growing a second runtime inside Main. |
| `src/App/Main.cs`, RunActiveController / StopActiveController / ResetActiveController | Toolbar Stop forcibly stops runtime. Offline Reset resets plant then runtime and projects outputs. External mode uses a different lifecycle. | A learner's Stop-button behavior must be tested by `operator.stop` pulse, not toolbar Stop. Toolbar Stop during test means cancellation. |
| `src/VirtualController/SceneIoBindingValidator.cs` | Validates symbolic bindings, direction, types, aggregates and duplicate output bindings; special BOOL-input bindings `operator.start`, `operator.stop`. | Combine binding validation with LadderCompiler; neither substitutes for the other. |
| `src/VirtualController/SceneIoImageMapper.cs` | Maps named program variables and symbolic scene bindings. | Resolve stimuli by binding, not fixed variable names. Accept learner renaming. |
| `src/VirtualController/SceneLadderProject.cs` | Creates declared-I/O exercises without creating the solution. | Reuse for starter document; optional required memory tag remains editable. |
| `src/VirtualController/AuthoredDemoLadderPrograms.cs` | Demo 3 has start/stop inputs, photoeye, seal_in and conveyor output for `scene-1-conveyor-stop`. | Use as one positive fixture, not as the only accepted solution. |
| `src/Scenes/ConveyorPlantModel.cs` | Pure C# model exposes LeadingEdge nullable, ObjectPresent, PhotoeyeBlocked, Completed, State; Reset/Step. | First challenge can be tested without Godot. |
| `src/Scenes/SceneSimulationRuntime.cs` | Godot Node with mutable `_points`; public Points exposes same mutable dictionary via IReadOnlyDictionary. `AdvanceConveyorStop` steps model, projects scene, applies bindings, emits StateChanged. | Copy observations. No claim that the whole plant runtime is already renderer-neutral. |
| Same runtime, ProjectConveyorPlant | Discharged carton projects object_position as zero; parts_completed increments and component_state changes. | Position alone can falsely pass a carton that left the belt. Observe presence/completion/state too. |
| `scenes/migrated/scene-1-conveyor-stop.scene.json` | length 1 m, speed .5 m/s, carton .2 m, beam .5 m, minimum beam hold .1 s; `productionApproved:false`. | Numeric challenge tolerance must come from this model; asset acceptance remains a separate gate. |
| `src/VirtualController/LadderEditorProjectJson.cs` | Schema 1 deliberately saves compiler-invalid draft documents. | Continue saving failed/incomplete solutions. Grading eligibility remains separate. |
| `src/Workspace/WorkspaceRepository.cs` | Validated versioned workspace serialization with same-directory temporary write and replacement. | Reuse atomic-write pattern for progress; do not embed progress in engineering workspace schema. |

## Requirement contract

- ARCH-01: Challenge execution is built-in offline only. It must not instantiate or call GuardedRuntimeClient or switch a connected external PLC into a test.
- ARCH-02: A run freezes challenge revision, program content hash, plant parameter revision, scan period and evaluator revision. Edited programs cannot inherit a current passing result.
- ARCH-03: Observations are copied at every completed accepted scan. Keep sampled input image, commands applied this scan, and post-step plant feedback distinct.
- ARCH-04: Each independent test case starts with fresh controller/session and freshly reset plant, no forces, no pending pulses, no timing backlog. Explicit repeat-cycle cases reuse state deliberately.
- ARCH-05: Grade observable requirements, not rung topology or a reference-program hash. At least two structurally different valid solutions must pass.
- ARCH-06: Stop priority and edge behavior are tested through declared operator bindings. Runtime Stop is a test cancellation, never evidence of correct learner Stop logic.
- ARCH-07: Bound maximum scans, trace memory and scenario size. Reject nonfinite numeric values, unknown points and malformed authored predicates before running.
- ARCH-08: Result outcomes distinguish Passed, Failed, InvalidProgram, InvalidChallenge, Cancelled and InfrastructureError. Only Passed updates completion.
- ARCH-09: Draft and progress writes preserve prior good files on failure. Missing challenge revisions mark saved results historical rather than currently valid.
- ARCH-10: Every model-level accepted solution must also pass actual Godot integration using the same program and scenario. Visual/native acceptance is independent from grading tests.

## Proposed interfaces: exact names, deliberately new

These are proposed types, not APIs that currently exist. Departmental plans can use these names until the studio meeting freezes them. Namespace `RungProof.Next.Challenges`.

```csharp
public sealed record ChallengeRunIdentity(
    string ChallengeId, int ChallengeRevision, string ProgramSha256,
    string PlantRevision, string EvaluatorRevision, long RunEpoch);

public sealed record ChallengeObservation(
    ChallengeRunIdentity Identity, string CaseId, long ScanNumber,
    TimeSpan SimulatedTime,
    IReadOnlyDictionary<string, bool> SampledInputs,
    IReadOnlyDictionary<string, bool> AppliedOutputs,
    IReadOnlyDictionary<string, bool> PlantFeedbackAfter,
    IReadOnlyDictionary<string, double> PlantNumbersAfter,
    IReadOnlyDictionary<string, string> PlantTextAfter,
    IReadOnlyList<string> RuntimeDiagnostics);

public interface IChallengePlantAdapter
{
    void Reset();
    IReadOnlyDictionary<string, bool> SampleBoolInputs();
    IReadOnlyDictionary<string, double> SampleNumericInputs();
    IReadOnlyDictionary<string, string> SampleTextInputs();
    void CommitBoolOutputs(IReadOnlyDictionary<string, bool> outputs);
    void CommitNumericOutputs(IReadOnlyDictionary<string, double> outputs);
    void CommitTextOutputs(IReadOnlyDictionary<string, string> outputs);
    void Advance(TimeSpan period);
    ChallengePlantState CaptureState(); // Copied state, never runtime.Points directly.
}

public interface IChallengeEvaluator
{
    void BeginCase(ChallengeRunIdentity identity, ChallengeTestCase testCase);
    ChallengeCaseStatus Observe(ChallengeObservation observation);
    ChallengeCaseResult FinishCase();
}

public sealed class ChallengeTestRunner
{
    public ChallengeRunResult Run(ChallengeDefinition definition,
        LadderProgram program, Func<IChallengePlantAdapter> createPlant,
        System.Threading.CancellationToken cancellationToken);
}
```

ChallengePlantState contains copied bool/numeric/text values plus explicit ObjectPresent for this conveyor adapter. Type definitions for ChallengeDefinition, ChallengeTestCase, results and enum values are owned by this department in `ChallengeContracts.cs`, with final objective/case data owned by controls/curriculum. Single conveyor scope supports BOOL stimuli; numeric/text observation fields avoid unsafe untyped conversion but do not commit us to all instruction/case authoring features now. No runtime predicates supplied as arbitrary C#, eval strings, scripts, shell commands or downloadable code. Use a small validated authored predicate enum plus explicit typed arguments.

Session seam proposal: add a dedicated `ScanCompleted` event with immutable `VirtualControllerScanCompleted` containing pre-scan sampled images and current controller snapshot, emitted only after commit+advance. Do not repurpose SnapshotPublished. A per-run adapter then captures copied post-plant state in that callback. Copy bool/numeric/text samples before execution; never infer sampled-before from plant-after. Keep this event renderer-neutral. For the slice an equally valid smaller alternative is wrapping callbacks in ChallengeTestRunner and capturing in the final advance callback; choose one pattern once at studio meeting, not two independent clocks. Preferred event reduces incidental coupling and permits exact frame-independent trace tests. Stop/Reset/force retain existing publication semantics.

## Test ownership and first challenge behavior

The test runner owns case setup and time; controller owns LD memory; plant owns travel and sensor generation; evaluator reads observations and cannot change commands or plant. ChallengeSessionCoordinator owns UI mode and cancellation. Run under test is a frozen program separate from the live editor draft. Do not destroy the player's exploratory plant state silently: show that Test runs a fresh trial, or snapshot/restore only if all plant-private state becomes serializable. Initial release should explicitly reset the shown trial, leave ladder edits intact, and finish stopped.

Challenge 1 candidate case table, final wording/thresholds subject to controls approval:

| Case | Stimulus | Required result | Negative fixture |
| --- | --- | --- | --- |
| Cold start | Reset and advance without Start | Conveyor output remains off; carton remains at origin | Unconditional output |
| Ordinary carton | Start pulse, then release | Travel occurs, raw beam blocks, sampled beam is seen next scan, command off by that scan, carton remains present | Missing photoeye contact |
| Operator Stop | Start, advance 10 scans, Stop pulse | Command off next accepted scan and remains off after release until new Start | Missing Stop contact |
| Simultaneous Start/Stop | Both pulses on same scan | Stop priority; conveyor off | Start-priority seal-in |
| Reset/repeat | Complete trial, explicit reset, new Start | Fresh normal result; no stale pulse/edge/force | State carried across reset |
| No unexpected restart | After photoeye stop, wait 25 scans | Output remains off without new Start | Oscillating timer or feedback-driven restart |

At 20 ms and .5 m/s each commanded scan moves .01 m. A sensor that becomes true after plant step n enters the controller input image at scan n+1; require off on n+1, not n. Fix exact acceptable position interval after model tests expose floating-point boundary behavior. Specify tolerance from speed*scan and epsilon; avoid unexplained .5 m equality. Completion requires actual travel, a witnessed beam transition, stopped command, carton present, unchanged completed count, and hold duration. Nonmovement cannot pass. A discharged carton cannot pass. Scenario Stop before beam must not grade as successful ordinary carton.

Minimum negative matrix also includes wrong binding/type, malformed/empty program, initially true output, force-assisted playback, manual sensor toggles, scene change mid-test, cancellation, runtime diagnostics, maximum-scan timeout, stale result after edit, corrupted progress and disk-write failure. Reject blocked/limited instructions on Verify+Load/Test, not only by hiding palette controls; imported programs can bypass a palette.

## File touchpoints and scoped implementation tasks

New proposed files:

- `src/Challenges/ChallengeContracts.cs`: versioned metadata, case/stimulus/predicate/result/observation definitions and shape validation.
- `src/Challenges/ChallengeTestRunner.cs`: deterministic offline case lifecycle; cancellation, budgets and observation copies.
- `src/Challenges/ConveyorChallengePlantAdapter.cs`: wraps existing ConveyorPlantModel, reads same scene parameters, publishes presence and model state.
- `src/Challenges/ChallengeEvaluator.cs`: temporal requirements and first-failure diagnostics.
- `src/Challenges/ChallengeProgressRepository.cs`: separate atomic local result storage with historical revision handling.
- `src/App/ChallengeSessionCoordinator.cs`: Godot integration, editor/program eligibility, source-mode guard, tests and UI result events.
- `src/App/SimulatorShell.Challenges.cs`: bounded challenge panel/navigation extension; existing SimulatorShell is partial.
- `challenges/catalog.json` and `challenges/conveyor-stop.challenge.json`: new authored content, separate from scene JSON.
- `tests/Challenges/RungProof.Next.Challenges.Tests.csproj` and `Program.cs`: pure executable tests consistent with current test projects.

Potential existing changes: `VirtualControllerSession.cs` for dedicated scan boundary; `Main.cs` to compose coordinator and attach scan observations; `SimulatorShell.cs` for challenge entry and coordinator events; `SceneSimulationRuntime.cs` for copied capture/access to conveyor presence (prefer narrowly typed read-only capture, no external setter); project glob exclusions for a proposed additional test directory are already covered by tests/**/*.cs. Leave LadderCompiler and runtime instruction execution unchanged unless a meaningful regression proves a defect. Keep `WorkspaceRepository`, ladder project schema and scene contract intact for the first slice.

Implementation tasks after authorization:

1. Executable contract/harness: write failing boundary/reset/renamed-binding tests, implement minimal contracts and conveyor adapter+runner; positive and negative fixtures. Deliverable: command-line test result proves no-force behavior and required failure discrimination without Godot.
2. Native coordinator: integrate one challenge with existing ladder editor/split scene, compile and binding eligibility, separate Run/Test/Cancel state, fresh trial semantics and copied observations. Deliverable: actual Godot integration proves loaded learner code drives carton and sensor.
3. Temporal feedback and persistence: first failing event with scan/time and phase, hint hook, invalid draft retention, atomic progress and result identity; prove edits invalidate current result.
4. Visual/native acceptance: inspect actual rendered objective/editor/machine/feedback in target resolutions and focus states; repeat existing demo and external-offline regressions; qualify offline package separately. This task belongs jointly to QA/UI, architecture supplies harness and evidence.

Every task needs a failing behavioral test before implementation, smallest patch, reviewer gate and documented evidence. Commit/push/release policy remains user's explicit authorization; this proposal itself does not authorize those actions.

## Grounded verification plan, not executed

Existing projects use .NET 10; existing portable dotnet and Godot 4.7.2 paths are declared in DEVELOPMENT_SETUP. Each project must isolate all three build directories; do not use shared tests/obj or parallel default outputs. From `rungproof-next/`, example existing-controller lane:

```powershell
$env:DOTNET_ROOT = Join-Path $PWD '.tools/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$taskBuildRoot = Join-Path $PWD '.tools/studio-verification'
$controllerBuild = Join-Path $taskBuildRoot 'controller'
& '.tools/dotnet/dotnet.exe' restore 'tests/RungProof.Next.VirtualController.Tests.csproj' "-p:BaseIntermediateOutputPath=$controllerBuild/obj/" "-p:MSBuildProjectExtensionsPath=$controllerBuild/obj/"
& '.tools/dotnet/dotnet.exe' build 'tests/RungProof.Next.VirtualController.Tests.csproj' --no-restore "-p:BaseIntermediateOutputPath=$controllerBuild/obj/" "-p:MSBuildProjectExtensionsPath=$controllerBuild/obj/" "-p:OutputPath=$controllerBuild/bin/"
& '.tools/dotnet/dotnet.exe' "$controllerBuild/bin/RungProof.Next.VirtualController.Tests.dll"
```

Repeat with distinct directories and actual assembly names for `tests/Plant/RungProof.Next.Plant.Tests.csproj`, `tests/OfflineDemo/RungProof.Next.OfflineDemo.Tests.csproj`, and proposed `tests/Challenges/RungProof.Next.Challenges.Tests.csproj`. Restore may require package access; authorize execution separately, don't install anything during planning. Reject nonzero exit or missing result output; a timeout/forced exit is not a pass.

Build app using existing `& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore` normal Godot output location. Existing native verifier flags in Main: `--verify-app-shell`, `--verify-virtual-controller`, `--verify-ladder-editor`, `--verify-split-view`, `--verify-demo-sequence`, `--verify-scene-controls`, `--verify-gallery-clock`, `--verify-external-playback`. Locate exact portable console Godot path, then invoke `--headless --path . -- <flag>`. Add proposed `--verify-challenge-conveyor` only during implementation; do not represent it as existing. Determine supported visual/headless needs per verifier; live PLC tests are excluded. Capture source/program/challenge/build hashes, command, exit, assertions and native visual artifacts separately.

## Alternatives and risks for the meeting

| Decision | Recommendation | Rejected/deferred alternative |
| --- | --- | --- |
| Whole plant abstraction | Narrow conveyor adapter now | Refactoring 77 scenes first multiplies regressions and delays playable evidence. |
| Test isolation | Frozen program and fresh deterministic trial, finish stopped | Mutating exploratory state mid-run creates unexplained outcomes. |
| Content | Validated data predicates and explicit cases | General scripting exposes execution/security/versioning complexity. |
| Grading | Behavioral plus contract eligibility | Exact authored rung comparison prevents alternate valid solutions. |
| Offline source | Coordinator denies external execution | Reusing PLC menu selection as a test runner can cross real transport boundary. |
| Saved results | Separate local versioned JSON and content identity | Cloud accounts, leaderboards and anti-cheat are unnecessary for first slice. |

Principal risks: duplicated adapter versus native plant mapping; false pass from phase confusion; viewport focus/shortcuts interference; forces or reference scene actions assisting a test; stale document versus loaded program; erroneous output initial values; model/scene parameter drift; insufficient failure vocabulary; current dirty WIP conflicts. Mitigate through shared model/parameters, native parity tests, bounded copied observations, explicit offline source guard, program hashes and current-source reinspection before implementation.

## Dependencies, uncertainty and estimate

Controls/curriculum must freeze exact objectives, response scan allowance, Stop/Start priority, Reset semantics and allowed instruction sets. UX must decide test reset visibility and draft/loaded program presentation. Art must qualify beam/carton/machine readability independently of symbolic plant correctness. QA must own independent negative fixtures and native evidence. Production must preserve dirty source baseline and assign exclusive ownership of Main/SimulatorShell edits.

Planning estimate, not commitment: 4-7 focused engineering days for harness/contracts and first challenge, 3-6 for native integration/feedback/persistence, 2-4 joint engineering/QA days for regression and native acceptance. Total roughly 9-17 person-days excluding art replacement, content review, package/release qualification, WIP conflict repair and user testing. Confidence medium-low until baseline builds and native test controls are rerun after authorization. Do not translate this directly into elapsed calendar days or agent-token promises.

Dot should reject implementation approval if scan-phase semantics, ordinary versus toolbar Stop, fresh trial behavior, learner instruction restrictions, independent negative fixtures, or dirty baseline ownership remain undecided. Architecture signoff applies to this offline slice only, not real PLC, physical safety or public launch readiness.
