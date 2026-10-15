# Payoff Reel help

Asset ID: `training.accessory.payoff_reel.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 5.3 m
- Height: 4.2 m
- Depth: 0.8 m
- Source: `res://assets/training_accessories/payoff_reel/source/payoff_reel.blend`
- Delivery: `res://assets/training_accessories/payoff_reel/delivery/payoff_reel.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `door_position` | linear | `KIN_bottom_bar` | 0 to 3.2 m | 1.2 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial coiling steel service door**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Clopay: Roll-Up Sheet Doors product page](https://www.clopaydoor.com/coiling-sheet-doors) | oem-product-page | 2026-09-22 |

Modeled family features:
- interlocking horizontal curtain slats
- vertical guide tracks
- header coil/shaft zone
- bottom bar
- side-mounted operator context

Intentionally generic / not claimed:
- No Clopay mark, slat gauge, curtain material, door size, drive voltage, cycle count, weather sealing, or safety edge is reproduced.
- Simulator travel is not a real door safety or commissioning claim.
