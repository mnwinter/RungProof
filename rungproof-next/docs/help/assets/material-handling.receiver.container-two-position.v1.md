# Two-Position Container Receiving Fixture help

Asset ID: `material-handling.receiver.container-two-position.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/receivers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 1.8 m
- Depth: 1.8 m
- Source: `res://assets/scene_support/two_position_container_receiver/source/two_position_container_receiver.blend`
- Delivery: `res://assets/scene_support/two_position_container_receiver/delivery/two_position_container_receiver.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `receiver_run` | `bool` | `input` |  | Receiver run. |
| `left_present` | `bool` | `output` |  | Left present. |
| `right_present` | `bool` | `output` |  | Right present. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `receiver_rollers` | continuous | `KIN_receiver_roller_left` | 0 to 360 deg | 45 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **two-position roller container receiver**.
Source-model review: **compared-pass** — Rendered review compared with Interroll pallet roller-conveyor modules: two guided roller bays have front stops, dock-funnel treatments, backstop, and presence-sensor treatments. It reads as a generic two-position container receiver.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Interroll: Modular Pallet Conveyor Platform](https://www.interroll.com/products/mpp) | oem-product-page | 2026-09-22 |

Modeled family features:
- two roller bays
- guide rails
- front stops
- dock funnels
- backstop
- presence sensors

Intentionally generic / not claimed:
- No Interroll mark, load rating, roller drive, speed, controls, sensor model, guarding, or safety function is reproduced.
- The visual does not convey or safely retain a real container.
