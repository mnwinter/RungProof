# Horizontal Injection Molding Machine help

Asset ID: `machines.plastics.injection-molder.horizontal.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/plastics/injection-molding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.4 m
- Height: 2.7 m
- Depth: 1.7 m
- Source: `res://assets/production-machines/injection_molding_machine/source/injection_molding_machine.blend`
- Delivery: `res://assets/production-machines/injection_molding_machine/delivery/injection_molding_machine.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `cycle_start` | `bool` | `input` |  | Cycle start. |
| `clamp_close` | `bool` | `input` |  | Clamp close. |
| `screw_run` | `bool` | `input` |  | Screw run. |
| `barrel_temperature` | `float32` | `output` | degC | Barrel temperature. |
| `clamp_closed` | `bool` | `output` |  | Clamp closed. |
| `cycle_complete` | `bool` | `output` |  | Cycle complete. |
| `machine_fault` | `bool` | `output` |  | Machine fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `clamp_position` | linear | `KIN_MOVING_PLATEN` | 0 to 1.05 m | 0.35 |
| `screw_rotation` | continuous | `KIN_INJECTION_SCREW` | 0 to 360 deg | 360 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **horizontal injection molding machine**.
Source-model review: **compared-pass** — Fresh blind render compared with ENGEL horizontal injection machines: a clamp/tie-bar end connects to an injection barrel and hopper, with a separate controller treatment. It reads as a generic horizontal injection-molding machine family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ENGEL: Injection molding machines](https://www.engelglobal.com/us/products/injection-molding-machines) | oem-product-page | 2026-09-22 |

Modeled family features:
- clamp end
- tie bars
- injection barrel
- hopper
- controller treatment

Intentionally generic / not claimed:
- No ENGEL mark, clamp force, shot size, resin, barrel temperature, mold, hydraulics/electrics, guarding, or certification is reproduced.
- The visual does not heat resin, close a mold, inject material, or validate a safeguarding design.
