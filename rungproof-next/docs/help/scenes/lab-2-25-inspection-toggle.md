# Lab 2.25 - Inspection Light Toggle help

Scene ID: `lab-2-25-inspection-toggle`  
Migrated source: `prototype/scenes/lab-2-25-inspection-toggle.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-25-inspection-toggle.scene.json`

## Purpose

Each operation of one spring-return button toggles a machine inspection light between latched off and latched on.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `toggle_button_pressed` | `BOOL` | **PC** | `False` |
| `toggle_memory` | `BOOL` | **PLC** | `False` |
| `inspection_light_on` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Pulse toggle button` | `pulse` | `toggle_button_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `toggle_button_pressed` | `inspection_toggle_button` | `switch` |
| `inspection_light_on` | `inspection_light` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `inspection_toggle_button` | `switch` | Inspection light toggle |
| `inspection_light` | `indicator` | Machine inspection light |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Each operation of one spring-return button toggles a machine inspection light between latched off and latched on.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command inspection_light_on.

### Normal sequence

- Observe the initial input state.
- Apply toggle button pulse.
- Verify the PLC produces only the required inspection_light_on.

### Expected observations

- Odd pulses turn the light on; even pulses return it off.
