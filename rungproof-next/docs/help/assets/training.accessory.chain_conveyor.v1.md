# Chain Conveyor help

Asset ID: `training.accessory.chain_conveyor.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.0 m
- Height: 1.3 m
- Depth: 1.25 m
- Source: `res://assets/training_accessories/chain_conveyor/source/chain_conveyor.blend`
- Delivery: `res://assets/training_accessories/chain_conveyor/delivery/chain_conveyor.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `chain_speed` | linear_continuous | `KIN_CHAIN_SLAT_-0.31_0` | -1.0 to 1.0 m/s | 0.8 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **two-strand roller-chain pallet conveyor**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [mk Technology Group: KTF-P 2010 chain conveyor](https://www.mk-group.com/en/products/chain-conveyors/ktf-p-2010.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- two parallel chain strands
- repeated chain links
- pallet
- rigid side frames
- end-drive treatment

Intentionally generic / not claimed:
- No mk mark, chain pitch, chain construction, pallet dimensions, load, speed, motor, tensioner, lubrication, guard, or safety function is reproduced.
- Transport is symbolic simulator behavior only and never moves physical product.
