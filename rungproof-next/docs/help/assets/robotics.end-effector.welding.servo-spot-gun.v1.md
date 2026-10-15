# Robotic Servo Spot-Weld Gun help

Asset ID: `robotics.end-effector.welding.servo-spot-gun.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/welding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.7 m
- Height: 2.0 m
- Depth: 1.5 m
- Source: `res://assets/robotics/servo_spot_weld_gun/source/servo_spot_weld_gun.blend`
- Delivery: `res://assets/robotics/servo_spot_weld_gun/delivery/servo_spot_weld_gun.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `electrode_position_command` | `float32` | `input` |  | Electrode position command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |
| `weld_trigger` | `bool` | `input` |  | Weld trigger. |
| `weld_complete` | `bool` | `output` |  | Weld complete. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `electrode_position` | linear | `KIN_MOVING_ELECTRODE` | 0 to 0.32 m | 0.25 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **robot-mounted servo resistance spot-weld gun**.
Source-model review: **compared-pass** — Rendered review compared with ABB GWT C9 servo spot welding guns: a robot mount carries a C-gun body, opposing copper electrode arms, servo-actuator treatment, and hose treatments. It reads as a generic servo spot-weld gun.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB Robotics: GWT C9 spot welding gun](https://www.abb.com/global/en/areas/robotics/solutions/functional-modules/spot-welding-functional-module/gwt-c9-spot-welding-gun) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot mount
- C-gun body
- opposing electrodes
- servo-actuator treatment
- hose treatments

Intentionally generic / not claimed:
- No ABB mark, force, current, transformer, cooling, stroke, weld schedule, guard design, or certification is reproduced.
- The visual does not apply force/current or make a weld or machine-safety claim.
