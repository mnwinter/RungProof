# Horizontal Pivot Bandsaw help

Asset ID: `machines.cutting.saw.horizontal-band.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/cutting/saws`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.8 m
- Height: 2.2 m
- Depth: 1.3 m
- Source: `res://assets/production-machines/horizontal_bandsaw/source/horizontal_bandsaw.blend`
- Delivery: `res://assets/production-machines/horizontal_bandsaw/delivery/horizontal_bandsaw.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `cycle_start` | `bool` | `input` |  | Cycle start. |
| `saw_run` | `bool` | `input` |  | Saw run. |
| `bow_down` | `bool` | `input` |  | Bow down. |
| `vise_clamp` | `bool` | `input` |  | Vise clamp. |
| `cut_complete` | `bool` | `output` |  | Cut complete. |
| `saw_fault` | `bool` | `output` |  | Saw fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `bow_angle` | rotary | `KIN_SAW_BOW` | 0 to 45 deg | 12 |
| `vise_position` | linear | `KIN_VISE_MOVING` | 0 to 0.35 m | 0.1 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **horizontal metal-cutting bandsaw**.
Source-model review: **compared-pass** — Fresh blind render shows the defining horizontal band-saw family form: a pivoting band head with paired wheels, thin exposed blade, material vise, coolant/base treatment, and pendant control. It reads as a generic horizontal bandsaw.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [DoALL: Band saw product families](https://www.doallsaws.com/) | oem-product-page | 2026-09-22 |

Modeled family features:
- pivoting saw head
- paired wheels
- band blade
- material vise
- coolant/base treatment
- control pendant

Intentionally generic / not claimed:
- No DoALL mark, blade size, cutting capacity, coolant system, motor rating, feed, clamp force, guarding, or certification is reproduced.
- The asset does not rotate a blade or cut material.
