# Inline Helical Gearmotor help

Asset ID: `drives.gearmotor.inline-helical.v1`  
Catalog status: **candidate / candidate**  
Category: `drives/gearmotors/helical`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.45 m
- Height: 0.76 m
- Depth: 0.92 m
- Source: `res://assets/mechanical_motion/inline_helical_gearmotor/source/inline_helical_gearmotor.blend`
- Delivery: `res://assets/mechanical_motion/inline_helical_gearmotor/delivery/inline_helical_gearmotor.glb`

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
| `output_rotation` | angular_continuous | `KIN_OUTPUT_SHAFT` | -500 to 500 rpm | 800 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **inline helical industrial gearmotor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SEW-EURODRIVE R-series inline helical gearmotor family: an inline motor and reducer body, output flange and shaft, mounting feet, and terminal housing read as a generic inline industrial gearmotor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SEW-EURODRIVE: R..DR.. helical gearmotors](https://www.seweurodrive.com/products/gearmotors/standard-gearmotors/helical-gearmotors-rdr/helical-gearmotors-rdr.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- inline motor
- reducer housing
- output flange
- output shaft
- mounting feet
- terminal housing

Intentionally generic / not claimed:
- No SEW-EURODRIVE mark, ratio, torque, motor power, output speed, mounting position, lubricant, brake, or electrical rating is reproduced.
- No actual torque train or physical drive command is implied.
