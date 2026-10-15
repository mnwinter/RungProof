# Robot Controller/Status Panel help

Asset ID: `training.accessory.robot_controller_status_panel.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.7 m
- Depth: 1.6 m
- Source: `res://assets/training_accessories/robot_controller_status_panel/source/robot_controller_status_panel.blend`
- Delivery: `res://assets/training_accessories/robot_controller_status_panel/delivery/robot_controller_status_panel.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

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
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

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
