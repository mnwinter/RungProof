# Lab 2.4 - Two-Station Call Beacon help

Scene ID: `lab-2-04-two-station-call`  
Migrated source: `prototype/scenes/lab-2-04-two-station-call.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-04-two-station-call.scene.json`

## Purpose

Either of two work areas can request assistance by energizing one shared amber call beacon.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `north_call_pressed` | `BOOL` | **PC** | `False` |
| `south_call_pressed` | `BOOL` | **PC** | `False` |
| `assistance_call_on` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press / release north call` | `toggle` | `north_call_pressed` |
| `Press / release south call` | `toggle` | `south_call_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `north_call_pressed` | `north_call_button` | `switch` |
| `south_call_pressed` | `south_call_button` | `switch` |
| `assistance_call_on` | `assistance_beacon` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `north_call_button` | `switch` | North station call |
| `south_call_button` | `switch` | South station call |
| `assistance_beacon` | `indicator` | Assistance call beacon |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Either of two work areas can request assistance by energizing one shared amber call beacon.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command assistance_call_on.

### Normal sequence

- Observe the initial input state.
- Apply north_call_pressed, south_call_pressed.
- Verify the PLC produces only the required assistance_call_on.

### Expected observations

- The beacon is off with neither input and on when either or both call inputs are active.
