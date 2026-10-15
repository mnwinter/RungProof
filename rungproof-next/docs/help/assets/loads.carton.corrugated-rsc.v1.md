# Corrugated Shipping Carton - Regular Slotted help

Asset ID: `loads.carton.corrugated-rsc.v1`  
Catalog status: **candidate / candidate**  
Category: `loads/cartons`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.85 m
- Height: 0.72 m
- Depth: 0.74 m
- Source: `res://assets/scene_loads/corrugated_shipping_carton/source/corrugated_shipping_carton.blend`
- Delivery: `res://assets/scene_loads/corrugated_shipping_carton/delivery/corrugated_shipping_carton.glb`

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

Generic reference family: **regular slotted corrugated shipping carton**.
Source-model review: **compared-pass** — Rendered review compared with Packaging Corporation of America RSC guidance: the carton has a rectangular corrugated shell, four top flaps, four bottom flaps, scored folds, and simple tape treatment. It reads as a generic RSC shipping carton.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Packaging Corporation of America: Basic box styles: regular slotted container](https://www.packagingcorp.com/resource-hub/beyond-the-box/basic-box-styles/) | oem-product-page | 2026-09-22 |

Modeled family features:
- rectangular corrugated shell
- four top flaps
- four bottom flaps
- scored folds
- tape treatment

Intentionally generic / not claimed:
- No PCA mark, board grade, dimensions, contents, ECT, tape specification, compression strength, food contact, or shipping rating is reproduced.
- The visual does not certify packaging performance.
