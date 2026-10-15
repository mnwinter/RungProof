# Lab 5.8 - Drawbridge Control help

Scene ID: `lab-5-08-drawbridge-control`  
Migrated source: `prototype/scenes/lab-5-08-drawbridge-control.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-08-drawbridge-control.scene.json`

## Purpose

Traffic barriers close before the hinged bridge raises; actual home feedback interlocks barrier opening and traffic release.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `traffic_stopped` | `BOOL` | **PC** | `False` |
| `bridge_request` | `BOOL` | **PC** | `False` |
| `bridge_home` | `BOOL` | **PC** | `True` |
| `bridge_raised` | `BOOL` | **PC** | `False` |
| `barriers_closed` | `BOOL` | **PC** | `True` |
| `barriers_open` | `BOOL` | **PC** | `False` |
| `motion_inhibited` | `BOOL` | **PC** | `False` |
| `bridge_angle` | `REAL` | **PC** | `0` |
| `barrier_angle` | `REAL` | **PC** | `0` |
| `bridge_raise` | `BOOL` | **PLC** | `False` |
| `bridge_lower` | `BOOL` | **PLC** | `False` |
| `barrier_close` | `BOOL` | **PLC** | `False` |
| `barrier_open` | `BOOL` | **PLC** | `False` |
| `traffic_release` | `BOOL` | **PLC** | `False` |
| `traffic_stop` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Turn BRIDGE CLOSE / OPEN (maintained)` | `toggle` | `bridge_request` |
| `Set manual STOPPED feedback NO / YES` | `toggle` | `traffic_stopped` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `bridge_request` | `switch_6` | `selector` |
| `traffic_stopped` | `switch_2` | `selector` |
| `bridge_home` | `training_accessory_5` | `indicatorChannel` |
| `bridge_raised` | `training_accessory_5` | `indicatorChannel` |
| `traffic_stop` | `indicator_1` | `indicatorChannel` |
| `traffic_release` | `indicator_1` | `indicatorChannel` |
| `traffic_stop` | `indicator_8` | `indicatorChannel` |
| `traffic_release` | `indicator_8` | `indicatorChannel` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `training_accessory_3` | `trainingAccessory` | Hinged bridge, supported approaches and channel |
| `training_accessory_4` | `trainingAccessory` | West approach hinged traffic barrier |
| `barrier_east` | `trainingAccessory` | East approach hinged traffic barrier |
| `training_accessory_5` | `trainingAccessory` | Bridge cam, HOME and RAISED roller switches |
| `indicator_1` | `trainingAccessory` | West approach red / green traffic signal |
| `indicator_8` | `trainingAccessory` | East approach red / green traffic signal |
| `switch_6` | `rotarySwitch` | Maintained bridge request CLOSE / OPEN |
| `switch_2` | `rotarySwitch` | Manual traffic stopped feedback NO / YES |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Traffic barriers close before the hinged bridge raises; actual home feedback interlocks barrier opening and traffic release.

### Start conditions

- Blank exercise: horizontal HOME, both barriers down, PLC commands false and no movement.
- Built-in offline controller with user-authored logic or explicitly opened .tools/plant-review-drawbridge.rpproj.json after --audit-drawbridge.

### Normal sequence

- Run the six-rung reference. CLOSE at HOME opens the barriers, then releases green.
- Select OPEN, acknowledge STOPPED YES; barriers close in 2 s, bridge raises in 4 s to 70 degrees.
- Select CLOSE: bridge lowers in 4 s to HOME, barriers open in 2 s, then green releases.

### Expected observations

- Bridge deck and limit cam share their actual hinge angle; HOME and RAISED lamps follow limits, not elapsed timers.
- The manual traffic-stopped input is an operator simulation acknowledgement; no vehicles, boats or traffic occupancy detector are simulated.
- Original geometry/travel times do not establish load capacity, actuator sizing, road compliance or real bridge safety.
