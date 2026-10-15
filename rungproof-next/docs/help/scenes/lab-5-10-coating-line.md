# Lab 5.10 - Coating Line help

Scene ID: `lab-5-10-coating-line`  
Migrated source: `prototype/scenes/lab-5-10-coating-line.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-10-coating-line.scene.json`

## Purpose

A workpiece is indexed into a coating enclosure, sprayed for a timed interval, and discharged after ventilation.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `workpiece_at_station` | `BOOL` | **PC** | `False` |
| `workpiece_at_exit` | `BOOL` | **PC** | `False` |
| `start_request` | `BOOL` | **PC** | `False` |
| `spray_ready` | `BOOL` | **PC** | `False` |
| `ventilation_ready` | `BOOL` | **PC** | `False` |
| `motion_inhibited` | `BOOL` | **PC** | `False` |
| `spray_inhibited` | `BOOL` | **PC** | `False` |
| `travel_limited` | `BOOL` | **PC** | `False` |
| `workpiece_position` | `REAL` | **PC** | `-2` |
| `belt_speed` | `REAL` | **PC** | `0` |
| `index_run` | `BOOL` | **PLC** | `False` |
| `spray_enable` | `BOOL` | **PLC** | `False` |
| `vent_run` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `START cycle` | `pulse` | `start_request` |
| `Manual spray ready` | `toggle` | `spray_ready` |
| `Manual ventilation ready` | `toggle` | `ventilation_ready` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `spray_ready` | `switch_10` | `selector` |
| `ventilation_ready` | `switch_11` | `selector` |
| `index_run` | `indicator_4` | `indicator` |
| `spray_enable` | `indicator_12` | `indicator` |
| `vent_run` | `indicator_13` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Index conveyor with integral drive |
| `fan_2` | `fan` | Mounted exhaust fan (command display, not airflow feedback) |
| `photoeye_3` | `photoeye` | STATION beam at workpiece height |
| `photoeye_exit` | `photoeye` | EXIT beam on the supported discharge belt |
| `training_accessory_5` | `trainingAccessory` | Open-ended spray tunnel with inspection glazing |
| `training_accessory_6` | `trainingAccessory` | Roof-mounted existing spray gun |
| `training_accessory_7` | `trainingAccessory` | Metal workpiece on flat carrying fixture |
| `training_accessory_8` | `trainingAccessory` | Connected rear exhaust duct, damper and fan stand |
| `switch_9` | `switch` | Momentary cycle START |
| `switch_10` | `rotarySwitch` | Manual spray supply permissive |
| `switch_11` | `rotarySwitch` | Manual ventilation permissive |
| `indicator_4` | `indicator` | PLC index command |
| `indicator_12` | `indicator` | PLC spray command |
| `indicator_13` | `indicator` | PLC ventilation command |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A workpiece is indexed into a coating enclosure, sprayed for a timed interval, and discharged after ventilation.

### Start conditions

- Exercise opens blank; author a PLC program or explicitly open the ignored review reference.
- Manual SPRAY READY and VENT READY true, workpiece at its supported home.

### Normal sequence

- Fresh START indexes to actual STATION.
- Reference holds index and runs vent/spray for 2 s.
- Spray stops; ventilation continues for a 1 s purge, then index discharges to actual EXIT.
- One supported workpiece remains visible; Reset reloads home.

### Expected observations

- PART stays on the belt, spray is visible only over the actual stationed body with both readiness inputs and vent command.
- Fan/damper are symbolic actuator projections; no measured airflow or coating quality is claimed.
