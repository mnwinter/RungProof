# Coded Safety Gate Switch help

Asset ID: `training.accessory.coded_safety_gate_switch.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.95 m
- Depth: 0.5 m
- Source: `res://assets/training_accessories/coded_safety_gate_switch/source/coded_safety_gate_switch.blend`
- Delivery: `res://assets/training_accessories/coded_safety_gate_switch/delivery/coded_safety_gate_switch.glb`

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

Generic reference family: **coded magnetic non-contact guard-switch and actuator pair**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Pilz: PSENmag non-contact magnetic safety switches](https://www.pilz.com/en-TH/products/sensor-technology/safety-switches/psenmag-non-contact-magnetic-safety-switches) | oem-product-page | 2026-09-22 |

Modeled family features:
- separate sensor and actuator
- guard-frame mounting context
- non-contact approach gap
- indicator context
- connector/cable treatment

Intentionally generic / not claimed:
- No Pilz mark, switching distance, coding level, contact configuration, diagnostic coverage, safety rating, wiring, or controller compatibility is reproduced.
- This model is not a functioning safety switch or guard-monitoring claim.
