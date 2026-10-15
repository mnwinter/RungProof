# Pedestal Resistance Spot Welder help

Asset ID: `machines.welding.resistance.pedestal-spot.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/welding/resistance`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.4 m
- Depth: 1.6 m
- Source: `res://assets/production-machines/resistance_spot_welder/source/resistance_spot_welder.blend`
- Delivery: `res://assets/production-machines/resistance_spot_welder/delivery/resistance_spot_welder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `weld_trigger` | `bool` | `input` |  | Weld trigger. |
| `electrode_close` | `bool` | `input` |  | Electrode close. |
| `weld_current` | `float32` | `output` | kA | Weld current. |
| `weld_complete` | `bool` | `output` |  | Weld complete. |
| `welder_fault` | `bool` | `output` |  | Welder fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `electrode_position` | linear | `KIN_UPPER_ELECTRODE` | 0 to 0.32 m | 0.18 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **pedestal resistance spot welder**.
Source-model review: **compared-pass** — Fresh blind render compared with industrial pedestal resistance welders: a tall pedestal supports opposed electrode arms across a work throat, with controller and pedal treatments. It reads as a generic pedestal spot-welder family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Lincoln Electric: Resistance welding automation](https://www.lincolnelectric.com/en/products/automation/resistance-welding) | oem-product-page | 2026-09-22 |

Modeled family features:
- pedestal
- opposed electrode arms
- work throat
- controller treatment
- pedal treatment

Intentionally generic / not claimed:
- No Lincoln mark, weld current, force, throat depth, transformer, water cooling, control schedule, guarding, or certification is reproduced.
- The visual does not deliver electrical current, weld material, or validate safeguarding.
