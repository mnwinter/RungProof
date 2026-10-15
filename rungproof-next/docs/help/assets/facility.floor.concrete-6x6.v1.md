# Industrial Concrete Floor Slab - 6 m x 6 m help

Asset ID: `facility.floor.concrete-6x6.v1`  
Catalog status: **production / approved**  
Category: `facility/floors`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 6.0 m
- Height: 0.2 m
- Depth: 6.0 m
- Source: `res://assets/factory_kit/sealed_concrete_floor_slab/source/sealed_concrete_floor_slab.blend`
- Delivery: `res://assets/factory_kit/sealed_concrete_floor_slab/delivery/sealed_concrete_floor_slab.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **jointed industrial concrete slab with floor-drain and traffic marking context**.
Source-model review: **compared-pass** — Rendered review shows a large continuous concrete slab with realistic saw-cut/control-joint grid, a recessed drain/grate, and a painted pedestrian boundary stripe. It reads as generic industrial concrete-floor geometry.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Sika: Concrete Flooring](https://www.sika.com/en/construction/floor-systems/concrete-flooring.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- concrete slab
- control-joint grid
- floor drain/grate
- painted boundary stripe

Intentionally generic / not claimed:
- No Sika floor system, concrete mix, reinforcement, slab thickness, flatness, drain design, load rating, coating, joint specification, or code compliance is reproduced.
- The asset is visual scene flooring only.
