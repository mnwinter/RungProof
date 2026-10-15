# Product Counter help

Asset ID: `training.accessory.product_counter.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.1 m
- Height: 1.1 m
- Depth: 0.72 m
- Source: `res://assets/training_accessories/product_counter/source/product_counter.blend`
- Delivery: `res://assets/training_accessories/product_counter/delivery/product_counter.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pusher_position` | linear | `KIN_pusher_plate` | 0 to 1.35 m | 0.65 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **double-acting single-rod pneumatic cylinder used as a pusher**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: CS1 Large Bore Tie Rod Actuator product page](https://www.smcusa.com/products/pneumatic-actuators/linear-actuators/tie-rod/cs1~53642) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylinder barrel
- end caps
- single extending rod
- mounting feet/clevis interface
- air-port zone

Intentionally generic / not claimed:
- No SMC mark, bore, stroke rating, pressure, cushioning, switch type, port thread, mounting load, or pneumatic circuit is reproduced.
- Pusher motion remains symbolic simulator kinematics.
