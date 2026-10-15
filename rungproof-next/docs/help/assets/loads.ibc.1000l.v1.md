# Intermediate Bulk Container - 1000 L help

Asset ID: `loads.ibc.1000l.v1`  
Catalog status: **production / approved**  
Category: `loads/containers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.25 m
- Height: 1.6 m
- Depth: 1.25 m
- Source: `res://assets/factory_kit/ibc_tote/source/ibc_tote.blend`
- Delivery: `res://assets/factory_kit/ibc_tote/delivery/ibc_tote.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **1,000 litre composite IBC tote with steel cage and pallet base**.
Source-model review: **compared-pass** — Rendered review shows the defining composite-IBC form: a molded inner bottle inside a welded tubular cage, pallet base, top fill cap, and low outlet valve with handle. It reads as a generic 1,000 L composite IBC family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Mauser Packaging Solutions: Composite IBCs](https://mauserpackaging.com/products-innovations/intermediate-bulk-containers/composite-ibcs/) | oem-product-page | 2026-09-22 |

Modeled family features:
- molded inner bottle
- tubular steel cage
- pallet base
- top fill cap
- low outlet valve and handle

Intentionally generic / not claimed:
- No Mauser mark, UN marking, contents, compatibility, valve size, pallet material, filling limit, pressure, transport rating, or hazardous-material claim is reproduced.
- The asset has no containment, handling, or process-safety claim.
