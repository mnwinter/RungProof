# Gravity Roller Conveyor Section help

Asset ID: `material-handling.conveyor.gravity-roller-800x2400.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/conveyors/roller`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 1.45 m
- Depth: 0.9 m
- Source: `res://assets/material_flow/gravity_roller_conveyor/source/gravity_roller_conveyor.blend`
- Delivery: `res://assets/material_flow/gravity_roller_conveyor/delivery/gravity_roller_conveyor.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **legged gravity roller conveyor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against mk RBS-P gravity-roller conveyors: two side frames carry repeated unpowered cylindrical rollers above legged supports, with an unpowered carton on the carry surface. It reads as a generic gravity roller conveyor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [mk North America: RBS-P gravity roller conveyor](https://www.mknorthamerica.com/Products/roller-conveyor/rbs-p-gravity-roller/) | oem-product-page | 2026-09-22 |

Modeled family features:
- side frames
- repeated cylindrical rollers
- leg supports
- unpowered carry surface
- carton context

Intentionally generic / not claimed:
- No mk mark, actual dimensions, roller diameter/pitch/material, load rating, slope, guard, or installation specification is reproduced.
- This is a visual material-flow asset only; it does not establish conveyed load capacity or physical equipment behavior.
