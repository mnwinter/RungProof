# Machine-Guarding Enclosure Panel with Personnel Door help

Asset ID: `safety.enclosure.personnel-door-panel.v1`  
Catalog status: **production / approved**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.25 m
- Height: 3.15 m
- Depth: 1.15 m
- Source: `res://assets/factory_kit/personnel_door_wall_module/source/personnel_door_wall_module.blend`
- Delivery: `res://assets/factory_kit/personnel_door_wall_module/delivery/personnel_door_wall_module.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **modular industrial equipment-enclosure personnel-door panel**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Panel Built modular-enclosure family: a framed blue personnel leaf occupies a real wall opening with steel jambs/header, three exposed hinges, a latch escutcheon and handle, kick plate, threshold, panel seams, edge posts, and base/top interfaces. It is generic enclosure geometry only and makes no lock, egress, fire, access-control, or compliance claim.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Panel Built: Modular Inplant Equipment Enclosure case study](https://www.panelbuilt.com/case-studies/inplant-equipment-enclosure/) | oem-product-page | 2026-09-22 |

Modeled family features:
- modular wall-panel field
- personnel door
- hinge side
- latch/handle context
- threshold and enclosure interfaces

Intentionally generic / not claimed:
- No Panel Built mark, door rating, fire rating, hardware rating, lock, access control, egress compliance, enclosure specification, or code compliance is reproduced.
- This model is not a machine-safety claim.
