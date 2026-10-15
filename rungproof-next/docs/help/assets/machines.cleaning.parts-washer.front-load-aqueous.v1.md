# Front-Load Aqueous Parts Washer help

Asset ID: `machines.cleaning.parts-washer.front-load-aqueous.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/cleaning/parts-washers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.1 m
- Depth: 1.6 m
- Source: `res://assets/production-machines/aqueous_parts_washer/source/aqueous_parts_washer.blend`
- Delivery: `res://assets/production-machines/aqueous_parts_washer/delivery/aqueous_parts_washer.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `cycle_start` | `bool` | `input` |  | Cycle start. |
| `door_open` | `bool` | `input` |  | Door open. |
| `wash_pump_run` | `bool` | `input` |  | Wash pump run. |
| `basket_run` | `bool` | `input` |  | Basket run. |
| `door_closed` | `bool` | `output` |  | Door closed. |
| `cycle_complete` | `bool` | `output` |  | Cycle complete. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `door_angle` | rotary | `KIN_FRONT_DOOR` | 0 to 105 deg | 30 |
| `basket_rotation` | continuous | `KIN_WASH_BASKET` | 0 to 360 deg | 18 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **front-load aqueous parts washer**.
Source-model review: **compared-pass** — Fresh blind render compared with AMSONIC front-load aqueous cleaning equipment: a sealed cabinet has a front viewing door, control face, enclosure panels, and neutral interlock/stop treatment. It reads as a generic front-load aqueous parts-washer family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [AMSONIC: Industrial cleaning systems](https://www.amsonic.com/) | oem-product-page | 2026-09-22 |

Modeled family features:
- sealed cabinet
- front viewing door
- control face
- enclosure panels
- interlock/stop treatment

Intentionally generic / not claimed:
- No AMSONIC mark, wash chemistry, temperature, pump, nozzle, load capacity, drying process, interlock rating, or certification is reproduced.
- The visual does not spray, heat, clean, drain, or prove chemical/process safety.
