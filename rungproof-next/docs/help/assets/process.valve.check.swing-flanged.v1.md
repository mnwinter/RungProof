# Flanged Swing Check Valve help

Asset ID: `process.valve.check.swing-flanged.v1`  
Catalog status: **candidate / candidate**  
Category: `process/valves/check`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.1 m
- Depth: 1.65 m
- Source: `res://assets/process_fluid/swing_check_valve/source/swing_check_valve.blend`
- Delivery: `res://assets/process_fluid/swing_check_valve/delivery/swing_check_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `flow_present` | `bool` | `output` |  | Flow present. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **flanged swing check valve with bolted cover**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Velan flanged swing-check valves: the rounded inline body has a large bolted top cover and an external hinge/lever-weight treatment, which distinguishes it from a manually operated valve. It reads as a generic flanged swing check valve.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Velan: API 594 cast steel swing check valve](https://velan.com/products/api-594-cast-steel-swing-check-valves/) | oem-product-page | 2026-09-22 |

Modeled family features:
- rounded inline body
- large bolted top cover
- flanged ends
- external hinge/lever-weight treatment

Intentionally generic / not claimed:
- No Velan mark, size, pressure class, disc, seat, hinge, cracking pressure, flow direction, temperature, flange standard, or service specification is reproduced.
- The model does not establish a functioning non-return or pressure-retaining device.
