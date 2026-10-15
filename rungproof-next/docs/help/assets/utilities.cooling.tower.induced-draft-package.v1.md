# Package Induced-Draft Cooling Tower help

Asset ID: `utilities.cooling.tower.induced-draft-package.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/cooling/towers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.9 m
- Depth: 2.8 m
- Source: `res://assets/utilities/induced_draft_cooling_tower/source/induced_draft_cooling_tower.blend`
- Delivery: `res://assets/utilities/induced_draft_cooling_tower/delivery/induced_draft_cooling_tower.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `fan_command` | `bool` | `input` |  | Fan command. |
| `basin_temperature` | `float32` | `output` | degC | Basin temperature. |
| `low_level_alarm` | `bool` | `output` |  | Low level alarm. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `fan_rotation` | continuous | `KIN_TOWER_FAN_RING` | 0 to 360 deg | 1080 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **induced-draft counterflow package cooling tower**.
Source-model review: **compared-pass** — Fresh blind render compared with BAC induced-draft counterflow cooling towers: the square package has a top fan stack, large intake louvers with visible fill-pack treatment, a cold-water basin, and side process connections. It reads as a generic induced-draft package cooling tower.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Baltimore Aircoil Company: PT2 cooling tower](https://baltimoreaircoil.com/products/cooling-towers/pt2-cooling-tower) | oem-product-page | 2026-09-22 |

Modeled family features:
- top fan stack
- intake louvers
- fill-pack treatment
- cold-water basin
- process connections

Intentionally generic / not claimed:
- No BAC mark, fan size, fill media, water flow, evaporation rate, drift control, basin volume, make-up, treatment, or performance rating is reproduced.
- The visual does not circulate water, reject heat, or create a biological-control claim.
