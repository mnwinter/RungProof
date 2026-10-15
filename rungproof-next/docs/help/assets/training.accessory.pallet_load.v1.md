# Pallet Load help

Asset ID: `training.accessory.pallet_load.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.219 m
- Height: 0.97 m
- Depth: 1.06 m
- Source: `res://assets/training_accessories/pallet_load/source/pallet_load.blend`
- Delivery: `res://assets/training_accessories/pallet_load/delivery/pallet_load.glb`

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

Generic reference family: **48 x 40 wood block pallet with corrugated cases**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [CHEP: 48 x 40 North American wood pallet](https://www.chep.com/us/en/products/pallets/standard-pallet-sizes) | oem-product-page | 2026-09-22 |

Modeled family features:
- wood pallet
- deck boards
- block/fork-entry treatment
- stacked corrugated cases

Intentionally generic / not claimed:
- No CHEP mark, pallet grade, wood species, load capacity, case contents, stack pattern, stability, or transport approval is reproduced.
- The visual is not a load-securing or forklift-use instruction.
