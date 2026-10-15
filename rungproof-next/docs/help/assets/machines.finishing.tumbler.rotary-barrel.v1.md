# Rotary Barrel Mass-Finishing Tumbler help

Asset ID: `machines.finishing.tumbler.rotary-barrel.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/finishing/tumblers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.9 m
- Height: 2.0 m
- Depth: 1.8 m
- Source: `res://assets/production-machines/rotary_barrel_tumbler/source/rotary_barrel_tumbler.blend`
- Delivery: `res://assets/production-machines/rotary_barrel_tumbler/delivery/rotary_barrel_tumbler.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_setpoint` | `float32` | `input` | rpm | Speed setpoint. |
| `running` | `bool` | `output` |  | Running. |
| `cycle_complete` | `bool` | `output` |  | Cycle complete. |
| `finisher_fault` | `bool` | `output` |  | Finisher fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `barrel_rotation` | continuous | `KIN_ROTARY_BARREL` | 0 to 360 deg | 30 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **rotary barrel finishing machine**.
Source-model review: **compared-pass** — Fresh blind render compared with Rösler rotary barrel finishing systems: a polygonal barrel has a load door, trunnion/drive treatment, discharge treatment, and nearby controls. It reads as a generic rotary-barrel finishing family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Rösler: Mass finishing systems](https://www.rosler.com/) | oem-product-page | 2026-09-22 |

Modeled family features:
- polygonal barrel
- load door
- trunnion/drive treatment
- discharge treatment
- control treatment

Intentionally generic / not claimed:
- No Rösler mark, barrel volume, media, compound, motor, speed, load, guard design, or certification is reproduced.
- The visual does not rotate, finish parts, contain media, or establish operator safety.
