# Function-Block Panel help

Asset ID: `training.accessory.function_block_panel.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.2 m
- Height: 0.95 m
- Depth: 0.82 m
- Source: `res://assets/training_accessories/function_block_panel/source/function_block_panel.blend`
- Delivery: `res://assets/training_accessories/function_block_panel/delivery/function_block_panel.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `shaft_rotation` | angular_continuous | `KIN_INNER_RACE` | -3000 to 3000 rpm | 6000 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **two-bolt mounted pillow-block ball-bearing unit**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SKF: SY 1.5/8 TF pillow block ball bearing unit](https://www.skf.com/ng/products/mounted-bearings/ball-bearing-units/pillow-block-ball-bearing-units/productid-SY%201.5%2F8%20TF?failover=true) | oem-product-page | 2026-09-22 |

Modeled family features:
- base-mounted housing
- circular bearing insert
- projecting shaft
- grease fitting
- locking-ring context
- base fasteners

Intentionally generic / not claimed:
- No SKF mark, bore, housing material, locking method, seal, grease, load, speed, alignment, or mounting rating is reproduced.
- The representation makes no bearing-life, shaft-support, or live-equipment claim.
