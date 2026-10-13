# RungProof Machine-Puzzle Implementation Plan

> For agentic workers: after explicit human authorization, use `superpowers:subagent-driven-development` or `superpowers:executing-plans` task by task. This plan authorizes no execution itself. Each task has one responsible source writer and an independent acceptance reviewer.

**Goal:** deliver one trustworthy, playable offline conveyor challenge while preserving existing RungProof behavior, then qualify a bounded internal pilot.

**Architecture:** use the existing ladder/runtime and pure conveyor model, add a copied accepted-scan boundary and isolated worker grader, and compose an additive native presenter around the current editor. Separate authored content, grading, local progress, native evidence and public-release gates.

**Tech stack:** existing Godot 4.7.2 Mono/C#, .NET 10, System.Text.Json, existing symbolic runtime/model, PowerShell packaging. No new product dependency is proposed.

**Spec:** `08-studio-specification.md`, including section 14 concurrent drift. Full studio roadmap is in spec section 11; this is the executable one-slice tranche, not full-game authorization.

## Global constraints

- Canonical root is `Siemens-PLC-PC-Interface/rungproof/rungproof-next`; paths below are relative to it unless noted.
- Current dirty WIP and concurrent release work must be reconciled/recoverably preserved before edits. Original HEAD alone is not the accepted application.
- Fixed challenge period 20 ms; exact CV01–07 and ungraded CV08, supported bindings and eligibility in the spec.
- Seven cases, 250 accepted scans each, total 1750; 32 metadata events; combined 2048 retained records. No silent truncation.
- No worker Godot/PLC/file-command access, forces, authored solution substitution, arbitrary scripts or output manipulation by evaluator.
- No source rewrite, second editor/language, catalog conversion, default demo framing change, cloud/replay/sound requirement or implicit release.
- Actual native visual/keyboard/DPI qualification is required before calling the slice complete. Static/headless/pure checks are separate evidence.
- Generated help is not hand-edited. Content/manifests are versioned trusted resources with exact byte provenance.

## Review focus

1. Original unsupported block/interface fields disappearing through serialization: validate original first, then clone and revalidate (T1/T5).
2. Lifecycle/stopped events and phase-mixed mutable points creating false success: typed copies, accepted counter, actual presence and binding maps (T2/T3/T8).
3. Computed result delivered after Cancel/navigation, or overlapping canceled workers: epoch retirement, single outstanding worker and timed cleanup policy (T5).
4. Invalid starter/save failure destroying work or showing stale current proof: draft guards, separate progress, write fault injection (T4/T6).
5. Green pure tests paired with unreadable/native-miswired scene or stale package manifest: same native fixtures, actual keyboard/DPI, exact artifact checks (T7–T9).

## Authoritative proposed types

These are **new** contracts to create, not existing APIs. Namespace `RungProof.Next.Challenges`. Signal/event records in the spec live in `RungProof.Next.VirtualController`. Public lists/maps must wrap private copies.

```csharp
public enum ChallengeOutcome { Passed, Failed, InvalidProgram, InvalidChallenge, Cancelled, InfrastructureError }
public enum ChallengeCaseKind { ColdStart, Holding, BeamStop, ProcessStop, SimultaneousStartStop, BlockedStart, ResetRepeat }
public enum ChallengeTraceEventKind { CaseStarted, ResetCheckpoint, CaseFinished }
public enum ChallengeUiState { Idle, Validating, Running, Cancelling, Finished }
public enum ChallengePersistenceStatus { NotAttempted, Saved, NotSaved }
public enum ChallengePendingAction { None, Navigate, Close, Reset }
public sealed record ConveyorPlantParameters(double Length, double Speed, double ObjectLength, double PhotoeyePosition, double MinimumPhotoeyeOn);
public sealed record ChallengeTestCase(string Id, ChallengeCaseKind Kind, int MaximumScans);
public sealed record ChallengeGlossaryEntry(string Binding, string DisplayName, string Ownership, string Meaning);
public sealed record ChallengeStarterResource(string ResourceId, string Sha256, long ByteLength);
public sealed record ChallengeDefinition(int SchemaVersion, string Id, int Revision,
    string SceneId, string ProtocolRevision, int ScanPeriodMs, string Title,
    string Objective, IReadOnlyList<string> Hints, ConveyorPlantParameters Plant,
    IReadOnlyList<ChallengeTestCase> Cases, IReadOnlyList<ChallengeGlossaryEntry> Glossary,
    string EligibilityPolicy, ChallengeStarterResource Starter);
public sealed record ChallengeDefinitionLoadResult(ChallengeDefinition? Definition, IReadOnlyList<string> Issues);
public sealed record ChallengeRunIdentity(string IdentityFormat, string ChallengeId,
    int ChallengeRevision, string ProgramSha256, string ChallengeSha256,
    string PlantSha256, string EngineRevision, string EngineSha256,
    string EvaluatorRevision, string EvaluatorSha256, int ScanPeriodMs, long RunEpoch);
public sealed record ChallengePlantState(IReadOnlyDictionary<string, bool> Feedback,
    IReadOnlyDictionary<string, double> Numbers, IReadOnlyDictionary<string, string> Text,
    bool ObjectPresent, long Completed);
public sealed record ChallengeObservation(ChallengeRunIdentity Identity, string CaseId,
    int Cycle, long CaseSequence, long ScanNumber, TimeSpan SimulatedTime,
    IReadOnlyDictionary<string, bool> EffectiveInputs,
    IReadOnlyDictionary<string, bool> AppliedOutputs, ChallengePlantState PlantAfter,
    IReadOnlyList<string> RuntimeDiagnostics);
public sealed record ChallengeTraceEvent(string CaseId, int Cycle, long CaseSequence,
    ChallengeTraceEventKind Kind, long ScanNumber, TimeSpan SimulatedTime,
    ChallengePlantState Plant, string Detail);
public sealed record ChallengeCaseResult(string CaseId, ChallengeOutcome Outcome,
    string Code, string Expected, string Observed, ChallengeObservation? FirstFailure);
public sealed record ChallengeRunResult(ChallengeRunIdentity Identity,
    ChallengeOutcome Outcome, IReadOnlyList<ChallengeCaseResult> Cases,
    IReadOnlyList<ChallengeObservation> Trace, IReadOnlyList<ChallengeTraceEvent> Events,
    string Code, string Diagnostic);
public sealed record ChallengePresentationState(ChallengeUiState State,
    ChallengeRunIdentity? ActiveRun, ChallengeRunResult? LastResult, bool IsCurrentProof,
    bool CanTest, bool IsWorkerOutstanding, bool MutationLocked,
    ChallengePendingAction PendingAction, ChallengePersistenceStatus PersistenceStatus,
    string StatusText);
public sealed record ChallengeCasePresentation(string CaseId, int Index, ChallengeCaseResult? Result);
// Result == null is the presentation label "Not Run", not a grading outcome.
public sealed record ChallengeProgressRecord(ChallengeRunIdentity Identity,
    DateTimeOffset CompletedUtc, IReadOnlyList<string> UsedHintIds);
public sealed record ChallengeProgressLoadResult(IReadOnlyList<ChallengeProgressRecord> Records, IReadOnlyList<string> Issues);
public sealed record ChallengeProgressWriteResult(bool Success, string Diagnostic);
public sealed record ChallengeSemanticSourceEntry(string RelativePath, string Sha256, long ByteLength);
public sealed record ChallengeSemanticManifest(int SchemaVersion, string IdentityFormat,
    string EngineRevision, string EngineSha256, string EvaluatorRevision,
    string EvaluatorSha256, string GeneratorSha256,
    IReadOnlyList<ChallengeSemanticSourceEntry> EngineInputs,
    IReadOnlyList<ChallengeSemanticSourceEntry> EvaluatorInputs);
public sealed record ChallengeSemanticManifestLoadResult(ChallengeSemanticManifest? Manifest, IReadOnlyList<string> Issues);
public sealed record ChallengeRunLifecycleState(long? WorkerEpoch, long? AwardEpoch,
    bool IsWorkerOutstanding, bool MutationLocked,
    ChallengePendingAction PendingAction, TimeSpan? PendingActionDeadline);
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
    ChallengePlantState CaptureState();
}
```

The evaluator's `EffectiveInputs` is binding-keyed from the event's effective tag-keyed inputs. Trial invariant: no forces, so sampled/effective input values match. Event SampledInputs and EffectiveInputs remain separately recorded/tested at runtime boundary. Native artifact/build hashes belong in evidence manifests, not RunEpoch/content identity. Progress serialization omits RunEpoch from persisted content identity; reconstruct it as 0 for historical records and never use it to award current proof.

`ChallengeEvaluator.BeginCase(ChallengeRunIdentity identity, ChallengeTestCase testCase) -> void`, `Observe(ChallengeObservation observation) -> ChallengeCaseResult?` and `FinishCase() -> ChallengeCaseResult` own temporal checks. Observe returns the first terminal failure when found, otherwise null; FinishCase returns the final typed result. Runner owns time/stimuli and continues independent cases after behavioral failure.

## T0 — Reconcile and preserve the current source

**Writer:** release/integration engineer. **Review:** Dot plus affected source owners.

**Read:** `00-baseline.md`, `baseline-working-files.json`, `baseline-delta-observed.json`, current Git status/diff, current AGENTS/current release records and all touched source. **New evidence only:** execution-specific preservation manifest and reconciliation ledger under an agreed non-source evidence location.

- [ ] Reinspect current branch/HEAD and every changed/new file relative to the original inventory. The observed four changed/six new files are a bounded earlier delta, not a final frozen count.
- [ ] Coordinate exclusive ownership of compiler, export scripts and newly appearing `verify_demo_package.ps1`/tests with concurrent work. Reuse the accepted checker rather than duplicate it.
- [ ] Copy recoverable tracked/modified/untracked application bytes plus explicitly needed ignored resources into a separate timestamped preservation directory; record status, HEAD, binary diff, file paths/lengths/hashes. Hash the copies against source and demonstrate restoration into a disposable copy without changing the checkout. Inventory alone or a stash alone is insufficient.
- [ ] Select working-state integration strategy. If isolated worktree/branch is chosen, carry the accepted WIP byte-for-byte under its manifest; never assume HEAD worktree contains it. Preserve unrelated changes and do not clean/reset/revert.
- [ ] Run bounded baseline build/controller/plant/offline/connection checks from the accepted snapshot after authorization, using isolated lanes below. No old PASS count substitutes for current evidence. A failure or unexpected source drift stops feature integration and gets a smallest scoped repair/review.
- [ ] Assign actual role holders and execution screen access. Confirm current signature/path/project assumptions. Freeze accepted source identity and reestimate W1–W8. No commit/push/publish is inferred.

**Exit:** recoverable byte-verified snapshot, drift disposition, buildable accepted baseline, sole-writer ledger. This gate is mandatory even if the human approves the rest of the plan.

## T1 — Versioned content, eligibility and conservative identity

**Writer:** challenge engineer; learning author owns content bytes. **Review:** Controls + independent QA.

**Create:** `src/Challenges/ChallengeContracts.cs`, `ChallengeDefinitionJson.cs`, `ChallengeEligibilityValidator.cs`, `ChallengeProgramIdentity.cs`, `ChallengeSemanticManifest.cs`; `challenges/catalog.json`, `challenges/conveyor-stop.challenge.json`, `challenges/starters/conveyor-stop.rpproj.json`; `tests/Challenges/RungProof.Next.Challenges.Tests.csproj`, `Program.cs`, `ChallengeFixtures.cs`, `ChallengeContractTests.cs`. **Release owner's T1 prerequisite:** `tools/build_challenge_semantic_manifest.ps1` and trusted `challenges/semantic-manifest.json` generation. T9 reuses/extends this producer; it is not first created after M1.

**Consumes existing:** `LadderEditorDocument.BuildProgram()`, `LadderCompiler.Compile`, `SceneIoBindingValidator.Validate`, `LadderProgramJson.Save/Load`, current scene contract. **Produces:** exact contracts above; `ChallengeDefinitionJson.Load(string) -> ChallengeDefinitionLoadResult`, `ChallengeEligibilityValidator.Validate(LadderProgram) -> IReadOnlyList<LadderValidationIssue>`, `ChallengeProgramIdentity.ProgramSha256(LadderProgram) -> string`, `ChallengeSemanticManifest.Load(string json) -> ChallengeSemanticManifestLoadResult`.

Producer parameters: `-SourceRoot <absolute Next directory> -OutputPath <absolute JSON file>`. Manifest schema 1/identity RP-CANON-1; engine revision `conveyor-engine-v1`, evaluator revision `conveyor-evaluator-v1`. Engine inputs are current complete `src/VirtualController/*.cs`, `src/Scenes/ConveyorPlantModel.cs`, `src/Challenges/*.cs`; evaluator inputs are exactly `ChallengeTestRunner.cs`, `ChallengeEvaluator.cs`, `ConveyorChallengeProtocol.cs`. Missing required files fail generation. T1 parser/canonicalizer tests use explicit in-memory/synthetic manifest fixtures; the producer is tested against a disposable fixture source tree, not a partially implemented runtime. At T3, all three evaluator files exist and generation against the complete accepted source is mandatory before the behavioral M1 build. There is no contract-only manifest accepted at runtime. Regenerate for every subsequent source change/build. Entries use ordinal-sorted relative paths, UTF-8 LF-normalized source bytes, lower-case SHA256 and normalized byte length. Generator hash is included in identity/build evidence; the manifest never includes itself. Hash the RP-CANON-1 canonical source-entry arrays to produce EngineSha256/EvaluatorSha256. Record canonicalizer implementation source as an engine input and compare generated hashes against the C# parser/canonicalizer independently.

- [ ] Write and run red tests `DefinitionRequiresExactlySevenCases`, `DefinitionRejectsDuplicateUnknownFieldsAndLimits`, `UnsolvedStarterRemainsEditableAndSaveable`, `DefaultEditorFunctionContinuousTaskIsEligible`, `OriginalHiddenInterfaceRejectedBeforeClone`, `RenamedTagsKeepRequiredBindings`, `ForbiddenUnusedInstructionAndShadowNetworksRejected`, `CanonicalIdentityNormalizesPropertiesNumbersAndRootDisplayMetadata`, `ExecutionBindingOrderInitialAndPeriodChangesIdentity`.
- [ ] Assertions pin schema 1/protocol conveyor-v1/20 ms/five plant constants; byte/depth/text/nodes limits; exact single Function/Continuous task policy; required BOOL bindings and all initial false; no hidden unsupported fields. Unknown policy/case produces InvalidChallenge; unsupported imported program produces InvalidProgram with location.
- [ ] Implement strict bounded parser/canonicalizer and original-object eligibility using existing compiler/binding rules without weakening sandbox or instruction semantics. Clone only an already eligible object and revalidate worker copy.
- [ ] Author unsolved packet/glossary/hints and publish all seven plain-language situations. Fixed catalog resource lookup rejects traversal; actual starter hash/length validated. Demo 3 file/entry unchanged.
- [ ] Re-run focused tests: require exit 0 and `CHALLENGE_TESTS_PASS` with zero FAIL. Record marker count, not a predetermined old suite total. Review content accuracy independently of parser agreement.

**Exit:** bounded exact packet and eligible normal editor/legacy variants, reproducible identity, meaningful pre-worker diagnostics. PGL-002/003/009/013, ARCH-02/07, QA-04/06, PROD-004.

## T2 — Accepted-scan boundary and pure conveyor adapter

**Writer:** runtime engineer. **Review:** challenge engineer + QA.

**Modify:** `src/VirtualController/VirtualControllerSession.cs`, narrowly scoped primitive input-image helper in `VirtualControllerRuntime.cs` if required; `src/Scenes/SceneSimulationRuntime.cs` for the narrow native capture below. **Create:** `src/VirtualController/VirtualControllerScanCompleted.cs`, `src/Challenges/ConveyorChallengePlantAdapter.cs`, `tests/Challenges/ChallengeBoundaryTests.cs`.

**Consumes:** exact spec event records, existing Session.ScanPeriod/Advance/Run/PulseInput, `SceneIoImageMapper`, `ConveyorPlantModel`. **Produces:** `ScanCompleted`; `ConveyorChallengePlantAdapter(ConveyorPlantParameters)` implementing `IChallengePlantAdapter`.

If extraction belongs in runtime, its new exact helper is `internal VirtualControllerSignalImage CaptureEffectiveInputs(VirtualControllerSnapshot snapshot)`: filter validated Input-role primitive leaves (including expanded aggregate leaves) into copied BOOL/numeric/text maps; exclude Memory/Output and nested aggregate objects. Session calls it for the accepted snapshot. This is a new helper, not an assumed existing API.

Native capture proposal: `SceneSimulationRuntime.CaptureConveyorChallengeState() -> ChallengePlantState`, called on the Godot thread after the accepted boundary. It copies `simulated_photoeye`, `object_position`, `component_state`, and explicit private `_conveyorPlant.ObjectPresent/Completed`; it returns no `_points` aliases and throws a readable invalid-operation error for a non-conveyor scene. No setter, evaluator command or worker Godot dependency is introduced. Main's native verifier consumes this exact copied method in T5/T8; pure console tests do not compile SceneSimulationRuntime.

- [ ] Red tests `LifecycleForceAndStoppedAdvancePublishNoCompletedScan`, `AcceptedEventOccursAfterCommitAndPlant`, `SampledAndEffectiveImagesAreCopiedPrimitives`, `ReturnedMapsCannotMutateRuntimeOrLaterRows`, `OperatorPulseResolvesByBindingAndLastsOneScan`, `DischargedPositionZeroRetainsAbsentAndCompletedState`.
- [ ] Pin exactly one event per incremented controller scan, correct pre-scan/operator/force distinctions, copied applied output and post-step state. Preserve existing SnapshotPublished behavior, including stopped Advance behavior; runner never calls it stopped.
- [ ] Implement copied read-only event maps and adapter based on existing model, with finite validated parameters. BOOL binding points are not variable names; mapper translates before/after session calls. No new physics or external transport path.
- [ ] Re-run boundary/plant/controller lanes. Force tests qualify event distinction only; trial runner exposes no forces. Verify no Godot references in pure challenge project.

**Exit:** trustworthy phase/copy boundary and pure model presence/state. ARCH-03/04/06, QA-02/03, ART-003.

## T3 — Behavioral runner, evaluator and independent fixtures

**Writer:** challenge engineer; QA owns independent fixture files. **Review:** Controls + runtime lead.

**Create:** `src/Challenges/ChallengeTestRunner.cs`, `ChallengeEvaluator.cs`, `ConveyorChallengeProtocol.cs`; `tests/Challenges/ChallengeBehaviorTests.cs`, `ChallengeIsolationTests.cs`. **Produces:** `ChallengeTestRunner.Run` signature in spec; evaluator methods above and fixed seven protocol implementations.

- [ ] Red tests `OutputSealInAndInternalAssignSealInBothPassAllCases`, `RenamedBindingVariantsPass`, `MissingHoldFailsCV02AfterStartRelease`, `MissingStopFailsCV04AtScan11`, `MissingSensorFailsCV03AtFirstEffectiveTrueScan`, `UnconditionalFailsCV01`, `AlwaysOffCannotPassStoppedOrigin`, `BlockedStartBypassFailsCV06`, `EligibleRestartFailsHold`, `ResetRepeatUsesMonotonicCaseSequence`, `DischargeCannotPassAtProjectedZero`.
- [ ] Pin CV03 first actual post-step transition n <= 75, effective sampled true/off at n+1, inclusive symbolic range with epsilon 1e-9, 25 subsequent held scans, present/completed invariants. CV07 reset is metadata with Cycle 1 and continuous CaseSequence; count both cycles toward 250.
- [ ] Implement protocols and evaluator separately: driver sets allowed operator inputs, session/model run time, evaluator only observes. Fresh case runtimes, no forces/backlog/pulses inherited. Stable codes include CH_CASE_TIMEOUT, CH_RUNTIME_DIAGNOSTIC, CH_CANCELLED, CH_INVALID_PROGRAM, CH_INVALID_CHALLENGE, CH_ADAPTER_ERROR and CH_TRACE_LIMIT.
- [ ] Red tests `CaseOrderAndRepeatedRunsHaveSameObservations`, `BatchAndSingleAcceptedScansAgree`, `MissingEventAndDiagnosticCannotPass`, `Case250Run1750Metadata32Trace2048Bounds`, `CancelAtEachBoundaryReturnsNoPass`, `UntrustedManualExplorationDoesNotEnterTrial`.
- [ ] Use one-scan Advance calls for normal worker execution; batch equivalence is a boundary regression, not a second clock. Continue fresh cases after behavior failure; infrastructure/cancel invalidity stops and unexecuted case rows are Not Run.
- [ ] Run focused and existing pure lanes, then independent fixture/code review. Do not count invalid-instruction rejection as proof of eligible restart discrimination.
- [ ] Before the M1 behavioral build, run the T1 producer against the now complete accepted source into the isolated fixture output; verify all three evaluator entries and source hashes. Regenerate after any runtime/challenge change; tests must never accept a missing/incomplete production manifest.

**Exit / M1:** executable console harness rejects plausible wrong ladders and accepts two actual eligible structures. This is pure simulator proof, not native completion. PGL-004/005/010, ARCH-05/08, QA-01/03/06.

## T4 — Separate local progress and recovery

**Writer:** challenge persistence engineer. **Review:** QA + app integration.

**Create:** `src/Challenges/ChallengeProgressRepository.cs`, `tests/Challenges/ChallengeProgressTests.cs`. **Consumes:** validated content identity and existing atomic-write pattern; absolute path injected by Main from existing build-specific user data. **Produces:** exact Load/Save result contracts above.

- [ ] Red tests `PassIdentityAndHintHistoryRoundTripWithoutRunEpoch`, `ChangedContentEngineOrProgramIsHistoricalNotCurrent`, `UnknownFutureSchemaPreservesOriginal`, `CorruptOversizedAndDuplicateRecordInputsRejected`, `TempOrReplaceFailurePreservesPriorGoodFile`, `ProgressWritesSerialized`, `FailedWriteReturnsNotSavedWithoutChangingSemanticPass`.
- [ ] Implement strict <=1 MiB/1000 records/depth32 serialization to fixed progress filename, same-directory temporary write/replacement, last-good backup and corrupt original preservation. No IDs in path construction, project-schema change or silent overwrite of drafts.
- [ ] Keep current proof invalidation in coordinator; historical disk pass never automatically verifies edited/undo-restored code. Progress failures show readable recoverable status.
- [ ] Run deterministic file-fault fixtures in temporary test directories, plus actual native restart later. Atomic replacement tests are not power-loss durability proof.

**Exit:** trustworthy separate progress and honest save failure. PGL-007/010, ARCH-09, QA-04/05.

## T5 — Main/coordinator lifecycle and worker isolation

**Sole writer:** app integration engineer for `src/App/Main.cs` and partials. **Review:** runtime + UX + independent QA.

**Create:** `src/App/ChallengeSessionCoordinator.cs`, `src/App/Main.Challenges.cs`, `src/App/Main.ChallengeReview.cs`. **Modify:** narrow Main construction/delegation paths. **Create tests:** pure lifecycle/state machine seam in `src/Challenges/ChallengeRunLifecycle.cs`, `tests/Challenges/ChallengeLifecycleTests.cs`.

**Consumes:** frozen packet/program, pure Run result, progress service, existing live controller/draft guards. **Produces:** presenter intents/notifications in spec, proposed `--verify-challenge-conveyor` native verifier. `ChallengeRunLifecycle.TryBegin(long epoch) -> bool` reserves a single worker slot and returns false if one is outstanding; `Retire(long epoch) -> bool` clears matching award eligibility without freeing worker ownership; `Complete(long epoch) -> bool` clears matching worker ownership and returns whether its epoch was still award-eligible. A mismatched epoch changes nothing and returns false. `State -> ChallengeRunLifecycleState` is copied primitive state; MutationLocked equals outstanding ownership. Coordinator derives presentation state and composes cancellation token/task, timed pending actions, main-thread delivery and existing actions.

For deterministic cleanup tests the same pure lifecycle owns pending action timing: `QueuePendingAction(ChallengePendingAction action, TimeSpan monotonicNow) -> bool` accepts one non-None action only while a worker is outstanding and sets deadline `monotonicNow + 2 seconds`; `ExpirePendingAction(TimeSpan monotonicNow) -> ChallengePendingAction` clears/returns the old action at or after its deadline, otherwise None; `TakePendingActionAfterCleanup(TimeSpan monotonicNow) -> ChallengePendingAction` first expires any late action, returns None while a worker remains, otherwise clears/returns the timely queued action. Complete itself never navigates or applies Reset. Coordinator supplies Stopwatch-derived monotonic elapsed time; unit tests inject TimeSpan values without sleeps. A late task completion cannot bypass the same expiration check.

- [ ] Red tests `OriginalDocumentHistoryPathAndDirtyBaselineUnaffectedByTest`, `UnsupportedOriginalRejectedBeforeSerialization`, `ComputedPassCancelledBeforeDeliveryNeverAwards`, `RetiredWorkerBlocksReplacementUntilActualCompletion`, `TwoSecondCleanupDropsPendingNavigationCloseReset`, `LateCompletionNeverReplaysDeferredAction`, `ReadCopySaveRemainAvailableWhileMutationLocked`, `StopSimulationCancelsAndNormallyStopsLive`, `CancelAloneLeavesLiveState`, `ResetWaitsForTimelyCleanupAndPreservesDraft`.
- [ ] Capture immutable worker copy only after original validation, then revalidate/hash; worker has no Godot references and no callback to live points. Reject external source or active PLC before test. Challenge entry does not silently switch a connected bench session.
- [ ] Implement immediate epoch retirement, token checks, one outstanding task, no UI Wait/Result. Lock mutation until actual worker exit; keep read/copy/save. On 2 s cleanup timeout drop queued action and remain workspace; no auto-navigation, kill or second worker. Save Draft intentionally uses normal save baseline behavior.
- [ ] Dispatch per-case progress/state and immutable results main-thread only; verify coordinator lifetime/epoch/document generation before award/write. Wrong hash/manifest is configuration error, not learner failure. Any draft mutation invalidates current proof; no implicit undo reinstatement.
- [ ] Add exact native verifier using shared frozen fixture/protocol identities and current scene adapter, with code/exit/PASS marker. Stale callbacks and failures produce no completion cue/progress.

**Exit:** robust isolated Test integrated with live exploration and draft lifecycle. ARCH-01/02/08, UX-006/008/012, QA-03/04/05.

## T6 — Focused challenge presenter and keyboard editor

**Sole writer:** UI engineer for `src/App/SimulatorShell.cs` and presenter/partials; editor engineer for current `src/App/LadderEditorCanvas.cs`, with explicit handover coordinated with the UI owner. **Review:** UX + QA + learning.

**Create:** `src/App/SimulatorShell.Challenges.cs`, `src/App/ChallengePresenter.cs`; scoped native verification additions in the sole Main partial owner. **Consumes/produces:** exact intents/state/result/progress/cleanup notifications in spec; no presenter runtime/output/progress writes.

- [ ] Add objective, seven disclosed test situations, correct glossary/starter, hints, relevant live controls and historical trace/result panel. Preserve existing demo entry, views, save/open, validation and focused shortcut behavior.
- [ ] Implement existing guard integration for challenge navigation/Revert Starter; Reset is not starter reversion. Invalid unsolved starter remains editable/saveable; Test explains compiler/binding/eligibility issue without losing it.
- [ ] Implement state/read-only mutation gating, Save Draft route, visible cancellation pending, Not Run rows and Passed/Progress not saved. Historical trace never paints live monitor values as replay.
- [ ] Meaningful verifier tests: `ChallengeEntryDoesNotLoadDemoSolution`, `NavigationCancelPreservesDraftAndFocus`, `DirtyEditImmediatelyInvalidatesCurrentProof`, `PaletteRestrictionAlsoChecksImportedProgram`, `HistoricalTraceDoesNotChangeLiveScene`, `HiddenOrUnfocusedCanvasDoesNotConsumeEdits`.
- [ ] Implement full keyboard branch/contact/tag flow, explicit layout presets or keyboard divider control, UI16/ladder14 logical text and aligned custom draw/hit scaling. Actual native keyboard qualification in T8; emitted button events are not sufficient.
- [ ] Run authoring/editor/shell regression lanes and inspect actual rendered entry/expanded hints/results/dialog/failure at target logical sizes once screen access is authorized.

**Exit:** learner can operate the first challenge without pointer dependence, with recoverable documents and readable evidence. PGL-001/008/011, UX-001–012.

## T7 — Reuse art, copied live labels and challenge framing

**Writer:** technical artist; Main integration requests go through sole owner. **Review:** Art + Controls + UX.

**Create:** `challenges/presentation/conveyor-stop.presentation.json`, scoped UI label/icon assets if needed; provenance/approval ledger under new evidence docs. **Modify only if justified:** current camera intent integration, selected reusable material/label presentation; preserve scene geometry/model and demo defaults.

- [ ] Reconcile current mesh hashes and candidate carton/older asset approval against source; distinguish historical visual review, new assembled challenge qualification and distribution rights.
- [ ] Add trusted presentation metadata for focus equipment, perspective direction/FOV and label anchors; invalid/missing anchors fall back to relevant I/O text. No orthographic rewrite or mesh rebuild in tranche.
- [ ] Use copied live main-thread observations for scene labels. Do not load worker trace into transforms/beam/current ladder. Green paired with textual state; plain background, no repeating lines, no shake/orbit/flashing celebration/sound dependence.
- [ ] Native idle/travel/beam/Stop/blocked/Reset review at actual scene aperture and supported DPI, with hints/results open; inspect picking/camera routing versus editor focus. Bounds checks alone do not accept visual readability.

**Exit:** focused understandable process, preserved model/installation behavior and explicit art/rights evidence. ART-001–010.

## T8 — Independent native parity and usability qualification

**Writer:** QA fixture/evidence owner; source fixes requested through assigned writers. **Review:** Dot + UX + Controls + Art.

**Tests/evidence:** same ChallengeFixtures and protocols in native Main verifier; actual keyboard/DPI/screenshots and runtime observations, with accepted source/program/content/manifests/artifact identity.

- [ ] Run both valid structures, renamed tags and six principal eligible behavioral negatives through native output/plant projection. Compare first failure/outcome, effective binding input/output, model position/presence/completion and scan phases against pure fixtures. Inspect Reset and no-phantom-event behavior.
- [ ] Run scoped preserved controller/plant/connections/offline policy/app shell/ladder/demo sequence verifiers; fake/in-memory transport only. A nonzero exit, missing marker, diagnostics indicating failure, stale binary or forced termination fails gate.
- [ ] Qualify actual logical 1280x720 at OS100/125/150/200 and baseline1920x1080 at100, recording physical monitor/scale/renderer/content/aperture. Native screenshot and full keyboard workflow must prove no overlap/clipping, required control access and focus routing. UI16/ladder14 threshold measured at declared zoom.
- [ ] Probe actual assistive semantic support. If insufficient, present explicit reduced scope for human decision or separately estimated remediation; never mark full accessibility passed from keyboard/screenshots.
- [ ] Exercise cancel/close/invalid draft/save recovery/long tags/hints/results in actual application. Preserve signals through monochrome/muted operation. Human screen access must be granted for this future gate; no desktop action in planning.

**Exit / M2:** one fully playable native slice with independently challenged grading and visible/keyboard evidence. Any open required support/false-pass/data-loss issue blocks slice completion.

## T9 — Build identity, exact internal candidate and release dossier

**Sole writer:** release engineer for generator/build/export/config/notices; independent QA reviewer.

**Reuse/extend T1:** `tools/build_challenge_semantic_manifest.ps1`, generated trusted `challenges/semantic-manifest.json`. **Create:** new challenge candidate evidence/known limitations/support/rights records. **Modify:** accepted current `tools/build_offline_demo.ps1` and existing candidate/checker documentation. **Reuse/review:** concurrent `tools/verify_demo_package.ps1`, `tools/test_verify_demo_package.ps1` once finalized, not a duplicate checker.

- [ ] Generate full semantic manifests before build, including generator/schema/canonicalizer/input identity, excluding output self-input. Fingerprint complete controller/challenge/model source, authored packet/starter and generator before/after export; compare actual inputs with generated entries. Fixture lane uses same producer into isolated output. Runtime reads fixed bundled resources only.
- [ ] Add content/manifest/starter to existing export fingerprint/inventory, and actual exported executable challenge/policy checks. Missing/mismatch fails export; development Test-disabled diagnostic is explicit. Pure console policy assembly is not proof of exported policy.
- [ ] Reuse current package checker capabilities after T0 reconciliation, add challenge identity/resource checks and meaningful negatives where missing. Record exact artifact hash, runtime/notices inventory and clean exit. Signing/public distribution remain separate gates.
- [ ] Test exact copied package on agreed standard-user clean Windows target, without development runtimes/tools/network, spaces/non-ASCII/read-only installation path and writable user data. Complete five supported demos plus challenge, invalid draft/reopen/cancel/recovery/restart; qualify minimum renderer/hardware or document blocked scope.
- [ ] Close relevant internal support/privacy/rights disposition; external participant distribution needs its own rights/recruitment authorization. Document unqualified VM/RDP/platform scope. Formal security/dependency/secret/SBOM work is separately commissioned and follows applicable skills.
- [ ] Produce current candidate dossier, supported/excluded scope and rollback/data-preservation procedure. No publish/push/storefront action implied.

**Exit / M3:** exact internal candidate with truthful environmental and rights status; no stale earlier candidate accepted by association. QA-07–10, PROD-005/010.

## T10 — Separately authorized formative pilot and bounded revision

**Owner:** Product/research; learning/Controls reviewers; human recruitment/distribution authority.

- [ ] Obtain explicit participant/contact/delivery authorization and rights/privacy readiness before contacting anyone.
- [ ] Use spec six-person protocol: four learners and two experienced reviewers, identical published-case questions, built-in hints allowed, logged intervention, individual evidence. No Start-duration variation outside the one-scan contract.
- [ ] Gate >=3/4 uncoached learners complete <=30 minutes, >=3/4 adequate causal explanation, both reviewers no unresolved semantic contradiction, zero critical false-pass/data-loss/offline defect. Explain tiny sample limitations.
- [ ] One bounded correction cycle within authorized allowance; re-run affected behavior/native/package gates with new content identity. Larger redesign/fresh follow-up needs explicit reestimate/recruitment scope.

**Exit / M4:** evidence-backed product revision decision, not statistical market proof or public launch.

## Grounded commands for future execution

All commands are **planned, not executed**. From accepted `rungproof-next/`, use its pinned runtime and initialize DOTNET_ROOT/PATH. New pure test project follows existing net10.0 executable pattern, warnings-as-errors/default compile items disabled, source-links `../../src/VirtualController/*.cs`, `../../src/Scenes/ConveyorPlantModel.cs`, `../../src/Challenges/*.cs`, explicit fixture/test files; no Godot/app project reference. System.Text.Json is the existing framework library.

```powershell
$env:DOTNET_ROOT = Join-Path $PWD '.tools/dotnet'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
$taskDotnet = Join-Path $env:DOTNET_ROOT 'dotnet.exe'
$taskLane = Join-Path $PWD '.tools/studio-verification/challenges'
& $taskDotnet restore 'tests/Challenges/RungProof.Next.Challenges.Tests.csproj' "-p:BaseIntermediateOutputPath=$taskLane/obj/" "-p:MSBuildProjectExtensionsPath=$taskLane/obj/"
if ($LASTEXITCODE -ne 0) { throw 'Challenge restore failed; no build/run permitted.' }
& $taskDotnet build 'tests/Challenges/RungProof.Next.Challenges.Tests.csproj' --no-restore "-p:BaseIntermediateOutputPath=$taskLane/obj/" "-p:MSBuildProjectExtensionsPath=$taskLane/obj/" "-p:OutputPath=$taskLane/bin/"
if ($LASTEXITCODE -ne 0) { throw 'Challenge build failed; do not run a stale DLL.' }
& $taskDotnet "$taskLane/bin/RungProof.Next.Challenges.Tests.dll"
if ($LASTEXITCODE -ne 0) { throw 'Challenge executable tests failed.' }
```

Each command stops on nonzero exit before proceeding; log paths/exit and PASS/FAIL markers. Before fixture build, generate a manifest via the new producer with explicit source root and isolated output path; its proposed parameters are `-SourceRoot <absolute Next root> -OutputPath <absolute JSON path>`. No tests run an old DLL after build failure.

Use distinct `controller`, `plant`, `connections`, `offline-policy` lane roots and all three MSBuild properties for existing projects respectively:

| Existing project | Expected executable assembly stem |
| --- | --- |
| `tests/RungProof.Next.VirtualController.Tests.csproj` | RungProof.Next.VirtualController.Tests |
| `tests/Plant/RungProof.Next.Plant.Tests.csproj` | RungProof.Next.Plant.Tests |
| `tests/Connections/RungProof.Next.Connections.Tests.csproj` | RungProof.Next.Connections.Tests |
| `tests/OfflineDemo/RungProof.Next.OfflineDemo.Tests.csproj` | RungProof.Next.OfflineDemo.Tests |

Verify actual AssemblyName in accepted source, preserve app's normal Godot output path:

```powershell
& $taskDotnet build RungProof.Next.csproj --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Application build failed; no verifier permitted.' }
$taskGodot = '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe'
if (-not (Test-Path -LiteralPath $taskGodot)) { throw 'Locate the pinned console executable from DEVELOPMENT_SETUP before running.' }
& $taskGodot --headless --path . -- --verify-app-shell
if ($LASTEXITCODE -ne 0) { throw 'App shell verifier failed.' }
& $taskGodot --headless --path . -- --verify-virtual-controller
if ($LASTEXITCODE -ne 0) { throw 'Virtual controller verifier failed.' }
& $taskGodot --headless --path . -- --verify-demo-sequence
if ($LASTEXITCODE -ne 0) { throw 'Demo sequence verifier failed.' }
# Proposed new verifier, created in T5/T8; not available today:
& $taskGodot --headless --path . -- --verify-challenge-conveyor
if ($LASTEXITCODE -ne 0) { throw 'Challenge native verifier failed.' }
```

Inspect each native verifier's real-window requirement; do not use headless results as layout/keyboard acceptance. `--verify-ladder-editor`, `--verify-split-view`, `--verify-scene-controls`, `--verify-external-playback` and `--verify-offline-build-policy` exist in inspected source but their supported execution mode/current concurrent changes must be confirmed. No actual plant transport in these lanes. Existing package lane `& ./tools/build_offline_demo.ps1` is inspected/reused at T9; do not freeze obsolete parameters from this planning snapshot.

## Milestones, authorization and review

This is a dependency-and-deliverable plan, not an elapsed processing schedule. The historical 40–67 person-days in the specification estimates HUMAN LABOR only. Agent elapsed duration is unknown; no speedup conversion or completion date is promised.

Use four concurrent agent slots in waves. T1 content and early release-owned manifest work can overlap within agreed interfaces; T2/T3/T4 depend on frozen contracts. T5 app integration follows the runtime/runner interfaces; T6/T7 presentation can overlap under exclusive file ownership; T8 native qualification precedes T9 exact candidate acceptance. Reviews consume slots too. Main, shell, runtime and export families each have one writer; native screen use is serialized. The critical path is preserved baseline -> contracts/observation -> runner -> app/presentation -> native qualification -> exact package -> separately authorized pilot.

Matt's implementation approval is pending. Native screen use requires availability/authorization; a changed accessibility/support scope returns for decision. Participant contact/distribution, campaign expansion and public release retain separate approval gates. Milestone evidence and reassessment after M0 determine progress, not the human-labor range.

M1 ends T3 after T0–T2; T4 may run parallel after contracts. T5 owns shared integration; T6/T7 own separate presentation work after its interfaces. T8 ends M2; T9 ends M3; T10 needs separate human participant authority. M5/M6 are master roadmap gates requiring fresh exact per-challenge/release plans, not hidden work in this tranche.

Every task ends with independent review and bounded evidence. Tests prove behavior/limits, native review proves sampled visible usability, exact package/environment review proves declared delivery scope; none proves physical machine safety. Human implementation authorization must select one-slice audience/support/accessibility assumptions, preservation strategy and budget tolerance. Dot's plan PASS only permits presenting this package for that decision.
