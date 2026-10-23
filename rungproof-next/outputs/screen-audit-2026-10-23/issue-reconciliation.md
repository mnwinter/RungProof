# Confirmed-issue reconciliation — 2026-10-23

Initial read-only reconciliation used canonical main d0c4566b. Later scoped native results are linked below; the application and original evidence remain unchanged. Current sources include independent native reviews and current help text. This report is an evidence index, not a newly repeated global walkthrough. Use `hard-issue-index.md`, `issue-scope-reconciliation.md` and `native-test-queue.json` for later closures when a historical section disagrees.

**Current confirmed functional/presentation defects: two deduplicated issues.** There is also a still-current two-entry documentation/interaction contradiction (QA-SPEC-002), kept separate from faulty PLC/input execution. Remaining untested cases and candidates are not defects. No final remediation plan is selected.

## Current confirmed issues

### QA-PLANT-001 / parcel subcase — Reset/repeat stalls (S2, high; repeatability blocker)

Affected entry: `lab-2-23-parcel-sorter` only for this remaining subcase. Retain the original shared ID; do not create separate count1/count2 issues or merge the repaired START-binding problem into this failure.

**Current supersession:** frozen diagnostic V7 completes three supported deliveries on fresh scene reconstruction, observed twice. The same V7 after actual AppReset/Run/physical START stalls at count1 twice; native Watch identifies medium outbound phase50/class2. Moving-handoff phase30/class2/count0 AppStop and Run-alone recovery also complete successfully. Thus normal completion is possible, while Reset repeat is a demonstrated failure. See [controlled comparison](parcel-v7-controlled-comparison-review.md), [handoff recovery](parcel-v7-handoff-recovery-review.md) and [first normal](parcel-v7-native-review.md). Expected: Reset restores an equivalent initialized plant for the same scene/program. A [source-supported transform/AABB ordering hazard](parcel-sensor-reset-decision-table.md) and static geometry counterexample explain a candidate cause; native private dimensions and a repaired build have not been observed.

Historical V3/V4 outcomes below remain valid only for those exact learner controllers; they do not establish universal normal-cycle impossibility:

- Repro: normal GUI scene selection, open valid V3 learner fixture, Verify/Load/Run, physical START. V3 stalls after one receiver. Repeat separately with versioned V4 diagnostic learner fixture (admission belt stops immediately at measured center); physical START reaches two receivers, then stalls despite thousands of advancing scans.
- Expected: explicit learner admission/index/transfer commands move all3actual parcels to the3correct receivers; all_parcels_received/cycle_complete becomeTRUE. This expectation belongs to the loaded documented fixture/plant contract, not an invented continuous throughput requirement.
- Actual V4: native scan4187 after the50s observation shows sorted_count2, all_parcels_receivedFALSE, cycle_completeFALSE, route_position0.5, table_alignedTRUE, table_loadedFALSE/table_clearFALSE, table_transfer_runTRUE, infeed_runFALSE and transfer_inhibitedTRUE. Physical second parcel has progressed; third is blocked near primary. This is progress, not closure.
- Original native evidence: [final actual Signals](../screen-audit-2026-10-21/small-parcel-v4-final-signals.png), [third-route geometry](../screen-audit-2026-10-21/small-parcel-v4-third-route.png), [second-route geometry](../screen-audit-2026-10-21/small-parcel-v4-second-route.png), [delivered UI flow](../screen-audit-2026-10-21/small-parcel-v4-flow.json). [Independent V4 review](../screen-audit-2026-10-21/postbackup-parcel-v4-isolation.md); [original V3 review](../screen-audit-2026-10-21/postbackup-parcel-review.md).
- Cause boundary: [isolated actual-source model reproduction](../screen-audit-2026-10-21/postbackup-parcel-boundary-analysis.md) independently reproduces material gate overlap/third-parcel over-admission with20mm queue gap,15mm bar clearance and18mm per scan. Floating-touch was ruled out for authored extents. Private model coordinates are cause evidence, not native observations; Godot support callback differences remain outside that experiment. No new repair was made after backup.

### QA-UI-002 — stale RUN BLOCKED footer after valid ladder runs (S3, medium; misleading status)

Affected demonstrated entry: `lab-2-01-workstation-call`. Shared editor/footer surface, but do not mark all77entries affected without reproduction. Runtime execution itself is visibly working.

- Repro: enter a fresh blank Lab2.1; Run correctly reports EDIT001/no networks. Through visible editor create Network+NO material_call_pressed→material_call_on; Run; select ErrorList and let advancing scans settle.
- Expected: successful current-draft verification/load/Run clears obsolete invalid-draft guidance consistently.
- Actual current B5 retest: ErrorList is0ERRORS/verification passed and rail/monitor/top runtime are RUNNING at settled scan1343/monitor1338, but outer SceneIO footer remains red RUN BLOCKED/current ladder draft invalid. Original ErrorList failure is improved; remaining footer is not closed.
- Original native evidence: [correct blank rejection](../screen-audit-2026-10-21/small-diagnostics-empty-run.png), [after valid Run](../screen-audit-2026-10-21/small-diagnostics-error-list-after-run.png), [settled contradiction](../screen-audit-2026-10-21/small-diagnostics-error-list-settled.png). [Exact checkpoint](../screen-audit-2026-10-21/postbackup-diagnostics-checkpoint.md), [independent review](../screen-audit-2026-10-21/postbackup-diagnostics-review.md).
- Do not duplicate this as a compiler failure, stopped-controller failure or independent per-scene issue. The original early EDIT001/list issue and later stale-footer residual share QA-UI-002 history.
- Later [causal analysis](qa-ui-002-causal-analysis.md) identifies separate outer/ladder status publication and the existing regression's missing outer-label assertion. No application repair is claimed.

## Current documentation contradiction, not a confirmed faulty control

**QA-SPEC-002; S4 documentation/interaction ambiguity:** `lab-2-01-workstation-call` and `lab-2-06-ready-attention`. Current Lab2.1 purpose still says lamp follows button while held; current Lab2.6 purpose says spring-return. Their action tables explicitly say toggle. Native mouse-down/up preserves the maintained request; a second click restores the other state. Expected is unambiguous interaction wording, not a presumed hardware/spring return implementation.

Original evidence: [Lab2.1 held](../screen-audit-2026-10-21/lab201-material-call-held.png), [released](../screen-audit-2026-10-21/lab201-material-call-released.png), [OS input log](../screen-audit-2026-10-21/lab201-held.json); [Lab2.6 held](../screen-audit-2026-10-21/lab206-native-held.png), [mouseup](../screen-audit-2026-10-21/lab206-native-mouseup.png), [input log](../screen-audit-2026-10-21/lab206-held-ui.json). Current [Lab2.1 help](../../docs/help/scenes/lab-2-01-workstation-call.md) and [Lab2.6 help](../../docs/help/scenes/lab-2-06-ready-attention.md) preserve that contradiction. No new functional failure is inferred from the documented maintained state.

## Old issue disposition / superseding proof

|Original issue|Current disposition and evidence boundary|
|---|---|
|QA-FUNC-001 missing controller binding;9scenes/13controls|Original rejection fixed for observed controls:2.11Start;2.12Raise/Lower;2.13Fill;2.16qualifiedDrill;2.19Open/Close;2.23Sort;2.24CNC;two rawtank pump/drain pairs. Accepted commands and later physical operation supersede the old guard. Parcel process stall remains QA-PLANT-001, not a surviving missing-binding error. See original binding records plus reviews listed below.|
|QA-PLANT-001 legacy missing mechanics|Bounded native closure for2.11/2.12, jug2.13, drill2.16, door2.19 and CNC2.24. Parcel2.23 remains open above. Do not count seven current failures from obsolete ledger cells.|
|QA-LOAD-0012.13 request provenance mismatch|Native composition/loading of2.13/2.23/2.24 after legend correction supersedes this failure. Original cause was station provenance validation, not proved Discard callback corruption.|
|QA-FUNC-002 unmapped actions|11.7physical commonStop now stops commands/pallet and fresh-clear recovery completes;10.3class selector now selects2/3/4 and distinct receiving routes complete. Original two subcases closed in tested build/routes; radar inspection candidate remains separate.|
|QA-VIS-002 fume speed stack|Native0→35→65→100→0 colors/flags, independent hood light, Stop/Run/Reset reviewed. Closed; no measured shaft-speed claim.|
|QA-REF-0019.3 invalid shipped counter preset|Repaired exact supplied reference GUI-loads and counts1→2→3, invalid condition masks result, Reset/fresh repeat works. Closed for repaired artifact, not an alleged compiler defect.|
|QA-DOC-002 teaching inventory omissions|Current help now includes Demo1RESET/COUNT, Demo2STOP/RESET/elapsed/equipment, Demo4machine controls/running, and2.7NC action binding explanation. Original textual omissions superseded by direct current-text correction; this does not independently prove UI operation.|
|QA-DOC-003 Demo4toggle-a-pulse procedure|Current help explicitly says press NEW PALLET once and is not a maintained OFF/ON toggle. Original wording corrected.|
|QA-SPEC-001 generic Stop/Run wording|Still a naming/path clarification: current generic Stop prose does not always distinguish machine controls from AppPlayback. Do not call observed machine counter retention a numerical fault. Independent Demo1/2AppStop cases remain bounded gaps.|
|QA-NAV-CANDIDATE-001 Discard/cache|Never confirmed. Later representative Cancel preservation, Discard Demo1 continuation, Save→correctordinarylab, reopen saved draft STOPPED and fresh functional Run support those exact routes. They do not prove original cached-draft semantics wrong or globally fixed.|
|QA-UX-CANDIDATE-002 gallery AppRun opens empty ladder|Dedicated SIM gallery motion controls subsequently operated; motion/stop of motor/fan/CNC observed. No failed gallery controller is established. Workflow wording remains a possible UX question, not confirmed broken animation.|
|QA-UX-CANDIDATE-003 radar inspection|Still candidate: actual body/transmitter clicks leave inspectionFALSE/opaque; intended clickable control not established. Static absent mapping supports route investigation, not automatic failure of arbitrary shell clicks.|
|QA-UI-CANDIDATE-001 curriculum RECOVERY marker|Still display-consistency candidate; dash versus check semantics unresolved. Completed physical recovery/curriculum pass must not be relabeled failed recovery.|
|QA-VIS-CANDIDATE-0012.3default wallplate framing|Historical readability candidate; no current repaired/worsened state verified here. Not failed control operation.|
|QA-NAV-CANDIDATE-002 out-of-client menu capture|Excluded environment/capture observation;77actual menu entries visited. No confirmed missing catalog rows.|

Superseding native review entry points: [2.11](../screen-audit-2026-10-21/evidence-review-lab211-plant-gap.md), [2.12](../screen-audit-2026-10-21/evidence-review-lab212-plant-gap.md), [jug](../screen-audit-2026-10-21/postbackup-jug-review.md), [drill](../screen-audit-2026-10-21/postbackup-small-drill-review.md), [door](../screen-audit-2026-10-21/evidence-review-fix-service-door.md), [CNC](../screen-audit-2026-10-21/postbackup-small-cnc-review.md), [tankhighlow](../screen-audit-2026-10-21/evidence-review-fix-tankhighlow.md), [tanklevel](../screen-audit-2026-10-21/evidence-review-fix-tanklevel.md), [commonStop](../screen-audit-2026-10-21/evidence-review-fix-commonstop.md), [visionselection](../screen-audit-2026-10-21/evidence-review-vision-class-route.md), [fume](../screen-audit-2026-10-21/evidence-review-fix-fume.md), [reference9.3](../screen-audit-2026-10-21/evidence-review-fix-shipped-sum-counter.md). Original PNGs referenced inside those reviews remain authoritative.

## All77 entries: current confirmed-issue association

This table is an issue association index, **not a per-entry PASS table**. “None identified in reviewed records” leaves untested behavior intact. Five demo aliases refer to their existing scene IDs; they add no entries. Shared QA-UI-002 is listed only where demonstrated.

|Scene ID|Current confirmed issue association|
|---|---|
| `lab-4-01-press-count-lamp` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-01-delayed-lamp` | None identified in reviewed records; not whole-scene PASS |
| `scene-2-conveyor-pusher` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-11-pallet-counting` | None identified in reviewed records; not whole-scene PASS |
| `lab-11-13-xy-palletizing` | None identified in reviewed records; not whole-scene PASS |
| `conveyor-cell` | None identified in reviewed records; not whole-scene PASS |
| `scene-1-conveyor-stop` | None identified in reviewed records; not whole-scene PASS |
| `equipment-gallery` | None identified in reviewed records; not whole-scene PASS |
| `tank-level` | None identified in reviewed records; not whole-scene PASS |
| `tank-high-low` | None identified in reviewed records; not whole-scene PASS |
| `tank-radar` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-01-workstation-call` | QA-UI-002S3; QA-SPEC-002S4documentation contradiction |
| `lab-2-02-dual-confirmation` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-03-service-marker-inhibit` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-04-two-station-call` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-05-bay-light-selector` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-06-ready-attention` | QA-SPEC-002S4documentation contradiction |
| `lab-2-07-dual-contact-permissive` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-08-inspection-vote` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-09-maintenance-beacon` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-10-dust-collector-seal-in` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-11-inbound-tote-stop` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-12-assembly-lift` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-13-coolant-jug-fill` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-14-sump-pump` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-15-fume-extractor` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-16-safe-drill` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-17-pallet-robot` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-18-pallet-pickup` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-19-service-door` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-20-bottle-shuttle` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-21-tote-finishing` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-22-dual-spindle` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-23-parcel-sorter` | QA-PLANT-001parcel S2 Reset/repeat stall; fresh normal and qualified Stop/recovery pass |
| `lab-2-24-robot-cnc` | None identified in reviewed records; not whole-scene PASS |
| `lab-2-25-inspection-toggle` | None identified in reviewed records; not whole-scene PASS |
| `lab-3-01-guarded-pallet-transfer` | None identified in reviewed records; not whole-scene PASS |
| `lab-3-02-robot-cell-safe-restart` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-02-counter-reset-lamp` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-03-repeat-cycle-counter` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-04-sequence-light-tower` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-05-dual-input-count-window` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-06-multi-press-confirmation` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-07-parking-garage-entry` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-08-package-grouping` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-09-chain-drive-lift` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-10-cookie-packaging` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-11-barrel-fill-station` | None identified in reviewed records; not whole-scene PASS |
| `lab-4-12-cable-cut-length` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-02-timed-lamp-off` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-03-rotary-flasher` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-04-alternating-lamps` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-05-variable-flash-rate` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-06-running-light-tower` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-07-pedestrian-crossing` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-08-drawbridge-control` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-09-bag-indexing-conveyor` | None identified in reviewed records; not whole-scene PASS |
| `lab-5-10-coating-line` | None identified in reviewed records; not whole-scene PASS |
| `lab-6-07-luggage-weight-sort` | None identified in reviewed records; not whole-scene PASS |
| `lab-6-08-hand-dryer` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-01-sum-function` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-02-product-function` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-03-sum-and-counter-function` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-04-function-selector` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-10-box-volume` | None identified in reviewed records; not whole-scene PASS |
| `lab-9-12-ev-charging-manager` | None identified in reviewed records; not whole-scene PASS |
| `lab-10-01-drive-alarm-code-string` | None identified in reviewed records; not whole-scene PASS |
| `lab-10-02-chicken-label-print` | None identified in reviewed records; not whole-scene PASS |
| `lab-10-03-vision-package-sorter` | None identified in reviewed records; not whole-scene PASS |
| `lab-10-04-motor-enum-state` | None identified in reviewed records; not whole-scene PASS |
| `lab-10-05-motor-struct-data` | None identified in reviewed records; not whole-scene PASS |
| `lab-10-06-ten-motor-array-startup` | None identified in reviewed records; not whole-scene PASS |
| `lab-11-06-wastewater-collection` | None identified in reviewed records; not whole-scene PASS |
| `lab-11-07-multi-conveyor-pallet-route` | None identified in reviewed records; not whole-scene PASS |
| `lab-11-11-service-elevator` | None identified in reviewed records; not whole-scene PASS |
| `lab-11-12-mobile-traffic-lights` | None identified in reviewed records; not whole-scene PASS |
| `lab-11-19-powder-batch-mixer` | None identified in reviewed records; not whole-scene PASS |

## Historical initial remaining-case list — superseded

Discovery is not complete. This initial list is retained as history; Q01 drawbridge, Q06 Demo1 and parcel moving-handoff Stop/recovery were subsequently reviewed. Use the current finite queue and individual later native reviews before executing any item. These entries are not new defects or a final repair plan.

1. Demo1/lab4.1 independent **AppStop→Run**, distinct from machine RESET/threepressrepeat already observed.
2. Demo2/lab5.1 **AppStop→Run with retained REQUEST**, distinct from proved machineSTOP/RESET; document actual restart path before judging generic prose.
3. Lab4.8 **AppStop during actual raised-gate group release/transfer**, rather than already sampled feeding stop.
4. Lab4.9 **AppStop while lift actually moves vertically**, rather than initial index; exposed destination-clear/box-present qualification only where normal actual UI routes exist.
5. Lab4.12 **Stop during actual knife stroke**, not already proved cable-feed Stop.
6. Lab5.10 **Stop during moving index**, not already proved stationary spray/purge loss cases.
7. Lab5.9 **forward pause permissive loss**, distinct from reviewed reverse CLEARloss. Active freshSTART semantics remain a policy question unless documented.
8. Lab5.3/5.5 **Stop from a known actual ON frame**; earlier pre-Stop frames were already dark. No invented exact timing/duty-cycle requirement.

Other bounded gaps require contract/route qualification first:2.20dedicatedStop/right-sensor reversal;2.21/2.22finalcycle/status semantics;5.2active TP retrigger only if the current lesson explicitly states non-extension;5.7CROSSduringWALK only if explicitignore/noqueue semantics; legitimate process fault controls where actually exposed. Do not invent sensor forcing, simultaneous inputs, independent inhibitors or safety functions merely to fill a matrix.

Already superseded gaps: actual elevator upper/descent/doorloss recovery; all10 ARRAY zone feedback and supported endhold;4.5A2/B4accepted+A2/B5rejected;4.6A2/B3accepted+A2/B4rejected; representative Browser/Viewer/demo/lab/Cancel/Discard/Save/reopen. Current [small native reviews](../screen-audit-2026-10-21/postbackup-native-continuation-checkpoint.md) and their links supersede older rows. No built-in sequential ARRAY startup is promised. Empty gallery belt remains visually unproved, not a defect: [native comparison](../screen-audit-2026-10-21/postbackup-small-gallery-review.md); unchanged dark belt pixels or symmetric rollers are insufficient. OCR missed zone8/9 and upper-landing truth; original frames/reviews resolve those, not a new sensor issue.

No remediation plan is devised here. Original failure history, source/model causes, deterministic tests and actual native acceptance remain separate.

## Exposed-route qualification for harder remaining cases

Current scene declarations make some old named gaps non-executable through available controls. Lab2.20 declares Start/Stop/Reset actions but only shuttle_start maps to a physical action; Lab2.21 similarly only line_start, and Lab2.22 only dual_drill_start. The shared OperatorActions rail is hidden. Consequently a separate **machineStop/machineReset** walkthrough for these three is not presently a grounded mandatory screen test. AppPlaybackStop/Reset remain exposed; do not send invented coordinates or edit private state to simulate a nonexistent station. This route observation is not itself a new defect without a promised physical control.

Likewise Lab4.9 exposes only lift_start_station; destination_clear/box-present are plant feedback, not independently declared operator toggles. Source/help qualification is needed before treating blocked-destination/absent-box injection as an actionable remaining test. The concrete normal-UI gap is still AppStop during actual vertical motion. Lab4.12 exposes Start only, but actual AppStop during its knife stroke is a valid phase-specific case. Lab4.8 exposes ENABLE/CLEAR/LOAD, so AppStop during actual group release is grounded. Lab5.9 exposes START and pause_clear; forward pause-loss is grounded. Lab5.10 exposes START/SPRAY/VENT plus AppPlayback; moving-index Stop is grounded. These distinctions narrow discovery without inventing hardware/safety/fault controls.
