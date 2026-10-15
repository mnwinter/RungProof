# Generic Compact Passenger Vehicle help

Asset ID: `loads.vehicle.compact-passenger.v1`  
Catalog status: **candidate / candidate**  
Category: `loads/vehicles`
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

- Width: 1.82 m
- Height: 1.76 m
- Depth: 4.35 m
- Source: `res://assets/scene_support/parking_vehicle/source/parking_vehicle.blend`
- Delivery: `res://assets/scene_support/parking_vehicle/delivery/parking_vehicle.glb`
- Collision: `res://assets/scene_support/parking_vehicle/collision/parking_vehicle_collision.glb`
- Thumbnail: `res://assets/scene_support/parking_vehicle/thumbnail.png`

## Expected reusable I/O

| Signal | Type | Direction | Unit | Description |
| --- | --- | --- | --- | --- |
| `vehicle_present` | `bool` | `output` |  | Vehicle present. |

Direction is the reusable asset direction only. An `input` is a command or demand supplied to the asset model; an `output` is feedback produced by the asset model. A scene binding must explicitly map each signal to a symbolic point and declare whether that point is PC-, PLC-, or simulator-owned. The signal list is the complete expected I/O for this catalog revision; an asset with no listed signals is passive and must not be assigned invented I/O.

## Kinematics


### Tag-by-tag use

| Tag | Use in logic | Integration note |
| --- | --- | --- |
| `vehicle_present` | Feedback returned by the asset model. | Consume as simulator feedback; do not treat it as proof of a wired field device. |

## Tag mapping checklist

- Preserve the catalog signal name when creating a scene binding so the asset contract remains traceable.
- Record the point owner explicitly: PC points are simulator feedback, PLC points are controller commands, and SIM points are internal model state.
- Keep units and data types unchanged at the asset boundary. Any scaling, inversion, debounce, timeout, or alarm policy belongs in the scene/controller contract and must be documented there.
- Do not infer a safety input, permissive, E-stop, interlock, or certified diagnostic from a similarly named tag.
No animated axis is declared; the asset is static unless a future catalog revision adds a kinematic contract.

### Animation tags

No animation tags are declared; this asset is static in the reusable contract.

### Motion use and limits

No motion controls apply to this asset.

## Connectors

No reusable connector contract is declared.

## Admission status

This is a candidate asset. It is not approved for production use until its visual evidence, independent recognition review, and catalog quality gates pass.

## Verification record

- Topology reviewed: `True`
- Materials reviewed: `True`
- Scale reviewed: `True`
- Animation evidence recorded: `True`
- Blind recognition review: `blind-2026-09-30-parking-round3-image-126`
- Recognition confidence: `0.86`
- Evidence basis: `not recorded`

These records document catalog/evidence checks. They do not certify the asset for a real machine, real PLC, electrical safety circuit, guarding, or production commissioning.

## Industrial reference basis

Generic reference family: **compact four-door passenger car**.
Source-model review: **compared-pass** — The rendered candidate uses the generic compact four-door passenger-car envelope: wheelbase, four road wheels, separated hood/trunk, greenhouse glazing, lamps, and a front plate. It is intentionally a scene load rather than a claimed exact vehicle model.

| OEM source | Type | Accessed |
| --- | --- | --- |
| [Toyota: 2026 Corolla features and performance](https://www.toyota.com/corolla/2026/section/features_performance/) | oem-product-page | 2026-09-30 |

Modeled family features:
- compact sedan proportions
- four road wheels and hubs
- hood and trunk volumes
- glazed greenhouse
- front/rear lamp treatment
- front plate treatment

Intentionally generic / not claimed:
- No Toyota mark, model identity, dimensions, trim, powertrain, safety rating, roadworthiness, or vehicle behavior is reproduced.
- No usable OEM CAD model was located during this pass; the source basis is explicitly the OEM exterior/product documentation, not a claimed exact vehicle CAD reconstruction.
- The asset is a visual parking-scene load and is not a drivable or collision-qualified vehicle model.
