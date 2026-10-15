# Lab 9.11 - Pallet Count Function help

Scene ID: `lab-9-11-pallet-counting`  
Migrated source: `prototype/scenes/lab-9-11-pallet-counting.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-11-pallet-counting.scene.json`

## Purpose

A reusable block counts manually validated pallet detection edges and publishes the current count and batch validity.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `pallet_detected` | `BOOL` | **PC** | `False` |
| `pallet_type_valid` | `BOOL` | **PC** | `False` |
| `count_request` | `BOOL` | **PC** | `False` |
| `pallet_count_valid` | `BOOL` | **PLC** | `False` |
| `pallet_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle pallet detected` | `toggle` | `pallet_detected` |
| `Toggle pallet type valid` | `toggle` | `pallet_type_valid` |
| `Toggle count request` | `toggle` | `count_request` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `pallet_detected` | `switch_7` | `switch` |
| `pallet_type_valid` | `switch_8` | `switch` |
| `count_request` | `switch_9` | `switch` |
| `pallet_count_valid` | `indicator_3` | `indicator` |
| `pallet_count` | `training_accessory_6` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Pallet Count Function conveyor |
| `box_1` | `box` | Pallet Count Function box |
| `machine_2` | `machine` | Pallet Count Function machine |
| `indicator_3` | `indicator` | Pallet Count Function indicator |
| `training_accessory_4` | `trainingAccessory` | Pallet Count Function - pallet load |
| `training_accessory_5` | `trainingAccessory` | Pallet Count Function - optical profile fixture (static) |
| `training_accessory_6` | `trainingAccessory` | Pallet Count Function - current count display |
| `switch_7` | `switch` | Pallet Count Function operator input |
| `switch_8` | `switch` | Pallet Count Function operator input |
| `switch_9` | `switch` | Pallet Count Function operator input |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A reusable block counts manually validated pallet detection edges and publishes the current count and batch validity.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Enable pallet type valid and count request.
- Toggle pallet detected off and on for each new valid event. The DINT count and 3D display advance once per rising detection edge.
- The batch-valid lamp turns on at five valid events. Invalid events are not counted; held detection does not recount.

### Expected observations

- The 3D readout and DINT pallet_count match the accumulated valid detection events. The batch-valid bit requires at least five events and both permissives.
- Stop clears the numeric output image to zero while retaining internal counter memory. Run republishes the retained count on its next scan. Reset clears inputs, memory and outputs.
- Pallet type is a manually supplied validity bit; the optical fixture does not classify pallets or generate detection events.
