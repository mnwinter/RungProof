# Withdrawable MCC Motor-Feeder Bucket help

Asset ID: `electrical.mcc.bucket.withdrawable-motor-feeder.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/distribution/mcc`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.15 m
- Height: 0.9 m
- Depth: 1.6 m
- Source: `res://assets/electrical_controls/mcc_withdrawable_bucket/source/mcc_withdrawable_bucket.blend`
- Delivery: `res://assets/electrical_controls/mcc_withdrawable_bucket/delivery/mcc_withdrawable_bucket.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `disconnect_command` | `bool` | `input` |  | Disconnect command. |
| `disconnect_closed` | `bool` | `output` |  | Disconnect closed. |
| `starter_running` | `bool` | `output` |  | Starter running. |
| `starter_fault` | `bool` | `output` |  | Starter fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `disconnect_angle` | angular | `KIN_DISCONNECT_HANDLE` | 0 to 90 deg | 120 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **low-voltage motor-control-center withdrawable feeder unit**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with Siemens SIVACON S8 withdrawable-unit equipment: an upright compartment has a racked-out feeder drawer, face handle, pilot details, bus-stab treatment, guide rails, and an open compartment. It reads as a generic withdrawable MCC motor-feeder bucket.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Siemens: SIVACON S8 power distribution boards](https://www.siemens.com/en-us/products/sivacon/s8/) | oem-product-page | 2026-09-22 |

Modeled family features:
- upright MCC compartment
- partly withdrawn feeder bucket
- front handle
- pilot-detail treatment
- bus-stab treatment
- guide rails
- open compartment

Intentionally generic / not claimed:
- No Siemens mark, breaker/fuse/starter size, bus rating, arc classification, interlocking, voltage, SCCR, withdrawal procedure, wiring, or protection coordination is reproduced.
- The simulator does not withdraw a physical feeder, energize a motor circuit, or represent live switchgear operation.
