# Spray Head help

Asset ID: `training.accessory.spray_head.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.6 m
- Height: 1.3 m
- Depth: 1.3 m
- Source: `res://assets/training_accessories/spray_head/source/spray_head.blend`
- Delivery: `res://assets/training_accessories/spray_head/delivery/spray_head.glb`

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

Generic reference family: **automatic industrial paint spray gun**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Dürr: EcoGun automatic spray guns](https://www.durr.com/en/products/paint-shop-application-technology/paint-application/spray-guns) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot mount
- compact gun body
- air-cap/nozzle face
- material cup
- fluid and air hose treatments

Intentionally generic / not claimed:
- No Dürr mark, paint chemistry, pressure, nozzle size, atomization, flow, electrostatics, exhaust, or hazardous-area rating is reproduced.
- The visual does not spray coating or establish booth/fire/exposure safety.
