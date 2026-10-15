# Coded Magnetic Safety Switch Pair help

Asset ID: `safety.interlock.coded-magnetic-pair.v1`  
Catalog status: **production / approved**  
Category: `safety/interlocks`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.95 m
- Depth: 0.5 m
- Source: `res://assets/controls_sensors/coded_magnetic_safety_switch/source/coded_magnetic_safety_switch.blend`
- Delivery: `res://assets/controls_sensors/coded_magnetic_safety_switch/delivery/coded_magnetic_safety_switch.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `guard_closed` | `bool` | `output` |  | Guard closed. |
| `safety_ok` | `bool` | `output` |  | Safety ok. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **coded magnetic non-contact guard-switch and actuator pair**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Pilz PSENmag family: matched compact dark sensor and actuator heads are separately mounted across a narrow moving-guard seam, with restrained molded end-cap treatment, one indicator, visible mounting fasteners, and a sensor-side M12/cable path. The guard posts are contextual mounting geometry; the model intentionally carries no vendor mark or safety-performance claim.

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
