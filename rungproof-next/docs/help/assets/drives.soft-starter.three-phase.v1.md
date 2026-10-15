# Three-Phase Soft Starter help

Asset ID: `drives.soft-starter.three-phase.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/drives/soft-starters`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 0.55 m
- Depth: 1.3 m
- Source: `res://assets/electrical_controls/soft_starter/source/soft_starter.blend`
- Delivery: `res://assets/electrical_controls/soft_starter/delivery/soft_starter.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `start_command` | `bool` | `input` |  | Start command. |
| `ramp_time` | `float32` | `input` | s | Ramp time. |
| `running` | `bool` | `output` |  | Running. |
| `starter_fault` | `bool` | `output` |  | Starter fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-phase solid-state motor soft starter**.
Source-model review: **compared-pass** — Fresh blind render compared with Schneider Electric Altistart 01 soft starters: a tall starter body has three upper line and three lower load connections, a front display/keypad treatment, and visible heat-sink/vent treatment. It reads as a generic three-phase soft starter.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: Altistart 01 soft start](https://ezlist.schneider-electric.com/ezws/prodinfo/?lang=en_US&range=779) | oem-product-page | 2026-09-22 |

Modeled family features:
- starter body
- three line terminals
- three load terminals
- front display/keypad treatment
- heat-sink/vent treatment

Intentionally generic / not claimed:
- No Schneider Electric mark, voltage, current, motor rating, ramp setting, bypass, torque control, fault behavior, parameter set, or certification is reproduced.
- The visual does not ramp, start, or stop a physical motor.
