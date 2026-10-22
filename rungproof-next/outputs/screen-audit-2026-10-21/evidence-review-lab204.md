# Independent Lab 2.4 review

Reviewed ten actual native captures through a compact derived ROI sheet, lab204-evidence-compact.jpg, and the physical/UI action plan plus monitored-results.jsonl. Full originals remain preserved. One full lab204-11.png was inspected to establish scene identity, physical control positions and crop bounds. No UI input or backend behavior execution by this reviewer.

## Screen-supported results

| Cases | North | South | Output/beacon | Result |
|---|---|---|---|---|
| lab204-00 | False | False | False/dark | Observed |
| lab204-10 | True | False | True/amber | Observed |
| lab204-11 | True | True | True/amber | Observed |
| lab204-01 | False | True | True/amber | Observed |
| lab204-00-repeat | False | False | False/dark | Observed |
| lab204-10-repeat | True | False | True/amber | Observed |
| lab204-app-stop | True | False | False/dark, runtime STOPPED | Observed |
| lab204-app-run | True | False | True/amber, runtime RUNNING | Observed |
| lab204-app-reset | False | False | False/dark, STOPPED/scan0 | Observed |
| lab204-reset-run | False | False | False/dark, runtime RUNNING | Observed |

The Signals panel explicitly identifies assistance_call_on, north_call_pressed and south_call_pressed. Left physical NORTH CALL toggles north, right SOUTH CALL toggles south; the amber beacon follows OR in all four maintained-input combinations and repeat checkpoints. This agrees with the help's explicit toggle action contract. App Reset was tested with North still active, so unlike the earlier Lab 2.3 reset case this supports clearing an active input. App Stop retains the North input but removes the output; App Run restores the output from that retained input.

No observed functional failure in these ten cases; no new issue added. This is a scoped fixture-backed screen test, not a claim that the app supplies a default solution or that every authoring/save/load/error/keyboard case passes. Mouse-hold semantics, simultaneous input delivery, exact scan latency and restart after navigation remain unproven.

## Evidence compression improvement

For each scene, inspect one full native frame to establish identity, camera, control positions and ROI bounds. For later checkpoints, preserve the full PNG/hash but present a compact strip containing only (1) physical controls/output, (2) readable Signals values, (3) runtime/scan badge and (4) case ID. Group ten to twelve cases per sheet. Parse OCR into one actual-value table rather than returning full OCR text; inspect the full frame only on a mismatch, missing label, scene/layout change or ambiguous motion. Never treat OCR success or a lit pixel alone as a whole-scene pass.

lab204-evidence-compact.jpg demonstrates this approach: ten cases fit one 1600x1150 sheet, preserving the physical beacon, all three symbolic values and runtime state for each case. No app repair plan is selected.
