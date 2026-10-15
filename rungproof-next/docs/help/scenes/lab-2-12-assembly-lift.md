# Lab 2.12 - Ergonomic Assembly Lift help

Scene ID: `lab-2-12-assembly-lift`  
Migrated source: `prototype/scenes/lab-2-12-assembly-lift.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-12-assembly-lift.scene.json`

## Purpose

Independent raise and lower commands move a scissor lift between modeled bottom and top limits.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `lift_up` | `BOOL` | **PLC** | `False` |
| `lift_down` | `BOOL` | **PLC** | `False` |
| `top_limit` | `BOOL` | **PC** | `False` |
| `bottom_limit` | `BOOL` | **PC** | `True` |
| `lift_position` | `REAL` | **SIM** | `0` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Raise to work height` | `start` | `raise` |
| `Lower to load height` | `start` | `lower` |
| `Stop lift` | `stop` | `` |
| `Reset lift` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `top_limit` | `top_limit_lamp` | `indicator` |
| `bottom_limit` | `bottom_limit_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `assembly_lift` | `liftTable` | Ergonomic assembly lift |
| `lift_fixture` | `box` | Assembly fixture |
| `raise_button` | `switch` | Raise lift |
| `lower_button` | `switch` | Lower lift |
| `top_limit_lamp` | `indicator` | Top limit |
| `bottom_limit_lamp` | `indicator` | Bottom limit |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `lift_up` | `False` |
| `lift_down` | `False` |

## Machine guide

Independent raise and lower commands move a scissor lift between modeled bottom and top limits.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command lift_up, lift_down.

### Normal sequence

- raising
- at top

### Expected observations

- Raise stops at the top limit; lower stops at the bottom limit; opposed outputs are never on together.
