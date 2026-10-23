# Finite Q02–Q04 phase Stop grounding

Read-only inspection of existing fixtures/source, 2026-10-23. No UI calls, application/fixture changes, or new runtime/model experiments. These are source-grounded candidate witnesses, not new native PASS results or a global remediation plan. Cutoff remains2026-10-23T11:00:00Z.

All three fixtures use20ms scans. Nominal times below describe accepted plant time, not fixed-wait Stop triggers. Use actual loaded controller, current native hitboxes, same-image feedback and physical pose. Capture immediate/held Stop and recovery. A stopped output image alone cannot identify the interrupted phase.

| Case | Existing phase witness | Counterexamples | Stop/recovery policy | Nominal opportunity |
| --- | --- | --- | --- | --- |
| Q02 grouping | `group_release=True`, `group_conveyor_run=True`, enable/clearTrue, completeFalse; plus **0<stop_position<100** for gate rise, or **stop_position100/stop_raisedTrue and advancing cartons** for group transfer. | Count3/group_readyTrue occurs before the third carton is staged; conveyorTrue also occurs during feed; receiverOccupied can remainTrue after completion. | Private counter/latch persist; public outputs clear, including count0. Run alone should republish3 and continue release without another LOAD. | Gate rise1s; then `(6.4−2.53)/0.4=9.675s` group travel. The raised-gate moving interval is easier to observe. |
| Q03 cable knife | fireTrue, doneFalse, homeFalse, **0<cutter_position<1**, lengthReachedTrue, feedFalse, faultFalse, actual intermediate blade pose. | HomeFalse or intermediate position also matches return or a held partial stroke; fire alone includes the bottom transition. **No numeric phase exists.** | Stop clears Output cycle_active. Run alone should hold; fresh physical START must authorize the remaining stroke. Length3/stock7 persist. | Outbound0.8s, return0.8s, after10s feed. Shortest window of these cases. |
| Q04 coating | Existing Watch **qa_phase1**, indexTrue, belt_speed>0 and actual moving nonstation part. Public alternative: bothReadyTrue, indexTrue, sprayFalse, **ventFalse**, stationFalse, exitFalse, advancing position. | IndexTrue/stationFalse also matches phase4 discharge. Phase1 alone can be retained while readiness blocks motion. In this exact fixture phase4 vents whenever vent readinessTrue, so the complete public predicate excludes it. | Memory phase survives; Stop clears commands and belt speed. Both readiness selectors remainTrue. Run alone should resume phase1; no fresh START required. | Roughly4s home−2→station near0 at0.5m/s; actual body/beam intersection decides entry. Existing review expects phase1 still moving at2s; source expectation only. |

## Q02 — grouping

Existing `outputs/screen-audit-2026-10-21/test-projects/fixture-lab-4-08-package-grouping-feedback.rpproj.json`, SHA256 `c453e673cc355c4d76f80b8626ed831de116e4d4130065f0d2eaed2693536313`.

- Physical faces: **ENABLE**, **CLEAR**, **LOAD**. Exposed labels: `Toggle conveyor enable`, `Toggle downstream clear`, `Load one carton (idle feed)` (scene174/190/206,309–323). LOAD requires previous feed idle, gate down and remaining capacity. For the first two cartons observe actual completed feed; `group_staged` requires all three. Ideal individual feeds take17.325s/15.2s/13.075s, plus operator delays. Repeated early LOAD clicks do not accelerate the model.
- Configured Watch list has all public points, then `arrivals`, `release_active`, `publish`. One-based rows14/15/16 are group_count/arrivals/release_active (fixture873–890). `watchTables:[]` falls back to this populated list, not an empty table (`LadderEditorProjectJson.cs:263–274`). There is no qa_step or qa_phase.
- Actual beam edges increment CTU; rung2 publishes arrivals.ACC; enabled+clear+counterDone+staged Sets release_active, and complete Resets it (fixture244/320/393/505). `PackageGroupingPlantModel.cs:26–32,41–67` makes group_stagedFalse immediately on release. Requiring stagedTrue throughout the Stop predicate would miss release. Gate must finish rising before carton travel starts.
- Held Stop should show stationary scan/gate/cartons and all BOOL outputsFalse; public count0 is expected while private arrivals3/release_activeTrue remain. Run should restore count3 without a fourth edge and transfer the same visible group to complete, then commandsOff/count3. Counter edge storage survives Stop; it is not reset with ordinary edge-input storage (`VirtualControllerRuntime.cs:257–282,488–513`).

## Q03 — cable

Existing `outputs/screen-audit-2026-10-21/test-projects/existing-cable-cut-reference.rpproj.json`, SHA256 `807181733704232a52ca5608f44064056ed0c66d6bd6a7bd8cb4df24c376bdb3`.

- Physical **START**, exposed `Start / resume cable cut`, pulses batch_start (scene141,284–289). The reference's stop_command is unbound; the scene has no physical action for it. App Stop is the lifecycle control, not an invented scene input.
- Schema1 explicitly has **watchVariables:[]** (fixture587), producing an empty default Watch. No private phase exists; private BOOLs are start_edge/start_accepted. All outbound predicate fields are public Signals. Visible Watch metadata additions, if root chooses them, must be recorded separately as unsaved metadata rather than described as original fixture rows. No such edits were made here.
- Rungs1/2 accept a fresh rising Start; rung4 seals through **Output** cycle_active. Rungs5/6 permit feed only before measured target with knife home, and fire only after target with feedOff/notDone (fixture148/193/310/423/504). By knife time start_accepted is normallyFalse; its Assign rung recomputesFalse with no fresh pulse. Memory retention therefore does not retain cycle authorization.
- Stop clears cycle_active and unscanned local pulses. Plant returns without blade movement while authorizationFalse (`CableCutPlantModel.cs:27–50`). On fresh START a partial outbound stroke continues; retained lengthReached prevents refeed. After done, fireOff allows the authorized return home. cutter_position is normalized0..1; physical full stroke is0.35m (`SceneSimulationRuntime.CableCut.cs:51–63`).
- Expected: held Stop clears feed/fire/cycleActive, holds blade/length/stock and scan. Run alone advances scans but holds blade/length/stock. Fresh START finishes home/cutCompleteTrue, length3/stock7, no fault/allOff, visible retained cut piece. Existing integrated downstroke/return checks (`Main.CableCutAudit.cs:91–109`) express this policy; they do not satisfy pending native knife proof. A Stop landing after done/home does not qualify the outbound case.

## Q04 — coating

Existing `outputs/screen-audit-2026-10-21/test-projects/fixture-lab-5-10-coating-line.rpproj.json`, SHA256 `233484949a6b10634300d783221ee0d8db7932e8f33932d64b7c9bbe3260ecec`.

- Physical **START**, **SPRAY READY**, **VENT READY**, selector positions **OFF/READY**. Exposed `START cycle`, `Manual spray ready`, `Manual ventilation ready` (scene163–205,346–362). Readiness is maintained manual input, not measured process feedback.
- Existing `QA fixture watch` rows11/12/13/14 are index_run/spray_enable/vent_run/**qa_phase**; rows15/16 are qa_spray/qa_purge (fixture1357–1527). qa_step/qa_scan_step do not exist. Actual meanings:0idle,1index-to-station,2spray,3vent-only purge,4index-to-exit,5finished (fixture487/599/857/943/1247).
- Phase1 can persist with readinessFalse and no motion, so require actual index/speed/position progression. Public indexTrue/stationFalse alone cannot separate phase1 from phase4. BothReadyTrue plus ventFalse separates them in this frozen fixture; Watch phase1 is the direct discriminator.
- Stop clears index/spray/vent. PauseCoatingClock sets belt speed0, hides spray and freezes actuator projection (`SceneSimulationRuntime.Coating.cs:50–54`). Phase/position persist. Run alone should continue to actual station→2s eligible spray→1s vent-only purge→exit/phase5/allOff, with a supported visible part. Nonretentive timer restart matters for other phases but is not the pending index case. Source review90–102 in `Main.CoatingReview.cs` states the intended index Stop/Run policy; native proof remains separate.

## Common retention boundary

Accepted plant advancement stops with controller playback (`SceneSimulationRuntime.cs:275–279`; `VirtualControllerSession.cs:207–221`). Runtime Stop zeros BOOL/numeric outputs, retains private memories/counters, and resets nonretentive timers (`VirtualControllerRuntime.cs:257–282`). It discards pending local pulses (`VirtualControllerSession.cs:101–111`; `SceneSimulationRuntime.cs:195–204`). These distinctions explain the three recovery policies; none implies a live-machine safety claim.

After the OS Stop lands, verify actual stopped phase/pose before crediting the target. A missed or ambiguous capture remains a pending QA case, not an application failure.
