# Pressure-Sensitive Industrial Safety Mat help

Asset ID: `safety.mat.pressure-sensitive-2x1m.v1`  
Catalog status: **candidate / candidate**  
Category: `safety/presence-sensing`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.35 m
- Height: 0.25 m
- Depth: 1.3 m
- Source: `res://assets/controls_sensors/industrial_safety_mat/source/industrial_safety_mat.blend`
- Delivery: `res://assets/controls_sensors/industrial_safety_mat/delivery/industrial_safety_mat.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `field_clear` | `bool` | `output` |  | Field clear. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial pressure-sensitive safety mat**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Tapeswitch ControlMat family: a low rectangular mat has a high-contrast perimeter, ribbed tread surface, a molded cable exit, and a compact termination housing. It reads as a generic pressure-sensitive industrial mat form.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Tapeswitch: ControlMat industrial-duty pressure-sensitive safety mat](https://www.tapeswitch.com/mats/ckp.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- low-profile rectangular mat
- perimeter edge
- ribbed tread
- cable exit
- termination housing

Intentionally generic / not claimed:
- No Tapeswitch mark, dimensions, sensing zones, force threshold, environmental rating, controller, safety rating, or installation instruction is reproduced.
- The simulator mat does not detect personnel or provide a safety input.
