# Lab 11.11 - Service Elevator help

Scene ID: `lab-11-11-service-elevator`  
Migrated source: `prototype/scenes/lab-11-11-service-elevator.plcscene`  
Scene contract: `prototype/scenes/lab-11-11-service-elevator.plcscene`

## Purpose

A lift travels between two landings only with door, position, and direction interlocks satisfied.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `call_valid` | `BOOL` | **PC** | `False` |
| `doors_closed` | `BOOL` | **PC** | `False` |
| `landing_clear` | `BOOL` | **PC** | `False` |
| `lift_up_cmd` | `BOOL` | **PLC** | `False` |
| `lift_down_cmd` | `BOOL` | **PLC** | `False` |
| `car_position_pct` | `REAL` | **PC** | `0` |
| `at_lower_landing` | `BOOL` | **PC** | `True` |
| `at_upper_landing` | `BOOL` | **PC** | `False` |
| `direction_conflict` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle call valid` | `toggle` | `call_valid` |
| `Toggle doors closed` | `toggle` | `doors_closed` |
| `Toggle landing clear` | `toggle` | `landing_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `call_valid` | `switch_2` | `switch` |
| `doors_closed` | `switch_8` | `switch` |
| `landing_clear` | `switch_9` | `switch` |
| `lift_up_cmd` | `indicator_3` | `indicator` |
| `lift_down_cmd` | `indicator_10` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `liftTable_0` | `liftTable` | Service Elevator lift Table |
| `rollerShutter_1` | `rollerShutter` | Service Elevator roller Shutter |
| `switch_2` | `switch` | Service Elevator switch |
| `indicator_3` | `indicator` | Service Elevator indicator |
| `training_accessory_4` | `trainingAccessory` | Service Elevator - elevator car/shaft |
| `training_accessory_5` | `trainingAccessory` | Service Elevator - floor call station |
| `training_accessory_6` | `trainingAccessory` | Service Elevator - landing door |
| `training_accessory_7` | `trainingAccessory` | Service Elevator - floor-position sensor |
| `switch_8` | `switch` | Service Elevator operator input |
| `switch_9` | `switch` | Service Elevator operator input |
| `indicator_10` | `indicator` | Service Elevator output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A lift travels between two landings only with door, position, and direction interlocks satisfied.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- The elevator receives one direction command only when the doors are closed and the landing is clear.
