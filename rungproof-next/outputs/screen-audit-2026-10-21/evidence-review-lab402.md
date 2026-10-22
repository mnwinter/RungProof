# Independent Lab 4.2 review

Reviewed actual lab402 held/native captures and thirteen r2 checkpoints through lab402-evidence-compact.jpg, with source manifest and recorded UI actions. One full frame established current scene identity/layout. No UI input, backend execution or app changes by this reviewer.

## Screen-supported cases

All thirteen settled checkpoints show pulse_received=False and reset_pressed=False, consistent with released momentary actions. Actual counter_lamp and physical green indication follow this sequence:

| Checkpoint | counter_lamp / visible lamp | Runtime |
|---|---|---|
| after-held-first-r2 | False / dark | RUNNING |
| count2-r2 | False / dark | RUNNING |
| count3-r2 | True / green | RUNNING |
| count4-r2 | True / green | RUNNING |
| machine-reset-r2 | False / dark | RUNNING |
| repeat1-r2 | False / dark | RUNNING |
| repeat2-r2 | False / dark | RUNNING |
| repeat3-r2 | True / green | RUNNING |
| app-stop-r2 | False / dark | STOPPED |
| app-run-retained-r2 | True / green | RUNNING |
| app-reset-r2 | False / dark | STOPPED, scan0 |
| reset-run-r2 | False / dark | RUNNING |
| new-count1-r2 | False / dark | RUNNING |

The native hold plan records one COUNT mouse-down held1.4seconds then mouse-up and0.4seconds observation; both held captures remain dark. The following next COUNT remains dark, and the next lights the threshold. This supports one event from that held gesture and no threshold-reaching continuous recount in this tested interval, relative to the documented three-event threshold. It does not expose or prove the exact internal CTU accumulator numerically; there is no accumulator readout in these frames.

Physical machine RESET darkens the lamp without stopping app playback; the second complete three-event sequence repeats correctly. App Stop removes the output; App Run relights it without new events, supporting retained internal count state. App Reset clears the threshold state, the next Run stays dark, and one new COUNT remains below preset. Count2/3 labels are action-sequence descriptions, not direct numeric readings.

## Menu/navigation evidence boundary

lab402-open-menu.png and lab402-menu-home.png show Demo3 as the first row visible at clienty7; the top navigation entries are absent from the captured client. Reusing the earlier first-row coordinate requested a different scene and produced the Unsaved scene workspace prompt (lab402-after-held-first.png). The main tester canceled and used View→Engineering Layout instead. This is a verified test-coordinate assumption failure. It is not evidence that Signals failed or that upper menu items are inaccessible.

The retained-menu scroll state, native popup bounds outside the1600x900 captured client, and actual scrolling to first item have not been established. Registered QA-NAV-CANDIDATE-002 with unscored environment/capture boundary, rather than a confirmed app clipping defect.

No new controller functional failure observed. Remaining cases include exact numeric accumulator inspection, Reset while COUNT held, pulse timing extremes, count overflow, load/edit/persistence/error/keyboard paths, and controlled fresh-session repetition. No remediation plan selected.
