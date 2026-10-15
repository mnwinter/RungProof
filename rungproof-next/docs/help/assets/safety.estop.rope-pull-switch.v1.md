# Rope-Pull Emergency-Stop Switch help

Asset ID: `safety.estop.rope-pull-switch.v1`  
Catalog status: **candidate / candidate**  
Category: `safety/emergency-stop`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.2 m
- Height: 0.95 m
- Depth: 0.65 m
- Source: `res://assets/controls_sensors/rope_pull_estop_station/source/rope_pull_estop_station.blend`
- Delivery: `res://assets/controls_sensors/rope_pull_estop_station/delivery/rope_pull_estop_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `estop_ok` | `bool` | `output` |  | Estop ok. |
| `rope_tripped` | `bool` | `output` |  | Rope tripped. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial pull-wire emergency-stop switch**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Schmersal pull-wire emergency-stop family: a red enclosure is attached to a support, with rope routed through the actuator axis, eyelets along both directions, a visible reset-style operator, and a restrained front plate. It reads as a generic rope-pull switch installation form.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schmersal: Pull-wire emergency stop switches](https://products.schmersal.com/en_US/pull-wire-emergency-stop-switches-1000074527) | oem-product-page | 2026-09-22 |

Modeled family features:
- enclosure
- dual-direction rope path
- rope guide eyelets
- mounting support
- reset/operator context

Intentionally generic / not claimed:
- No Schmersal mark, wire length, switch contacts, tension setting, safety performance, wiring, installation, or regulatory use is reproduced.
- This model never establishes a protective function or operates physical equipment.
