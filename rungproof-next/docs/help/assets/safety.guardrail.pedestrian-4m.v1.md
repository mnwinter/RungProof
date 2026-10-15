# Pedestrian Guardrail - 4 m help

Asset ID: `safety.guardrail.pedestrian-4m.v1`  
Catalog status: **production / approved**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.1 m
- Height: 1.15 m
- Depth: 0.35 m
- Source: `res://assets/factory_kit/pedestrian_guardrail/source/pedestrian_guardrail.blend`
- Delivery: `res://assets/factory_kit/pedestrian_guardrail/delivery/pedestrian_guardrail.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **two-line steel-pipe industrial guardrail with bolt-down posts**.
Source-model review: **compared-pass** — Rendered review shows a continuous two-rail yellow guardrail with upright posts, capped ends, lower beam, bolt-down feet, and a long straight run. It reads as a generic two-line industrial pedestrian/asset guardrail family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Ideal Shield: Standard Guardrail](https://www.idealshield.com/products/guardrail/standard-guardrail/) | oem-product-page | 2026-09-22 |

Modeled family features:
- two horizontal rails
- upright posts
- post caps
- lower beam
- bolt-down base plates

Intentionally generic / not claimed:
- No Ideal Shield mark, pipe schedule, rail height, impact rating, mounting calculation, anchor specification, plastic sleeve, code compliance, or protection claim is reproduced.
- The asset is not a site-specific barrier design.
