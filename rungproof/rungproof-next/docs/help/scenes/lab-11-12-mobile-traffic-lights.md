# Lab 11.12 - Mobile Traffic Lights help

Scene ID: `lab-11-12-mobile-traffic-lights`  
Migrated source: `prototype/scenes/lab-11-12-mobile-traffic-lights.plcscene`  
Scene contract: `prototype/scenes/lab-11-12-mobile-traffic-lights.plcscene`

## Purpose

Two synchronized mobile signal heads coordinate a safe alternating vehicle-flow state.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `controller_ready` | `BOOL` | **PC** | `False` |
| `road_a_clear` | `BOOL` | **PC** | `False` |
| `road_b_clear` | `BOOL` | **PC** | `False` |
| `road_a_green` | `BOOL` | **PLC** | `False` |
| `road_b_green` | `BOOL` | **PLC** | `False` |
| `road_a_red` | `BOOL` | **PLC** | `False` |
| `road_a_amber` | `BOOL` | **PLC** | `False` |
| `road_b_red` | `BOOL` | **PLC** | `False` |
| `road_b_amber` | `BOOL` | **PLC** | `False` |
| `road_a_occupied` | `BOOL` | **PC** | `False` |
| `road_b_occupied` | `BOOL` | **PC** | `False` |
| `crossing_clear` | `BOOL` | **PC** | `True` |
| `signal_conflict` | `BOOL` | **PC** | `False` |
| `road_a_position_m` | `REAL` | **PC** | `-3.6` |
| `road_b_position_m` | `REAL` | **PC** | `3.6` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle controller ready` | `toggle` | `controller_ready` |
| `Toggle road a clear` | `toggle` | `road_a_clear` |
| `Toggle road b clear` | `toggle` | `road_b_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `controller_ready` | `switch_2` | `switch` |
| `road_a_clear` | `switch_6` | `switch` |
| `road_b_clear` | `switch_7` | `switch` |
| `road_a_green` | `indicator_0` | `indicator` |
| `road_b_green` | `indicator_1` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `indicator_0` | `indicator` | Mobile Traffic Lights indicator |
| `indicator_1` | `indicator` | Mobile Traffic Lights indicator |
| `switch_2` | `switch` | Mobile Traffic Lights switch |
| `training_accessory_3` | `trainingAccessory` | Mobile Traffic Lights - mobile traffic signal head |
| `training_accessory_4` | `trainingAccessory` | Mobile Traffic Lights - roadway module |
| `training_accessory_5` | `trainingAccessory` | Mobile Traffic Lights - signal synchronization link |
| `switch_6` | `switch` | Mobile Traffic Lights operator input |
| `switch_7` | `switch` | Mobile Traffic Lights operator input |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Two synchronized mobile signal heads coordinate a safe alternating vehicle-flow state.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- Only one road receives a green command at a time, and both roads stop on a fault.
