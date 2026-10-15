# Centrifugal Pump and Motor Skid help

Asset ID: `process.pump.centrifugal-skid.v1`  
Catalog status: **production / approved**  
Category: `process/pumps`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.2 m
- Height: 1.5 m
- Depth: 1.2 m
- Source: `res://assets/scene_core/centrifugal_pump_skid/source/centrifugal_pump_skid.blend`
- Delivery: `res://assets/scene_core/centrifugal_pump_skid/delivery/centrifugal_pump_skid.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `running` | `bool` | `output` |  | Running. |
| `flow_estimate` | `float32` | `output` |  | Flow estimate. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pump_rotation` | rotary_continuous | `KIN_pump_shaft` | 0 to 3600 rpm | 720 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **horizontal end-suction centrifugal pump and motor on baseplate**.
Source-model review: **compared-pass** — Rendered review shows a foot-mounted horizontal end-suction pump with a volute casing, axial suction flange, top discharge flange, bearing/drive-side housing, coupled electric motor, coupling guard context, and common baseplate. It reads as a generic centrifugal pump skid family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Flowserve: Durco Mark 3 Sealed ASME/ANSI Chemical Process Pump](https://www.flowserve.com/products/products-catalog/pumps/overhung-pumps/chemical-process-pumps-ansi-iso/durco-mark-3-sealed-asme-ansi-chemical-process-pump/) | oem-product-page | 2026-09-22 |

Modeled family features:
- volute casing
- axial suction flange
- top discharge flange
- bearing/drive housing
- electric motor
- coupling-guard context
- common baseplate

Intentionally generic / not claimed:
- No Flowserve/Durco mark, pump curve, material, seal, impeller, motor, coupling, hazardous-area rating, vibration limit, piping load, or process-duty claim is reproduced.
- The asset has no operational pumping or process-safety behavior claim.
