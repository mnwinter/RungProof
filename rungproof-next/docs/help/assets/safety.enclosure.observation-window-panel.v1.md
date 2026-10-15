# Machine-Guarding Enclosure Panel with Observation Window help

Asset ID: `safety.enclosure.observation-window-panel.v1`  
Catalog status: **production / approved**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.25 m
- Height: 3.15 m
- Depth: 0.55 m
- Source: `res://assets/factory_kit/observation_window_wall_module/source/observation_window_wall_module.blend`
- Delivery: `res://assets/factory_kit/observation_window_wall_module/delivery/observation_window_wall_module.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **modular industrial equipment-enclosure observation-window panel**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Panel Built modular-enclosure family: segmented panel fields sit between edge posts and base/top interfaces, with a recessed glazed observation opening, steel jamb/rail retention frame, mullion, sill, and visible retainer fasteners. The glazing and structural details are generic visual representation only; no impact, fire, acoustic, or machine-safety rating is claimed.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Panel Built: Modular Inplant Equipment Enclosure case study](https://www.panelbuilt.com/case-studies/inplant-equipment-enclosure/) | oem-product-page | 2026-09-22 |

Modeled family features:
- modular panel field
- framed observation opening
- retention frame
- panel-to-panel interfaces
- floor and top connection context

Intentionally generic / not claimed:
- No Panel Built mark, panel material, glazing type, impact rating, fire rating, sound rating, enclosure specification, or code compliance is reproduced.
- This model is not a machine-safety claim.
