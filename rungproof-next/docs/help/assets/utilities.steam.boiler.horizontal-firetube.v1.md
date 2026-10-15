# Horizontal Firetube Steam Boiler help

Asset ID: `utilities.steam.boiler.horizontal-firetube.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/steam/boilers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.8 m
- Height: 1.8 m
- Depth: 2.8 m
- Source: `res://assets/utilities/horizontal_firetube_boiler/source/horizontal_firetube_boiler.blend`
- Delivery: `res://assets/utilities/horizontal_firetube_boiler/delivery/horizontal_firetube_boiler.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `steam_pressure_setpoint` | `float32` | `input` | bar | Steam pressure setpoint. |
| `steam_pressure` | `float32` | `output` | bar | Steam pressure. |
| `flame_proven` | `bool` | `output` |  | Flame proven. |
| `boiler_trip` | `bool` | `output` |  | Boiler trip. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **horizontal packaged firetube steam boiler**.
Source-model review: **compared-pass** — Fresh blind render compared with horizontal packaged firetube boiler form: the model has a horizontal cylindrical shell, front burner and motor treatment, stack, gauge, control cabinet, and support base. It reads as a generic horizontal firetube boiler package.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Cleaver-Brooks: ClearFire CE firetube boiler](https://info.cleaverbrooks.com/CFC-E) | oem-product-page | 2026-09-22 |

Modeled family features:
- horizontal shell
- front burner treatment
- burner motor
- stack
- gauge
- control cabinet
- base

Intentionally generic / not claimed:
- No Cleaver-Brooks mark, firing rate, fuel, pressure, temperature, safety valves, combustion controls, code stamp, emissions, or boiler rating is reproduced.
- The asset is not a fired pressure vessel and does not generate steam or combustion.
