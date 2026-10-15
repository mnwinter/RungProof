# Modular Plastic Belt Conveyor help

Asset ID: `material-handling.conveyor.modular-plastic-belt.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/conveyors/belt`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.8 m
- Height: 1.35 m
- Depth: 1.3 m
- Source: `res://assets/material_flow/modular_plastic_belt_conveyor/source/modular_plastic_belt_conveyor.blend`
- Delivery: `res://assets/material_flow/modular_plastic_belt_conveyor/delivery/modular_plastic_belt_conveyor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint` | `float32` | `input` | m/s | Speed setpoint. |
| `actual_speed` | `float32` | `output` | m/s | Actual speed. |
| `running` | `bool` | `output` |  | Running. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `belt_speed` | linear_continuous | `KIN_BELT_LINK_0_0` | -1.5 to 1.5 m/s | 1.2 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **plastic modular-belt conveyor with side guides**.
Source-model review: **compared-pass** — Fresh blind render reviewed against HabasitLINK plastic modular belt families: repeated interlocked plastic modules form a continuous carry surface between prominent side guides, with drive-end context and support legs. It reads as a generic modular plastic belt conveyor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Habasit: HabasitLINK plastic modular belts product guide](https://www.habasit.com/-/media/Project/Habasit/PublicWebSite/Documents/English/Products/Plastic-Modular/4178_Habasit-Link-Plastic-Modular-Belts-Product-Guide_0623_en.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- interlocked plastic modules
- continuous carry surface
- side guides
- drive-end context
- leg supports

Intentionally generic / not claimed:
- No Habasit mark, belt series, material, pitch, sprocket, width, speed, washdown, food suitability, load, or guard specification is reproduced.
- The rendered belt does not run a real conveyor or validate a product-transfer application.
