# Industrial Axial Exhaust Fan help

Asset ID: `air-handling.fan.axial-1900.v1`  
Catalog status: **production / approved**  
Category: `air-handling/fans`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 2.2 m
- Height: 2.4 m
- Depth: 1.3 m
- Source: `res://assets/scene_core/axial_exhaust_fan/source/axial_exhaust_fan.blend`
- Delivery: `res://assets/scene_core/axial_exhaust_fan/delivery/axial_exhaust_fan.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `running` | `bool` | `output` |  | Running. |
| `speed_percent` | `float32` | `input` |  | Speed percent. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `fan_rotation` | rotary_continuous | `KIN_fan_hub` | 0 to 1800 rpm | 720 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **industrial axial propeller exhaust fan**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the Greenheck AX family: the asset presents a short cylindrical shroud, central propeller hub, rear motor, structural motor supports, a retained concentric grille with stand-offs, and a bolted base/frame. It is a generic axial exhaust-fan representation, not a Greenheck reproduction or an airflow, guarding, or ventilation-performance claim.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Greenheck: Model AX axial fan catalog](https://webcontent.greenheck.com/atg-cms-prod/docs/default-source/pdf-downloads/catalogs/ax_catalog.pdf?sfvrsn=9bd001ca_7) | oem-datasheet | 2026-09-22 |

Modeled family features:
- cylindrical/short axial housing
- central hub and propeller
- guard grille
- mounting base or wall interface
- motor/drive support

Intentionally generic / not claimed:
- No Greenheck mark, airflow, pressure, motor, blade diameter, guard rating, noise, or ventilation performance is reproduced.
- The asset has no real air-handling behavior claim.
