# Flanged Process Pipe Spool help

Asset ID: `process.pipe.flanged-spool.v1`  
Catalog status: **production / approved**  
Category: `process/piping`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.6 m
- Height: 1.2 m
- Depth: 0.8 m
- Source: `res://assets/scene_core/flanged_pipe_spool/source/flanged_pipe_spool.blend`
- Delivery: `res://assets/scene_core/flanged_pipe_spool/delivery/flanged_pipe_spool.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **flanged process-pipe spool with instrument branch**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the documented flanged spool family: a continuous straight process pipe has bolted end flanges, a supported horizontal run, a welded-looking vertical branch neck and branch flange, and a stem-connected pressure-gauge form rather than a floating disc. It is deliberately a generic instrumented spool, not a certified pressure boundary, specified expansion joint, or process design.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Metraflex: Spool Type Rubber Expansion Joint 100 HT](https://www.metraflex.com/products/100-ht-spool-type/) | oem-product-page | 2026-09-22 |

Modeled family features:
- straight pipe spool
- two flange faces
- bolting pattern
- branch connection
- instrument mounting
- pipe supports

Intentionally generic / not claimed:
- No Metraflex mark, material, pressure class, pipe schedule, flange standard, gasket, bolt torque, instrument type, process medium, or piping-design claim is reproduced.
- The model is not process-design or pressure-boundary evidence.
