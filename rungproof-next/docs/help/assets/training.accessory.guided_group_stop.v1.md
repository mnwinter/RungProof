# Guided Group Stop help

Asset ID: `training.accessory.guided_group_stop.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.05 m
- Height: 0.82 m
- Depth: 0.88 m
- Source: `res://assets/training_accessories/guided_group_stop/source/guided_group_stop.blend`
- Delivery: `res://assets/training_accessories/guided_group_stop/delivery/guided_group_stop.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `tool_plate_travel` | linear | `KIN_TOOL_PLATE` | 0 to 0.55 m | 0.8 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **compact guided pneumatic cylinder with internal guide shafts**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: MGP standard guided cylinder](https://www.smcusa.com/products/pneumatic-actuators/guided-actuators/heavy-duty/internal-guide-shaft/standard-guided-cylinder-mgp~137432) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact guide body
- twin guide shafts
- moving plate
- piston rod
- port fittings
- mounting base

Intentionally generic / not claimed:
- No SMC mark, bore, stroke, bearing type, load rating, cushion, port, sensor, or pressure specification is reproduced.
- Guided travel is visual-only simulator kinematics and carries no physical-motion claim.
