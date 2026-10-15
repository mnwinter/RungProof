# Collection Tank Bank help

Asset ID: `training.accessory.collection_tank_bank.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.4 m
- Height: 5.8 m
- Depth: 3.4 m
- Source: `res://assets/training_accessories/collection_tank_bank/source/collection_tank_bank.blend`
- Delivery: `res://assets/training_accessories/collection_tank_bank/delivery/collection_tank_bank.glb`

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

Generic reference family: **vertical process tank with roof guardrail and external level indication**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Paul Mueller Company: Processing Tank](https://www.paulmueller.com/store/product/processing-tank?scLang=en) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical vessel shell
- top manway/fitting context
- roof guardrail
- external level indication context
- lower flanged nozzle
- legs and anchor plates

Intentionally generic / not claimed:
- No Mueller mark, vessel material, size, pressure/vacuum rating, sanitary rating, wall thickness, manway rating, relief device, level technology, piping connection, ladder design, or code compliance is reproduced.
- The asset is visual process context only and not a pressure-vessel or storage-safety claim.
