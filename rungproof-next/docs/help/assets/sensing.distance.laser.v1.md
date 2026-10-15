# Industrial Laser Distance Sensor help

Asset ID: `sensing.distance.laser.v1`  
Catalog status: **production / approved**  
Category: `sensing/distance`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.75 m
- Depth: 1.45 m
- Source: `res://assets/controls_sensors/laser_distance_sensor/source/laser_distance_sensor.blend`
- Delivery: `res://assets/controls_sensors/laser_distance_sensor/delivery/laser_distance_sensor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `distance_mm` | `float32` | `output` | mm | Distance mm. |
| `in_range` | `bool` | `output` |  | In range. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact rectangular industrial laser distance sensor**.
Source-model review: **compared-pass** — The corrected review shows a compact rectangular dark housing on a simple bracket, a protected front optical window with a neutral red window and receiver detail, small status indicators, and rear M12 treatment. It reads as a generic compact industrial laser distance-sensor family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SICK: DT80 next-generation laser distance sensor](https://www.sick.com/gb/en/-sicks-next-generation-dt80-distance-sensor-sets-precision-standard/w/Press-Next-Gen-DT80) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact rectangular metal housing
- protected front optical window
- neutral status/interface detail
- mounting face or bracket
- rear or lower connector/cable treatment

Intentionally generic / not claimed:
- No SICK mark, laser class, range, accuracy, display, interface, IP rating, optical performance, or installation orientation is reproduced.
- Any rendered beam must remain visual-only and never represent emitted radiation, a safety boundary, or a live measurement.
