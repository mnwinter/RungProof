# Safety Light-Curtain Pair help

Asset ID: `training.accessory.safety_light_curtain_pair.v1`  
Catalog status: **candidate / candidate**  
Category: `training/accessories`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 2.1 m
- Depth: 0.45 m
- Source: `res://assets/training_accessories/safety_light_curtain_pair/source/safety_light_curtain_pair.blend`
- Delivery: `res://assets/training_accessories/safety_light_curtain_pair/delivery/safety_light_curtain_pair.glb`

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

Generic reference family: **approximately 1800 mm emitter/receiver safety light curtain pair**.
Source-model review: **reference-identified** — Generic family reference inherited for candidate package review; the training accessory must be independently compared before promotion.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Banner Engineering: EZ-SCREEN LS safety light curtain kit product page](https://www.bannerengineering.com/my/en/products/part.89619.html) | oem-product-page | 2026-09-22 |

Modeled family features:
- separate slim emitter and receiver rails
- long protected opening
- end mounting brackets
- M12/pigtail connector end

Intentionally generic / not claimed:
- No Banner mark, exact protected height, resolution, range, safety rating, connector pinout, or safety function is reproduced.
- The simulator has no claimed protective-field design or safety performance.
