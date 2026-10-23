# Q09 / Q10 / Q11 finite native case grounding

Read-only case notes from current AuthoredDemoLadderPrograms, scene declarations/help and existing native-test-queue. No source edits/input. Ground actual screen controls anew; IDs below identify controls, not reusable screen coordinates. Use authored numbered Demo4/5/3 routes, not unrelated learner fixtures.

## Q09 Demo4 — pallet count retention and full next batch

Scene `lab-9-11-pallet-counting`. Physical switch7 NEW PALLET(pulse pallet_detected), switch8 TYPE VALID(maintained pallet_type_valid), switch9 COUNT REQUEST(maintained count_request), switch10 START(pulse start_command), switch11 STOP(pulse machine_stop), switch12 RESET(pulse reset_pressed). Confirm actual face labels before click.

Authored LAD FB counter DB101 counts **rising pallet_detected** only with batch_ready; preset5. batch_ready requires typevalid+countrequest+machine_running+NOTreset. MachineStart seals machine_running until machineStop/Reset; ordinary machineStop does not reset counter. Counter FB continues under always_scan; Reset explicitly clears its instance. Count output copies accumulator; validation requires completed batch/current permissives.

Minimum finite witnesses: (1) valid running count2 after two separated NEWPALLET events, actual machineSTOP acknowledged/machine_runningFalse/count retained, freshSTART with no phantom count; (2) applicationStop/held freezes scans and may clear projected count, Run restores retained accumulator count without new event; distinguish output image from DB memory; (3) actual machineRESET0, freshSTART and five distinct NEWPALLET events to visibleCOUNT5/BATCHVALID under valid inputs. Capture first postreset1 and final5 with intervening input log; no need five full images. This is full second batch, unlike old count1-only repeat. Reject a STOP filename if running lamp/feedback staysTrue, a held detection newly gated into a false event, or curriculumPassed alone without actual count.

## Q10 Demo5 — ENABLE loss during actual gantry travel

Scene `lab-11-13-xy-palletizing`. switch10 ENABLE toggle system_enable; switch9 START, switch14 STOP, switch15 RESET, switch16 TRIP SIM, switch11 PALLET POSITION VALID. Actual command names in scene/help include private machine_run, PLC vacuum_pick and gantry_x/y/z_positive/negative commands; actual measured encoder counts, tool/carton pose and placed_cartons are the witnesses.

Authored machine_permissive includes system_enable+validpallet+NOTtrip+NOTaxis/vacuum/encoder faults. machine_run seal includes permissive on BOTH start/holding branches, so enableOFF drops run; enableON alone cannot reseal. Sequence states10pickup XZ,20lowerY,30vacuum,40carryXZ,50placeY,60release,70raiseY,80returnXZ,100done. Interrupt during an actually changing nonhome axis; avoid interpreting idle phase as motion stop.

Minimum finite witnesses: moving two poses/counts with machine_runTrue→physicalENABLEOFF accepted, machine_runFalse/motioncommandsOff and held measured pose/counts→ENABLEON only, remain idle→freshSTART resumes existing sequence subject to feedback and finishes supported four-slot layer/home. Prefer an early unloaded travel phase to avoid adding an unrelated unsupported-load/drop question. If interruption occurs carrying a carton, document actual support/attachment rather than asserting all axis+vacuum behaviors from one status lamp. Scope is this authored permissive-loss/restart policy, not safety performance or all-phase recovery.

## Q11 Demo3 — ENABLE loss and actual destination routes

Scene `scene-2-conveyor-pusher`. scene2_station START pulse start_command; switch2 ENABLE toggle system_enable; switch1 RESET, switch4 STOP, switch3 TRIP SIM, switch5 OVERLOAD. Authored machine_permissive includes enable/tripclear/no conveyor or pusher fault. Both seal branches require permissive; restoring enable does not recreate machine_run without freshSTART. Drive enable/overload gates conveyor, pusher enable gates extension until actual extended limit. Physical sensors and carton height determine sorting; read `part_at_pusher`, limit/height feedback and actual conveyor/pusher commands, not only curriculumPASSED.

Minimum finite witnesses: fresh batch at least one short carton physically remains on main conveyor and one tall carton is visibly diverted onto `scene2_receiver` side takeaway; show actual moving carton before ENABLEOFF, commands/runFalse and held pose after; ENABLEON alone idle, freshSTART then continued supported route. Same bounded batch can supply both route and enable witnesses; do not multiply every fault/height/phase combination. Reject empty-belt texture differences as carton movement proof or count/status as a replacement for destination geometry. Existing STOP/TRIP/OVERLOAD evidence remains separate and should not be repeated merely for ENABLE coverage.

No global remediation plan or new arbitrary phase permutations are introduced. Actual acceptance remains dependent on root's native evidence.
