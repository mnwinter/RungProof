# Lab 2.13 - Coolant Jug Filling Cell help

Scene ID: `lab-2-13-coolant-jug-fill`  
Migrated source: `prototype/scenes/lab-2-13-coolant-jug-fill.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-13-coolant-jug-fill.scene.json`

## Purpose

An empty coolant jug indexes under a fill valve, fills to a high probe, and then exits the station.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `jug_present` | `BOOL` | **PC** | `False` |
| `fill_valve_open` | `BOOL` | **PLC** | `False` |
| `high_level_probe` | `BOOL` | **PC** | `False` |
| `fill_skid_run` | `BOOL` | **PLC** | `False` |
| `fill_percent` | `REAL` | **SIM** | `0` |
| `cell_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start jug fill` | `start` | `fill-cycle` |
| `Stop fill cell` | `stop` | `` |
| `Reset jug` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `conveyor_run` | `fill_conveyor` | `running` |
| `jug_present` | `jug_present_sensor` | `photoeye` |
| `fill_valve_open` | `fill_valve` | `position` |
| `fill_skid_run` | `fill_skid` | `running` |
| `cell_color` | `fill_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `fill_conveyor` | `conveyor` | Jug indexing conveyor |
| `coolant_jug` | `box` | Coolant jug |
| `jug_present_sensor` | `photoeye` | Jug present sensor |
| `fill_valve` | `toteFiller` | Coolant fill nozzle and valve manifold |
| `fill_skid` | `meteringSkid` | Metered coolant skid |
| `fill_start` | `switch` | Start fill cycle |
| `fill_status` | `indicator` | Fill cell status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyor_run` | `False` |
| `fill_valve_open` | `False` |
| `fill_skid_run` | `False` |
| `cell_color` | `red` |

## Machine guide

An empty coolant jug indexes under a fill valve, fills to a high probe, and then exits the station.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command conveyor_run, fill_valve_open, fill_skid_run.

### Normal sequence

- indexing
- filling
- level reached
- discharging
- ready

### Expected observations

- The valve cannot remain open after the high probe; the filled jug exits and the conveyor stops.
