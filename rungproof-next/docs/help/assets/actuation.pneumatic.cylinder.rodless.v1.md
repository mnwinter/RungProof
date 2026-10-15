# Rodless Pneumatic Cylinder help

Asset ID: `actuation.pneumatic.cylinder.rodless.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/pneumatic/cylinders`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.0 m
- Height: 0.62 m
- Depth: 0.8 m
- Source: `res://assets/mechanical_motion/rodless_pneumatic_cylinder/source/rodless_pneumatic_cylinder.blend`
- Delivery: `res://assets/mechanical_motion/rodless_pneumatic_cylinder/delivery/rodless_pneumatic_cylinder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `position_command_m` | `float32` | `input` | m | Position command m. |
| `position_m` | `float32` | `output` | m | Position m. |
| `in_position` | `bool` | `output` |  | In position. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `carriage_travel` | linear | `KIN_CARRIAGE` | -0.65 to 0.65 m | 1.2 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **mechanically jointed rodless pneumatic cylinder with carriage**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SMC MY1 mechanically jointed rodless-cylinder family: the elongated cylinder body, end caps, exposed seal-band/guide region, top carriage, and mounting feet read as a compact long-stroke rodless actuator.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: MY1M rodless cylinder with slide bearing](https://www.smcusa.com/products/pneumatic-actuators/rodless-cylinders/rodless-cylinder-mechanically-jointed/rodless-cylinder-mechanically-jointed-slide-bearing~159061) | oem-product-page | 2026-09-22 |

Modeled family features:
- long cylinder body
- end caps
- carriage
- seal/guide region
- mounting feet

Intentionally generic / not claimed:
- No SMC mark, bore, stroke, guide type, seal type, load rating, cushion, ports, or pressure specification is reproduced.
- The asset does not create or control physical pneumatic motion.
