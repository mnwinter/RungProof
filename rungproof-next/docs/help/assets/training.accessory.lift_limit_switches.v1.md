# Lift Limit Switches help

Asset ID: `training.accessory.lift_limit_switches.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.1 m
- Height: 2.4 m
- Depth: 2.0 m
- Source: `res://assets/training_accessories/lift_limit_switches/source/lift_limit_switches.blend`
- Delivery: `res://assets/training_accessories/lift_limit_switches/delivery/lift_limit_switches.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `lift_height` | linear | `KIN_platform` | 0 to 2.2 m | 1.1 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **stationary industrial hydraulic scissor lift table**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Bishamon: Stationary Lift Tables](https://bishamon.com/stationary-lift-tables/) | oem-product-page | 2026-09-22 |

Modeled family features:
- load platform
- scissor-arm mechanism
- pivot and roller points
- base frame
- actuator/hydraulic-power context

Intentionally generic / not claimed:
- No Bishamon mark, capacity, platform size, lift travel, hydraulic circuit, guarding, safety devices, or certification is reproduced.
- Lift animation remains symbolic simulator kinematics.
