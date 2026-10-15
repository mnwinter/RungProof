# Regenerative Side-Channel Blower help

Asset ID: `utilities.air.blower.regenerative-side-channel.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/air/blowers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.1 m
- Height: 1.1 m
- Depth: 1.7 m
- Source: `res://assets/utilities/regenerative_blower/source/regenerative_blower.blend`
- Delivery: `res://assets/utilities/regenerative_blower/delivery/regenerative_blower.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `discharge_pressure` | `float32` | `output` | kPa | Discharge pressure. |
| `blower_fault` | `bool` | `output` |  | Blower fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `motor_rotation` | continuous | `KIN_MOTOR_SHAFT` | 0 to 360 deg | 3600 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **regenerative side-channel blower**.
Source-model review: **compared-pass** — Fresh blind render compared with FPZ side-channel blowers: a large toroidal blower casing is directly coupled to a motor on a common base and carries distinct inlet/outlet pipe treatments plus a terminal box. It reads as a generic regenerative side-channel blower.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [FPZ: Side channel blower solutions](https://www.fpz.com/brand/) | oem-product-page | 2026-09-22 |

Modeled family features:
- toroidal blower casing
- direct-coupled motor
- common base
- inlet/outlet treatment
- terminal box

Intentionally generic / not claimed:
- No FPZ mark, stage count, airflow, pressure/vacuum, motor rating, ATEX suitability, process medium, or installation orientation is reproduced.
- The model does not move air or establish a pressure/vacuum source.
