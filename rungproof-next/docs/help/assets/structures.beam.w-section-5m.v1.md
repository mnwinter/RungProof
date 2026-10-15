# Structural W-Section Beam - 5 m help

Asset ID: `structures.beam.w-section-5m.v1`  
Catalog status: **production / approved**  
Category: `structures/steel`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 5.0 m
- Height: 0.8 m
- Depth: 0.55 m
- Source: `res://assets/factory_kit/structural_w_beam/source/structural_w_beam.blend`
- Delivery: `res://assets/factory_kit/structural_w_beam/delivery/structural_w_beam.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **rolled wide-flange structural steel beam**.
Source-model review: **compared-pass** — Rendered review shows a long rolled I/W-section member with distinct top and bottom flanges, central web, and end connection-plate context. It reads as a generic structural wide-flange beam family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Nucor: Steel Beam](https://nucor.com/products/beam/) | oem-product-page | 2026-09-22 |

Modeled family features:
- top flange
- bottom flange
- central web
- long rolled member
- end connection-plate context

Intentionally generic / not claimed:
- No Nucor mark, ASTM grade, W designation, section properties, span, connection design, weld, bolt, loading, fire protection, or structural-capacity claim is reproduced.
- The asset is visual facility geometry only.
