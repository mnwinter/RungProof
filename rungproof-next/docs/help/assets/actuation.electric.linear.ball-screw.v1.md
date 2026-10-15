# Servo Ball-Screw Linear Actuator help

Asset ID: `actuation.electric.linear.ball-screw.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/electric/linear`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.9 m
- Height: 0.9 m
- Depth: 0.92 m
- Source: `res://assets/mechanical_motion/ball_screw_linear_actuator/source/ball_screw_linear_actuator.blend`
- Delivery: `res://assets/mechanical_motion/ball_screw_linear_actuator/delivery/ball_screw_linear_actuator.glb`

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
| `carriage_travel` | linear | `KIN_BALL_NUT_CARRIAGE` | -0.75 to 0.75 m | 1.5 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **guided ball-screw electric linear actuator**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Thomson MF-K ball-screw linear-unit family: a long guided base carries a moving saddle over a visibly segmented screw/nut path, with end supports, a compact motor/end housing, and mounting base. It reads as a generic screw-driven linear actuator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Thomson: MF Movopart ball screw linear units](https://www.thomsonlinear.com/en/products/linear-motion-systems/mfk) | oem-product-page | 2026-09-22 |

Modeled family features:
- guided base
- moving saddle
- visible screw/nut path
- end supports
- motor/end housing
- mounting base

Intentionally generic / not claimed:
- No Thomson mark, stroke, screw diameter/lead, motor, encoder, brake, guide type, force, speed, accuracy, repeatability, or environmental rating is reproduced.
- The visible travel is symbolic simulator kinematics; it has no physical positioning, torque, or load claim.
