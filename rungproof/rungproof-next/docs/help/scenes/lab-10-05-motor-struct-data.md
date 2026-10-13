# Lab 10.5 - Motor STRUCT Data help

Scene ID: `lab-10-05-motor-struct-data`  
Migrated source: `prototype/scenes/lab-10-05-motor-struct-data.plcscene`  
Scene contract: `prototype/scenes/lab-10-05-motor-struct-data.plcscene`

## Purpose

The scene declares typed motor feedback and command STRUCT roots. Its numeric power and temperature fields are explicit user or QA fixture data, with validity flags and unspecified units; they are not measured hardware values or a physical motor model. Legacy BOOL inputs and commands remain compatible.

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

`motor_feedback` has BOOL fields `record_valid`, `temperature_valid`, `alarm_clear`, `power_valid`, and REAL fields `power`, `temperature`. All BOOLs start False; numeric values start zero and are hidden until their validity is true. The existing three operator switches mirror the first three fields. `motor_command` has BOOL fields `enable` and `record_ready`. Controller operands access declared fields, for example `motor_command.enable`; whole-record instructions and nested aggregates are unsupported.

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
| `motor_feedback.temperature` | `training_accessory_4` | `numericDisplay`, temperature_valid gate |
| `motor_feedback.power` | `training_accessory_5` | `numericDisplay`, power_valid gate |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `motor_0` | `motor` | Motor commanded by motor_enable |
| `machine_1` | `machine` | Motor STRUCT Data machine |
| `indicator_2` | `indicator` | Motor STRUCT Data indicator |
| `training_accessory_3` | `trainingAccessory` | Motor Record display - no live value |
| `training_accessory_4` | `trainingAccessory` | Temperature display - no live value |
| `training_accessory_5` | `trainingAccessory` | Struct Data display - no live value |
| `switch_6` | `switch` | Motor STRUCT Data operator input |
| `switch_7` | `switch` | Motor STRUCT Data operator input |
| `switch_8` | `switch` | Motor STRUCT Data operator input |
| `indicator_9` | `indicator` | Motor STRUCT Data output indication |

## Current operating workflow

Author or load controller logic before Run. Require motor_record_valid, temperature_valid, and alarm_clear for the documented validity condition; the controller owns motor_enable and record_ready. Verify that losing each permissive removes the commands, and that Stop and Reset clear them. For aggregate-only control, leave the legacy outputs False. Effective enable is legacy motor_enable OR motor_command.enable; effective readiness is legacy record_ready OR motor_command.record_ready. This OR projection does not overwrite either command source. The record display reports fixture validity, and numeric displays report declared data only when valid. No units or calibration are inferred.

## Current verification boundary

Backend declaration, member access, persistence and scene mapping have automated checks. The separate typed QA project is prepared by the motor aggregate audit. Native typed-project loading, readout readability and normal tag-schema authoring remain unverified; native acceptance of the schema editor remains pending. These gaps keep product acceptance open.

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

The scene declares typed motor feedback and command STRUCT roots. Its numeric power and temperature fields are explicit user or QA fixture data, with validity flags and unspecified units; they are not measured hardware values or a physical motor model. Legacy BOOL inputs and commands remain compatible.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- The motor record is accepted only when its required fields are valid and alarms are clear.

## Structured tag controls

In PLC tags, choose STRUCT to edit each field name, scalar type and typed initial value; use + STRUCT FIELD to add a field. Choose ARRAY to set scalar element type, lower bound and count, then edit individual initial values. Selecting an aggregate scene binding loads its declared schema and current fixture values. Apply Tag Changes commits a validated definition; invalid shapes or initial values leave the document unchanged. Constant member/element operands appear in contact, coil and numeric operand selectors with parent/type tooltips. Save/Open and Undo/Redo preserve the schema. These are implementation descriptions; current native usability/readability acceptance remains a separate check.
