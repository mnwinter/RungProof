# Lab 5.1 - Delayed Lamp help

Scene ID: `lab-5-01-delayed-lamp`  
Migrated source: `prototype/scenes/lab-5-01-delayed-lamp.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-01-delayed-lamp.scene.json`

## Purpose

A maintained OFF/ON selector requests the existing authored two-second PLC TON; only PLC timer completion energizes the lamp.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `timer_request` | `BOOL` | **PC** | `False` |
| `machine_stop` | `BOOL` | **PC** | `False` |
| `reset_pressed` | `BOOL` | **PC** | `False` |
| `delayed_lamp` | `BOOL` | **PLC** | `False` |
| `delay_elapsed_ms` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Timer REQUEST selector` | `toggle` | `timer_request` |
| `Machine Stop pushbutton` | `pulse` | `machine_stop` |
| `Machine Reset pushbutton` | `pulse` | `reset_pressed` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `timer_request` | `switch_0` | `selector` |
| `machine_stop` | `switch_2` | `switch` |
| `reset_pressed` | `switch_3` | `switch` |
| `delayed_lamp` | `indicator_1` | `indicator` |
| `delay_elapsed_ms` | `training_accessory_4` | `numericDisplay` (`ELAPSED MS`) |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `rotarySwitch` | Timer REQUEST selector OFF / ON |
| `indicator_1` | `indicator` | PLC timed lamp |
| `switch_2` | `switch` | Delayed Lamp machine stop |
| `switch_3` | `switch` | Delayed Lamp machine reset |
| `training_accessory_4` | `trainingAccessory` | TON elapsed-time display |

The timed output is one green bench lamp bound to `delayed_lamp`; it is not a three-color machine-status tower.

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A maintained OFF/ON selector requests the existing authored two-second PLC TON; only PLC timer completion energizes the lamp.

### Start conditions

- Use the built-in offline controller and the stated authored or explicitly opened reference program.
- Initial input false, timer zero and lamp off.

### Normal sequence

- Select Demo 2, Run, then turn REQUEST ON.
- The authored PLC TON keeps the lamp off for 100 accepted 20 ms scans (2 s), then on while REQUEST remains ON.
- Turning OFF clears the timer and lamp on the next accepted scan; a new ON needs the full delay.

### Expected observations

- Two-second TON in existing Demo 2; OFF resets it, Stop resets it, Run with retained ON starts the full delay again.
