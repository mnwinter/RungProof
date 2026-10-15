# Seven-Station Pneumatic Solenoid Valve Manifold help

Asset ID: `pneumatics.valve-manifold.seven-station.v1`  
Catalog status: **candidate / candidate**  
Category: `pneumatics/directional-control`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.2 m
- Height: 0.85 m
- Depth: 1.05 m
- Source: `res://assets/process_fluid/pneumatic_valve_manifold/source/pneumatic_valve_manifold.blend`
- Delivery: `res://assets/process_fluid/pneumatic_valve_manifold/delivery/pneumatic_valve_manifold.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `valve_1_command` | `bool` | `input` |  | Valve 1 command. |
| `valve_2_command` | `bool` | `input` |  | Valve 2 command. |
| `valve_3_command` | `bool` | `input` |  | Valve 3 command. |
| `supply_pressure_ok` | `bool` | `output` |  | Supply pressure ok. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **multi-station pneumatic directional-valve manifold**.
Source-model review: **compared-pass** — Fresh blind render reviewed against SMC VPA manifold families: repeated valve stations line a long common base/manifold, each carrying a distinct top solenoid/pilot housing and indicator-like feature, with end plates and supply/exhaust treatment. It reads as a generic seven-station pneumatic valve manifold.

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
