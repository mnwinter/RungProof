# Pallet Receiver help

Asset ID: `training.accessory.pallet_receiver.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.25 m
- Height: 0.3 m
- Depth: 1.05 m
- Source: `res://assets/training_accessories/pallet_receiver/source/pallet_receiver.blend`
- Delivery: `res://assets/training_accessories/pallet_receiver/delivery/pallet_receiver.glb`

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

Generic reference family: **48 by 40 inch wood GMA stringer pallet**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [48forty Solutions: Recycled Pallets product page](https://www.48forty.com/recycled-pallets) | oem-product-page | 2026-09-22 |

Modeled family features:
- 48 by 40 proportion
- top and bottom deck boards
- stringer structure
- fork-entry openings

Intentionally generic / not claimed:
- No supplier mark, wood species, grade, repair state, heat treatment, load rating, or logistics certification is reproduced.
- The pallet is a passive scene load.
