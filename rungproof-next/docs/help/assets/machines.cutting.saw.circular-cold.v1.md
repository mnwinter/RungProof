# Pivoting Circular Cold Saw help

Asset ID: `machines.cutting.saw.circular-cold.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/cutting/saws`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.3 m
- Depth: 1.4 m
- Source: `res://assets/production-machines/cold_saw/source/cold_saw.blend`
- Delivery: `res://assets/production-machines/cold_saw/delivery/cold_saw.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `blade_run` | `bool` | `input` |  | Blade run. |
| `head_down` | `bool` | `input` |  | Head down. |
| `vise_clamped` | `bool` | `output` |  | Vise clamped. |
| `cut_complete` | `bool` | `output` |  | Cut complete. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `blade_rotation` | continuous | `KIN_SAW_BLADE` | 0 to 360 deg | 1800 |
| `head_angle` | rotary | `KIN_SAW_HEAD_ARM` | 0 to 55 deg | 18 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **manual circular cold saw**.
Source-model review: **compared-pass** — Fresh blind render shows the defining cold-saw form: a guarded circular blade on a pivoting head, horizontal work vise, coolant/base treatment, and operator handle. It reads as a generic circular cold saw.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Everett Industries: Industrial circular saws](https://www.everettindustries.com/) | oem-product-page | 2026-09-22 |

Modeled family features:
- guarded circular blade
- pivoting head
- material vise
- operator handle
- coolant/base treatment

Intentionally generic / not claimed:
- No manufacturer mark, blade diameter, speed, material capacity, coolant, clamp, motor, guarding validation, or certification is reproduced.
- The model does not rotate a blade or cut material.
