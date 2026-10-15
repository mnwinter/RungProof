# Three-Pole Contactor and Overload Starter help

Asset ID: `electrical.starter.contactor-overload-three-pole.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/motor-control/starters`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.65 m
- Height: 0.5 m
- Depth: 1.1 m
- Source: `res://assets/electrical_controls/contactor_overload_starter/source/contactor_overload_starter.blend`
- Delivery: `res://assets/electrical_controls/contactor_overload_starter/delivery/contactor_overload_starter.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `coil_command` | `bool` | `input` |  | Coil command. |
| `auxiliary_closed` | `bool` | `output` |  | Auxiliary closed. |
| `overload_tripped` | `bool` | `output` |  | Overload tripped. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-pole contactor and thermal-overload motor starter**.
Source-model review: **compared-pass** — Fresh blind render compared with ABB AF contactor and overload-relay assemblies: a stacked contactor and overload body shows three upper line lugs, three lower load lugs, reset and adjustment treatments, and a clear motor-starter face. It reads as a generic three-pole contactor-overload starter.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB: AF three-pole contactors and overload relays](https://new.abb.com/low-voltage/products/motor-protection/3-pole-contactors-and-overload-relays-for-motor-starting/afcontactors) | oem-product-page | 2026-09-22 |

Modeled family features:
- stacked contactor/overload body
- three line lugs
- three load lugs
- reset treatment
- adjustment treatment
- motor-starter face

Intentionally generic / not claimed:
- No ABB mark, coil voltage, contact rating, overload class/range, auxiliary contacts, motor rating, coordination type, enclosure, or wiring is reproduced.
- The visual does not energize a coil, switch motor power, or provide overload protection.
