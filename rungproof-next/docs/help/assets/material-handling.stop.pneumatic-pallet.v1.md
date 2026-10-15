# Pneumatic Pallet Stop help

Asset ID: `material-handling.stop.pneumatic-pallet.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/stops`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.95 m
- Height: 0.72 m
- Depth: 1.1 m
- Source: `res://assets/material_flow/pneumatic_pallet_stop/source/pneumatic_pallet_stop.blend`
- Delivery: `res://assets/material_flow/pneumatic_pallet_stop/delivery/pneumatic_pallet_stop.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `raise_command` | `bool` | `input` |  | Raise command. |
| `lower_command` | `bool` | `input` |  | Lower command. |
| `raised` | `bool` | `output` |  | Raised. |
| `lowered` | `bool` | `output` |  | Lowered. |
| `pallet_present` | `bool` | `output` |  | Pallet present. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `stop_height` | linear | `KIN_STOP_BLADE` | 0 to 0.3 m | 0.5 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pneumatic conveyor pallet stop with spring-return behavior**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Dorner pneumatic pallet stops: a low mounting base carries a vertical stop blade with guide posts, pneumatic-cylinder body, and fitting treatment. It reads as a generic rail-mounted pneumatic pallet stop/separator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Dorner: FlexMove pallet-system pallet stops](https://www.dornerconveyors.com/products/flexmove/flexmove-pallet-system/pallet-stops) | oem-product-page | 2026-09-22 |

Modeled family features:
- mounting base
- vertical stop blade
- guide posts
- pneumatic-cylinder body
- fitting treatment

Intentionally generic / not claimed:
- No Dorner mark, stop height, spring return, cushion, load, speed, air pressure, sensing, rail interface, or guarding specification is reproduced.
- The asset does not provide a real stop, separation, pneumatic function, or safety control.
