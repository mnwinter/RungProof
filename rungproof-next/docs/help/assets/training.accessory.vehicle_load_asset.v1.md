# Vehicle/Load Asset help

Asset ID: `training.accessory.vehicle_load_asset.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.5 m
- Depth: 1.1 m
- Source: `res://assets/training_accessories/vehicle_load_asset/source/vehicle_load_asset.blend`
- Delivery: `res://assets/training_accessories/vehicle_load_asset/delivery/vehicle_load_asset.glb`

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

Generic reference family: **three-pole contactor and thermal-overload motor starter**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB: AF three-pole contactors and overload relays](https://new.abb.com/low-voltage/products/motor-protection/3-pole-contactors-and-overload-relays-for-motor-starting/afcontactors) | oem-product-page | 2026-09-22 |

Modeled family features:
- stacked contactor/overload body
- three line lugs
- three load lugs
- reset treatment
- adjustment treatment
- motor-starter face

Intentionally generic / not claimed:
- No ABB mark, coil voltage, contact rating, overload class/range, auxiliary contacts, motor rating, coordination type, enclosure, or wiring is reproduced.
- The visual does not energize a coil, switch motor power, or provide overload protection.
