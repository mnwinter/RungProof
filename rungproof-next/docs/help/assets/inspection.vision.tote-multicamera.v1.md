# Multi-Camera Tote Vision Inspection Station help

Asset ID: `inspection.vision.tote-multicamera.v1`  
Catalog status: **candidate / candidate**  
Category: `inspection/vision/packaging`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.1 m
- Height: 2.7 m
- Depth: 1.9 m
- Source: `res://assets/tote_processing/tote_vision_inspection_station/source/tote_vision_inspection_station.blend`
- Delivery: `res://assets/tote_processing/tote_vision_inspection_station/delivery/tote_vision_inspection_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `inspection_run` | `bool` | `input` |  | Inspection run. |
| `inspection_ok` | `bool` | `output` |  | Inspection ok. |
| `vision_fault` | `bool` | `output` |  | Vision fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `camera_scan` | rotary | `KIN_vision_lens` | -4 to 4 deg | 8 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **multi-camera package inspection station**.
Source-model review: **compared-pass** — Rendered review compared with Cognex OmniView multi-camera package-inspection architecture: a rigid overhead arch surrounds the tote with a central camera, two opposed side cameras, front optical faces, integrated light treatments, backlight panel, and cable treatment. It reads as a generic multi-camera package-inspection station.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Cognex: OmniView multi-camera vision solution data sheet](https://partner.cognex.com/Root/Specifications/OmniView%20datasheet.pdf.xsd) | oem-datasheet | 2026-09-22 |

Modeled family features:
- overhead arch
- central camera
- opposed side cameras
- front optical faces
- light treatments
- backlight panel
- cable treatment

Intentionally generic / not claimed:
- No Cognex mark, camera model, lens, lighting wavelength, field of view, resolution, inspection algorithm, network, accuracy, or safety classification is reproduced.
- The visual does not illuminate, image, inspect, reject, or make a quality decision.
