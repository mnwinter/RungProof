# Multi-Pipe Support Rack help

Asset ID: `process.support.multi-pipe-rack.v1`  
Catalog status: **production / approved**  
Category: `process/supports`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.7 m
- Height: 1.85 m
- Depth: 0.75 m
- Source: `res://assets/factory_kit/trapeze_pipe_support/source/trapeze_pipe_support.blend`
- Delivery: `res://assets/factory_kit/trapeze_pipe_support/delivery/trapeze_pipe_support.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **floor-mounted multi-pipe support rack with clamps**.
Source-model review: **compared-pass** — Rendered review shows parallel pipe runs in U-bolt clamps on a rigid steel rack with cross beams, braced legs, and floor-anchor plates. It reads as a generic multi-pipe support rack family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Anvil International: Pipe Hangers and Supports catalog](https://www.anvilintl.com/PDF/PH-2.12.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- parallel pipe runs
- U-bolt clamp context
- cross beams
- braced legs
- floor-anchor plates

Intentionally generic / not claimed:
- No Anvil mark, pipe size, material, support spacing, load calculation, vibration restraint, anchor specification, thermal-expansion treatment, or code compliance is reproduced.
- The asset is not pipe-support engineering evidence.
