# Luggage Load help

Asset ID: `training.accessory.luggage_load.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.7200000286102295 m
- Height: 0.6024999618530273 m
- Depth: 0.5464999675750732 m
- Source: `res://assets/training_accessories/luggage_load/source/luggage_load.blend`
- Delivery: `res://assets/training_accessories/luggage_load/delivery/luggage_load.glb`

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

No OEM-family reference record is registered yet. This asset is in source-provenance remediation and must not be represented as an exact real-world component.
