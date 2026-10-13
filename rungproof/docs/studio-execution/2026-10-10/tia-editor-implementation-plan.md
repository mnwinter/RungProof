# TIA editor implementation plan

> For agentic workers: use superpowers:subagent-driven-development task by task, with an independent reviewer for each integrated change. Checkboxes record evidence, not intention.

**Goal:** Deliver the complete reviewed offline TIA-side editor scope with functional commands, truthful live instruction values, recursive authoring and real parameterized calls.

**Architecture:** Retain the existing Godot shell and deterministic controller. Extract shared command targeting, recursive document editing, scoped call execution and observation presentation behind focused interfaces. Both vendor renderers consume the same document and monitoring semantics.

**Tech stack:** Existing C#/.NET, Godot 4.7.2 Mono and repository console-test harnesses; no new paid dependency or live PLC connection.

**Spec:** `tia-editor-upgrade-design.md`, `tia-editor-semantics-contract.md`, `tia-editor-monitoring-contract.md`, `tia-editor-command-ledger.md` in this directory. All are mandatory. Status: Dot approved the design for user review; Matt authorized implementation with “Continue”. Implementation and verification are in progress. Checkboxes remain open until the corresponding acceptance evidence is complete.

## Governance and constraints

Matt explicitly requested the complete studio plan before final authorization. That ordering supersedes the planning skill's default intermediate spec-only approval. Submit spec and plan together, not repeated permission prompts. Execution method is the requested agent studio: controls/runtime, editor/UX, persistence/testing, with Dot independent review. Root owns integration, all builds/exports and final evidence.

Preserve all preexisting dirty work; record hashes/diffs before a task and review only that task's changes. Do not reset/clean or stage the entire worktree. No automatic commit/push/package replacement. Use existing studio checkout. Assign exclusive write ownership by file; runtime and shell changes with shared files are sequential, not parallel. Independent auditors may work read-only concurrently.

Editor/runtime schema 1 remains readable, schema 2 is the new writable format; workspace/bundle versions stay unchanged. Depth 32; exact types; explicit Legacy mode; no unsupported parameter fallback. Native matrix, budgets and unavailable states are exactly those in the contracts. No stripe/grid/repeating-line visual background. Native input must respect current user permission and actual available control tools; screenshots alone cannot satisfy interaction acceptance.

## Review focus

1. A stale popup after undo or block switch must never edit another node (task 1).
2. Forwarded InOut aliases can resolve to the same destination despite different names (task 7).
3. An interrupted save must leave the original project and current unsaved work recoverable (task 4).
4. A late scan publication after reset must not attach old values to a reused scan number (task 5).
5. Importing nested logic with an edge contact must not duplicate its evaluation (task 3).

## Shared execution cycle

For each task: add named regression cases described below; run the relevant test project and retain the failing result; implement the smallest task scope; rerun to green; have a fresh reviewer inspect behavior, diff and evidence. Record changed files, failures fixed and remaining gates in a task evidence note. A blocked task remains open; do not hide a required feature or rename it as informational to close it.

Primary deterministic command from rungproof-next:

```powershell
$env:DOTNET_ROOT = (Resolve-Path '.tools/dotnet').Path
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj --no-restore
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
```

Expected: exit 0, no failing cases; record actual counts/log paths, never prefill pass. New test files must be included in the existing test project/harness, whose explicit includes must be checked. Native reviews are additional, not replaced by this command.

## 1. Exact command targeting and current defects

Owner editor/UX. Modify `src/App/LadderEditorCanvas.cs`, `src/App/SimulatorShell.cs`, `src/VirtualController/LadderEditorDocument.cs`, `LadderEditorHistory.cs`; create `src/VirtualController/LadderEditorCommands.cs` and `tests/EditorCommandTests.cs`.

Proposed interfaces in the new commands file: immutable `EditorTarget` (document revision, block ID, network ID, node ID, operand role, insertion path); `EditorCommandRequest` (command ID, target, typed payload); `EditorCommandResult` (applied flag, reason, resulting selection); `CanExecute(document, request)` and `Execute(document, history, request)`. These are new internal APIs, not existing framework methods. A target resolver in canvas performs one ordered hit test for both pointer buttons; the dispatcher validates revision and mutation lock again at execution.

- [ ] Add `WirePopupPreservesSlot`, `NetworkDeleteNeverDeletesFinalContact`, `OutputCopyIsOutputOnly`, `StaleTargetIsRejected`, `AllRoutesHonorMutationLock` with exact ID/document/history assertions.
- [ ] Route menus, toolbars, keyboard, instruction tree and drops through the dispatcher. Fix EDIT-01–08 with ledger-defined behavior; no unrelated shell rewrite.
- [ ] Add `BoolMenuResetsPendingType`, `NamedBlockAndTaskCreation`, `AggregateWatchRevealsMember`, `DiagnosticsReflectLoadedState`, `HiddenDockNavigationReopensHost`, including blank/duplicate names and cancelled creation.
- [ ] Verify existing document/history suites; capture native wire/header/output menu targets and named form behavior after implementation. Review task before continuing.

## 2. Atomic point-of-use editing

Owner editor/UX, after 1. Create `src/App/LadderOperandEditor.cs`, `src/App/SimulatorShell.EditorTransactions.cs`; modify shell property wiring, `LadderProjectIndex.cs`, `AggregateTagEditor.cs`; tests `tests/EditorTransactionTests.cs`.

Interface: a staged edit session owns original target/revision and pending values; `TryCommit()` produces one EditorCommandRequest, `Cancel()` produces none. It never mutates from text-change or focus-loss signals. Scope/type resolution comes from declarations/index, not display strings.

- [ ] Cases `EscapeLeavesDocumentAndHistoryUnchanged`, `FocusLossKeepsPendingDraft`, `CreateTagAndAssignIsAtomic`, `InvalidPropertyHasNoPartialEdit`, `SaveResolvesPendingForm`, `TextFieldShortcutsDoNotDeleteNetwork`.
- [ ] Implement F2 inline typed operand entry, autocomplete, explicit undefined-tag creation, staged properties and Apply/Discard/Cancel target navigation. Add Shift+F10/menu key with the same immutable target as pointer menus.
- [ ] Implement real Save As and Cut through existing file guard/history paths; preserve clipboard when a cut is rejected. Make output/network scopes explicit.
- [ ] Add array tests preserving values by absolute index, no implicit type conversion, staged removal and undo. Verify malformed values remain visible and do not mutate the document.
- [ ] Native tests Enter/Escape/blur, popup edges, hidden panels, text focus and keyboard/context routes; review.

## 3. Lossless recursive authoring

Owner document/persistence, after 1; exclusive document ownership. Create `src/VirtualController/EditableLadderTree.cs`; modify document/snapshots/history, canvas layout, `LadderProjectIndex.cs`; tests `tests/RecursiveEditorTests.cs`.

Interface: recursive editable node has stable ID, Contact/Compare/Series/Parallel kind, ordered children and leaf operands. A network has root plus one terminal action. `Insert`, `WrapParallel`, `RemoveSubtree`, `MoveSubtree`, `CloneWithFreshIds` produce atomic mutations through commands. One measured layout exposes bounds, wire slots and traversal order to both styles.

- [ ] `NestedEdgeVisitedOnce`, `NestedEmptyShapesPreserved`, `PasteRegeneratesAllIds`, `UnrelatedIdsSurviveEdit`, `Depth32Accepted33Rejected`, `LabelReturnJumpRemainTerminalActions`.
- [ ] Replace ExpandPaths projection with structural import; preserve evaluation order and all empty shapes; reject unsupported graphs with exact path.
- [ ] Implement nested branch start/end targeting, subtree cut/copy/paste, pruning and keyboard traversal; preserve all preexisting flat authoring routes.
- [ ] Compare before/after visit order and outputs on adversarial nested inputs; inspect both layouts for wire and selection accuracy. Review before migration enablement.

## 4. Versioned persistence and recovery

Owner document/persistence, after 3; extend again after 7 for bound calls. Modify `LadderEditorProjectJson.cs`, `LadderProgramJson.cs`, `LadderCompiler.cs`, `LadderEditorDocument.cs`, `src/Workspace/WorkspaceProjectBundleRepository.cs`, shell save/open handlers. Create `tests/EditorMigrationTests.cs` and `tests/fixtures/editor-migration/` with v1/v2/future/corrupt/bundle fixtures.

Interface: loader returns document plus explicit migration/version diagnostics without writing disk. v1→v2 preserves Legacy semantics; serializer writes v2. File save validates temporary content, backs up original on first migrated replacement, then atomically replaces; failed backup/replacement leaves original intact and dirty state true.

- [ ] All five existing demos compare legacy scan results before/after migration; include invalid drafts and declaration-only interfaces.
- [ ] `FutureVersionNeverRewritten`, `MigrationFailurePreservesBytes`, `InterruptedSaveRetainsOriginal`, `BundleRoundTripPreservesWorkspace`, `WatchBindingAndActiveBlockRoundTrip`, `DuplicateIdsRejected`.
- [ ] Implement schema and backup flow; test read-only destination, failed backup, cancelled dialog and name collision using temporary directories.
- [ ] Reopen saved projects in app, inspect nested layout and bindings. Review actual fixture bytes and runtime equivalence.

## 5. Instruction-time observations and lifecycle

Owner controls/runtime, after 1; may develop tests independently of 2–4, serialize shared-file changes. Create `src/VirtualController/InstructionObservation.cs`, `InstructionObservationCollector.cs`; modify runtime/session/accepted-scan/completed records. Create `tests/InstructionObservationTests.cs` and `tests/ObservationLifecycleTests.cs`.

Stable-ID integration depends on task 3; independent test/collector work may start earlier, but task 5 cannot be accepted against the retired flat document identity model.

Interface: immutable observation/envelope fields and dispositions exactly as monitoring contract sections 3–7; collector records actual evaluation before/after and emits a bounded immutable envelope. No evaluator call from presentation. Source IDs from 3 persist; task/call frames from 7 enrich the same context contract.

- [ ] `LaterMoveDoesNotRewriteEarlierObservation`, `SameValueWriteIsStillWrite`, `FalseTimerStillEvaluates`, `SharedCounterShowsInstructionAndInstanceValues`, `ForceHasSeparatePublication`, `ResetRejectsLatePublication`, `WatchdogIsAbortedNotCompleted`.
- [ ] Instrument every enum/catalog family, normalize/write/fault paths and session phases. Update every successful-scan consumer to check explicit completion; preserve legacy watchdog output/plant effects as specified.
- [ ] Add catalog set equality, all unavailable states, identity reuse, deep immutable-history tests and overflow/context eviction tests. Unsupported oversized strings are negative language fixtures and separate presenter robustness fixtures.
- [ ] Monitoring on/off golden results must match outputs, timer/counter/edge state, task order, diagnostics and plant callback order. Review before UI integration.

## 6. Readable monitoring for every instruction

Owner editor/UX, after 3 and 5. Create `src/App/LadderInstructionPresentation.cs`; modify canvas and shell snapshot binding; tests `tests/InstructionPresentationTests.cs`; create `src/App/Main.EditorMonitoringReview.cs` with optional review entry in Main.cs.

Interface: `BuildPresentation(instruction, context, envelope)` returns typed rows/status/full-detail provenance plus size requirements; canvas uses a single measurement for drawing/hit-testing/scroll reveal. Both styles share data; no renderer computes execution results.

- [ ] Exact family coverage, negative/unavailable/zero/false/empty text distinction, configured versus instance preset, consumed versus final values, same-value/failed/forced write labels.
- [ ] Add task/path/instance selection, partial-scan badge, last invocation count and explicit truncation/capture-capacity indicators.
- [ ] Native rendered matrix for contacts, timers, counters, compare, math, text, calls/flow; no overlaps with conductors, headings or adjacent branches. Inspect screenshots, not only verifier exits.
- [ ] Run W1–W4 profiling and budgets in contract; record machine/workload/monitor-on-off measurements. Failed budgets stay open for optimization or reviewed adjustment.

## 7. Real parameterized FC/FB execution

Owner controls/runtime, after 3–5; split review between compile/schema and execution. Create `src/VirtualController/LadderCallBindings.cs`, `LadderCallFrames.cs`, `LadderInstanceStorage.cs`; modify program/compiler/JSON/document/runtime/index and migration fixtures. Tests `tests/ParameterizedCallTests.cs`.

Interfaces: explicit Legacy/Parameterized block/call mode; stable scoped symbol and instance IDs; formal-to-actual binding records; frame with sampled Input/local Output/Temp/resolved InOut; FB store keyed by declared instance identity. Binding resolution returns exact ultimate storage identity and type or a structured diagnostic. Runtime frame entry/exit handles sampling, normal copy-out and aborted unwind exactly as semantics contract.

- [ ] First review compiler/schema tests: complete/missing/duplicate bindings, exact types, FC Static rejection, unsupported aggregate formals, depth/recursion, instance type mismatch, Legacy preservation and migration round-trip.
- [ ] Then execution tests: Input sampling; FC initialization; FB Output/Static retention; separate instances; repeated calls; nested FC edge instance ancestry; shared FC path across tasks; forwarded writable alias rejection before effects; RETURN copy-out; aborted no-copyout with retained partial FB storage.
- [ ] Timer tests true/true, true/false/true, reset/true, two tasks and skipped periods prove once-per-scan quantum for new instance timers and unchanged legacy behavior. Stop clears/prime edges and applies specified timer policy.
- [ ] Extend observations with true frame/instance and transfer phases; verify selected instance cannot display another instance's values. No declaration initial value substituted for execution.
- [ ] Dot reviews semantics tests/diff before exposing parameter controls.

## 8. Call authoring and whole-editor integration

Owner editor/UX, after 7 and 2. Create `src/App/LadderCallEditor.cs`; modify shell block/interface properties and canvas call presentation; tests `tests/CallEditorTests.cs` and a native review in `Main.EditorMonitoringReview.cs`.

Interface: call form stages target, semantic mode, explicit instance and bindings, then commits one command. Mode conversion is explicit/undoable and never silently rewrites globals to locals. Interface editor shows supported sections/types and diagnostics.

- [ ] `LegacyConversionIsExplicit`, `MissingBindingNavigatesExactField`, `TwoInstancesRemainDistinctAfterReopen`, `CallFormCancelIsAtomic`, `InstanceRenamePreservesBindings`.
- [ ] Reconcile all 167 ledger rows to tests/evidence. Informational rows prove accurate navigation/content; excluded rows prove honest omission/boundary. All 155 required rows must pass applicable behavior/native gates; source handler existence is not a pass.
- [ ] Complete exported workflow: create tags and blocks, author nested logic and parameterized call, fix errors, load/run, monitor two instances, watch/override offline values, stop/reset, undo/redo, save/close/reopen and compare values/structure.

## 9. Final candidate and Dot acceptance

Root owns this task; no concurrent edits while source is frozen. Retain V25 candidate and current shortcut until the new candidate passes. Run project build and deterministic regression suites required by affected code plus app-shell/editor native reviews. Use existing `tools/build_offline_demo.ps1 -AllowDirty -DirectHeadlessExport -OutputDirectory <new-unique-path>` only after source checks, then run the same editor workflows against the export.

- [ ] Record exact source hashes, candidate manifest/hash, tool versions, test logs, profiles and screenshots together. Godot-generated UID changes require re-freeze before export qualification.
- [ ] Repeat applicable W1–W4 performance/capacity and native matrix checks after tasks 7–8 on this final candidate. Task 6 results are preliminary and cannot qualify subsequent CALL/editor changes.
- [ ] Root and independent reviewer inspect actual rendered pages at the contract's dimensions. Perform physical input workflows only with available authorized tooling; if blocked, retain pending status and do not mark full editor complete.
- [ ] Dot checks ledger completeness, rejected invalid cases, migrations, lifecycle truthfulness and regression evidence. No green summary if any required row remains pending/defective.
- [ ] Present completed evidence and remaining external limits to Matt. Update internal test shortcut only after candidate integrity and visual/behavior acceptance; no public release, signing or PLC commissioning claim.

## Studio meeting decision and dependencies

Controls audit requires instruction-time values and scoped storage; UX audit requires exact targets and real route behavior; persistence contract requires recursive tree and explicit migrations. Dot rejected the initial vague design and those objections were resolved into the companion contracts. Integration order above prevents UI from exposing parameter behavior before execution exists. Work may be parallel only where file ownership does not conflict; mandatory dependent gates remain sequential.

Completion means all required ledger functions and instruction coverage pass against one exported candidate. Finishing only command cleanup or readout styling is an intermediate milestone, not completion of Matt's whole-editor request.

