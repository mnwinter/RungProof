# Industrial Floor Platform Scale help

Asset ID: `sensing.scale.floor-platform.v1`  
Catalog status: **production / approved**  
Category: `sensing/weight`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.65 m
- Depth: 2.0 m
- Source: `res://assets/factory_kit/industrial_floor_scale/source/industrial_floor_scale.blend`
- Delivery: `res://assets/factory_kit/industrial_floor_scale/delivery/industrial_floor_scale.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `weight_kg` | `float32` | `output` | kg | Weight kg. |
| `overload` | `bool` | `output` |  | Overload. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **low-profile industrial floor platform scale with remote indicator**.
Source-model review: **compared-pass** — Rendered review shows a broad low-profile steel weighing deck within a structural base, four corner support/load-cell contexts, ramp approaches, and a separate upright indicator. It reads as a generic industrial floor-platform scale family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Cardinal Scale: MH Floor Scales](https://cardinalscale.com/product/product-overview/Floor-Scales/MH-Floor-Scales) | oem-product-page | 2026-09-22 |

Modeled family features:
- steel deck
- structural base
- corner support/load-cell context
- approach ramps
- upright remote indicator

Intentionally generic / not claimed:
- No Cardinal mark, capacity, platform dimensions, load-cell type, accuracy, calibration, legal-for-trade status, cable specification, or installation instruction is reproduced.
- The platform and display are simulator context only; they do not weigh a load, provide a certified result, or connect to physical I/O.
