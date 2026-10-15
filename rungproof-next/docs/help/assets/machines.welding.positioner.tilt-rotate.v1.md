# Tilt-Rotate Welding Positioner help

Asset ID: `machines.welding.positioner.tilt-rotate.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/welding/positioners`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.8 m
- Height: 1.8 m
- Depth: 1.7 m
- Source: `res://assets/production-machines/welding_positioner/source/welding_positioner.blend`
- Delivery: `res://assets/production-machines/welding_positioner/delivery/welding_positioner.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `rotate_command` | `bool` | `input` |  | Rotate command. |
| `rotation_speed` | `float32` | `input` | rpm | Rotation speed. |
| `tilt_setpoint` | `float32` | `input` | deg | Tilt setpoint. |
| `positioner_fault` | `bool` | `output` |  | Positioner fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `table_rotation` | continuous | `KIN_ROTARY_TABLE` | 0 to 360 deg | 30 |
| `table_tilt` | rotary | `KIN_TILT_TRUNNION` | 0 to 135 deg | 20 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **tilt-rotate welding positioner**.
Source-model review: **compared-pass** — Fresh blind render compared with Lincoln Electric welding positioners: a tilted circular fixture table is carried by a pedestal with neutral drive/foot-control treatment. It reads as a generic tilt-rotate welding-positioner family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Lincoln Electric: Welding positioners](https://promotions.lincolnelectric.com/positioners/) | oem-product-page | 2026-09-22 |

Modeled family features:
- tilted circular fixture table
- pedestal
- drive treatment
- foot-control treatment

Intentionally generic / not claimed:
- No Lincoln mark, load capacity, rotation speed, tilt range, controls, grounding, welding process, or certification is reproduced.
- The visual does not rotate a workpiece, supply welding current, or establish a safe welding cell.
