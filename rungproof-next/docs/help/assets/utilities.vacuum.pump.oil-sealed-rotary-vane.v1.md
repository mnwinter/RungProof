# Oil-Sealed Rotary-Vane Vacuum Pump help

Asset ID: `utilities.vacuum.pump.oil-sealed-rotary-vane.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/vacuum/pumps`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.0 m
- Depth: 1.3 m
- Source: `res://assets/utilities/rotary_vane_vacuum_pump/source/rotary_vane_vacuum_pump.blend`
- Delivery: `res://assets/utilities/rotary_vane_vacuum_pump/delivery/rotary_vane_vacuum_pump.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `vacuum_level` | `float32` | `output` | kPa | Vacuum level. |
| `pump_fault` | `bool` | `output` |  | Pump fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `motor_rotation` | continuous | `KIN_MOTOR_SHAFT` | 0 to 360 deg | 1800 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **oil-lubricated rotary-vane vacuum pump**.
Source-model review: **compared-pass** — Fresh blind render compared with Busch R5 oil-lubricated rotary-vane pumps: a base-mounted motor drives a rectangular oil-sealed pump body with a vertical inlet, side exhaust/gauge treatment, cooling fins, service enclosure, and coupling cover. It reads as a generic oil-sealed rotary-vane vacuum pump.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Busch Vacuum Solutions: R5 oil-lubricated rotary vane vacuum pumps](https://www.buschvacuum.com/ca/en/products/vacuum-pumps/rotary-vane/r5/) | oem-product-page | 2026-09-22 |

Modeled family features:
- motor
- coupling cover
- oil-sealed pump body
- vertical inlet
- exhaust/gauge treatment
- cooling fins
- base

Intentionally generic / not claimed:
- No Busch mark, pumping speed, ultimate pressure, oil capacity, motor rating, inlet/outlet size, gas compatibility, cooling, or certification is reproduced.
- The visual does not create vacuum or move process gas.
