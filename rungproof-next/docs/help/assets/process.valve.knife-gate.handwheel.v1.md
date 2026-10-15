# Handwheel Knife-Gate Valve help

Asset ID: `process.valve.knife-gate.handwheel.v1`  
Catalog status: **candidate / candidate**  
Category: `process/valves/slurry`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.3 m
- Height: 1.0 m
- Depth: 2.65 m
- Source: `res://assets/process_fluid/knife_gate_valve/source/knife_gate_valve.blend`
- Delivery: `res://assets/process_fluid/knife_gate_valve/delivery/knife_gate_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `open_command` | `bool` | `input` |  | Open command. |
| `close_command` | `bool` | `input` |  | Close command. |
| `position_percent` | `float32` | `output` | percent | Position percent. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `blade_position` | linear | `KIN_GATE_BLADE` | 0 to 0.7 m | 0.16 |
| `handwheel_angle` | angular_continuous | `KIN_HANDWHEEL` | -3600 to 3600 deg | 360 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **rising-blade knife gate valve with handwheel**.
Source-model review: **compared-pass** — Fresh blind render reviewed against DeZURIK knife-gate valves: a broad flat plate blade rises visibly through a tall open yoke from an inline body, driven by a central stem and handwheel. It reads as a generic handwheel knife gate valve.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [DeZURIK: Knife gate valves](https://www.dezurik.com/products/knife-gate-valves/) | oem-product-page | 2026-09-22 |

Modeled family features:
- flat rising blade
- tall open yoke
- inline body
- central stem
- handwheel
- flanged pipe context

Intentionally generic / not claimed:
- No DeZURIK mark, blade material, seat, size, pressure, slurry/media compatibility, packing, actuator, flange standard, shutoff, or service specification is reproduced.
- The blade does not isolate any physical process or verify a safe maintenance condition.
