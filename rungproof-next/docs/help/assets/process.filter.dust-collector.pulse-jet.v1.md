# Pulse-Jet Dust Collector help

Asset ID: `process.filter.dust-collector.pulse-jet.v1`  
Catalog status: **candidate / candidate**  
Category: `process/separation/dust`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.6 m
- Height: 1.5 m
- Depth: 3.3 m
- Source: `res://assets/process_fluid/pulse_jet_dust_collector/source/pulse_jet_dust_collector.blend`
- Delivery: `res://assets/process_fluid/pulse_jet_dust_collector/delivery/pulse_jet_dust_collector.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `fan_run_command` | `bool` | `input` |  | Fan run command. |
| `pulse_enable` | `bool` | `input` |  | Pulse enable. |
| `differential_pressure` | `float32` | `output` | Pa | Differential pressure. |
| `hopper_level_high` | `bool` | `output` |  | Hopper level high. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

| ID | Type | Orientation |
| --- | --- | --- |
| `process_1` |  |  |
| `process_2` |  |  |
| `process_3` |  |  |

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pulse-jet baghouse dust collector with hopper discharge**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Donaldson FT pulse-jet baghouse form: a tall rectangular collector housing has an upper clean-air plenum, external compressed-air header/pulse-valve treatment, lower hopper, rotary-airlock-like discharge, inlet/outlet nozzles, and legged support. It reads as a generic pulse-jet dust collector.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Donaldson Torit: FT pulse jet baghouse dust collectors](https://www.donaldson.com/content/dam/donaldson/dust-fume-mist/literature/north-america/equipment/dust-collectors/baghouse/ft-pulse/f118146/FT-Pulse-Jet-Baghouse-Collector.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- collector housing
- upper plenum
- compressed-air header
- pulse-valve treatment
- hopper
- airlock-like discharge
- inlet/outlet nozzles
- leg supports

Intentionally generic / not claimed:
- No Donaldson mark, filter media, air-to-cloth ratio, flow, dust type, pulse pressure, explosion protection, fan, emissions performance, hopper capacity, or installation specification is reproduced.
- The simulator neither cleans filters nor manages airborne particulate.
