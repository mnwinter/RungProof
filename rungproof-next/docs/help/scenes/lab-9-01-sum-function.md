# Lab 9.1 - Sum Function Block help

Scene ID: `lab-9-01-sum-function`  
Migrated source: `prototype/scenes/lab-9-01-sum-function.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-01-sum-function.scene.json`

## Purpose

Select A and B, confirm both input-valid flags, and request a sum. The numeric readouts show the manual inputs and the result from your loaded ladder program.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `operand_a_valid` | `BOOL` | **PC** | `False` |
| `operand_b_valid` | `BOOL` | **PC** | `False` |
| `calculate_request` | `BOOL` | **PC** | `False` |
| `sum_result_valid` | `BOOL` | **PLC** | `False` |
| `operand_a` | `DINT` | **PC** | `0` |
| `operand_b` | `DINT` | **PC** | `0` |
| `sum_result` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle operand a valid` | `toggle` | `operand_a_valid` |
| `Toggle operand b valid` | `toggle` | `operand_b_valid` |
| `Toggle calculate request` | `toggle` | `calculate_request` |
| `Cycle operand_a (0, 1, 2, 5, 10)` | `cycle` | `operand_a` |
| `Cycle operand_b (0, 1, 2, 5, 10)` | `cycle` | `operand_b` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `operand_a_valid` | `switch_1` | `switch` |
| `operand_b_valid` | `switch_5` | `switch` |
| `calculate_request` | `switch_6` | `switch` |
| `sum_result_valid` | `indicator_2` | `indicator` |
| `operand_a` | `numeric_display_0` | `numericDisplay` |
| `operand_b` | `numeric_display_1` | `numericDisplay` |
| `sum_result` | `training_accessory_4` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_1` | `switch` | A VALID manual input |
| `indicator_2` | `indicator` | Sum Function Block indicator |
| `training_accessory_4` | `trainingAccessory` | SUM live numeric result |
| `switch_5` | `switch` | B VALID manual input |
| `switch_6` | `switch` | CALCULATE manual input |
| `numeric_display_0` | `trainingAccessory` | A NEXT live numeric readout |
| `numeric_display_1` | `trainingAccessory` | B NEXT live numeric readout |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Offline sum calculation exercise with two manually selected DINT values and a live SUM result. Validity requires both manual input-valid flags and CALCULATE. Open programs/examples/09-sum-function-reference.rpproj.json to run the supplied reference.

### Start conditions

- Local offline runtime selected; a reference or student program must be loaded.

### Normal sequence

- Open programs/examples/09-sum-function-reference.rpproj.json in Project > Open, verify and load offline, then Run.
- Set A=2 and B=5, then enable both valid inputs and CALCULATE. Expect SUM 7.

### Expected observations

- A=2 and B=5 produce SUM 7 only with all three permissives; sum_result_valid clears when any permissive is lost.
- An empty exercise has no calculation controller until a program is loaded.
