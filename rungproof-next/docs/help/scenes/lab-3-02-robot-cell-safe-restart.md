# Lab 3.2 - Robot Cell Safe Restart help

Scene ID: `lab-3-02-robot-cell-safe-restart`  
Migrated source: `prototype/scenes/lab-3-02-robot-cell-safe-restart.plcscene`  
Scene contract: `res://scenes/migrated/lab-3-02-robot-cell-safe-restart.scene.json`

## Purpose

A robot cell requires a closed gate, reset edge, and ready status before motion may be requested.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `gate_closed` | `BOOL` | **PC** | `False` |
| `reset_complete` | `BOOL` | **PC** | `False` |
| `robot_ready` | `BOOL` | **PC** | `False` |
| `motion_request` | `BOOL` | **PC** | `False` |
| `robot_enable` | `BOOL` | **PLC** | `False` |
| `cell_ready` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle gate closed` | `toggle` | `gate_closed` |
| `Press reset (momentary)` | `pulse` | `reset_complete` |
| `Toggle robot ready` | `toggle` | `robot_ready` |
| `Press Start motion (momentary)` | `pulse` | `motion_request` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `gate_closed` | `switch_2` | `switch` |
| `reset_complete` | `switch_8` | `switch` |
| `robot_ready` | `switch_9` | `switch` |
| `robot_enable` | `indicator_3` | `indicator` |
| `cell_ready` | `indicator_10` | `indicator` |
| `robot_enable` | `robotArm_0` | `running` |
| `gate_closed` | `training_accessory_4` | `switch` |
| `motion_request` | `motion_request_station` | `switch` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `robotArm_0` | `robotArm` | Robot enable sweep |
| `machine_1` | `machine` | Enclosed CNC - static process equipment |
| `switch_2` | `switch` | Gate closed feedback |
| `indicator_3` | `indicator` | Robot enable command |
| `training_accessory_4` | `trainingAccessory` | Robot cell fence and hinged access gate |
| `training_accessory_5` | `trainingAccessory` | Gate sensor and moving coded actuator |
| `training_accessory_6` | `trainingAccessory` | Emergency-stop station - visual reference only |
| `training_accessory_7` | `trainingAccessory` | Robot controller cabinet |
| `switch_8` | `switch` | Momentary reset request |
| `switch_9` | `switch` | Robot ready feedback |
| `indicator_10` | `indicator` | Cell ready command |
| `motion_request_station` | `switch` | Separate robot motion Start request |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A robot cell requires a closed gate, reset edge, and ready status before motion may be requested.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- A restart request cannot enable motion until the gate is closed and the robot is ready.
