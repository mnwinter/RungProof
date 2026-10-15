# Adjustable Robotic Pallet-Fork End Effector help

Asset ID: `robotics.end-effector.fork.adjustable-pallet.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/forks`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.7 m
- Depth: 1.6 m
- Source: `res://assets/robotics/robotic_pallet_fork/source/robotic_pallet_fork.blend`
- Delivery: `res://assets/robotics/robotic_pallet_fork/delivery/robotic_pallet_fork.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `left_fork_position_command` | `float32` | `input` |  | Left fork position command. |
| `right_fork_position_command` | `float32` | `input` |  | Right fork position command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `left_fork_position` | linear | `KIN_FORK_LEFT` | 0 to 0.35 m | 0.25 |
| `right_fork_position` | linear | `KIN_FORK_RIGHT` | 0 to 0.35 m | 0.25 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **adjustable robot pallet-fork end effector**.
Source-model review: **compared-pass** — Rendered review shows a robot-mounted fork end effector with a rigid backplate, two adjustable forks, heel members, and central spacing-actuator treatment. It reads as a generic adjustable pallet-fork end effector.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [J. Schmalz: Palletizer end-of-arm tooling overview](https://www.schmalz.com/en/support/know-how/glossary/palletizer) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot mount
- backplate
- two forks
- heel members
- spacing-actuator treatment

Intentionally generic / not claimed:
- No Schmalz mark, load capacity, fork spacing, drive type, pallet interface, load retention, robot interface, guarding, or safety function is reproduced.
- The visual does not lift, support, or transport a pallet.
