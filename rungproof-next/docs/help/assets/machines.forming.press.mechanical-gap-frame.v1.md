# Mechanical Gap-Frame Press help

Asset ID: `machines.forming.press.mechanical-gap-frame.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/forming/presses`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.5 m
- Depth: 1.4 m
- Source: `res://assets/production-machines/mechanical_gap_frame_press/source/mechanical_gap_frame_press.blend`
- Delivery: `res://assets/production-machines/mechanical_gap_frame_press/delivery/mechanical_gap_frame_press.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `motor_run` | `bool` | `input` |  | Motor run. |
| `single_stroke` | `bool` | `input` |  | Single stroke. |
| `top_of_stroke` | `bool` | `output` |  | Top of stroke. |
| `bottom_of_stroke` | `bool` | `output` |  | Bottom of stroke. |
| `press_fault` | `bool` | `output` |  | Press fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `flywheel_rotation` | continuous | `KIN_FLYWHEEL` | 0 to 360 deg | 420 |
| `slide_position` | linear | `KIN_SLIDE` | 0 to 0.35 m | 0.25 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **mechanical gap-frame stamping press**.
Source-model review: **compared-pass** — Fresh blind render compared with Komatsu mechanical gap-frame presses: a C-frame press shows an upper drive housing, flywheel/crank treatment, slide, bolster, guarded work zone, and pendant-control treatment. It reads as a generic mechanical gap-frame press.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Komatsu: Mechanical gap frame presses](https://www.komatsupress.com/products/mechanical-press/) | oem-product-page | 2026-09-22 |

Modeled family features:
- gap frame
- drive housing
- flywheel/crank treatment
- slide
- bolster
- guarding/control treatment

Intentionally generic / not claimed:
- No Komatsu mark, tonnage, clutch/brake, stroke, die, guarding certification, controls, or production rate is reproduced.
- The visual does not cycle a press or validate machine safety.
