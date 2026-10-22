# Missing START-binding diagnostic probes

These disposable probes isolate the normal UI command-binding guard after a valid controller is loaded and RUNNING. They are not normal solutions or reference controllers. No process sequence, PC START binding, or controllerStartBinding is invented.

Each schema 9 project contains exact PC/PLC SceneIO names, types, ownership, and initial values from its source scene; SIM-only state is excluded. One unbound BOOL Memory qa_probe_false (initial false, no writes) feeds one ordinary NO contact and one Assign BOOL PLC coil. Other commands retain declared initial values. No DBs or instances. All six scenes have a BOOL PLC command, so no numeric MOVE was needed.

Use the real UI to select the matching scene, load its probe, Run, verify scans advance, then click the physical controls listed below. Expected diagnostic for each mapped START is: Action blocked · this scene action has no command binding in the loaded ladder program. Read Engineering > right inspector > Health > EVENT HISTORY; changing views overwrites the bottom status but retains event history. Stationary equipment is expected and is not evidence of this guard.

The six new files cover eight START actions. Together with existing Lab 2.11 (inbound_start / Start inbound cycle; start-cycle / Start inbound cycle), there are seven scenes and nine candidate missing-binding actions. Lab 2.11 guard was observed by the parent tester; this artifact makes no runtime claim for these six files.

Lab 2.16 declares both-hand and workpiece/top prerequisites. Main first checks current scene permissives, before the controller binding guard. Initial left/right hand requests are FALSE, so an initial START is expected to report scene permissives are not satisfied. This rejection only records feedback; it does not itself latch a drill trip or mutate points. Through the normal physical controls toggle left/right TRUE, verify workpiece_present and drill_at_top TRUE, then click DRILL CYCLE to test the binding guard. No forcing or fabricated requests. APP RESET is optional for a clean baseline and restores both requests FALSE and stops the controller; APP RUN and reapply both requests afterward. This scene has no physical Reset control; APP RESET or App Playback > Reset simulator and scene is exposed. Observe the actual UI message and do not infer success/failure from drill movement.

| Scene | Probe filename | Physical equipment / label | Action ID / label | Held FALSE |
|---|---|---|---|---|
| lab-2-12-assembly-lift | probe-lab-2-12-start-binding.rpproj.json | raise_button / Raise lift | raise-lift / Raise to work height | lift_up |
| lab-2-12-assembly-lift | probe-lab-2-12-start-binding.rpproj.json | lower_button / Lower lift | lower-lift / Lower to load height | lift_up |
| lab-2-13-coolant-jug-fill | probe-lab-2-13-start-binding.rpproj.json | fill_start / Start fill cycle | start-fill / Start jug fill | conveyor_run |
| lab-2-16-safe-drill | probe-lab-2-16-start-binding.rpproj.json | drill_cycle_button / Initiate guarded cycle | start-drill / Start guarded drill cycle | drill_run |
| lab-2-19-service-door | probe-lab-2-19-start-binding.rpproj.json | door_open_button / Open door | open-door / Open service door | motor_open |
| lab-2-19-service-door | probe-lab-2-19-start-binding.rpproj.json | door_close_button / Close door | close-door / Close service door | motor_open |
| lab-2-23-parcel-sorter | probe-lab-2-23-start-binding.rpproj.json | sorter_start / Start parcel batch | start-sort / Sort three-parcel batch | conveyors_run |
| lab-2-24-robot-cnc | probe-lab-2-24-start-binding.rpproj.json | cnc_cell_start / Start CNC tending | start-cnc / Start robot/CNC cycle | infeed_run |


Verification performed here: JSON parse, exact source declaration comparison, unique IDs, one false-contact/coil network, no DBs/instances. No importer, compiler, runtime, process, or native input was executed.

