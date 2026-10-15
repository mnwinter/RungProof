# Fixed Caged Ladder - 4 m help

Asset ID: `facility.access.caged-ladder-4m.v1`  
Catalog status: **production / approved**  
Category: `facility/access`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.1 m
- Height: 4.1 m
- Depth: 1.0 m
- Source: `res://assets/factory_kit/fixed_caged_ladder/source/fixed_caged_ladder.blend`
- Delivery: `res://assets/factory_kit/fixed_caged_ladder/delivery/fixed_caged_ladder.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **fixed steel ladder with safety cage and walk-through top**.
Source-model review: **compared-pass** — Rebuilt review leaves the lower climbing zone open, starts the cage above the entry, and adds a flared lower cage transition plus top walk-through/landing guardrails. It remains generic facility geometry, not an access-safety design.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Cotterman: Fixed Steel Ladders product page](https://www.cotterman.com/products/fixed-ladders/) | oem-product-page | 2026-09-22 |

Modeled family features:
- angle side rails
- evenly spaced round rungs
- stand-off wall brackets
- cage hoops/verticals
- walk-through landing rails

Intentionally generic / not claimed:
- No Cotterman mark, OSHA/ANSI conformance, cage-start height, material, anchor design, or fall-protection claim is reproduced.
- The model is visual facility geometry, not an access-safety design.
