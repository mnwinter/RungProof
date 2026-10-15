# Enclosed Laser Marking Station help

Asset ID: `machines.marking.laser.enclosed-class1.v1`  
Catalog status: **candidate / candidate**  
Category: `machines/marking/laser`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.9 m
- Height: 2.2 m
- Depth: 1.7 m
- Source: `res://assets/production-machines/enclosed_laser_marking_station/source/enclosed_laser_marking_station.blend`
- Delivery: `res://assets/production-machines/enclosed_laser_marking_station/delivery/enclosed_laser_marking_station.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `mark_start` | `bool` | `input` |  | Mark start. |
| `door_open` | `bool` | `input` |  | Door open. |
| `focus_position` | `float32` | `input` | mm | Focus position. |
| `door_closed` | `bool` | `output` |  | Door closed. |
| `laser_ready` | `bool` | `output` |  | Laser ready. |
| `mark_complete` | `bool` | `output` |  | Mark complete. |
| `laser_fault` | `bool` | `output` |  | Laser fault. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `door_position` | linear | `KIN_SAFETY_DOOR` | 0 to 1.1 m | 0.35 |
| `head_z` | linear | `KIN_LASER_HEAD` | 0 to 0.45 m | 0.12 |

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Industrial reference basis

Generic reference family: **enclosed laser marking station**.
Source-model review: **compared-pass** — Fresh blind render compared with TRUMPF TruMark Station 3000 systems: a sealed enclosure has a front protective viewing panel, interlocked access treatment, status tower, and operator console. It reads as a generic enclosed laser-marking-station family.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [TRUMPF: TruMark Station 3000](https://www.trumpf.com/en_GB/products/machines-systems/laser-marking-systems/trumark-station-3000/) | oem-product-page | 2026-09-22 |

Modeled family features:
- sealed enclosure
- front protective viewing panel
- access-door treatment
- status tower
- operator console

Intentionally generic / not claimed:
- No TRUMPF mark, laser source, wavelength, power, class, material process, extraction, interlock performance, enclosure rating, or certification is reproduced.
- The visual emits no laser and does not make a laser-safety or marking-process claim.
