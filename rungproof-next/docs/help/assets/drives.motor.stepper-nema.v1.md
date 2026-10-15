# NEMA Frame Stepper Motor help

Asset ID: `drives.motor.stepper-nema.v1`  
Catalog status: **candidate / candidate**  
Category: `drives/motors/stepper`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.25 m
- Height: 0.68 m
- Depth: 0.82 m
- Source: `res://assets/mechanical_motion/nema_stepper_motor/source/nema_stepper_motor.blend`
- Delivery: `res://assets/mechanical_motion/nema_stepper_motor/delivery/nema_stepper_motor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable` | `bool` | `input` |  | Enable. |
| `step_command` | `int32` | `input` | count | Step command. |
| `direction` | `bool` | `input` |  | Direction. |
| `position_steps` | `int32` | `output` | count | Position steps. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `shaft_rotation` | angular_continuous | `KIN_STEPPER_SHAFT` | -1500 to 1500 rpm | 4000 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **NEMA-frame hybrid stepper motor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Oriental Motor NEMA-frame stepper families: a square mounting face with four bolts, stacked laminated stator body, output shaft, and rear cable/connector context form a credible generic hybrid stepper motor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Oriental Motor: Stepper motor frame sizes](https://www.orientalmotor.com/stepper-motors/stepper-motor-frame-sizes.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- square mounting face
- four-bolt pattern
- stacked laminated body
- output shaft
- rear cable/connector context

Intentionally generic / not claimed:
- No Oriental Motor mark, NEMA size, step angle, holding torque, current, winding, driver, feedback, or cable specification is reproduced.
- The asset is not a motion controller or an electrical-motor model.
