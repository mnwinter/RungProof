# Parcel V7 repeat outcome — independent native review

Reviewed original pixels from the actual Reset/Run, repeat observation and subsequent application Stop. AppPID12768, native1021×660; Lab2.23/local simulator, PLC disconnected. No input by reviewer. Recalculated hashes match native action records for Reset, Run, Stop, held and editor originals; observe050/100/142 match their result-manifest hashes. Earlier [V7 completed first batch](parcel-v7-native-review.md) remains valid.

## Repeat evidence

- [Actual Reset](parcel-v7-r3-real-reset-menu.png): STOPPED, scan0, count0, three cartons requeued at infeed; tableclearTrue/loadedFalse, commandsFalse. Footer explicitly reports built-in controller reset/stopped/new machine Start required.
- [Actual Run](parcel-v7-r3-real-run-menu.png): RUNNING scan24; count0, route0, aligned/clearTrue, loadedFalse, infeed/table commandsFalse.
- `parcel-v7-handoff-stop-r1-1792730232033277200/actions.jsonl` records one grounded physical Start(612,386), delivered04:37:12.347UTC, then App Playback menu opening. Observation originals show accepted SORT THREE-PARCEL BATCH MOMENTARY footer. The helper result explicitly records **stop_attemptedFalse**, elapsed40.074s: its requested predicate was not recognized by OCR. This missed automatic Stop is a QA recognition limitation, not an application defect or successful interruption test.
- [observe050](parcel-v7-handoff-stop-r1-1792730232033277200/observe-050.png), scan17436; [observe100](parcel-v7-handoff-stop-r1-1792730232033277200/observe-100.png), scan18124; [observe142](parcel-v7-handoff-stop-r1-1792730232033277200/observe-142.png), scan18700, all visibly show **sorted_count1, route_position0.5, primary_table_alignedTrue, primary_table_clearFalse, primary_table_loadedFalse, table_transfer_runTrue, transfer_inhibitedTrue**. infeed_runFalse/gate_openFalse; small_detectedTrue/misrouteFalse. The original displays numeric1 despite OCR confusing it withI.
- These three wide originals show unchanged carton poses: one carton resting on the lower/front receiver, another at the primary table/transfer area, another queued at infeed. No second receipt or completion is proved. Scans advance1264 across these samples (25.28s at displayed20ms), with the same public feedback/command pattern. This is a sustained repeat-route stall; source cause, exact private LAD phase and class are not established by these images.

## Subsequent real application Stop

[AppStop](parcel-v7-repeat-stall-appstop.png) and [held](parcel-v7-repeat-stall-held.png) are byte-identical originals (SHA256a3ec50a633d3b8ea32c636ff4d38d6a83dcd2d604609cfa4de4504450ff86c78). Both show STOPPED scan20583, table_transfer_runFalse/infeed_runFalse/gate_openFalse, route output0, count1, same actual carton poses. transfer_inhibited remainsTrue. The held capture follows logged1.5s wait. Thus Stop removes the shown commands and freezes scan/pose; it does not clear the retained receipt count. No recovery is established.

[Editor](parcel-v7-repeat-stall-editor.png) confirms runtime STOPPED scan20583 and open saved V7 retentive-class diagnostic fixture. It does **not** expose private qa_step/qa_class values in this frame.

## Reconciliation

V7 first native batch completed count3 with all received/complete and three supported endpoints. V7 also stalls at count1 after this subsequent actual Reset/Run/Start. Preserve both outcomes: neither universal success nor universal impossibility is supported. The reset/repeat requirement remains failed in this exact attempt, while root cause and division between controller policy, state restoration and plant feedback remain unproved. Do not count the OCR predicate miss as a separate product defect. The intended medium-handoff Stop/recovery remains untested by the helper; the later real Stop is at an already stalled state.
