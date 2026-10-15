# Industrial Workbench - 2400 mm help

Asset ID: `facility.workbench.2400mm.v1`  
Catalog status: **production / approved**  
Category: `facility/workstations`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.5 m
- Height: 1.45 m
- Depth: 1.2 m
- Source: `res://assets/factory_kit/industrial_workbench/source/industrial_workbench.blend`
- Delivery: `res://assets/factory_kit/industrial_workbench/delivery/industrial_workbench.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **steel-frame industrial workbench with worktop and lower shelf**.
Source-model review: **compared-pass** — Rendered review shows a long industrial work surface on a rigid steel leg frame with an integrated lower shelf and a machine vise as use context. It reads as a generic industrial workbench family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [LISTA: Workbenches for workshops and industry](https://www.lista.com/en/products/workbenches/workbenches.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- long worktop
- steel legs
- lower shelf
- rigid frame
- vise use context

Intentionally generic / not claimed:
- No LISTA mark, worktop composition, load capacity, height adjustment, drawer configuration, grounding, ESD property, or workstation rating is reproduced.
- The vise is a separate visual-context asset.
