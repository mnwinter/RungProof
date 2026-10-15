# Incremental Rotary Encoder help

Asset ID: `sensing.position.incremental-encoder.v1`  
Catalog status: **production / approved**  
Category: `sensing/position`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 0.75 m
- Depth: 0.85 m
- Source: `res://assets/controls_sensors/incremental_rotary_encoder/source/incremental_rotary_encoder.blend`
- Delivery: `res://assets/controls_sensors/incremental_rotary_encoder/delivery/incremental_rotary_encoder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position_counts` | `int32` | `output` |  | Position counts. |
| `speed_rpm` | `float32` | `output` | rpm | Speed rpm. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **flange-mounted solid-shaft incremental rotary encoder**.
Source-model review: **compared-pass** — Rendered review shows a compact cylindrical encoder body, machined mounting flange with fastener pattern, concentric face detail, exposed solid shaft, and rear-housing/connector context. It reads as a generic flange-mounted incremental rotary encoder family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm: RU3500 incremental encoder with solid shaft](https://www.ifm.com/gb/en/product/RU3500?source=gs) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical encoder housing
- mounting flange
- fastener pattern
- solid shaft
- rear housing
- connector/cable context

Intentionally generic / not claimed:
- No ifm mark, resolution, output type, shaft diameter, IP rating, coupling, mounting torque, speed limit, or cable specification is reproduced.
- The shaft is visual geometry only; it does not create pulses, establish position, or connect to physical I/O.
