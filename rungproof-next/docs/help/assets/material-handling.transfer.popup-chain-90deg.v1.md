# 90-Degree Pop-Up Chain Transfer help

Asset ID: `material-handling.transfer.popup-chain-90deg.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/transfers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.35 m
- Depth: 1.25 m
- Source: `res://assets/material_flow/popup_chain_transfer/source/popup_chain_transfer.blend`
- Delivery: `res://assets/material_flow/popup_chain_transfer/delivery/popup_chain_transfer.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `raise_command` | `bool` | `input` |  | Raise command. |
| `run_command` | `bool` | `input` |  | Run command. |
| `raised` | `bool` | `output` |  | Raised. |
| `lowered` | `bool` | `output` |  | Lowered. |
| `running` | `bool` | `output` |  | Running. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `transfer_lift` | linear | `KIN_LIFT_CYLINDER` | 0 to 0.12 m | 0.4 |
| `chain_speed` | linear_continuous | `KIN_POPUP_CHAIN_-0.28` | -1 to 1 m/s | 0.8 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pneumatically raised 90-degree pop-up chain transfer**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Erie pop-up chain-transfer equipment: several narrow chain strands sit above a live-roller bed at a perpendicular transfer orientation, with an under-bed lift/actuation volume and side frame. It reads as a generic 90-degree pop-up chain transfer.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Erie Technical Systems: Pop-up chain transfer conveyor](https://www.erietechnicalsystems.com/bulk-material-transfer-equipment/Pop-Up-Chain-Transfer-Conveyor) | oem-product-page | 2026-09-22 |

Modeled family features:
- perpendicular chain strands
- roller bed
- under-bed lift volume
- side frame
- 90-degree transfer layout

Intentionally generic / not claimed:
- No Erie mark, lift stroke, pneumatic circuit, pallet size, capacity, transfer rate, motor, sensor, interlock, or guarding is reproduced.
- No physical transfer, lifting, or machine-safety action is performed or implied.
