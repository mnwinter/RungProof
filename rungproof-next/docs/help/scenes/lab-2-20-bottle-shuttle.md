# Lab 2.20 - Bottle Shuttle Conveyor help

Scene ID: `lab-2-20-bottle-shuttle`  
Migrated source: `prototype/scenes/lab-2-20-bottle-shuttle.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-20-bottle-shuttle.scene.json`

## Purpose

A reusable bottle travels to a right-hand sensor, reverses, returns to the left sensor, and stops.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `motor_run` | `BOOL` | **PLC** | `False` |
| `motor_direction` | `INT` | **PLC** | `0` |
| `left_sensor_active` | `BOOL` | **PC** | `True` |
| `right_sensor_active` | `BOOL` | **PC** | `False` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start bottle shuttle` | `start` | `round-trip` |
| `Stop shuttle` | `stop` | `` |
| `Reset bottle` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `motor_run` | `shuttle_conveyor` | `running` |
| `left_sensor_active` | `left_sensor` | `photoeye` |
| `right_sensor_active` | `right_sensor` | `photoeye` |
| `status_color` | `shuttle_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `shuttle_conveyor` | `conveyor` | Bottle shuttle conveyor |
| `shuttle_bottle` | `box` | Reusable process bottle |
| `left_sensor` | `photoeye` | Left end sensor |
| `right_sensor` | `photoeye` | Right end sensor |
| `shuttle_start` | `switch` | Start shuttle |
| `shuttle_status` | `indicator` | Shuttle status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `motor_run` | `False` |
| `motor_direction` | `0` |
| `status_color` | `red` |

## Machine guide

A reusable bottle travels to a right-hand sensor, reverses, returns to the left sensor, and stops.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command motor_run, motor_direction.

### Normal sequence

- travel right
- right detected
- travel left
- left detected

### Expected observations

- Right sensor reverses travel; left sensor stops the completed round trip.
