# Tongue-Actuated Safety Interlock Switch help

Asset ID: `safety.interlock.tongue-switch.v1`  
Catalog status: **production / approved**  
Category: `safety/interlocks`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.85 m
- Height: 0.9 m
- Depth: 0.55 m
- Source: `res://assets/controls_sensors/tongue_safety_interlock/source/tongue_safety_interlock.blend`
- Delivery: `res://assets/controls_sensors/tongue_safety_interlock/delivery/tongue_safety_interlock.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `guard_closed` | `bool` | `output` |  | Guard closed. |
| `safety_ok` | `bool` | `output` |  | Safety ok. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **tongue-actuated guard-door safety interlock**.
Source-model review: **compared-pass** — Rendered review shows a separate rectangular interlock body and tongue actuator crossing a guarded seam, with a dedicated actuator-entry face, mounting surfaces, and cable exit. It reads as a generic tongue-actuated guard-door interlock family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schmersal: AZM 190 solenoid interlock product page](https://products.schmersal.com/en_US/azm-190-1000375253) | oem-product-page | 2026-09-22 |

Modeled family features:
- separate body and tongue actuator across a guard seam
- rectangular interlock body with a guarded actuator entry
- machine-side mounting plates and cable exit

Intentionally generic / not claimed:
- No Schmersal markings, locking force, safety category, diagnostic coverage, wiring, or process safety claim is reproduced.
- The model is a visual guard-state teaching aid and does not represent a safety design.
