# Lab 10.3 - Vision Package Sorter help

Scene ID: `lab-10-03-vision-package-sorter`  
Migrated source: `prototype/scenes/lab-10-03-vision-package-sorter.plcscene`  
Scene contract: `res://scenes/migrated/lab-10-03-vision-package-sorter.scene.json`

## Purpose

A vision result routes packages to one of four destination lanes using a conveyor and actuator handshake.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `package_present` | `BOOL` | **PC** | `False` |
| `vision_result_valid` | `BOOL` | **PC** | `False` |
| `destination_clear` | `BOOL` | **PC** | `False` |
| `sort_conveyor_run` | `BOOL` | **PLC** | `False` |
| `diverter_enable` | `BOOL` | **PLC** | `False` |
| `vision_class` | `INT` | **PC** | `1` |
| `sort_complete` | `BOOL` | **PC** | `False` |
| `sort_latched_class` | `INT` | **SIM** | `0` |
| `sort_phase` | `STRING` | **SIM** | `Awaiting` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Select simulated vision class (1-4)` | `cycle` | `vision_class` |
| `Toggle package present` | `toggle` | `package_present` |
| `Toggle vision result valid` | `toggle` | `vision_result_valid` |
| `Toggle destination clear` | `toggle` | `destination_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `vision_class` | `training_accessory_6` | `numericDisplay` |
| `package_present` | `switch_9` | `switch` |
| `vision_result_valid` | `switch_10` | `switch` |
| `destination_clear` | `switch_11` | `switch` |
| `sort_conveyor_run` | `indicator_4` | `indicator` |
| `diverter_enable` | `indicator_12` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Vision Package Sorter conveyor |
| `box_1` | `box` | Vision Package Sorter box |
| `photoeye_2` | `photoeye` | Vision Package Sorter photoeye |
| `rotaryTable_3` | `rotaryTable` | Vision Package Sorter rotary Table |
| `indicator_4` | `indicator` | Vision Package Sorter indicator |
| `training_accessory_5` | `trainingAccessory` | Vision Package Sorter - industrial vision camera |
| `training_accessory_6` | `trainingAccessory` | Vision Package Sorter - package-class result display |
| `switch_9` | `switch` | Vision Package Sorter operator input |
| `switch_10` | `switch` | Vision Package Sorter operator input |
| `switch_11` | `switch` | Vision Package Sorter operator input |
| `indicator_12` | `indicator` | Vision Package Sorter output indication |
| `sorter_handoff_bridge` | `trainingAccessory` | Supported infeed-to-parcel-table bridge |
| `destination_lane_1` | `conveyor` | Destination lane 1 |
| `destination_lane_2` | `conveyor` | Destination lane 2 |
| `destination_lane_3` | `conveyor` | Destination lane 3 |
| `destination_lane_4` | `conveyor` | Destination lane 4 |
| `sorter_outgoing_bridge_1` | `trainingAccessory` | Supported table-to-lane 1 handoff |
| `sorter_outgoing_bridge_2` | `trainingAccessory` | Supported table-to-lane 2 handoff |
| `sorter_outgoing_bridge_3` | `trainingAccessory` | Supported table-to-lane 3 handoff |
| `sorter_outgoing_bridge_4` | `trainingAccessory` | Supported table-to-lane 4 handoff |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A vision result routes packages to one of four destination lanes using a conveyor and actuator handshake.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- Sorting is enabled only for a present package with a valid vision result and clear destination.
