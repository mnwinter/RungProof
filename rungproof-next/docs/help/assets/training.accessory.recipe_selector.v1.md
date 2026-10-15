# Recipe Selector help

Asset ID: `training.accessory.recipe_selector.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.7 m
- Height: 1.7 m
- Depth: 0.6 m
- Source: `res://assets/training_accessories/recipe_selector/source/recipe_selector.blend`
- Delivery: `res://assets/training_accessories/recipe_selector/delivery/recipe_selector.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `selector_position` | rotary | `KIN_selector_handle` | -55 to 55 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **22 mm maintained industrial selector switch on an operator station**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: Harmony XB4 metal selector switch product data](https://shop.se.com/pro/us/en/product/head-for-selector-switch-harmony-xb4-metal-black-22mm-long-handle-3-positions-stay-put/) | oem-product-page | 2026-09-22 |

Modeled family features:
- round 22 mm panel-mounted operator
- metal mounting bezel
- black handle with defined positions
- enclosure/pedestal mounting context

Intentionally generic / not claimed:
- No Schneider mark, exact number of positions, contact blocks, wiring, protection rating, or operating-mode logic is reproduced.
- The scene selector points remain symbolic simulator I/O only.
