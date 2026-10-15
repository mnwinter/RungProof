# Powered Conveyor Swing-Arm Diverter help

Asset ID: `material-handling.diverter.powered-swing-arm.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/diverters`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 1.25 m
- Depth: 1.45 m
- Source: `res://assets/material_flow/powered_diverter_arm/source/powered_diverter_arm.blend`
- Delivery: `res://assets/material_flow/powered_diverter_arm/delivery/powered_diverter_arm.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `divert_command` | `bool` | `input` |  | Divert command. |
| `home_command` | `bool` | `input` |  | Home command. |
| `diverted` | `bool` | `output` |  | Diverted. |
| `home` | `bool` | `output` |  | Home. |

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
Source-model review: **compared-pass** — Fresh blind render reviewed against Norpak powered pivot-diverter families: a roller conveyor carries a carton while a contrasting pivoted swing arm reaches into the conveying plane, with a drive/actuator treatment at the pivot. It reads as a generic powered conveyor diverter.

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
