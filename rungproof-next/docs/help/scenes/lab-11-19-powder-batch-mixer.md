# Lab 11.19 - Powder Batch Mixer help

Scene ID: `lab-11-19-powder-batch-mixer`  
Migrated source: `prototype/scenes/lab-11-19-powder-batch-mixer.plcscene`  
Scene contract: `res://scenes/migrated/lab-11-19-powder-batch-mixer.scene.json`

## Purpose

Batch-control exercise with manual recipe, dose-complete and mixer-ready inputs. PLC outputs drive indicators; equipment is an illustrative static layout.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `recipe_valid` | `BOOL` | **PC** | `False` |
| `dose_complete` | `BOOL` | **PC** | `False` |
| `mixer_ready` | `BOOL` | **PC** | `False` |
| `dose_run` | `BOOL` | **PLC** | `False` |
| `mixer_run` | `BOOL` | **PLC** | `False` |
| `discharge_valve_open` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle recipe valid` | `toggle` | `recipe_valid` |
| `Toggle dose complete` | `toggle` | `dose_complete` |
| `Toggle mixer ready` | `toggle` | `mixer_ready` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `recipe_valid` | `switch_11` | `switch` |
| `dose_complete` | `switch_12` | `switch` |
| `mixer_ready` | `switch_13` | `switch` |
| `dose_run` | `indicator_5` | `indicator` |
| `mixer_run` | `indicator_14` | `indicator` |
| `discharge_valve_open` | `indicator_15` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `tank_0` | `tank` | Powder Batch Mixer tank |
| `tank_1` | `tank` | Powder Batch Mixer tank |
| `tank_2` | `tank` | Powder Batch Mixer tank |
| `motor_3` | `motor` | Powder Batch Mixer motor |
| `valve_4` | `valve` | Powder Batch Mixer valve |
| `indicator_5` | `indicator` | Powder Batch Mixer indicator |
| `training_accessory_6` | `trainingAccessory` | Powder Batch Mixer - bulk powder hopper |
| `training_accessory_7` | `trainingAccessory` | Powder Batch Mixer - slide-gate feeder |
| `training_accessory_8` | `trainingAccessory` | Powder Batch Mixer - load cell |
| `training_accessory_9` | `trainingAccessory` | Powder Batch Mixer - recipe selector |
| `training_accessory_10` | `trainingAccessory` | Powder Batch Mixer - powder discharge chute |
| `switch_11` | `switch` | Powder Batch Mixer operator input |
| `switch_12` | `switch` | Powder Batch Mixer operator input |
| `switch_13` | `switch` | Powder Batch Mixer operator input |
| `indicator_14` | `indicator` | Powder Batch Mixer output indication |
| `indicator_15` | `indicator` | Powder Batch Mixer output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Practice batch sequencing with symbolic inputs and output indicators. The separate equipment models do not simulate powder transfer, weighing or mixing. Tank diameter/height size the cylindrical shell; attachments remain proportional.

### Start conditions

- Author or load a controller program for this exercise; no batch reference ladder is supplied.
- The local recipe_valid, dose_complete and mixer_ready inputs begin false.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- Input actions toggle recipe_valid, dose_complete and mixer_ready.
- PLC output indicators display dose_run, mixer_run and discharge_valve_open from the user's controller.
- A correct controller should enforce dosing before mixing and discharge after batch completion; the initial-safe-state case alone does not prove that sequence.
