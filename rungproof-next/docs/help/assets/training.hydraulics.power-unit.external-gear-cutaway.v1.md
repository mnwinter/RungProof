# External-Gear Hydraulic Power Unit Trainer help

Asset ID: `training.hydraulics.power-unit.external-gear-cutaway.v1`  
Catalog status: **candidate / candidate**  
Category: `training/hydraulics/power-units`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.8 m
- Height: 2.2 m
- Depth: 1.65 m
- Source: `res://assets/process_fluid/external_gear_pump/source/external_gear_pump.blend`
- Delivery: `res://assets/process_fluid/external_gear_pump/delivery/external_gear_pump.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint_rpm` | `float32` | `input` | rpm | Speed setpoint rpm. |
| `running` | `bool` | `output` |  | Running. |
| `system_pressure` | `float32` | `output` | bar | System pressure. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `shaft_speed` | angular_continuous | `KIN_PUMP_SHAFT` | -3000 to 3000 rpm | 1500 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **fixed-displacement external gear hydraulic pump**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Parker fixed-displacement external gear-pump form: a square gear housing and shaft-end flange sit between a motor/coupling treatment and a cylindrical hydraulic outlet/port volume, with visible service-port lines. It reads as a generic external gear pump assembly.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Parker: D Series aluminum fixed-displacement gear pump](https://ph.parker.com/us/en/d11bs2a) | oem-product-page | 2026-09-22 |

Modeled family features:
- square pump housing
- shaft-end flange
- motor/coupling treatment
- hydraulic port volume
- service-port lines

Intentionally generic / not claimed:
- No Parker mark, displacement, pressure, speed, shaft, flange, port thread, fluid, motor, reservoir, relief valve, or schematic function is reproduced.
- This training visual does not create hydraulic pressure or represent a functioning power unit.
