# Pneumatic Globe Control Valve help

Asset ID: `process.valve.control.globe-diaphragm.v1`  
Catalog status: **candidate / candidate**  
Category: `process/valves/control`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.15 m
- Depth: 2.35 m
- Source: `res://assets/process_fluid/pneumatic_control_valve/source/pneumatic_control_valve.blend`
- Delivery: `res://assets/process_fluid/pneumatic_control_valve/delivery/pneumatic_control_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position_command` | `float32` | `input` | percent | Position command. |
| `position_feedback` | `float32` | `output` | percent | Position feedback. |
| `air_failure` | `bool` | `input` |  | Air failure. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `stem_position` | linear | `KIN_STEM` | 0 to 0.14 m | 0.1 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **spring-and-diaphragm actuated globe control valve**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Fisher diaphragm-actuated globe control-valve assemblies: an inline globe body carries a stem bonnet below a large spring/diaphragm actuator casing and a separate positioner-like instrument enclosure. It reads as a generic pneumatic globe control valve.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Emerson Fisher: Fisher 657 diaphragm actuator](https://www.emerson.com/en/final-control/products/fisher-657) | oem-product-page | 2026-09-22 |

Modeled family features:
- inline globe body
- bonnet
- large diaphragm actuator casing
- stem connection
- instrument/positioner treatment
- flanged pipe context

Intentionally generic / not claimed:
- No Fisher mark, valve size, Cv, trim, characteristic, actuator area, spring range, fail action, positioner, air supply, pressure class, flange standard, or process service is reproduced.
- Displayed motion is a local simulator visualization, never a live control-loop or physical valve command.
