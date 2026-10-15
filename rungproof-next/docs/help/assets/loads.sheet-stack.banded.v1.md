# Banded Sheet-Metal Stack help

Asset ID: `loads.sheet-stack.banded.v1`  
Catalog status: **production / approved**  
Category: `loads/metal`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.6 m
- Height: 0.82 m
- Depth: 1.3 m
- Source: `res://assets/factory_kit/banded_sheet_metal_stack/source/banded_sheet_metal_stack.blend`
- Delivery: `res://assets/factory_kit/banded_sheet_metal_stack/delivery/banded_sheet_metal_stack.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **banded flat-rolled steel sheet stack on dunnage**.
Source-model review: **compared-pass** — Rendered review shows a clean stack of thin rectangular metal sheets on wooden dunnage, with two broad transverse securing bands and edge-protector context. It reads as a generic banded flat-sheet bundle family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Nucor: Steel Sheet](https://nucor.com/products/sheet/) | oem-product-page | 2026-09-22 |

Modeled family features:
- stacked flat sheets
- two transverse bands
- edge-protector context
- wood dunnage

Intentionally generic / not claimed:
- No Nucor mark, grade, sheet dimensions, bundle mass, band specification, packaging standard, dunnage spacing, or lifting method is reproduced.
- The asset must not be read as a load-securement instruction.
