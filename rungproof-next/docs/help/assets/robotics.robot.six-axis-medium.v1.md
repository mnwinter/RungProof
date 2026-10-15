# Six-Axis Industrial Robot help

Asset ID: `robotics.robot.six-axis-medium.v1`  
Catalog status: **production / approved**  
Category: `robotics/arms`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.3 m
- Height: 3.8 m
- Depth: 2.0 m
- Source: `res://assets/scene_core/six_axis_robot/source/six_axis_robot.blend`
- Delivery: `res://assets/scene_core/six_axis_robot/delivery/six_axis_robot.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `running` | `bool` | `output` |  | Running. |
| `at_home` | `bool` | `output` |  | At home. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `axis_1` | rotary | `KIN_axis_1` | -170 to 170 deg | 120 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **medium six-axis articulated industrial robot**.
Source-model review: **compared-pass** — Rendered review shows a pedestal base, distinct shoulder/elbow and wrist joint groups, six-axis articulated-arm silhouette, and tool flange. Dimensions, reach, cabling, and operating envelope remain generic.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB: IRB 1300 articulated robot product page](https://new.abb.com/products/robotics/robots/articulated-robots/irb-1300) | oem-product-page | 2026-09-22 |

Modeled family features:
- pedestal base
- shoulder/elbow articulated arm
- compact wrist
- tool flange
- segmented six-axis joint layout

Intentionally generic / not claimed:
- No ABB mark, reach, payload, controller, cabling, ratings, motion envelope, collision performance, or safety configuration is reproduced.
- Scene kinematics are simulator behavior and not a robot specification.
