# Manual Engine Lathe help

Asset ID: `machines.machining.lathe.manual-engine.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/machining/lathes`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.3 m
- Height: 1.9 m
- Depth: 1.4 m
- Source: `res://assets/production-machines/manual_engine_lathe/source/manual_engine_lathe.blend`
- Delivery: `res://assets/production-machines/manual_engine_lathe/delivery/manual_engine_lathe.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `spindle_run` | `bool` | `input` |  | Spindle run. |
| `spindle_speed` | `float32` | `input` | rpm | Spindle speed. |
| `carriage_feed` | `bool` | `input` |  | Carriage feed. |
| `chuck_guard_closed` | `bool` | `output` |  | Chuck guard closed. |
| `lathe_fault` | `bool` | `output` |  | Lathe fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `spindle_rotation` | continuous | `KIN_SPINDLE_CHUCK` | 0 to 360 deg | 2400 |
| `carriage_position` | linear | `KIN_CARRIAGE` | 0 to 1.55 m | 0.18 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **manual engine lathe**.
Source-model review: **compared-pass** — Fresh blind render compared with Haas manual lathes: bedways carry a headstock and chuck, carriage/toolpost, tailstock, and handwheel treatment. It reads as a generic manual engine-lathe family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Haas Automation: HML-2V-CE manual lathe](https://www.haascnc.com/machines/shop-equipment/manual-lathes/hml-2v-ce.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- bedways
- headstock and chuck
- carriage/toolpost
- tailstock
- handwheel treatment

Intentionally generic / not claimed:
- No Haas mark, swing, bed length, spindle speed, motor rating, tooling, guard design, wiring, or certification is reproduced.
- The visual does not rotate a spindle, machine stock, or establish a safe setup.
