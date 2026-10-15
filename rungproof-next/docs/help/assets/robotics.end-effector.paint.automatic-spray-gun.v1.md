# Robotic Automatic Paint Spray Gun help

Asset ID: `robotics.end-effector.paint.automatic-spray-gun.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/painting`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.6 m
- Height: 1.3 m
- Depth: 1.3 m
- Source: `res://assets/robotics/robotic_paint_spray_gun/source/robotic_paint_spray_gun.blend`
- Delivery: `res://assets/robotics/robotic_paint_spray_gun/delivery/robotic_paint_spray_gun.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |
| `spray_command` | `bool` | `input` |  | Spray command. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **automatic industrial paint spray gun**.
Source-model review: **compared-pass** — Rendered review compared with Dürr EcoGun automatic spray guns: a robot mount supports a compact gun body, air-cap/nozzle face, material cup and separated fluid/air hose treatments. It reads as a generic automatic paint-spray gun.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Dürr: EcoGun automatic spray guns](https://www.durr.com/en/products/paint-shop-application-technology/paint-application/spray-guns) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot mount
- compact gun body
- air-cap/nozzle face
- material cup
- fluid and air hose treatments

Intentionally generic / not claimed:
- No Dürr mark, paint chemistry, pressure, nozzle size, atomization, flow, electrostatics, exhaust, or hazardous-area rating is reproduced.
- The visual does not spray coating or establish booth/fire/exposure safety.
