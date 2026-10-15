# Non-Contact Radar Level Transmitter help

Asset ID: `sensing.level.radar.v1`  
Catalog status: **production / approved**  
Category: `sensing/level`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.771 m
- Height: 1.422 m
- Depth: 0.68 m
- Source: `res://assets/scene_core/radar_level_transmitter/source/radar_level_transmitter.blend`
- Delivery: `res://assets/scene_core/radar_level_transmitter/delivery/radar_level_transmitter.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `level_percent` | `float32` | `output` |  | Level percent. |
| `signal_ma` | `float32` | `output` |  | Signal ma. |
| `echo_ok` | `bool` | `output` |  | Echo ok. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact non-contact radar level transmitter**.
Source-model review: **compared-pass** — Fresh blind and close-up renders reviewed against the VEGA VEGAPULS compact radar family: a compact vertical electronics head has a display/keypad face, terminal cover, side cable gland, continuous process neck, bolted mounting flange, downward horn, and dielectric lens. The rendered beam is a symbolic visual cue only; there is no level-accuracy, process-connection, hazardous-area, or measurement-performance claim.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [VEGA: VEGAPULS 21 compact radar sensor](https://www.vega.com/en-us/products/product-catalog/level/radar/vegapuls-21) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact instrument housing
- neutral status or display context
- process fitting
- proportioned radar antenna/horn
- cable or connector treatment

Intentionally generic / not claimed:
- No VEGA mark, radar frequency, range, accuracy, output, HART setting, process medium, pressure rating, antenna selection, or installation instruction is reproduced.
- The asset is visual-only; it does not transmit, receive, or establish a live level measurement.
