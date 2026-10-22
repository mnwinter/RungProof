# Remaining motion UI contracts — Labs5.7–5.10

Static expectations from current help + existing test-projects/fixture-<scene-id>.rpproj.json only. No new controller/source edits, UI input or runtime execution. Ground coordinates on actual scene; cabinet installation order does not establish projected screen order. Normal Open→Verify+Load→Run with matching scene identity. Distinguish actual PC feedback, PLC commands and physical motion; short screenshots do not establish exact accepted-scan timing.

## 5.7 pedestrian crossing — lab-5-07-pedestrian-crossing

Controls: switch_2 CROSS→toggle-crossing_request→PC crossing_request (momentary pulse); switch_7 PATH→toggle-clear_to_finish→PC clear_to_finish (maintained). Only PATH is cabinet-relocated; CROSS remains at its authored roadside station, so do not search for both on one cabinet.

Outputs: vehicle_ready(green),vehicle_amber,vehicle_stop(red),pedestrian_walk,pedestrian_stop(hand),sequence_running. Private qa_phase:5 startup,0 idle,1 amber,2 allred,3 WALK,4 clearance. First Run: red+hand3s, then wait PATH CLEAR→green+hand. Fresh CROSS while idle+clear: amber1s→allred0.5s→whiteWALK2s with vehicle red→steady hand/red3s→idle only if PATH CLEAR. Flashing hand is explicitly OMITTED by current lesson, not a missing fixture function.

Lean path: Run startup→PATH CLEAR→idle green→CROSS full cycle→PATH BLOCK during clearance holds red/hand→PATH CLEAR releases→fresh CROSS second cycle. An unavailable/held request is sampled/discarded, not deferred; no queued advance from rapid unsampled clicks. Stop zeroes all output images and holds accepted scan; Run's unbound scanning marker forces startup red/hand again before green. Reset clears inputs/phase/timers and stays stopped. No vehicles/people are simulated; expected physical observations are both opposing heads/far-side pedestrian heads, not traffic motion. No counter.

## 5.8 drawbridge — lab-5-08-drawbridge-control

Cabinet inventory: switch_6 BRIDGE→toggle-bridge_request→PC bridge_request (FALSE=CLOSE,TRUE=OPEN); switch_2 STOPPED→toggle-traffic_stopped→PC traffic_stopped acknowledgement. Both maintained.

Actual PC: bridge_home,bridge_raised,barriers_closed,barriers_open,bridge_angle,barrier_angle,motion_inhibited. PLC: bridge_raise,bridge_lower,barrier_close,barrier_open,traffic_release,traffic_stop. Fixture no timers/phase counters: commands follow actual limits/interlocks. CLOSE+home opens barriers; only actual barriers_open permits traffic_release. OPEN removes release/closes barriers; raise requires STOPPED+actual barriers_closed. At raised limit raise stops. CLOSE lowers with STOPPED+closed barriers; home permits barriers open, then traffic release. Plant reference travel: barriers2s,deck4s to70degrees, not PLC-generated feedback.

Lean path: initialRun CLOSE/home→barriers open/green; OPEN with STOPPEDFALSE: barriers close but deck must not raise; STOPPEDTRUE→raise to actualraised/70deg; CLOSE→lowerhome→openbarriers→green. Stop during deck travel holds angle/commands0; Run with maintained request/permissives resumes — this fixture has no fresh-start lockout. Remove STOPPED during motion to remove deck command; restoring resumes (fixture behavior, not a mandatory universal restart policy). Reset returns home/closed-barrier baseline and stops. Inspect actual hinged deck and gate limits; no vehicles/boats/occupancy detection. No counter.

## 5.9 bag indexing — lab-5-09-bag-indexing-conveyor

Cabinet order: switch_4 START→pulse-start_request→PC start_request (pulse); switch_9 PAUSE CLEAR→toggle-pause_clear→PC pause_clear (maintained); output indicators indicator_10/11 follow installation. Actual PC: bag_at_entry,bag_at_exit,bag_position,belt_speed,motion_inhibited,travel_limited. PLC: conveyor_run and separate conveyor_reverse direction select.

Fixture qa_phase0ENTRY idle→freshSTART forward phase1→actualEXIT pause phase2; freshSTART atEXIT AND PAUSE CLEAR→reverse phase3→actualENTRY idle0. START evaluated before endpoint transitions: travel press does not carry into newly entered wait state. This is pulse-input use, not extra fabricated edge or automatic return. Plant reference speed0.5m/s.

Lean path: initial actualENTRY→START→observe supported sack travelling and actualEXIT→runFALSE; START with CLEARFALSE leaves pause; CLEARTRUE alone still paused; freshSTART→reverse+run until actualENTRY→runFALSE. Reverse remains a direction selector while phase3; inspect run independently. Withdraw CLEAR during return stops run; restore CLEAR resumes retained return phase (no fresh-edge lockout on that restoration). App Stop holds pose and clears commands; Run restores retained phase subject to limits/clear. Reset loadsENTRY/phase0/inputsfalse and stays stopped; repeat. Actual optical beam feedback, not numeric position comparison, stops legs. No counter.

## 5.10 coating — lab-5-10-coating-line

Cabinet inventory lower: switch_9 START→pulse-start_request→PC start_request; switch_10 SPRAY READY→toggle-spray_ready→PC spray_ready; switch_11 VENT READY→toggle-ventilation_ready→PC ventilation_ready;indicator_4. Upper indicators indicator_12/13. Read actual labels/Signals, not projected order.

Actual PC: workpiece_at_station,workpiece_at_exit,workpiece_position,belt_speed,motion_inhibited,spray_inhibited,travel_limited. PLC: index_run,spray_enable,vent_run. qa_phase0 idle; freshSTART with both ready→1 index until actualSTATION→2 eligible spray2s→3 sprayoff/ventpurge1s→4 discharge until actualEXIT→5 done. Timers run before phase changes and require actual command/readiness conditions; a new phase begins timing on next accepted scan. vent_run covers phases2..4 when ventready. One supported workpiece; Reset reloads.

Lean path: START with missing readiness does not start; enable both, freshSTART→actualstation/stopindex→physicalspray withvent→purge withoutspray→discharge/EXITstop. Readiness loss removes affected commands: losing sprayready pauses indexing/spray; losing ventready stops index/spray/vent and resets relevant nonretentive timing. Restoration resumes retained phase and full timing where reset — do not expect automatic total-cycle abort or freshSTART lockout absent from this fixture. Stop holds plant, clears commands/timers; Run resumes retained phase with available ready fixtures. Reset clears phase/input and reloads supported part; fresh repeat required. Plant spray eligibility additionally checks actual part position/ventilation; no measured airflow/coating quality claim. No counter.

## Evidence boundaries

Record input calibration, one complete normal cycle, stated unavailable condition, interruption/recovery, Reset and fresh repeat separately. Counters are absent in all four controllers. Hazard conditions above describe educational fixture semantics only, not real equipment safety. Classify a failure only from an actual matched scene/controller/UI observation; fixture policy and source expectations alone are not runtime proof.
