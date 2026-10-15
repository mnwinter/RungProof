# Lab 2.22 - Dual-Spindle Plate Cell help

Scene ID: `lab-2-22-dual-spindle`  
Migrated source: `prototype/scenes/lab-2-22-dual-spindle.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-22-dual-spindle.scene.json`

## Purpose

Two drill heads process a clamped plate in parallel, retract independently, and release a transfer slide only after both are home.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `drill_a_run` | `BOOL` | **PLC** | `False` |
| `drill_b_run` | `BOOL` | **PLC** | `False` |
| `drill_a_home` | `BOOL` | **PC** | `True` |
| `drill_b_home` | `BOOL` | **PC** | `True` |
| `transfer_extend` | `BOOL` | **PLC** | `False` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |
| `drill_a_position` | `REAL` | **SIM** | `0` |
| `drill_b_position` | `REAL` | **SIM** | `0` |
| `drill_a_feed` | `BOOL` | **PLC** | `False` |
| `drill_a_retract` | `BOOL` | **PLC** | `False` |
| `drill_b_feed` | `BOOL` | **PLC** | `False` |
| `drill_b_retract` | `BOOL` | **PLC** | `False` |
| `transfer_retract` | `BOOL` | **PLC** | `False` |
| `drill_a_at_depth` | `BOOL` | **PC** | `False` |
| `drill_b_at_depth` | `BOOL` | **PC** | `False` |
| `transfer_home` | `BOOL` | **PC** | `True` |
| `transfer_at_end` | `BOOL` | **PC** | `False` |
| `transfer_inhibited` | `BOOL` | **PC** | `False` |
| `transfer_position` | `REAL` | **SIM** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start dual-spindle cycle` | `start` | `dual-cycle` |
| `Stop both spindles` | `stop` | `` |
| `Reset plate cell` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `drill_a_run` | `drill_a` | `running` |
| `drill_b_run` | `drill_b` | `running` |
| `transfer_extend` | `plate_transfer` | `running` |
| `status_color` | `dual_drill_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `drill_a` | `drillPress` | Pilot drill spindle |
| `drill_b` | `drillPress` | Countersink spindle |
| `metal_plate` | `box` | Clamped metal plate |
| `plate_transfer` | `pusher` | Plate transfer slide |
| `dual_drill_start` | `switch` | Start dual-spindle cycle |
| `dual_drill_status` | `indicator` | Dual-spindle status |
| `plate_bed` | `containerReceiver` | Supported plate fixture and receiving bed |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `drill_a_run` | `False` |
| `drill_b_run` | `False` |
| `transfer_extend` | `False` |
| `status_color` | `red` |
| `drill_a_feed` | `False` |
| `drill_a_retract` | `False` |
| `drill_b_feed` | `False` |
| `drill_b_retract` | `False` |
| `transfer_retract` | `False` |

## Machine guide

Two drill heads process a clamped plate in parallel, retract independently, and release a transfer slide only after both are home.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC commands rotation, feed/retract and transfer separately. Native feed/return durations are 1.7/1.6 s for 330 mm travel; transfer is 1.2 s for 2.2 m travel. These are simulator settings, not physical machine specifications.

### Normal sequence

- parallel drilling
- spindle dwell
- retracting
- both home
- transfer plate
- complete

### Expected observations

- Transfer begins only after both spindles are home; all motion commands finish off.
