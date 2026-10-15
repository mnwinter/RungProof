# Lab 4.3 - Repeat-Cycle Counter help

Scene ID: `lab-4-03-repeat-cycle-counter`  
Migrated source: `prototype/scenes/lab-4-03-repeat-cycle-counter.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-03-repeat-cycle-counter.scene.json`

## Purpose

A PLC counts actual completed CNC dry strokes and stops a three-cycle reference batch. Commands, machine feedback and counter state have separate owners.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `cycle_request` | `BOOL` | **PC** | `False` |
| `machine_enabled` | `BOOL` | **PC** | `False` |
| `machine_home` | `BOOL` | **PC** | `True` |
| `machine_busy` | `BOOL` | **PC** | `False` |
| `cycle_done` | `BOOL` | **PC** | `False` |
| `head_position` | `REAL` | **PC** | `100.0` |
| `cycle_active` | `BOOL` | **PLC** | `False` |
| `cycle_complete` | `BOOL` | **PLC** | `False` |
| `batch_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start batch` | `pulse` | `cycle_request` |
| `Toggle machine enable` | `toggle` | `machine_enabled` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `cycle_request` | `switch_1` | `switch` |
| `machine_enabled` | `switch_3` | `switch` |
| `cycle_active` | `indicator_2` | `indicator` |
| `cycle_complete` | `indicator_4` | `indicator` |
| `batch_count` | `batch_count_display` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `machine_0` | `machine` | Enclosed CNC dry-stroke station |
| `switch_1` | `switch` | Batch request button |
| `indicator_2` | `indicator` | PLC cycle command indication |
| `switch_3` | `switch` | Machine enable selector |
| `indicator_4` | `indicator` | PLC batch complete indication |
| `batch_count_display` | `trainingAccessory` | PLC completed-cycle count |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A PLC counts actual completed CNC dry strokes and stops a three-cycle reference batch. Commands, machine feedback and counter state have separate owners.

### Start conditions

- Built-in simulator or an explicitly configured symbolic external controller image.
- Author or explicitly open a compatible ladder project; the training editor starts empty.
- Enable the machine before START.

### Normal sequence

- Enable and press START once.
- Watch actual head feed/dwell/return and machine_home, machine_busy and cycle_done feedback.
- The explicit reference publishes COUNT 1, 2, 3 and then batch complete; application Reset is required for a new batch.

### Expected observations

- One accepted START with enable gives three actual feed/dwell/return cycles, COUNT=3 and batch completion; a held request or command must not add an extra cycle.
