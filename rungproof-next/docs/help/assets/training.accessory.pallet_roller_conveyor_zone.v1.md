# Pallet Roller Conveyor Zone help

Asset ID: `training.accessory.pallet_roller_conveyor_zone.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.2 m
- Height: 1.35 m
- Depth: 2.25 m
- Source: `res://assets/training_accessories/pallet_roller_conveyor_zone/source/pallet_roller_conveyor_zone.blend`
- Delivery: `res://assets/training_accessories/pallet_roller_conveyor_zone/delivery/pallet_roller_conveyor_zone.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `roller_speed` | linear_continuous | `KIN_roller_00` | -1.0 to 1.0 m/s | 0.6 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **powered pallet roller conveyor with gear-motor drive**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

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
