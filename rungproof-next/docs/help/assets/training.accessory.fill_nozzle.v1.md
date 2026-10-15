# Fill Nozzle help

Asset ID: `training.accessory.fill_nozzle.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.8 m
- Depth: 1.7 m
- Source: `res://assets/training_accessories/fill_nozzle/source/fill_nozzle.blend`
- Delivery: `res://assets/training_accessories/fill_nozzle/delivery/fill_nozzle.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `nozzle_position` | linear | `KIN_fill_nozzle` | 0 to 0.12 m | 0.35 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **tote liquid filling station**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [National Bulk Equipment: Liquid drum and tote filling systems](https://www.nbe-inc.com/packaging-systems/liquid-drum-and-tote-fillers) | oem-product-page | 2026-09-22 |

Modeled family features:
- filling mast
- fill nozzle
- caged tote
- product piping
- metering/sight treatment
- reservoir treatment
- drip tray
- local controls

Intentionally generic / not claimed:
- No NBE mark, fill method, flow, capacity, fluid compatibility, accuracy, scale, classification, sanitation, or certification is reproduced.
- The visible liquid is explanatory artwork; the asset does not move, meter, or contain fluid.
