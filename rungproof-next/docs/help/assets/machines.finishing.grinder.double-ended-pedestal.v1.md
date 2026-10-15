# Double-Ended Pedestal Grinder help

Asset ID: `machines.finishing.grinder.double-ended-pedestal.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/finishing/grinders`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.8 m
- Height: 2.0 m
- Depth: 1.1 m
- Source: `res://assets/production-machines/pedestal_grinder/source/pedestal_grinder.blend`
- Delivery: `res://assets/production-machines/pedestal_grinder/delivery/pedestal_grinder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `running` | `bool` | `output` |  | Running. |
| `grinder_fault` | `bool` | `output` |  | Grinder fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `motor_rotation` | continuous | `KIN_GRINDER_MOTOR` | 0 to 360 deg | 3600 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **double-ended pedestal grinder**.
Source-model review: **compared-pass** — Fresh blind render compared with Baldor pedestal grinders: a central pedestal supports a double-ended motor, paired wheel guards, eye shields, and tool rests. It reads as a generic double-ended pedestal grinder.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Baldor-Reliance: Industrial bench and pedestal grinders](https://www.baldor.com/mvc/DownloadCenter/Files/9AKK107348) | oem-datasheet | 2026-09-22 |

Modeled family features:
- pedestal
- central motor
- two wheel guards
- eye shields
- tool rests

Intentionally generic / not claimed:
- No Baldor mark, wheel size, grit, RPM, motor rating, guard specification, dust control, or safety certification is reproduced.
- The visual does not rotate abrasive wheels or support grinding.
