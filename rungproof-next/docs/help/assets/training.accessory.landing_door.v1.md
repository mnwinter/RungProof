# Landing Door help

Asset ID: `training.accessory.landing_door.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.25 m
- Height: 3.15 m
- Depth: 1.15 m
- Source: `res://assets/training_accessories/landing_door/source/landing_door.blend`
- Delivery: `res://assets/training_accessories/landing_door/delivery/landing_door.glb`

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

Generic reference family: **modular industrial equipment-enclosure personnel-door panel**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Panel Built: Modular Inplant Equipment Enclosure case study](https://www.panelbuilt.com/case-studies/inplant-equipment-enclosure/) | oem-product-page | 2026-09-22 |

Modeled family features:
- modular wall-panel field
- personnel door
- hinge side
- latch/handle context
- threshold and enclosure interfaces

Intentionally generic / not claimed:
- No Panel Built mark, door rating, fire rating, hardware rating, lock, access control, egress compliance, enclosure specification, or code compliance is reproduced.
- This model is not a machine-safety claim.
