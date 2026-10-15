# Flanged Globe Valve help

Asset ID: `process.valve.globe.flanged-handwheel.v1`  
Catalog status: **candidate / candidate**  
Category: `process/valves/throttling`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.0 m
- Depth: 2.25 m
- Source: `res://assets/process_fluid/flanged_globe_valve/source/flanged_globe_valve.blend`
- Delivery: `res://assets/process_fluid/flanged_globe_valve/delivery/flanged_globe_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position_command` | `float32` | `input` | percent | Position command. |
| `position_percent` | `float32` | `output` | percent | Position percent. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `stem_position` | linear | `KIN_STEM` | 0 to 0.18 m | 0.08 |
| `handwheel_angle` | angular_continuous | `KIN_HANDWHEEL` | -1800 to 1800 deg | 300 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **flanged globe valve with handwheel**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Velan API 623 flanged globe-valve family: a rounded globe body joins inline flanged ends beneath a central bonnet, stem, and handwheel. It reads as a generic flanged handwheel globe valve.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Velan: API 623 flanged globe valve product family](https://velan.com/certificates/ce-0056-ped-h-vel-001-22/) | oem-datasheet | 2026-09-22 |

Modeled family features:
- rounded globe body
- inline flanged ends
- central bonnet
- stem
- handwheel

Intentionally generic / not claimed:
- No Velan mark, size, pressure class, flow direction, plug/seat trim, packing, temperature, flange standard, shutoff, torque, or service specification is reproduced.
- The asset makes no pressure-boundary, flow-control, or physical I/O claim.
