# Lab 2.11 - Inbound Tote Stop help

Scene ID: `lab-2-11-inbound-tote-stop`  
Migrated source: `prototype/scenes/lab-2-11-inbound-tote-stop.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-11-inbound-tote-stop.scene.json`

## Purpose

A tote advances to a scan photoeye, pauses for identification, then clears the station before the conveyor returns to ready.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `tote_at_scanner` | `BOOL` | **PC** | `False` |
| `scan_complete` | `BOOL` | **PC** | `False` |
| `cell_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start tote cycle` | `start` | `scan-cycle` |
| `Stop conveyor safely` | `stop` | `` |
| `Reset tote` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `conveyor_run` | `inbound_conveyor` | `running` |
| `tote_at_scanner` | `scan_photoeye` | `photoeye` |
| `cell_color` | `inbound_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `inbound_conveyor` | `conveyor` | Inbound tote conveyor |
| `inbound_tote` | `box` | Reusable tote |
| `scan_photoeye` | `photoeye` | Barcode scan photoeye |
| `inbound_start` | `switch` | Start inbound cycle |
| `inbound_status` | `indicator` | Inbound status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyor_run` | `False` |
| `cell_color` | `red` |

## Machine guide

A tote advances to a scan photoeye, pauses for identification, then clears the station before the conveyor returns to ready.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command conveyor_run.

### Normal sequence

- feeding
- scanning
- clearing
- ready

### Expected observations

- The tote stops at the scanner, records completion, clears, and leaves the conveyor stopped.
