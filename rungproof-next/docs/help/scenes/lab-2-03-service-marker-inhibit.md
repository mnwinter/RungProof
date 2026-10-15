# Lab 2.3 - Service Marker Inhibit help

Scene ID: `lab-2-03-service-marker-inhibit`  
Migrated source: `prototype/scenes/lab-2-03-service-marker-inhibit.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-03-service-marker-inhibit.scene.json`

## Purpose

A normally lit white service marker turns off while its local inhibit button is active.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `marker_inhibit_pressed` | `BOOL` | **PC** | `False` |
| `service_marker_on` | `BOOL` | **PLC** | `True` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press / release marker inhibit` | `toggle` | `marker_inhibit_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `marker_inhibit_pressed` | `marker_inhibit_button` | `switch` |
| `service_marker_on` | `service_marker_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `marker_inhibit_button` | `switch` | Marker inhibit button |
| `service_marker_lamp` | `indicator` | Service marker lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A normally lit white service marker turns off while its local inhibit button is active.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command service_marker_on.

### Normal sequence

- Observe the initial input state.
- Apply marker_inhibit_pressed.
- Verify the PLC produces only the required service_marker_on.

### Expected observations

- The marker is on with the button released and off while the inhibit input is active.
