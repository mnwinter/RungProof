# Three-Piece Filter-Regulator-Lubricator help

Asset ID: `pneumatics.air-preparation.frl-three-piece.v1`  
Catalog status: **candidate / candidate**  
Category: `pneumatics/air-preparation`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.6 m
- Height: 0.85 m
- Depth: 1.55 m
- Source: `res://assets/process_fluid/filter_regulator_lubricator/source/filter_regulator_lubricator.blend`
- Delivery: `res://assets/process_fluid/filter_regulator_lubricator/delivery/filter_regulator_lubricator.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `pressure_setpoint` | `float32` | `input` | bar | Pressure setpoint. |
| `outlet_pressure` | `float32` | `output` | bar | Outlet pressure. |
| `filter_dp_alarm` | `bool` | `output` |  | Filter dp alarm. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pressure_setting` | angular_continuous | `KIN_PRESSURE_KNOB` | 0 to 1080 deg | 180 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **modular filter-regulator-lubricator air-preparation assembly**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SMC modular FRL family: three serial cylindrical modules present a filter bowl, adjustable regulator/knob and gauge-like detail, and lubricator-style module with guards/support rings. It reads as a generic three-piece air-preparation unit.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: Modular F.R.L./pressure control equipment](https://www.smcusa.com/products/secondary-battery/modular-f-r-l-pressure-control-equipment~167541) | oem-product-page | 2026-09-22 |

Modeled family features:
- three serial modules
- filter-bowl treatment
- regulator knob
- gauge-like detail
- lubricator-style module
- support rings

Intentionally generic / not claimed:
- No SMC mark, port size, bowl material, filtration grade, pressure setting, gauge accuracy, lubricant, flow, drain, mounting, or service specification is reproduced.
- The model does not prepare, regulate, or release compressed air.
