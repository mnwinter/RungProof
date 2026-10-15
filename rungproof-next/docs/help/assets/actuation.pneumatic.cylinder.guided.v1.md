# Guided Pneumatic Cylinder help

Asset ID: `actuation.pneumatic.cylinder.guided.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/pneumatic/cylinders`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.05 m
- Height: 0.82 m
- Depth: 0.88 m
- Source: `res://assets/mechanical_motion/guided_pneumatic_cylinder/source/guided_pneumatic_cylinder.blend`
- Delivery: `res://assets/mechanical_motion/guided_pneumatic_cylinder/delivery/guided_pneumatic_cylinder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `extend_command` | `bool` | `input` |  | Extend command. |
| `retract_command` | `bool` | `input` |  | Retract command. |
| `position_m` | `float32` | `output` | m | Position m. |
| `extended` | `bool` | `output` |  | Extended. |
| `retracted` | `bool` | `output` |  | Retracted. |

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
Source-model review: **compared-pass** — Fresh blind render reviewed against the SMC MGP guided-cylinder family: a compact body integrates twin guide shafts, a moving plate, rod, end plate, mounting base, and top port fittings. The plate/guide relationship is clear in the rendered form.

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
