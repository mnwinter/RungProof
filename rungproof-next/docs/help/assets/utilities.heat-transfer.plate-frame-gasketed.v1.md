# Gasketed Plate-and-Frame Heat Exchanger help

Asset ID: `utilities.heat-transfer.plate-frame-gasketed.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/heat-transfer/exchangers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.8 m
- Height: 1.1 m
- Depth: 1.9 m
- Source: `res://assets/utilities/plate_frame_heat_exchanger/source/plate_frame_heat_exchanger.blend`
- Delivery: `res://assets/utilities/plate_frame_heat_exchanger/delivery/plate_frame_heat_exchanger.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `primary_inlet_temperature` | `float32` | `input` | degC | Primary inlet temperature. |
| `secondary_inlet_temperature` | `float32` | `input` | degC | Secondary inlet temperature. |
| `secondary_outlet_temperature` | `float32` | `output` | degC | Secondary outlet temperature. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **gasketed plate-and-frame heat exchanger**.
Source-model review: **compared-pass** — Fresh blind render compared with Alfa Laval gasketed plate-and-frame exchangers: two strong end frames compress a dense visible plate pack using long tie bolts, with four grouped corner-port treatments. It reads as a generic gasketed plate-frame heat exchanger.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Alfa Laval: Gasketed plate heat exchangers](https://www.alfalaval.com/globalassets/documents/microsites/heating-and-cooling-hub/pd-leaflets---gasketed/alfa-laval-gasketed-plate-heat-exchangers.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- end frames
- visible plate pack
- tie bolts
- four port treatments
- base

Intentionally generic / not claimed:
- No Alfa Laval mark, plate count, plate material, gasket material, pressure, temperature, media, heat duty, connection class, or code approval is reproduced.
- The visual does not exchange heat or establish a pressure boundary.
