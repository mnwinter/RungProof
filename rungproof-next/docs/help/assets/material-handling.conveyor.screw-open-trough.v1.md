# Open-Trough Screw Conveyor help

Asset ID: `material-handling.conveyor.screw-open-trough.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/bulk/screw-conveyors`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.2 m
- Height: 1.05 m
- Depth: 1.55 m
- Source: `res://assets/material_flow/open_trough_screw_conveyor/source/open_trough_screw_conveyor.blend`
- Delivery: `res://assets/material_flow/open_trough_screw_conveyor/delivery/open_trough_screw_conveyor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint_rpm` | `float32` | `input` | rpm | Speed setpoint rpm. |
| `actual_speed_rpm` | `float32` | `output` | rpm | Actual speed rpm. |
| `running` | `bool` | `output` |  | Running. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `screw_speed` | angular_continuous | `KIN_SCREW_SHAFT` | -180 to 180 rpm | 300 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **open U-trough bulk-material screw conveyor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against UGES open U-trough screw conveyor families: a long open trough exposes a continuous helical screw and center shaft between circular end plates, with an end gearmotor, legs, and an infeed carton context. It reads as a generic open-trough screw conveyor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [UGES: Screw conveyors](https://ugesl.com/products/screw-conveyors) | oem-product-page | 2026-09-22 |

Modeled family features:
- open trough
- continuous helical flight
- center shaft
- end plates
- gearmotor
- leg supports

Intentionally generic / not claimed:
- No UGES mark, trough dimensions, material, flight pitch, capacity, speed, motor, seals, cover, dust containment, guard, or maintenance specification is reproduced.
- The exposed screw is a visual asset only and does not operate or define a safe rotating-equipment condition.
