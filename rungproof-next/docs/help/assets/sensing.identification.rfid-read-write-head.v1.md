# Industrial RFID Read/Write Head help

Asset ID: `sensing.identification.rfid-read-write-head.v1`  
Catalog status: **production / approved**  
Category: `sensing/identification`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.1 m
- Height: 0.8 m
- Depth: 0.65 m
- Source: `res://assets/controls_sensors/rfid_read_write_head/source/rfid_read_write_head.blend`
- Delivery: `res://assets/controls_sensors/rfid_read_write_head/delivery/rfid_read_write_head.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `read_trigger` | `bool` | `input` |  | Read trigger. |
| `tag_present` | `bool` | `output` |  | Tag present. |
| `tag_code` | `int32` | `output` |  | Tag code. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact square HF RFID read/write head**.
Source-model review: **compared-pass** — Rebuilt review shows a compact square scan face, front status indicators, mounting plate, and rear M12-style interface. The adjacent tagged tote is installation context only and does not imply a read distance or protocol.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm electronic: ANT513 RFID read/write head data sheet](https://media.ifm.com/dam/07c5d0d0-8192-45a3-89c5-aec84915d2cd/Original/ANT513-00_EN-GB.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- square face
- short rectangular body
- front LED indicators
- rear M12-style connector and mounting pattern

Intentionally generic / not claimed:
- No ifm mark, exact dimensions, operating frequency, read distance, protocol, approval, wiring, or performance is reproduced.
- The simulator does not claim RFID read/write behavior beyond declared symbolic points.
