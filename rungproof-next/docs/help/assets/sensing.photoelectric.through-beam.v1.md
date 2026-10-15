# Through-Beam Photoelectric Sensor Pair help

Asset ID: `sensing.photoelectric.through-beam.v1`  
Catalog status: **production / approved**  
Category: `sensing/photoelectric/through-beam`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.25 m
- Height: 1.04 m
- Depth: 1.65 m
- Source: `res://assets/scene_core/through_beam_photoeye/source/through_beam_photoeye.blend`
- Delivery: `res://assets/scene_core/through_beam_photoeye/delivery/through_beam_photoeye.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `beam_blocked` | `bool` | `output` |  | True while a modeled object interrupts the optical beam. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `beam_center` |  |  |

## Industrial reference basis

Generic reference family: **compact rectangular through-beam photoelectric sender and receiver**.
Source-model review: **compared-pass** — The corrected review shows two compact dark rectangular sender/receiver heads on adjustable upright brackets, protected optical lens details, neutral status indicators, routed cable/M12 treatments, and a visible alignment path. It reads as a generic compact industrial through-beam photoelectric pair.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SICK: W12 photoelectric sensor product overview](https://www.sick.com/media/productoverview/5/65/765/productoverview_W12_g555765_en.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- compact rectangular housings
- emitter/receiver optical windows
- neutral status indicators
- connector treatment
- machine bracket context

Intentionally generic / not claimed:
- No SICK mark, range, light source, output, supply voltage, IP rating, alignment requirement, or mounting dimension is reproduced.
- Any beam is visual-only simulator context, not optical emission, a safety boundary, or a live detection claim.
