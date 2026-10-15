# Four-Axis Palletizing Robot help

Asset ID: `robotics.robot.palletizer-four-axis.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/robots/palletizing`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.6 m
- Height: 2.7 m
- Depth: 1.8 m
- Source: `res://assets/robotics/four_axis_palletizer/source/four_axis_palletizer.blend`
- Delivery: `res://assets/robotics/four_axis_palletizer/delivery/four_axis_palletizer.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `axis_1_command` | `float32` | `input` |  | Axis 1 command. |
| `axis_2_command` | `float32` | `input` |  | Axis 2 command. |
| `axis_3_command` | `float32` | `input` |  | Axis 3 command. |
| `axis_4_command` | `float32` | `input` |  | Axis 4 command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `axis_1` | rotary | `KIN_AXIS_1` | -180 to 180 deg | 140 |
| `axis_2` | rotary | `KIN_AXIS_2` | -90 to 90 deg | 120 |
| `axis_3` | rotary | `KIN_AXIS_3` | -120 to 120 deg | 150 |
| `axis_4` | continuous | `KIN_AXIS_4` | 0 to 360 deg | 240 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **four-axis palletizing robot**.
Source-model review: **compared-pass** — Rendered review compared with FANUC palletizing-robot families: a large floor pedestal supports a multi-link arm ending in a level pallet/case gripper over a pallet-and-carton context. It reads as a generic four-axis palletizing robot.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [FANUC America: M-410 series palletizing robots](https://www.fanucamerica.com/products/series/m-410) | oem-product-page | 2026-09-22 |

Modeled family features:
- floor pedestal
- multi-link arm
- level end-effector treatment
- pallet context
- carton context

Intentionally generic / not claimed:
- No FANUC mark, payload, reach, axis configuration, speed, controller, EOAT capacity, load stability, guarding, or safety function is reproduced.
- The visual does not lift cases, stack a pallet, or establish a safe palletizing cell.
