# Level Transmitters help

Asset ID: `training.accessory.level_transmitters.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.8305 m
- Height: 3.605 m
- Depth: 0.64 m
- Source: `res://assets/training_accessories/level_transmitters/source/level_transmitters.blend`
- Delivery: `res://assets/training_accessories/level_transmitters/delivery/level_transmitters.glb`

## Expected reusable I/O

No reusable external signals are declared. Do not invent I/O for this passive asset.

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **guided-wave continuous level transmitter with analogue-output family**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ifm: Level sensors product guide](https://www.ifm.com/ifmweb/downcont.nsf/files/ifm%20UK%20LevelSensors/%24file/ifm%20UK%20LevelSensors.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- proportioned transmitter head
- neutral display or indicator context
- cable entry
- threaded or flanged process connection
- guided-wave rod

Intentionally generic / not claimed:
- No ifm mark, output scaling, process connection, rod length, material, pressure/temperature rating, IP rating, configuration, calibration, or installation instruction is reproduced.
- The asset exposes symbolic simulator I/O only; it is not a 4–20 mA loop, configured instrument, or live process measurement.
