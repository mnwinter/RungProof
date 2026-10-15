# Floor Call Station help

Asset ID: `training.accessory.floor_call_station.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.3 m
- Height: 2.05 m
- Depth: 0.55 m
- Source: `res://assets/training_accessories/floor_call_station/source/floor_call_station.blend`
- Delivery: `res://assets/training_accessories/floor_call_station/delivery/floor_call_station.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **floor-standing modular electrical enclosure**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Rittal: TS 8 Floormount Enclosure product page](https://www.rittal.com/us-en_US/products/PG20231215SCH101/PG20240111SCH301/PRO39595?variantId=8018807) | oem-product-page | 2026-09-22 |

Modeled family features:
- tall floor-mounted cabinet envelope
- door and handle zone
- roof/base structure
- removable floor-stand context

Intentionally generic / not claimed:
- No Rittal marks, exact sheet thickness, dimensions, listed protection category, material, panel layout, or certification is reproduced.
- The enclosure does not imply energized equipment or live I/O.
