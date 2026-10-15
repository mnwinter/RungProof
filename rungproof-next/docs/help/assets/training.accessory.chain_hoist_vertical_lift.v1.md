# Chain Hoist/Vertical Lift help

Asset ID: `training.accessory.chain_hoist_vertical_lift.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.9 m
- Depth: 1.8 m
- Source: `res://assets/training_accessories/chain_hoist_vertical_lift/source/chain_hoist_vertical_lift.blend`
- Delivery: `res://assets/training_accessories/chain_hoist_vertical_lift/delivery/chain_hoist_vertical_lift.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `spindle_rotation` | continuous | `KIN_SPINDLE` | 0 to 360 deg | 3600 |
| `table_x` | linear | `KIN_X_TABLE` | 0 to 1.1 m | 0.12 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **vertical knee mill**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Sharp Industries: TMV-1 vertical knee mill](https://sharp-industries.com/product/tmv-1/) | oem-product-page | 2026-09-22 |

Modeled family features:
- column
- knee and slotted table
- quill/spindle head
- drive housing
- handwheel treatment

Intentionally generic / not claimed:
- No Sharp mark, travel, spindle taper, speed, motor rating, tooling, coolant, guarding, or certification is reproduced.
- The visual does not rotate a spindle, machine material, or establish a safe setup.
