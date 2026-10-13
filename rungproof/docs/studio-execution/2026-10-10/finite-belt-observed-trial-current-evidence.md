# Finite-belt observed trial checkpoint

Status: PASS_WITH_LIMITS for this bounded milestone; overall studio work remains IN PROGRESS. Bounded implementation is covered by Matt's existing three-mode authorization. This checkpoint does not approve wider color/material rollout or complete the studio task.

## Verified so far

Root reports pure observer v3: 54 passing checks, including accepted-time holds at 20/50/100 ms, reset-home start bounds, wrong-result continuity and lifecycle invalidation. Build v3 passed with zero warnings/errors. Native v3 reached its terminal result with failures 0: four enabled/disabled actual plant trace pairs at 20/40 ms. The controller suite v1 passed 164 checks with 0 failures and no physical PLC transport.

The observer receives copied scalar accepted input/output and measured plant observations. It owns no clock, command writer or automatic correction. Intended behavior requires real request, released travel, physical beam transition, exactly the next accepted feedback response and at least 0.5 accepted seconds of supported stationary hold. Valid wrong logic remains executable and can produce a needs-attention observation.

## Preserved failures and corrections

Native v1 had two failures: terminal NeedsAttention retained an old LastScan/LastTime, so subsequent accepted observations appeared discontinuous. Terminal results now track accepted history without replacing the diagnosis; identity/force/binding checks still invalidate it. The original failure evidence remains historical.

The first operator capture v1 was rejected for clipping. The card was reordered with the result first. Root personally inspected the intended objective v2 view: it fits the normal operator rail after the actual SceneIO table is collapsed and the real hint button remains available. This is a bounded inspection, not final visual qualification.

## State limits

Manual Stop before completion produces Incomplete, including a wrong trial; it does not retain a needs-attention diagnosis as a current running trial. Stop after Observed retains that observation as historical. Reset, changed program/layout, project replacement, forces and unavailable bindings invalidate current eligibility. A timeout means no completed observed trial, not a general verdict that the learner's program is wrong. Pure fixtures do not substitute for native plant trace equivalence.

## Final bounded evidence

Final evidence record: `.tools/studio-verification/finite-belt-observer-final-evidence.json`. Current compiled assembly SHA256: `AE6905D5AC565C1172FC8CDF5897DAA27C60C5EBB74402332752E000661DA0CE`. Build v4 passed with zero warnings/errors. Native v4 completed with failures 0, including completed trial translation, Undo and joint reopen coverage at 20/40 ms. Root rechecked a scoped 430-file source freeze.

Four capture-v3 PNGs use that exact assembly: normal default intended view, intended hint view, valid wrong terminal view and invalid current view. Root personally inspected all four. Individual image and program/draft/accepted-state identities are retained in the final evidence record. Dot personally inspected all four exact-assembly frames and returned PASS_WITH_LIMITS in `finite-belt-observer-dot-v3.md` and `.json`. Default primary result, identity and objective are visible. Optional hint/timeout content below the fold is accessible by scrolling; no hard visual blocker was found.

Pure v3's 54 checks and controller suite v1's 164 PASS/0 FAIL remain separately scoped source-test executions. The controller suite ran before the subsequent QA-only lifecycle coverage addition; it is not falsely labeled a v4 execution. Earlier finite adapter evidence on `1FCBA145` and indexed diagnostics on `F5901A18` retain separate identities and coverage.

No wider two-room color/material propagation, current whole-catalog render qualification, arbitrary plant graph support, live PLC qualification or overall studio completion is approved here.

Root final recheck: scoped430-file source freeze has zero drift; compiled DLL unchanged; final logs have no ERROR matches. These bounded checks do not approve broader material rollout or overall completion.
