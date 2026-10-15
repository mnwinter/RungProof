# 10-Inch Industrial Touch HMI help

Asset ID: `controls.hmi.touch-panel-10in.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/hmi`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.2 m
- Height: 0.28 m
- Depth: 0.95 m
- Source: `res://assets/electrical_controls/industrial_hmi/source/industrial_hmi.blend`
- Delivery: `res://assets/electrical_controls/industrial_hmi/delivery/industrial_hmi.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `screen_active` | `bool` | `input` |  | Screen active. |
| `operator_touch` | `bool` | `output` |  | Operator touch. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **panel-mounted industrial touchscreen HMI**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with Siemens SIMATIC HMI Comfort Panel families: a landscape panel has a dark rear enclosure, broad protected bezel, flush wide touchscreen, corner mounting screws, and clearly separated header/process/alarm regions. It reads as a generic 10-inch industrial touchscreen HMI.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Siemens: SIMATIC HMI Panels](https://www.siemens.com/en-us/products/simatic-hmi/panels/) | oem-product-page | 2026-09-22 |

Modeled family features:
- landscape bezel
- flush widescreen touch area
- protected enclosure
- corner mounting screws
- header/process/alarm display regions

Intentionally generic / not claimed:
- No Siemens mark, panel part number, display resolution, enclosure rating, touchscreen technology, firmware, runtime, alarm configuration, network address, or operator authorization is reproduced.
- The display is a simulator visual only; it does not act as a live HMI or write any physical control point.
