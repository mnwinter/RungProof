# Inline Tote Capping Station help

Asset ID: `process.packaging.capper.tote-inline.v1`  
Catalog status: **candidate / candidate**  
Category: `process/packaging/capping`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.8 m
- Depth: 1.7 m
- Source: `res://assets/tote_processing/tote_capping_station/source/tote_capping_station.blend`
- Delivery: `res://assets/tote_processing/tote_capping_station/delivery/tote_capping_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `capper_run` | `bool` | `input` |  | Capper run. |
| `cap_present` | `bool` | `output` |  | Cap present. |
| `capper_fault` | `bool` | `output` |  | Capper fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `capper_rotation` | continuous | `KIN_capper_spindle` | 0 to 360 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **inline container capping station**.
Source-model review: **compared-pass** — Rendered review compared with PackWest inline capping and cap-handling systems: a cap hopper/funnel and feed chute lead to a central torque-chuck spindle above a tote closure, with cap-rail, escarpment, drive, guarding, and local controls. It reads as a generic inline closure-handling/capping station.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ProMach PackWest: Inline capping and cap handling systems](https://www.promachbuilt.com/our-brands/packwest/) | oem-product-page | 2026-09-22 |

Modeled family features:
- cap hopper/funnel
- cap feed chute
- cap rail
- torque chuck spindle
- drive motor
- guard treatment
- local controls

Intentionally generic / not claimed:
- No PackWest mark, cap size, torque, throughput, change parts, drive details, guarding validation, or certification is reproduced.
- The asset does not place or torque a closure and does not prove package integrity.
