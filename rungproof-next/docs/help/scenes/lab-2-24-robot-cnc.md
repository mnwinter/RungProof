# Lab 2.24 - Robot CNC Tending Cell help

Scene ID: `lab-2-24-robot-cnc`  
Migrated source: `prototype/scenes/lab-2-24-robot-cnc.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-24-robot-cnc.scene.json`

## Purpose

A loaded learner controller explicitly commands robot tool targets, grip, access, clamp, spindle and belts. Actual solved joint/part poses and mechanism endpoints provide feedback. A separate standalone reference preview remains available; neither path proves hardware commissioning or robot safety.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `start_cnc_request` | `BOOL` | **PC** | `False` |
| `infeed_run` | `BOOL` | **PLC** | `False` |
| `outfeed_run` | `BOOL` | **PLC** | `False` |
| `blank_at_pickup` | `BOOL` | **PC** | `False` |
| `robot_run` | `BOOL` | **PLC** | `False` |
| `cnc_ready` | `BOOL` | **PC** | `True` |
| `cnc_run` | `BOOL` | **PLC** | `False` |
| `machining_complete` | `BOOL` | **PC** | `False` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Additive controller interface

All positions below are world metres in this simulator, not robot hardware coordinates.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `robot_target_x` | `REAL` | **PLC** | `0` |
| `robot_target_y` | `REAL` | **PLC** | `2.8` |
| `robot_target_z` | `REAL` | **PLC** | `-1.4` |
| `robot_grip_close` | `BOOL` | **PLC** | `False` |
| `cnc_access_open` | `BOOL` | **PLC** | `False` |
| `cnc_clamp_close` | `BOOL` | **PLC** | `False` |
| `robot_actual_x` | `REAL` | **PC** | `0` |
| `robot_actual_y` | `REAL` | **PC** | `2.8` |
| `robot_actual_z` | `REAL` | **PC** | `-1.4` |
| `robot_at_target` | `BOOL` | **PC** | `False` |
| `robot_grip_confirmed` | `BOOL` | **PC** | `False` |
| `cnc_access_is_open` | `BOOL` | **PC** | `False` |
| `cnc_access_is_closed` | `BOOL` | **PC** | `True` |
| `cnc_tool_clear` | `BOOL` | **PC** | `False` |
| `cnc_clamp_closed` | `BOOL` | **PC** | `False` |
| `cnc_clamp_open` | `BOOL` | **PC** | `True` |
| `stock_on_vise` | `BOOL` | **PC** | `False` |
| `stock_on_outfeed` | `BOOL` | **PC** | `False` |
| `robot_clear_of_enclosure` | `BOOL` | **PC** | `True` |
| `cnc_cut_permitted` | `BOOL` | **PC** | `False` |
| `cell_motion_inhibited` | `BOOL` | **PC** | `False` |
| `part_at_exit` | `BOOL` | **PC** | `False` |
| `stock_actual_x` | `REAL` | **PC** | `-6.1` |
| `stock_actual_y` | `REAL` | **PC** | `0.9` |
| `stock_actual_z` | `REAL` | **PC** | `0.55` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start robot/CNC cycle` | `start` | Loaded controller: pulse `start_cnc_request` (legacy preview: `cnc-cycle`) |
| `Stop cell safely` | `stop` | `` |
| `Reset CNC cell` | `reset` | `` |

With a loaded ladder controller, each Start action pulses the named PC-owned BOOL request binding; the learner program decides actuator commands. A legacy preview sequence name does not imply that the loaded controller automatically executes that sequence.

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `infeed_run` | `cnc_infeed` | `running` |
| `outfeed_run` | `cnc_outfeed` | `running` |
| `blank_at_pickup` | `cnc_infeed_sensor` | `photoeye` |
| `robot_run` | `cnc_robot` | `running` |
| `cnc_run` | `cnc_machine` | `running` |
| `status_color` | `cnc_cell_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `cnc_infeed` | `conveyor` | CNC infeed conveyor |
| `cnc_outfeed` | `conveyor` | CNC outfeed conveyor |
| `cnc_workpiece` | `box` | Machining blank |
| `cnc_robot` | `robotArm` | CNC tending robot |
| `cnc_machine` | `machine` | CNC machining center |
| `cnc_infeed_sensor` | `photoeye` | Blank pickup sensor |
| `cnc_cell_start` | `switch` | Start CNC tending |
| `cnc_cell_status` | `indicator` | CNC cell status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `infeed_run` | `False` |
| `outfeed_run` | `False` |
| `robot_run` | `False` |
| `cnc_run` | `False` |
| `status_color` | `red` |

## Machine guide

A loaded learner controller explicitly commands robot tool targets, grip, access, clamp, spindle and belts. Actual solved joint/part poses and mechanism endpoints provide feedback. A separate standalone reference preview remains available; neither path proves hardware commissioning or robot safety.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command infeed_run, outfeed_run, robot_run, cnc_run.

### Normal sequence

- infeed blank
- pickup
- machining
- machine complete
- unload
- outfeed
- ready

### Expected observations

- Preview action opens access and retracts the tool before the gripped stock crosses the opening.
- Released stock remains on the vise support; the robot parks and access closes before the machining timer.
- The same stock is regripped and delivered to outfeed. Reset is required after completion or interruption.
- Timed reference states do not prove sensed completion, clamp feedback or PLC interlock operation.


## Loaded-controller mechanics and learner sequence

`robot_run` is motion enable only; `robot_target_x/y/z` choose one tool target. No hidden recipe advances targets. Actual IK must solve the next commanded pose before `robot_actual_x/y/z` and `robot_at_target` change. `stock_actual_x/y/z` report actual blank position, so the learner can target the blank where the pickup beam really stops it. The0.4m-long blank interrupts the beam before its centre reaches beam X; do not assume fixed pickup centre coordinates.

`robot_grip_close` requests maintained grip; confirmation requires actual tool/load alignment. Release is admitted only at the supported vise or outfeed datum. Unsupported release retains the stock and reports `cell_motion_inhibited`. Explicit access and clamp commands move the visible doors/tool-clearance and movable jaw; measured endpoints govern feedback. Clamp closure requires actual seated stock and robot clear. Enclosure entry requires access fully open. Closing access requires the robot/held stock clear. Commands are never rewritten by these guards.

`cnc_ready` retains its initial cell-ready meaning; it is not permission to cut. `cnc_cut_permitted` requires actual stock on vise, actual clamp closed, actual access closed and actual robot clearance. `cnc_run` rotates/cuts only with those measured conditions. `machining_complete` latches after two seconds of permitted spindle exposure (explicit simulation parameter). It clears on Reset. Opening access/unclamping while a permitted spindle request is active is inhibited.

The V3 learner ladder owns each approach/corridor/lower/grip/lift/transfer/seat/release/withdraw/park target, then clamp/access/spindle, and every reverse unload target. It waits for actual feedback before changing phases; no scene sequence is started from controller output. Reset is required after a completed/interrupted single-stock cycle. Stop freezes physical mechanisms and removes commands; the learner phase may remain until Reset. Old projects load with additive SceneIO tags; old `robot_run`-only programs cannot invent targets, grip or access/clamp decisions and therefore do not perform a full tending cycle.

Model parameters are0.75m/s belts,0.8m/s Cartesian tool speed,1second access travel,0.5second clamp travel,2seconds permitted cutting. They are educational simulation choices, not robot payload, speed, collision, machine guarding or safety ratings. The actual imported robot joint solver and supported stock/fixture geometry are used, but workspace/front-access constraints are not a general collision/safety certification.
