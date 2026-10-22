# Independent evidence review: Demos 3 and 4

Reviewed actual PNGs/contact sheets and current scene help on 2026-10-21. No UI input, app changes, backend tests, or repair planning. Scene identity is established from the image title/Inspector; filenames alone are insufficient because capture after scene switching can lag.

## Demo 3: Height Sorting Pusher Cell

- Normal checkpoints show changing carton positions, green conveyor status, and DONE rising from 1 to 3 (demo3-normal-checkpoints.jpg). They establish visible progression, not the individual destination of every 420/820/600 mm carton.
- Trip checkpoints show the red tower, unchanged DONE=3 across the trip/start attempt, then green status and DONE=1 after clear/Reset/fresh Start. Stop/reset checkpoints show green extinguished, reset DONE=0, then DONE=3 after the repeated run (demo3-recovery-checkpoints.jpg).
- Final trip-recovery card remains IN PROGRESS, explicitly requesting overload (demo3-final-curriculum.png). It must not be cited as a final pass.
- Overload checkpoints show amber status and DONE=9 during the blocked-start case, dark tower/DONE=0 after clear/reset, then green status/DONE=3 after restart (demo3-overload-checkpoints.jpg).
- The actual final card is demo3-overload-final-curriculum.png: PASSED THIS SESSION, 3/5, and app-reported evidence for three routes, machine Stop/restart, trip clear/Reset/restart and overload clear/Reset/restart. Its current parts_completed=0 is a current snapshot after reset, not the earlier completion count; the latched pass alone is not a substitute for route observation.

Still unproven: independent per-carton destination/height-boundary trace, pusher retraction before every feed, blocked-photoeye restart, simultaneous trip+overload indication, Enable removal/recovery, phase-by-phase Stop/reset, app playback lifecycle, all navigation/help/logic routes, and scan-by-scan PLC architecture ownership. The reset/no-auto-restart snapshots cover a short checkpoint interval; they do not prove indefinite inhibition.

## Demo 4: Batch Process Blocks

- demo4-invalid-event.png shows COUNT=0, RUNNING amber on, BATCH VALID off after the recorded invalid detection event. It demonstrates rejection of that tested invalid configuration; it does not independently isolate every validity/permissive combination.
- demo4-valid-first-event.png shows COUNT=1 after permissives and a new event. demo4-five-events.png shows COUNT=5 with both RUNNING and BATCH VALID lit.
- demo4-machine-stop.png and the board-checks crop show COUNT retained at 5 while RUNNING and BATCH VALID go dark. The app runtime remains RUNNING, distinguishing physical Stop from app playback Stop.
- demo4-machine-restart.png shows COUNT=5 and both lamps lit again. demo4-machine-reset.png shows COUNT=0 and both lamps dark. demo4-second-batch-first-event.png shows COUNT=1/RUNNING on/BATCH VALID off after restart and a new event.
- demo4-curriculum-progress.png shows PASSED THIS SESSION, 4/5, and recovery first batch 5 + machine RESET to 0 + batch-two COUNT 1. Unlike Demos 1–3, all four stage labels include checkmarks here.

Still unproven: held detection/no recount, permissive change while detection is held/no fabricated edge, each invalid-input combination independently, events while stopped, count limit/overflow, complete second five-event batch, app playback lifecycle, FB/FC/instance-DB behavior through the visible editor, all navigation/help routes, and retention across scene switching.

Documentation questions: the lab help's generic Stop/Run numeric-output description does not explicitly name app playback; it must not be used to fail physical Stop retention. Help omits physical Start/Stop/Reset from its operator table, and its Normal sequence says to toggle detection OFF/ON despite the action table and actual UI using a NEW PALLET pulse event.

## Cross-demo presentation observations

The passed cards for Demos 1–3 show a dash before RECOVERY despite pass-complete feedback; Demo 4 shows a checkmark for RECOVERY. This strengthens the candidate display inconsistency, without establishing a functional recovery failure or common implementation cause.

Demo 4's pass-trace text includes batch_count_value=0 while its headline/readout show pallet_count=1. Those are different named values; their intended relationship was not established by this review. Do not call this an application defect without verifying the display contract.

No confirmed functional failure identified in this evidence subset. Remaining scope stays open.
