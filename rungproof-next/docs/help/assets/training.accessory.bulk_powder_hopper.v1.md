# Bulk Powder Hopper help

Asset ID: `training.accessory.bulk_powder_hopper.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.75 m
- Height: 1.45 m
- Depth: 2.05 m
- Source: `res://assets/training_accessories/bulk_powder_hopper/source/bulk_powder_hopper.blend`
- Delivery: `res://assets/training_accessories/bulk_powder_hopper/delivery/bulk_powder_hopper.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `gate_position` | linear | `KIN_SLIDE_GATE` | 0 to 0.4 m | 0.35 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **gravity-discharge bulk hopper with slide-gate valve**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WAMGROUP: VIB-VIBX low profile slide valves](https://sepcom.wamgroup.com/pt-BR/SEPCOM/Product/VIB-VIBX/Valvulas-de-gaveta-de-baixo-perfil) | oem-product-page | 2026-09-22 |

Modeled family features:
- wide rectangular hopper
- converging hopper walls
- lower outlet
- transverse slide-gate blade
- support frame

Intentionally generic / not claimed:
- No WAMGROUP mark, material flow rate, hopper volume, material compatibility, valve size, seal, actuator, pressure, dust control, guard, or installation specification is reproduced.
- The illustrated gate never releases physical bulk material or creates a process-safety claim.
