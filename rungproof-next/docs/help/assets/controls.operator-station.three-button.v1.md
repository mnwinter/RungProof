# Three-Button Start Reset Stop Station help

Asset ID: `controls.operator-station.three-button.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/operator-stations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.6 m
- Height: 1.0 m
- Depth: 0.55 m
- Source: `res://assets/controls_sensors/three_button_control_station/source/three_button_control_station.blend`
- Delivery: `res://assets/controls_sensors/three_button_control_station/delivery/three_button_control_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `start` | `bool` | `input` |  | Start. |
| `reset` | `bool` | `input` |  | Reset. |
| `stop` | `bool` | `input` |  | Stop. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-button industrial control station**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Schneider Harmony three-operator station family: a compact yellow enclosure presents three vertically arranged 22 mm-scale green, amber, and red operators on a retained dark front plate. It reads as a generic start/reset/stop station; behavior and labels are intentionally not copied from an OEM assembly.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: 9001BG303 three-push-button control station](https://www.se.com/us/en/product/9001BG303/push-button-control-station%2C-3-momentary-push-buttons%2C-open-close-stop%2C-600-vac%2C-5-a%2C-nema-1/) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact enclosure
- retained front plate
- three circular operators
- distinct operator colors
- bottom-entry context

Intentionally generic / not claimed:
- No Schneider mark, legend, contact set, voltage, enclosure rating, wiring, reset logic, or control function is reproduced.
- The controls supply only symbolic simulator inputs and never command physical I/O.
