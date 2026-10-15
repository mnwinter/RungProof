# Roller-Lever Limit Switch help

Asset ID: `sensing.position.roller-lever-limit-switch.v1`  
Catalog status: **production / approved**  
Category: `sensing/position`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 1.05 m
- Depth: 0.65 m
- Source: `res://assets/controls_sensors/roller_lever_limit_switch/source/roller_lever_limit_switch.blend`
- Delivery: `res://assets/controls_sensors/roller_lever_limit_switch/delivery/roller_lever_limit_switch.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `actuated` | `bool` | `output` |  | Actuated. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **metal-bodied roller-lever industrial limit switch**.
Source-model review: **compared-pass** — The corrected review shows a compact dark metal-bodied switch on a mounting bracket, a distinct upper operating head, lever pivot, angled metal arm, roller, and rear M12 treatment. It reads as a generic roller-lever industrial limit-switch family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: XCKJ10541 roller-lever limit switch data sheet](https://iportal2.schneider-electric.com/Contents/docs/SQD-XCKJ10541.PDF) | oem-datasheet | 2026-09-22 |

Modeled family features:
- metal switch body
- rotary operating head
- spring-return lever
- roller
- mounting holes
- cable entry

Intentionally generic / not claimed:
- No Schneider Electric mark, contact arrangement, actuation angle, cable-gland size, electrical rating, environment rating, safety function, or wiring is reproduced.
- Lever movement is simulator-only geometry and does not establish a real limit or safety input.
