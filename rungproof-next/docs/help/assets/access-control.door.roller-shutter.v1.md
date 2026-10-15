# Motorized Industrial Roller Shutter help

Asset ID: `access-control.door.roller-shutter.v1`  
Catalog status: **production / approved**  
Category: `access-control/doors`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 5.3 m
- Height: 4.2 m
- Depth: 0.8 m
- Source: `res://assets/scene_core/motorized_roller_shutter/source/motorized_roller_shutter.blend`
- Delivery: `res://assets/scene_core/motorized_roller_shutter/delivery/motorized_roller_shutter.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `open_command` | `bool` | `input` |  | Open command. |
| `close_command` | `bool` | `input` |  | Close command. |
| `open_limit` | `bool` | `output` |  | Open limit. |
| `closed_limit` | `bool` | `output` |  | Closed limit. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `door_position` | linear | `KIN_bottom_bar` | 0 to 3.2 m | 1.2 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **industrial coiling steel service door**.
Source-model review: **compared-pass** — Rendered review shows a coiling curtain with repeated interlocking slats, vertical guide tracks, a header coil/shaft enclosure, bottom bar, and side operator context. It reads as a generic industrial rolling steel service-door family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Clopay: Roll-Up Sheet Doors product page](https://www.clopaydoor.com/coiling-sheet-doors) | oem-product-page | 2026-09-22 |

Modeled family features:
- interlocking horizontal curtain slats
- vertical guide tracks
- header coil/shaft zone
- bottom bar
- side-mounted operator context

Intentionally generic / not claimed:
- No Clopay mark, slat gauge, curtain material, door size, drive voltage, cycle count, weather sealing, or safety edge is reproduced.
- Simulator travel is not a real door safety or commissioning claim.
