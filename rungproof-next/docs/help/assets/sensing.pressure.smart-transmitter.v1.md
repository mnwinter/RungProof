# Smart Industrial Pressure Transmitter help

Asset ID: `sensing.pressure.smart-transmitter.v1`  
Catalog status: **production / approved**  
Category: `sensing/pressure`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.55 m
- Height: 1.15 m
- Depth: 0.55 m
- Source: `res://assets/controls_sensors/pressure_transmitter/source/pressure_transmitter.blend`
- Delivery: `res://assets/controls_sensors/pressure_transmitter/delivery/pressure_transmitter.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `pressure_bar` | `float32` | `output` | bar | Pressure bar. |
| `healthy` | `bool` | `output` |  | Healthy. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact general-industrial pressure transmitter**.
Source-model review: **compared-pass** — The corrected review shows a compact cylindrical transmitter body, short threaded process stem, hex process fitting, neutral top electrical entry, cable treatment, and a simple mounting bracket. It reads as a generic compact general-industrial pressure-transmitter family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WIKA: A-10 pressure transmitter data sheet PE 81.60](https://www.wika.com/media/Data-sheets/Pressure/Pressure-sensors/ds_pe8160_en_co.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- compact transmitter body
- threaded process stem
- top electrical entry
- small local display/indicator zone

Intentionally generic / not claimed:
- No WIKA mark, process thread designation, range, accuracy, material, output protocol, calibration, or environmental rating is reproduced.
- Declared pressure_bar and healthy are symbolic simulator outputs, not a transmitter specification.
