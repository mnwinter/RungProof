# Lab 2.1 - Workstation Call Lamp help

Scene ID: `lab-2-01-workstation-call`  
Migrated source: `prototype/scenes/lab-2-01-workstation-call.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-01-workstation-call.scene.json`

## Purpose

A workstation call button directly controls a blue material-request lamp while the button is held.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `material_call_pressed` | `BOOL` | **PC** | `False` |
| `material_call_on` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press / release material call` | `toggle` | `material_call_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `material_call_pressed` | `material_call_button` | `switch` |
| `material_call_on` | `material_call_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `material_call_button` | `switch` | Material call button |
| `material_call_lamp` | `indicator` | Material request lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A workstation call button directly controls a blue material-request lamp while the button is held.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command material_call_on.

### Normal sequence

- Observe the initial input state.
- Apply material_call_pressed.
- Verify the PLC produces only the required material_call_on.

### Expected observations

- The call lamp follows the button state: off when released and on when pressed.
