# Lab 3.1 - Guarded Pallet Transfer help

Scene ID: `lab-3-01-guarded-pallet-transfer`  
Migrated source: `prototype/scenes/lab-3-01-guarded-pallet-transfer.plcscene`  
Scene contract: `res://scenes/migrated/lab-3-01-guarded-pallet-transfer.scene.json`

## Purpose

A pallet transfer lane runs only while the access protection and clear-path inputs agree.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `guard_closed` | `BOOL` | **PC** | `False` |
| `entry_clear` | `BOOL` | **PC** | `False` |
| `exit_clear` | `BOOL` | **PC** | `False` |
| `transfer_run` | `BOOL` | **PLC** | `False` |
| `transfer_permissive` | `BOOL` | **PLC** | `False` |
| `carton_position` | `REAL` | **PC** | `-5.9` |
| `entry_carton_present` | `BOOL` | **PC** | `False` |
| `exit_carton_present` | `BOOL` | **PC** | `False` |
| `transfer_complete` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle guard closed` | `toggle` | `guard_closed` |
| `Toggle entry clear` | `toggle` | `entry_clear` |
| `Toggle exit clear` | `toggle` | `exit_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `guard_closed` | `switch_4` | `switch` |
| `entry_clear` | `switch_9` | `switch` |
| `exit_clear` | `switch_10` | `switch` |
| `transfer_run` | `indicator_5` | `indicator` |
| `transfer_permissive` | `indicator_11` | `indicator` |
| `transfer_run` | `conveyor_0` | `running` |
| `entry_carton_present` | `photoeye_2` | `photoeye` |
| `exit_carton_present` | `photoeye_3` | `photoeye` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Guarded Pallet Transfer conveyor |
| `box_1` | `box` | Guarded Pallet Transfer box |
| `photoeye_2` | `photoeye` | Guarded Pallet Transfer photoeye |
| `photoeye_3` | `photoeye` | Guarded Pallet Transfer photoeye |
| `switch_4` | `switch` | Guarded Pallet Transfer switch |
| `indicator_5` | `indicator` | Guarded Pallet Transfer indicator |
| `training_accessory_6` | `trainingAccessory` | Guarded Pallet Transfer - safety light-curtain pair |
| `training_accessory_7` | `trainingAccessory` | Guarded Pallet Transfer - guarded access gate |
| `training_accessory_8` | `trainingAccessory` | Guarded Pallet Transfer - safety relay/status beacon |
| `switch_9` | `switch` | Guarded Pallet Transfer operator input |
| `switch_10` | `switch` | Guarded Pallet Transfer operator input |
| `indicator_11` | `indicator` | Guarded Pallet Transfer output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A pallet transfer lane runs only while the access protection and clear-path inputs agree.

### Start conditions

- Load or create a compatible offline controller program; Run alone does not supply lesson logic.
- Set the manual guard_closed, entry_clear and exit_clear fixtures true to permit the review transfer.

### Normal sequence

- PLC logic evaluates the three manual permissive inputs and owns transfer_permissive and transfer_run.
- The prescribed carton crosses the entry and exit optical routes, reporting separate PC occupancy feedback.
- At the supported exit, transfer_complete becomes true; the explicit review PLC removes transfer_run.

### Expected observations

- The carton remains supported and visible throughout travel and at completion.
- Only all three manual permissives allow the review PLC transfer; completed travel remains stopped until Reset.
