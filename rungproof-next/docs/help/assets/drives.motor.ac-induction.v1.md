# AC Induction Motor help

Asset ID: `drives.motor.ac-induction.v1`  
Catalog status: **production / approved**  
Category: `drives/motors`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 1.8 m
- Height: 1.1 m
- Depth: 0.9 m
- Source: `res://assets/scene_core/ac_induction_motor/source/ac_induction_motor.blend`
- Delivery: `res://assets/scene_core/ac_induction_motor/delivery/ac_induction_motor.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `run_command` | `bool` | `input` |  | Run command. |
| `running` | `bool` | `output` |  | Running. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `motor_rotation` | rotary_continuous | `KIN_motor_shaft` | 0 to 3600 rpm | 1450 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **foot-mounted IEC low-voltage induction motor**.
Source-model review: **compared-pass** — Rendered review shows the expected ribbed frame, end shields, shaft, terminal box, lifting eye, and foot mounts. It remains intentionally generic rather than a dimensioned ABB configuration.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [ABB: IEC low-voltage motor catalogue](https://library.e.abb.com/public/eba1819b4636419a93d38e67b44d94d2/Master%20-IEC%20Stock%20Motor%20offering_cover.pdf) | oem-datasheet | 2026-09-22 |

Modeled family features:
- ribbed cylindrical frame
- terminal box
- drive-end shaft
- end shields
- foot mounting

Intentionally generic / not claimed:
- No ABB mark, frame size, power, voltage, efficiency class, mounting code, protection rating, or wiring is reproduced.
- The asset does not imply an energized motor or real drive behavior.
