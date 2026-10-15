# Lab 2.9 - Maintenance Beacon Selector help

Scene ID: `lab-2-09-maintenance-beacon`  
Migrated source: `prototype/scenes/lab-2-09-maintenance-beacon.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-09-maintenance-beacon.scene.json`

## Purpose

A four-position selector chooses off, lockout red, service amber, or released green on a maintenance beacon.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `beacon_position` | `DINT` | **PC** | `0` |
| `red_beacon` | `BOOL` | **PLC** | `False` |
| `amber_beacon` | `BOOL` | **PLC** | `False` |
| `green_beacon` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Advance beacon selector` | `cycle` | `beacon_position` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `beacon_position` | `beacon_selector` | `selector` |
| `red_beacon` | `maintenance_beacon` | `indicator` |
| `amber_beacon` | `maintenance_beacon` | `indicator` |
| `green_beacon` | `maintenance_beacon` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `beacon_selector` | `rotarySwitch` | Maintenance state selector |
| `maintenance_beacon` | `indicator` | Maintenance beacon |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A four-position selector chooses off, lockout red, service amber, or released green on a maintenance beacon.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command red_beacon, amber_beacon, green_beacon.

### Normal sequence

- Observe the initial input state.
- Apply beacon_position.
- Verify the PLC produces only the required red_beacon, amber_beacon, green_beacon.

### Expected observations

- Each selector position produces exactly the documented beacon state.
