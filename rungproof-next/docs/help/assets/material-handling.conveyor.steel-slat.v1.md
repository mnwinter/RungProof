# Steel Slat Conveyor help

Asset ID: `material-handling.conveyor.steel-slat.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/conveyors/slat`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.8 m
- Height: 1.35 m
- Depth: 1.25 m
- Source: `res://assets/material_flow/steel_slat_conveyor/source/steel_slat_conveyor.blend`
- Delivery: `res://assets/material_flow/steel_slat_conveyor/delivery/steel_slat_conveyor.glb`

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
| `slat_speed` | linear_continuous | `KIN_SLAT_0` | -1.2 to 1.2 m/s | 1.0 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial steel-slat apron conveyor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against ATS steel-slat apron conveyors: a full-width deck is visibly composed of repeated transverse steel slats within a deep rigid frame, with legged support and end-drive context. It reads as a generic industrial steel slat conveyor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ATS Group: Steel slat conveyor](https://www.ats-group.com/EN/product-solutions/products/slat-steel-conveyor.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- repeated transverse slats
- full-width deck
- rigid side frame
- leg supports
- end-drive context

Intentionally generic / not claimed:
- No ATS mark, slat dimensions/material, deck loading, speed, drive, pit, covers, fencing, or safety specification is reproduced.
- The model is not an operator-walkway or live conveyor safety representation.
