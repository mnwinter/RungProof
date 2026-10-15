# Six-Axis Collaborative Robot help

Asset ID: `robotics.robot.collaborative-six-axis.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/robots/collaborative`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 2.2 m
- Depth: 1.4 m
- Source: `res://assets/robotics/six_axis_cobot/source/six_axis_cobot.blend`
- Delivery: `res://assets/robotics/six_axis_cobot/delivery/six_axis_cobot.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `joint_1_command` | `float32` | `input` |  | Joint 1 command. |
| `joint_2_command` | `float32` | `input` |  | Joint 2 command. |
| `joint_3_command` | `float32` | `input` |  | Joint 3 command. |
| `joint_4_command` | `float32` | `input` |  | Joint 4 command. |
| `joint_5_command` | `float32` | `input` |  | Joint 5 command. |
| `joint_6_command` | `float32` | `input` |  | Joint 6 command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `joint_1` | rotary | `KIN_J1` | -180 to 180 deg | 180 |
| `joint_2` | rotary | `KIN_J2` | -180 to 180 deg | 180 |
| `joint_3` | rotary | `KIN_J3` | -180 to 180 deg | 180 |
| `joint_4` | rotary | `KIN_J4` | -180 to 180 deg | 180 |
| `joint_5` | rotary | `KIN_J5` | -180 to 180 deg | 180 |
| `joint_6` | rotary | `KIN_J6` | -180 to 180 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **six-axis collaborative robot**.
Source-model review: **compared-pass** — Rendered review compared with the Universal Robots UR10e family: a compact base supports a serial articulated arm with six visually distinct joint covers and a wrist/tool flange treatment. It reads as a generic six-axis collaborative-robot form.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Universal Robots: UR10e technical specification](https://www.universal-robots.com/media/1807466/ur10e_e-series_datasheets_web.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- compact base
- serial articulated arm
- six visible joint treatments
- wrist/tool flange treatment

Intentionally generic / not claimed:
- No Universal Robots mark, payload, reach, speed, force sensing, controller, I/O, safety function, protective rating, or collaborative-use claim is reproduced.
- This symbolic model is not a risk assessment and does not establish that a cell can be operated collaboratively.
