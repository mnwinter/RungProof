# 4-20 mA Level Transmitter help

Asset ID: `sensing.level.analog-4-20ma.v1`  
Catalog status: **production / approved**  
Category: `sensing/level`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.8305 m
- Height: 3.605 m
- Depth: 0.64 m
- Source: `res://assets/scene_core/level_transmitter_4_20ma/source/level_transmitter_4_20ma.blend`
- Delivery: `res://assets/scene_core/level_transmitter_4_20ma/delivery/level_transmitter_4_20ma.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `level_percent` | `float32` | `output` |  | Level percent. |
| `signal_ma` | `float32` | `output` |  | Signal ma. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **guided-wave continuous level transmitter with analogue-output family**.
Source-model review: **compared-pass** — The corrected review shows a proportioned process flange and bolt context, compact round electronics head with protected neutral window, terminal cover and cable-gland treatment, sealing neck, and a long guided-wave rod with a defined tip. It reads as a generic continuous guided-wave level-transmitter family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm: Level sensors product guide](https://www.ifm.com/ifmweb/downcont.nsf/files/ifm%20UK%20LevelSensors/%24file/ifm%20UK%20LevelSensors.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- proportioned transmitter head
- neutral display or indicator context
- cable entry
- threaded or flanged process connection
- guided-wave rod

Intentionally generic / not claimed:
- No ifm mark, output scaling, process connection, rod length, material, pressure/temperature rating, IP rating, configuration, calibration, or installation instruction is reproduced.
- The asset exposes symbolic simulator I/O only; it is not a 4–20 mA loop, configured instrument, or live process measurement.
