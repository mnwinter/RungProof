# Industrial Stereo 3D Vision Camera help

Asset ID: `robotics.vision.camera.stereo-3d-industrial.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/vision/cameras`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.4 m
- Height: 1.6 m
- Depth: 1.3 m
- Source: `res://assets/robotics/industrial_3d_vision_camera/source/industrial_3d_vision_camera.blend`
- Delivery: `res://assets/robotics/industrial_3d_vision_camera/delivery/industrial_3d_vision_camera.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |
| `trigger` | `bool` | `input` |  | Acquisition trigger. |
| `inspection_passed` | `bool` | `output` |  | Inspection result. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial stereo 3D vision camera**.
Source-model review: **compared-pass** — Rendered review compared with SICK Ranger3 industrial 3D cameras: a rigid stand supports a compact industrial camera housing with separated front optical elements, structured-light treatment, industrial connector treatments, and calibration target. It reads as a generic industrial stereo/3D vision camera.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SICK: Ranger3 3D vision product information](https://www.sick.com/media/docs/8/48/448/product_information_ranger3_3d_vision_en_im0080448.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- rigid stand
- compact camera housing
- separated optical elements
- structured-light treatment
- industrial connector treatments
- calibration target

Intentionally generic / not claimed:
- No SICK mark, optics, resolution, measurement range, illumination, network interface, protection rating, calibration, accuracy, or safety classification is reproduced.
- The visual does not emit light, capture images, or produce a dimensional measurement.
