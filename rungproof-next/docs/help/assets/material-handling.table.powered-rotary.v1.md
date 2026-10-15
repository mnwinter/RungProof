# Powered Indexing Rotary Table help

Asset ID: `material-handling.table.powered-rotary.v1`  
Catalog status: **production / approved**  
Category: `material-handling/tables`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.7 m
- Height: 1.2 m
- Depth: 2.7 m
- Source: `res://assets/scene_core/powered_rotary_table/source/powered_rotary_table.blend`
- Delivery: `res://assets/scene_core/powered_rotary_table/delivery/powered_rotary_table.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position_command` | `int32` | `input` |  | Position command. |
| `at_position` | `bool` | `output` |  | At position. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `table_angle` | rotary | `KIN_table` | -360 to 360 deg | 90 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact powered rotary indexing table**.
Source-model review: **compared-pass** — Rendered review shows a low circular indexing plate with radial T-slots, central fixture context, a compact cylindrical base, side motor/gearbox, cable entry, and anchored base plate. It reads as a generic powered rotary-indexing-table family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WEISS: Rotary indexing tables Generation 5](https://gen5.weiss-world.com/en-us/) | oem-product-page | 2026-09-22 |

Modeled family features:
- circular index plate
- radial T-slots
- fixture context
- cylindrical base
- side drive context
- cable entry and anchor base

Intentionally generic / not claimed:
- No WEISS mark, index accuracy, load, torque, motor, drive train, controller, wiring, or safety function is reproduced.
- Rotation is symbolic simulator behavior only.
