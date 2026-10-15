# Cable Dancer help

Asset ID: `training.accessory.cable_dancer.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.1 m
- Height: 0.8 m
- Depth: 1.1 m
- Source: `res://assets/training_accessories/cable_dancer/source/cable_dancer.blend`
- Delivery: `res://assets/training_accessories/cable_dancer/delivery/cable_dancer.glb`

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

Generic reference family: **straight-section industrial ladder cable tray**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

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
