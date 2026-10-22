# Typed starter UI contracts: Labs 10.2 and 10.4

Static test expectations only; no UI/runtime execution or PASS claims. Sources: migrated scene simulation.actions/training.machineGuide, src/VirtualController/AuthoredLessonLadderPrograms.cs, SceneComposer.ControlInstallationPlans.cs and ControlInstallations.cs; ChickenLabelPlantModel.cs and SceneSimulationRuntime.ChickenLabel.cs clarify actual timing/feedback ownership. Root must ground coordinates from current screen and verify input changes. Supplied editable starters still require Verify + Load and Run.

## Control installation: do not infer order from screen row

Both scenes use a cabinet (Bench=false), four columns maximum. Inventory is placed in order, first four lower row; remaining upper row. World positions in scene JSON are superseded for listed cabinet devices. Rows are centered independently, not aligned to identical four-column slots; perspective can reverse apparent order. Native click feedback/Signals identifies the control, not a guessed row label.

| Scene | Lower row installation order | Upper row order |
|---|---|---|
|10.2|switch_8 LOAD & WEIGH; switch_9 PRINTER READY; switch_10 DATA VALID; training_accessory_7 label readout|none|
|10.4|switch_1 START REQUEST; switch_3 STOP REQUEST; switch_4 FAULT ACTIVE; switch_6 FAULT RESET|training_accessory_7 state readout (centered)|

10.2 uses measured device envelope spacing; its long label readout can make control spacing much wider than other cabinets. 10.4 uses ordinary cabinet pitch. Table order describes construction, not pixel coordinates or guaranteed screen left-to-right.

## Lab 10.2 — lab-10-02-chicken-label-print

| Physical ID / face | Action / input | Meaning |
|---|---|---|
|switch_8 / LOAD & WEIGH|toggle-product_weighed; type chickenLoad|Load a fresh default1.237kg product, not toggle the measured BOOL; only permitted when not Printing/Applying|
|switch_9 / PRINTER READY|toggle-printer_ready|Maintained manual BOOL|
|switch_10 / DATA VALID|toggle-label_data_valid|Maintained manual BOOL|

No numeric/string cycles. product_weighed, weight_kg, print_complete and application_complete are actual plant feedback. The default mass is not a clickable setpoint. Original switch world positions are respectively[-2.5,0,2], [0,0,2], [2.5,0,3.5], but all are cabinet-relocated.

Editable Memory: selected default tag table contains label_template STRING initial raw text `CHICKEN {0:F3} kg`, unbound. No quotes in INITIAL editor. Change only through tag table EDIT > INITIAL > APPLY TAG CHANGES > Verify + Load > Run; confirm actual new value. This is startup configuration, not a live modify. Restore by Undo + Verify + Load as documented, or edit initial back explicitly. Do not save over app/reference sources.

Saved starter logic: format when product_weighed AND label_data_valid; label_text is retained when format network not enabled. print_request requires weighed+printer_ready+data_valid+NOT print_complete+NOT application_complete+nonempty label_text. apply_request requires weighed+data_valid+print_complete+NOT application_complete. label_applied follows actual application_complete. Printer readiness does not gate the starter's apply network.

| Minimum useful path | Expected (doc-clear unless noted) |
|---|---|
|Reset+Run initial|Absent product; paper hidden; weight0; no completion or commands; readiness/data false|
|LOAD & WEIGH with other buttons false; wait|Tray travel0.3s; weighing stable0.5s; product_present/weighed true, weight1.237; no print without valid data/readiness|
|DATA VALID ON while printer false|label_text=`CHICKEN 1.237 kg`; print_request false; no printed sheet|
|PRINTER READY ON|Print begins with exact captured text;0.6s actual printing; print_complete then separate apply_request,0.5s application; application_complete/label_applied true; print/apply requests return false|
|Wait after completion, requests/permissives held|No duplicate job; captured caption remains exact accepted text|
|LOAD & WEIGH after completed job|Clears completion/paper/captured job and starts new measurement; held readiness/data can automatically enable fresh print/apply after measured feedback; observe second job|
|Separate repeat: lose READY or DATA during0.6s printing, restore|Printing pauses while lost (model gates progress with ready AND data); resumes afterward. Short timing window: absence of a captured pause is coverage limitation, not failure|
|APP STOP during active job, then RUN|Stop freezes model/paper; Run resumes. Observe actual job state; no claim that output requests remain high while stopped|
|APP RESET|Absent off-platform home, hidden paper, cleared job/inputs/outputs; Run again for next case|
|Temporary label_template F3→F2; Apply+VerifyLoad+Run; fresh load+permissives|Actual1.237kg formats and captures `CHICKEN 1.24 kg`; observe actual paper/caption, not editor text only. Restore F3 and repeat fresh job ->`CHICKEN 1.237 kg`|

The print model captures PLC label_text at accepted print edge; later changes to current label_text do not change already printed caption. Input text max255 UTF-16 code units; no default physical control exposes arbitrary mass/text or overlength text. Do not claim those boundaries tested through these three buttons.

## Lab 10.4 — lab-10-04-motor-enum-state

| Physical ID / face | Action | Input behavior |
|---|---|---|
|switch_1 / START REQUEST|toggle-start_request|Maintained BOOL; release and reassert for fresh edge|
|switch_3 / STOP REQUEST|toggle-stop_request|Maintained BOOL|
|switch_4 / FAULT ACTIVE|toggle-fault_active|Maintained BOOL|
|switch_6 / FAULT RESET|reset-motor-fault|Momentary pulse reset_request; not APP RESET|

No numeric/ENUM cycles. State readout is PLC-owned motor_state ENUM{Stopped,Running,Fault}. Original positions start[-1.1,0,0],stop[3.3,0,0],fault[-2.85,0,2.35],reset[1.55,0,2.35] are superseded by cabinet layout.

Editable Memory defaults: fault_latched=false, start_seen=false, start_edge=false, always_scan=true; generated rising-edge baseline BOOL is initialized true to require a sampled-low Start before a new rising event. These are internal logic state, not normal operator setpoints; do not edit/force them for tests. motor_state is an Output initialized Stopped, not editable Memory.

Source and docs agree: actual ENUM retains state; ordered transitions establish fault > Stop > fresh Start. Fault latches until reset with fault_active false. Every sampled Start is remembered, including blocked Start; releasing a blocker while Start remains held must not restart. motor_running derives from state==Running; state_valid checks membership in all three declared values (Fault is a valid state, not state_valid=false).

| Minimum useful path, beginning reset+Run with Start/Stop/Fault false | Expected |
|---|---|
|Initial after scanned low|Stopped; motor_running false; state_valid true (initial output image may be false before first scan)|
|START ON|Running; motor_running true; actual shaft motion|
|START OFF|Remains Running (not hold-to-run)|
|STOP ON|Stopped; motor_running false|
|START ON while STOP held; then STOP OFF while START held|Stays Stopped; no manufactured fresh edge|
|START OFF then ON|Running again|
|FAULT ON|Fault; motor_running false; state_valid true; fault_latched true|
|FAULT RESET while FAULT remains ON|Remains Fault; fault dominates Reset|
|FAULT OFF|Remains Fault (latched)|
|FAULT RESET with Start still ON|Stopped; latch clears; no restart from held Start|
|START OFF then ON|Running; proves fault recovery|
|APP STOP then RUN while START held|Stop clears motor_running and resets output enum to first member Stopped; held Start must not restart (doc-clear). Observe state_valid while stopped separately from running-domain validity|
|APP RESET then RUN|Clears inputs/memory/latch and named state; sample Start low then new ON ->Running|

Documented edit proof: in the Running transition network change MOV SourceA from quoted `Running` to quoted `Stopped` through ordinary instruction properties; Apply+VerifyLoad+Run, sampled-low then fresh START must remain Stopped/motor_running false. Undo+VerifyLoad restores original; repeat fresh START and Stop/fault recovery. This is a temporary isolated learner draft edit, not an app source change. No normal input exposes an invalid ENUM member; do not claim invalid-domain runtime coverage from the three valid named states.

## Observation boundary

Record loaded draft identity and RUNNING scan progression, actual Signals and named outputs, and visible motion/captured text. A row of controls is not mapped merely by appearance. Short print/application transients require before/after evidence and explicit coverage notes; code timing is an expectation rather than proof of screen response.
