# Lab 2.24 - Robot CNC Tending Cell help

Scene ID: `lab-2-24-robot-cnc`  
Migrated source: `prototype/scenes/lab-2-24-robot-cnc.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-24-robot-cnc.scene.json`

## Purpose

Offline reference: a joint-driven robot carries one blank through open CNC access, seats it on a supported vise, waits for a machining timer and delivers it to outfeed. Feedback-driven door/clamp interlocks and normal controller operation remain unverified.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `infeed_run` | `BOOL` | **PLC** | `False` |
| `outfeed_run` | `BOOL` | **PLC** | `False` |
| `blank_at_pickup` | `BOOL` | **PC** | `False` |
| `robot_run` | `BOOL` | **PLC** | `False` |
| `cnc_ready` | `BOOL` | **PC** | `True` |
| `cnc_run` | `BOOL` | **PLC** | `False` |
| `machining_complete` | `BOOL` | **PC** | `False` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start robot/CNC cycle` | `start` | `cnc-cycle` |
| `Stop cell safely` | `stop` | `` |
| `Reset CNC cell` | `reset` | `` |

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

Offline reference: a joint-driven robot carries one blank through open CNC access, seats it on a supported vise, waits for a machining timer and delivers it to outfeed. Feedback-driven door/clamp interlocks and normal controller operation remain unverified.

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
