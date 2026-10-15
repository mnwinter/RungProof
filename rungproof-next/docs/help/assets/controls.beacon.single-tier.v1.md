# Single-Tier Signal Beacon help

Asset ID: `controls.beacon.single-tier.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/indication`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.4 m
- Height: 1.4 m
- Depth: 0.4 m
- Source: `res://assets/scene_core/single_tier_beacon/source/single_tier_beacon.blend`
- Delivery: `res://assets/scene_core/single_tier_beacon/delivery/single_tier_beacon.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `lamp_on` | `bool` | `input` |  | Lamp on. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **single-tier base-mounted industrial signal beacon**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the WERMA 210 beacon family: the model reads as a compact base-mounted single amber light with a translucent cylindrical lens, black cap/base rings, internal lamp context, vertical stem, and bolted mounting plate. It is generic indication geometry only and does not claim a particular voltage, optical output, ingress rating, or alarm function.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WERMA Signaltechnik: 210 Permanent Beacon technical data sheet](https://www.werma.com/media/6c/19/22/1733928892/21010000_en.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- single translucent light lens
- top and base caps
- internal lamp context
- stem
- base mounting plate

Intentionally generic / not claimed:
- No WERMA mark, part number, color code, voltage, connection, ingress rating, optical performance, or signaling standard is reproduced.
- The beacon state is symbolic simulator output only and does not signal a real plant condition.
