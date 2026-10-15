# Automatic Robot Tool Changer help

Asset ID: `robotics.tooling.changer.automatic-pneumatic.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/tooling/tool-changers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.6 m
- Height: 1.2 m
- Depth: 1.3 m
- Source: `res://assets/robotics/automatic_tool_changer/source/automatic_tool_changer.blend`
- Delivery: `res://assets/robotics/automatic_tool_changer/delivery/automatic_tool_changer.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `lock_position_command` | `float32` | `input` |  | Lock position command. |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `lock_position` | linear | `KIN_MASTER_COUPLER` | 0 to 0.04 m | 0.1 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pneumatic automatic robot tool changer**.
Source-model review: **compared-pass** — Rendered review compared with ATI QC pneumatic tool changers: a robot-side master plate mates with a tool-side plate, with circular coupling faces, utility-port treatments, and a locking-piston treatment. It reads as a generic pneumatic automatic tool changer.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ATI Industrial Automation: QC-11 tool changer](https://www.ati-ia.com/products/toolchanger/qc.aspx?id=qc-11) | oem-product-page | 2026-09-22 |

Modeled family features:
- robot-side master plate
- tool-side plate
- circular coupling faces
- utility-port treatments
- locking-piston treatment

Intentionally generic / not claimed:
- No ATI mark, capacity, locking force, port size, pressure, electrical module, sensing, or fail-safe performance is reproduced.
- The visual does not couple tooling, transfer utilities, or make a safe-lock claim.
