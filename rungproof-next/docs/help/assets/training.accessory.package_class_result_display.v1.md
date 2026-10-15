# Package-Class Result Display help

Asset ID: `training.accessory.package_class_result_display.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.25 m
- Height: 1.04 m
- Depth: 1.65 m
- Source: `res://assets/training_accessories/package_class_result_display/source/package_class_result_display.blend`
- Delivery: `res://assets/training_accessories/package_class_result_display/delivery/package_class_result_display.glb`

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

Generic reference family: **compact rectangular through-beam photoelectric sender and receiver**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SICK: W12 photoelectric sensor product overview](https://www.sick.com/media/productoverview/5/65/765/productoverview_W12_g555765_en.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- compact rectangular housings
- emitter/receiver optical windows
- neutral status indicators
- connector treatment
- machine bracket context

Intentionally generic / not claimed:
- No SICK mark, range, light source, output, supply voltage, IP rating, alignment requirement, or mounting dimension is reproduced.
- Any beam is visual-only simulator context, not optical emission, a safety boundary, or a live detection claim.
