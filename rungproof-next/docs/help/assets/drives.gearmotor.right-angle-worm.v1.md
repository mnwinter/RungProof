# Right-Angle Worm Gearmotor with Hollow Output help

Asset ID: `drives.gearmotor.right-angle-worm.v1`  
Catalog status: **candidate / candidate**  
Category: `drives/gearmotors/worm`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.25 m
- Height: 1.28 m
- Depth: 1.02 m
- Source: `res://assets/mechanical_motion/right_angle_worm_gearmotor/source/right_angle_worm_gearmotor.blend`
- Delivery: `res://assets/mechanical_motion/right_angle_worm_gearmotor/delivery/right_angle_worm_gearmotor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint_rpm` | `float32` | `input` | rpm | Speed setpoint rpm. |
| `actual_speed_rpm` | `float32` | `output` | rpm | Actual speed rpm. |
| `faulted` | `bool` | `output` |  | Faulted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `output_rotation` | angular_continuous | `KIN_OUTPUT_HUB` | -250 to 250 rpm | 500 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **right-angle helical-worm industrial gearmotor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SEW-EURODRIVE S-series helical-worm gearmotor family: the input motor joins an orthogonal reduction housing with a flanged output/hub, output shaft/key context, terminal housing, and base mounting. It reads as a generic right-angle worm gearmotor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SEW-EURODRIVE: S..DR.. helical-worm gearmotors](https://www.seweurodrive.com/products/gearmotors/standard-gearmotors/helical-worm-gearmotors-sdr/helical-worm-gearmotors-sdr.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- input motor
- orthogonal reduction housing
- flanged output/hub
- output shaft/key context
- terminal housing
- base mount

Intentionally generic / not claimed:
- No SEW-EURODRIVE mark, ratio, torque, motor power, output speed, hollow-shaft size, lubricant, brake, or electrical rating is reproduced.
- The geometry is a simulator representation only, not a machine-design or physical-drive claim.
