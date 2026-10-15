# Industrial Welded-Body Hydraulic Cylinder help

Asset ID: `actuation.hydraulic.cylinder.welded-body.v1`  
Catalog status: **candidate / candidate**  
Category: `actuation/hydraulic/cylinders`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.6 m
- Height: 1.3 m
- Depth: 1.2 m
- Source: `res://assets/mechanical_motion/hydraulic_welded_body_cylinder/source/hydraulic_welded_body_cylinder.blend`
- Delivery: `res://assets/mechanical_motion/hydraulic_welded_body_cylinder/delivery/hydraulic_welded_body_cylinder.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `extend_command` | `bool` | `input` |  | Extend command. |
| `retract_command` | `bool` | `input` |  | Retract command. |
| `position_m` | `float32` | `output` | m | Position m. |
| `pressure_bar` | `float32` | `output` | bar | Pressure bar. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `rod_travel` | linear | `KIN_CHROME_ROD` | 0 to 0.7 m | 0.45 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **industrial welded-body hydraulic rod cylinder**.
Source-model review: **compared-pass** — Fresh blind render reviewed against Parker welded-rod-cylinder form: a continuous cylindrical barrel has a rod/gland end, extended rod, clevis eye, welded-looking cap, port tubes, and a rear mounting interface. It reads as a generic welded-body hydraulic cylinder.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Parker Hannifin: HY18-1000 welded rod cylinders catalog](https://www.parker.com/content/dam/Parker-com/Literature/Industrial-Cylinder/cylinder/cat/english/Parker_Mobile_Cylinder_Products_Catalog_HY18-1000.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- cylindrical barrel
- gland
- piston rod
- clevis eye
- welded-looking end cap
- hydraulic ports
- rear mount

Intentionally generic / not claimed:
- No Parker mark, bore, stroke, pressure, seal, port thread, mount rating, fluid, or load is reproduced.
- Cylinder geometry is not a hydraulic circuit, pressure-boundary, or physical-motion claim.
