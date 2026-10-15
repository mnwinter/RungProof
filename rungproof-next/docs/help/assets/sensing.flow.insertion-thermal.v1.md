# Insertion Thermal Flow Sensor on Pipe Spool help

Asset ID: `sensing.flow.insertion-thermal.v1`  
Catalog status: **candidate / candidate**  
Category: `sensing/flow`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.65 m
- Height: 1.3 m
- Depth: 0.8 m
- Source: `res://assets/controls_sensors/thermal_flow_sensor/source/thermal_flow_sensor.blend`
- Delivery: `res://assets/controls_sensors/thermal_flow_sensor/delivery/thermal_flow_sensor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `flow_m3h` | `float32` | `output` | m3/h | Flow m3h. |
| `flow_switch` | `bool` | `output` |  | Flow switch. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **thermal insertion flow sensor on a process pipe**.
Source-model review: **compared-pass** — Rebuilt review render shows a plain insertion probe through a compression fitting into a pipe and removes the prior invented external sensing fins.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm electronic: SA thermal flow sensor installation guidelines](https://www.ifm.com/us/en/us/learn-more/flow/sa-learn-more/installation-guidelines) | oem-product-page | 2026-09-22 |

Modeled family features:
- long insertion probe entering a pipe through a compression fitting
- instrument body above the process connection
- top electrical connector/display area
- pipe-spool installation context

Intentionally generic / not claimed:
- No ifm mark, probe length, pipe-size limit, insertion calculation, flow range, pressure limit, output type, or accuracy is reproduced.
- flow_m3h and flow_switch remain symbolic simulator outputs only.
