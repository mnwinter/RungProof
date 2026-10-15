# Liquid Metering Pump Skid help

Asset ID: `process.dosing.skid.liquid-metering.v1`  
Catalog status: **candidate / candidate**  
Category: `process/dosing/metering`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.3 m
- Height: 1.9 m
- Depth: 1.5 m
- Source: `res://assets/scene_support/liquid_metering_skid/source/liquid_metering_skid.blend`
- Delivery: `res://assets/scene_support/liquid_metering_skid/delivery/liquid_metering_skid.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `metering_pump_run` | `bool` | `input` |  | Metering pump run. |
| `flow_proven` | `bool` | `output` |  | Flow proven. |
| `metering_fault` | `bool` | `output` |  | Metering fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pump_rotation` | continuous | `KIN_metering_pump_shaft` | 0 to 360 deg | 1750 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **diaphragm liquid-metering skid**.
Source-model review: **compared-pass** — Rendered review compared with Milton Roy process diaphragm metering-pump architecture: a motor/reducer drives a diaphragm-pump head with dampener, calibration column, suction/discharge path, check-valve/flange treatments, gauge, and local panel. It reads as a generic liquid-metering skid.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Milton Roy: Series Process Diaphragm Pump](https://www.miltonroy.com/api/get-file?id=CONT05B4D727AFCC4FE9B73AF6B907AF68A1) | oem-datasheet | 2026-09-22 |

Modeled family features:
- motor/reducer
- diaphragm pump head
- dampener
- calibration column
- suction/discharge piping
- gauge
- local panel

Intentionally generic / not claimed:
- No Milton Roy mark, capacity, pressure, chemistry, materials, calibration, controls, containment, or certification is reproduced.
- The visual does not pump, meter, or contain a process chemical.
