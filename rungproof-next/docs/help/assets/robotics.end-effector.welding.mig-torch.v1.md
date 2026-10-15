# Robotic MIG Welding Torch help

Asset ID: `robotics.end-effector.welding.mig-torch.v1`  
Catalog status: **candidate / candidate**  
Category: `robotics/end-effectors/welding`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.7 m
- Height: 1.2 m
- Depth: 1.3 m
- Source: `res://assets/robotics/robotic_mig_torch/source/robotic_mig_torch.blend`
- Delivery: `res://assets/robotics/robotic_mig_torch/delivery/robotic_mig_torch.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `ready` | `bool` | `output` |  | Ready status. |
| `fault` | `bool` | `output` |  | Fault status. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **robotic MIG/MAG welding torch**.
Source-model review: **compared-pass** — Rendered review compared with ABICOR BINZEL robotic MIG/MAG torch systems: a robot mount carries a collision-mount treatment, torch body, curved neck, gas nozzle, contact-tip treatment, and cable. It reads as a generic robotic MIG torch.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABICOR BINZEL: ROBO Compact W600 MIG/MAG welding torch system](https://www.binzel-abicor.com/uploads/Content/Germany/PDF-Files/PDF_Files_Download/Catalogues/ROBO/PRO_R143_EN_ROBO_WEB.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- robot mount
- collision-mount treatment
- torch body
- curved neck
- gas nozzle
- contact-tip treatment
- cable

Intentionally generic / not claimed:
- No ABICOR BINZEL mark, current, duty cycle, wire, gas, cooling, collision response, consumables, or certification is reproduced.
- The visual does not feed wire, supply shielding gas/current, create an arc, or establish welding safety.
