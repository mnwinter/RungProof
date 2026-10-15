# Twin-Tower PSA Nitrogen Generator help

Asset ID: `utilities.gas-generation.nitrogen-psa-twin-tower.v1`  
Catalog status: **candidate / candidate**  
Category: `utilities/gas-generation/nitrogen`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.4 m
- Height: 1.3 m
- Depth: 2.7 m
- Source: `res://assets/utilities/psa_nitrogen_generator/source/psa_nitrogen_generator.blend`
- Delivery: `res://assets/utilities/psa_nitrogen_generator/delivery/psa_nitrogen_generator.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `enable_command` | `bool` | `input` |  | Enable command. |
| `nitrogen_purity` | `float32` | `output` | percent | Nitrogen purity. |
| `active_tower` | `int32` | `output` |  | Active tower. |
| `generator_fault` | `bool` | `output` |  | Generator fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **twin-tower PSA nitrogen generator**.
Source-model review: **compared-pass** — Fresh blind render compared with Atlas Copco NGP PSA generators: two tall adsorber vessels, upper linking manifold, lower valve/piping treatment, central control cabinet, and buffer-tank treatment form a credible generic twin-tower PSA nitrogen generator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Atlas Copco: NGP PSA nitrogen generators](https://www.atlascopco.com/content/dam/atlas-copco/compressor-technique/industrial-air/documents/leaflets/gas-generators/industrial-gases/2935097745_L.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- two adsorber vessels
- upper manifold
- lower valves/piping
- central controller
- buffer-tank treatment

Intentionally generic / not claimed:
- No Atlas Copco mark, nitrogen purity, flow, pressure, CMS media, compressor supply, oxygen analyzer, valve cycle, venting, or hazardous-area rating is reproduced.
- The asset does not separate gas or produce nitrogen.
