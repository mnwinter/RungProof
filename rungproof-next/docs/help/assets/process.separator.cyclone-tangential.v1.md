# Tangential-Inlet Cyclone Separator help

Asset ID: `process.separator.cyclone-tangential.v1`  
Catalog status: **candidate / candidate**  
Category: `process/separation/dust`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.2 m
- Height: 1.8 m
- Depth: 3.45 m
- Source: `res://assets/process_fluid/cyclone_separator/source/cyclone_separator.blend`
- Delivery: `res://assets/process_fluid/cyclone_separator/delivery/cyclone_separator.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `airflow` | `float32` | `input` | m3/h | Airflow. |
| `dust_level_high` | `bool` | `output` |  | Dust level high. |
| `differential_pressure` | `float32` | `output` | Pa | Differential pressure. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |
| `process_3` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **tangential-inlet industrial cyclone separator**.
Source-model review: **compared-pass** — All four fresh blind-review angles were inspected against Donaldson cyclone dust collectors: the upper cylindrical barrel, clearly tangential side inlet, top clean-air outlet, conical hopper, lower discharge, and four-leg support form a credible generic cyclone separator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Donaldson Torit: Cyclone dust collectors](https://www.donaldson.com/en/products/cyclone-dust-collector/) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylindrical barrel
- tangential side inlet
- top outlet
- conical hopper
- lower discharge
- four-leg support

Intentionally generic / not claimed:
- No Donaldson mark, airflow, separation efficiency, dust type/loading, temperature, pressure, fan, explosion protection, outlet handling, or installation specification is reproduced.
- The model does not separate air or dust and makes no environmental or safety-performance claim.
