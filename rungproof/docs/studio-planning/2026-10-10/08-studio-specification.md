# RungProof machine-puzzle studio specification

Date: 2026-10-10. Status: final planning submission to Dot, awaiting human implementation authorization. Governing companion: `09-implementation-plan.md`. Department investigations and actual cross-review responses are retained as evidence; this document supersedes their incompatible alternatives and the preliminary `07-meeting-proposal.md`.

## 1. Intent, scope and assumptions

The user requests a full studio investigation and planning process, with each section reporting to Dot, a final cross-department meeting, Dot's independent review, and human authorization before implementation. Six distinct agents completed product/learning, UX/editor/accessibility, art/technical-art/audio, architecture, QA/security/release, and production/content/commercial deep dives. Their actual asynchronous critiques identified material defects; the resolutions below are deliberate decisions rather than manufactured unanimous approval.

The screenshot is a reference for visible programmable logic beside an approachable machine. It does not specify another game's internal architecture or authorize copying its art. Recommend controls learners as the primary audience and a Windows offline machine-puzzle mode alongside existing demos and sandbox. These remain proposed human scope choices.

The executable first tranche is **one complete conveyor challenge**, `scene-1-conveyor-stop`, with a focused workspace, learner-owned ladder, behavioral tests, explanatory evidence, hints, local progress and an exact internal candidate. The master roadmap extends through a formative pilot, a curated campaign and potential public release, with separate tranche and delivery gates. Execution is measured by milestone deliverables and dependencies. The historical 40–67 person-days estimates HUMAN LABOR for the first internal pilot slice, not agent processing duration or a finished commercial game.

Retain five authored demos, their numbering/programs, the engineering editor, sandbox and guarded development bridge. Challenge execution is exclusively local/offline and cannot use a PLC. Exclude a second programming language, full-catalog conversion, asset rebuild, orthographic migration, required audio, replay, cloud/accounts, leaderboards, telemetry, storefront work and live challenge mode from the first tranche.

## 2. Baseline and evidence

Canonical repository: `C:/Users/mattn/OneDrive/Documents/ChatGPT/Siemens-PLC-PC-Interface`, branch `main`, HEAD `1aeee4d7ed23fa938f5227942e7727b4ef6981dc`, with substantial existing WIP. `00-baseline.md` and `baseline-working-files.json` inventory 103 existing modified/untracked files. That inventory is not a recoverable backup or a frozen executable. The adjacent `RungProof` directory is not the implementation checkout.

All studio work in this turn is static source/document inspection and new planning documents. No app launch, current screenshot, test, build, export, installation, PLC interaction, commit, push or release occurred. Historical reports remain attributed to their recorded source/artifact. Current Godot code and dated current records govern; legacy Qt/migration statements and generated operation help do not establish current capability.

Before authorized source changes, obtain a recoverable, byte-verified WIP snapshot and reconcile current status against the inventory. A new worktree at HEAD alone omits dependencies of this design. Preserve unrelated source/assets/generated files. Select a recovery/integration strategy at authorization; inspect and stop on concurrent drift before assigning writers.

## 3. Product and learning contract

The player reads a process objective, edits ladder, runs live exploration, observes commands and feedback, then tests a frozen copy in fresh isolated trials. Multiple eligible ladder structures can pass. Grading never compares against a reference hash or rung arrangement and never operates outputs to manufacture success.

The initial authored learning packet is version 1 and includes:

- Challenge ID/revision, `scene-1-conveyor-stop`, protocol `conveyor-v1`, fixed 20 ms scan period and seven case kinds.
- Objective and plain-language disclosure of idle, hold after Start release, photoeye stop, Process Stop, simultaneous Stop priority, blocked Start and Reset/repeat behavior.
- Unsolved `.rpproj.json` starter, fixed trusted resource reference, actual byte length and SHA-256; correct symbolic tags/bindings and initially false values, without complete solution rungs. An unfinished starter may be compiler-invalid and must remain saveable/editable.
- Glossary entries identifying binding, learner-facing name, ownership, input/command/feedback meaning and modeled limitations.
- Eligibility policy `conveyor-bool-v1`, concise palette explanation and imported-program diagnostics.
- Sequential hints about observed release behavior, remembering a request and inhibit conditions. An optional worked explanation is illustrative and never the grading oracle.

Demo 3's complete authored solution stays on its demonstration path. Challenge entry and Revert Starter route through existing draft Save/Discard/Cancel guards. Revert is distinct from Reset. Reset resets runtime/plant/forces, keeps the program, and must not silently overwrite the learner document.

Use unambiguous labels: **Run Simulation**, **Stop Simulation**, **Reset Attempt**, **Process Start**, **Process Stop**, **Test**, **Cancel Test**. Process Stop is an ordinary simulated input, not a certified protective function. Vendor-style editor views do not establish vendor compatibility, physical safety or commissioning readiness.

## 4. Execution and observation architecture

Current `VirtualControllerSession.Advance` samples, executes, commits and advances the plant, but also advances its loop when a stopped runtime returns an unchanged scan number. `SnapshotPublished` additionally occurs on lifecycle/force changes, and lifecycle projection can follow publication. It is not an accepted-scan event. `SceneSimulationRuntime.Points` aliases mutable state through a read-only interface.

Add a dedicated renderer-neutral event and preserve existing publication behavior:

```csharp
public sealed record VirtualControllerSignalImage(
    IReadOnlyDictionary<string, bool> Bools,
    IReadOnlyDictionary<string, double> Numbers,
    IReadOnlyDictionary<string, string> Text);
public sealed record VirtualControllerScanCompleted(
    long ScanNumber, TimeSpan SimulatedTime,
    VirtualControllerSignalImage SampledInputs,
    VirtualControllerSignalImage EffectiveInputs,
    VirtualControllerSignalImage Outputs,
    IReadOnlyList<string> Diagnostics);
// New member of existing VirtualControllerSession:
public event Action<VirtualControllerScanCompleted>? ScanCompleted;
```

Sampled inputs are the actual tag-keyed scan arguments after session operator overlays and before forces; effective inputs are Input-role primitives used after force application. Outputs are tag-keyed post-execution primitives. Copy every map/list into private storage and expose read-only wrappers; no full snapshot, nested aggregate or mutable original is handed to consumers. Emit only when the controller scan number increments by one, after commits, plant step and pulse cleanup. Lifecycle, force and stopped advances emit none. The runner explicitly starts the runtime and never advances a stopped trial.

Use existing `SceneIoImageMapper.SampleBoolInputs` and `CommitBoolOutputs` for scene/tag translation. Resolve operator pulses through the variable whose binding equals `operator.start` or `operator.stop`. The evaluator sees normalized binding-keyed inputs (`operator.start`, `operator.stop`, `simulated_photoeye`) and output (`conveyor_running`), not guessed tag names. The pure adapter supplies photoeye feedback and owns actual modeled travel. Copied plant state includes nullable-position presence, leading edge, component state and completion count; position zero after discharge cannot mean success.

Test uses a pure `Task.Run` worker with frozen objects and a fresh controller/session/model per independent case. CV07 intentionally resets within its case. No worker calls Godot, reads source files, loads render assets, accepts UI forces/toggles or touches exploration. Live scene labels use a separate copied interactive observation on the main thread. Historical trace selection never moves the carton or recolors live ladder as if it were replay.

Validate the original `BuildProgram()` object with compiler, scene binding validator and challenge eligibility **before** `LadderProgramJson.Save/Load` cloning, then revalidate the worker-owned clone. The serializer omits block type/interface data; validating after serialization alone could erase unsupported original content. Mutable record/list graphs are not frozen merely by copying a record reference. Content lists are likewise bounded, strictly parsed and privately copied.

## 5. Eligibility and resource policy

Challenge-only eligibility, with no changes to sandbox semantics:

- Schema 1, language `LD`, scan period exactly 20 ms.
- Legacy flat networks without blocks/tasks; or exactly one Function block with no interface parameters and matching entry ID, with no tasks or exactly one Continuous task at 20 ms, priority 10, targeting that block. Root networks must match the sole block's networks; reject hidden shadow instructions. Count effective networks once.
- All variables BOOL, literal initial false. Exactly one input per required binding `operator.start`, `operator.stop`, `simulated_photoeye`; exactly one output bound to `conveyor_running`; memory binding empty. Learner variable names may differ. Missing/duplicate/reversed/type-invalid/writable-feedback bindings are rejected by existing validation plus challenge policy.
- Ordinary NO/NC contacts with no edge mode, Series/Parallel logic, ordinary Assign coils targeting memory/output. No compare, edge, timer, counter, numeric, call, return, jump, label, Set/Reset coil, extra task/block, interface or unsupported unused field.
- At most 32 variables, 64 effective networks, 256 effective logic nodes and branch depth 16. Eligibility diagnostics identify unsupported location; they are InvalidProgram, not behavioral Failed.

Content bounds: 256 KiB JSON, depth 32, exact schema/protocol and seven unique required case kinds, identifiers at most 64 characters, normal text 512, hint text 2048, at most eight hints. Reject unknown properties, duplicate JSON keys, missing/eighth/duplicate cases, nonfinite numbers and invalid fixed constants before allocating large graphs. Fixed case enums select implemented protocols; no general stimulus/predicate/scripting system is promised.

Every case permits exactly a maximum of 250 accepted scans; run maximum 1750. Trace rows at most 1750, metadata events at most 32, combined retained records at most 2048. No truncation or overflow-to-pass. Row maps are bounded primitives: at most 32 BOOL inputs/outputs, 16 plant numeric/text entries, strings 256. Reset checkpoints are metadata, not accepted scans.

Progress input at most 1 MiB/1000 records, depth 32, strict versioned schema. IDs are data, never filesystem paths. Unknown future versions/corruption preserve original files and produce a readable diagnostic. These are proposed support limits, not measured real-time guarantees.

## 6. Authoritative conveyor cases

Constants are **symbolic plant model coordinates**, not a 1:1 measurement promise for rendered installation geometry. The model has length 1, speed 0.5 model m/s, carton length 0.2, photoeye 0.5 and minimum sensor hold 0.1 s; rendered conveyor installation uses a different authored scale/projection. Preserve that mapping and qualify it separately.

Period = 20 ms; commanded advance = 0.01 model m per scan; epsilon = 1e-9 model m. Do not round before assertions. Sensor crossing comes from actual feedback, not position reconstructed with epsilon. The first blocked step may be scan 50 or 51; the following sampled-sensor rule is exact.

| Case | Stimulus and required result |
| --- | --- |
| CV01 ColdStart | Reset/Run; 25 accepted scans, no Start. Output false, present carton at `abs(edge) <= epsilon`, completion 0. |
| CV02 Holding | Start pulse scan 1, release thereafter; through scan 10, output true on every scan, `abs(edge - 0.1) <= epsilon`, present, completion 0. |
| CV03 BeamStop | Start scan 1; witness actual travel and first post-step photoeye transition at n <= 75. At n+1 effective sampled photoeye true and applied command false. Stop edge satisfies `0.5 - epsilon <= edge <= 0.51 + epsilon`; present, completion 0. For 25 further scans, command false, position changes by at most epsilon, present/completion unchanged. |
| CV04 ProcessStop | Start scan 1; Process Stop pulse scan 11. Command false scan 11 and next 25; `abs(edge - 0.1) <= epsilon`, present/completion 0. Start is false. Never substitute runtime Stop. |
| CV05 SimultaneousStartStop | Both pulses scan 1, then 25 accepted scans. No command or movement; Stop dominates. |
| CV06 BlockedStart | Complete CV03 including hold; pulse Start next scan while beam sampled true; 25 scans including pulse scan show no command/movement, present/completion 0. |
| CV07 ResetRepeat | Complete ordinary run/hold; reset controller/session/plant in same case. Check scan/time zero, false output, loaded origin, completion 0, cleared pulses/forces/backlog. Run/new Start; repeat CV03. `Cycle` increments at reset; monotonic `CaseSequence` counts accepted scans across both cycles under the same 250 budget. |

All seven must pass. Required Start/Stop inputs are one-accepted-scan pulses; no held-Start rearming guarantee is inferred. CV08 is a separate ungraded lifecycle acceptance: Stop Simulation cancels test and normally stops exploration; Cancel Test alone leaves exploration unchanged; Reset preserves draft and follows cleanup policy. A runtime Stop cannot earn CV04/05 success.

Runner continues independent fresh cases after a behavioral failure, allowing a useful case ledger. Invalid content/program, cancellation and infrastructure faults stop execution; omitted cases display Not Run. Missing accepted-scan event or runtime semantic diagnostics fails immediately. Outcomes are Passed, Failed, InvalidProgram, InvalidChallenge, Cancelled and InfrastructureError. Only a current Passed result covering all seven can update progress.

Independent fixture suite includes two valid structures: output-contact seal-in and internal BOOL seal-in with separate output Assign coil; renamed-tag variants and a normal editor-created default block/task. Behavioral negatives include always-off, unconditional output, missing holding, missing Stop, missing sensor, simultaneous priority error, occupied-beam bypass, restart and one-cycle stale state. Eligible restart negatives must use eligible instructions; a prohibited timer fixture is InvalidProgram, not behavioral evidence. Include discharge-to-zero false-pass, force/manual isolation, invalid bindings, hidden instructions, malformed/limit data and delayed cancellation races.

Native parity runs both valid structures, renamed bindings and at least the six principal behavioral negatives with the same protocols. Compare normalized observations, outcomes and first-failure case/scan/phase through actual scene output projection and generated feedback. Pure model agreement cannot prove Godot mapper wiring or visible usability.

## 7. Shared new contracts and ownership

All types below are proposals in `RungProof.Next.Challenges`, not claims of existing APIs. `ChallengePlantState` has copied Feedback/Numbers/Text maps, ObjectPresent and Completed. `ChallengeObservation` contains identity, case ID, Cycle, monotonic CaseSequence, controller ScanNumber/time, binding-keyed effective inputs/applied outputs, copied plant-after state and diagnostics. `ChallengeTraceEvent` records CaseStarted/ResetCheckpoint/CaseFinished, case/cycle/sequence/scan/time/state/detail.

`ChallengeDefinition` includes SchemaVersion, ID/revision, SceneId, ProtocolRevision, ScanPeriodMs, title/objective/hints, five typed plant parameters, seven typed cases, glossary, eligibility policy and starter resource identity. `ChallengeCaseResult` includes case ID, typed outcome, stable code, expected/observed text and nullable first-failure observation. `ChallengeRunResult` includes identity, outcome, executed cases, trace, events, code and diagnostic. Null first failure is valid for pre-scan errors; do not fabricate scan 0.

Exact service boundaries:

```csharp
ChallengeDefinitionLoadResult ChallengeDefinitionJson.Load(string json);
IReadOnlyList<LadderValidationIssue> ChallengeEligibilityValidator.Validate(LadderProgram program);
string ChallengeProgramIdentity.ProgramSha256(LadderProgram eligibleProgram);
ChallengeRunResult ChallengeTestRunner.Run(ChallengeDefinition definition,
    LadderProgram program, ChallengeRunIdentity identity,
    Func<IChallengePlantAdapter> createPlant, CancellationToken cancellationToken);
ChallengeProgressLoadResult ChallengeProgressRepository.Load(string absolutePath);
ChallengeProgressWriteResult ChallengeProgressRepository.Save(string absolutePath,
    IReadOnlyList<ChallengeProgressRecord> records);
```

Load results carry nullable validated data plus bounded issues; write results carry success and diagnostic; progress records contain content identity, completion timestamp and hint usage, never trusted anti-cheat achievements. `IChallengePlantAdapter` has Reset; typed sample/commit methods; Advance(TimeSpan); CaptureState. `ConveyorChallengePlantAdapter` takes immutable validated plant parameters and no Godot types. `ChallengeEvaluator` owns case predicates/first failure; it has no command/plant setters.

Presentation state has UI state Idle/Validating/Running/Cancelling/Finished, active identity, last result, current-proof flag, CanTest, outstanding-worker flag, mutation lock, pending action, status and separate persistence status NotAttempted/Saved/NotSaved. Coordinator owns transitions and awards; presenter owns rendering.

Presenter intents: Test(challenge ID), Cancel(epoch), StopSimulation, ResetAttempt, Navigate(destination), Close, Hint(ID), RevertStarter(challenge ID). Coordinator notifications: State, per-case Progress, Result and CleanupDelay. All main-thread callbacks verify coordinator lifetime/epoch. Existing live Process Start/Stop delegation remains separate. DraftChanged invalidates current proof immediately. SaveDraft success/failure describes document persistence and never awards completion.

## 8. Identity, packaging and local progress

Identity format `RP-CANON-1` is conservative executable/content identity, not semantic equivalence. Use validated worker-owned `LadderProgramJson.Save` output, omit only root id/name/watchVariables, ordinal-sort object keys recursively, preserve arrays, decode/reencode strings with System.Text.Json default encoder, no Unicode normalization/whitespace/BOM. Reject duplicate keys. Finite numeric values within binary64 exact-integer envelope normalize negative zero to 0, otherwise invariant round-trip representation with lowercase exponent and no exponent plus/leading zeroes. UTF-8 SHA-256 is lower-case 64 hex. Preserve all node/network IDs/labels, ordering, variables/bindings/initials, tasks/blocks/period.

Identity includes challenge/revision, program hash, complete learning packet hash including actual starter bytes/length, plant hash, engine revision/hash, evaluator revision/hash, format and period. Coordinator epoch is local cancellation identity, not persisted content proof. Engine manifest lists full `src/VirtualController/*.cs`, pure ConveyorPlantModel and all `src/Challenges/*.cs`. Evaluator manifest lists runner/evaluator/protocol files. Paths sorted ordinal; source text normalized UTF-8 LF; record exact input list, generator/schema/canonicalizer identity. Exclude manifest output from its own inputs. Full challenge packet/starter and generator are included in build fingerprints. Final native DLL/package hash is independent evidence; source hashes do not prove correctness or rights.

New `tools/build_challenge_semantic_manifest.ps1` generates trusted bundled `challenges/semantic-manifest.json` before build/freeze. Build/fixture/export lanes compare it against reviewed source inputs, not merely against itself. Missing/mismatched development manifest disables Test with configuration guidance; exported candidate fails its gate rather than shipping a broken challenge. Runtime reads only fixed trusted bundled resources, never source files or commands. Pure fixture manifests use the same producer in isolated output.

Progress is separate `user://challenge-progress.json`, with existing build-specific user-data separation. Serialize writes, same-directory temporary replacement, retained last-good backup, preserve corrupt originals without deletion, readable future-schema refusal. Fault injection proves prior valid data survives temp/replace errors. Passed with failed progress write remains Passed plus **Progress not saved**. Any draft mutation immediately invalidates displayed current proof; no automatic undo resurrection in MVP. Historical completion stays historical after content/engine/program changes. Test itself preserves history/path/unsaved baseline; explicit Save Draft performs normal intentional saved-baseline updates.

## 9. Cancellation and document lifecycle

Exactly one outstanding worker. Retire award epoch before token cancellation. Check token before each accepted scan and case/reset boundary. Canceled/computed-but-undelivered work cannot award/save, and no replacement worker starts until the old task actually ends. Mutation stays locked while worker outstanding; reading, scrolling, selection, copying and Save Draft remain available. Retain token/continuation ownership until cleanup.

250 ms is a measured reference-machine responsiveness target, not a hard real-time guarantee. Navigation, close and Reset asynchronously wait at most 2 seconds for cleanup; no UI-thread Wait/Result blocking. On timeout discard the queued action, stay in the current workspace, show cancellation pending, retain access/save and Test disabled, permit retry. Eventual completion does not replay old navigation/close/reset. Stop Simulation can perform ordinary live Stop immediately. Successful timely cleanup permits the requested action through existing draft guards. No forced process kill, hidden discard or second worker.

Delayed-worker fixtures cancel after computation/before main-thread delivery and exercise canceled navigation/close/reset plus retry. They must prove no stale award, mutation leakage, unbounded worker count or late auto-navigation. Disposal detaches safely or guards retired continuations from touching freed UI.

## 10. UX, art and accessibility qualification

Add a focused presentation around the existing editor, not a replacement. Objective, relevant operator controls, ladder and process are accessible in one workspace; compact editor-only/scene-only presets preserve objective/results and state. Hints/results wrap or scroll. Do not shrink text to hide layout failure. Respect focused/visible edit shortcut gates, unsaved guards, dialog Cancel focus and normal save/open behavior.

Target minimum **1280x720 actual logical content** at OS scaling 100/125/150/200%; additional baseline 1920x1080 logical at 100%. Record physical resolution, OS scale, renderer scale, actual content bounds and scene aperture. The display must accommodate scaled content plus chrome; 200% minimum requires at least 2560x1440 plus chrome. OS scaling alone does not prove Godot logical behavior. UI ordinary text at least 16 logical units, custom ladder labels/annotations at least 14 at declared zoom, with synchronized draw/hit geometry. If infeasible, stop and present a revised support scope; do not claim the target passed.

Actual keyboard workflow covers entry, rung/contact selection, insertion, branches, tag properties, deletion/move, undo/redo, save dialogs, Process Start/Stop, Test/Cancel, trace inspection and navigation. Divider keyboard control or explicit layout presets is required. Focus remains visible and stable during monitoring; hidden/unfocused editors and dialogs do not consume edits. Button-signal emissions and headless state checks are bounded evidence, not keyboard-route proof.

Signals/results need text and shape, not color/sound alone. Green means the explicitly named Boolean state, not safety or task success. Retain perspective, current meshes, plain floor/background and canonical brand; no repeating grid/stripe/scan-line background. Challenge-specific framing preserves existing demo/sandbox defaults and shows carton trajectory, sensor and operator controls. Distinguish model coordinates from rendered scale, live labels from historical trace, sensor held feedback from apparent geometry. No cosmetic motion drives simulation; no replay or completion animation in MVP.

Native captures cover idle, released Start travel, beam crossing, next sampled inhibition, Process Stop, simultaneous input, blocked restart and Reset, with long tags, hints/results open and compact aperture. Screenshots prove sampled visibility/readability; controller observations prove modeled behavior. Neither establishes continuous clearance, physical safety or live PLC acceptance.

Keyboard/native visibility and screen-reader semantics are separate gates. Probe the pinned engine/Windows stack after authorization. If full semantic support is infeasible, explicitly propose reduced support to the human or separately estimate remediation; no support/certification claim from a frame-only tree or custom drawing. Full screen-reader remediation is outside the initial allowance unless explicitly selected.

Art ledger has three separate statuses: historical visual asset approval, new assembled challenge native qualification, and redistribution rights. Candidate carton/older contradictory reviews and `productionApproved:false` are explicit future evidence items. Map shipped asset/font/SVG/runtime hashes to source, creator/reference, rights/notices and review. Unresolved rights block relevant external distribution, not internal source planning.

## 11. Comprehensive studio roadmap and production

M0: authorization, recoverable WIP, current baseline, exact contracts and assigned writers. M1: pure executable harness and independently discriminating fixtures. M2: one complete native playable conveyor experience, persistence and qualifying render/keyboard/DPI. M3: exact internal candidate, fresh export, clean target/recovery/support/rights disposition. M4: separately authorized observed learner pilot and one bounded correction cycle. M5: separately authorized/reestimated three-total-challenge tranche (primer, conveyor, delayed lamp), then counter/batch and sequence/palletizer tranches with their own recovery/evaluation contracts. M6: separately authorized public release after all external gates.

Content pipeline: intake/outcome -> source/controls contract -> versioned brief/starter/glossary/hints -> independently challenged valid/negative fixtures -> model/native parity -> visual/accessibility -> editorial/claims -> exact artifact -> pilot/revision -> release register. Generated help is a projection, never an unreviewed instructional authority. No arbitrary conversion of 77 scenes or 500-family catalog aspirations into production obligations.

Critical path: scope -> preserved baseline -> contract/observation/fixture -> runner -> Main/coordinator -> focused shell/art -> native qualification -> exact package/environment/rights readiness -> approved pilot -> corrections. Source authorship and native screen slots are serialized; content/rights investigation can proceed in parallel. More agents do not eliminate these dependencies.

One writer per family: runtime engineer for Session/scene capture; challenge engineer for pure modules/progress; app integration engineer for Main/partials/coordinator; UI engineer for Shell/presenter; editor engineer for canvas, coordinated with UI; technical artist for additive presentation assets/metadata, requesting Main changes through owner; learning author for packet; QA for independent fixtures; release engineer for generator/build/export/config/notices. Dot arbitrates explicit handovers. No parallel Main/Shell/composer changes.

Historical human-labor context only, with reviews counted once: baseline/contracts 3–5 person-days; engine/progress 8–13; app/UI/editor 7–12; one learning packet 3–5; reuse art 4–7; independent QA 7–11; internal package/environment/support 4–7; six-session pilot/one revision 4–7. Total **40–67 HUMAN-LABOR person-days**, uncertain and reassessed after M0. This does not estimate agent elapsed processing time, which is unknown; no arbitrary speedup conversion, completion date or agent cost promise is supported. Excludes full assistive remediation, new meshes, audio, cloud, hardware/legal/signing acquisition, storefront/campaign/live scope, external waits and additional redesign cycles.

Operational execution uses four concurrent agent slots in waves, with independent reviews sharing that capacity. Content and rights work can overlap engineering; runtime contracts precede runner implementation and app integration. Each shared file family has one writer and native screen sessions are serialized. Matt's first-tranche authorization remains pending; native screen access, changed support scope, participant activity, campaign expansion and public release each retain their stated gates.

## 12. Pilot and external gates

Propose six sessions up to 45 minutes: four target controls learners and two experienced reviewers unfamiliar with RungProof. Built-in hints allowed; facilitator coaching only logged recovery. Gate: at least 3/4 learners complete within 30 minutes uncoached, at least 3/4 explain command/feedback/Stop causally, both reviewers identify no unresolved semantic contradiction, and zero unresolved critical false-pass/data-loss/offline-boundary defects. Use identical published-case questions; remove Start-duration variation outside the one-scan contract. Record individual build/content identities, prior experience, timing, hints, intervention and barriers. A tiny formative sample supports revision, not statistical learning/market claims. External recruitment/contact/participant distribution needs separate authorization and appropriate rights/privacy readiness.

Open owned release gates: clean standard-account Windows/no developer runtimes, minimum Forward+ hardware/VM/RDP scope, corrupt/interrupted-save recovery, full supported native DPI/keyboard/assistive scope, actual shipped rights/notices/name/IP, formal security/dependency/secret/SBOM review, signing/final signed artifact, support/privacy, distribution/update and rollback. Release engineer/QA own environment/artifact evidence; rights coordinator collects evidence for human rights authority; security reviewer owns commissioned review; support/content owner owns support claims; human controls pilot/release authority. Development/internal review, external delivery and real PLC commissioning remain separate decisions.

## 13. Traceability and final authority

PGL-001–014 map to sections 3/6/10/12; UX-001–012 to 7/9/10; ART-001–010 to 4/10/12; ARCH-01–10 to 4–9; QA-01–10 to 5–12; PROD-001–010 to 1/2/11/12. Task-level tests and owners are in the companion implementation plan, with actual meeting issue dispositions in `10-meeting-minutes.md` and risks/decisions in `11-decision-risk-register.md`.

Dot reviews coherent coverage, exact contracts, alternative resolution, independent negative evidence, feasible sequencing, WIP preservation and honest support/delivery limits. A plan PASS can coexist with explicit human scope decisions and future verification gates. No implementation begins until the human approves the concrete tranche and assumptions. No public release, external hiring/contact or automation is authorized by this planning package.

## 14. Concurrent baseline drift observed during planning

Root's final read-only observation found four inventoried files changed by concurrent work outside this studio: release review, supported configuration, `src/VirtualController/LadderCompiler.cs` and `tools/build_offline_demo.ps1`. Six new files outside this planning directory include rights/dependency/qualification/operations records and package checker/test scripts. `baseline-delta-observed.json` preserves that bounded observation; the original inventory remains unchanged. This is not evidence of studio source modifications or a frozen final checkout.

Root inspected the compiler delta as iterative CALL-graph traversal replacing recursion; the slice excludes CALL instructions and still requires current compiler qualification. New records report another candidate and prior test evidence, not studio reruns or whole-app security clearance. Newly appearing `verify_demo_package.ps1` and its tests must be inspected/reused at T0/T9 rather than duplicated blindly. Their final behavior and artifact hashes can change again.

Implementation authorization therefore permits a **reconciliation-first gate**, not edits against assumed original hashes. Read the final deltas, agree exclusive ownership with concurrent work, obtain a recoverable accepted snapshot, rebuild semantic manifests from that accepted state, and revalidate exact commands/interfaces before feature integration. Unreconciled source/package drift stops source edits. Plan quality approval does not claim that the source is unchanged or implementation-ready today.
