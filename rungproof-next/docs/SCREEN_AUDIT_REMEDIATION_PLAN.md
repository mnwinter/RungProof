# Screen audit remediation plan

Current status: **in progress; no full-app acceptance**. The user authorized fixing all confirmed findings, superseding the earlier discovery-only gate. This document is the current work plan, not a chronological log. Historical failures, excluded QA attempts and earlier build results remain in the linked evidence.

The canonical file is `rungproof-next/docs/SCREEN_AUDIT_REMEDIATION_PLAN.md`.

## Scope and evidence boundary

All77 unique scenes have a normal-route disposition:67 bounded normal reviews, five legacy routes still needing process completion, and five partial demo workflows. Every overall scene remains PARTIAL. All six standalone entries were exercised: both conveyors, gallery, radar and the two tanks now have bounded reviewed normal behavior; gallery animation limits and radar inspection-view candidate remain open. A bounded normal review does not pass every control, sensor, fault, navigation or timing case.

Final B5 build PASS0warnings/0errors; full controller suite424PASS/2existingFAIL (watch-schema expectation and TIA artifact hash). B5 fixes the CNCsupport regression. Native UI acceptance remains incomplete: jugloader78633is live without final native closure, and exact UI002 authoring reproduction remains undone. See deadline-final-status.md for frozen handoff boundaries.

Current live handles and final build state are maintained in SESSION-STATE.json/current-built-app-identity.json. See [deadline handoff](../outputs/screen-audit-2026-10-21/deadline-final-status.md) for source/native/backup boundaries.

## Current finding matrix

“Screen verified” applies only to the named subcase/build. “Source verified” is sufficient for the documentation corrections and is not a screen-behavior claim. Effort is relative remaining work, not a time estimate.

| Finding / impact | Current disposition | Remaining work, dependency and effort | Acceptance evidence |
|---|---|---|---|
| QA-FUNC-001 — S2 rejected machine commands;9scenes/13controls total | Partially resolved. Lab2.11 Start, Lab2.12 Raise/Lower and both tanks' pump/drain routes have bounded native verification. Named requests/migration and automated input checks cover the original bindings. | Five legacy scenes still need normal-path completion:2.13 and2.19 now have independently confirmed plant gaps;2.16,2.23 and2.24 remain unverified. Check loadability, physical request acceptance and command-to-plant behavior separately. Depends on the contracts/priority table below. Medium–large. | For each remaining command, actual loaded matching controller, prerequisites, grounded physical click, accepted request and appropriate plant/feedback response; Stop/Reset/repeat as applicable. Source/input checks alone do not close the process route. |
| QA-FUNC-002 — S2 missing physical action mappings | Lab11.7 common-stop subcase screen verified closed: moving pallet holds, release alone stays off, fresh clear OFF→ON restarts, route completes and Reset restores. Lab10.3 class-selection subcase screen verified closed: class2,3,4 selection and distinct physical receiving routes complete; class1 was previously documented. | No remaining work to close the demonstrated unavailable-selection defect. Exhaustive in-flight class changes/selector wrap remain ordinary open coverage. MissingClear after Reset and early layout failures were QA exclusions. | Readable native vision_class/latched_class, actual distinct receiving route, Complete and commands off. Do not infer class values from filenames or wide unreadable digits. |
| QA-REF-001 — S2 shipped9.3reference rejected | Screen verified closed for repaired shipped artifact, SHA `a2740587a08156c961152b0ea1d3ed75afa1ed029523248af0039ef8287592bd`. Normal load,2+5 sum7, held/fresh counting, retained state, Reset and fresh repeat verified. | No work to close the original load blocker. Counter limits/arithmetic extremes remain ordinary open coverage. Small only if expanded coverage is requested. | [Repaired reference review](../outputs/screen-audit-2026-10-21/evidence-review-fix-shipped-sum-counter.md). Original rejected artifact remains historical. |
| QA-UI-002 — S3 stale invalid-draft diagnostics | Implemented. Successful Run publishes verified state only for the matching current draft. Targeted Godot check passes both stale-error clearing and genuine invalid-draft retention. | Exact native initially-invalid→visible-authoring→Run→settled diagnostic reproduction still pending. Small; use current B2 and preserve genuine invalid draft warnings. | Read current error list/footer and monitor after authoring/Run; then make the draft invalid and confirm its actual errors remain. Loading an already valid fixture alone is insufficient. |
| QA-VIS-002 — S3 absent fume speed indication | Screen verified closed across12 native normal/light/lifecycle cases:0/35/65/100 command display, matching SIM flags and dark/green/amber/red stack; independent light, Stop/Run/Reset. | No remaining work for the demonstrated indicator failure. Shaft-speed ratios and unsampled transients are not claimed. | [Fume review](../outputs/screen-audit-2026-10-21/evidence-review-fix-fume.md). |
| QA-DOC-002 — S4 incomplete teaching inventories | Source verified corrected. Demo1 RESET/COUNT; Demo2 STOP/RESET/ELAPSED/equipment; Demo4 machine controls/run point/equipment; Lab2.7 NC action route now match current contracts. | No behavioral UI test required to verify this documentation defect. Rendered-help review is optional and not claimed. Small if performed. | Four current help documents compared with scene points/actions/equipment; NC mapping accurately identifies config.action rather than inventing a pointBindings row. |
| QA-DOC-003 — S4 pulse described as maintained toggle | Source verified corrected. Demo4 now instructs machine START, qualifiers, one NEW PALLET pulse per event and release between presses. | No behavioral UI test required for the wording correction. Optional rendered-help comparison only. | Current `docs/help/scenes/lab-9-11-pallet-counting.md` and its pulse/action contract. |
| QA-PLANT-001 — S2 absent command-driven mechanics | All seven legacy plant instances are now independently native-confirmed.2.11/2.12/2.19 have bounded closure;2.16 normal and hand-loss hold reviewed, full lifecycle still pending. Jug fill, parcel and CNC require final-build native proof. | Explicit2.11 scannerTrue sample/exact dwell remains unproved. Lift simultaneous-conflict behavior has model-regression evidence, not native UI proof. No remaining instance is inferred solely from source; latest native findings and exact bounds are in the evidence reviews. | [Tote review](../outputs/screen-audit-2026-10-21/evidence-review-lab211-plant-gap.md), [lift review](../outputs/screen-audit-2026-10-21/evidence-review-lab212-plant-gap.md). Interrupted lift route's cycleFalse was correct; previous cycleTrue expectation excluded. |
| QA-LOAD-001 — S2 Lab2.13 composition rejects new request provenance | Three stale legend expectations corrected/built; native post-fix2.13 target-load confirmation now closes the original composition subcase. Its newly confirmed absent plant response is separate QA-PLANT-001. | No remaining work to close the demonstrated2.13 load rejection. Continue its separate confirmed jug plant fix;2.23/2.24 broader scene behavior is not inherited from this load closure. | Actual logs proved correct Discard continuation reached the target before Compose rejected provenance. Do not call this a Discard callback bug or silently close the earlier weak navigation candidate. |

## Remaining plant work order

Use [remaining-plant-adapter-candidates](../outputs/screen-audit-2026-10-21/remaining-plant-adapter-candidates.md) as the source assessment. All five remaining instances are now independently native-confirmed;2.19 bounded native closure is complete,2.16 normal/hand-loss verified with lifecycle gaps, jug/parcel/CNC require final B5 native proof. Authoritative LAD owns sequencing; the plant integrates actuator commands and publishes actual position/content-derived feedback. No hidden reference sequence may stand in for learner control logic.

| Order | Scope / impact | Dependency and effort | Native acceptance boundary |
|---|---|---|---|
| 1 |2.19 service door — confirmed absent plant response | B3 built; native independent open/close travel and NC limits. Small–medium. | Actual shutter travel both directions, midtravel hold, endpoint-derived switches, Stop/Reset/fresh commands; no automatic reversal. |
| 2 |2.13 coolant jug — confirmed absent plant response; load fixed | B3 built; verify documented alignment, fill rate/threshold and supported route; LAD owns feed/fill/discharge. Medium. | Actual jug reaches sensor, fills only aligned/stopped with valve and skid commands, measured high feedback, supported discharge; no timer-fabricated sensor. |
| 3 |2.24 robot/CNC — bounded infeed coverage first | Clear supported infeed-to-pickup geometry/PC sensor. Medium for bounded infeed; full tending needs a larger explicit contract. | Honest infeed movement/actual pickup photoeye. Do not claim grip/door/vise/robot-clear interlocks or full tending from robot_run. |
| 4 |2.16 drill — confirmed absent plant response, explicit command contract now implemented | B3 includes independent PLC feed/retract commands and PC motion inhibition; use matching updated learner fixture. Medium native retest. | Actual downfeed/retract/endpoints and hand-loss hold, no automatic scripted return; Stop/Reset/fresh sequence under LAD. |
| 5 |2.23 sorter — geometry/feedback first | Supported parcel transfers, commanded table slew and real sensor/alignment feedback; extra PC feedback may be needed before robust LAD routing. Large. | Actual measured parcel routes/counts, no timer-generated size bank or reference-step routing. |

Independent small retests (native diagnostics and remaining input/control scopes) can proceed while contract work is prepared; root coordinates builds so they do not race a live pinned app. Batch only grounded controls and selected readable checkpoints; avoid unnecessary full-frame sweeps. Preserve explicit failed/blocked/unproven scopes and QA exclusions.

## Completion gate and evidence

The goal remains incomplete until remaining confirmed subcases are verified, required process contracts/implementation are handled and independent case gaps are dispositioned.Bounded normal reviews do not establish full functionality, exact scan boundaries, continuous conflict freedom, safety performance, installed-TIA parity or live-PLC operation.

- [77-scene ledger](../outputs/screen-audit-2026-10-21/coverage-ledger.json) and [scene issue index](../outputs/screen-audit-2026-10-21/scene-issue-index.md): current per-scene scopes; every overall remains PARTIAL.
- [Deduplicated findings](../outputs/screen-audit-2026-10-21/provisional-issues.md): original reproduction and subsequent scoped closures; candidates stay distinct.
- [Remediation evidence](../outputs/screen-audit-2026-10-21/remediation-evidence.md): historical commands/results and build limits; [B3 build](../outputs/screen-audit-2026-10-21/remediation-build-b3.log), [B3 controller tests](../outputs/screen-audit-2026-10-21/remediation-controller-tests-b3.log), [B2 composition](../outputs/screen-audit-2026-10-21/remediation-b2-composition-results.json) and [B3 health](../outputs/screen-audit-2026-10-21/remediation-b3-live-health.json) establish the current automated/artifact checkpoint.

Preserve the unrelated dirty checkout. No Git commit, GitHub issue, publication or real-PLC action is authorized or performed by this plan update.

Help inventory audit: six owned documents (2.11/2.12/2.16/2.19/2.23/2.24) now list current PC BOOL request points and explicit loaded-controller pulse bindings.2.13 request row/action is coordinated with the active jug owner to avoid conflicts. See [inventory audit](../outputs/screen-audit-2026-10-21/new-binding-help-inventory-audit.md). Source-only documentation check; no UI/build performed for these edits.






