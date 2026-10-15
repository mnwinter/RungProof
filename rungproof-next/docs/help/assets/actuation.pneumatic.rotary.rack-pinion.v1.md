# Rack-and-Pinion Pneumatic Rotary Actuator help

Asset ID: `actuation.pneumatic.rotary.rack-pinion.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/pneumatic/rotary`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.95 m
- Height: 0.86 m
- Depth: 1.0 m
- Source: `res://assets/mechanical_motion/pneumatic_rotary_actuator/source/pneumatic_rotary_actuator.blend`
- Delivery: `res://assets/mechanical_motion/pneumatic_rotary_actuator/delivery/pneumatic_rotary_actuator.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `clockwise_command` | `bool` | `input` |  | Clockwise command. |
| `counterclockwise_command` | `bool` | `input` |  | Counterclockwise command. |
| `angle_deg` | `float32` | `output` | deg | Angle deg. |
| `clockwise_limit` | `bool` | `output` |  | Clockwise limit. |
| `counterclockwise_limit` | `bool` | `output` |  | Counterclockwise limit. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `output_angle` | angular | `KIN_OUTPUT_FLANGE` | 0 to 180 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **compact rack-and-pinion pneumatic rotary actuator**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SMC CRQ compact rack-and-pinion family: a compact square body carries a central rotary output, bounded angle-indicator ring, air-port fittings, end caps, and mounting base. It reads as a generic limited-rotation pneumatic actuator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: CRQ compact rotary actuator](https://www.smcusa.com/products/pneumatic-actuators/rotary-actuators/rack-and-pinion/metric-specifications/crq~20170) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact housing
- central rotary output
- angle reference ring
- air-port fittings
- end caps
- base mount

Intentionally generic / not claimed:
- No SMC mark, rotation angle, torque, bore, shaft type, cushion, port, sensor, or pressure specification is reproduced.
- The illustrated angle is not a real actuator position or control command.
