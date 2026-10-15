# Centralized Grease Lubrication Skid help

Asset ID: `utilities.lubrication.centralized-grease-skid.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/lubrication/systems`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.8 m
- Height: 1.1 m
- Depth: 2.0 m
- Source: `res://assets/utilities/central_lubrication_skid/source/central_lubrication_skid.blend`
- Delivery: `res://assets/utilities/central_lubrication_skid/delivery/central_lubrication_skid.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `lube_cycle_command` | `bool` | `input` |  | Lube cycle command. |
| `cycle_complete` | `bool` | `output` |  | Cycle complete. |
| `low_grease_level` | `bool` | `output` |  | Low grease level. |
| `lube_fault` | `bool` | `output` |  | Lube fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pump_rotation` | continuous | `KIN_LUBE_PUMP` | 0 to 360 deg | 300 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **centralized automatic grease-lubrication pump skid**.
Source-model review: **compared-pass** — Fresh blind render compared with Graco automatic grease-lubrication pumps: a prominent grease reservoir sits above a pump/controller base, with gauge and multiple distribution-line outlets on a skid. It reads as a generic centralized grease-lubrication unit.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Graco: G-Mini grease lubrication pump with controller](https://www.graco.com/us/en/in-plant-manufacturing/product/25r821.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- grease reservoir
- pump/controller base
- gauge
- distribution outlets
- service lines
- skid

Intentionally generic / not claimed:
- No Graco mark, grease grade, reservoir volume, pressure, output, metering blocks, line assignment, controller schedule, or fault monitoring is reproduced.
- The visual does not dispense grease or lubricate equipment.
