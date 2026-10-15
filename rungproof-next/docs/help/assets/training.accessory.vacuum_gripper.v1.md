# Vacuum Gripper help

Asset ID: `training.accessory.vacuum_gripper.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.05 m
- Depth: 1.25 m
- Source: `res://assets/training_accessories/vacuum_gripper/source/vacuum_gripper.blend`
- Delivery: `res://assets/training_accessories/vacuum_gripper/delivery/vacuum_gripper.glb`

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

Generic reference family: **modular multi-cup industrial vacuum gripper**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schmalz: VacuMaster vacuum lifter brochure](https://media.schmalz.com/MAM_Library/Dokumente/Publikation/Kataloge_Broschueren/1f39fa070e57_29.01.03.01409_Brochure_HS_Jumbo_VacuMaster_Schmalz_2024_en-EN.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- central manifold
- two cross rails
- four suction cups
- vacuum hose context
- tooling plate

Intentionally generic / not claimed:
- No Schmalz mark, vacuum level, cup size, payload, flow, material compatibility, safety factor, or handling capability is reproduced.
- The visual vacuum state is symbolic only; it does not create vacuum or control a physical lift.
