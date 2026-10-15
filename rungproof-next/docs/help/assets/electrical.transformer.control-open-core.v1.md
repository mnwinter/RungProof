# Open-Core Control Transformer help

Asset ID: `electrical.transformer.control-open-core.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/power/transformers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.15 m
- Height: 0.9 m
- Depth: 1.15 m
- Source: `res://assets/electrical_controls/control_transformer/source/control_transformer.blend`
- Delivery: `res://assets/electrical_controls/control_transformer/delivery/control_transformer.glb`

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

Generic reference family: **open-core industrial control power transformer**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with Eaton Type MTE industrial control transformers: an open laminated-core structure has distinct copper primary and secondary winding blocks, top and bottom core yokes, four touch-safe-terminal treatments, and a wide base. It reads as a generic control power transformer.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Eaton: Type MTE industrial control transformer C0100E1B](https://www.eaton.com/us/en-us/skuPage.C0100E1B.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- laminated-core treatment
- separate primary/secondary winding blocks
- upper and lower yokes
- four terminal treatments
- mounting base

Intentionally generic / not claimed:
- No Eaton mark, VA rating, input/output voltage, winding configuration, taps, insulation class, terminal designation, fuse protection, temperature rise, or approval is reproduced.
- The asset does not transform, isolate, or energize electrical power.
