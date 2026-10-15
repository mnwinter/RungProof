# Lab 2.2 - Dual Confirmation Lamp help

Scene ID: `lab-2-02-dual-confirmation`  
Migrated source: `prototype/scenes/lab-2-02-dual-confirmation.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-02-dual-confirmation.scene.json`

## Purpose

A handoff-ready lamp turns on only after both the operator and quality confirmation buttons are active.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `operator_confirmed` | `BOOL` | **PC** | `False` |
| `quality_confirmed` | `BOOL` | **PC** | `False` |
| `handoff_ready` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press / release operator confirm` | `toggle` | `operator_confirmed` |
| `Press / release quality confirm` | `toggle` | `quality_confirmed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `operator_confirmed` | `operator_confirm_button` | `switch` |
| `quality_confirmed` | `quality_confirm_button` | `switch` |
| `handoff_ready` | `handoff_ready_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `operator_confirm_button` | `switch` | Operator confirmation |
| `quality_confirm_button` | `switch` | Quality confirmation |
| `handoff_ready_lamp` | `indicator` | Handoff ready lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A handoff-ready lamp turns on only after both the operator and quality confirmation buttons are active.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command handoff_ready.

### Normal sequence

- Observe the initial input state.
- Apply operator_confirmed, quality_confirmed.
- Verify the PLC produces only the required handoff_ready.

### Expected observations

- The ready lamp is on only when both confirmation inputs are active.
