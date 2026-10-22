# Lab 2.16 - Fixture-Safe Drill Station help

Scene ID: `lab-2-16-safe-drill`  
Migrated source: `prototype/scenes/lab-2-16-safe-drill.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-16-safe-drill.scene.json`

## Purpose

A guarded drill cycle requires a present workpiece and both hand-request inputs before the spindle can descend.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `start_drill_request` | `BOOL` | **PC** | `False` |
| `left_hand_request` | `BOOL` | **PC** | `False` |
| `right_hand_request` | `BOOL` | **PC** | `False` |
| `workpiece_present` | `BOOL` | **PC** | `True` |
| `drill_run` | `BOOL` | **PLC** | `False` |
| `drill_feed_down` | `BOOL` | **PLC** | `False` |
| `drill_retract_up` | `BOOL` | **PLC** | `False` |
| `drill_motion_inhibited` | `BOOL` | **PC** | `False` |
| `drill_at_bottom` | `BOOL` | **PC** | `False` |
| `drill_at_top` | `BOOL` | **PC** | `True` |
| `cycle_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle left hand request` | `togglePoint` | `left_hand_request` |
| `Toggle right hand request` | `togglePoint` | `right_hand_request` |
| `Start guarded drill cycle` | `start` | Loaded controller: pulse `start_drill_request` (legacy preview: `drill-cycle`) |
| `Stop spindle and hold` | `stop` | `` |
| `Reset drill` | `reset` | `` |

With a loaded ladder controller, each Start action pulses the named PC-owned BOOL request binding; the learner program decides actuator commands. A legacy preview sequence name does not imply that the loaded controller automatically executes that sequence.

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `left_hand_request` | `left_hand_button` | `switch` |
| `right_hand_request` | `right_hand_button` | `switch` |
| `drill_run` | `safe_drill` | `running` |
| `cycle_color` | `drill_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `safe_drill` | `drillPress` | Guarded drill press |
| `drill_workpiece` | `box` | Clamped workpiece |
| `left_hand_button` | `switch` | Left hand request |
| `right_hand_button` | `switch` | Right hand request |
| `drill_cycle_button` | `switch` | Initiate guarded cycle |
| `drill_status` | `indicator` | Drill status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `drill_run` | `False` |
| `cycle_color` | `red` |

## Machine guide

A guarded drill cycle requires a present workpiece and both hand-request inputs before the spindle can descend.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command drill_run.

### Normal sequence

- spindle start
- drilling
- bottom dwell
- retracting
- cycle complete

### Expected observations

- Start is blocked without both requests; a valid cycle drills, retracts, and stops at top.


## Explicit loaded-controller drill interface

`drill_run` drives spindle rotation only. `drill_feed_down` and `drill_retract_up` command separate feed directions; spindle ON never initiates a hidden cycle. New outputs are added to SceneIO when an old learner project opens, but old one-output programs require explicit feed/retract ladder logic to move.

The simulated axis has a245mm stroke and1.4-second full travel (simulation parameters). Downfeed requires actual spindle command, both hand inputs and workpiece present. Losing a hand or workpiece inhibits descent immediately. Explicit retract can withdraw without spindle, hands or workpiece; this is a documented educational actuator contract, not a safety-rated circuit. Both direction commands TRUE or both FALSE hold position. `drill_motion_inhibited` reports a blocked descent or conflicting commands without changing PLC outputs. Top/bottom feedback follows actual position.

The learner controller owns spindle spinup, feed until actual bottom, dwell, explicit retract until actual top, and spindle OFF. Stop freezes motion and removes output commands. Reset returns authored home. No automatic return or automatic restart after permissive loss is supplied. A normal test fixture uses0.35-second spinup/dwell as explicit QA timing choices; these are not equipment ratings.
