# Wall-Mount Variable Frequency Drive help

Asset ID: `drives.vfd.wall-mount-keypad.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/drives/vfd`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.8 m
- Height: 0.5 m
- Depth: 1.3 m
- Source: `res://assets/electrical_controls/wall_vfd/source/wall_vfd.blend`
- Delivery: `res://assets/electrical_controls/wall_vfd/delivery/wall_vfd.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `speed_reference` | `float32` | `input` | Hz | Speed reference. |
| `output_frequency` | `float32` | `output` | Hz | Output frequency. |
| `drive_fault` | `bool` | `output` |  | Drive fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **wall-mounted low-voltage AC variable-frequency drive**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with ABB ACS580 wall-mounted drives: a tall wall-drive enclosure has a projecting keypad/display face, operator keys, lower power-terminal treatment, and a deep rear cooling heat sink. It reads as a generic wall-mounted variable-frequency drive.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB: ACS580 drives](https://www.abb.com/global/en/areas/motion/drives/low-voltage-ac-drives/general-purpose-drives/acs580) | oem-product-page | 2026-09-22 |

Modeled family features:
- tall drive enclosure
- keypad/display face
- operator buttons
- lower terminal treatment
- rear cooling heat sink

Intentionally generic / not claimed:
- No ABB mark, frame size, voltage, current, enclosure class, firmware, parameter set, motor data, braking, EMC option, or power rating is reproduced.
- The visual does not output power, command a motor, or expose a live-drive operating interface.
