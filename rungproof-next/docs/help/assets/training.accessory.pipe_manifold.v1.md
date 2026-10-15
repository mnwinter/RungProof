# Pipe Manifold help

Asset ID: `training.accessory.pipe_manifold.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.2 m
- Height: 0.85 m
- Depth: 1.05 m
- Source: `res://assets/training_accessories/pipe_manifold/source/pipe_manifold.blend`
- Delivery: `res://assets/training_accessories/pipe_manifold/delivery/pipe_manifold.glb`

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

Generic reference family: **multi-station pneumatic directional-valve manifold**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: VPA 5-port manifolds](https://www.smcusa.com/products/directional-control-valves/air-operated-valve/vpa/5-port/manifolds~51926) | oem-product-page | 2026-09-22 |

Modeled family features:
- seven repeated stations
- common manifold base
- top solenoid/pilot housings
- indicator-like details
- end plates
- supply/exhaust treatment

Intentionally generic / not claimed:
- No SMC mark, valve series, ports, thread, coil voltage, protocol, pressure, flow, pilot supply, wiring, exhaust treatment, or safety function is reproduced.
- The stations do not energize valves, direct air, or write physical I/O.
