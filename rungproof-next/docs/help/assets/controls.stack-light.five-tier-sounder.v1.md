# Five-Tier Stack Light with Sounder help

Asset ID: `controls.stack-light.five-tier-sounder.v1`  
Catalog status: **candidate / candidate**  
Category: `controls/indication`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.6 m
- Height: 2.9 m
- Depth: 0.6 m
- Source: `res://assets/controls_sensors/five_tier_stacklight_siren/source/five_tier_stacklight_siren.blend`
- Delivery: `res://assets/controls_sensors/five_tier_stacklight_siren/delivery/five_tier_stacklight_siren.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `green_on` | `bool` | `input` |  | Green on. |
| `white_on` | `bool` | `input` |  | White on. |
| `amber_on` | `bool` | `input` |  | Amber on. |
| `blue_on` | `bool` | `input` |  | Blue on. |
| `red_on` | `bool` | `input` |  | Red on. |
| `sounder_on` | `bool` | `input` |  | Sounder on. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **five-tier industrial signal tower with audible sounder**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the WERMA RST 56 five-tier-with-buzzer family: a tubular base mount supports five separately bounded colored lens tiers and a larger dark top sounder housing. It reads as a generic five-tier stack light with sounder.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [WERMA Signaltechnik: Signal Towers overview](https://www.werma.com/media/9b/5d/79/1720011694/Signaltower_Overview_EN.pdf?ts=1772026681) | oem-datasheet | 2026-09-22 |

Modeled family features:
- five separate lens tiers
- inter-tier rings
- dark sounder housing
- tube mount
- base plate

Intentionally generic / not claimed:
- No WERMA mark, lens order, audible level, voltage, IP rating, mounting rating, or signaling standard is reproduced.
- Light and sound states are symbolic simulator output only, never real notification or alarm behavior.
