# Production Hydraulic Power Unit help

Asset ID: `utilities.hydraulic.power-unit.production-skid.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/hydraulic/power-units`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.3 m
- Height: 1.4 m
- Depth: 1.9 m
- Source: `res://assets/utilities/hydraulic_power_unit/source/hydraulic_power_unit.blend`
- Delivery: `res://assets/utilities/hydraulic_power_unit/delivery/hydraulic_power_unit.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `pump_command` | `bool` | `input` |  | Pump command. |
| `pressure_setpoint` | `float32` | `input` | bar | Pressure setpoint. |
| `system_pressure` | `float32` | `output` | bar | System pressure. |
| `low_oil_level` | `bool` | `output` |  | Low oil level. |
| `hydraulic_fault` | `bool` | `output` |  | Hydraulic fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pump_motor_rotation` | continuous | `KIN_PUMP_MOTOR` | 0 to 360 deg | 1800 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **production hydraulic power-unit skid**.
Source-model review: **compared-pass** — Fresh blind render compared with Parker hydraulic power units: a reservoir cabinet supports a motor/pump treatment, filter/reservoir hardware, gauge, control enclosure, and grouped hydraulic service ports. It reads as a generic production hydraulic power-unit skid.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Parker: Hydraulic power unit](https://www.parker.com/content/dam/Parker-com/Literature/Power-Generation-Market/PDF-Files/Hydraulic-Power-Unit.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- reservoir cabinet
- motor/pump treatment
- filter hardware
- gauge
- control enclosure
- grouped service ports

Intentionally generic / not claimed:
- No Parker mark, flow, pressure, fluid, reservoir volume, filtration, accumulator, relief settings, motor rating, hose routing, or safety function is reproduced.
- The model does not create hydraulic pressure or move a physical actuator.
