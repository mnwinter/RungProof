# Hydraulic Scissor Lift Table help

Asset ID: `material-handling.lift.scissor-table.v1`  
Catalog status: **production / approved**  
Category: `material-handling/lifts`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.1 m
- Height: 2.4 m
- Depth: 2.0 m
- Source: `res://assets/scene_core/scissor_lift_table/source/scissor_lift_table.blend`
- Delivery: `res://assets/scene_core/scissor_lift_table/delivery/scissor_lift_table.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `raise_command` | `bool` | `input` |  | Raise command. |
| `lower_command` | `bool` | `input` |  | Lower command. |
| `top_limit` | `bool` | `output` |  | Top limit. |
| `bottom_limit` | `bool` | `output` |  | Bottom limit. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `lift_height` | linear | `KIN_platform` | 0 to 2.2 m | 1.1 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **stationary industrial hydraulic scissor lift table**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Bishamon stationary lift-table family: the load platform is supported by two crossed-arm stages with visible pivot pins, retained washers, base and platform roller tracks, an inclined hydraulic cylinder/rod with clevises, and a separate hydraulic power-unit context. The generic asset deliberately omits a claimed guard package, capacity, travel, or hydraulic design; lift motion remains symbolic simulator kinematics.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Bishamon: Stationary Lift Tables](https://bishamon.com/stationary-lift-tables/) | oem-product-page | 2026-09-22 |

Modeled family features:
- load platform
- scissor-arm mechanism
- pivot and roller points
- base frame
- actuator/hydraulic-power context

Intentionally generic / not claimed:
- No Bishamon mark, capacity, platform size, lift travel, hydraulic circuit, guarding, safety devices, or certification is reproduced.
- Lift animation remains symbolic simulator kinematics.
