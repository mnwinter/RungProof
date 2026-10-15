# Motorized Rack-and-Pinion Linear Actuator help

Asset ID: `actuation.electric.linear.rack-pinion.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/electric/linear`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.75 m
- Depth: 1.02 m
- Source: `res://assets/mechanical_motion/rack_pinion_linear_actuator/source/rack_pinion_linear_actuator.blend`
- Delivery: `res://assets/mechanical_motion/rack_pinion_linear_actuator/delivery/rack_pinion_linear_actuator.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable` | `bool` | `input` |  | Enable. |
| `position_setpoint_m` | `float32` | `input` | m | Position setpoint m. |
| `position_m` | `float32` | `output` | m | Position m. |
| `in_position` | `bool` | `output` |  | In position. |
| `faulted` | `bool` | `output` |  | Faulted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `carriage_travel` | linear | `KIN_CARRIAGE` | -0.75 to 0.75 m | 2.5 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **motorized rack-and-pinion industrial linear actuator**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Rollon rack-and-pinion linear-actuator family: a motor drives a clearly visible pinion beside a long straight toothed rack, while a broad carriage spans the rail/base and end supports bound the travel. It reads as a generic rack-and-pinion linear actuator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Rollon: Rack and pinion driven linear actuators](https://www.rollon.com/usa/en/family/actuator-line/rack-and-pinion-driven-linear-actuators/) | oem-product-page | 2026-09-22 |

Modeled family features:
- motor
- visible pinion
- straight toothed rack
- broad carriage
- rail/base
- end supports

Intentionally generic / not claimed:
- No Rollon mark, rack module, stroke, guide configuration, motor, gearbox, payload, speed, precision, repeatability, lubrication, or safety specification is reproduced.
- The model does not establish a real long-travel axis or command physical I/O.
