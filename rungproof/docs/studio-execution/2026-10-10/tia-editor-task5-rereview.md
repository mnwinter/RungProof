# Task 5 scoped independent rereview

2026-10-11. Static review of the five findings in `tia-editor-task5-code-review.md`, their current implementation, and new regression tests. No builds, tests, UI actions, or product edits performed by this reviewer. Root reports integration-10 passed 215 tests, including 28 Task 5 tests; that execution was not independently repeated here.

## Verdict

The five original defects have substantive fixes and targeted regression coverage. Two remaining lifecycle/publication defects prevent unconditional acceptance of the publication foundation. These are concrete synchronous callback cases, not a demand for multithreaded session support.

## Remaining findings

1. **P2 — A lifecycle change in ScanAttempted can publish the old envelope after the new lifecycle envelope.** `src/VirtualController/VirtualControllerSession.cs:314-315` invokes `ScanAttempted(publication)` and then unconditionally invokes `PublicationPublished(publication)`. A ScanAttempted subscriber calling Reset synchronously emits the newer Reset publication first; after it returns, the outer call emits the old ScanCompleted publication. The active `Publication` property remains Reset, but event consumers see revisions out of order and can restore pre-reset observations. The subsequent ScanCompleted/SnapshotPublished calls already guard invalidation; PublicationPublished must receive the same protection. Test a one-shot Reset/Stop callback from ScanAttempted and assert no older runtime envelope follows the lifecycle event, current publication remains the lifecycle result, and no successful-scan callback fires afterward. In the failure branch (`329-330`), capture the failure envelope locally and avoid republishing the mutable Publication property after a callback changes it; currently this can duplicate the Reset event instead of publishing the failure envelope. Attempt disposition delivery can remain independent from active publication delivery.

2. **P2 — Force changes inside an in-flight callback do not invalidate the old publication image.** `VirtualControllerSession.cs:119-140` publishes ForceChanged without incrementing the private lifecycle invalidation token; `218` checks only that token. For example, call `SetBoolForce("output", false)` from the BOOL commit callback after a program wrote TRUE. Runtime Snapshot and the immediate ForceChanged publication now contain FALSE, but the outer cycle continues using the immutable pre-force `executed` snapshot, overwrites the active Publication with TRUE at `307`, and emits TRUE to SnapshotPublished at `317`. Consumers can therefore display a value inconsistent with the actual runtime's forced output. This also affects a force change in ScanAttempted or PublicationPublished. Invalidate the in-flight publication when forces change (without changing ExecutionGeneration or attempt identity), or explicitly serialize/defer force mutation through a documented boundary. Add Set/Remove/Clear force callback cases and assert the final active envelope and snapshot events cannot supersede the newer force state with stale values. Preserve the actual already-completed output/plant phase evidence; do not imply rollback.

## Original finding disposition

| Original finding | Inspection result |
|---|---|
| Aborted scans delivered as successful | Fixed: successful-only ScanCompleted, separate disposition-carrying ScanAttempted; watchdog retained in history; commit/plant sequence preserved. `WatchdogIsAbortedNotCompleted` asserts events, counts and history. |
| Interrupted attempts disappear | Fixed for tested pre-publication callback phases: original identity captured before callbacks, independent AbandonedAttempts retention, completed phases recorded before invalidation checks, active Reset preserved. `InterruptedAttemptsRetainOriginalIdentityAtEachPhase` covers input, numeric commit, plant and post-input. Publication callback cases above remain. |
| Failed phases lose images or are mislabeled | Fixed: explicit sampling/Program/commit/plant phases, hoisted captured images, per-type CommitProgress, unavailable-image reasons. `EachFailurePhasePreservesAvailableImages` and partial commit tests cover all listed operational phases. Subscriber exceptions deliberately propagate after disposition is final; they do not retroactively reclassify an already recorded execution. |
| Missing per-node upstream continuity | Fixed: series passes incoming continuity AND preceding local aggregate; parallel passes the same incoming continuity to each branch. Evaluation still visits all children, records consumed values and preserves local result separately. FALSE-to-TRUE nested fixture exercises this distinction. |
| Watchdog progress depends on observation capacity | Fixed: independent completed-network count and progress ID/ordinal updated even when capture is disabled or capacity is exceeded. Count increments only after executed network completion. Tests cover both cases. Network IDs are observable container IDs and may be the last completed observation. |

## Bounded acceptance

This rereview does not close the earlier exact-family/status, canonical content identity, invocation, capacity/saturation, measured allocation/latency, parameterized CALL, presenter, or native acceptance gates. The collector's fixed record limits and retained-payload estimate do not establish measured performance budgets. The abandoned and ordinary history collections each have a 128-entry cap and share the byte budget; whether the product exposes them as one 128-envelope history should be resolved during history UI integration.

## Second scoped rereview: callback fixes

2026-10-11. Reviewed the subsequent frozen session source and the three new lifecycle regression tests. Root reports integration-11 passed 223 tests; this reviewer did not run builds/tests or native actions.

**Both remaining P2 findings are closed at source review. No new blocker was found in this scoped change.**

- Attempt completion now guards active publication, successful completion and snapshot delivery after the callback boundary. The failure envelope is captured locally, retained with its own identity, and is not replaced by a newer mutable Publication property when notifying ScanAttempted. Reset/Stop regressions explicitly assert only the new lifecycle publication survives.
- Set/Remove/Clear force increment the private invalidation token after successful runtime mutation. ExecutionGeneration is unchanged. Pre-publication interruption preserves the old attempt's phase/commit evidence in AbandonedAttempts while retaining ForceChanged as active. The test matrix covers all three force operations during commit, ScanAttempted and PublicationPublished.
- NotifyPublication, NotifyCompleted and NotifySnapshot check the exact active envelope before each subscriber. This also prevents an early subscriber's nested lifecycle change from delivering the stale envelope to later subscribers. Lifecycle methods retain their own local envelope/snapshot for these guards.
- ScanAttempted remains an immutable attempt-disposition event and is intentionally independent of active-image delivery. Already recorded completion is not retroactively rewritten by a later publication callback, while successful-scan notification is suppressed after invalidation. Operational callback failures still preserve completed phase evidence and propagate the exception.

This closes the five original findings plus the two callback follow-ups for the Task 5 foundation only. The broader contract/performance/native gates in the preceding Bounded acceptance section remain open. No whole-editor or release approval is implied.
