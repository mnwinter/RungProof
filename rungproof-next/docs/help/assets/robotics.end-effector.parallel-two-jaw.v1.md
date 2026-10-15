# Industrial Parallel Two-Jaw Gripper help

Asset ID: `robotics.end-effector.parallel-two-jaw.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/grippers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.1 m
- Height: 1.15 m
- Depth: 1.1 m
- Source: `res://assets/mechanical_motion/parallel_two_jaw_gripper/source/parallel_two_jaw_gripper.blend`
- Delivery: `res://assets/mechanical_motion/parallel_two_jaw_gripper/delivery/parallel_two_jaw_gripper.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `close_command` | `bool` | `input` |  | Close command. |
| `open_command` | `bool` | `input` |  | Open command. |
| `grip_width_m` | `float32` | `output` | m | Grip width m. |
| `part_present` | `bool` | `output` |  | Part present. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `jaw_left` | linear | `KIN_JAW_SLIDE_-0.23` | 0 to 0.12 m | 0.5 |
| `jaw_right` | linear | `KIN_JAW_SLIDE_0.23` | 0 to 0.12 m | 0.5 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pneumatic two-finger parallel gripper**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SCHUNK PGN-plus-P family: a compact actuator body carries a pair of opposing parallel jaws with extended fingers, direct pneumatic-line context, and a workpiece witness. It reads as a generic parallel two-jaw industrial gripper.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SCHUNK: PGN-plus-P parallel gripper](https://schunk.com/us/en/gripping-systems/parallel-gripper/pgn-plus-p/pgn-plus-p-50-2-p/p/000000000000318479) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact actuator body
- opposing parallel jaws
- finger extensions
- pneumatic-line context
- workpiece witness

Intentionally generic / not claimed:
- No SCHUNK mark, gripping force, stroke, air pressure, finger tooling, workpiece suitability, sensor, or mounting specification is reproduced.
- Opening and closing are symbolic simulator kinematics only; the model cannot grip or command physical equipment.
