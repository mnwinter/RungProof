# Hard-issue evidence index

Current routing index, not a repair plan. Application/source unchanged. Native observations, source/model explanations and QA-invalid attempts are separate. Read the exact later native review when an older long report disagrees.

## Confirmed current findings

| Finding | Current exact scope | Primary evidence |
|---|---|---|
| QA-PLANT-001 / parcel Reset repeat | V7 fresh-load normal3 receipts PASS; actual phase30/count0 AppStop/Run-alone recovery PASS. Two same-scene Reset repeats stallcount1: reliability FAIL. No universal normal-impossible claim. | [Controlled comparison](parcel-v7-controlled-comparison-review.md), [handoff recovery](parcel-v7-handoff-recovery-review.md), [first normal](parcel-v7-native-review.md) |
| QA-UI-002 diagnostic footer | Valid authored runtime executes and ErrorList clears, outer RUNBLOCKED persists. Actual native repro done; no new closure. | [Exact diagnostics review](../screen-audit-2026-10-21/postbackup-diagnostics-review.md) |

## Source/model causal material — not native acceptance

- [Hard-issue model index](hard-issue-model-case-index.json):52 recorded model cases, grouped baseline:12, scan-sweep:4, meter:8, class-meter:5, retentive-meter:15, late-stop:8. `nativeAcceptance` is false for these diagnostics; model success is not a physical walkthrough.
- [Source provenance](hard-issue-analysis-provenance.json): sourceCommit `d0c4566bc956706ab3430e25d4142b03f4431e9c`; applicationChanges=False; nativeAcceptance=False.
- [Parcel reset decision table](parcel-sensor-reset-decision-table.md) and [causal analysis](hard-issue-analysis.md) explain transform/AABB initialization and admission alternatives. Native fresh-scene versus reset comparison supports history dependence but does not uniquely prove the stored geometry or a verified fix.

## Candidates and route questions

| Candidate | Disposition / evidence route |
|---|---|
| Radar inspection ghost view | Advertised action lacks grounded equipment mapping; body/transmitter attempts remainFalse/opaque. Contract/accessibility candidate, not failed arbitrary click or N/A. [Route analysis](native-test-queue.md) |
| Gallery empty-belt motion | Featureless surface inconclusive; mapped CNC/fan/motor responses reviewed. Not a confirmed dead control. [Gallery review](../screen-audit-2026-10-21/postbackup-small-gallery-review.md) |
| Five legacy declared Stop/Reset routes | Physical START responses supported; separate declared actions have no mapped equipment route and generic action bank is hidden. Explicit route/documentation question, not five invented button tests. [All77 action inventory](native-test-queue.json) |
| Earlier Discard/cache navigation | Never confirmed; representative Cancel/Discard/Save/reopen routes now reviewed. Do not resurrect as a callback defect without a new failed transition. [Navigation](../screen-audit-2026-10-21/postbackup-small-navigation-review.md) |
| Stop wording / curriculum marker / default framing | Specification or presentation candidates remain separate from successful actual recovery; use original issue history for exact scope. [Baseline issue history](issue-reconciliation.md) |

## Excluded QA observations / historical cases

- Earlier V7 handoff probe sent no Stop because OCR read1 asI. Not an application defect; later actual phase30 Stop/recovery is reviewed.
- The immediate latest Stop frame still had the open menu/RUNNING; acceptedStop proof is later settled STOPPED1333 plus paired physicalhold, not that transitional image.
- Earlier fresh-reload-leave filename still showed2.23; actual away-scene proof is2.25 behind the later selection menu.
- Old parcelV3/V4 stalls and firstV7normal/failedReset attempts remain valid controller-specific histories; they are not erased or treated as all-controller impossibility.
- Wrong coordinates/DPI, invalid fixture schemas/DB collisions, missed transient OCR and filename-only completion expectations are QA limitations, not new product failures. [Queue corrections](native-test-queue.md), [baseline issue history](issue-reconciliation.md).

## Current finite native queue

12 READY cases. Q01/Q04/Q06/Q07/Q20/Q22 scoped CLOSED. Q23 Stop/recovery CLOSED while Reset/repeat FAIL remains recorded.

| Case | Scene | Actual UI control / observation |
|---|---|---|
|Q02|lab-4-08-package-grouping|APP STOP/RUN during actual release; ENABLE/CLEAR/LOAD for setup|
|Q03|lab-4-12-cable-cut-length|APP STOP/RUN plus physical START while knife stroke active|
|Q05|lab-5-09-bag-indexing-conveyor|PAUSE BLOCKED/CLEAR during forward travel|
|Q08|lab-11-12-mobile-traffic-lights|Controller READY while actual vehicle occupies crossing|
|Q09|lab-9-11-pallet-counting|APP STOP/RUN and full second batch using NEW PALLET|
|Q10|lab-11-13-xy-palletizing|ENABLE OFF/ON during actual gantry motion|
|Q11|scene-2-conveyor-pusher|ENABLE OFF/ON; observe actual carton destinations|
|Q12|lab-2-11-inbound-tote-stop|Physical START; observe scanner phase|
|Q18|lab-2-18-pallet-pickup|Manual JOG at actual completed pickup|
|Q19|lab-4-09-chain-drive-lift|APP STOP/RUN plus START during actual vertical lift|
|Q21|lab-5-07-pedestrian-crossing|CROSS during actual WALK|
|Q24|lab-9-03-sum-and-counter-function|A NEXT numeric_display_0; B NEXT numeric_display_1|

Exact procedures/fixtures: [queue](native-test-queue.md), [JSON](native-test-queue.json). Current matrix:34 non-SCOPED_PASS cells, including failures and route/visual questions—not34 extra tests. [Matrix](coverage-reconciliation.md), [finite cells](remaining-finite-cells.json).

Newest closure: [coating initial-index Stop/Run-alone](coating-index-stop-review.md). Public index/bothReady/ventFalse plus physical movingbody qualifies the phase; no exact timing/privatephase test is added. Other recent closures: [Demo2](demo2-appstop-review.md), [activeTP](timed-lamp-active-retrigger-review.md), [markerReset](marker-inhibit-reset-review.md). Earlier requests remain historical JSON supersessions.

No final remediation plan or whole-audit acceptance asserted.
