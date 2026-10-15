# Analog Pressure Gauge help

Asset ID: `instrumentation.pressure.analog-gauge.v1`  
Catalog status: **production / approved**  
Category: `instrumentation/pressure`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.85 m
- Height: 1.15 m
- Depth: 0.55 m
- Source: `res://assets/controls_sensors/analog_pressure_gauge/source/analog_pressure_gauge.blend`
- Delivery: `res://assets/controls_sensors/analog_pressure_gauge/delivery/analog_pressure_gauge.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **industrial dial/analog pressure gauge with process connection**.
Source-model review: **compared-pass** — The corrected review shows a neutral bottom-connected dial gauge with a dark cylindrical case, metal bezel, dial ticks, pointer and hub, short stem, hex fitting, and threaded process connection. It reads as a generic industrial analog pressure-gauge family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Ashcroft: Pressure Gauges for Reliable Measurements](https://www.ashcroft.com/products/pressure/pressure-gauges/) | oem-product-page | 2026-09-22 |

Modeled family features:
- circular instrument case
- bezel
- neutral dial
- pointer
- bottom or back process connection
- threaded fitting context

Intentionally generic / not claimed:
- No Ashcroft mark, pressure range, accuracy, process medium, material, connection size, liquid fill, calibration, certification, or installation direction is reproduced.
- The dial and pointer are visual-only simulator context, not an instrument reading or pressure-boundary claim.
