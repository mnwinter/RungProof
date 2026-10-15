# Lab 10.1 - Drive Alarm-Code String help

Scene ID: `lab-10-01-drive-alarm-code-string`  
Migrated source: `prototype/scenes/lab-10-01-drive-alarm-code-string.plcscene`  
Scene contract: `res://scenes/migrated/lab-10-01-drive-alarm-code-string.scene.json`

## Purpose

Editable loaded STRING ladder matches complete drive alarm codes in received text; validity and Reset gate the alarm outputs. Offline symbolic message fixtures.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `drive_alarm_string_valid` | `BOOL` | **PC** | `False` |
| `alarm_code_found` | `BOOL` | **PLC** | `False` |
| `alarm_reset` | `BOOL` | **PC** | `False` |
| `drive_alarm_active` | `BOOL` | **PLC** | `False` |
| `alarm_match_valid` | `BOOL` | **PLC** | `False` |
| `drive_alarm_text` | `STRING` | **PC** | `` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle drive alarm string valid` | `toggle` | `drive_alarm_string_valid` |
| `Next received alarm message` | `cycle` | `drive_alarm_text` |
| `Toggle alarm reset` | `toggle` | `alarm_reset` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `drive_alarm_string_valid` | `switch_6` | `switch` |
| `alarm_reset` | `switch_8` | `switch` |
| `drive_alarm_active` | `indicator_2` | `indicator` |
| `alarm_match_valid` | `indicator_9` | `indicator` |
| `drive_alarm_active` | `training_accessory_5` | `indicator` |
| `drive_alarm_text` | `training_accessory_4` | `textDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `motor_0` | `motor` | Drive Alarm-Code String motor |
| `machine_1` | `machine` | Drive Alarm-Code String machine |
| `indicator_2` | `indicator` | Drive alarm output |
| `training_accessory_3` | `trainingAccessory` | Drive Alarm-Code String - VFD diagnostic panel |
| `training_accessory_4` | `trainingAccessory` | Drive Alarm-Code String - fieldbus alarm-string display |
| `training_accessory_5` | `trainingAccessory` | Drive Alarm-Code String - drive status indicator |
| `switch_6` | `switch` | MESSAGE VALID |
| `switch_7` | `switch` | Cycle simulated received alarm message |
| `switch_8` | `switch` | RESET |
| `indicator_9` | `indicator` | Alarm code match output |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Editable loaded STRING ladder matches complete drive alarm codes in received text; validity and Reset gate the alarm outputs. Offline symbolic message fixtures.

### Start conditions

- Verify + Load the supplied editable STRING ladder.
- Run, enable MESSAGE VALID, then cycle received messages.

### Normal sequence

- F003 matches the default selected_alarm_code; F030, F0030, prefixes, lowercase and empty messages do not.
- Edit selected_alarm_code in the tag table, Apply then Verify + Load to select a different code.

### Expected observations

- The live display shows received STRING text; alarm_code_found and both alarm lamps are derived by the loaded program.
