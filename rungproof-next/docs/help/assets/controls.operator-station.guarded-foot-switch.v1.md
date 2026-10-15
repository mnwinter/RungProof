# Guarded Industrial Foot Switch help

Asset ID: `controls.operator-station.guarded-foot-switch.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/operator-stations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.85 m
- Height: 0.7 m
- Depth: 1.0 m
- Source: `res://assets/controls_sensors/guarded_foot_switch/source/guarded_foot_switch.blend`
- Delivery: `res://assets/controls_sensors/guarded_foot_switch/delivery/guarded_foot_switch.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `pressed` | `bool` | `output` |  | Pressed. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **single-pedal industrial foot switch with protective shield**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Schmersal TFH 232 protected-foot-switch family: a single pedal sits in a substantial three-sided yellow protective shield with tread strips, a robust base, and visible cable-entry context. It reads as a generic guarded industrial foot switch.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schmersal: TFH 232 foot switch family](https://products.schmersal.com/en_GB/foot-switch-1000074849) | oem-product-page | 2026-09-22 |

Modeled family features:
- protective shield
- single pedal
- tread strips
- stable base
- cable-entry context

Intentionally generic / not claimed:
- No Schmersal mark, contact arrangement, switch rating, pressure point, interlock, safety category, or cable specification is reproduced.
- Pressing the simulator pedal is visual/symbolic behavior only and is not a safety function.
