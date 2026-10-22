# Independent Lab 2.20 bottle shuttle review

Reviewed actual native roundtrip/interrupt contact sheets, one full matching-scene frame, eight monitored-roundtrip PNGs through lab220-evidence-compact.jpg, recorded UI plans and help. No UI input, backend execution or app/source changes by this reviewer. Existing reference logic was GUI-loaded by the main tester.

## Physical movement and measured endpoint evidence

The first six-frame roundtrip sheet visibly shows outbound bottle displacement and green running status, but its frame named completed-check is still moving/outbound. It is not proof of completed travel; names are not assertions.

The later monitored sequence provides stronger evidence:

| Native case | Actual motor_run | Actual direction | Actual cycle_complete | Actual sensors/status | Physical observation |
|---|---|---|---|---|---|
| repeat-outbound, motion1, motion2 | True | +1 | False | LeftFalse, RightFalse, green | Bottle progresses from left toward right |
| motion3, motion4, motion5 | True | -1 | False | LeftFalse, RightFalse, green | Bottle progresses back left |
| motion6 and repeat-complete | False | 0 | True | LeftTrue, RightFalse, amber | Bottle at left/home and stationary in the later settled checkpoint |

This establishes actual visible travel in both directions and final stopped/complete/left-home state, rather than inferring motion from a command bit. The transient right_sensor_active=True edge was not captured; the right-sensor-to-reversal causal/scan boundary is therefore not independently proven. Intermediate expected:{} records are observations with no assertions and must not be counted as passed test cases. The final repeat-complete record has explicit expected values that match the actual image.

lab220-settled-observation.png also shows motorFalse/direction0/leftTrue/rightFalse/cycleCompleteTrue with amber status after an earlier run. Together with the later physically observed complete roundtrip, this supports repeatability at the tested endpoint scope.

## App interruption, restart and reset

lab220-interrupt-ui-checkpoints.jpg shows a midtravel bottle/green status, then App Stop with amber status and runtimeSTOPPED. The next stopped checkpoint1.2seconds later retains the same bottle position and scan badge. App Run returns green and the bottle advances from that interrupted position. App Reset returns the bottle to left/home with amber status and runtimeSTOPPED; the next Run leaves it home/amber at the short checkpoint. These support bounded stop stability, continuation, and reset positioning; they do not prove an indefinite pause or every travel-phase reset.

The physical start button was exercised. Dedicated Stop shuttle/Reset bottle actions exposed through Operator Console have not been assessed by this evidence subset. The help's declared simulation safe-state status_color=red must not automatically be used to fail App Stop's observed amber status without establishing which Stop path owns that contract. Scope added to QA-SPEC-001 naming question.

No new confirmed functional failure. Missing cases include the transient right sensor, exact reversal timing, dedicated scene Stop/Reset paths, Start while already moving, phase-specific/both-sensor conditions, navigation/persistence, keyboard access and repeated fresh-session loading. No whole-scene PASS or repair plan selected.
