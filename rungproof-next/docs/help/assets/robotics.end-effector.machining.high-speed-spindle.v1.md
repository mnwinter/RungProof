# Robotic High-Speed Machining Spindle help

Asset ID: `robotics.end-effector.machining.high-speed-spindle.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/machining`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.4 m
- Height: 1.7 m
- Depth: 1.4 m
- Source: `res://assets/robotics/robotic_high_speed_spindle/source/robotic_high_speed_spindle.blend`
- Delivery: `res://assets/robotics/robotic_high_speed_spindle/delivery/robotic_high_speed_spindle.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `spindle_rotation_command` | `float32` | `input` |  | Spindle rotation command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `spindle_rotation` | continuous | `KIN_SPINDLE_MOTOR` | 0 to 360 deg | 24000 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **robot-mounted high-speed machining spindle**.
Source-model review: **compared-pass** — Rendered review compared with HSD industrial electrospindle families: a robot mount supports a cylindrical motor housing with cooling-fin treatment, spindle nose, collet/tool treatment, and coolant line. It reads as a generic high-speed machining spindle.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [HSD Mechatronics: Electrospindles product portfolio](https://www.hsdmechatronics.com/) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot mount
- cylindrical motor housing
- cooling-fin treatment
- spindle nose
- collet/tool treatment
- coolant line

Intentionally generic / not claimed:
- No HSD mark, speed, power, taper, tool retention, cooling, dust extraction, controller, guarding, or certification is reproduced.
- The visual does not rotate a cutter or machine stock.
