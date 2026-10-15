# Baseplate End-Suction Centrifugal Pump help

Asset ID: `process.pump.centrifugal.end-suction-baseplate.v1`  
Catalog status: **candidate / candidate**  
Category: `process/pumps/centrifugal`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.1 m
- Depth: 1.45 m
- Source: `res://assets/process_fluid/end_suction_centrifugal_pump/source/end_suction_centrifugal_pump.blend`
- Delivery: `res://assets/process_fluid/end_suction_centrifugal_pump/delivery/end_suction_centrifugal_pump.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint_rpm` | `float32` | `input` | rpm | Speed setpoint rpm. |
| `running` | `bool` | `output` |  | Running. |
| `discharge_pressure` | `float32` | `output` | bar | Discharge pressure. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `shaft_speed` | angular_continuous | `COUPLING_GUARD` | -3600 to 3600 rpm | 1800 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **horizontal end-suction centrifugal pump on baseplate**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Grundfos end-suction centrifugal-pump form: a horizontal motor joins a volute casing on a shared baseplate, with axial front suction and elevated radial discharge connections. It reads as a generic end-suction centrifugal pump package.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Grundfos: Introduction to end suction pumps](https://www.grundfos.com/in/learn/ecademy/all-courses/grundfos-end-suction-pumps/introduction-to-end-suction-pumps) | oem-product-page | 2026-09-22 |

Modeled family features:
- horizontal motor
- volute casing
- shared baseplate
- axial front suction
- elevated radial discharge
- coupling treatment

Intentionally generic / not claimed:
- No Grundfos mark, pump size, impeller, flow, head, NPSH, seal, motor rating, coupling guard, baseplate design, pressure, temperature, or fluid service is reproduced.
- The asset does not pump fluid, establish a pressure boundary, or control a physical motor.
