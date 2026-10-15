# Vertical Reciprocating Conveyor help

Asset ID: `material-handling.lift.vertical-reciprocating.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/lifts`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.55 m
- Height: 1.25 m
- Depth: 2.85 m
- Source: `res://assets/material_flow/vertical_reciprocating_conveyor/source/vertical_reciprocating_conveyor.blend`
- Delivery: `res://assets/material_flow/vertical_reciprocating_conveyor/delivery/vertical_reciprocating_conveyor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `raise_command` | `bool` | `input` |  | Raise command. |
| `lower_command` | `bool` | `input` |  | Lower command. |
| `height_m` | `float32` | `output` | m | Height m. |
| `upper_limit` | `bool` | `output` |  | Upper limit. |
| `lower_limit` | `bool` | `output` |  | Lower limit. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `carriage_height` | linear | `KIN_LIFT_CARRIAGE` | 0 to 1.35 m | 0.65 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **four-post vertical reciprocating material conveyor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Autoquip four-post vertical reciprocating conveyors: a guarded load carriage with roller-deck treatment is supported between tall structural posts, with a top drive housing and visible vertical guide elements. It reads as a generic VRC material lift.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Autoquip: Freight lifts and vertical reciprocating conveyors](https://autoquip.com/products/freight-lifts-vrc/) | oem-product-page | 2026-09-22 |

Modeled family features:
- four-post structural frame
- roller-deck carriage
- handrail treatment
- top drive housing
- vertical guide elements

Intentionally generic / not claimed:
- No Autoquip mark, lifting method, capacity, travel, speed, landing gate, interlock, guarding, safety circuit, code compliance, or installation requirement is reproduced.
- Visual lift position is simulator-only and never represents personnel or freight lifting authorization.
