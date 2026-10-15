# Industrial Pedestal Drill Press help

Asset ID: `machining.drill-press.pedestal.v1`  
Catalog status: **production / approved**  
Category: `machining/drilling`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 4.1 m
- Depth: 1.6 m
- Source: `res://assets/scene_core/industrial_drill_press/source/industrial_drill_press.blend`
- Delivery: `res://assets/scene_core/industrial_drill_press/delivery/industrial_drill_press.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `guard_closed` | `bool` | `input` |  | Guard closed. |
| `spindle_position` | `float32` | `output` |  | Spindle position. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `spindle_speed` | rotary_continuous | `KIN_spindle` | 0 to 3000 rpm | 800 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **floor-standing industrial pedestal drill press**.
Source-model review: **compared-pass** — Rendered review shows the recognizable floor drill-press family: broad cast base, vertical column, slotted adjustable work table, head with belt/motor enclosure, feed handles, spindle/chuck, and a table vise. It reads as a generic industrial pedestal drill press rather than a branded machine.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [JET Tools: 20 inch Floor Model Drill Press, J-2550](https://www.jettools.com/j-2550-20-floor-model-drill-press-230v-1ph) | oem-product-page | 2026-09-22 |

Modeled family features:
- cast base
- vertical column
- slotted work table
- drill head
- motor/belt enclosure
- feed handles
- spindle and chuck
- vise context

Intentionally generic / not claimed:
- No JET mark, drilling capacity, motor rating, spindle taper, spindle speed, tool, work material, guarding design, electrical specification, or operating procedure is reproduced.
- The symbolic run state is simulator-only and does not represent a live or safe drilling operation.
