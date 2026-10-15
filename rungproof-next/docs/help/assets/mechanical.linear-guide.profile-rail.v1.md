# Profile Rail Linear Guide and Carriage help

Asset ID: `mechanical.linear-guide.profile-rail.v1`  
Catalog status: **candidate / candidate**  
Category: `mechanical/linear-motion/guides`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.1 m
- Height: 0.72 m
- Depth: 0.7 m
- Source: `res://assets/mechanical_motion/profile_rail_linear_guide/source/profile_rail_linear_guide.blend`
- Delivery: `res://assets/mechanical_motion/profile_rail_linear_guide/delivery/profile_rail_linear_guide.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position_m` | `float32` | `input` | m | Position m. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `carriage_travel` | linear | `KIN_GUIDE_CARRIAGE` | -0.75 to 0.75 m | 2.0 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **profile-rail recirculating-ball linear guide**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the THK HSR LM Guide family: a rectangular precision-rail form passes through a wider recirculating-ball block, with visible rail mounting holes and top block fasteners. It reads as a generic profile-rail linear guide.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [THK: Global Standard Model HSR LM Guide](https://www.thk.com/us/en/products/lm_guide/full_ball/hsr/) | oem-product-page | 2026-09-22 |

Modeled family features:
- profile rail
- wide guide block
- rail mounting holes
- top block fasteners
- rail/block interface

Intentionally generic / not claimed:
- No THK mark, rail width, block length, preload, accuracy class, lubrication, load, travel, seal, or mounting specification is reproduced.
- The visual slider position is symbolic and not a precision-motion or physical-load claim.
