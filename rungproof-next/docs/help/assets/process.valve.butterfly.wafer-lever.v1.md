# Wafer Butterfly Valve with Lever help

Asset ID: `process.valve.butterfly.wafer-lever.v1`  
Catalog status: **candidate / candidate**  
Category: `process/valves/isolation`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.3 m
- Height: 1.0 m
- Depth: 1.75 m
- Source: `res://assets/process_fluid/wafer_butterfly_valve/source/wafer_butterfly_valve.blend`
- Delivery: `res://assets/process_fluid/wafer_butterfly_valve/delivery/wafer_butterfly_valve.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `open_command` | `bool` | `input` |  | Open command. |
| `close_command` | `bool` | `input` |  | Close command. |
| `position_percent` | `float32` | `output` | percent | Position percent. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `disc_angle` | angular | `KIN_DISC` | 0 to 90 deg | 90 |
| `lever_angle` | angular | `KIN_LEVER` | 0 to 90 deg | 90 |

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **wafer-style lever-operated butterfly valve**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Bray Series 3W wafer butterfly-valve family: a thin circular wafer body is captured between flanged pipe stubs, with a visible disc sector, central stem, bolting ears, and quarter-turn lever. It reads as a generic lever-operated wafer butterfly valve.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Bray: Series 3W/3L resilient seated butterfly valve](https://www.bray.com/valves-actuators-controls/butterfly-valves/resilient-seated/series-3w-3l-resilient-seated-butterfly-valve) | oem-product-page | 2026-09-22 |

Modeled family features:
- thin circular wafer body
- disc sector
- central stem
- bolting ears
- quarter-turn lever
- flanged pipe context

Intentionally generic / not claimed:
- No Bray mark, size, seat, disc material, pressure, temperature, end standard, shutoff, flow characteristic, lockout, or handle-index specification is reproduced.
- Lever movement is symbolic and not a live process command.
