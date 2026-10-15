# Reusable Plastic Tote help

Asset ID: `loads.tote.reusable-plastic.v1`  
Catalog status: **production / approved**  
Category: `loads/containers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.1 m
- Height: 0.82 m
- Depth: 0.82 m
- Source: `res://assets/factory_kit/reusable_plastic_tote/source/reusable_plastic_tote.blend`
- Delivery: `res://assets/factory_kit/reusable_plastic_tote/delivery/reusable_plastic_tote.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **straight-wall reusable industrial plastic tote**.
Source-model review: **compared-pass** — Rendered review shows the straight-wall shell, reinforced vertical ribs, open top with stacking rim, and handhold zone expected for a reusable industrial tote.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ORBIS Corporation: AIAG stackable containers product family](https://www.orbiscorporation.com/en-us/products/hand-held-containers/straight-wall/) | oem-product-page | 2026-09-22 |

Modeled family features:
- rectangular straight-wall shell
- stacking rim
- reinforced corners
- open-top load volume

Intentionally generic / not claimed:
- No ORBIS mark, exact footprint, resin, load rating, dunnage, or automated-handling compatibility is reproduced.
- The tote declares no I/O.
