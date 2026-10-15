# 14-Way DIN-Rail Terminal Strip help

Asset ID: `electrical.terminal-strip.din-rail-14way.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/panel/terminals`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.7 m
- Height: 0.5 m
- Depth: 0.65 m
- Source: `res://assets/electrical_controls/din_terminal_strip/source/din_terminal_strip.blend`
- Delivery: `res://assets/electrical_controls/din_terminal_strip/delivery/din_terminal_strip.glb`

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

Generic reference family: **14-way DIN-rail feed-through terminal-block assembly**.
Source-model review: **compared-pass** — Fresh blind render compared with WAGO TOPJOB S rail-mount terminal blocks: fourteen repeated rail-mounted blocks, distinct end stops, screw/test-point treatment, and neutral/potential color variation read as a generic 14-way DIN terminal strip.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WAGO: TOPJOB S rail-mount terminal blocks](https://www.wago.com/global/products/electrical-interconnections/discover-rail-mount-terminal-blocks/topjob-s) | oem-product-page | 2026-09-22 |

Modeled family features:
- 14 repeated blocks
- DIN rail
- end stops
- screw/test-point treatment
- potential color variation

Intentionally generic / not claimed:
- No WAGO mark, terminal series, conductor range, voltage/current rating, jumper assignment, terminal numbering, wiring, or certification is reproduced.
- The visual does not terminate conductors or create electrical continuity.
