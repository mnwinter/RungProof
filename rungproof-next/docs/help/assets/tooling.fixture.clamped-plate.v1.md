# Clamped Plate Workholding Fixture help

Asset ID: `tooling.fixture.clamped-plate.v1`  
Catalog status: **candidate / candidate**  
Category: `tooling/fixtures`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.48 m
- Height: 0.36 m
- Depth: 0.92 m
- Source: `res://assets/scene_loads/clamped_plate_workholding/source/clamped_plate_workholding.blend`
- Delivery: `res://assets/scene_loads/clamped_plate_workholding/delivery/clamped_plate_workholding.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **toe-clamped plate workholding fixture**.
Source-model review: **compared-pass** — Rendered review compared with conventional toe-clamp workholding: a steel workpiece sits on a subplate with paired toe clamps, studs, washers, hex nuts, stepped support blocks, and locating pins. It reads as a generic clamped-plate fixture.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Jergens: Workholding solutions master catalog](https://www.jergensinc.com/Popular%20Catalog/Jergens%20WSG_Master%20Catalog.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- subplate
- steel workpiece
- toe clamps
- studs/washers/nuts
- step blocks
- locating pins

Intentionally generic / not claimed:
- No Jergens mark, clamp force, fastener grade, workpiece alloy, machining force, mounting torque, or safety approval is reproduced.
- The visual does not clamp a real part or validate machining setup.
