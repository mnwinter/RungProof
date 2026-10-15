# Industrial AC Servo Motor help

Asset ID: `drives.motor.ac-servo-flange.v1`  
Catalog status: **candidate / candidate**  
Category: `drives/motors/servo`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.05 m
- Height: 0.72 m
- Depth: 0.82 m
- Source: `res://assets/mechanical_motion/ac_servo_motor/source/ac_servo_motor.blend`
- Delivery: `res://assets/mechanical_motion/ac_servo_motor/delivery/ac_servo_motor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable` | `bool` | `input` |  | Enable. |
| `speed_setpoint_rpm` | `float32` | `input` | rpm | Speed setpoint rpm. |
| `actual_speed_rpm` | `float32` | `output` | rpm | Actual speed rpm. |
| `in_position` | `bool` | `output` |  | In position. |
| `faulted` | `bool` | `output` |  | Faulted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `shaft_rotation` | angular_continuous | `KIN_SERVO_SHAFT` | -3000 to 3000 rpm | 6000 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **flange-mount industrial AC servomotor**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Yaskawa Sigma-7 flange-mount servo family: the square mounting flange and pilot face, four mounting bolts, output shaft, motor body, and separate power/encoder connector treatments are visible. It reads as a generic flange-mount AC servomotor.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Yaskawa: Sigma-7 AC Servopack and Servomotor technical manual](https://www.yaskawa.com/delegate/getAttachment?cmd=documents&documentId=SIEPC23021000&documentName=siepc23021000h_7_0.pdf) | oem-manual | 2026-09-22 |

Modeled family features:
- square flange
- pilot face
- four mounting bolts
- output shaft
- motor body
- separate connector treatment

Intentionally generic / not claimed:
- No Yaskawa mark, frame size, rated torque, speed, brake, encoder type, cable, drive, or feedback specification is reproduced.
- The displayed shaft motion is simulator-only and does not energize or control a motor.
