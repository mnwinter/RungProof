# Emergency-Stop Pedestal Station help

Asset ID: `controls.operator-station.emergency-stop.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/operator-stations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.5 m
- Height: 1.1 m
- Depth: 0.4 m
- Source: `res://assets/scene_core/emergency_stop_station/source/emergency_stop_station.blend`
- Delivery: `res://assets/scene_core/emergency_stop_station/delivery/emergency_stop_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `estop_ok` | `bool` | `output` |  | Estop ok. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `estop_travel` | linear | `KIN_estop` | 0 to 0.018 m | 0.2 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **enclosed emergency-stop control station with mushroom operator**.
Source-model review: **compared-pass** — Fresh multi-view render reviewed against the Schneider Harmony XALK station family: the anchored pedestal enclosure has a front-mounted red mushroom operator on a yellow collar, turn-to-release cue, retained panel, bottom-entry gland, and enclosure/base fasteners. The generic label and geometry are visual context only, not a safety-function, stop-category, wiring, or reset claim.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: XALK178EH7 Harmony emergency-stop control station](https://www.se.com/us/en/product/XALK178EH7/control-station-harmony-plastic-yellow-1-red-mushroom-head-push-button-40mm-emergency-stop-turn-to-release-1no-%2B-1nc-unmarked-ul-csa-certified/) | oem-product-page | 2026-09-22 |

Modeled family features:
- mushroom operating head
- yellow collar
- front panel
- enclosure
- pedestal base
- cable-entry context

Intentionally generic / not claimed:
- No Schneider mark, part number, contact arrangement, rating, stop category, safety function, wiring, reset sequence, or enclosure rating is reproduced.
- The station remains symbolic-only simulator I/O and does not provide personnel protection or control physical equipment.
