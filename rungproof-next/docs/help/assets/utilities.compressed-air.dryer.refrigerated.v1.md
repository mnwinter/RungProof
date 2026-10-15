# Refrigerated Compressed-Air Dryer help

Asset ID: `utilities.compressed-air.dryer.refrigerated.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/compressed-air/dryers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.5 m
- Height: 1.0 m
- Depth: 1.6 m
- Source: `res://assets/utilities/refrigerated_air_dryer/source/refrigerated_air_dryer.blend`
- Delivery: `res://assets/utilities/refrigerated_air_dryer/delivery/refrigerated_air_dryer.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `dew_point` | `float32` | `output` | degC | Dew point. |
| `dryer_fault` | `bool` | `output` |  | Dryer fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **packaged refrigerated compressed-air dryer**.
Source-model review: **compared-pass** — Fresh blind render compared with packaged refrigerated compressed-air dryers: a cabinet has a control face, condenser fan/grille, exposed lower compressor and refrigerant-line service bay, inlet/outlet ports, and automatic-drain treatment. It reads as a generic refrigerated air dryer.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Kaeser Compressors: 20 HP rotary screw compressors with refrigerated-dryer package details](https://pr.kaeser.com/en/products-and-solutions/rotary-screw-compressors/20-hp.aspx) | oem-product-page | 2026-09-22 |

Modeled family features:
- dryer cabinet
- controller face
- condenser fan/grille
- refrigerant compressor
- refrigerant lines
- air ports
- automatic drain

Intentionally generic / not claimed:
- No dryer make, refrigerant, dew point, flow, pressure drop, electrical supply, drain capacity, heat-exchanger design, or performance certification is reproduced.
- The visual does not cool air, drain condensate, or provide a process-air quality claim.
