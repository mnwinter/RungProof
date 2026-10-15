# Robot Seventh-Axis Linear Track help

Asset ID: `robotics.positioner.linear-track-seventh-axis.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/positioners/tracks`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.8 m
- Height: 1.1 m
- Depth: 1.0 m
- Source: `res://assets/robotics/robot_linear_track/source/robot_linear_track.blend`
- Delivery: `res://assets/robotics/robot_linear_track/delivery/robot_linear_track.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `track_position_command` | `float32` | `input` |  | Track position command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `track_position` | linear | `KIN_TRACK_CARRIAGE` | 0 to 2.7 m | 1.5 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **robot seventh-axis linear track**.
Source-model review: **compared-pass** — Rendered review compared with KUKA linear units: parallel rails and rack support a moving robot carriage, cable-energy chain, drive-end treatment, and compact robot silhouette. It reads as a generic seventh-axis robot track.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [KUKA: Robot linear units](https://www.kuka.com/en-gb/products/robotics-system/robot-periphery/linear-units) | oem-product-page | 2026-09-22 |

Modeled family features:
- parallel rails
- rack treatment
- moving carriage
- cable-energy chain
- drive-end treatment
- robot mounting flange

Intentionally generic / not claimed:
- No KUKA mark, rail section, travel, load, speed, motor, controller, cable system, guarding, or safety function is reproduced.
- The visual does not move a robot or create a physical external axis.
