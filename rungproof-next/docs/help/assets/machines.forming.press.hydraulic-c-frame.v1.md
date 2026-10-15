# Hydraulic C-Frame Press help

Asset ID: `machines.forming.press.hydraulic-c-frame.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/forming/presses`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.7 m
- Height: 2.5 m
- Depth: 1.3 m
- Source: `res://assets/production-machines/hydraulic_c_frame_press/source/hydraulic_c_frame_press.blend`
- Delivery: `res://assets/production-machines/hydraulic_c_frame_press/delivery/hydraulic_c_frame_press.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `cycle_start` | `bool` | `input` |  | Cycle start. |
| `ram_extend` | `bool` | `input` |  | Ram extend. |
| `ram_position` | `float32` | `output` | mm | Ram position. |
| `pressure` | `float32` | `output` | bar | Pressure. |
| `guard_clear` | `bool` | `output` |  | Guard clear. |
| `press_fault` | `bool` | `output` |  | Press fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `ram_position` | linear | `KIN_PRESS_RAM` | 0 to 0.55 m | 0.2 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **hydraulic C-frame forming press**.
Source-model review: **compared-pass** — Fresh blind render compared with Beckwood C-frame hydraulic presses: the open three-sided C frame carries an overhead cylinder, ram, bed/bolster, guarded two-hand control treatment, and side control panel. It reads as a generic hydraulic C-frame press.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Beckwood Press: C-frame hydraulic presses](https://beckwoodpress.com/product-lines/custom/c-frame/) | oem-product-page | 2026-09-22 |

Modeled family features:
- C frame
- overhead cylinder
- ram
- bed
- two-hand control treatment
- control panel

Intentionally generic / not claimed:
- No Beckwood mark, tonnage, stroke, die, guarding validation, controls, hydraulic circuit, or certification is reproduced.
- The asset never exerts force or performs a forming operation.
