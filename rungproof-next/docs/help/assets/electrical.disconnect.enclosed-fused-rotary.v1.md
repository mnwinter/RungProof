# Enclosed Fused Disconnect Switch help

Asset ID: `electrical.disconnect.enclosed-fused-rotary.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/distribution/disconnects`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.8 m
- Height: 0.55 m
- Depth: 1.3 m
- Source: `res://assets/electrical_controls/fused_disconnect/source/fused_disconnect.blend`
- Delivery: `res://assets/electrical_controls/fused_disconnect/delivery/fused_disconnect.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `close_command` | `bool` | `input` |  | Close command. |
| `closed` | `bool` | `output` |  | Closed. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `handle_angle` | angular | `KIN_HANDLE` | 0 to 90 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **enclosed fused rotary-handle disconnect switch**.
Source-model review: **compared-pass** — After a fresh corrected blind render, the enclosure was compared with Schneider Electric UL 98 rotary-handle fused-disconnect equipment: the upright enclosure has a through-door rotary hub and handle, clear ON/OFF indication, cable-entry treatment, and a sealed three-position fuse inspection row. It reads as a generic enclosed fused rotary disconnect.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: NEMA-Style UL 98 rotary handle disconnect switches](https://www.se.com/us/en/product-range/7501-nemastyle-ul-98-rotary-handle-disconnect-switches/) | oem-product-page | 2026-09-22 |

Modeled family features:
- enclosed body
- through-door rotary hub
- rotary handle
- ON/OFF indication
- cable-entry treatment
- sealed three-position fuse inspection row

Intentionally generic / not claimed:
- No Schneider Electric mark, enclosure type, switch/fuse class, ampere rating, voltage, SCCR, fuse status, interlock, lockout provision, internal wiring, or certification is reproduced.
- The visual does not isolate a circuit or establish an electrically safe work condition; follow the applicable LOTO and verification procedure on real equipment.
