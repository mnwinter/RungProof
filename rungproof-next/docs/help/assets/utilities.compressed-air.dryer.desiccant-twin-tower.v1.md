# Twin-Tower Desiccant Air Dryer help

Asset ID: `utilities.compressed-air.dryer.desiccant-twin-tower.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/compressed-air/dryers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 1.1 m
- Depth: 2.5 m
- Source: `res://assets/utilities/twin_tower_desiccant_dryer/source/twin_tower_desiccant_dryer.blend`
- Delivery: `res://assets/utilities/twin_tower_desiccant_dryer/delivery/twin_tower_desiccant_dryer.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `active_tower` | `int32` | `output` |  | Active tower. |
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

Generic reference family: **twin-tower regenerative desiccant compressed-air dryer**.
Source-model review: **compared-pass** — Fresh blind render compared with Kaeser twin-tower regenerating dryers: two tall desiccant vessels are linked by top and lower manifolds with valve treatments and a central control module. It reads as a generic twin-tower desiccant dryer.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Kaeser Compressors: Regenerative desiccant dryers KAD/KED/KBD](https://us.kaeser.com/download.ashx?id=tcm%3A46-181651) | oem-datasheet | 2026-09-22 |

Modeled family features:
- two vertical vessels
- top manifold
- lower manifold
- switching-valve treatment
- central controller
- base support

Intentionally generic / not claimed:
- No Kaeser mark, desiccant media, dew point, purge rate, flow, pressure, valve timing, heater/blower option, filtration, or certification is reproduced.
- The model does not dry air, regenerate media, or make a compressed-air quality claim.
