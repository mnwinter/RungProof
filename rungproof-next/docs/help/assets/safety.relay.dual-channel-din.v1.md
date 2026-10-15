# Dual-Channel DIN-Rail Safety Relay help

Asset ID: `safety.relay.dual-channel-din.v1`  
Catalog status: **candidate / candidate**  
Category: `safety/control/relays`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.45 m
- Height: 0.45 m
- Depth: 1.05 m
- Source: `res://assets/electrical_controls/safety_relay/source/safety_relay.blend`
- Delivery: `res://assets/electrical_controls/safety_relay/delivery/safety_relay.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `channel_a` | `bool` | `input` |  | Channel a. |
| `channel_b` | `bool` | `input` |  | Channel b. |
| `reset_command` | `bool` | `input` |  | Reset command. |
| `safety_output` | `bool` | `output` |  | Safety output. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **dual-channel DIN-rail safety relay**.
Source-model review: **compared-pass** — Fresh blind render compared with Pilz PNOZ X3 safety relays: a narrow DIN-mounted relay has a distinct safety-colored front, separate top/bottom terminal decks, channel/status LEDs, reset treatment, and a dual-channel face. It reads as a generic dual-channel safety relay.

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
