# Independent Lab 2.2 and Lab 2.3 checkpoint review

Read-only review of native screen PNGs, monitored screen OCR, recorded OS input plans, and help on 2026-10-21. No UI input, source changes, or backend execution by this reviewer. The matching ladder fixture was opened through GUI by the main tester; it is not an app-supplied default solution.

## Lab 2.2: physical mapping and AND behavior

The cabinet controls in lab202-truth-table-controls.jpg show OPERATOR on the left green button, QUALITY OK on the right blue button, and a separate green ready lamp. Recorded clicks are at the visibly identified buttons, not simulator API calls. The monitor captures establish the resulting symbolic state independently of fixture filenames:

| Screen checkpoint | operator_confirmed | quality_confirmed | handoff_ready | Visible lamp |
|---|---|---|---|---|
| lab202-monitored-00.png | False | False | False | Dark |
| lab202-monitored-10.png | True | False | False | Dark |
| lab202-monitored-11.png | True | True | True | Green |
| lab202-monitored-01.png | False | True | False | Dark |
| lab202-monitored-00-again.png | False | False | False | Dark |
| lab202-monitored-11-again.png | True | True | True | Green |

This supports the tested physical-input mapping, complete maintained-input AND truth table, clearing either input, and a repeated both-active case. It agrees with the help's explicit toggle actions and both-confirmations-active purpose.

App Stop with both inputs active produces a dark lamp; App Run restores green with retained inputs (lab202-stop-active.png / lab202-restart-retained-inputs.png). App Reset produces a dark lamp, then App Run remains dark (lab202-reset.png / lab202-reset-restart.png). The stronger reset monitor capture lab202-reset-inputs-cleared.png shows operator=False, quality=False, handoff_ready=False while running. Thus reset input clearing is supported, rather than inferred from lamp color alone.

Source-scene protection is separately screen-observed: lab202-project-loaded.png actually shows the Powder Batch Mixer and PROJECT OPEN BLOCKED / SOURCE SCENE MISMATCH / select lab-2-02-dual-confirmation then open again. It is not a successful load despite its filename. lab202-fixture-running.png shows the correct Dual Confirmation scene running after matching-scene navigation and GUI reopening. No incorrect-scene execution is evidenced.

Unproven: every GUI loading failure/cancel path, fresh-session reproducibility, keyboard alternatives, file persistence, diagnostic refresh after this fixture load, transient scan response time, and modification/undo behavior. This targeted truth-table result does not pass unrelated labs or establish hardware/TIA behavior.

## Lab 2.3: default-camera teaching-plate clipping

Both breadth-lab-2-03-service-marker-inhibit-entry.png (STOPPED) and fixture-lab-2-03-service-marker-inhibit-running.png (RUNNING) show a teaching wall plate continuing beyond the right edge of the 1600x900 client area. Its NORMAL / INHIBIT heading and symbolic names are cut off. The physical INHIBIT button and white service-marker lamp are fully visible, as is the left scenario description. The running image shows the initial white lamp lit, consistent with documented service_marker_on=True; it does not establish inhibit input behavior.

Registered QA-VIS-CANDIDATE-001 as an S4 default-framing/readability candidate. Camera navigation/reframing may provide access but has not been tested here. Do not classify this as a blocked control or failed inhibit function.

No additional functional failure identified in this reviewed subset. No repair plan selected.

## Lab 2.3 targeted operation follow-up

Reviewed the nine terminal r2 cases in monitored-results.jsonl against the actual native PNGs and enlarged symbolic-point crops. Normal state is marker_inhibit_pressed=False/service_marker_on=True with the white lamp lit. A physical INHIBIT click makes input=True/output=False and lamp dark; another click restores False/True and the lamp. The second inhibit/release pair repeats the same result. These are maintained toggle operations, consistent with the explicit help action table; the filenames called release refer to a second toggle click, not mouse-up behavior.

App Stop gives False/False, dark lamp, runtime STOPPED (lab203-app-stop-r2.png). App Run restores False/True (lab203-app-run-r2.png). App Reset while the input is already false gives input=False, output=True, runtime STOPPED/scan0 (lab203-app-reset-r2.png); this is the scene's declared initial output state and is distinct from Stop's output removal. App Run after reset shows False/True again (lab203-reset-run-r2.png). This reset case does not prove clearing an active inhibit input because the pre-reset input was already false.

The initial normal-case automated OCR missed the service_marker_on label even though the native PNG visibly showed True. Enlarged image OCR restored the label/value correspondence in the r2 evidence. That was an evidence-reading failure, not an app defect. Nine terminal case records have matching expected screen values, but only the specific tested toggle/repeat and playback cases are supported; no blanket scene completion is claimed. The clipped default teaching plate remains the separate S4 visual candidate; no new functional issue was observed.
