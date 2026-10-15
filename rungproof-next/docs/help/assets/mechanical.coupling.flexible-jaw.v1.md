# Flexible Jaw Coupling with Elastomer Spider help

Asset ID: `mechanical.coupling.flexible-jaw.v1`  
Catalog status: **candidate / candidate**  
Category: `mechanical/couplings`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.6 m
- Height: 0.68 m
- Depth: 0.72 m
- Source: `res://assets/mechanical_motion/flexible_jaw_coupling/source/flexible_jaw_coupling.blend`
- Delivery: `res://assets/mechanical_motion/flexible_jaw_coupling/delivery/flexible_jaw_coupling.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `input_speed_rpm` | `float32` | `input` | rpm | Input speed rpm. |
| `output_speed_rpm` | `float32` | `output` | rpm | Output speed rpm. |
| `slip_detected` | `bool` | `output` |  | Slip detected. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `input_rotation` | angular_continuous | `KIN_HUB_INPUT` | -3000 to 3000 rpm | 6000 |
| `output_rotation` | angular_continuous | `KIN_HUB_OUTPUT` | -3000 to 3000 rpm | 6000 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **three-piece elastomer-spider jaw coupling**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Lovejoy L-type jaw-coupling family: two round hubs meet around a visible contrasting elastomer spider, with projecting shafts and repeated interlocking jaw treatment. It reads as a generic flexible jaw coupling.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Lovejoy: L Type standard jaw coupling](https://www.lovejoy-inc.com/products/jaw-type-couplings/l-type-standard-jaw-coupling/) | oem-product-page | 2026-09-22 |

Modeled family features:
- two hubs
- contrasting elastomer spider
- interlocking jaw treatment
- projecting shafts

Intentionally generic / not claimed:
- No Lovejoy mark, coupling size, bore, keyway, torque, speed, spider compound, misalignment allowance, guard, or installation specification is reproduced.
- The asset conveys neither real torque transmission nor a rotating-equipment safety boundary.
