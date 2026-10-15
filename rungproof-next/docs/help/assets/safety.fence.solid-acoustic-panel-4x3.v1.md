# Solid Machine-Guarding / Acoustic Barrier Panel help

Asset ID: `safety.fence.solid-acoustic-panel-4x3.v1`  
Catalog status: **candidate / candidate**  
Category: `safety/guarding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.25 m
- Height: 3.15 m
- Depth: 0.45 m
- Source: `res://assets/factory_kit/insulated_wall_panel/source/insulated_wall_panel.blend`
- Delivery: `res://assets/factory_kit/insulated_wall_panel/delivery/insulated_wall_panel.glb`

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

Generic reference family: **modular solid industrial acoustic barrier panel**.
Source-model review: **compared-pass** — Current source and review render show a continuous solid panel between two anchored posts, with capped post tops, base plates, and a top trim. Compared with modular industrial acoustic-barrier construction, it reads as a generic solid acoustic-barrier/guard-panel family; the earlier mesh finding was stale evidence for a different factory-kit asset.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Noise Barriers: QuietLine SL modular acoustic barrier walls](https://www.noisebarriers.com/barrier-walls/sl-slr) | oem-product-page | 2026-09-22 |

Modeled family features:
- continuous solid panel skin
- two vertical support posts
- top trim
- post caps
- anchored base plates

Intentionally generic / not claimed:
- No Noise Barriers mark, panel thickness, fill material, perforation, acoustic attenuation, structural rating, fire rating, or installation method is reproduced.
- The model is a visual partition only; it does not establish a noise-control, machine-guarding, or personnel-protection performance claim.
