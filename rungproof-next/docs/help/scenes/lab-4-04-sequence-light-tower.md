# Lab 4.4 - Sequence Light Tower help

Scene ID: `lab-4-04-sequence-light-tower`  
Migrated source: `prototype/scenes/lab-4-04-sequence-light-tower.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-04-sequence-light-tower.scene.json`

## Purpose

A four-color tower advances red -> amber -> green -> blue -> off on separate Step requests.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `sequence_start` | `BOOL` | **PC** | `False` |
| `sequence_step_due` | `BOOL` | **PC** | `False` |
| `tower_active` | `BOOL` | **PLC** | `False` |
| `sequence_complete` | `BOOL` | **PLC** | `False` |
| `tower_red` | `BOOL` | **PLC** | `False` |
| `tower_amber` | `BOOL` | **PLC** | `False` |
| `tower_green` | `BOOL` | **PLC** | `False` |
| `tower_blue` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press sequence Start (momentary)` | `pulse` | `sequence_start` |
| `Press sequence Step (momentary)` | `pulse` | `sequence_step_due` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `sequence_start` | `switch_1` | `switch` |
| `sequence_step_due` | `switch_3` | `switch` |
| `sequence_complete` | `indicator_4` | `indicator` |
| `tower_red` | `indicator_2` | `indicatorChannel` |
| `tower_amber` | `indicator_2` | `indicatorChannel` |
| `tower_green` | `indicator_2` | `indicatorChannel` |
| `tower_blue` | `indicator_2` | `indicatorChannel` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `machine_0` | `machine` | Static process equipment |
| `switch_1` | `switch` | Momentary sequence Start |
| `indicator_2` | `indicator` | Four-color sequence tower |
| `switch_3` | `switch` | Momentary sequence Step |
| `indicator_4` | `indicator` | Sequence complete status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A four-color tower advances red -> amber -> green -> blue -> off on separate Step requests.

### Start conditions

- An offline controller program is verified and loaded.
- Controller scans are running; the sequence is inactive.

### Normal sequence

- Accept a fresh Start only while inactive and reset the step counter.
- Advance one state on each fresh Step while active; decode only one of four color outputs.
- After the fourth Step, turn all tower colors off and indicate completion separately.
- Stop removes commands; require a fresh Start rather than resuming the previous state.

### Expected observations

- Red -> amber -> green -> blue -> off; four separate Step presses, exactly one color while active, no idle advance.
