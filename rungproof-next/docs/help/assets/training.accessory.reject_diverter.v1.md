# Reject Diverter help

Asset ID: `training.accessory.reject_diverter.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 1.25 m
- Depth: 1.45 m
- Source: `res://assets/training_accessories/reject_diverter/source/reject_diverter.blend`
- Delivery: `res://assets/training_accessories/reject_diverter/delivery/reject_diverter.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `diverter_angle` | angular | `KIN_DIVERTER_ARM` | 0 to 35 deg | 90 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **powered pivot-arm conveyor diverter**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Norpak: Powered pivot conveyor diverters](https://www.norpak.com/diverters/conveyor-equipment/products.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- roller conveyor
- carton context
- pivoted swing arm
- pivot mount
- drive/actuator treatment

Intentionally generic / not claimed:
- No Norpak mark, divergence angle, drive type, speed, carton size, capacity, sensor, guard, or control logic is reproduced.
- Arm position is symbolic simulator animation only and does not move physical product.
