# Dot independent design review — TIA-side editor

Date: 2026-10-11. Decision: **revise before implementation authorization**. The overall direction is sound; this is a preimplementation design review, not runtime acceptance, a TIA parity finding, or authorization to implement.

Reviewed `tia-editor-upgrade-design.md`, `tia-editor-independent-audit.md` including its command coverage appendix, and `instruction-monitoring-full-audit.md`. Read repository instructions and the PLC/TIA skill; spot-checked editable CALL representation and runtime CALL/RETURN dispatch. Existing dirty work was preserved. No product code edits, build, executable launch, UI interaction, or PLC connection occurred.

## Required corrections

### 1. Define completion against the user's whole-editor requirement

The proposed phases are sensible, but “intentionally unavailable with explanation” cannot count as fulfilling an exposed working function. Split the ledger into (a) required final capability, (b) supported informational content, and (c) explicitly excluded capability. A disabled control may explain a temporary phase boundary; it is not evidence that the requested function works. Parameterized calls and nested authoring are described as required later workstreams: retain them as mandatory final gates unless Matt explicitly narrows scope. Do not announce the whole redesign complete after phases 1–3.

Before implementation, produce the actual finite command inventory, not only a requirement to produce it. Include every visible entry route, with source handler and proposed behavior. Derive instruction coverage from enum/catalog membership and compare it with the acceptance table so newly exposed entries cannot escape review.

### 2. Bring the audit's concrete defects into the design and plan

The architecture presently leaves several confirmed inconsistencies implicit. Add explicit repair/acceptance rows for: network deletion choosing an arbitrary final contact; generic output Copy copying a whole network; Add BOOL retaining the previously selected type; instruction-tree coil activation only focusing a selector; aggregate watch declaration navigation; hardcoded diagnostics; Add block/task ignoring the entered name; and enabled blank-name operations returning silently. Also cover diagnostic navigation to the exact instruction/operand and opening panels when their parent dock is collapsed.

For each, decide the final user-visible contract before coding. Changing a label is sufficient only when that label accurately describes an intentionally supported operation; relabeling an absent required action is not semantic completion.

### 3. Specify CALL semantics before authorizing the semantic phase

The design correctly rejects declaration values as live values. However, “supported aliasing” and “reset/retention” are unresolved decisions, not an executable contract. Runtime currently queues target networks and uses a sentinel for RETURN; a call path observation does not itself create invocation-local storage.

Require a reviewed table covering valid block kinds/targets, formal type and direction rules, missing/duplicate actuals, Input sampling, Output copy-out timing, InOut aliasing, output behavior on skipped/aborted calls, FC Temp initialization, FB Static lifetime, instance identity, reset/reload behavior, recursion/reentrancy policy, and aggregate restrictions. Include the case where two Output/InOut actuals name the same global and where nested calls share an instance. Rejection with a precise compiler diagnostic is an acceptable defined subset; silent aliasing or a fallback to globals is not. Freeze this table and migration schema before enabling parameter controls.

### 4. Make monitoring identity and publication explicit

“Scan identity” must include loaded program identity and execution generation, because reset/load can reuse a scan number. Add snapshot revision/lifecycle provenance for force and Stop publications without scans. Specify whether watchdog/fault-aborted scans expose partial observations, and make that state distinguishable from a fully accepted scan. Label counts as counts within the captured scan, not lifetime counts.

An instruction ID plus last invocation is acceptable only if its task/block/call path is shown or reachable; it cannot be represented as values for a currently selected FB instance unless the identity matches. Distinguish skipped action from false enable where false TON/TOF/OTE evaluation still mutates state. Define write disposition for failed arithmetic, successful same-value writes, forced outputs, and formatting faults that clear a destination. A presenter must not turn missing observations into zero/FALSE or infer write success from enable.

### 5. Preserve semantics and saved projects with explicit migrations

Versioned migration needs concrete source/target schema versions, fixtures and failure behavior. Old target-only calls must retain their shared-global semantics, even when a block has declaration-only interface metadata. Do not automatically activate old declarations as parameters.

For nested branches, specify the editable tree and legal output placement before changing renderer/commands. Verify old flat networks, nested runtime imports, empty branches and unsupported tree shapes. If a shape cannot round-trip, reject it with an actionable message rather than flattening it. Stable IDs must survive save/reopen and unrelated edits; pasted subtrees must receive fresh IDs throughout. Include project/workspace bundles, scene I/O bindings, watch selections, active block and instruction references in round-trip fixtures. Migration failure must leave the original file intact; unsupported future versions must not be rewritten as an older format.

### 6. Resolve transaction and cancellation semantics

The proposed “Escape cancels without edits” conflicts with the audit finding that properties currently mutate on selection/submission. Define which surfaces are live-editing and which stage edits. For the new inline editor and multi-field properties, specify commit boundary, invalid draft handling, one-operation undo grouping, focus-loss behavior, and cancellation after tag creation. Creating an undefined tag and assigning it should either be one atomic history transaction or an explicitly described pair of operations.

A stale menu target should fail harmlessly and explain why; do not retarget the command to whatever is currently selected. Include stale revision, deleted target, block switch, undo/redo, and mutation-lock changes between menu opening and activation in tests.

### 7. Turn feasibility and native acceptance into measurable gates

The plan names performance profiling but no pass criterion. Record representative large projects, scan/task/call rates, observation capacity, retained-history count and worst-case text sizes; choose explicit memory, allocation, execution and render budgets before accepting instrumentation. Truncation/eviction must be visible and must not silently change last-invocation meaning. Monitoring should not change simulated execution order or outcomes.

Name the smaller supported viewport, zoom range and DPI cases rather than leaving them open. Require both renderers, actual pointer/keyboard event routing, long operands/text, dense nested branches, edge menus, panel reopening, and saved/reopened workflows. No-focus captures establish rendered appearance only; they cannot close focus, keyboard, drag/drop or menu interaction acceptance. Keep physical input tests pending until permitted. Record final candidate identity, test results and captures together so source tests cannot qualify a different exported executable.

## What is already sound

Reuse of recursive runtime nodes, shared command predicates, stable selection targets, instruction-time observations without reevaluation, explicit snapshot versus consumed values, truthful interface disclosure, and shared presentation semantics are appropriate foundations. Preserve these. The design's scope exclusions and separate physical/native/release gates are appropriate; editor success does not confer Siemens instruction parity or machine commissioning approval.

## Approval path

Root should incorporate the corrections, publish the command/capability ledger and concrete file-level plan, obtain the requested studio review, then present that reviewable scope to Matt for final implementation authorization. CALL and nested-tree contracts can be detailed as separate reviewed design packets, but their mandatory end-state must remain visible in the overall completion criteria. This review authorizes no implementation and closes no runtime or native acceptance gate.

## Concurrent design update verified

After the first review draft, root added EDIT-01 through EDIT-08 to the design. I verified those rows are present and cover wire targeting, explicit deletion, copy labeling/semantics, coil insertion, BOOL creation, aggregate watch navigation, device/diagnostics truthfulness and named block/task creation. This satisfies the explicit defect-inclusion portion of correction 2. The remaining transaction, whole-editor inventory, exact diagnostic navigation, collapsed-panel and semantic-contract requirements above still apply. These new design rows are commitments, not tested fixes.

## Semantics-contract follow-up — 2026-10-11

Reviewed `tia-editor-semantics-contract.md`. It substantially resolves the previous scope, migration, tree preservation, transactional editing and native-matrix concerns. Architectural approval is the current gate. Per root's clarification of the active brainstorming workflow, the file-level implementation plan follows Matt's written-spec approval; its absence is not a missing artifact for this architectural review. The earlier Approval path paragraph is superseded on that sequencing point. No implementation is authorized by this review.

Remaining corrections before accepting this semantics contract:

1. **Stop/edge contradiction (source-confirmed).** Lifecycle says Stop/start preserves edge memory except output/timer behavior. Current `VirtualControllerRuntime.Stop()` explicitly clears `_edgeInputs` (line 179); contact evaluation establishes its first baseline without a pulse after Stop/Reset (lines 674–685). Preserve this for Legacy mode. Explicitly decide parameterized edge Stop/Run and first-load/reset baseline behavior, including the first invocation when an input is already TRUE. Tests must cover skipped-before-first-call, false-to-true while skipped, Stop then Run, and Reset then Run. Do not quietly change old edge behavior while adding scoped storage.

2. **Repeated-call timer advancement (source-confirmed ambiguity).** `EvaluateTimer` advances by `ScanPeriod` per invocation (lines 599–605). The contract permits sequential same-instance calls, cross-task instance sharing and global timers inside FCs. Specify whether the new parameterized language advances a shared timer once per scan, per invocation, or from a monotonic simulation clock, including non-due/conditionally skipped calls and call–reset–call sequences. Preserve Legacy execution separately. Two calls in one scan must not acquire accidental undocumented timing semantics; exact elapsed-value tests are required, not only instance separation tests.

3. **Nested FC edge memory needs owning instance context.** A static call-site path plus node ID alone can collapse the FC state reached from two instances of the same FB. State keys need an explicit rule including enclosing FB instance identity and the FC call-site chain. Also decide whether the same top-level FC call site executed by two tasks intentionally shares state. State the choice in help and tests. New-frame allocation must not reset this persistent memory, and dynamic invocation ordinal must not create unbounded new state keys.

4. **Aborted FB Output retention is incomplete.** The contract says FB Output persists and local output writes happen before copy-out, but abort language only lists global/InOut and FB Static writes as non-rolled-back. State explicitly whether partially written FB Output storage survives abort and is copied by a later successful invocation that does not overwrite it. Either choice needs a test; leaving it implicit can unexpectedly publish a value from an aborted call later. Clarify whether compiler failure/load rejection preserves existing live instance state (expected from the current no-replacement promise).

5. **Prevent aliases that change as frames execute.** Exact actual identity checks are good. Define Output/InOut binding destinations as stable resolved references captured at entry, including caller-local and forwarded aliases, and define alias detection after following InOut references. Otherwise two differently named formals forwarded by a nested call could pass a textual-name check while targeting the same storage. Specify whether caller Temp/Output actuals are allowed and ensure their references cannot outlive the frame. No dynamic indexing is necessary for this subset.

6. **Make the final action model exhaustive.** Recursive contract allows at most one terminal action and rejects no-action drafts at Verify + Load. Verify this remains correct for current LABEL, RETURN and jump networks and legacy imports; describe these as terminal-action variants explicitly. A shape restriction must not render a previously executable legacy control-flow network unloadable. Distinguish duplicate IDs across the whole program from duplicates only within one tree, since observations/reference navigation use IDs.

The new output-only clipboard decision, non-overwriting migration backup, alias restrictions, explicit Legacy mode, no-distribution tree policy and staged edit transactions are strong corrections. The remaining points are language-contract choices and regression requirements, not a request to emulate undocumented Siemens behavior. Final combined review remains pending the command ledger and monitoring contract.

## Combined architectural review — revision 2

Read the updated main design, semantics contract, monitoring contract and 156-row command ledger. **Decision: architecture is suitable for written-spec approval after the ledger contradictions below are reconciled; implementation and acceptance remain unauthorized/unproven.** No additional runtime redesign is required by this review.

The six semantics findings above are substantively resolved: Stop establishes a fresh edge baseline; FC keys carry enclosing FB instance ancestry; new FB timer time is capped to one quantum per scan while legacy global timing is unchanged; aborted FB Output storage retains internally without claiming actual copy-out; forwarded aliases resolve to ultimate storage; terminal actions and program-wide identities are explicit. The timer rule is acceptable as a clearly labeled discrete offline language policy. Preserve exact call–reset–call and periodic-task tests because the policy intentionally differs from wall-clock timers.

The monitoring contract now adequately defines loaded identity, generation, attempt and publication separation; aborted versus completed work; write dispositions; exact selected context or explicit missing capture; set-based catalog coverage; bounded capture/history and proposed performance budgets. MON-ABORT-01 is an appropriate classification correction provided all success-assuming consumers are migrated and legacy output/plant effects remain preserved as promised. Calling reset-to-default BOOL/numeric values “safe” is not a physical safety claim; final UI should say default/fault output values.

### Exact ledger reconciliations required

| Row | Conflict | Required correction |
|---|---|---|
| X03 | Parameterized FB calls/instance storage is excluded | Mark supported parameterized FC/FB calls and explicit instance storage required under the semantics contract. Multi-instance declarations and arbitrary DB semantics remain excluded. |
| X04 | Nested authoring is excluded | Separate required recursive Series/Parallel authoring within depth/terminal-action limits from excluded arbitrary graph/cross-wire connectivity. |
| P12/P13 | Final interface contract remains metadata-only | State Legacy declarations remain inert; Parameterized declarations bind and execute under the reviewed CALL contract. Both need round-trip and execution tests. |
| E03 | Final copy contract only asks accurate labeling | Require output-only clipboard plus distinct Copy network, matching EDIT-03 and semantics. |
| T01 | Rename-menu alternative remains | Require BOOL creation initialized without overwriting the selected existing tag, matching EDIT-05. |
| P09/P16 | Default-name disclosure remains an alternative | Require validated entered name on creation, matching EDIT-08. |
| C04 | Network delete may remain disabled merely with a reason | Require explicit network deletion where the selected target is legally deletable; disable only for a defined genuine constraint. |
| K03 | F2 behavior unspecified | Require selected operand inline editing, with defined unavailable reason for targets with no editable operand. |
| Q01/Q10/Q11 and property rows | Final commit behavior left generic | Reference staged transaction contract explicitly: Enter/Apply commits, focus loss remains pending, Escape/Cancel discards. |

Also add rows for newly required routes absent from the current-surface inventory: Save As, Cut, keyboard context menu, inline create-and-assign, output-only clipboard and recursive subtree operations, plus parameterized call binding/instance authoring. These can be grouped rows with explicit route aliases; the ledger need not preserve the number 156. Source-handler columns for proposed routes must say new/proposed, never imply current implementation. This makes the ledger a final-scope ledger as its introduction promises.

One small semantic clarification remains advisable: explicitly say whether a top-level FC's same static call-site path reached by two serial tasks intentionally shares edge memory (the currently specified key implies yes). Keep TaskId in observation provenance regardless. Record this policy in the contract and test it rather than allowing implementers to decide accidentally.

After these documentation reconciliations, no known architectural blocker remains from this review. Matt's written-spec decision comes next; the concrete file-level plan and its authorization remain later gates. No build, product edit, application launch or native acceptance occurred during this combined review.

## Final preimplementation studio decision

**APPROVE FOR USER REVIEW — spec and implementation plan, not implementation authorization.** Independently reread the actual updated files: main design, semantics contract, monitoring contract, command ledger and new nine-phase implementation plan. Counted the ledger from its rows: **167 total; 155 required, 8 informational, 4 excluded**. Verified the previously conflicting rows now require the definitive behavior, proposed routes are present, and FC cross-task edge-state sharing is explicit. The main decision gate correctly asks Matt to review spec and plan together for final authorization.

The user's explicit complete-plan-before-final-authorization workflow takes precedence over the earlier intermediate written-spec gate. The earlier sequencing comments in this review are superseded accordingly. Preparing this plan is not permission to execute it.

The plan identifies concrete files, proposed interfaces, dependent phases, regression cases, single-writer ownership, independent review, migration protection and final exported-candidate evidence. It retains mandatory executable calls and nested authoring, rather than stopping after visual improvements. No known design blocker remains for presenting the complete packet to Matt.

Execution clarifications to carry into task evidence (not additional approval gates):

- Phase 6 profiling is preliminary because phases 7–8 add scoped calls, transfer observations and authoring. Repeat applicable W1–W4/performance and native tests after those changes against the final frozen candidate; final evidence cannot reuse preliminary timing as if unchanged.
- Phase 5 can develop tests before the recursive document finishes, but integrate against the stable IDs produced by phase 3 before declaring observation identity acceptance. Shared file ownership remains sequential.
- The main design is authoritative for scope, semantics/monitoring contracts for behavior, and ledger for per-route accounting. If implementation reveals a contradiction, reconcile documents and tests explicitly; do not silently choose the easier acceptance rule.

This approval concerns the reviewability and coherence of a proposed plan. No implementation, build, benchmark, product launch, physical input test, export acceptance, release approval or TIA parity is asserted. Existing code findings remain defects until their planned tests and native evidence pass.
