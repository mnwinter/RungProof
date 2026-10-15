# Two-Fan Air-Cooled Water Chiller help

Asset ID: `utilities.cooling.chiller.air-cooled-two-fan.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/cooling/chillers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.9 m
- Height: 1.6 m
- Depth: 1.8 m
- Source: `res://assets/utilities/air_cooled_water_chiller/source/air_cooled_water_chiller.blend`
- Delivery: `res://assets/utilities/air_cooled_water_chiller/delivery/air_cooled_water_chiller.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `leaving_water_setpoint` | `float32` | `input` | degC | Leaving water setpoint. |
| `leaving_water_temperature` | `float32` | `output` | degC | Leaving water temperature. |
| `chiller_running` | `bool` | `output` |  | Chiller running. |
| `chiller_fault` | `bool` | `output` |  | Chiller fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `fan_a_rotation` | continuous | `KIN_CONDENSER_FAN_-0.72_RING` | 0 to 360 deg | 1440 |
| `fan_b_rotation` | continuous | `KIN_CONDENSER_FAN_0.72_RING` | 0 to 360 deg | 1440 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **two-fan air-cooled liquid chiller**.
Source-model review: **compared-pass** — Fresh blind render compared with Trane CGAM air-cooled chillers: a long rectangular outdoor package has two top propeller fans, side condenser-coil panels, a controller face, and separated chilled-water supply/return connections. It reads as a generic two-fan air-cooled chiller.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Trane: CGAM air-cooled scroll chiller](https://www.trane.com/commercial/north-america/us/en/products-systems/chillers/air-cooled-chillers/cgam.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- rectangular outdoor package
- two top fans
- condenser-coil panels
- controller face
- chilled-water connections

Intentionally generic / not claimed:
- No Trane mark, tonnage, refrigerant, compressor arrangement, voltage, flow, temperature, efficiency, protection, or controls are reproduced.
- The asset does not refrigerate water or run a physical fan.
