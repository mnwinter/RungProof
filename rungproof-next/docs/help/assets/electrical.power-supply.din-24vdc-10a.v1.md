# DIN-Rail 24 VDC 10 A Power Supply help

Asset ID: `electrical.power-supply.din-24vdc-10a.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/power/dc-supplies`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.6 m
- Height: 0.45 m
- Depth: 1.1 m
- Source: `res://assets/electrical_controls/din_24v_power_supply/source/din_24v_power_supply.blend`
- Delivery: `res://assets/electrical_controls/din_24v_power_supply/delivery/din_24v_power_supply.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `ac_present` | `bool` | `input` |  | Ac present. |
| `dc_ok` | `bool` | `output` |  | Dc ok. |
| `output_voltage` | `float32` | `output` | VDC | Output voltage. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **DIN-rail 24 VDC switched-mode control power supply**.
Source-model review: **compared-pass** — Fresh blind render compared with Siemens SITOP DIN-rail power supplies: an upright compact enclosure has cooling slots, distinct upper AC and lower DC terminal treatments, status LED, adjustment detail, and a 24 VDC 10 A face indication. It reads as a generic DIN-rail 24 VDC supply.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Siemens: SITOP 24 volt power supplies](https://www.siemens.com/en-us/products/sitop/) | oem-product-page | 2026-09-22 |

Modeled family features:
- upright DIN enclosure
- cooling slots
- upper and lower terminal rows
- status LED
- adjustment detail
- 24 VDC face indication

Intentionally generic / not claimed:
- No Siemens mark, input range, output capacity, efficiency, adjustment range, protection, diagnostics interface, approval, or wiring is reproduced.
- The asset supplies no electrical energy and is not a live power source.
