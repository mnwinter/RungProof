# Powered Pallet Roller Conveyor - 1000 mm x 4 m help

Asset ID: `material-handling.pallet-roller-conveyor.1000x4000.v1`  
Catalog status: **production / approved**  
Category: `material-handling/conveyors/roller`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.2 m
- Height: 1.35 m
- Depth: 2.25 m
- Source: `res://assets/material_handling/pallet_roller_conveyor_1000x4000/source/pallet_roller_conveyor_1000x4000.blend`
- Delivery: `res://assets/material_handling/pallet_roller_conveyor_1000x4000/delivery/pallet_roller_conveyor_1000x4000.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Permissive run request from simulation logic. |
| `speed_setpoint` | `float32` | `input` | m/s | Signed commanded pallet speed. |
| `estop_ok` | `bool` | `input` |  | Guarded simulator safety permissive; not a real safety function. |
| `pallet_present` | `bool` | `output` |  | True when the modeled photoeye beam is blocked by a simulated load. |
| `running` | `bool` | `output` |  | True while roller motion is established. |
| `actual_speed` | `float32` | `output` | m/s | Ramp-limited simulated roller surface speed. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `roller_speed` | linear_continuous | `KIN_roller_00` | -1.0 to 1.0 m/s | 0.6 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `pallet_in` |  |  |
| `pallet_out` |  |  |
| `power_3phase` |  |  |

## Industrial reference basis

Generic reference family: **powered pallet roller conveyor with gear-motor drive**.
Source-model review: **compared-pass** — Rendered review shows a long pallet-scale roller bed with close-spaced steel rollers, rigid side frames, legs with anchor feet, braces, a below-frame motor/gearbox context, and basic operator hardware. It reads as a generic powered pallet roller-conveyor family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Interroll: PM 9710 Roller conveyor with gear motor](https://www.interroll.com/products/mpp/pm-9710) | oem-product-page | 2026-09-22 |

Modeled family features:
- steel roller bed
- side frames
- support legs and anchor feet
- under-frame motor/gearbox context
- operator-control context

Intentionally generic / not claimed:
- No Interroll mark, drive voltage, pallet capacity, roller pitch, speed, zone logic, guarding, controls architecture, or installation rating is reproduced.
- Conveyor motion and input points remain simulator-only.
