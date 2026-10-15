# Three-Arm Delta Pick Robot help

Asset ID: `robotics.robot.delta-three-arm.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/robots/delta`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.3 m
- Height: 2.8 m
- Depth: 2.2 m
- Source: `res://assets/robotics/delta_pick_robot/source/delta_pick_robot.blend`
- Delivery: `res://assets/robotics/delta_pick_robot/delivery/delta_pick_robot.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `shoulder_a_command` | `float32` | `input` |  | Shoulder a command. |
| `shoulder_b_command` | `float32` | `input` |  | Shoulder b command. |
| `shoulder_c_command` | `float32` | `input` |  | Shoulder c command. |
| `platform_z_command` | `float32` | `input` |  | Platform z command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `shoulder_a` | rotary | `KIN_SHOULDER_0` | -55 to 55 deg | 500 |
| `shoulder_b` | rotary | `KIN_SHOULDER_1` | -55 to 55 deg | 500 |
| `shoulder_c` | rotary | `KIN_SHOULDER_2` | -55 to 55 deg | 500 |
| `platform_z` | linear | `KIN_MOVING_PLATFORM` | 0 to 1.2 m | 4.0 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-arm delta pick robot**.
Source-model review: **compared-pass** — Rendered review compared with the ABB FlexPicker delta family: an overhead housing and three distributed drives support three paired-link arms converging to a moving platform over a pick conveyor. It reads as a generic three-arm delta pick robot.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB Robotics: IRB 360 FlexPicker delta robot](https://new.abb.com/products/robotics/nl/onze-robots/delta-robots/irb-360) | oem-product-page | 2026-09-22 |

Modeled family features:
- overhead housing
- three distributed drives
- three paired-link arm sets
- moving platform
- pick-conveyor context

Intentionally generic / not claimed:
- No ABB mark, payload, reach, axis count, speed, hygiene rating, controller, vision configuration, or safety function is reproduced.
- The visual does not move, pick product, or establish a guarded high-speed cell.
