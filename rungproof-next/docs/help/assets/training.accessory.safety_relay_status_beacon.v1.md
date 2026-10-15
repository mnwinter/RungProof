# Safety Relay/Status Beacon help

Asset ID: `training.accessory.safety_relay_status_beacon.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.45 m
- Height: 0.45 m
- Depth: 1.05 m
- Source: `res://assets/training_accessories/safety_relay_status_beacon/source/safety_relay_status_beacon.blend`
- Delivery: `res://assets/training_accessories/safety_relay_status_beacon/delivery/safety_relay_status_beacon.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **dual-channel DIN-rail safety relay**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Pilz: PNOZ X3 safety relay](https://www.pilz.com/en-INT/eshop/product/774316) | oem-product-page | 2026-09-22 |

Modeled family features:
- narrow DIN relay
- safety-colored face
- top/bottom terminal decks
- channel LEDs
- reset treatment
- dual-channel label

Intentionally generic / not claimed:
- No Pilz mark, safety category, performance level, SIL, wiring diagram, reset mode, output contacts, response time, proof test, or certification is reproduced.
- This simulator visual does not implement a safety function and must never be treated as a safety device.
