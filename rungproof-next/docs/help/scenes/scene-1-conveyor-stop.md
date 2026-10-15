# Conveyor Stop help

Scene ID: `scene-1-conveyor-stop`  
Migrated source: `prototype/scenes/scene-1-conveyor-stop.plcscene`  
Scene contract: `res://scenes/migrated/scene-1-conveyor-stop.scene.json`

## Purpose

The established beginner scene: the PLC runs one conveyor until the simulated package blocks the photoeye, then the conveyor stops with the package held at the sensor.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `conveyor_running` | `BOOL` | **PLC** | `False` |
| `simulated_photoeye` | `BOOL` | **PC** | `False` |
| `object_position` | `REAL` | **SIM** | `0` |
| `component_state` | `STRING` | **SIM** | `stopped_loaded` |
| `parts_completed` | `DINT` | **SIM** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start / stop conveyor` | `toggle` | `conveyor_running` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `conveyor_running` | `scene1_conveyor` | `running` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `scene1_conveyor` | `conveyor` | Scene 1 conveyor |
| `scene1_product` | `box` | Scene 1 package |
| `scene1_photoeye` | `photoeye` | Simulated photoeye |
| `scene1_station` | `switch` | Scene Start / Stop |
| `scene1_stacklight` | `indicator` | Scene 1 status light |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.
