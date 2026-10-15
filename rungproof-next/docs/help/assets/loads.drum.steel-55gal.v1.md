# Steel Shipping Drum - 55 gal help

Asset ID: `loads.drum.steel-55gal.v1`  
Catalog status: **production / approved**  
Category: `loads/containers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 1.2 m
- Depth: 0.75 m
- Source: `res://assets/factory_kit/steel_shipping_drum/source/steel_shipping_drum.blend`
- Delivery: `res://assets/factory_kit/steel_shipping_drum/delivery/steel_shipping_drum.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **closed-head 55-gallon steel shipping drum**.
Source-model review: **compared-pass** — Rendered review shows a closed head, two closure fittings, rolled chimes, and rolling hoops consistent with a generic 55-gallon tight-head steel drum.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Mauser Packaging Solutions: Steel Drum Portfolio](https://mauserpackaging.com/wp-content/uploads/2025/02/Mauser_BRO_STEEL_DRUM_PORTFOLIO-1.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- cylindrical steel body
- rolling hoops
- closed head
- small closure fittings

Intentionally generic / not claimed:
- No Mauser mark, lining, closure thread, UN marking, contents, corrosion resistance, or transport approval is reproduced.
- The model is a passive scene load with no process claim.
