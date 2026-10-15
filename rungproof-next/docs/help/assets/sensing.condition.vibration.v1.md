# Stud-Mount Industrial Vibration Sensor help

Asset ID: `sensing.condition.vibration.v1`  
Catalog status: **candidate / candidate**  
Category: `sensing/condition`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.8 m
- Height: 0.8 m
- Depth: 0.7 m
- Source: `res://assets/controls_sensors/piezo_vibration_sensor/source/piezo_vibration_sensor.blend`
- Delivery: `res://assets/controls_sensors/piezo_vibration_sensor/delivery/piezo_vibration_sensor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `velocity_mm_s` | `float32` | `output` | mm/s | Velocity mm s. |
| `healthy` | `bool` | `output` |  | Healthy. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **stud-mounted cylindrical industrial vibration transmitter**.
Source-model review: **compared-pass** — Rebuilt review render shows the documented cylindrical, stud-mounted, top-connector family and replaces the prior generic cube/wave-ring treatment.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm electronic: VTV121 vibration transmitter data sheet](https://media.ifm.com/dam/780b84a6-f270-4c76-ba53-ad3e2084d32c/Original/VTV121-01_EN-GB.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- small cylindrical sensor body
- threaded stud mounting into a machine surface
- wrench-flat/base detail
- top M12-style electrical connector

Intentionally generic / not claimed:
- No ifm mark, exact dimensions, mounting torque, frequency response, velocity range, output signal, approval, or protection rating is reproduced.
- velocity_mm_s and healthy remain symbolic simulator outputs only.
