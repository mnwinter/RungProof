# Lab 2.18 - Shipping Pallet Accumulation help

Scene ID: `lab-2-18-pallet-pickup`  
Migrated source: `prototype/scenes/lab-2-18-pallet-pickup.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-18-pallet-pickup.scene.json`

## Purpose

Automatic mode advances a loaded shipping pallet to the pickup photoeye; a separate jog sequence demonstrates manual positioning.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `auto_start_request` | `BOOL` | **PC** | `False` |
| `manual_jog_request` | `BOOL` | **PC** | `False` |
| `auto_mode` | `BOOL` | **PC** | `True` |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `pickup_sensor` | `BOOL` | **PC** | `False` |
| `pallet_position` | `REAL` | **SIM** | `0` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle auto mode` | `togglePoint` | `auto_mode` |
| `Start automatic travel` | `start` | `automatic` |
| `Jog pallet one increment` | `start` | `manual` |
| `Stop conveyor` | `stop` | `` |
| `Reset pallet` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `auto_mode` | `pickup_mode` | `selector` |
| `conveyor_run` | `pickup_conveyor` | `running` |
| `pickup_sensor` | `pickup_end_sensor` | `photoeye` |
| `status_color` | `pickup_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `pickup_conveyor` | `conveyor` | Shipping pallet conveyor |
| `shipping_pallet` | `palletLoad` | Loaded shipping pallet |
| `pickup_end_sensor` | `photoeye` | Forklift pickup sensor |
| `pickup_mode` | `rotarySwitch` | Auto / manual selector |
| `auto_start` | `switch` | Automatic start |
| `manual_jog` | `switch` | Manual jog |
| `pickup_status` | `indicator` | Pickup conveyor status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyor_run` | `False` |
| `status_color` | `red` |

## Machine guide

Automatic mode advances a loaded shipping pallet to the pickup photoeye; a separate jog sequence demonstrates manual positioning.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command conveyor_run.

### Normal sequence

- automatic travel
- pickup position

### Expected observations

- Auto stops at the pickup sensor; manual jog moves only one bounded increment.
