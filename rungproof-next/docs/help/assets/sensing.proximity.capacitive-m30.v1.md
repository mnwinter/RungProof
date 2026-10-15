# M30 Capacitive Proximity Sensor help

Asset ID: `sensing.proximity.capacitive-m30.v1`  
Catalog status: **production / approved**  
Category: `sensing/proximity`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.75 m
- Depth: 0.85 m
- Source: `res://assets/controls_sensors/capacitive_proximity_sensor_m30/source/capacitive_proximity_sensor_m30.blend`
- Delivery: `res://assets/controls_sensors/capacitive_proximity_sensor_m30/delivery/capacitive_proximity_sensor_m30.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `detected` | `bool` | `output` |  | Detected. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **M30 threaded capacitive proximity sensor**.
Source-model review: **compared-pass** — Rendered review shows the characteristic threaded cylindrical body, two lock nuts, flush sensing face, rear connector lead, and installation against a nonmetallic target. It reads as a generic M30 capacitive-sensor family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm electronic: KI533A capacitive sensor data sheet](https://media.ifm.com/dam/2eb89bf3-8934-4449-98e6-1f0fe5aa61fa/Original/KI533A-02_EN-GB.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- M30 x 1.5 threaded cylindrical housing
- two lock nuts at the mounting zone
- flat polymer sensing face
- rear M12-style connector and status/adjustment area

Intentionally generic / not claimed:
- No ifm logo, product number, dimensions, performance range, wiring, or rating is reproduced.
- The hopper and pellet target only establish installation context; they do not demonstrate sensing performance.
