# Mobile Tool Cabinet help

Asset ID: `facility.storage.mobile-tool-cabinet.v1`  
Catalog status: **production / approved**  
Category: `facility/storage`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.35 m
- Height: 1.45 m
- Depth: 0.8 m
- Source: `res://assets/factory_kit/mobile_tool_cabinet/source/mobile_tool_cabinet.blend`
- Delivery: `res://assets/factory_kit/mobile_tool_cabinet/delivery/mobile_tool_cabinet.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **mobile multi-drawer tool cabinet/workbench**.
Source-model review: **compared-pass** — Rendered review shows a low rolling tool cabinet with a durable top, repeated horizontal drawer fronts and pulls, side push handle, and visible caster arrangement. It reads as a generic mobile multi-drawer tool-storage family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Husky: 46 inch 9 Drawer Mobile Workbench use and care guide](https://images.thdstatic.com/catalog/pdfImages/f8/f8f67ba5-a66c-4e59-a493-0940451e0e73.pdf) | oem-manual | 2026-09-22 |

Modeled family features:
- flat work surface
- stacked drawers
- drawer pulls
- side handle
- casters

Intentionally generic / not claimed:
- No Husky mark, drawer count, dimensions, drawer rating, lock, caster brake, worktop material, or storage capacity is reproduced.
- The cabinet has no real load or mobility behavior claim.
