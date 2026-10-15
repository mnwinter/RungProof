# Horizontal Shell-and-Tube Heat Exchanger help

Asset ID: `process.heat-exchanger.shell-tube-horizontal.v1`  
Catalog status: **candidate / candidate**  
Category: `process/heat-transfer`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.7 m
- Height: 1.25 m
- Depth: 1.85 m
- Source: `res://assets/process_fluid/shell_tube_heat_exchanger/source/shell_tube_heat_exchanger.blend`
- Delivery: `res://assets/process_fluid/shell_tube_heat_exchanger/delivery/shell_tube_heat_exchanger.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `shell_inlet_temperature` | `float32` | `input` | degC | Shell inlet temperature. |
| `tube_inlet_temperature` | `float32` | `input` | degC | Tube inlet temperature. |
| `shell_outlet_temperature` | `float32` | `output` | degC | Shell outlet temperature. |
| `tube_outlet_temperature` | `float32` | `output` | degC | Tube outlet temperature. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |
| `process_3` |  |  |
| `process_4` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **horizontal shell-and-tube heat exchanger on saddle supports**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Alfa Laval horizontal shell-and-tube exchangers: a long cylindrical shell spans two saddle supports between large bolted channel/head flanges, with distinct shell-side and tube-side nozzle treatment. It reads as a generic horizontal shell-and-tube heat exchanger.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Alfa Laval: Shell-and-tube heat exchangers](https://www.alfalaval.com/en-GB/products/heat-transfer/tubular-heat-exchangers/shell-and-tube-heat-exchanger/) | oem-product-page | 2026-09-22 |

Modeled family features:
- long cylindrical shell
- saddle supports
- bolted channel/head flanges
- shell-side nozzle
- tube-side connection treatment

Intentionally generic / not claimed:
- No Alfa Laval mark, tube bundle, baffles, materials, duty, temperature, pressure, flow, thermal design, nozzle rating, relief, or code compliance is reproduced.
- This asset neither exchanges heat nor establishes a pressure-containing process system.
