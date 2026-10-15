# Four-Axis SCARA Robot help

Asset ID: `robotics.robot.scara-four-axis.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/robots/scara`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.7 m
- Height: 1.8 m
- Depth: 1.5 m
- Source: `res://assets/robotics/scara_robot/source/scara_robot.blend`
- Delivery: `res://assets/robotics/scara_robot/delivery/scara_robot.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `joint_1_command` | `float32` | `input` |  | Joint 1 command. |
| `joint_2_command` | `float32` | `input` |  | Joint 2 command. |
| `z_axis_command` | `float32` | `input` |  | Z axis command. |
| `tool_rotation_command` | `float32` | `input` |  | Tool rotation command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `joint_1` | rotary | `KIN_JOINT_1` | -180 to 180 deg | 360 |
| `joint_2` | rotary | `KIN_JOINT_2` | -150 to 150 deg | 360 |
| `z_axis` | linear | `KIN_Z_QUILL` | 0 to 0.65 m | 1.5 |
| `tool_rotation` | continuous | `KIN_TOOL_ROTATION` | 0 to 360 deg | 720 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **four-axis SCARA robot**.
Source-model review: **compared-pass** — Rendered review compared with the ABB IRB 910SC family: a fixed pedestal supports two horizontal rotary links, a vertical Z quill, wrist/tool treatment, and table base. It reads as a generic four-axis SCARA robot.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB Robotics: IRB 910SC SCARA product data](https://library.e.abb.com/public/2ac1d57a1bf4431391aa558f27c99cf5/IRB910SC-revF-9AKK106713A1510.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- fixed pedestal
- two horizontal rotary links
- vertical Z quill
- wrist/tool treatment
- base mounting

Intentionally generic / not claimed:
- No ABB mark, dimensions, payload, reach, axis limits, speed, controller, wiring, protective rating, or safety function is reproduced.
- Kinematic points are simulator-only and do not command a robot or validate a robot cell.
