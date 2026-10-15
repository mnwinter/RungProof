# M18 Inductive Proximity Sensor help

Asset ID: `sensing.proximity.inductive-m18.v1`  
Catalog status: **production / approved**  
Category: `sensing/proximity`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.55 m
- Height: 0.65 m
- Depth: 0.75 m
- Source: `res://assets/controls_sensors/inductive_proximity_sensor_m18/source/inductive_proximity_sensor_m18.blend`
- Delivery: `res://assets/controls_sensors/inductive_proximity_sensor_m18/delivery/inductive_proximity_sensor_m18.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `detected` | `bool` | `output` |  | Detected. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **threaded M18 inductive proximity sensor**.
Source-model review: **compared-pass** — The corrected review shows a proportioned threaded M18 barrel on an open bracket, exposed locknut stack and thread crests, a dark recessed sensing face, a small neutral status window, rear M12/cable treatment, and a separate metal target. It reads as a generic installed M18 inductive sensor family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm: IG5527 inductive sensor](https://www.ifm.com/us/en/product/IG5527?tab=details) | oem-product-page | 2026-09-22 |

Modeled family features:
- M18 threaded barrel
- sensing face
- wrench flats
- two lock nuts
- neutral status LED context
- rear cable or M12 connector
- mounting bracket

Intentionally generic / not claimed:
- No ifm mark, sensing distance, output type, supply voltage, IP rating, mounting orientation, electrical connection, or installation torque is reproduced.
- This asset must remain a symbolic simulator input and does not measure physical target distance or write physical I/O.
