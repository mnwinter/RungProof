# Length Encoder help

Asset ID: `training.accessory.length_encoder.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 0.75 m
- Depth: 0.85 m
- Source: `res://assets/training_accessories/length_encoder/source/length_encoder.blend`
- Delivery: `res://assets/training_accessories/length_encoder/delivery/length_encoder.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **flange-mounted solid-shaft incremental rotary encoder**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

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
