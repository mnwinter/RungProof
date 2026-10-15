# Pneumatic Product Pusher - 1350 mm Stroke help

Asset ID: `actuation.pneumatic-pusher.1350mm.v1`  
Catalog status: **production / approved**  
Category: `actuation/pneumatic/linear`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.

## Physical envelope

- Width: 3.1 m
- Height: 1.1 m
- Depth: 0.72 m
- Source: `res://assets/scene_core/pneumatic_pusher/source/pneumatic_pusher.blend`
- Delivery: `res://assets/scene_core/pneumatic_pusher/delivery/pneumatic_pusher.glb`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `extend_command` | `bool` | `input` |  | Extend command. |
| `retract_command` | `bool` | `input` |  | Retract command. |
| `extended` | `bool` | `output` |  | Extended. |
| `retracted` | `bool` | `output` |  | Retracted. |

Direction is the reusable asset direction only. A scene binding must explicitly map it to a symbolic point and declare whether the point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics

| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `pusher_position` | linear | `KIN_pusher_plate` | 0 to 1.35 m | 0.65 |

## Connectors

No reusable connector contract is declared.

## Industrial reference basis

Generic reference family: **double-acting single-rod pneumatic cylinder used as a pusher**.
Source-model review: **compared-pass** — Fresh blind render reviewed against the SMC CS1 tie-rod-cylinder family: it now has a cylindrical large-bore barrel, distinct round end caps, four external tie rods with retaining nuts, port/fitting and tubing context, a single rod, and a mechanically guided pusher carriage. The carton, deck, and pusher label are generic function witnesses, not vendor identification or a pneumatic sizing claim.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [SMC: CS1 Large Bore Tie Rod Actuator product page](https://www.smcusa.com/products/pneumatic-actuators/linear-actuators/tie-rod/cs1~53642) | oem-product-page | 2026-09-22 |

Modeled family features:
- cylinder barrel
- end caps
- single extending rod
- mounting feet/clevis interface
- air-port zone

Intentionally generic / not claimed:
- No SMC mark, bore, stroke rating, pressure, cushioning, switch type, port thread, mounting load, or pneumatic circuit is reproduced.
- Pusher motion remains symbolic simulator kinematics.
