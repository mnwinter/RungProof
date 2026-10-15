# Lab 5.2 - Timed Lamp-Off help

Scene ID: `lab-5-02-timed-lamp-off`  
Migrated source: `prototype/scenes/lab-5-02-timed-lamp-off.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-02-timed-lamp-off.scene.json`

## Purpose

A momentary START supplies one accepted-scan pulse; PLC pulse-timer logic owns the lamp interval. No manually precomputed time-active input.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `start_pulse` | `BOOL` | **PC** | `False` |
| `timed_lamp` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press START (one accepted scan)` | `pulse` | `start_pulse` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `start_pulse` | `switch_0` | `switch` |
| `timed_lamp` | `indicator_1` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `switch` | START momentary timer trigger |
| `indicator_1` | `indicator` | PLC timed lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A momentary START supplies one accepted-scan pulse; PLC pulse-timer logic owns the lamp interval. No manually precomputed time-active input.

### Start conditions

- Use the built-in offline controller and the stated authored or explicitly opened reference program.
- Initial input false, timer zero and lamp off.

### Normal sequence

- Default exercise editor is empty. Build a PLC pulse-timer program or explicitly open .tools/plant-review-timed-lamp-off.rpproj.json after --audit-timer-lessons.
- Run and press START. The original offline reference uses a three-second TP, a declared training choice absent from the source lesson.
- START pulses during an active interval are ignored; after expiry, a fresh START triggers a new interval. Clicks before the same accepted scan coalesce.

### Expected observations

- Explicit original reference TP: lamp immediately on after accepted START, off at three-second accumulated time, extra active presses ignored, fresh press after expiry restarts; Stop cancels.
