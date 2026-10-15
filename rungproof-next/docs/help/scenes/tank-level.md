# Tank Level / 4–20 mA help

Scene ID: `tank-level`  
Migrated source: `prototype/scenes/tank-level.json`  
Scene contract: `res://scenes/migrated/tank-level.scene.json`

## Purpose

Run the scene, start the inlet pump, and open or close the drain. Fluid level, discrete low/high switches, and a linear 4–20 mA transmitter update together.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `inlet_pump_run` | `BOOL` | **PLC** | `False` |
| `drain_valve_open` | `BOOL` | **PLC** | `False` |
| `tank_level` | `REAL` | **SIM** | `42` |
| `level_transmitter` | `REAL` | **PC** | `10.72` |
| `low_level_switch` | `BOOL` | **PC** | `False` |
| `high_level_switch` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Run simulated process` | `run` | `` |
| `Toggle inlet pump (simulated)` | `toggle` | `inlet_pump_run` |
| `Toggle drain valve (simulated)` | `toggle` | `drain_valve_open` |
| `Stop simulated process` | `stop` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `drain_valve_open` | `drain_valve` | `position` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `process_tank` | `tank` | Process tank T-101 |
| `inlet_pump` | `pump` | Inlet pump P-101 |
| `inlet_pipe` | `pipe` | Tank inlet pipe |
| `outlet_pipe` | `pipe` | Tank outlet pipe |
| `drain_valve` | `valve` | Tank drain valve |
| `low_level_switch` | `levelSensor` | Low level switch LSL-101 |
| `high_level_switch` | `levelSensor` | High level switch LSH-101 |
| `level_transmitter` | `levelSensor` | Level transmitter LT-101 |
| `pump_station` | `switch` | Pump local station |
| `drain_station` | `switch` | Drain valve local station |
| `tank_stacklight` | `indicator` | Tank status light |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.
