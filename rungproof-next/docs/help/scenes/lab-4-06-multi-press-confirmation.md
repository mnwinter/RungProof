# Lab 4.6 - Multi-Press Confirmation help

Scene ID: `lab-4-06-multi-press-confirmation`  
Migrated source: `prototype/scenes/lab-4-06-multi-press-confirmation.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-06-multi-press-confirmation.scene.json`

## Purpose

Count raw A/B button presses in independent PLC counters and compare their accumulated counts. Reset clears both counters.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `button_a_pressed` | `BOOL` | **PC** | `False` |
| `button_b_pressed` | `BOOL` | **PC** | `False` |
| `reset_pressed` | `BOOL` | **PC** | `False` |
| `confirmation_valid` | `BOOL` | **PLC** | `False` |
| `button_a_count` | `DINT` | **PLC** | `0` |
| `button_b_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press A` | `pulse` | `button_a_pressed` |
| `Press B` | `pulse` | `button_b_pressed` |
| `Reset both counters` | `pulse` | `reset_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `button_a_pressed` | `switch_0` | `switch` |
| `button_b_pressed` | `switch_1` | `switch` |
| `reset_pressed` | `reset_button` | `switch` |
| `confirmation_valid` | `indicator_2` | `indicator` |
| `button_a_count` | `count_a_display` | `numericDisplay` |
| `button_b_count` | `count_b_display` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `switch` | Channel A pulse button |
| `switch_1` | `switch` | Channel B pulse button |
| `indicator_2` | `indicator` | PLC count comparison indication |
| `reset_button` | `switch` | Reset both PLC counters |
| `count_a_display` | `trainingAccessory` | PLC channel A accumulated count |
| `count_b_display` | `trainingAccessory` | PLC channel B accumulated count |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Count raw A/B button presses in independent PLC counters and compare their accumulated counts. Reset clears both counters.

### Start conditions

- Use the built-in offline controller; explicitly author or open a compatible project before Run.
- Both raw press inputs and reset_pressed start false; both PLC count outputs start zero.

### Normal sequence

- Author or explicitly open a compatible ladder project, then Run.
- Press A twice and B three times, waiting for the momentary input to return false between presses.
- Verify both PLC count displays and the green indication; extra presses beyond the reference limits remove the indication.
- Press RESET to clear both PLC counters and the indication.
- Stop clears both PLC count readouts to zero while retaining counter memory; Run republishes the retained counts. Application Reset clears counter memory and stays stopped.

### Expected observations

- The original offline reference enables confirmation_valid only at exactly A=2 and B=3; extra presses beyond the limits remove it. These values are chosen training assumptions, not source-specified presets. Press order is unrestricted; there is no elapsed-time window.
