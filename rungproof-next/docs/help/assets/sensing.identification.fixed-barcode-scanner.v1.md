# Fixed Industrial Barcode Scanner help

Asset ID: `sensing.identification.fixed-barcode-scanner.v1`  
Catalog status: **production / approved**  
Category: `sensing/identification`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 1.85 m
- Depth: 0.75 m
- Source: `res://assets/factory_kit/fixed_barcode_scanner/source/fixed_barcode_scanner.blend`
- Delivery: `res://assets/factory_kit/fixed_barcode_scanner/delivery/fixed_barcode_scanner.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `trigger` | `bool` | `input` |  | Trigger. |
| `code_present` | `bool` | `output` |  | Code present. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **compact fixed-mount industrial barcode scanner**.
Source-model review: **compared-pass** — The corrected review shows a compact dark protective housing on an adjustable machine bracket, a protected front scan aperture, two neutral optical elements, status indicator, rear connector/cable treatment, and a barcode-marked target parcel. It reads as a generic fixed-mount industrial barcode-scanner family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SICK: CLV62x fixed-mount barcode scanner](https://www.sick.com/my/en/catalog/products/machine-vision-and-identification/fixed-mount-barcode-scanners/clv62x/c/g79824) | oem-product-page | 2026-09-22 |

Modeled family features:
- compact protective housing
- front scan aperture
- neutral status indicator context
- rear or side connector
- machine-mount bracket

Intentionally generic / not claimed:
- No SICK mark, code type, reading distance, scan rate, optical performance, interface, IP rating, mounting dimensions, or barcode data is reproduced.
- Any scan line is visual-only simulator context and must never claim active optical emission or code reading.
