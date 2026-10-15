# ISO Tie-Rod Pneumatic Cylinder help

Asset ID: `actuation.pneumatic.cylinder.iso-tie-rod.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/pneumatic/cylinders`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 0.6 m
- Depth: 0.9 m
- Source: `res://assets/mechanical_motion/iso_tie_rod_pneumatic_cylinder/source/iso_tie_rod_pneumatic_cylinder.blend`
- Delivery: `res://assets/mechanical_motion/iso_tie_rod_pneumatic_cylinder/delivery/iso_tie_rod_pneumatic_cylinder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `extend_command` | `bool` | `input` |  | Extend command. |
| `retract_command` | `bool` | `input` |  | Retract command. |
| `position_m` | `float32` | `output` | m | Position m. |
| `extended` | `bool` | `output` |  | Extended. |
| `retracted` | `bool` | `output` |  | Retracted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `rod_travel` | linear | `KIN_rod` | 0 to 0.6 m | 1.0 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **ISO-style pneumatic tie-rod cylinder**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SMC CA2 tie-rod-cylinder family: round barrel, opposing end plates, four external tie rods, rod, rod-eye mount, port fittings, and base mounting context form a credible generic pneumatic tie-rod cylinder.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: Core Automation Products - CA2 tie rod cylinder](https://www.smcusa.com/solutions/applications/core-products) | oem-product-page | 2026-09-22 |

Modeled family features:
- round barrel
- end plates
- external tie rods
- piston rod
- rod eye
- air fittings
- base mount

Intentionally generic / not claimed:
- No SMC mark, ISO size, bore, stroke, pressure, port thread, cushioning, sensor, load, or mounting rating is reproduced.
- Motion is symbolic simulator kinematics only; no pneumatic circuit or physical actuator is controlled.
