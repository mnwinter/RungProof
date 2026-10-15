# Lab 9.3 - Sum and Counter Function help

Scene ID: `lab-9-03-sum-and-counter-function`  
Migrated source: `prototype/scenes/lab-9-03-sum-and-counter-function.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-03-sum-and-counter-function.scene.json`

## Purpose

Numeric sum and event-count exercise: manual operands and call-complete input; PLC-authored logic owns the result and count.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `inputs_valid` | `BOOL` | **PC** | `False` |
| `calculate_request` | `BOOL` | **PC** | `False` |
| `call_complete` | `BOOL` | **PC** | `False` |
| `result_valid` | `BOOL` | **PLC** | `False` |
| `event_counted` | `BOOL` | **PLC** | `False` |
| `operand_a` | `DINT` | **PC** | `0` |
| `operand_b` | `DINT` | **PC** | `0` |
| `sum_result` | `DINT` | **PLC** | `0` |
| `event_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle inputs valid` | `toggle` | `inputs_valid` |
| `Toggle calculate request` | `toggle` | `calculate_request` |
| `Toggle call complete` | `toggle` | `call_complete` |
| `Cycle operand_a (0, 1, 2, 5, 10)` | `cycle` | `operand_a` |
| `Cycle operand_b (0, 1, 2, 5, 10)` | `cycle` | `operand_b` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `inputs_valid` | `switch_1` | `switch` |
| `calculate_request` | `switch_3` | `switch` |
| `call_complete` | `switch_4` | `switch` |
| `result_valid` | `indicator_2` | `indicator` |
| `event_counted` | `indicator_5` | `indicator` |
| `operand_a` | `numeric_display_0` | `numericDisplay` |
| `operand_b` | `numeric_display_1` | `numericDisplay` |
| `sum_result` | `numeric_display_2` | `numericDisplay` |
| `event_count` | `numeric_display_3` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_1` | `switch` | Manual inputs valid input |
| `indicator_2` | `indicator` | PLC result valid indication |
| `switch_3` | `switch` | Manual calculate input |
| `switch_4` | `switch` | Manual call complete input |
| `indicator_5` | `indicator` | PLC event counted indication |
| `numeric_display_0` | `trainingAccessory` | A NEXT live numeric readout |
| `numeric_display_1` | `trainingAccessory` | B NEXT live numeric readout |
| `numeric_display_2` | `trainingAccessory` | SUM live numeric readout |
| `numeric_display_3` | `trainingAccessory` | COUNT live numeric readout |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Numeric sum and event-count exercise: manual operands and call-complete input; PLC-authored logic owns the result and count.

### Start conditions

- Built-in virtual controller selected; no physical PLC is required.
- Author or explicitly load the reference ladder, then Verify + Load and Run.

### Normal sequence

- Choose A and B with the A NEXT and B NEXT readouts or sidebar actions.
- Enable INPUTS VALID and CALCULATE; toggle manual CALL COMPLETE off/on for each new completion.
- With the reference, SUM shows the qualified arithmetic result and COUNT advances once per valid completion edge.

### Expected observations

- A NEXT and B NEXT immediately show their PC input values.
- SUM and COUNT stay zero with the empty exercise program.
- With the opt-in reference: A=2, B=5 and one valid completion yield SUM=7, COUNT=1; event_counted is a one-scan pulse, result_valid follows the three Boolean inputs.
