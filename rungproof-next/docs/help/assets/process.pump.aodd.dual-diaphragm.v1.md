# Air-Operated Double-Diaphragm Pump help

Asset ID: `process.pump.aodd.dual-diaphragm.v1`  
Catalog status: **candidate / candidate**  
Category: `process/pumps/positive-displacement`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.5 m
- Height: 1.05 m
- Depth: 1.35 m
- Source: `res://assets/process_fluid/air_operated_double_diaphragm_pump/source/air_operated_double_diaphragm_pump.blend`
- Delivery: `res://assets/process_fluid/air_operated_double_diaphragm_pump/delivery/air_operated_double_diaphragm_pump.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `air_enable` | `bool` | `input` |  | Air enable. |
| `stroke_rate` | `float32` | `input` | cycles/min | Stroke rate. |
| `running` | `bool` | `output` |  | Running. |
| `leak_alarm` | `bool` | `output` |  | Leak alarm. |

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

Generic reference family: **air-operated double-diaphragm fluid pump**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Graco Husky AODD pump families: two large opposed round fluid chambers join a central air-valve body beneath a shared manifold, with multiple bolted covers and base mounting. It reads as a generic air-operated double-diaphragm pump.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Graco: Husky air-operated double diaphragm pumps](https://www.graco.com/gb/en/lp/pro/husky-air-operated-double-diaphragm-pump.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- opposed round diaphragm chambers
- central air-valve body
- shared manifold
- bolted chamber covers
- base mount

Intentionally generic / not claimed:
- No Graco mark, pump size, diaphragm/seat/ball material, port size, fluid compatibility, flow, pressure, air supply, pulsation, grounding, or hazardous-area suitability is reproduced.
- The asset never transfers fluid or uses compressed air.
