# Machine Vise with Rectangular 4140 Stock help

Asset ID: `tooling.workholding.machine-vise-stock.v1`  
Catalog status: **production / approved**  
Category: `tooling/workholding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.18 m
- Height: 0.61 m
- Depth: 0.8 m
- Source: `res://assets/scene_loads/rectangular_machining_blank/source/rectangular_machining_blank.blend`
- Delivery: `res://assets/scene_loads/rectangular_machining_blank/delivery/rectangular_machining_blank.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **manual precision machine vise with rectangular workpiece**.
Source-model review: **compared-pass** — Rendered review shows a low machine vise with fixed and moving jaws, a central workpiece, a screw/handle drive, rigid body, and slotted mounting-base context. It reads as a generic precision machine-vise family holding rectangular stock.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Kurt Workholding: Kurt Workholding Solutions](https://www.kurt.com/kurt-workholding-solutions/) | oem-product-page | 2026-09-22 |

Modeled family features:
- fixed jaw
- moving jaw
- rectangular workpiece
- screw/handle drive
- rigid body
- slotted mounting-base context

Intentionally generic / not claimed:
- No Kurt mark, jaw width, clamping force, repeatability, workpiece alloy, torque, fixture key, mounting procedure, machining parameters, or guarding claim is reproduced.
- The model is visual workholding context, not an operating setup instruction.
