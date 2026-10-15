# Lab 11.7 - Multi-Conveyor Pallet Route help

Scene ID: `lab-11-07-multi-conveyor-pallet-route`  
Migrated source: `prototype/scenes/lab-11-07-multi-conveyor-pallet-route.plcscene`  
Scene contract: `res://scenes/migrated/lab-11-07-multi-conveyor-pallet-route.scene.json`

## Purpose

Several conveyor zones start only from a clear leading edge and stop together from a common stop request.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `infeed_blocked` | `BOOL` | **PC** | `False` |
| `handoff_1_blocked` | `BOOL` | **PC** | `False` |
| `zone_2_blocked` | `BOOL` | **PC** | `False` |
| `receiver_entry_blocked` | `BOOL` | **PC** | `False` |
| `route_position` | `REAL` | **SIM** | `-9.3` |
| `route_complete` | `BOOL` | **SIM** | `False` |
| `common_stop_request` | `BOOL` | **PC** | `False` |
| `zone_1_clear` | `BOOL` | **PC** | `False` |
| `zone_2_clear` | `BOOL` | **PC** | `False` |
| `zone_3_clear` | `BOOL` | **PC** | `False` |
| `zone_1_run` | `BOOL` | **PLC** | `False` |
| `zone_2_run` | `BOOL` | **PLC** | `False` |
| `zone_3_run` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle common stop request` | `toggle` | `common_stop_request` |
| `Toggle zone 1 clear` | `toggle` | `zone_1_clear` |
| `Toggle zone 2 clear` | `toggle` | `zone_2_clear` |
| `Toggle zone 3 clear` | `toggle` | `zone_3_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `zone_3_run` | `training_accessory_6` | `running` |
| `infeed_blocked` | `photoeye_4` | `photoeye` |
| `handoff_1_blocked` | `training_accessory_7` | `photoeye` |
| `zone_2_blocked` | `photoeye_5` | `photoeye` |
| `receiver_entry_blocked` | `photoeye_3` | `photoeye` |
| `zone_1_clear` | `switch_9` | `switch` |
| `zone_2_clear` | `switch_10` | `switch` |
| `zone_3_clear` | `switch_11` | `switch` |
| `zone_1_run` | `indicator_12` | `indicator` |
| `zone_2_run` | `indicator_13` | `indicator` |
| `zone_3_run` | `indicator_14` | `indicator` |
| `zone_1_run` | `conveyor_0` | `running` |
| `zone_2_run` | `conveyor_1` | `running` |
| `zone_3_run` | `conveyor_2` | `running` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `route_pallet` | `palletLoad` | Loaded route pallet |
| `conveyor_0` | `conveyor` | Multi-Conveyor Pallet Route conveyor |
| `conveyor_1` | `conveyor` | Multi-Conveyor Pallet Route conveyor |
| `conveyor_2` | `conveyor` | Multi-Conveyor Pallet Route conveyor |
| `photoeye_3` | `photoeye` | Multi-Conveyor Pallet Route photoeye |
| `photoeye_4` | `photoeye` | Multi-Conveyor Pallet Route photoeye |
| `photoeye_5` | `photoeye` | Multi-Conveyor Pallet Route photoeye |
| `training_accessory_6` | `trainingAccessory` | Multi-Conveyor Pallet Route - pallet roller conveyor zone |
| `training_accessory_7` | `photoeye` | Zone 1 to 2 handoff photoeye |
| `training_accessory_8` | `trainingAccessory` | Multi-Conveyor Pallet Route - common stop station |
| `switch_9` | `switch` | ZONE 1 CLEAR |
| `switch_10` | `switch` | ZONE 2 CLEAR |
| `switch_11` | `switch` | ZONE 3 CLEAR |
| `indicator_12` | `indicator` | Multi-Conveyor Pallet Route output indication |
| `indicator_13` | `indicator` | Multi-Conveyor Pallet Route output indication |
| `indicator_14` | `indicator` | Multi-Conveyor Pallet Route output indication |
| `zone_handoffs` | `sceneInstallation` | Supported conveyor zone handoffs |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Several conveyor zones start only from a clear leading edge and stop together from a common stop request.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- A blocked zone removes the affected run command while preserving a diagnosable handoff state.
