# Diffuse Photoelectric Sensor help

Asset ID: `sensing.photoelectric.diffuse.v1`  
Catalog status: **production / approved**  
Category: `sensing/photoelectric`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.55 m
- Height: 0.7 m
- Depth: 0.6 m
- Source: `res://assets/controls_sensors/diffuse_photoelectric_sensor/source/diffuse_photoelectric_sensor.blend`
- Delivery: `res://assets/controls_sensors/diffuse_photoelectric_sensor/delivery/diffuse_photoelectric_sensor.glb`

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

Generic reference family: **compact rectangular diffuse-reflective photoelectric sensor**.
Source-model review: **compared-pass** — The corrected review shows a compact dark photoeye housing on an open L-bracket, protected shared optical face with distinct recessed emitter/receiver elements, neutral status indicator, rear M12/cable treatment, and carton target context. It reads as a generic diffuse-reflective industrial photoelectric sensor family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Banner Engineering: Q4X Laser Distance Sensor product manual](https://info.bannerengineering.com/cs/groups/public/documents/literature/183055.pdf) | oem-manual | 2026-09-22 |

Modeled family features:
- compact rectangular sensor housing
- front optical window with separate emitter and receiver apertures
- status indicator area
- connector at the rear/lower housing

Intentionally generic / not claimed:
- No Banner mark, exact housing dimensions, laser classification, range, output type, or performance claim is reproduced.
- The visible beam is explanatory simulator artwork, not a field-safe optical-path or range representation.
