# Task 6 independent rereview

2026-10-11. **APPROVE the scoped review corrections for continued integration.** No new blocking defect found in this rereview. This is not approval of the entire editor, final candidate, performance budgets, or complete native interaction matrix.

Reviewed current presenter, canvas monitoring, shell eligibility/attachment integration, history selectors, selection bounds, real-runtime presenter tests, and `tia-editor-task6-implementation-evidence.md`. Read existing `editor-monitoring-06.log` and `counter-render-06.log`; did not run builds/tests or control the UI. Parent independently inspected the actual GPU images; visual acceptance here relies on that parent evidence rather than a new reviewer inspection.

## Prior findings

1. **Context-dependent indication — corrected.** `IsElementEnergized` now delegates to `ObservationIsEnergized`. That uses the same identity/eligibility-checked presentation and selected exact context as text; unavailable contexts, failed writes and lifecycle-only retained observations cannot inherit global highlight. Existing native log reports opposing-context and shared-counter checks passing in both styles.

2. **Draft eligibility reasons — corrected.** Shell retains the envelope separately from explicit NoProgramLoaded/DraftMismatch/InvalidDraft eligibility. Canvas binds the reason into the pure presenter instead of reducing every failure to a null snapshot. New attachment clears the old publication; snapshot binding clears pending publications. Shell verification exercises executable mismatch, invalid operand, equivalent restore, watch-only edit, reset and invalid load identity.

3. **Inline size and fallback arity — corrected within the bounded counter checkpoint.** Counters now use a 320×126 rectangle and three compact rows separating CV, configured PV, instance PV, input and done. Terminal drawing, hit-testing and selection reveal share bounds. Numeric fallback delegates B/C applicability to the executable arity rules and uses MIN/VALUE/MAX labels. Parent reports both full-shell counter images fit; log confirms 54 checks without failures. Full family/zoom/DPI viewport acceptance remains open.

4. **Historical instruction inspection — corrected.** Historical instruction/context selectors bind to the selected retained publication, with explicit historical status and no live selection mutation. A later scan does not change the selected record. Eviction disables selectors and requires explicit reselection. Shell verifier asserts retained FALSE consumption after a later TRUE scan and explicit eviction.

5. **Real family coverage beyond synthetic keys — corrected at source/test level.** Additional tests execute real runtime programs covering the catalog set, assert consumed identities/values and exact numeric arity, timer/counter state, false-enable effects, FORMAT_TEXT failure-clears, and actual parameterized FB copy-out/instance metadata. Synthetic registry checks remain supplementary. Runtime semantics and complete CALL authoring remain separately reviewed work.

## Publication cadence and lifecycle

Completed scans queue entire immutable publications; `_Process` applies the newest pending envelope after the 33,334 µs interval. Presentation cache invalidates when applying an envelope or changing selected context. Lifecycle publications and unavailable eligibility bypass completed-scan coalescing, and applying them clears the pending envelope. `SetMonitorSnapshot` also clears pending state, preventing a prior queued scan from reappearing after detachment/reset-style snapshot binding. This preserves a coherent displayed scan rather than mixing rows from different scans. Existing cadence log reports 370 publications and nine refreshes over 285,815 µs. This bounded result is not a proof of the final performance budgets.

## Remaining acceptance boundaries

- Physical keyboard/pointer context-selector, history and selection-reveal flows, plus all required viewport/DPI/zoom combinations.
- W1–W4 overhead, allocation and render-budget qualification; previously reported budget failures are not waived.
- Large visible graphs reaching the 200-instruction render cap need an explicit user-facing indication and continued selection access verified in final stress acceptance; the current two-visible-box culling fixture does not exercise that limit.
- Scoped-instance final-value visibility and final Task 7/8 integration, followed by frozen candidate export verification.
