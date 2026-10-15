# Vibratory Bowl Feeder help

Asset ID: `material-handling.feeder.vibratory-bowl.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/feeders/vibratory`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.65 m
- Height: 1.45 m
- Depth: 1.35 m
- Source: `res://assets/material_flow/vibratory_bowl_feeder/source/vibratory_bowl_feeder.blend`
- Delivery: `res://assets/material_flow/vibratory_bowl_feeder/delivery/vibratory_bowl_feeder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `amplitude_setpoint` | `float32` | `input` | percent | Amplitude setpoint. |
| `part_available` | `bool` | `output` |  | Part available. |
| `running` | `bool` | `output` |  | Running. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `feed_rate` | linear_continuous | `KIN_SPIRAL_TRACK_0` | 0 to 1 normalized | 2.0 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **outside-track industrial vibratory bowl feeder**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Vibratory Feeders Inc. outside-track bowl families: a circular vibratory bowl contains a rising spiral track that carries and orients small parts to a tangential linear exit rail, above a separate drive base. It reads as a generic vibratory bowl feeder.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Vibratory Feeders, Inc.: Vibratory bowls and parts-feeding systems](https://www.vibratoryfeeders.com/) | oem-product-page | 2026-09-22 |

Modeled family features:
- circular bowl
- rising spiral track
- small part representations
- tangential linear exit
- separate drive base

Intentionally generic / not claimed:
- No VFI mark, bowl diameter, part-orientation tooling, vibration frequency/amplitude, drive, controller, rate, material, noise, guard, or safety specification is reproduced.
- Parts do not actually vibrate, orient, or feed in the simulator.
