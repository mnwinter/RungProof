# Lab 2.22 - Dual-Spindle Plate Cell help

Scene ID: `lab-2-22-dual-spindle`  
Migrated source: `prototype/scenes/lab-2-22-dual-spindle.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-22-dual-spindle.scene.json`

## Purpose

Two drill heads process a clamped plate in parallel, retract independently, and release a transfer slide only after both are home.

## Current implementation limits

The reference preview now places both drill axes over one shared steel plate.
The fixture rests on a steel bed throughout its 2.2 m transfer. Both spindle
assemblies feed 330 mm and retract; home feedback follows their actual reference
positions. The slide carries the fixture with matching travel and retained
contact, rather than moving the fixture with an independent animation.

Start requires the initial pose. Stop holds partial travel and removes commands;
use Reset before restarting an interrupted or completed reference cycle. Reset
restores the fixture, carriage and both spindle homes.

The offline `--audit-dual-spindle` diagnostic passes 18 installation/reference
checks. Native Windows inspection covers home, full feed, intermediate transfer
and endpoint from five angles. This is sampled visual/reference evidence;
selected-controller axial feed/transfer and independent retraction logic remain
unverified. `cycle_complete=True` still belongs to the timed reference sequence.

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

## Machine guide

Two drill heads process a clamped plate in parallel, retract independently, and release a transfer slide only after both are home.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command drill_a_run, drill_b_run, transfer_extend.

### Normal sequence

- parallel drilling
- spindle dwell
- retracting
- both home
- transfer plate
- complete

### Expected observations

- Transfer begins only after both spindles are home; all motion commands finish off.
