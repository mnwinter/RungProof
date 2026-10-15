# GMA Wood Pallet - 48 x 40 in help

Asset ID: `loads.pallet.gma-48x40.v1`  
Catalog status: **production / approved**  
Category: `loads/pallets`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.25 m
- Height: 0.3 m
- Depth: 1.05 m
- Source: `res://assets/factory_kit/gma_wood_pallet/source/gma_wood_pallet.blend`
- Delivery: `res://assets/factory_kit/gma_wood_pallet/delivery/gma_wood_pallet.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **48 by 40 inch wood GMA stringer pallet**.
Source-model review: **compared-pass** — Rendered review shows deck boards, three longitudinal stringers, fork-entry openings, and 48-by-40-style proportion consistent with a generic GMA pallet.

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
