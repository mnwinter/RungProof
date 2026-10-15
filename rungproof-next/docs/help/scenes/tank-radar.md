# Water Tank — Radar Level help

Scene ID: `tank-radar`  
Migrated source: `prototype/scenes/tank-radar.json`  
Scene contract: `res://scenes/migrated/tank-radar.scene.json`

## Purpose

Symbolic non-contact radar example. The roof-mounted transmitter reports distance from its antenna lens to the simulated surface, level and idealized 4-20 mA feedback. The opaque tank hides the internal beam; echo health is idealized.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `inlet_pump_run` | `BOOL` | **PLC** | `False` |
| `drain_valve_open` | `BOOL` | **PLC** | `False` |
| `tank_level` | `REAL` | **SIM** | `35` |
| `radar_level` | `REAL` | **PC** | `35` |
| `radar_distance` | `REAL` | **PC** | `3.1003565788269043` |
| `radar_signal` | `REAL` | **PC** | `9.6` |
| `radar_echo_ok` | `BOOL` | **PC** | `True` |
| `tank_inspection_view` | `BOOL` | **SIM** | `False` |
| `cycle_request` | `BOOL` | **PC** | `False` |
| `manual_drain_request` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Run simulated process` | `run` | `` |
| `Toggle automatic cycle request` | `toggle` | `cycle_request` |
| `Toggle manual drain request` | `toggle` | `manual_drain_request` |
| `Stop simulated process` | `stop` | `` |
| `Inspection view: ghost shell and roof` | `toggle` | `tank_inspection_view` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `drain_valve_open` | `radar_drain_valve` | `position` |
| `cycle_request` | `radar_pump_station` | `switch` |
| `manual_drain_request` | `radar_drain_station` | `switch` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `water_tank_radar` | `tank` | Water tank T-301 |
| `radar_inlet_pump` | `pump` | Water inlet pump P-301 |
| `radar_inlet_pipe` | `pipe` | Tank inlet pipe |
| `radar_outlet_pipe` | `pipe` | Tank outlet pipe |
| `radar_transmitter` | `radarLevelSensor` | Radar level transmitter LT-301 |
| `radar_pump_station` | `switch` | Automatic fill/drain cycle request |
| `radar_drain_station` | `switch` | Manual drain request (requires enabled cycle) |
| `radar_stacklight` | `indicator` | Tank status light |
| `radar_drain_valve` | `valve` | Radar tank grounded drain valve |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.
