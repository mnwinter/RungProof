# Rotary Selector Station (three-mark master) help

Asset ID: `controls.operator-station.selector.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/operator-stations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.7 m
- Height: 1.7 m
- Depth: 0.6 m
- Source: `res://assets/scene_core/rotary_selector_station/source/rotary_selector_station.blend`
- Delivery: `res://assets/scene_core/rotary_selector_station/delivery/rotary_selector_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position` | `int32` | `output` |  | Position. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `selector_position` | rotary | `KIN_selector_handle` | -55 to 55 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **22 mm maintained industrial selector switch on an operator station**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Schneider Harmony XB4 family: the revised operator is a small 22 mm-scale black maintained handle with a metal-looking bezel and three neutral detents on an enclosure door. The anchored pedestal, enclosure, door fasteners, and bottom-entry gland provide station context; positions and all signal behavior remain generic symbolic simulator I/O.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: Harmony XB4 metal selector switch product data](https://shop.se.com/pro/us/en/product/head-for-selector-switch-harmony-xb4-metal-black-22mm-long-handle-3-positions-stay-put/) | oem-product-page | 2026-09-22 |

Modeled family features:
- round 22 mm panel-mounted operator
- metal mounting bezel
- black handle with defined positions
- enclosure/pedestal mounting context

Intentionally generic / not claimed:
- No Schneider mark, exact number of positions, contact blocks, wiring, protection rating, or operating-mode logic is reproduced.
- The scene selector points remain symbolic simulator I/O only.
