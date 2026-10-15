# Lab 9.4 - Function Selector help

Scene ID: `lab-9-04-function-selector`  
Migrated source: `prototype/scenes/lab-9-04-function-selector.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-04-function-selector.scene.json`

## Purpose

Select A, B and function: 1 SUM, 2 PRODUCT. Confirm both validity flags and CALCULATE; the loaded ladder program owns RESULT.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `operand_set_valid` | `BOOL` | **PC** | `False` |
| `function_select_valid` | `BOOL` | **PC** | `False` |
| `calculate_request` | `BOOL` | **PC** | `False` |
| `selected_result_valid` | `BOOL` | **PLC** | `False` |
| `operand_a` | `DINT` | **PC** | `0` |
| `operand_b` | `DINT` | **PC** | `0` |
| `function_choice` | `DINT` | **PC** | `0` |
| `selected_result` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle operand set valid` | `toggle` | `operand_set_valid` |
| `Toggle function select valid` | `toggle` | `function_select_valid` |
| `Toggle calculate request` | `toggle` | `calculate_request` |
| `Next operand_a` | `cycle` | `operand_a` |
| `Next operand_b` | `cycle` | `operand_b` |
| `Next function (1 SUM, 2 PRODUCT)` | `cycle` | `function_choice` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `operand_set_valid` | `switch_0` | `switch` |
| `function_select_valid` | `switch_5` | `switch` |
| `calculate_request` | `switch_6` | `switch` |
| `selected_result_valid` | `indicator_2` | `indicator` |
| `operand_a` | `numeric_display_0` | `numericDisplay` |
| `operand_b` | `numeric_display_1` | `numericDisplay` |
| `function_choice` | `numeric_display_2` | `numericDisplay` |
| `selected_result` | `numeric_display_3` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `switch` | OPERANDS manual input |
| `indicator_2` | `indicator` | Function Selector indicator |
| `switch_5` | `switch` | FUNC VALID manual input |
| `switch_6` | `switch` | CALCULATE manual input |
| `numeric_display_0` | `trainingAccessory` | A NEXT live numeric readout |
| `numeric_display_1` | `trainingAccessory` | B NEXT live numeric readout |
| `numeric_display_2` | `trainingAccessory` | FUNC NEXT live numeric readout |
| `numeric_display_3` | `trainingAccessory` | RESULT live numeric readout |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Select A, B and function: 1 SUM, 2 PRODUCT. Confirm both validity flags and CALCULATE; the loaded ladder program owns RESULT.

### Start conditions

- Local offline runtime selected; a reference or student program must be loaded.

### Normal sequence

- Open programs/examples/09-function-selector-reference.rpproj.json in Project > Open, verify and load offline, then Run.
- Set A=2 and B=5, enable OPERANDS, FUNC VALID and CALCULATE. Choice 1 gives RESULT 7; choice 2 gives RESULT 10.

### Expected observations

- Only the selected supported path executes: 1 SUM produces 7 and 2 PRODUCT produces 10 for A=2, B=5. Unsupported choices never assert selected_result_valid.
- An empty exercise has no calculation controller until a program is loaded.
