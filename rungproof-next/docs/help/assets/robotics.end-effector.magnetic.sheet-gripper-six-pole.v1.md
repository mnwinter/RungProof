# Six-Pole Electromagnetic Sheet Gripper help

Asset ID: `robotics.end-effector.magnetic.sheet-gripper-six-pole.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/magnetic`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.8 m
- Height: 1.5 m
- Depth: 1.5 m
- Source: `res://assets/robotics/electromagnetic_sheet_gripper/source/electromagnetic_sheet_gripper.blend`
- Delivery: `res://assets/robotics/electromagnetic_sheet_gripper/delivery/electromagnetic_sheet_gripper.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |
| `magnet_command` | `bool` | `input` |  | Magnet command. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **multi-pole magnetic sheet gripper**.
Source-model review: **compared-pass** — Rendered review compared with Schmalz magnetic grippers for robotic sheet handling: a robot-mounted backplate carries six individually modeled round pole faces, cable treatment, and steel-sheet context. It reads as a generic multi-pole magnetic sheet gripper.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [J. Schmalz: Magnetic gripper handling set SGM-SV](https://www.schmalz.com/en-gb/solutions/media-center/magnetic-gripper-handling-set-sgm-sv-for-handling-ferromagnetic-workpieces-with-lightweight-robots-and-cobots) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot mount
- backplate
- six round pole faces
- power-cable treatment
- steel-sheet context

Intentionally generic / not claimed:
- No Schmalz mark, holding force, sheet thickness, material compatibility, power/air supply, residual magnetism, safety factor, or safety function is reproduced.
- The visual does not energize a magnet or hold a real load.
