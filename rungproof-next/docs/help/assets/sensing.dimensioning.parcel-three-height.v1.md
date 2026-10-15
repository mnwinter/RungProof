# Three-Height Parcel Sensor Bank help

Asset ID: `sensing.dimensioning.parcel-three-height.v1`  
Catalog status: **candidate / candidate**  
Category: `sensing/dimensioning`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.6 m
- Height: 2.4 m
- Depth: 2.6 m
- Source: `res://assets/scene_support/three_height_parcel_sensor_bank/source/three_height_parcel_sensor_bank.blend`
- Delivery: `res://assets/scene_support/three_height_parcel_sensor_bank/delivery/three_height_parcel_sensor_bank.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `size_beam_low` | `bool` | `output` |  | Size beam low. |
| `size_beam_mid` | `bool` | `output` |  | Size beam mid. |
| `size_beam_high` | `bool` | `output` |  | Size beam high. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-height through-beam parcel dimensioning sensor bank**.
Source-model review: **compared-pass** — Rendered review shows a compact parcel dimensioning portal with paired photoelectric heads at three heights, a readout/junction box, and a parcel/belt context. It reads as a generic three-height parcel-detection bank.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Banner Engineering: Photoelectric sensors product family](https://www.bannerengineering.com/us/en/products/sensors/photoelectric-sensors.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- paired photoelectric heads
- three measurement heights
- portal frame
- junction box
- height readout
- parcel/belt context

Intentionally generic / not claimed:
- No Banner mark, sensing range, beam diameter, response time, output, accuracy, safety rating, or dimensional algorithm is reproduced.
- The visible beams are explanatory artwork and do not measure a parcel.
