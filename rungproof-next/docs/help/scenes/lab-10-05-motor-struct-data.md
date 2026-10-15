# Lab 10.5 - Motor STRUCT Data help

Scene ID: `lab-10-05-motor-struct-data`  
Migrated source: `prototype/scenes/lab-10-05-motor-struct-data.plcscene`  
Scene contract: `res://scenes/migrated/lab-10-05-motor-struct-data.scene.json`

## Purpose

Typed motor feedback and command STRUCT exercise with legacy BOOL compatibility. Numeric power/temperature data are explicit user or fixture values without a physical process model or specified units; author or load controller logic before Run.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `motor_record_valid` | `BOOL` | **PC** | `False` |
| `temperature_valid` | `BOOL` | **PC** | `False` |
| `alarm_clear` | `BOOL` | **PC** | `False` |
| `motor_enable` | `BOOL` | **PLC** | `False` |
| `record_ready` | `BOOL` | **PLC** | `False` |
| `motor_feedback` | `STRUCT` | **PC** | `{'record_valid': False, 'temperature': 0.0, 'power': 0.0, 'temperature_valid': False, 'alarm_clear': False, 'power_valid': False}` |
| `motor_command` | `STRUCT` | **PLC** | `{'record_ready': False, 'enable': False}` |
| `motor_effective_enable` | `BOOL` | **SIM** | `False` |
| `motor_effective_ready` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle motor record valid` | `toggle` | `motor_record_valid` |
| `Toggle temperature valid` | `toggle` | `temperature_valid` |
| `Toggle alarm clear` | `toggle` | `alarm_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `motor_record_valid` | `switch_6` | `switch` |
| `temperature_valid` | `switch_7` | `switch` |
| `alarm_clear` | `switch_8` | `switch` |
| `motor_effective_enable` | `indicator_2` | `indicator` |
| `motor_effective_ready` | `indicator_9` | `indicator` |
| `motor_effective_enable` | `motor_0` | `running` |
| `motor_feedback.temperature` | `training_accessory_4` | `numericDisplay` |
| `motor_feedback.power` | `training_accessory_5` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `motor_0` | `motor` | Motor commanded by motor_enable |
| `machine_1` | `machine` | Motor STRUCT Data machine |
| `indicator_2` | `indicator` | Motor STRUCT Data indicator |
| `training_accessory_3` | `trainingAccessory` | Motor record fixture display |
| `training_accessory_4` | `trainingAccessory` | Temperature fixture display - units unspecified |
| `training_accessory_5` | `trainingAccessory` | Power fixture display - units unspecified |
| `switch_6` | `switch` | Record Valid |
| `switch_7` | `switch` | Temp Valid |
| `switch_8` | `switch` | Alarm Clear |
| `indicator_9` | `indicator` | Motor STRUCT Data output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Typed motor feedback and command STRUCT exercise with legacy BOOL compatibility. Numeric power/temperature data are explicit user or fixture values without a physical process model or specified units; author or load controller logic before Run.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- The motor record is accepted only when its required fields are valid and alarms are clear.
