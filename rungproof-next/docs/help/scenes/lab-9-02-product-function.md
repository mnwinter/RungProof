# Lab 9.2 - Product Function Block help

Scene ID: `lab-9-02-product-function`  
Migrated source: `prototype/scenes/lab-9-02-product-function.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-02-product-function.scene.json`

## Purpose

Select A and B, confirm both input-valid flags, and request a product. The numeric readouts show the manual inputs and the result from your loaded ladder program.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `factor_a_valid` | `BOOL` | **PC** | `False` |
| `factor_b_valid` | `BOOL` | **PC** | `False` |
| `calculate_request` | `BOOL` | **PC** | `False` |
| `product_result_valid` | `BOOL` | **PLC** | `False` |
| `factor_a` | `DINT` | **PC** | `0` |
| `factor_b` | `DINT` | **PC** | `0` |
| `product_result` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle factor a valid` | `toggle` | `factor_a_valid` |
| `Toggle factor b valid` | `toggle` | `factor_b_valid` |
| `Toggle calculate request` | `toggle` | `calculate_request` |
| `Cycle factor_a (0, 1, 2, 5, 10)` | `cycle` | `factor_a` |
| `Cycle factor_b (0, 1, 2, 5, 10)` | `cycle` | `factor_b` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `factor_a_valid` | `switch_1` | `switch` |
| `factor_b_valid` | `switch_5` | `switch` |
| `calculate_request` | `switch_6` | `switch` |
| `product_result_valid` | `indicator_2` | `indicator` |
| `factor_a` | `numeric_display_0` | `numericDisplay` |
| `factor_b` | `numeric_display_1` | `numericDisplay` |
| `product_result` | `training_accessory_4` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_1` | `switch` | A VALID manual input |
| `indicator_2` | `indicator` | Product Function Block indicator |
| `training_accessory_4` | `trainingAccessory` | PRODUCT live numeric result |
| `switch_5` | `switch` | B VALID manual input |
| `switch_6` | `switch` | CALCULATE manual input |
| `numeric_display_0` | `trainingAccessory` | A NEXT live numeric readout |
| `numeric_display_1` | `trainingAccessory` | B NEXT live numeric readout |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Offline product calculation exercise with two manually selected DINT values and a live PRODUCT result. Validity requires both manual input-valid flags and CALCULATE. Open programs/examples/09-product-function-reference.rpproj.json to run the supplied reference.

### Start conditions

- Local offline runtime selected; a reference or student program must be loaded.

### Normal sequence

- Open programs/examples/09-product-function-reference.rpproj.json in Project > Open, verify and load offline, then Run.
- Set A=2 and B=5, then enable both valid inputs and CALCULATE. Expect PRODUCT 10.

### Expected observations

- A=2 and B=5 produce PRODUCT 10 only with all three permissives; product_result_valid clears when any permissive is lost.
- An empty exercise has no calculation controller until a program is loaded.
