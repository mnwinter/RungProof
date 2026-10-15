# Single-Transducer Ultrasonic Distance Sensor help

Asset ID: `sensing.distance.ultrasonic-single-transducer.v1`  
Catalog status: **production / approved**  
Category: `sensing/distance`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.75 m
- Depth: 0.65 m
- Source: `res://assets/controls_sensors/ultrasonic_distance_sensor/source/ultrasonic_distance_sensor.blend`
- Delivery: `res://assets/controls_sensors/ultrasonic_distance_sensor/delivery/ultrasonic_distance_sensor.glb`

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

Generic reference family: **compact single-transducer industrial ultrasonic distance sensor**.
Source-model review: **compared-pass** — The corrected review shows a compact threaded-barrel sensor on an open bracket, hex locknuts, thread crests, a single recessed transducer membrane, status window, and rear M12 treatment. It reads as a generic single-transducer industrial ultrasonic sensor family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Banner Engineering: QS18U Series Compact High Speed Ultrasonic Sensor](https://www.bannerengineering.com/my/en/products/sensors/ultrasonic-sensors/ultrasonic-sensors-qs18u-series.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact cylindrical or rectangular housing
- single ultrasonic transducer face
- threaded or bracket mounting
- neutral status indicators
- rear connector/cable treatment

Intentionally generic / not claimed:
- No Banner mark, sensing range, teach function, output type, supply voltage, IP rating, environmental limit, or mounting torque is reproduced.
- Any emitted cone/beam is visual-only simulator context, not an acoustic exposure, measurement, or process claim.
