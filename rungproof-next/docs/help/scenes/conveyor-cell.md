# Conveyor Inspection Cell help

Scene ID: `conveyor-cell`  
Migrated source: `prototype/scenes/conveyor-cell.json`  
Scene contract: `res://scenes/migrated/conveyor-cell.scene.json`

## Purpose

Three reusable cartons circulate on a motor-driven conveyor. The through-beam photoeye changes state as each carton crosses it.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `estop_ok` | `BOOL` | **SIM** | `True` |
| `photoeye_blocked` | `BOOL` | **PC** | `False` |
| `conveyor_speed` | `REAL` | **SIM** | `0` |
| `parts_completed` | `DINT` | **SIM** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start / stop inspection conveyor` | `toggle` | `conveyor_run` |
| `Emergency stop conveyor` | `emergencyStop` | `estop_ok` |
| `Reset emergency stop` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `photoeye_blocked` | `inspection_photoeye` | `photoeye` |
| `conveyor_run` | `main_conveyor` | `running` |
| `estop_ok` | `main_conveyor` | `estopPermissive` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `main_conveyor` | `conveyor` | Main inspection conveyor |
| `carton_1` | `box` | Carton 1 |
| `carton_2` | `box` | Carton 2 |
| `carton_3` | `box` | Carton 3 |
| `inspection_photoeye` | `photoeye` | Inspection photoeye |
| `operator_station` | `switch` | Conveyor start station |
| `cell_stacklight` | `indicator` | Cell stack light |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyor_run` | `False` |
