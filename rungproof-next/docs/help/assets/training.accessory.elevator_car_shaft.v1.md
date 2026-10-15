# Elevator Car/Shaft help

Asset ID: `training.accessory.elevator_car_shaft.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.45 m
- Depth: 2.95 m
- Source: `res://assets/training_accessories/elevator_car_shaft/source/elevator_car_shaft.blend`
- Delivery: `res://assets/training_accessories/elevator_car_shaft/delivery/elevator_car_shaft.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `bucket_speed` | linear_continuous | `KIN_BUCKET_BELT` | 0 to 1.5 m/s | 0.8 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **continuous bucket elevator for bulk materials**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Universal Industries: Twin Trunk Alpha Series bucket elevators](https://universalindustries.com/bucket-elevators-alphaseries/) | oem-product-page | 2026-09-22 |

Modeled family features:
- vertical trunk
- visible bucket run
- top head/discharge housing
- base boot
- drive treatment

Intentionally generic / not claimed:
- No Universal Industries mark, bucket type, belt/chain, capacity, speed, material compatibility, dust control, discharge method, inspection access, guarding, or installation specification is reproduced.
- No physical elevation or bulk-material handling is implied by the rendered asset.
