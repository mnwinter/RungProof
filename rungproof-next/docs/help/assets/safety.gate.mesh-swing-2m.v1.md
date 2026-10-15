# Machine Safety Swing Gate - 2 m help

Asset ID: `safety.gate.mesh-swing-2m.v1`  
Catalog status: **production / approved**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 2.15 m
- Depth: 0.45 m
- Source: `res://assets/factory_kit/machine_safety_swing_gate/source/machine_safety_swing_gate.blend`
- Delivery: `res://assets/factory_kit/machine_safety_swing_gate/delivery/machine_safety_swing_gate.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `gate_closed` | `bool` | `output` |  | Gate closed. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **modular welded-wire machine-guarding swing gate**.
Source-model review: **compared-pass** — Rendered review shows a framed welded-wire gate leaf between rigid posts, a distinct hinge side, latch-side handle context, lower sweep clearance, and floor-mounted posts. It reads as a generic swinging machine-guarding gate family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Troax: Machine Guarding brochure](https://www.troax.com/app/uploads/Brochure-Machine-guarding_EN.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- welded-wire gate leaf
- surround frame
- hinge-side post
- latch-side hardware context
- floor-mounted posts

Intentionally generic / not claimed:
- No Troax mark, opening width, latch type, safety switch, safety distance, hinge rating, fastener system, floor anchorage, code compliance, or performance claim is reproduced.
- Gate travel is symbolic simulator geometry, not a safety-function claim.
