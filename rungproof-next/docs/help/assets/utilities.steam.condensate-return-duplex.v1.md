# Duplex Steam Condensate Return Unit help

Asset ID: `utilities.steam.condensate-return-duplex.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/steam/condensate`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.6 m
- Height: 1.4 m
- Depth: 2.0 m
- Source: `res://assets/utilities/condensate_return_unit/source/condensate_return_unit.blend`
- Delivery: `res://assets/utilities/condensate_return_unit/delivery/condensate_return_unit.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `auto_enable` | `bool` | `input` |  | Auto enable. |
| `receiver_level` | `float32` | `output` | percent | Receiver level. |
| `pump_a_running` | `bool` | `output` |  | Pump a running. |
| `pump_b_running` | `bool` | `output` |  | Pump b running. |
| `high_level_alarm` | `bool` | `output` |  | High level alarm. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pump_a_rotation` | continuous | `KIN_PUMP_0.62` | 0 to 360 deg | 1800 |
| `pump_b_rotation` | continuous | `KIN_PUMP_0.98` | 0 to 360 deg | 1800 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **duplex condensate-return receiver and pump package**.
Source-model review: **compared-pass** — Fresh blind render compared with Skidmore duplex condensate-return units: a rectangular receiver carries vent and process-return nozzles, side control enclosure, and a visible pump/motor discharge assembly. It reads as a generic duplex condensate-return package.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Skidmore: V Series condensate return](https://skidmorepump.com/products/v-series-condensate-return/) | oem-product-page | 2026-09-22 |

Modeled family features:
- receiver tank
- vent
- process-return nozzle
- control enclosure
- pump/motor treatment
- discharge piping

Intentionally generic / not claimed:
- No Skidmore mark, receiver capacity, flow, discharge pressure, motor rating, pump metallurgy, controls, float setpoint, steam conditions, or boiler-feed design is reproduced.
- The model does not receive, pump, or return condensate.
