# Lab 6.8 - Timed Hand-Dryer help

Scene ID: `lab-6-08-hand-dryer`  
Migrated source: `prototype/scenes/lab-6-08-hand-dryer.plcscene`  
Scene contract: `res://scenes/migrated/lab-6-08-hand-dryer.scene.json`

## Purpose

Hand presence starts a PLC-timed drying interval with blower, heater and live remaining seconds. Illustrated commands are not measured airflow or temperature.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `hands_inserted` | `BOOL` | **PC** | `False` |
| `hands_present` | `BOOL` | **PC** | `False` |
| `heater_inhibited` | `BOOL` | **PC** | `False` |
| `blower_run` | `BOOL` | **PLC** | `False` |
| `heater_enable` | `BOOL` | **PLC** | `False` |
| `remaining_seconds` | `REAL` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Insert / withdraw hands` | `toggle` | `hands_inserted` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `hands_inserted` | `switch_7` | `switch` |
| `blower_run` | `indicator_2` | `indicator` |
| `heater_enable` | `indicator_9` | `indicator` |
| `remaining_seconds` | `training_accessory_6` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `fan_0` | `fan` | Timed Hand-Dryer fan |
| `indicator_2` | `indicator` | BLOWER command |
| `training_accessory_3` | `trainingAccessory` | Timed Hand-Dryer - hand-presence sensor |
| `training_accessory_4` | `trainingAccessory` | Timed Hand-Dryer - air outlet |
| `training_accessory_5` | `trainingAccessory` | Timed Hand-Dryer - heating element |
| `training_accessory_6` | `trainingAccessory` | Timed Hand-Dryer - progress display |
| `switch_7` | `switch` | Insert / withdraw hands |
| `indicator_9` | `indicator` | HEATER command |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Hand presence starts a PLC-timed drying interval with blower, heater and live remaining seconds. Illustrated commands are not measured airflow or temperature.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Insert hands; a user-authored PLC program starts its timer and outputs.
- The PLC calculates remaining seconds; completion removes outputs.
- Withdraw and reinsert to rearm.

### Expected observations

- The mounted fan and heater project eligible commands.
- The readout displays PLC-owned remaining_seconds; the scene does not manufacture a timer result.
