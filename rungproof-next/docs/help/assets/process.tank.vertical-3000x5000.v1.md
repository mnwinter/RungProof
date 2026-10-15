# Vertical Process Tank - 3 m x 5 m help

Asset ID: `process.tank.vertical-3000x5000.v1`  
Catalog status: **production / approved**  
Category: `process/tanks`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.4 m
- Height: 5.8 m
- Depth: 3.4 m
- Source: `res://assets/scene_core/vertical_process_tank/source/vertical_process_tank.blend`
- Delivery: `res://assets/scene_core/vertical_process_tank/delivery/vertical_process_tank.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `level_percent` | `float32` | `output` |  | Level percent. |
| `high_level` | `bool` | `output` |  | High level. |
| `low_level` | `bool` | `output` |  | Low level. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **vertical process tank with roof guardrail and external level indication**.
Source-model review: **compared-pass** — Rendered review shows a vertical cylindrical vessel with top manway/fitting context, roof guardrail, level-indication context, lower flanged nozzle, and legged/anchored support arrangement. It reads as a generic vertical process-tank family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Paul Mueller Company: Processing Tank](https://www.paulmueller.com/store/product/processing-tank?scLang=en) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical vessel shell
- top manway/fitting context
- roof guardrail
- external level indication context
- lower flanged nozzle
- legs and anchor plates

Intentionally generic / not claimed:
- No Mueller mark, vessel material, size, pressure/vacuum rating, sanitary rating, wall thickness, manway rating, relief device, level technology, piping connection, ladder design, or code compliance is reproduced.
- The asset is visual process context only and not a pressure-vessel or storage-safety claim.
