# Lab 2.10 - Dust Collector Seal-In help

Scene ID: `lab-2-10-dust-collector-seal-in`  
Migrated source: `prototype/scenes/lab-2-10-dust-collector-seal-in.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-10-dust-collector-seal-in.scene.json`

## Purpose

Separate start and stop controls latch a dust-collector command until an explicit stop request resets it.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `collector_start_request` | `BOOL` | **PC** | `False` |
| `collector_stop_request` | `BOOL` | **PC** | `False` |
| `run_latched` | `BOOL` | **PLC** | `False` |
| `collector_run` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Pulse collector start` | `pulse` | `collector_start_request` |
| `Pulse collector stop` | `pulse` | `collector_stop_request` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `collector_start_request` | `collector_start` | `switch` |
| `collector_stop_request` | `collector_stop` | `switch` |
| `collector_run` | `collector_motor` | `running` |
| `collector_run` | `collector_run_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `collector_start` | `switch` | Collector start |
| `collector_stop` | `switch` | Collector stop |
| `collector_motor` | `fan` | Dust collector fan |
| `collector_run_lamp` | `indicator` | Collector running lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Separate start and stop controls latch a dust-collector command until an explicit stop request resets it.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command collector_run.

### Normal sequence

- Observe the initial input state.
- Apply collector-start, collector-stop.
- Verify the PLC produces only the required collector_run.

### Expected observations

- Start seals in the run command; stop removes it.
