# Lab 4.2 - Counter Reset Lamp help

Scene ID: `lab-4-02-counter-reset-lamp`  
Migrated source: `prototype/scenes/lab-4-02-counter-reset-lamp.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-02-counter-reset-lamp.scene.json`

## Purpose

Count raw operator pulses in the PLC counter, light the lamp at its preset, and clear the count with a momentary reset.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `pulse_received` | `BOOL` | **PC** | `False` |
| `reset_pressed` | `BOOL` | **PC** | `False` |
| `counter_lamp` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Count pulse` | `pulse` | `pulse_received` |
| `Reset counter` | `pulse` | `reset_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `pulse_received` | `switch_0` | `switch` |
| `reset_pressed` | `switch_2` | `switch` |
| `counter_lamp` | `indicator_1` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `switch` | Count pulse button |
| `indicator_1` | `indicator` | Counter Reset Lamp indicator |
| `switch_2` | `switch` | Counter reset button |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Count raw operator pulses in the PLC counter, light the lamp at its preset, and clear the count with a momentary reset.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Author or explicitly load a compatible counter/reset ladder project and Run.
- Press COUNT three times, waiting for the input to return false between presses.
- Press RESET to clear the counter and lamp; the next COUNT starts a new batch.

### Expected observations

- Three separate count pulses light the reference lamp; reset clears count and lamp, and a subsequent new count starts at one.
