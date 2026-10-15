# Flanged Rising-Stem Gate Valve help

Asset ID: `process.valve.gate.flanged-handwheel.v1`  
Catalog status: **candidate / candidate**  
Category: `process/valves/isolation`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 0.9 m
- Depth: 2.35 m
- Source: `res://assets/process_fluid/flanged_gate_valve/source/flanged_gate_valve.blend`
- Delivery: `res://assets/process_fluid/flanged_gate_valve/delivery/flanged_gate_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `open_command` | `bool` | `input` |  | Open command. |
| `close_command` | `bool` | `input` |  | Close command. |
| `position_percent` | `float32` | `output` | percent | Position percent. |
| `open_limit` | `bool` | `output` |  | Open limit. |
| `closed_limit` | `bool` | `output` |  | Closed limit. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `stem_position` | linear | `KIN_STEM` | 0 to 0.34 m | 0.12 |
| `handwheel_angle` | angular_continuous | `KIN_HANDWHEEL` | -3600 to 3600 deg | 360 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **flanged rising-stem wedge gate valve with handwheel**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Velan API 600 flanged gate-valve family: a rectangular bonnet/yoke above a flanged straight-through body supports a rising stem and large handwheel. It reads as a generic flanged handwheel gate valve.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Velan: API 600 gate, globe, and swing-check valve product family](https://velan.com/certificates/ce-0056-ped-h-vel-001-22/) | oem-datasheet | 2026-09-22 |

Modeled family features:
- flanged inline body
- rectangular bonnet/yoke
- rising stem
- large handwheel
- body-to-bonnet fasteners

Intentionally generic / not claimed:
- No Velan mark, size, pressure class, wedge design, trim, packing, temperature, flange standard, shutoff, torque, or service specification is reproduced.
- Handwheel position is visual only; it neither isolates fluid nor controls physical I/O.
