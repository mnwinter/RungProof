# Dual-spindle round-trip review

`dual-spindle-round-trip-review.rpproj.json` is a separate offline diagnostic example for `lab-2-22-dual-spindle`. It is not automatically loaded and is not the training exercise solution.

Open the dual-spindle scene in the Windows visual-review app. In Logic Editor use Project > Open, select this file, then Online > Verify + load offline. Expected: one block/task, 13 rungs, 21 tags. Return to Scene, enable Hold offline plant clock, Run, then press Start dual-spindle cycle. Run alone must leave the plant home.

Use Step 0.5 s to review motion, and Step 20 ms near transitions. The QA drives both feeds, retracts A before B, extends only after both measured homes, waits one second at actual transfer_at_end, then explicitly requests return until actual transfer_home. Expected: the shared fixture returns along its supporting bed and stays home without re-extending. Inspect FR/FL/RL/RR/Top while held during return, then Stop and Reset.

Opening the editor releases the visual-review Hold. Stop before checking the watch table when the pose must remain fixed. Expected final watch values: qa_return TRUE, transfer_home TRUE, transfer_at_end FALSE, transfer_retract FALSE. The timer symbol qa_return_delay displays its ET/Q; do not add qa_return_delay.Q as a separately declared watch symbol.

Native validation to date: normal Open/Verify+load, actual Run/Start, returned-home pose and final latch/feedback were observed. Held intermediate return at 38.3% and returned home were inspected from all five angles; the 80% return pose has four distinct named views. No gross visible support loss was observed. Exact transition frames, unsampled poses and continuous solid clearance remain unverified. No physical PLC was connected or written.
