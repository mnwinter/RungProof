# Next math references: UI test contracts

Static expectations only; no runtime PASS is claimed. Sources: each named migrated scene's simulation.actions, training.solution and training.machineGuide; existing programs/examples/09-*-reference.rpproj.json blocks/tags. These opt-in references must be opened through the real UI, loaded, and RUNNING. An empty exercise is not expected to calculate. Coordinates must be grounded from the actual screen. Numeric displays with NEXT actions are clickable physical controls, not editable setpoints. Sidebar actions mentioned in help may be hidden; use the mapped physical control IDs below.

All listed buttons are maintained toggles. A click inverts the actual current BOOL. Verify its state rather than assuming an initial state after prior testing. Numeric cycles advance through their values and wrap; initial numeric value is 0. APP RESET clears inputs and memory and stops the controller: APP RUN is needed afterward. APP STOP zeroes PLC output images; retained memory versus recalculation is distinguished below. Do not infer a one-scan event from a slow screenshot.

## Lab 9.3 — lab-9-03-sum-and-counter-function

Reference: programs/examples/09-sum-counter-reference.rpproj.json.

| Physical ID | Face/display label | Action | Input / cycle |
|---|---|---|---|
| switch_1 | INPUTS VALID | toggle-inputs_valid | inputs_valid BOOL |
| switch_3 | CALCULATE | toggle-calculate_request | calculate_request BOOL |
| switch_4 | CALL COMPLETE | toggle-call_complete | call_complete BOOL |
| numeric_display_0 | A NEXT | cycle-operand_a | operand_a: 0,1,2,5,10,wrap0 |
| numeric_display_1 | B NEXT | cycle-operand_b | operand_b: 0,1,2,5,10,wrap0 |

Doc-clear and saved-program supported: sum_result=A+B only while inputs_valid AND calculate_request AND call_complete. result_valid follows that same conjunction. An unqualified sum network does not overwrite the previous numeric sum. event_counted is a one-scan rising edge of call_complete qualified by inputs_valid and calculate_request. Main scans FC continuously; a global CTU counts event_counted, then MOV publishes completed_calls.ACC to event_count. The edge contact precedes permissive contacts: restoring a permissive while CALL COMPLETE remains held must not fabricate an edge (explicit documentation).

| Minimal useful path, from reset+Run | Expected screen/Signals result |
|---|---|
| Initial | A=B=SUM=COUNT=0; validity/event false |
| A NEXT twice, B NEXT three times | A=2, B=5; SUM/COUNT remain0 |
| INPUTS VALID ON, CALCULATE ON; CALL COMPLETE ON | SUM7, COUNT1, result_valid true; event_counted transient only |
| Hold CALL COMPLETE across multiple scans | COUNT stays1 |
| CALL COMPLETE OFF then ON | COUNT2; valid false while OFF and true while ON; SUM retains7 |
| CALL COMPLETE OFF; INPUTS VALID OFF; CALL COMPLETE ON | COUNT stays2; validity false |
| Restore INPUTS VALID while completion held | COUNT stays2; SUM7, validity true; no manufactured event |
| OFF/ON completion again | COUNT3 |
| Completion OFF; CALCULATE OFF; completion ON; restore CALCULATE while held | COUNT remains3, validity false then true; repeat new OFF/ON gives COUNT4 |
| With completion ON, APP STOP then APP RUN | Stop SUM/COUNT0 and validity false. Run republishes retained COUNT (doc-clear); qualified SUM7 recalculates. Observe whether held completion unexpectedly recounts; docs explicitly promise retained count but do not fully specify edge-storage lifecycle across Stop/Run |
| APP RESET, then Run | Inputs, operands, sum, count and memory zero; toggles false; repeat first valid event to prove recovery |

Cycle coverage extension: traverse both A/B full cycle independently through10 and wrap0, checking readout at every state. With valid completion held, SUM follows current A+B but COUNT must not increment solely from operand changes. All exposed operand pairs are non-overflowing.

## Lab 9.4 — lab-9-04-function-selector

Reference: programs/examples/09-function-selector-reference.rpproj.json.

| Physical ID | Face/display label | Action | Input / cycle |
|---|---|---|---|
| switch_0 | OPERANDS | toggle-operand_set_valid | operand_set_valid BOOL |
| switch_5 | FUNC VALID | toggle-function_select_valid | function_select_valid BOOL |
| switch_6 | CALCULATE | toggle-calculate_request | calculate_request BOOL |
| numeric_display_0 | A NEXT | cycle-operand_a | 0,1,2,5,10,wrap0 |
| numeric_display_1 | B NEXT | cycle-operand_b | 0,1,2,5,10,wrap0 |
| numeric_display_2 | FUNC NEXT | cycle-function_choice | 0,1 SUM,2 PRODUCT,99 invalid,wrap0 |

Doc-clear and saved-program supported: Main always calls FB_Selector. Qualified choice1 calls FC_Sum, choice2 calls FC_Product. selected_result_valid requires all three BOOL inputs AND choice in{1,2}. Unsupported choices0/99 or any lost BOOL clear validity and retain last numeric selected_result. Global tags are used; no modeled FB parameter transfer or instance-local state.

| Minimal useful path, reset+Run | Expected |
|---|---|
| Initial | choice0, RESULT0, validity false |
| A2 (two clicks), B5 (three); turn all three BOOLs ON, leave choice0 | RESULT0 invalid |
| FUNC NEXT →1 | RESULT7 valid |
| FUNC NEXT →2 | RESULT10 valid; only chosen product path publishes |
| FUNC NEXT →99 | RESULT10 retained, invalid despite FUNC VALID ON |
| FUNC NEXT →0 | RESULT10 retained invalid |
| FUNC NEXT →1 | RESULT7 valid again |
| Toggle OPERANDS OFF/ON, then FUNC VALID OFF/ON, then CALCULATE OFF/ON individually | Each OFF: last RESULT7 retained invalid; each ON: RESULT7 valid |
| At choice1, change A2→5 | RESULT10 valid; choose2 →RESULT25 valid |
| APP STOP then RUN with supported choice and permissives held | Stop RESULT0 invalid; Run recalculates25 valid (source-derived from continuously scanned routes) |
| APP RESET then RUN | Inputs/choice/result reset0, validity false; repeat supported selection to verify recovery |

Cycle coverage extension: both operand readouts cover0,1,2,5,10 and wrap. Choice0 and99 are both UI reachable invalid tests. No arbitrary values or forcing needed.

## Lab 9.10 — lab-9-10-box-volume

Reference: programs/examples/09-box-volume-reference.rpproj.json.

| Physical ID | Face/display label | Action | Input / cycle |
|---|---|---|---|
| switch_5 | LENGTH | toggle-length_valid | length_valid BOOL |
| switch_6 | WIDTH | toggle-width_valid | width_valid BOOL |
| switch_7 | HEIGHT | toggle-height_valid | height_valid BOOL |
| numeric_display_0 | L mm NEXT | cycle-length_mm | 0,250,500,720,850,1000,wrap0 |
| numeric_display_1 | W mm NEXT | cycle-width_mm | 0,250,500,720,850,1000,wrap0 |
| numeric_display_2 | H mm NEXT | cycle-height_mm | 0,250,500,720,850,1000,wrap0 |

Doc-clear and saved-program supported: all three validity BOOLs AND every dimension >0 AND <=1000 enable both MUL networks and the validity coil. First base_area_mm2=L*W; then volume_mm3=base_area_mm2*H. Invalid numeric dimensions or a missing flag retain the last numeric volume and clear volume_result_valid. There is no CALCULATE action. Fixture/sensor heads and carton are static illustrations and dimensions do not resize the carton.

| Minimal useful path, reset+Run | Expected |
|---|---|
| Initial | L/W/H/VOLUME0, validity false |
| All three flags ON at zero dimensions | VOLUME0 invalid (flags alone cannot authorize zero) |
| L→850 (4 clicks), W→720 (3), H→720 (3) | 440640000 mm3, valid |
| LENGTH OFF/ON; WIDTH OFF/ON; HEIGHT OFF/ON individually | Every OFF retains440640000 invalid; ON recomputes same valid |
| L850→1000; W720→850→1000; H720→850→1000 | Final1000000000 valid; intermediate products use actual displayed dimensions |
| L1000→0 | Retain1000000000 invalid; restore L→250 yields250000000 valid |
| W1000→0 then→250 | Zero invalid retaining250000000; restored result62500000 valid |
| H1000→0 then→250 | Zero invalid retaining62500000; restored result15625000 valid |
| APP STOP then RUN with250/250/250 all valid | Stop VOLUME0 invalid; Run recalculates15625000 valid (source-derived continuous networks) |
| APP RESET then RUN | All input flags/dimensions and numeric memory/output zero; invalid; repeat a valid case to verify recovery |

Cycle coverage extension: traverse every dimension readout through all six values and wrap, verifying actual input readout after each click. 250^3=15625000, 500^3=125000000, 720^3=373248000, 850^3=614125000, 1000^3=1000000000 are useful equal-dimension anchors. Negatives,1mm and >1000 are documented/source guard conditions but are NOT reachable through declared physical cycles; do not claim those boundaries tested without a separately authorized normal UI edit route. The visible cycle maximum is1000; no backend modification is permitted for this walkthrough.

## Evidence boundary

This document is a test matrix, not an execution report and not an application repair plan. Record UI-loaded reference identity, RUNNING scan progression, observed inputs, output/validity, and explicit event/history feedback for each actual path. Distinguish readable actual screenshots/Signals from arithmetic/source-derived expected values. Missing one-scan event imagery is not a failure when persistent COUNT proves the repeated-event behavior.
