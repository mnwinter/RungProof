# Safety Bollard - 120 mm help

Asset ID: `safety.bollard.120mm.v1`  
Catalog status: **production / approved**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.5 m
- Height: 1.25 m
- Depth: 0.5 m
- Source: `res://assets/factory_kit/safety_bollard/source/safety_bollard.blend`
- Delivery: `res://assets/factory_kit/safety_bollard/delivery/safety_bollard.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **anchored high-visibility industrial safety bollard**.
Source-model review: **compared-pass** — Rendered review shows a high-visibility cylindrical bollard with a rounded impact cap, contrasting wear collar, base collar, and four-bolt floor plate. It reads as a generic anchored industrial bollard family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [A-SAFE: Bollard 130](https://www.asafe.com/en-us/products/bollard-130/) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical upright
- rounded cap
- contrasting wear collar
- base collar
- four-bolt anchor plate

Intentionally generic / not claimed:
- No A-SAFE mark, polymer construction, impact energy, protection zone, anchor specification, performance rating, or certification is reproduced.
- The asset is not a site-specific barrier-design or impact-protection claim.
