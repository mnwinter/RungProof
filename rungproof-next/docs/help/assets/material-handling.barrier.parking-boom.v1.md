# Parking Barrier Boom help

Asset ID: `material-handling.barrier.parking-boom.v1`  
Catalog status: **candidate / candidate**  
Category: `material-handling/barriers`
Training requirement: `not training-specific`
Generic basis asset: `not declared`

## Purpose and integration boundary

This document is generated from the reusable asset catalog. It defines only the symbolic asset contract; it does not assign PLC addresses, DB offsets, safety ratings, or live commissioning behavior. Scene-level bindings determine PC/PLC/SIM ownership.
Copyright boundary: Use only the declared generic family boundary.

## How to use this asset

1. Add the delivery model to a scene and position it using the documented physical envelope.
2. Bind only the declared signal names. Treat each name as a symbolic tag; assign actual PLC addresses, DB members, or HMI aliases only in the scene/integration contract.
3. Initialize command inputs to a known inactive state before enabling the scene. Read output signals as simulator feedback, not as proof of a real sensor or actuator.
4. If the asset declares kinematics, drive motion through its declared command signals and observe the declared feedback signals and axis limits.
5. Use the collision resource for interaction and placement checks. Do not use the visual mesh as a substitute for a safety envelope or certified guarding model.

## Physical envelope

- Width: 1.05 m
- Height: 3.6 m
- Depth: 1.85 m
- Source: `res://assets/scene_support/parking_barrier/source/parking_barrier.blend`
- Delivery: `res://assets/scene_support/parking_barrier/delivery/parking_barrier.glb`
- Collision: `res://assets/scene_support/parking_barrier/collision/parking_barrier_collision.glb`
- Thumbnail: `res://assets/scene_support/parking_barrier/thumbnail.png`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `barrier_raise` | `bool` | `input` |  | Barrier raise. |
| `barrier_open` | `bool` | `output` |  | Barrier open. |

Direction is the reusable asset direction only. An `input` is a command or demand supplied to the asset model; an `output` is feedback produced by the asset model. A scene binding must explicitly map each signal to a symbolic point and declare whether that point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics


### Tag-by-tag use

| Tag | Use in logic | Integration note |
| --- | --- | --- |
| `barrier_raise` | Command/demand sent to the asset model. | Drive from the scene or controller contract; do not assign a physical output address here. |
| `barrier_open` | Feedback returned by the asset model. | Consume as simulator feedback; do not treat it as proof of a wired field device. |

## Tag mapping checklist

- Preserve the catalog signal name when creating a scene binding so the asset contract remains traceable.
- Record the point owner explicitly: PC points are simulator feedback, PLC points are controller commands, and SIM points are internal model state.
- Keep units and data types unchanged at the asset boundary. Any scaling, inversion, debounce, timeout, or alarm policy belongs in the scene/controller contract and must be documented there.
- Do not infer a safety input, permissive, E-stop, interlock, or certified diagnostic from a similarly named tag.
| ID | Kind | Node | Range | Maximum rate |
| --- | --- | --- | --- | --- |
| `barrier_boom` | rotary | `BARRIER_BOOM` | 0 to 90 deg | 20 |

### Animation tags

No animation tags are declared; this asset is static in the reusable contract.

### Motion use and limits

Use only the declared axis ID and exported node path. The range and maximum rate are the simulator contract for this catalog revision; they are not a physical actuator stroke, pressure rating, acceleration limit, or commissioning value.
Motion is safe-model behavior only. Verify commanded direction, end-stop behavior, and feedback transitions in the scene before using the asset in a training sequence.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Verification record

- Topology reviewed: `True`
- Materials reviewed: `True`
- Scale reviewed: `True`
- Animation evidence recorded: `True`
- Blind recognition review: `blind-2026-09-30-parking-round1-image-127`
- Recognition confidence: `0.99`
- Evidence basis: `not recorded`

These records document catalog/evidence checks. They do not certify the asset for a real machine, real PLC, electrical safety circuit, guarding, or production commissioning.

## Industrial reference basis

Generic reference family: **automatic parking access barrier with boom arm**.
Source-model review: **compared-pass** — The rendered candidate uses the documented automatic parking-barrier family form: anchored cabinet, pivot housing, horizontal boom, warning stripes, and status lamp. The boom is authored as a simulator kinematic member.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [FAAC: FAAC automatic barriers](https://www.faac.biz/products/automatic-barriers/faac-barriers) | oem-product-page | 2026-09-30 |
| [FAAC: B680H installation and barrier body drawings](https://faac.blob.core.windows.net/web/1/root/b680h-732719-rev-g-en.pdf) | oem-manual | 2026-09-30 |

Modeled family features:
- anchored barrier cabinet
- pivot housing
- horizontal boom arm
- high-visibility warning treatment
- status lamp

Intentionally generic / not claimed:
- No FAAC mark, boom length, opening time, duty cycle, motor, controls, safety sensing, weather rating, or access-control performance is reproduced.
- The kinematic contract is symbolic simulator behavior only and is not a safety or traffic-control design.
