# Studio meeting response — architecture

Date: 2026-10-10. Read-only meeting review of 07 and cross-department requirements from 01, 02, 03, 05 and 06. Only this new response is written. No implementation/build/test/launch occurred. Disposition: CONDITIONAL APPROVAL of direction, RETURN the interface specification for the exact amendments below before Dot's final plan approval. This is a real critique, not unconditional consensus.

## Ratified direction and supersession

Accept D01 isolated pure trial and no replay for the first slice. It supersedes the visible-reset-trial alternative in 04. Trial leaves live plant, controller, document, history, saved path and unsaved baseline untouched. Scene labels are live exploration, result rows are historical isolated trials. Application Stop remains an explicit exception: cancels trial and performs normal live Stop. Cancel alone changes no live state. Reset cancels test before normal live reset; neither Reset nor Cancel deletes the draft. Accept D03/D05/D06/D08/D09/D11/D12 with their stated native/rights/human evidence gates. D02/D04/D07/D10 require amendments.

## A01: preserve the ordinary editor's default block/task

Owner: architecture eligibility + editor; blocker severity: high.

Source defect in vague D04: LadderEditorDocument.AddBlock creates an explicit Continuous task; BuildProgram emits Blocks and Tasks. Rejecting all declared tasks or blocks rejects a normal newly authored document. Exactly allow:

- SchemaVersion 1, Language `LD`, ScanPeriod.Ticks == TimeSpan.FromMilliseconds(20).Ticks.
- Legacy representation with no Blocks and no Tasks, one effective Main network collection; or exactly one Block whose ID equals EntryBlock and whose BlockType is Function, with no interface parameters, plus absent Tasks or exactly one task with Kind `LadderTaskKind.Continuous`, Period 20 ms, Priority 10 and EntryBlock equal to the sole block.
- If Blocks exist, root Networks must match the sole block's Networks in semantic content and IDs; reject mismatched shadow networks before normalization. Count each effective network once, not the serializer's duplicate root+block representation. Inspect both representations to reject hidden unsupported fields; no additional disconnected blocks.
- Every tag is BOOL and has literal bool InitialValue. Input bindings are precisely `operator.start`, `operator.stop`, `simulated_photoeye`; all three must exist. Output binding precisely `conveyor_running` and exactly one output exists. Memory binding empty; no unbound Inputs/Outputs. Exactly one tag per required input binding and one output binding, arbitrary unique learner tag names. All initial values false, including inputs; no manual-start shortcut embedded in initial data.
- NO/NC Contacts with EdgeMode.None, Series and Parallel only; ordinary Assign coil targets Memory or Output. Reject every timer/counter/reset/load/numeric/call/return/jump/label instruction field, compare node, edge contact, Set/Reset coil, additional task, additional block, interface parameter and unsupported type even when unused. At most 32 variables, 64 effective networks, 256 total effective logic nodes, branch depth 16. Empty/invalid rungs fail existing compiler. Coil count at most one per network is already represented by current IR; eligibility still validates presence and all optional instruction fields explicitly.

Invoke LadderCompiler.Compile and SceneIoBindingValidator.Validate against the current Scene 1 declared points and special operator bindings; then challenge eligibility. No pressure to weaken existing validators or change sandbox. Test ordinary editor BuildProgram output as a positive fixture. Negative duplicates, reversed roles, writable photoeye, extra input, initial output true, hidden unused timer and task priority changes must fail before a worker starts.

## A02: freeze without losing unsupported fields

Owner: coordinator + identity/eligibility; blocker severity: high.

LadderProgram is a record containing IReadOnlyList fields, not intrinsically an immutable graph. Main-thread BuildProgram -> validate compiler/bindings/eligibility on the complete original object -> LadderProgramJson.Save -> LadderProgramJson.Load -> revalidate -> worker-owned loaded copy. Never pass editor-owned lists to Task.Run. Source inspection shows LadderProgramJson.Save currently omits block BlockType/interface; this is safe only after the complete original object has passed A01's Function/no-interface restriction. This is not general-purpose project cloning. Retain invalid draft untouched; invalid programs never enter runner.

Freeze ChallengeDefinition by bounded strict parse into worker-owned immutable lists too. Read/write collections exposed to presenter must be immutable wrappers over private copies; IReadOnlyDictionary alone is insufficient. Start the worker only after capture/validation/hash all succeed. A synthetic validator/loader error maps to InvalidProgram with no RunIdentity invented from unrelated code.

## A03: exact completed-scan event and binding names

Owner: runtime writer + challenge adapter; blocker severity: high.

Prefer one dedicated event. Do not change SnapshotPublished lifecycle semantics. NEW renderer-neutral proposed types:

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
// NEW member on existing VirtualControllerSession:
public event Action<VirtualControllerScanCompleted>? ScanCompleted;
```

SampledInputs is the actual scan argument after session operator overlay, before controller force override. EffectiveInputs is the declared Input-role leaf values actually used by execution after forces. Both are tag-name keyed. Outputs is post-scan controller tag-name keyed. Copy raw samples before calling Scan; build effective-input image through a NEW internal runtime helper that filters validated input declarations (including expanded aggregate leaves) from the snapshot. Signal maps contain only primitives and diagnostics strings; event intentionally carries no full VirtualControllerSnapshot or untyped aggregate objects, avoiding hidden nested mutability. All copies wrapped with ReadOnlyDictionary and Array.AsReadOnly; no mutable originals retained. Finite numbers validated at source boundary.

Capture beforeScanNumber before Scan. Emit only if resulting ScanNumber == before+1, after all output commits and plant advance and pulse cleanup. Never emit for stopped Advance, lifecycle, force changes or missing scan. Current Advance does call plant advance while stopped even though ScanNumber does not increment; preserve existing behavior, but runner must Run before stepping and never advance stopped state. This distinction requires a meaningful regression.

ChallengeObservation.SampledInputs is normalized binding-keyed BOOL actual/effective input image for evaluation: `operator.start`, `operator.stop`, `simulated_photoeye`. AppliedOutputs normalized to `conveyor_running`. Runner maps validated named tags to bindings explicitly; original event has tag names, do not compare it with scene keys directly. Use existing SceneIoImageMapper.SampleBoolInputs for plant-to-tags and CommitBoolOutputs for tags-to-plant. Pulses resolve the variable whose Binding matches operator name, then Session.PulseInput(variable.Name). The pure plant only produces simulated_photoeye; operator inputs are controlled by protocol. No UI force or scene point toggle is forwarded.

Normalize ChallengePlantState keys: Feedback["simulated_photoeye"], Numbers["object_position"] = actual leading edge when present, Text["component_state"], explicit ObjectPresent, explicit Completed. When absent, Numbers may hold 0 only for consistency with native projection; ObjectPresent false and Completed >0 prohibit pass. Do not derive presence from position. Native verifier captures SceneSimulationRuntime after the same completed boundary on Godot thread and must expose actual model presence with a narrow copied read-only method. Art labels consume a distinct live observation event, never trial events. No worker Godot calls or model transforms fabricated for grading.

## A04: fixed case schema, reset metadata and terminal errors

Owner: challenge content + QA + UI; blocker severity: medium.

Keep exact seven case enum members from 07. Remove the meaningless 32-stimuli bound and any generalized predicate/stimulus list from the slice schema: protocol is fixed code selected by enum. Definition schema additions NEW: `int SchemaVersion` (must equal 1), `string ProtocolRevision` (must equal `conveyor-v1`), `int ScanPeriodMs` (must equal 20). Challenge IDs unique; definition must contain precisely one each ColdStart, Holding, BeamStop, ProcessStop, SimultaneousStartStop, BlockedStart, ResetRepeat and no duplicates. MaximumScans may be present but fixed <=250; reject a value too small for its protocol rather than silently skip required hold. Exactly seven cases in this definition, no generic eighth unsupported case. Missing/unknown properties reject with a readable InvalidChallenge, not permissive defaults. Apply byte/depth/width limits before large graph allocation.

CV07 requires independent observation identity after reset. Define existing proposed `Cycle` as reset generation within a case, zero on case start and one after its intentional reset. NEW add `long CaseSequence` to ChallengeObservation: monotonic accepted-scan ordinal across both cycles, not reset with controller ScanNumber. MaximumScans counts CaseSequence (cumulative across cycles), not resettable controller count. Case budget 250; run budget 1750 counts accepted scan observations. CV07 reset checkpoint is not a completed-scan observation.

NEW metadata/result amendments:

```csharp
public enum ChallengeTraceEventKind { CaseStarted, ResetCheckpoint, CaseFinished }
public sealed record ChallengeTraceEvent(string CaseId, int Cycle,
    long CaseSequence, ChallengeTraceEventKind Kind, long ScanNumber,
    TimeSpan SimulatedTime, ChallengePlantState Plant, string Detail);
public sealed record ChallengeRunResult(ChallengeRunIdentity Identity,
    ChallengeOutcome Outcome, IReadOnlyList<ChallengeCaseResult> Cases,
    IReadOnlyList<ChallengeObservation> Trace,
    IReadOnlyList<ChallengeTraceEvent> Events, string Code, string Diagnostic);
```

Trace rows <=1750; metadata events <=32; combined retained records <=2048. No silent truncate. In any valid definition seven cases required. Use `CH_CANCELLED`, `CH_CASE_TIMEOUT`, `CH_RUNTIME_DIAGNOSTIC`, `CH_INVALID_PROGRAM`, `CH_INVALID_CHALLENGE`, `CH_ADAPTER_ERROR`, `CH_TRACE_LIMIT` as stable run-level codes; case-specific expected/observed text is plain text bounded 512 chars. Infrastructure error includes adapter exceptions/trace bound; runtime semantic diagnostics are Failed. FirstFailure may be null for pre-scan errors; presenter uses result Code/Diagnostic, not a null scan. Stop at first failed case is acceptable only if omitted cases are clearly NotRun; simplest MVP: continue fresh independent cases after Failed, stop on Cancelled/InfrastructureError and retain only executed cases; UI displays remaining cases as not run. Passed only if all seven complete Passed.

ChallengeUiState in 07 is workable as Idle/Validating/Running/Cancelling/Finished plus separate terminal outcome. Do not encode Passed both in state and outcome. Exact presenter payload NEW `ChallengePresentationState(ChallengeUiState State, ChallengeRunIdentity? ActiveRun, ChallengeRunResult? LastResult, bool IsCurrentProof, bool CanTest, bool IsWorkerOutstanding, string StatusText)`; engineering owns transitions, UX owns rendering. Progress save status is separate from grading Outcome (Saved/NotSaved/NotAttempted); cannot turn a semantic pass into failure simply because disk is full. NEW `ChallengePersistenceStatus` enum and add it to presenter payload, not worker result.

## A05: numeric acceptance

Owner: controls + architecture + independent QA; disposition: accept ledger with exact algorithm.

Fixed scene constants validated exact configured values, not renderer dimensions:1,.5,.2,.5,.1. Delta = speed*(20/1000d) = .01. eps=1e-9 m. For every positional assertion use finite-value check then abs(actual-expected)<=eps; sensor-range inclusive `position>=.5-eps && position<=.5+delta+eps`. No rounding before assertions. Sensor transition determined from actual model bool, not reconstructed from epsilon position. n must be witnessed <=75; n+1 uses actual effective sampled beam. Require exact off on n+1 plus present, completed0, and 25 later accepted scans unchanged relative to n+1 stop position. Either floating-point reach at scan50 or51 passes if all state/command conditions hold; do not mandate a single scan number. CV02/CV04 require positions .1±eps after10commanded scans. Time is ScanNumber*20ms exact integer ticks; no wall clock. Native compares model and rendered mappings separately; broad tolerance is not permission for an extra commanded scan after feedback.

CV06 Start must be injected after CV03's full hold, with beam still sampled true; require25following accepted scans including stimulated scan. CV07 runs both ordinary+hold phases under one250 budget, verifies reset checkpoint before Run, then next ordinary run; reset clears forces/backlog/operator pulse memory. A positive reset checkpoint at scan0 is not a graded accepted scan.

## A06: exact identity and hashes

Owner: identity/runtime/release + QA; blocker severity: high.

Canonical format NEW `RP-CANON-1`. Use already validated eligible LadderProgramJson.Save worker copy; omit only root properties `id`, `name`, `watchVariables`. Object properties ordinal-sorted recursively; array order retained; strings decoded then encoded with System.Text.Json default JavaScriptEncoder.Default (no Unicode normalization), JSON literals lowercase, no whitespace/BOM. Numeric values finite and |value|<=2^53-1 for this narrow schema; normalize negative zero to `0`, otherwise binary64 ToString("R",InvariantCulture), written as JSON numeric token with lowercase exponent `e` and remove exponent `+`/leading zeroes. Thus20/20.0 normalize20. All eligible program numeric fields are bounded period/priority/revision; actual numeric instruction values are not supported. Serialize UTF8 and hash SHA256; output64lowercase hex. Pin algorithm tag into identity so a future writer change cannot silently reuse old proofs. Reject duplicate JSON keys before canonicalization. No claim of RFC canonical JSON compatibility.

Include `string IdentityFormat` == RP-CANON-1 and `string EngineRevision`, `string EngineSha256` in ChallengeRunIdentity, separately from EvaluatorRevision/EvaluatorSha256. Engine semantic manifest canonical hash explicitly contains source byte SHA256 entries (LF-normalized UTF8 text) for VirtualControllerRuntime.cs, VirtualControllerSession.cs, LadderCompiler.cs, LadderProgram.cs, SceneIoImageMapper.cs, ConveyorPlantModel.cs, ConveyorChallengePlantAdapter.cs, eligibility and digest implementation; evaluator manifest covers runner/evaluator/protocol implementation. Sort manifest relative paths ordinal; packaged manifest revision/content hashes verified against exact build evidence. Compiler/runtime changes invalidate old current proof even if evaluator source unchanged. Release owner owns manifest generation and truthfulness. Native main assembly/DLL hash is EvidenceArtifactSha256 recorded separately; do not use it as worker evaluator/engine hash because test and Godot assemblies differ. A source digest establishes identity only, not semantics/rights/quality proof.

ChallengeSha256 hashes full typed definition including objective/hints/protocol/cases; PlantSha256 hashes the five canonical named parameters; helper writes stable property names `length`, `speed`, `objectLength`, `photoeyePosition`, `minimumPhotoeyeOn`. Program root display name/id/watch edits conservatively invalidate displayed current proof immediately; rerun is required even if hash unchanged. No automatic resurrection on Undo in MVP. Historical pass remains in progress. RunEpoch is monotonically increasing coordinator-local token, not saved content identity and not a claim of cross-restart global sequence. Cancel retires it immediately.

## A07: worker cancellation and lifecycle failure path

Owner: native coordinator + UX + QA; blocker severity: high.

Task.Run only receives frozen pure objects. Check cancellation before each scan and after each case/reset boundary. Callback result dispatch occurs on main thread and requires same active epoch, not cancelled, same challenge/program generation and live coordinator. Cancel retires epoch before requesting token cancellation; canceled worker can never persist progress or announce pass, even if already computed it. Only coordinator main-thread valid-result path writes progress. One worker slot retained until task actually ends; Test disabled while old worker outstanding, including after canceled UI epochs. Do not dispose token or shared callbacks while still in use.

250ms is reference-machine observed target, not hard real-time guarantee. Navigation/close/reset requests allow <=2seconds asynchronous wait for cleanup (no UI-thread Wait/Result blocking). If worker still running, stay in current workspace, show `Cancellation still pending`, preserve read/copy/Save Draft access, allow cancellation retry; no forcekill, process termination, document discard or second worker. Discard deferred navigation/close/reset intent after the timeout; no late automatic action when the worker eventually ends. Application Stop may perform ordinary live Stop immediately; normal Reset proceeds only once timely cancellation cleanup completes. Closing coordinator subscriptions detaches only after worker completion, or pending continuation observes retired/disposed guard without touching freed UI. Actual reference hardware responsiveness must be measured; deliberate delayed-worker tests prove no stale award and no subsequent worker spawn. Per UX meeting resolution, all draft mutations remain locked while worker outstanding, including canceled epochs; CanTest stays false until actual completion. User-requested Save Draft intentionally updates saved baseline; Test alone never does.

## Other-department critique and dependency ledger

| Interface issue | Amendment/disposition | Owner | Evidence |
| --- | --- | --- | --- |
| UX no replay but art trial-looking live scene | Live exploration label; isolated trace owns case scan/time; no highlights pretending live sensor is failed historical sensor | UX+art+coordinator | Native screenshot/keyboard state review |
| Product7case ledger vs architecture6cases | 07 CV01-07 accepted; explicit Cycle/CaseSequence/reset metadata in A04 | Content+QA | Positive and mutation case matrix |
| QA asks raw/effective/copied phases | A03 event with three typed signal maps and no aggregate references | Runtime+QA | Force/input-overlay/deep-copy/batch/stopped-event tests |
| Production isolated test vs draft save/close recovery | A07 retirement/outstanding-worker distinction | Coordinator+UX | Delayed-worker close/cancel/reset/edit tests |
| Assembly identity conflated with evaluator | Semantic source manifests + independent evidence artifact hash | Release+runtime | Manifest/content consistency and mismatch rejection |
| Bounds mention stimuli absent case schema | Remove irrelevant general stimulus interface and8thcase allowance | Content+QA | Exact schema/all7required/duplicate/unknown case tests |

## A08: complete authored packet and conservative identity scope

Owner: learning/content + identity/release. Definition additionally needs a versioned glossary (binding/name/meaning), disclosed eligibility policy identifier and unsolved starter relative resource path + SHA256. Resource lookup is fixed trusted catalog lookup, never a user-supplied traversal path. Validate starter bytes before loading; starter retains tag descriptions and no solution rungs. Default challenge never loads solved Demo 3. ChallengeSha256 hashes canonical full authored packet plus the referenced starter byte SHA256 and byte length; those hash values are validated against actual packaged bytes, not trusted because the packet declares them. Changing objective/hints/glossary/starter/policy invalidates current proof. A separate packet manifest retains exact-byte hash for packaging provenance.

For EngineSha256 use the complete sorted source manifest of `src/VirtualController/*.cs` plus `src/Scenes/ConveyorPlantModel.cs` and challenge adapter/eligibility/identity implementation, rather than assume A06's illustrative explicit subset contains all semantic dependencies. Evaluator manifest includes all runner/evaluator/protocol implementation files. Artifact fingerprints are conservative executable-content identity, not proof of semantic equivalence. Source textual differences can invalidate a proof without changing behavior; acceptable for this slice. Native artifact/build dependency fingerprints remain independent evidence.

The pilot's variable Start-duration transfer probe is removed from first-slice acceptance because all seven protocols define one accepted-scan pulses. General held-Start/rearming behavior requires a separately disclosed later contract.

Meeting disposition after these amendments: architecture supports the narrow offline slice and task sequence. Structural plan issues remain until final spec incorporates A01-08 and QA/UX confirm their acceptance boundaries. No runtime, native visual, accessibility, package, rights or physical/live-PLC acceptance is claimed. Final user authorization is still required before implementation.
