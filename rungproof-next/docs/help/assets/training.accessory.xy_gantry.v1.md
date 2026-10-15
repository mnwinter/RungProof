# Xy Gantry help

Asset ID: `training.accessory.xy_gantry.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.5 m
- Height: 2.7 m
- Depth: 2.5 m
- Source: `res://assets/training_accessories/xy_gantry/source/xy_gantry.blend`
- Delivery: `res://assets/training_accessories/xy_gantry/delivery/xy_gantry.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `x_axis` | linear | `KIN_X_BRIDGE` | 0 to 2.6 m | 2.0 |
| `y_axis` | linear | `KIN_Y_CARRIAGE` | 0 to 1.4 m | 2.0 |
| `z_axis` | linear | `KIN_Z_AXIS` | 0 to 1.3 m | 1.5 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **XYZ cartesian gantry robot**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Festo: EXCM/EXCH planar gantry documentation](https://media.festo.com/media/4086_documentation.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- four support posts
- parallel rails
- transverse bridge
- carriage
- vertical Z axis
- tool treatment

Intentionally generic / not claimed:
- No Festo mark, axis travel, payload, motor, encoder, controller, cable management, guarding, or safety rating is reproduced.
- The kinematic paths are simulator-only and do not move physical axes.
