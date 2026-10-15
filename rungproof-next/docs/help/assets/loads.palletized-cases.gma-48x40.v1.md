# Palletized Corrugated Case Load - GMA 48 x 40 in help

Asset ID: `loads.palletized-cases.gma-48x40.v1`  
Catalog status: **candidate / candidate**  
Category: `loads/palletized-goods`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.219 m
- Height: 0.97 m
- Depth: 1.06 m
- Source: `res://assets/loads/palletized_case_load/source/palletized_case_load.blend`
- Delivery: `res://assets/loads/palletized_case_load/delivery/palletized_case_load.glb`

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
Source-model review: **compared-pass** — Rendered review compared with North American 48 x 40 block-pallet construction: a wood pallet supports a regular stacked case load with deck-board, block, and fork-entry treatments. It reads as a generic GMA-size palletized case load.

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
