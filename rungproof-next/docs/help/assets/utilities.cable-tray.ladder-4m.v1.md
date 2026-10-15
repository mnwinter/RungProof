# Ladder Cable Tray - 4 m help

Asset ID: `utilities.cable-tray.ladder-4m.v1`  
Catalog status: **production / approved**  
Category: `utilities/cable-management`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.1 m
- Height: 0.8 m
- Depth: 1.1 m
- Source: `res://assets/factory_kit/ladder_cable_tray/source/ladder_cable_tray.blend`
- Delivery: `res://assets/factory_kit/ladder_cable_tray/delivery/ladder_cable_tray.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **straight-section industrial ladder cable tray**.
Source-model review: **compared-pass** — Rendered review shows an open ladder-tray run with two longitudinal side rails, regular transverse rungs, a below-tray support arrangement, and representative segregated cables. It reads as a generic industrial ladder cable-tray family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Eaton: B-Line series Imperial cable tray and ladder](https://www.eaton.com/us/en-us/catalog/support-systems/imperial-cable-tray-and-ladder.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- two side rails
- transverse rungs
- straight tray run
- support context
- representative cables

Intentionally generic / not claimed:
- No Eaton/B-Line mark, material, width, rung spacing, support spacing, cable fill, grounding, ampacity, fire performance, voltage class, installation method, or code compliance is reproduced.
- The cables are visual context and not a wiring design.
