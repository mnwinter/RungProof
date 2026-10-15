# Lab 2.7 - Dual-Contact Permissive help

Scene ID: `lab-2-07-dual-contact-permissive`  
Migrated source: `prototype/scenes/lab-2-07-dual-contact-permissive.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-07-dual-contact-permissive.scene.json`

## Purpose

A permissive lamp demonstrates how a normally-open reset input and an inverted normally-closed contact can produce the same logical request.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `reset_request` | `BOOL` | **PC** | `False` |
| `stop_contact_nc` | `BOOL` | **PC** | `True` |
| `permit_output` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle NO reset` | `toggle` | `reset_request` |
| `Toggle NC contact` | `toggle` | `stop_contact_nc` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `reset_request` | `reset_button` | `switch` |
| `permit_output` | `permit_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `reset_button` | `switch` | NO reset request |
| `nc_button` | `switch` | NC test contact |
| `permit_lamp` | `indicator` | Permit lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A permissive lamp demonstrates how a normally-open reset input and an inverted normally-closed contact can produce the same logical request.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command permit_output.

### Normal sequence

- Observe the initial input state.
- Apply reset_request, stop_contact_nc.
- Verify the PLC produces only the required permit_output.

### Expected observations

- Normal state is off; either logical request turns the permit lamp on.
