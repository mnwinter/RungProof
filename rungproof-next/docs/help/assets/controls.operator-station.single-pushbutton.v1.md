# Single Pushbutton Pedestal Station help

Asset ID: `controls.operator-station.single-pushbutton.v1`  
Catalog status: **production / approved**  
Category: `controls/operator-stations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.5 m
- Height: 1.1 m
- Depth: 0.4 m
- Source: `res://assets/scene_core/single_pushbutton_station/source/single_pushbutton_station.blend`
- Delivery: `res://assets/scene_core/single_pushbutton_station/delivery/single_pushbutton_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `pressed` | `bool` | `output` |  | Pressed. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `button_travel` | linear | `KIN_pushbutton` | 0 to 0.018 m | 0.2 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **single 22 mm pushbutton control station on a fabricated pedestal**.
Source-model review: **compared-pass** — Rendered review shows a compact one-operator enclosure with a 22 mm-style flush green pushbutton, front legend area, mounting plate, and anchored pedestal. It reads as a generic pedestal adaptation of a single-position industrial control station.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: Harmony XALD 22 mm plastic control stations](https://www.se.com/uk/en/product/XALD01H7/harmony-xald-xalk-empty-control-station-plastic-dark-grey-lid-light-grey-base-1-cutout-%C3%B822-ul-csa-certified/) | oem-product-page | 2026-09-22 |

Modeled family features:
- single circular operator
- front legend area
- compact enclosure
- pedestal
- anchored base

Intentionally generic / not claimed:
- No Schneider mark, contact type, color meaning, rating, enclosure rating, wiring, or safety function is reproduced.
- The station input remains a symbolic simulator command.
