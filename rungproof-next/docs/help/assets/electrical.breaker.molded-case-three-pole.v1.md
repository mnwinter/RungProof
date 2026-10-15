# Three-Pole Molded-Case Circuit Breaker help

Asset ID: `electrical.breaker.molded-case-three-pole.v1`  
Catalog status: **candidate / candidate**  
Category: `electrical/protection/breakers`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 0.75 m
- Height: 0.5 m
- Depth: 1.15 m
- Source: `res://assets/electrical_controls/molded_case_breaker/source/molded_case_breaker.blend`
- Delivery: `res://assets/electrical_controls/molded_case_breaker/delivery/molded_case_breaker.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `close_command` | `bool` | `input` |  | Close command. |
| `closed` | `bool` | `output` |  | Closed. |
| `tripped` | `bool` | `output` |  | Tripped. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `handle_angle` | angular | `KIN_BREAKER_HANDLE` | 0 to 45 deg | 180 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-pole molded-case circuit breaker**.
Source-model review: **compared-pass** — Fresh front three-quarter review was compared with Schneider Electric ComPact NSX MCCBs: a molded case presents three separated line/load lug treatments, a centered projecting trip handle, and a face-mounted MCCB rating-label region. It reads as a generic three-pole molded-case circuit breaker.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Schneider Electric: ComPact NSX molded-case circuit breakers](https://www.se.com/in/en/product-range/1887-compact-nsx-630a/) | oem-product-page | 2026-09-22 |

Modeled family features:
- molded case
- three line lugs
- three load lugs
- center trip handle
- rating-label region

Intentionally generic / not claimed:
- No Schneider Electric mark, frame size, interrupting rating, trip unit, current setting, voltage, terminal conductor range, accessory, coordination, fault protection, or certification is reproduced.
- The simulated handle is not a live breaker operation and does not energize, protect, or disconnect a circuit.
