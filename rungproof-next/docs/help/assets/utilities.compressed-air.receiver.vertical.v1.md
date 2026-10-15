# Vertical Compressed-Air Receiver help

Asset ID: `utilities.compressed-air.receiver.vertical.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/compressed-air/receivers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.5 m
- Height: 1.5 m
- Depth: 2.9 m
- Source: `res://assets/utilities/vertical_air_receiver/source/vertical_air_receiver.blend`
- Delivery: `res://assets/utilities/vertical_air_receiver/delivery/vertical_air_receiver.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `receiver_pressure` | `float32` | `output` | bar | Receiver pressure. |
| `high_pressure_alarm` | `bool` | `output` |  | High pressure alarm. |
| `drain_command` | `bool` | `input` |  | Drain command. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **vertical compressed-air receiver vessel**.
Source-model review: **compared-pass** — Fresh blind render compared with standard vertical compressed-air receiver families: the tall cylindrical shell has formed heads, a pressure-gauge treatment, top relief valve, side inlet/outlet nozzles, bottom drain, and support legs. It reads as a generic vertical air receiver.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Kaeser Compressors: Industrial rotary screw compressors](https://pr.kaeser.com/en/products-and-solutions/rotary-screw-compressors/screw-compressors.aspx) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical shell
- formed heads
- pressure gauge treatment
- relief-valve treatment
- inlet/outlet nozzles
- drain
- support legs

Intentionally generic / not claimed:
- No vessel manufacturer, volume, MAWP, temperature rating, material, ASME stamp, relief sizing, nozzle rating, inspection interval, or drain specification is reproduced.
- The visual is not a pressure boundary and contains no compressed air.
