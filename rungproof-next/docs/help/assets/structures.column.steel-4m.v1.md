# Structural Steel Support Column - 4 m help

Asset ID: `structures.column.steel-4m.v1`  
Catalog status: **production / approved**  
Category: `structures/steel`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.92 m
- Height: 4.08 m
- Depth: 0.92 m
- Source: `res://assets/factory_kit/structural_w_column/source/structural_w_column.blend`
- Delivery: `res://assets/factory_kit/structural_w_column/delivery/structural_w_column.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **rolled wide-flange structural steel column with base plate**.
Source-model review: **compared-pass** — Rendered review shows a vertical wide-flange/H-section steel column with a cap plate, base plate, and four anchor-bolt context. It reads as a generic anchored structural steel column family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Nucor: Steel Beam](https://nucor.com/products/beam/) | oem-product-page | 2026-09-22 |

Modeled family features:
- vertical W/H-section
- cap plate
- base plate
- anchor-bolt context

Intentionally generic / not claimed:
- No Nucor mark, ASTM grade, W designation, section properties, connection design, base-plate calculation, anchor design, weld, loading, fire protection, or structural-capacity claim is reproduced.
- The asset is visual facility geometry only.
