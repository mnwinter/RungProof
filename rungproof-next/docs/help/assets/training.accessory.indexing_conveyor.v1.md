# Indexing Conveyor help

Asset ID: `training.accessory.indexing_conveyor.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 6.2 m
- Height: 1.5 m
- Depth: 1.55 m
- Source: `res://assets/training_accessories/indexing_conveyor/source/indexing_conveyor.blend`
- Delivery: `res://assets/training_accessories/indexing_conveyor/delivery/indexing_conveyor.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `belt_speed` | linear_continuous | `KIN_belt_surface` | -2.0 to 2.0 m/s | 0.75 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **600 mm flat-belt conveyor with support legs**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

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
