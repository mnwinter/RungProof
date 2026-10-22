# Independent evidence review: Demos 1 and 2

Reviewed actual current-audit PNGs and the Demo 2 recheck contact sheet on 2026-10-21. No app input, behavioral scripts, repairs, or backend tests were run by this reviewer. Initial demo2-batch captures are excluded because the input-delivery calibration was unreliable. Filenames identify artifacts; the observations below come from their pixels.

## Supported visible behavior

| Workflow | Current screen evidence | Result supported |
|---|---|---|
| Demo 1 first cycle | demo1-current.png, demo1-two-pulses.png, demo1-third-event.png | COUNT 1 and 2 with dark lamp; COUNT 3 with green lamp. These stills do not independently establish the number or duration of individual clicks. |
| Demo 1 machine reset | demo1-reset.png | COUNT 0 and dark lamp after the recorded machine RESET action. |
| Demo 1 repeated cycle/curriculum | demo1-repeat-check.png | COUNT 3, green lamp, PASSED THIS SESSION, first count 3 + machine RESET to 0 + repeat count 3, RECOVERY PROOF COMPLETE, curriculum 1/5. The displayed pass is app-reported evidence, supplemented by the independent count/lamp screenshots. |
| Demo 2 request/delay | demo2-recheck-request-off.png, demo2-recheck-before-delay.png, demo2-recheck-after-delay.png | OFF/elapsed 0/dark lamp; ON/740 ms/dark lamp; later 2000 ms/green lamp. Consistent with a delay; exact scan boundary is unproven. |
| Demo 2 machine Stop | demo2-recheck-stop.png | Elapsed 0 and dark lamp after recorded physical STOP; app runtime remains RUNNING. |
| Demo 2 fresh request/repeat | demo2-recheck-restart.png | After the recorded OFF/ON sequence: elapsed 2000 ms and green lamp. |
| Demo 2 machine Reset | demo2-recheck-reset.png | Elapsed 0 and dark lamp after recorded physical RESET. |
| Demo 2 curriculum | demo2-curriculum-repeat-pass.png | PASSED THIS SESSION, 2/5, timer ET 2000 ms/Q TRUE, recovery trace naming machine STOP, RESET ET 0, fresh REQUEST OFF/ON and repeat TON. |

No confirmed functional application failure is established by these captures.

## Findings to consolidate

### QA-SPEC-001 — Demo 2 restart wording does not identify the Stop path (specification question)

Affected: Demo 2 / lab-5-01-delayed-lamp.

Reproduction: read the scene help's Expected observations, then compare DEMO_FUNCTIONAL_ACCEPTANCE.md's current implementation row and the recovery-stage curriculum display.

Expected: instructions distinguish app playback Run/Stop from physical machine Stop/Reset and give an unambiguous restart procedure.

Actual: docs/help/scenes/lab-5-01-delayed-lamp.md:63 says "Run with retained ON starts the full delay again"; DEMO_FUNCTIONAL_ACCEPTANCE.md:25 retains a similar older statement. Its current implementation row at :125 explicitly says machine Stop/Reset disarm restart until fresh request OFF/ON. demo2-curriculum-progress.png instructs REQUEST OFF after reset. The older statement may describe app playback, so these statements are not established as contradictory. This is a naming/specification question, not proof the timer or documentation semantics are wrong.

### QA-DOC-002 — Help control/I/O inventories omit current teaching controls (S4 documentation)

Affected: Demo 1 / lab-4-01-press-count-lamp and Demo 2 / lab-5-01-delayed-lamp.

Reproduction: compare each help document's Expected I/O and Operator actions tables with its current scene and curriculum evidence.

Expected: inventories cover controls and displayed teaching state required by the lesson.

Actual: Demo 1's tables list only pulse_received/threshold_lamp and the pulse action, despite visible machine RESET/COUNT and curriculum reset_pressed/press_count_value. Demo 2's tables list only timer_request/delayed_lamp and REQUEST action; its expected-equipment table lists two objects, whereas current scene shows REQUEST, lamp, STOP, RESET and elapsed display, and Inspector reports five rendered objects. These are documentation omissions; operation of those extra controls is separately evidenced above.

### QA-UI-CANDIDATE-001 — Recovery stage indicator remains a dash after completion (S4 candidate)

Affected: Demo 1 and Demo 2 curriculum cards.

Reproduction: complete the lesson recovery/repeat procedure and open the Scenarios curriculum card.

Observed in both demo1-repeat-check.png and demo2-curriculum-repeat-pass.png: RECOVERY PROOF COMPLETE and PASS COMPLETE coexist with a bottom strip showing checkmarks for QUALIFY, OPERATE and PASS, but a dash before RECOVERY. This looks inconsistent; the dash's intended display contract has not been established, so retain as a candidate rather than a confirmed functional defect. No root cause inferred.

## Remaining proof gaps

- Demo 1 held-input behavior, rapid/repeated input, reset while a button is held, app playback Stop/Run/Reset, and persistence of curriculum progress across resets/navigation are not independently demonstrated by these captures.
- Demo 2 dropping REQUEST during active timing or after completion is not cleanly isolated in the authoritative recheck sequence: its initial OFF/0 capture follows a prior machine reset. Do not claim active request-drop cancellation from that image alone.
- Demo 2's exact 99th/100th accepted-scan boundary, retained-ON behavior across app playback Stop/Run, reset during active timing, and prolonged inhibition without a fresh OFF/ON remain unproven.
- Both: help/logic links, keyboard alternatives, save/load, error paths, accessibility, all entry routes, and complete playback/navigation matrix remain open. Full-scene PASS is not justified by a curriculum pass alone.
- None of this establishes PLC architecture ownership, installed-TIA parity, live equipment operation, safety performance, or completion of the other 75 unique scenes.

No remediation plan selected; discovery remains in progress.
