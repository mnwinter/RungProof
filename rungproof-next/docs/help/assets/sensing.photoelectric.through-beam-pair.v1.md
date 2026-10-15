# Through-Beam Photoelectric Sensor Pair help

Asset ID: `sensing.photoelectric.through-beam-pair.v1`  
Catalog status: **production / approved**  
Category: `sensing/photoelectric`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.45 m
- Height: 1.3 m
- Depth: 0.45 m
- Source: `res://assets/controls_sensors/through_beam_photoelectric_pair/source/through_beam_photoelectric_pair.blend`
- Delivery: `res://assets/controls_sensors/through_beam_photoelectric_pair/delivery/through_beam_photoelectric_pair.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `beam_clear` | `bool` | `output` |  | Beam clear. |
| `detected` | `bool` | `output` |  | Detected. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **opposed-mode compact photoelectric emitter and receiver pair**.
Source-model review: **compared-pass** — The corrected review shows two compact opposing optical heads on adjustable brackets, each with a protected optical face, recessed lens, neutral status window, and rear M12 treatment. It reads as a generic industrial through-beam emitter/receiver pair.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Banner Engineering: QS18 Series all-purpose photoelectric sensor](https://www.bannerengineering.com/us/en/products/sensors/photoelectric-sensors/qs18-series.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- two compact opposing housings
- distinct emitter and receiver faces
- neutral status indicators
- threaded lens or bracket mounting
- connector/cable treatment

Intentionally generic / not claimed:
- No Banner mark, sensing range, output, supply voltage, IP rating, lens type, alignment procedure, or mounting torque is reproduced.
- Any beam is visual-only simulator context, not optical emission, a safety boundary, or a live detection claim.
