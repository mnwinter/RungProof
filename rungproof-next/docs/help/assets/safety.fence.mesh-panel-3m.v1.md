# Machine Safety Fence Panel - 3 m help

Asset ID: `safety.fence.mesh-panel-3m.v1`  
Catalog status: **production / approved**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.5 m
- Height: 2.15 m
- Depth: 0.4 m
- Source: `res://assets/factory_kit/machine_safety_fence_panel/source/machine_safety_fence_panel.blend`
- Delivery: `res://assets/factory_kit/machine_safety_fence_panel/delivery/machine_safety_fence_panel.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **modular welded-wire machine-guarding fence panel**.
Source-model review: **compared-pass** — Rendered review shows a framed welded-wire panel spanning two upright posts with top and bottom rails, post caps, lower mounting interfaces, and floor anchor plates. It reads as a generic modular machine-guarding panel family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Troax: ST20 Machine Guarding Panel](https://www.troax.com/product/panel-st20/) | oem-product-page | 2026-09-22 |

Modeled family features:
- welded-wire infill
- surround frame
- upright posts
- top and bottom rails
- post caps
- anchor plates

Intentionally generic / not claimed:
- No Troax mark, wire size, mesh opening, panel dimension, safety distance, fastener system, floor anchorage, code compliance, or performance claim is reproduced.
- This generic model is not a guarding design or safety-distance instruction.
