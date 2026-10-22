# Water Tank — High/Low Switches help

Scene ID: `tank-high-low`  
Migrated source: `prototype/scenes/tank-high-low.json`  
Scene contract: `res://scenes/migrated/tank-high-low.scene.json`

## Purpose

A discrete level-control example with separate low and high point level switches. Run the scene, then operate the inlet pump and drain to cross each switch elevation.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback or local operator requests; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `pump_request` | `BOOL` | **PC** | `False` |
| `drain_request` | `BOOL` | **PC** | `False` |
| `inlet_pump_run` | `BOOL` | **PLC** | `False` |
| `drain_valve_open` | `BOOL` | **PLC** | `False` |
| `tank_level` | `REAL` | **SIM** | `50` |
| `low_level_switch` | `BOOL` | **PC** | `False` |
| `high_level_switch` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Run simulated process` | `run` | `` |
| `Toggle inlet pump request` | `toggle` | `pump_request` |
| `Toggle drain request` | `toggle` | `drain_request` |
| `Stop simulated process` | `stop` | `` |

The two local stations toggle maintained request inputs. A loaded controller must consume those inputs and produce `inlet_pump_run` / `drain_valve_open`; clicking a station never writes either PLC output. A simple learner policy gives drain priority, stops fill at the high switch, and inhibits drain at the low switch. Stop removes commands; maintained requests may still be ON when Run resumes. Reset/toggle them OFF before a fresh test. Standalone preview rules retain manual pump/drain operation with drain priority.

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `drain_valve_open` | `hl_drain_valve` | `position` |
| `pump_request` | `hl_pump_station` | `switch` |
| `drain_request` | `hl_drain_station` | `switch` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `water_tank_hl` | `tank` | Water tank T-201 |
| `hl_inlet_pump` | `pump` | Water inlet pump P-201 |
| `hl_inlet_pipe` | `pipe` | Tank inlet pipe |
| `hl_outlet_pipe` | `pipe` | Tank outlet pipe |
| `hl_drain_valve` | `valve` | Tank drain valve |
| `hl_low_switch` | `levelSensor` | Low level switch LSL-201 |
| `hl_high_switch` | `levelSensor` | High level switch LSH-201 |
| `hl_pump_station` | `switch` | Pump local station |
| `hl_drain_station` | `switch` | Drain valve local station |
| `hl_stacklight` | `indicator` | Tank status light |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.
