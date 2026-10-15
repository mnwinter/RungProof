# Actuated Ball Valve help

Asset ID: `process.valve.actuated-ball.v1`  
Catalog status: **production / approved**  
Category: `process/valves`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.22 m
- Height: 1.816 m
- Depth: 0.77 m
- Source: `res://assets/scene_core/actuated_process_valve/source/actuated_process_valve.blend`
- Delivery: `res://assets/scene_core/actuated_process_valve/delivery/actuated_process_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `open_command` | `bool` | `input` |  | Open command. |
| `close_command` | `bool` | `input` |  | Close command. |
| `open_limit` | `bool` | `output` |  | Open limit. |
| `closed_limit` | `bool` | `output` |  | Closed limit. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `valve_position` | rotary | `KIN_valve_stem` | 0 to 90 deg | 90 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **flanged industrial ball valve with actuator**.
Source-model review: **compared-pass** — Rendered review shows flanged ends, a central valve body, top stem/actuator interface, actuator body, and supports consistent with a generic actuated flanged ball valve family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Bray: Resolute Ball valve product page](https://www.bray.com/valves-actuators-controls/ball-valves/ball-valve-accessories/resolute-ball) | oem-product-page | 2026-09-22 |

Modeled family features:
- flanged valve ends
- central valve body
- top stem
- mounted actuator/drive interface

Intentionally generic / not claimed:
- No Bray mark, bore, pressure class, seal material, process compatibility, actuator torque, control signal, or shutoff performance is reproduced.
- The simulator valve state is symbolic and not a process or safety claim.
