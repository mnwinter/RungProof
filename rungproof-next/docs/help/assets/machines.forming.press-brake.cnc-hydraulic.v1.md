# CNC Hydraulic Press Brake help

Asset ID: `machines.forming.press-brake.cnc-hydraulic.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/forming/press-brakes`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.5 m
- Height: 2.3 m
- Depth: 1.7 m
- Source: `res://assets/production-machines/cnc_press_brake/source/cnc_press_brake.blend`
- Delivery: `res://assets/production-machines/cnc_press_brake/delivery/cnc_press_brake.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `cycle_start` | `bool` | `input` |  | Cycle start. |
| `beam_down` | `bool` | `input` |  | Beam down. |
| `bend_angle_setpoint` | `float32` | `input` | deg | Bend angle setpoint. |
| `beam_position` | `float32` | `output` | mm | Beam position. |
| `safety_clear` | `bool` | `output` |  | Safety clear. |
| `machine_fault` | `bool` | `output` |  | Machine fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `beam_position` | linear | `KIN_PRESS_BEAM` | 0 to 0.32 m | 0.12 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **CNC hydraulic press brake**.
Source-model review: **compared-pass** — Fresh blind render compared with TRUMPF CNC press brakes: a long portal frame carries an upper ram, lower die/bed, rear-side controller, and foot-pedal treatment. It reads as a generic CNC hydraulic press brake.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [TRUMPF: TruBend press brake portfolio](https://www.trumpf.com/en_GB/landing-pages/uk/press-brakes/) | oem-product-page | 2026-09-22 |

Modeled family features:
- portal frame
- upper ram
- lower die/bed
- controller
- foot pedal

Intentionally generic / not claimed:
- No TRUMPF mark, bend length, tonnage, tooling, backgauge, laser safety, program, or hydraulic specification is reproduced.
- The visual does not bend sheet metal or implement safety guarding.
