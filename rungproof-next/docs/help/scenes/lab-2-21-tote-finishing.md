# Lab 2.21 - Chemical Tote Finishing Line help

Scene ID: `lab-2-21-tote-finishing`  
Migrated source: `prototype/scenes/lab-2-21-tote-finishing.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-21-tote-finishing.scene.json`

## Purpose

A small chemical tote is filled, capped, labeled, inspected, and discharged through a five-station finishing line.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `fill_valve_open` | `BOOL` | **PLC** | `False` |
| `capper_run` | `BOOL` | **PLC** | `False` |
| `labeler_run` | `BOOL` | **PLC** | `False` |
| `inspection_run` | `BOOL` | **PLC** | `False` |
| `inspection_ok` | `BOOL` | **PC** | `False` |
| `station_number` | `DINT` | **SIM** | `0` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |
| `tote_position` | `REAL` | **PC** | `-5.8` |
| `tote_at_fill` | `BOOL` | **PC** | `False` |
| `tote_at_cap` | `BOOL` | **PC** | `False` |
| `tote_at_label` | `BOOL` | **PC** | `False` |
| `tote_at_inspection` | `BOOL` | **PC** | `False` |
| `tote_at_exit` | `BOOL` | **PC** | `False` |
| `fill_percent` | `REAL` | **PC** | `0` |
| `actual_fill_percent_per_second` | `REAL` | **PC** | `0` |
| `fill_inhibited` | `BOOL` | **PC** | `False` |
| `fill_complete` | `BOOL` | **PC** | `False` |
| `cap_extension_percent` | `REAL` | **PC** | `0` |
| `cap_applied` | `BOOL` | **PC** | `False` |
| `cap_busy` | `BOOL` | **PC** | `False` |
| `cap_inhibited` | `BOOL` | **PC** | `False` |
| `cap_home` | `BOOL` | **PC** | `True` |
| `tote_transfer_inhibited` | `BOOL` | **PC** | `False` |
| `label_applied` | `BOOL` | **PC** | `False` |
| `label_extension_percent` | `REAL` | **PC** | `0` |
| `label_busy` | `BOOL` | **PC** | `False` |
| `label_home` | `BOOL` | **PC** | `True` |
| `label_inhibited` | `BOOL` | **PC** | `False` |
| `inspection_busy` | `BOOL` | **PC** | `False` |
| `inspection_done` | `BOOL` | **PC** | `False` |
| `inspection_inhibited` | `BOOL` | **PC** | `False` |
| `inspection_defect_code` | `DINT` | **PC** | `0` |
| `inspection_view_clear` | `BOOL` | **PC** | `False` |
| `tote_label_aligned` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start tote finishing` | `start` | `finish-cycle` |
| `Stop line safely` | `stop` | `` |
| `Reset tote line` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `conveyor_run` | `finishing_conveyor` | `running` |
| `fill_valve_open` | `finishing_fill_valve` | `position` |
| `capper_run` | `capper` | `running` |
| `labeler_run` | `labeler` | `running` |
| `inspection_run` | `vision_inspector` | `running` |
| `status_color` | `line_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `finishing_conveyor` | `conveyor` | Tote finishing conveyor |
| `finishing_tote` | `box` | Chemical tote |
| `finishing_fill_valve` | `toteFiller` | Metered tote filling station |
| `capper` | `toteCapper` | Servo capper |
| `labeler` | `toteLabeler` | Print-and-apply labeler |
| `vision_inspector` | `toteVision` | Vision inspection station |
| `line_start` | `switch` | Start finishing cycle |
| `line_status` | `indicator` | Finishing line status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyor_run` | `False` |
| `fill_valve_open` | `False` |
| `capper_run` | `False` |
| `labeler_run` | `False` |
| `inspection_run` | `False` |
| `status_color` | `red` |

## Machine guide

A small chemical tote is filled, capped, labeled, inspected, and discharged through a five-station finishing line.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command conveyor_run, fill_valve_open, capper_run, labeler_run, inspection_run.

### Normal sequence

- index to fill
- fill
- index to cap
- cap
- index to label
- label
- index to inspect
- inspect
- discharge
- ready

### Expected observations

- Stations execute in order and every actuator is off after a passing tote discharges.
