# Lab 4.1 - Press-Count Lamp help

Scene ID: `lab-4-01-press-count-lamp`  
Migrated source: `prototype/scenes/lab-4-01-press-count-lamp.plcscene`  
Scene contract: `prototype/scenes/lab-4-01-press-count-lamp.plcscene`

## Purpose

A discrete counter lesson turns a station lamp on after a defined number of operator pulses.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `pulse_received` | `BOOL` | **PC** | `False` |
| `threshold_lamp` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle pulse received` | `toggle` | `pulse_received` |
| `Toggle count at threshold` | `toggle` | `count_at_threshold` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `pulse_received` | `switch_0` | `switch` |
| `count_at_threshold` | `switch_2` | `switch` |
| `threshold_lamp` | `indicator_1` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `switch` | Press-Count Lamp switch |
| `indicator_1` | `indicator` | Press-Count Lamp indicator |
| `switch_2` | `switch` | Press-Count Lamp operator input |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A discrete counter lesson turns a station lamp on after a defined number of operator pulses.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- The lamp is off below the threshold and on when count_at_threshold is true.
