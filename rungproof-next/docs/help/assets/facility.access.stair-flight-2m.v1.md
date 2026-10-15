# Industrial Stair Flight - 2 m Rise help

Asset ID: `facility.access.stair-flight-2m.v1`  
Catalog status: **production / approved**  
Category: `facility/access`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.0 m
- Height: 3.05 m
- Depth: 1.2 m
- Source: `res://assets/factory_kit/industrial_stair_flight/source/industrial_stair_flight.blend`
- Delivery: `res://assets/factory_kit/industrial_stair_flight/delivery/industrial_stair_flight.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **fixed industrial metal stair flight with handrails**.
Source-model review: **compared-pass** — Rendered review shows a consistent metal-tread flight, paired stringers, continuous handrail/midrail groups, and attached base/top transitions consistent with a generic industrial stair flight.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Lapeyre Stair: Metal Industrial Stairs product page](https://www.lapeyrestair.com/products/industrial-stairs/) | oem-product-page | 2026-09-22 |

Modeled family features:
- consistent tread/riser flight
- stringers
- landing transitions
- handrails and midrails
- base/landing attachment

Intentionally generic / not claimed:
- No supplier mark, stair slope, tread type, load rating, code compliance, anchorage, or finish is reproduced.
- The model is visual facility geometry, not a safe access design.
