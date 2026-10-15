# Banded Steel Coil on Saddles help

Asset ID: `loads.coil.steel-banded.v1`  
Catalog status: **production / approved**  
Category: `loads/metal`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.75 m
- Height: 1.55 m
- Depth: 1.25 m
- Source: `res://assets/factory_kit/steel_coil_on_saddles/source/steel_coil_on_saddles.blend`
- Delivery: `res://assets/factory_kit/steel_coil_on_saddles/delivery/steel_coil_on_saddles.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **banded flat-rolled steel coil on storage saddles**.
Source-model review: **compared-pass** — Rendered review shows a broad flat-rolled steel coil with open bore, exposed sheet wraps, radial securing bands, edge protectors, and two saddle supports. It reads as a generic secured steel-coil handling family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Nucor: Steel Sheet](https://nucor.com/products/sheet/) | oem-product-page | 2026-09-22 |

Modeled family features:
- flat-rolled coil
- open bore
- sheet wraps
- radial bands
- edge protectors
- saddle supports

Intentionally generic / not claimed:
- No Nucor mark, coil grade, width, mass, band specification, packaging standard, saddle capacity, storage rule, or lifting method is reproduced.
- The asset must not be read as a material-handling procedure or load-securement instruction.
