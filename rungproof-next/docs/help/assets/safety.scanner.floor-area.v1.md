# Industrial Safety Laser Scanner help

Asset ID: `safety.scanner.floor-area.v1`  
Catalog status: **production / approved**  
Category: `safety/presence-sensing`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.2 m
- Height: 0.9 m
- Depth: 3.2 m
- Source: `res://assets/controls_sensors/safety_laser_scanner/source/safety_laser_scanner.blend`
- Delivery: `res://assets/controls_sensors/safety_laser_scanner/delivery/safety_laser_scanner.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `field_clear` | `bool` | `output` |  | Field clear. |
| `warning_field_clear` | `bool` | `output` |  | Warning field clear. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact floor-mounted safety laser scanner**.
Source-model review: **compared-pass** — Rebuilt review shows a low mounted body, distinct upper optical cover, front display/keypad, rear system interface, and mounting plate. The red rays remain clearly illustrative and do not represent a safety-field design.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SICK: microScan3 safety laser scanner operating instructions](https://www.sick.com/media/docs/7/57/757/Operating_instructions_microScan3_Safety_laser_scanner_en_IM0063757.PDF) | oem-manual | 2026-09-22 |

Modeled family features:
- low compact scanner body
- upper optical cover
- front display/status zone
- system-plug/mounting interface

Intentionally generic / not claimed:
- No SICK mark, scan angle, field geometry, range, response time, safety level, configuration, or connector pinout is reproduced.
- Displayed floor field is illustrative only and not a safety-field design.
