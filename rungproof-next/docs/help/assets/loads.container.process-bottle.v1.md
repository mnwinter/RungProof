# Reusable Capped Process Bottle help

Asset ID: `loads.container.process-bottle.v1`  
Catalog status: **candidate / candidate**  
Category: `loads/containers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.54 m
- Height: 1.2 m
- Depth: 0.54 m
- Source: `res://assets/scene_loads/reusable_process_bottle/source/reusable_process_bottle.blend`
- Delivery: `res://assets/scene_loads/reusable_process_bottle/delivery/reusable_process_bottle.glb`

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

Generic reference family: **HDPE wide-mouth process bottle**.
Source-model review: **compared-pass** — Rendered review compared with HDPE wide-mouth bottle families: a cylindrical polymer bottle includes shoulder, threaded neck, removable cap, and simple label panel. It reads as a generic reusable process bottle.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Nalgene: HDPE Ultralite bottle collection](https://nalgene.com/collections/ultralite/) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical polymer body
- shoulder
- threaded neck
- cap
- label panel

Intentionally generic / not claimed:
- No Nalgene mark, volume, thread, resin grade, closure, chemical compatibility, sterilization, pressure, or regulatory claim is reproduced.
- The visual does not contain or dispense a process material.
