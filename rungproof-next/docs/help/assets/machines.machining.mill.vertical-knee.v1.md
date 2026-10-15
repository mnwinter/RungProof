# Vertical Knee Milling Machine help

Asset ID: `machines.machining.mill.vertical-knee.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/machining/mills`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.9 m
- Depth: 1.8 m
- Source: `res://assets/production-machines/vertical_knee_mill/source/vertical_knee_mill.blend`
- Delivery: `res://assets/production-machines/vertical_knee_mill/delivery/vertical_knee_mill.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `spindle_run` | `bool` | `input` |  | Spindle run. |
| `spindle_speed` | `float32` | `input` | rpm | Spindle speed. |
| `table_feed` | `bool` | `input` |  | Table feed. |
| `mill_fault` | `bool` | `output` |  | Mill fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `spindle_rotation` | continuous | `KIN_SPINDLE` | 0 to 360 deg | 3600 |
| `table_x` | linear | `KIN_X_TABLE` | 0 to 1.1 m | 0.12 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **vertical knee mill**.
Source-model review: **compared-pass** — Fresh blind render compared with Sharp vertical knee mills: a tall column supports a knee and slotted table below a quill/spindle head with drive housing. It reads as a generic vertical knee-mill family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Sharp Industries: TMV-1 vertical knee mill](https://sharp-industries.com/product/tmv-1/) | oem-product-page | 2026-09-22 |

Modeled family features:
- column
- knee and slotted table
- quill/spindle head
- drive housing
- handwheel treatment

Intentionally generic / not claimed:
- No Sharp mark, travel, spindle taper, speed, motor rating, tooling, coolant, guarding, or certification is reproduced.
- The visual does not rotate a spindle, machine material, or establish a safe setup.
