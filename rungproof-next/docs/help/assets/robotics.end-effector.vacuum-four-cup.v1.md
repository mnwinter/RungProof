# Four-Cup Vacuum Carton Gripper help

Asset ID: `robotics.end-effector.vacuum-four-cup.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/vacuum`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.05 m
- Depth: 1.25 m
- Source: `res://assets/mechanical_motion/vacuum_multi_cup_gripper/source/vacuum_multi_cup_gripper.blend`
- Delivery: `res://assets/mechanical_motion/vacuum_multi_cup_gripper/delivery/vacuum_multi_cup_gripper.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `vacuum_command` | `bool` | `input` |  | Vacuum command. |
| `vacuum_ok` | `bool` | `output` |  | Vacuum ok. |
| `part_present` | `bool` | `output` |  | Part present. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **modular multi-cup industrial vacuum gripper**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Schmalz VacuMaster modular multiple-suction-cup family: a central manifold supports two cross rails, four compliant-looking cups, hose connections, and a tooling plate. It reads as a generic four-cup vacuum end effector.

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
