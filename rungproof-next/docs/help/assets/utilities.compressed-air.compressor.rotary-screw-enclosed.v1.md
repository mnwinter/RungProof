# Enclosed Rotary-Screw Air Compressor help

Asset ID: `utilities.compressed-air.compressor.rotary-screw-enclosed.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/compressed-air/compressors`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 1.3 m
- Depth: 1.7 m
- Source: `res://assets/utilities/rotary_screw_air_compressor/source/rotary_screw_air_compressor.blend`
- Delivery: `res://assets/utilities/rotary_screw_air_compressor/delivery/rotary_screw_air_compressor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `pressure_setpoint` | `float32` | `input` | bar | Pressure setpoint. |
| `running` | `bool` | `output` |  | Running. |
| `discharge_pressure` | `float32` | `output` | bar | Discharge pressure. |
| `compressor_fault` | `bool` | `output` |  | Compressor fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **packaged enclosed rotary-screw air compressor**.
Source-model review: **compared-pass** — Fresh blind render compared with Kaeser packaged rotary-screw compressors: a sound-enclosed rectangular cabinet has a removable service door, intake vents, controller/display face, emergency-stop treatment, base isolation, and a distinct air outlet. It reads as a generic enclosed rotary-screw compressor package.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Kaeser Compressors: 3 to 30 hp rotary screw compressors](https://us.kaeser.com/products-and-solutions/rotary-screw-compressors/belt-drive/) | oem-product-page | 2026-09-22 |

Modeled family features:
- sound enclosure
- service door
- intake vents
- controller/display
- emergency-stop treatment
- isolation base
- air outlet

Intentionally generic / not claimed:
- No Kaeser mark, airend, motor, pressure, flow, oil system, dryer option, electrical rating, safety system, or service interval is reproduced.
- The visual does not compress air, generate pressure, or control a physical machine.
