# Handled HDPE Jerry Can help

Asset ID: `loads.container.jerry-can-hdpe.v1`  
Catalog status: **candidate / candidate**  
Category: `loads/containers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.64 m
- Height: 1.22 m
- Depth: 0.44 m
- Source: `res://assets/scene_loads/hdpe_jerry_can/source/hdpe_jerry_can.blend`
- Delivery: `res://assets/scene_loads/hdpe_jerry_can/delivery/hdpe_jerry_can.glb`

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

Generic reference family: **industrial HDPE jerrycan**.
Source-model review: **compared-pass** — Rendered review compared with Mauser industrial HDPE jerrycans: a rectangular blow-molded body has integrated handle, threaded neck/cap, shoulder transitions, and recessed panel treatments. It reads as a generic HDPE jerrycan.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Mauser Packaging Solutions: Plastic tight-head containers and jerrycans](https://mauserpackaging.com/products-innovations/plastic-tight-head-containers-jerrycans/) | oem-product-page | 2026-09-22 |

Modeled family features:
- rectangular blow-molded body
- integrated handle
- threaded neck/cap
- shoulder transitions
- recessed panels

Intentionally generic / not claimed:
- No Mauser mark, volume, resin grade, barrier, closure, UN approval, chemical compatibility, or transport rating is reproduced.
- The visual does not contain a product or establish chemical-handling suitability.
