# Continuous Bucket Elevator help

Asset ID: `material-handling.elevator.bucket-continuous.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/bulk/elevators`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 1.45 m
- Depth: 2.95 m
- Source: `res://assets/material_flow/bucket_elevator/source/bucket_elevator.blend`
- Delivery: `res://assets/material_flow/bucket_elevator/delivery/bucket_elevator.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint` | `float32` | `input` | m/s | Speed setpoint. |
| `actual_speed` | `float32` | `output` | m/s | Actual speed. |
| `running` | `bool` | `output` |  | Running. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `bucket_speed` | linear_continuous | `KIN_BUCKET_BELT` | 0 to 1.5 m/s | 0.8 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **continuous bucket elevator for bulk materials**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Universal Industries continuous bucket-elevator form: a tall enclosed vertical trunk carries a visible vertical run of bucket forms to a top head/discharge housing, with base boot and drive treatment. It reads as a generic continuous bucket elevator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Universal Industries: Twin Trunk Alpha Series bucket elevators](https://universalindustries.com/bucket-elevators-alphaseries/) | oem-product-page | 2026-09-22 |

Modeled family features:
- vertical trunk
- visible bucket run
- top head/discharge housing
- base boot
- drive treatment

Intentionally generic / not claimed:
- No Universal Industries mark, bucket type, belt/chain, capacity, speed, material compatibility, dust control, discharge method, inspection access, guarding, or installation specification is reproduced.
- No physical elevation or bulk-material handling is implied by the rendered asset.
