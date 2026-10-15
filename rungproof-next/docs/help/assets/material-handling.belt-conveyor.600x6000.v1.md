# Belt Conveyor - 600 mm x 6 m help

Asset ID: `material-handling.belt-conveyor.600x6000.v1`  
Catalog status: **production / approved**  
Category: `material-handling/conveyors/belt`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 6.2 m
- Height: 1.5 m
- Depth: 1.55 m
- Source: `res://assets/material_handling/belt_conveyor_600x6000/source/belt_conveyor_600x6000.blend`
- Delivery: `res://assets/material_handling/belt_conveyor_600x6000/delivery/belt_conveyor_600x6000.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Permissive run request from the simulation logic. |
| `speed_setpoint` | `float32` | `input` | m/s | Signed commanded belt speed. |
| `estop_ok` | `bool` | `input` |  | Guarded simulator safety permissive; not a real safety function. |
| `running` | `bool` | `output` |  | True while simulated belt motion is established. |
| `actual_speed` | `float32` | `output` | m/s | Ramp-limited simulated belt speed. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `belt_speed` | linear_continuous | `KIN_belt_surface` | -2.0 to 2.0 m/s | 0.75 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `material_in` |  |  |
| `material_out` |  |  |
| `power_24vdc` |  |  |

## Industrial reference basis

Generic reference family: **600 mm flat-belt conveyor with support legs**.
Source-model review: **compared-pass** — Rendered drive detail shows a flat-belt/end-roller conveyor with supported frame, motorized drive end, return path, and adjustable legs. It remains intentionally generic rather than a Dorner configuration.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Dorner Conveyors: GAL-25-VUA Flat Belt Conveyor product page](https://www.dornerconveyors.com/europe/en/gal-ges/gal-25-vua-flat-belt-conveyor) | oem-product-page | 2026-09-22 |

Modeled family features:
- flat belt and end rollers
- side frame
- drive/end structure
- multiple adjustable support legs

Intentionally generic / not claimed:
- No Dorner mark, exact profile, drive option, belt construction, load capacity, electrical specification, or guarding configuration is reproduced.
- Motion and I/O remain scene-declared simulator behavior only.
