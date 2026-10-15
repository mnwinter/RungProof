# Pedestal Pushbutton and E-Stop Station help

Asset ID: `controls.operator-station.pushbutton-estop.v1`  
Catalog status: **production / approved**  
Category: `controls/operator-stations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.55 m
- Height: 1.35 m
- Depth: 0.48 m
- Source: `res://assets/scene_core/operator_pushbutton_station/source/operator_pushbutton_station.blend`
- Delivery: `res://assets/scene_core/operator_pushbutton_station/delivery/operator_pushbutton_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `pushbutton_pressed` | `bool` | `output` |  | Operator pushbutton state. |
| `estop_ok` | `bool` | `output` |  | Simulated E-stop contact state; not a real safety function. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pushbutton_travel` | linear | `KIN_pushbutton` | 0 to 0.018 m | 0.2 |
| `estop_travel` | linear | `KIN_estop` | 0 to 0.018 m | 0.2 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **22 mm emergency-stop control station on a fabricated pedestal**.
Source-model review: **compared-pass** — Rendered review shows a freestanding operator station with a conspicuous red mushroom operator, yellow mounting collar, protected face plate, compact enclosure, and anchored pedestal. It reads as a generic pedestal adaptation of a 22 mm control-station family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: Harmony XALK emergency-stop control station](https://shop.se.com/pro/us/en/product/control-station-harmony-plastic-yellow-1-red-mushroom-head-push-button-40mm-emergency-stop-turn-to-release-2-nc-unmarked-ul-csa-certified/) | oem-product-page | 2026-09-22 |

Modeled family features:
- red mushroom operator
- yellow collar
- front control plate
- enclosure
- pedestal and anchor base

Intentionally generic / not claimed:
- No Schneider mark, circuit architecture, contact arrangement, stop category, safety function, release method, enclosure rating, or certification is reproduced.
- The simulator control is not an emergency-stop safety function or a live machine claim.
