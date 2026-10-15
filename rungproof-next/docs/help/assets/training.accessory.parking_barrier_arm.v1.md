# Parking Barrier Arm help

Asset ID: `training.accessory.parking_barrier_arm.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 4.25 m
- Height: 3.15 m
- Depth: 0.45 m
- Source: `res://assets/training_accessories/parking_barrier_arm/source/parking_barrier_arm.blend`
- Delivery: `res://assets/training_accessories/parking_barrier_arm/delivery/parking_barrier_arm.glb`

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
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

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
