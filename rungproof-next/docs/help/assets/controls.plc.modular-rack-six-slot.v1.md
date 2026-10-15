# Six-Slot Modular PLC Rack help

Asset ID: `controls.plc.modular-rack-six-slot.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/plc`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.6 m
- Height: 0.4 m
- Depth: 0.9 m
- Source: `res://assets/electrical_controls/plc_rack/source/plc_rack.blend`
- Delivery: `res://assets/electrical_controls/plc_rack/delivery/plc_rack.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `controller_run` | `bool` | `input` |  | Controller run. |
| `controller_ok` | `bool` | `output` |  | Controller ok. |
| `io_fault` | `bool` | `output` |  | Io fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **modular cabinet-mounted programmable-logic-controller rack**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with a Siemens SIMATIC S7-1500 modular controller: a cabinet-mounted backplane carries a distinct power module, CPU with display/communication treatment, and four separate labeled I/O modules with status LEDs. It reads as a generic six-slot modular PLC rack.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Siemens: SIMATIC S7-1500 PLC](https://www.siemens.com/en-gb/products/simatic/s7-1500/) | oem-product-page | 2026-09-22 |

Modeled family features:
- backplane
- power module
- CPU/display treatment
- six module positions
- separate I/O faces
- status LEDs
- communication/terminal treatment

Intentionally generic / not claimed:
- No Siemens mark, CPU part number, I/O count, voltage, terminal pinout, firmware, network address, program, safety rating, or electrical behavior is reproduced.
- The PLC visual represents simulator-owned tags only; it does not connect to, read, or write physical PLC I/O.
