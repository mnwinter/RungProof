# Tuning-Fork Point Level Switch help

Asset ID: `sensing.level.tuning-fork.v1`  
Catalog status: **production / approved**  
Category: `sensing/level`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.46 m
- Height: 2.104 m
- Depth: 0.435 m
- Source: `res://assets/scene_core/tuning_fork_level_switch/source/tuning_fork_level_switch.blend`
- Delivery: `res://assets/scene_core/tuning_fork_level_switch/delivery/tuning_fork_level_switch.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `covered` | `bool` | `output` |  | Covered. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **vibronic tuning-fork point level switch**.
Source-model review: **compared-pass** — The corrected review shows a compact neutral instrument head, small status indicator, top M12 connector, sealed neck, hex process fitting and thread context, extension stem, and symmetric two-prong fork. It reads as a generic vibronic tuning-fork point level-switch family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Endress+Hauser: Liquiphant FTL43 vibronic point level switch](https://www.endress.com/en/field-instruments-overview/level-measurement/Vibronics-Liquiphant-FTL43) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact instrument head
- neutral status indicator
- process fitting
- extension tube
- two-prong tuning-fork probe
- connector/cable entry

Intentionally generic / not claimed:
- No Endress+Hauser mark, hygienic rating, process rating, density limit, switch point, safety function, output, material, or installation instruction is reproduced.
- The fork is static simulator geometry and does not perform a point-level safety or process function.
