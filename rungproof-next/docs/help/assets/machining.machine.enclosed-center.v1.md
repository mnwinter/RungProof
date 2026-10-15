# Enclosed Machining Center help

Asset ID: `machining.machine.enclosed-center.v1`  
Catalog status: **candidate / candidate**  
Category: `machining/machine-tools`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.6 m
- Height: 3.3 m
- Depth: 2.3 m
- Source: `res://assets/scene_core/enclosed_machine_center/source/enclosed_machine_center.blend`
- Delivery: `res://assets/scene_core/enclosed_machine_center/delivery/enclosed_machine_center.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `running` | `bool` | `output` |  | Running. |
| `door_closed` | `bool` | `output` |  | Door closed. |
| `faulted` | `bool` | `output` |  | Faulted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `spindle_speed` | rotary_continuous | `KIN_spindle` | 0 to 12000 rpm | 3000 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **enclosed vertical CNC machining center**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Haas VF vertical-machining-center family: the generic model has a full enclosure, twin glazed sliding front doors, visible spindle/work envelope, chip/coolant-style internal context, base, and a separate operator control pendant. It is not an exact Haas representation and carries no machining, guarding, spindle-speed, or commissioning claim.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Haas Automation: VF Series Vertical Mills](https://www.haascnc.com/machines/vertical-mills/vf-series.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- enclosed machine shell
- sliding observation doors
- spindle/work-zone context
- operator control pendant
- anchored base

Intentionally generic / not claimed:
- No Haas mark, model, axis travel, spindle taper, speed, toolchanger, coolant system, control, guarding rating, or machine specification is reproduced.
- Commands and kinematics remain symbolic simulator behavior; this asset is never a live CNC or commissioning representation.
