# Demo 1 application Stop / Run / Reset independent native review

Reviewed 2026-10-23 UTC using the seven original native PNGs, corresponding OCR and `native-ui-actions.jsonl`. This is an independent pixel review, with no UI input or application queries by the reviewer. The action log identifies app PID **12768**, native client **1021 × 660**, and local runtime; the visible header shows PLC disconnected. All seven original SHA256 values were recalculated and match their action-log records.

## Visible sequence

| Original frame | Logged action | Actual visible result |
|---|---|---|
| [count1-before-appstop](demo1-r1-count1-before-appstop.png) | PULSE click (314,234), then 0.5 s | RUNNING; scan 10186; COUNT **1**; threshold lamp dark; lesson IN PROGRESS. |
| [appstop](demo1-r1-appstop.png) | Application Stop (113,646), then 0.5 s | STOPPED; scan **10220**; COUNT **0**; lamp dark; lesson NOT QUALIFIED. |
| [appstop-held](demo1-r1-appstop-held.png) | No input; wait 2 s | Still STOPPED, scan **10220**, COUNT **0**, lamp dark. Tooltip explicitly describes stopping simulator scan/output projection, separate from the lesson machine Stop input. |
| [apprun-resumed](demo1-r1-apprun-resumed.png) | Application Run (45,646), then 0.5 s | RUNNING; scan **10243**; COUNT returns to **1**, lamp dark. No intervening PULSE input is recorded. |
| [after-second-event](demo1-r1-after-second-event.png) | PULSE click (314,234), then 0.5 s | RUNNING; scan **10302**; COUNT **2**, lamp dark. |
| [after-third-event](demo1-r1-after-third-event.png) | PULSE click (314,234), then 0.5 s | RUNNING; scan **10360**; COUNT **3**, actual green threshold lamp visibly illuminated. |
| [appreset](demo1-r1-appreset.png) | Application Reset (190,646), then 0.5 s | STOPPED; scan **0**; COUNT **0**, lamp dark; lesson NOT QUALIFIED. |

Inputs ran 04:00:21.888 through 04:00:30.228 UTC. The full hashes and input delivery records remain in [native-ui-actions.jsonl](native-ui-actions.jsonl), plan `demo1-appstop-r1.json`, steps 0–6.

## Finding

This sequence establishes application Stop freezing the displayed scan and clearing the projected numeric output, while Run restores the previous count **1** without another pulse. The later two separate pulses produce **2**, then **3** and illuminate the threshold lamp. Thus the visible output reset during Stop is distinct from retained counter state across Stop/Run. Internal counter retention is an inference from restoration and subsequent increments; no private DB was inspected. Application Reset then clears the displayed result and returns scan/playback to STOPPED/0.

No defect is demonstrated in this tested path. This closes the previously missing Demo 1 application Stop/Run retention case **at count 1**; it does not establish overall Demo 1 or 77-scene acceptance.

## Remaining boundaries

- Stop was applied with the threshold lamp already dark. Clearing an illuminated threshold lamp specifically by application Stop remains unobserved here; Reset clearing that lamp is observed.
- This does not test the lesson's machine Reset input, prolonged held/retrigger behavior, or every threshold/reset combination.
- Frames are discrete observations, not continuous timing measurements. The unchanging STOPPED scan is bounded by the two saved stop frames.
