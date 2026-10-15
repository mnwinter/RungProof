# Lab 2.6 - Ready / Attention Button help

Scene ID: `lab-2-06-ready-attention`  
Migrated source: `prototype/scenes/lab-2-06-ready-attention.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-06-ready-attention.scene.json`

## Purpose

A spring-return request button transfers indication between a white ready lamp and an amber attention lamp.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `request_held` | `BOOL` | **PC** | `False` |
| `ready_light` | `BOOL` | **PLC** | `True` |
| `attention_light` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press / release request` | `toggle` | `request_held` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `request_held` | `request_button` | `switch` |
| `ready_light` | `ready_lamp` | `indicator` |
| `attention_light` | `attention_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `request_button` | `switch` | Attention request button |
| `ready_lamp` | `indicator` | Ready lamp |
| `attention_lamp` | `indicator` | Attention lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A spring-return request button transfers indication between a white ready lamp and an amber attention lamp.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command ready_light, attention_light.

### Normal sequence

- Observe the initial input state.
- Apply request_held.
- Verify the PLC produces only the required ready_light, attention_light.

### Expected observations

- Exactly one indication is on in both released and held states.
