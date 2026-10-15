# Pressure-Sensitive Tote Labeling Station help

Asset ID: `process.packaging.labeler.tote-pressure-sensitive.v1`  
Catalog status: **candidate / candidate**  
Category: `process/packaging/labeling`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.1 m
- Height: 2.6 m
- Depth: 1.8 m
- Source: `res://assets/tote_processing/tote_labeling_station/source/tote_labeling_station.blend`
- Delivery: `res://assets/tote_processing/tote_labeling_station/delivery/tote_labeling_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `labeler_run` | `bool` | `input` |  | Labeler run. |
| `label_low` | `bool` | `output` |  | Label low. |
| `labeler_fault` | `bool` | `output` |  | Labeler fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `label_roll_rotation` | continuous | `KIN_label_roll` | 0 to 360 deg | 55 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pressure-sensitive print-and-apply labeling station**.
Source-model review: **compared-pass** — Rendered review compared with Videojet print-and-apply architecture: a print/apply enclosure exposes a label-roll window, roll core, exit slot, horizontal air-cylinder/tamp slide, applicator pad, and applied-label treatment on the tote. It reads as a generic pressure-sensitive print-and-apply labeling station.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Videojet: 9550 label printer applicator](https://www.videojet.com/us/homepage/industry-solutions/pharma-hub/videojet-coding-and-marking-technology-lpa.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- print/apply enclosure
- label-roll window
- roll core
- exit slot
- tamp slide
- applicator pad
- applied-label treatment

Intentionally generic / not claimed:
- No Videojet mark, printer technology, label size, print quality, throughput, air requirement, barcode content, data integration, or certification is reproduced.
- The visual does not print, encode, verify, or apply a physical label.
