# Lab 10.4 - Motor Operating-State Enum help

Scene ID: `lab-10-04-motor-enum-state`  
Migrated source: `prototype/scenes/lab-10-04-motor-enum-state.plcscene`  
Scene contract: `res://scenes/migrated/lab-10-04-motor-enum-state.scene.json`

## Purpose

Editable loaded ENUM state machine with Stopped, Running and Fault states, Stop/fault priority, latched fault reset and fresh Start requirements.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `start_request` | `BOOL` | **PC** | `False` |
| `stop_request` | `BOOL` | **PC** | `False` |
| `fault_active` | `BOOL` | **PC** | `False` |
| `motor_running` | `BOOL` | **PLC** | `False` |
| `state_valid` | `BOOL` | **PLC** | `False` |
| `reset_request` | `BOOL` | **PC** | `False` |
| `motor_state` | `ENUM` | **PLC** | `Stopped` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle start request` | `toggle` | `start_request` |
| `Toggle stop request` | `toggle` | `stop_request` |
| `Toggle fault active` | `toggle` | `fault_active` |
| `Reset motor fault` | `pulse` | `reset_request` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `start_request` | `switch_1` | `switch` |
| `stop_request` | `switch_3` | `switch` |
| `fault_active` | `switch_4` | `switch` |
| `motor_running` | `indicator_2` | `indicator` |
| `state_valid` | `indicator_5` | `indicator` |
| `motor_running` | `motor_0` | `running` |
| `reset_request` | `switch_6` | `switch` |
| `motor_state` | `training_accessory_7` | `textDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `motor_0` | `motor` | Motor commanded by motor_running |
| `switch_1` | `switch` | Start request |
| `indicator_2` | `indicator` | Motor running indication |
| `switch_3` | `switch` | Stop request |
| `switch_4` | `switch` | Simulated fault active |
| `indicator_5` | `indicator` | State-valid indication |
| `switch_6` | `switch` | Reset latched motor fault |
| `training_accessory_7` | `trainingAccessory` | Named motor ENUM state |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Editable loaded ENUM state machine with Stopped, Running and Fault states, Stop/fault priority, latched fault reset and fresh Start requirements.

### Start conditions

- Verify + Load the supplied editable ENUM ladder.
- Release Start, Stop and Fault before Run; Start requires a fresh rising request.

### Normal sequence

- Start changes motor_state from Stopped to Running; the shaft follows motor_running derived from that named state.
- Stop returns Stopped. A fault enters Fault and remains latched after the fault input clears.
- Clear the fault input and press FAULT RESET, then release and press Start again.

### Expected observations

- The live state display and motor_state watch show the actual loaded ENUM value.
- Changing the Running transition MOV to Stopped, then Verify + Load, prevents the motor from running on Start.
