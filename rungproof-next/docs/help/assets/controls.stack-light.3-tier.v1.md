# Three-Tier Stack Light with Buzzer help

Asset ID: `controls.stack-light.3-tier.v1`  
Catalog status: **production / approved**  
Category: `controls/indication/stack-lights`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.42 m
- Height: 1.82 m
- Depth: 0.38 m
- Source: `res://assets/scene_core/stack_light_3_tier/source/stack_light_3_tier.blend`
- Delivery: `res://assets/scene_core/stack_light_3_tier/delivery/stack_light_3_tier.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `red_on` | `bool` | `input` |  | Red lens command. |
| `amber_on` | `bool` | `input` |  | Amber lens command. |
| `green_on` | `bool` | `input` |  | Green lens command. |
| `buzzer_on` | `bool` | `input` |  | Audible annunciator command. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **three-tier pole-mounted signal tower with buzzer option**.
Source-model review: **compared-pass** — Rendered review shows three stacked cylindrical red, amber, and green lens modules on a pole-mounted base with a top sounder-cap treatment. It reads as a generic three-tier visual/audible signal-tower family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [PATLITE: LR6-302QJBW-RYG 60 mm 3-tier signal tower](https://shop.patlite.com/60mm-3-Tier-Signal-Tower-p/lr6-302qjbw-ryg.htm) | oem-product-page | 2026-09-22 |

Modeled family features:
- three stacked lens modules
- red/amber/green visual order
- top audible-cap context
- pole mount
- anchored base

Intentionally generic / not claimed:
- No PATLITE mark, luminous output, sound level, voltage, ingress rating, communication interface, wiring, or annunciation standard is reproduced.
- Rendered light state is simulator-only visual feedback.
