# Road Barrier help

Asset ID: `training.accessory.road_barrier.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 5.100000202655792 m
- Height: 1.3600000143051147 m
- Depth: 0.6000000238418579 m
- Source: `res://assets/training_accessories/road_barrier/source/road_barrier.blend`
- Delivery: `res://assets/training_accessories/road_barrier/delivery/road_barrier.glb`

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
