# Elevated Industrial Catwalk Platform - 4 m help

Asset ID: `structures.catwalk.platform-4m.v1`  
Catalog status: **production / approved**  
Category: `facility/access`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.1 m
- Height: 2.4 m
- Depth: 1.45 m
- Source: `res://assets/factory_kit/elevated_catwalk_platform/source/elevated_catwalk_platform.blend`
- Delivery: `res://assets/factory_kit/elevated_catwalk_platform/delivery/elevated_catwalk_platform.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **elevated industrial work platform with guardrails**.
Source-model review: **compared-pass** — Rendered review shows an elevated deck with continuous top/midrail guardrails, deck ribs, multiple footed legs, and cross-bracing consistent with a generic industrial work platform.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Lapeyre Stair: Industrial Work Platforms product page](https://www.lapeyrestair.com/products/work-platforms/) | oem-product-page | 2026-09-22 |

Modeled family features:
- elevated deck
- guardrail top/midrail
- support legs
- cross-bracing
- stair access interface

Intentionally generic / not claimed:
- No supplier mark, deck rating, railing code compliance, bracing calculation, anchorage, finish, or safety claim is reproduced.
- The model is visual facility geometry only.
