# Lab 2.14 - Sump Dewatering Pump help

Scene ID: `lab-2-14-sump-pump`  
Migrated source: `prototype/scenes/lab-2-14-sump-pump.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-14-sump-pump.scene.json`

## Purpose

A simulated sump rises to the high float, starts a dewatering pump, and pumps down until the low float resets the run latch.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `start_sump` | `BOOL` | **PC** | `False` |
| `cycle_enable` | `BOOL` | **PLC** | `False` |
| `sump_level` | `REAL` | **SIM** | `22` |
| `high_float_active` | `BOOL` | **PC** | `False` |
| `low_float_active` | `BOOL` | **PC** | `False` |
| `pump_run` | `BOOL` | **PLC** | `False` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Simulate sump cycle` | `start` | `level-cycle` |
| `Stop pump safely` | `stop` | `` |
| `Reset sump` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `pump_run` | `sump_pump` | `running` |
| `high_float_active` | `high_float` | `levelSensor` |
| `low_float_active` | `low_float` | `levelSensor` |
| `status_color` | `sump_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `sump_tank` | `tank` | Equipment-room sump |
| `sump_pump` | `pump` | Dewatering pump |
| `low_float` | `levelSensor` | Low level float switch |
| `high_float` | `levelSensor` | High level float switch |
| `discharge_pipe` | `pipe` | Sump discharge |
| `discharge_isolation` | `valve` | Discharge isolation valve |
| `sump_start` | `switch` | Run level cycle |
| `sump_status` | `indicator` | Sump status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `pump_run` | `False` |
| `status_color` | `red` |

## Machine guide

A simulated sump rises to the high float, starts a dewatering pump, and pumps down until the low float resets the run latch.

### Start conditions

- Load and Verify + load an offline ladder program, then press Run. Local testing does not establish a physical PLC or watchdog connection.
- All required simulator inputs are at their documented initial state.
- Press Simulate sump cycle to pulse start_sump. The ladder must command cycle_enable and pump_run.

### Normal sequence

- level rising
- pump latched
- pumping down
- low level stop

### Expected observations

- High float starts the pump; it remains on through the deadband and stops only at low float.
