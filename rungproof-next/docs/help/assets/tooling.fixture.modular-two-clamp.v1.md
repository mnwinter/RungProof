# Two-Clamp Modular Assembly Fixture help

Asset ID: `tooling.fixture.modular-two-clamp.v1`  
Catalog status: **candidate / candidate**  
Category: `tooling/fixtures`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.44 m
- Height: 0.62 m
- Depth: 0.96 m
- Source: `res://assets/scene_loads/modular_assembly_fixture/source/modular_assembly_fixture.blend`
- Delivery: `res://assets/scene_loads/modular_assembly_fixture/delivery/modular_assembly_fixture.glb`

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

Generic reference family: **modular fixture plate with toggle clamps**.
Source-model review: **compared-pass** — Rendered review compared with Jergens modular workholding: a fixture plate carries locating pins, two over-center toggle-clamp assemblies, clamp pads, and mounting-hole treatments. It reads as a generic two-clamp modular fixture.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Jergens: Modular workholding](https://www.jergensinc.com/en/categories/modular-workholding) | oem-product-page | 2026-09-22 |

Modeled family features:
- fixture plate
- locating pins
- two toggle clamps
- clamp pads
- mounting holes

Intentionally generic / not claimed:
- No Jergens mark, plate grid, clamp force, workpiece geometry, machining load, mounting torque, or safety approval is reproduced.
- The visual does not hold a production workpiece or validate machining setup.
