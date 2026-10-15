# Book-Form Servo Drive help

Asset ID: `drives.servo.book-form.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/drives/servo`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 0.5 m
- Depth: 1.3 m
- Source: `res://assets/electrical_controls/book_servo_drive/source/book_servo_drive.blend`
- Delivery: `res://assets/electrical_controls/book_servo_drive/delivery/book_servo_drive.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `servo_enable` | `bool` | `input` |  | Servo enable. |
| `velocity_command` | `float32` | `input` | rpm | Velocity command. |
| `actual_velocity` | `float32` | `output` | rpm | Actual velocity. |
| `drive_fault` | `bool` | `output` |  | Drive fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **book-form modular servo axis amplifier**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with Siemens SINAMICS S120 Booksize modules: the upright compact axis unit has a rear extruded heat-sink envelope, upper DC-bus plug treatment, axis/status panel, removable motor/control connectors, encoder detail, and motion-network ports. It reads as a generic book-form servo axis amplifier.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Siemens: SINAMICS S120 Booksize servo drive system](https://www.siemens.com/en-gb/products/sinamics/s120-booksize/) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact upright axis body
- rear heat-sink envelope
- upper DC-bus plug treatment
- status/display panel
- motor connector
- control I/O connector
- encoder and motion-port treatment

Intentionally generic / not claimed:
- No Siemens mark, frame width, voltage, bus rating, motor rating, feedback protocol, firmware, safety function, connector pinout, parameterization, or certification is reproduced.
- The simulator does not generate motion torque, establish a DC bus, or claim an implemented safety function.
