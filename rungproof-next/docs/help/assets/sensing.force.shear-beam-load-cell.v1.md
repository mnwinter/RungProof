# Shear-Beam Load Cell help

Asset ID: `sensing.force.shear-beam-load-cell.v1`  
Catalog status: **candidate / candidate**  
Category: `sensing/force`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.45 m
- Height: 0.55 m
- Depth: 0.55 m
- Source: `res://assets/controls_sensors/shear_beam_load_cell/source/shear_beam_load_cell.blend`
- Delivery: `res://assets/controls_sensors/shear_beam_load_cell/delivery/shear_beam_load_cell.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `force_n` | `float32` | `output` | N | Force n. |
| `overload` | `bool` | `output` |  | Overload. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **single-ended shear-beam industrial load cell**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Rice Lake single-ended shear-beam family: a low rectangular beam has a fixed-end mounting hole, threaded loading feature, machined relief region, cable loop, and capacity nameplate context. It reads as a generic shear-beam load cell, not a certified weighing assembly.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Rice Lake Weighing Systems: VPG Sensortronics 65023A single-ended shear-beam load cell](https://www.ricelake.com/products/vpg-sensortronics-65023a-alloy-steel-single-ended-beam-load-cell/?part=17159) | oem-product-page | 2026-09-22 |

Modeled family features:
- rectangular beam body
- fixed-end mount
- load feature
- machined relief
- cable treatment
- nameplate context

Intentionally generic / not claimed:
- No Rice Lake, VPG, or Sensortronics mark, capacity, calibration, cable, accuracy, legal-for-trade status, or load rating is reproduced.
- The object does not sense force or produce real/safety I/O.
