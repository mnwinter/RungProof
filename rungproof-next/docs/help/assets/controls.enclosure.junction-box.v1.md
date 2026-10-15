# Industrial Junction Box help

Asset ID: `controls.enclosure.junction-box.v1`  
Catalog status: **production / approved**  
Category: `controls/enclosures`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.05 m
- Height: 0.9 m
- Depth: 0.55 m
- Source: `res://assets/factory_kit/industrial_junction_box/source/industrial_junction_box.blend`
- Delivery: `res://assets/factory_kit/industrial_junction_box/delivery/industrial_junction_box.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **wall-mounted industrial terminal box with gland plate**.
Source-model review: **compared-pass** — Rendered review shows a compact wall-mount terminal enclosure with a removable gasketed front cover, four retained fasteners, external mounting lugs, and cable-entry context. It reads as a generic industrial terminal-box family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Rittal: KX terminal box, carbon steel with flange](https://www.rittal.com/us-en_US/products/PG20231215SCH101/PG20231215SCH201/PRO70535?variantId=1538000) | oem-product-page | 2026-09-22 |

Modeled family features:
- enclosure body
- removable cover
- cover fasteners
- external mounting lugs
- cable-entry/gland context

Intentionally generic / not claimed:
- No Rittal mark, enclosure rating, material thickness, dimensions, gland thread, internal component layout, or approval is reproduced.
- The asset is visual simulator geometry only.
