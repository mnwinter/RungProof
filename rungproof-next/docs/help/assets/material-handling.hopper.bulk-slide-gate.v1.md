# Bulk Hopper with Powered Slide Gate help

Asset ID: `material-handling.hopper.bulk-slide-gate.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/bulk/hoppers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.75 m
- Height: 1.45 m
- Depth: 2.05 m
- Source: `res://assets/material_flow/bulk_hopper_slide_gate/source/bulk_hopper_slide_gate.blend`
- Delivery: `res://assets/material_flow/bulk_hopper_slide_gate/delivery/bulk_hopper_slide_gate.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `open_command` | `bool` | `input` |  | Open command. |
| `close_command` | `bool` | `input` |  | Close command. |
| `gate_open` | `bool` | `output` |  | Gate open. |
| `gate_closed` | `bool` | `output` |  | Gate closed. |

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
Source-model review: **compared-pass** — Fresh blind render reviewed against WAM VIB low-profile slide-gate form at a hopper discharge: a wide rectangular hopper converges to a lower outlet where a contrasting transverse slide blade/gate and support frame are visibly modeled. It reads as a generic bulk hopper with slide gate.

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
