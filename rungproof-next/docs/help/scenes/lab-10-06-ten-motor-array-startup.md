# Lab 10.6 - Ten-Motor Array Startup help

Scene ID: `lab-10-06-ten-motor-array-startup`  
Migrated source: `prototype/scenes/lab-10-06-ten-motor-array-startup.plcscene`  
Scene contract: `res://scenes/migrated/lab-10-06-ten-motor-array-startup.scene.json`

## Purpose

Ten-zone conveyor BOOL ARRAY [0..9] command exercise. Plant-generated zone occupancy returns on the next accepted scan. Aggregate, independent legacy and group commands combine with OR; keep legacy/group commands false for array-only control. Write or load valid logic before Run.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `zone_occupied_0` | `BOOL` | **PC** | `True` |
| `zone_occupied_1` | `BOOL` | **PC** | `False` |
| `zone_occupied_2` | `BOOL` | **PC** | `False` |
| `zone_occupied_3` | `BOOL` | **PC** | `False` |
| `zone_occupied_4` | `BOOL` | **PC** | `False` |
| `zone_occupied_5` | `BOOL` | **PC** | `False` |
| `zone_occupied_6` | `BOOL` | **PC** | `False` |
| `zone_occupied_7` | `BOOL` | **PC** | `False` |
| `zone_occupied_8` | `BOOL` | **PC** | `False` |
| `zone_occupied_9` | `BOOL` | **PC** | `False` |
| `group_start_request` | `BOOL` | **PC** | `False` |
| `all_motors_ready` | `BOOL` | **PC** | `False` |
| `group_alarm_clear` | `BOOL` | **PC** | `False` |
| `motor_array_run` | `BOOL` | **PLC** | `False` |
| `startup_sequence_active` | `BOOL` | **PLC** | `False` |
| `motor_0_run` | `BOOL` | **PLC** | `False` |
| `motor_1_run` | `BOOL` | **PLC** | `False` |
| `motor_2_run` | `BOOL` | **PLC** | `False` |
| `motor_3_run` | `BOOL` | **PLC** | `False` |
| `motor_4_run` | `BOOL` | **PLC** | `False` |
| `motor_5_run` | `BOOL` | **PLC** | `False` |
| `motor_6_run` | `BOOL` | **PLC** | `False` |
| `motor_7_run` | `BOOL` | **PLC** | `False` |
| `motor_8_run` | `BOOL` | **PLC** | `False` |
| `motor_9_run` | `BOOL` | **PLC** | `False` |
| `motor_commands` | `ARRAY` | **PLC** | `[False, False, False, False, False, False, False, False, False, False]` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle group start request` | `toggle` | `group_start_request` |
| `Toggle all motors ready` | `toggle` | `all_motors_ready` |
| `Toggle group alarm clear` | `toggle` | `group_alarm_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `zone_occupied_0` | `zone_sensor_0` | `photoeye` |
| `zone_occupied_1` | `zone_sensor_1` | `photoeye` |
| `zone_occupied_2` | `zone_sensor_2` | `photoeye` |
| `zone_occupied_3` | `zone_sensor_3` | `photoeye` |
| `zone_occupied_4` | `zone_sensor_4` | `photoeye` |
| `zone_occupied_5` | `zone_sensor_5` | `photoeye` |
| `zone_occupied_6` | `zone_sensor_6` | `photoeye` |
| `zone_occupied_7` | `zone_sensor_7` | `photoeye` |
| `zone_occupied_8` | `zone_sensor_8` | `photoeye` |
| `zone_occupied_9` | `zone_sensor_9` | `photoeye` |
| `group_start_request` | `switch_9` | `switch` |
| `all_motors_ready` | `switch_10` | `switch` |
| `group_alarm_clear` | `switch_11` | `switch` |
| `motor_array_run` | `indicator_5` | `indicator` |
| `startup_sequence_active` | `indicator_12` | `indicator` |
| `motor_array_run` | `motor_0` | `running` |
| `motor_array_run` | `motor_1` | `running` |
| `motor_array_run` | `motor_2` | `running` |
| `motor_array_run` | `motor_3` | `running` |
| `motor_array_run` | `motor_4` | `running` |
| `motor_array_run` | `motor_5` | `running` |
| `motor_array_run` | `motor_6` | `running` |
| `motor_array_run` | `motor_7` | `running` |
| `motor_array_run` | `motor_8` | `running` |
| `motor_array_run` | `motor_9` | `running` |
| `motor_0_run` | `motor_0` | `running` |
| `motor_1_run` | `motor_1` | `running` |
| `motor_2_run` | `motor_2` | `running` |
| `motor_3_run` | `motor_3` | `running` |
| `motor_4_run` | `motor_4` | `running` |
| `motor_5_run` | `motor_5` | `running` |
| `motor_6_run` | `motor_6` | `running` |
| `motor_7_run` | `motor_7` | `running` |
| `motor_8_run` | `motor_8` | `running` |
| `motor_9_run` | `motor_9` | `running` |
| `motor_commands[0]` | `motor_0` | `running` |
| `motor_commands[1]` | `motor_1` | `running` |
| `motor_commands[2]` | `motor_2` | `running` |
| `motor_commands[3]` | `motor_3` | `running` |
| `motor_commands[4]` | `motor_4` | `running` |
| `motor_commands[5]` | `motor_5` | `running` |
| `motor_commands[6]` | `motor_6` | `running` |
| `motor_commands[7]` | `motor_7` | `running` |
| `motor_commands[8]` | `motor_8` | `running` |
| `motor_commands[9]` | `motor_9` | `running` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `zone_sensor_0` | `photoeye` | Zone 0 sensing-line occupancy |
| `zone_sensor_1` | `photoeye` | Zone 1 sensing-line occupancy |
| `zone_sensor_2` | `photoeye` | Zone 2 sensing-line occupancy |
| `zone_sensor_3` | `photoeye` | Zone 3 sensing-line occupancy |
| `zone_sensor_4` | `photoeye` | Zone 4 sensing-line occupancy |
| `zone_sensor_5` | `photoeye` | Zone 5 sensing-line occupancy |
| `zone_sensor_6` | `photoeye` | Zone 6 sensing-line occupancy |
| `zone_sensor_7` | `photoeye` | Zone 7 sensing-line occupancy |
| `zone_sensor_8` | `photoeye` | Zone 8 sensing-line occupancy |
| `zone_sensor_9` | `photoeye` | Zone 9 sensing-line occupancy |
| `motor_0` | `motor` | Motor 01 - group command |
| `motor_1` | `motor` | Motor 02 - group command |
| `motor_2` | `motor` | Motor 03 - group command |
| `motor_3` | `motor` | Motor 04 - group command |
| `motor_4` | `motor` | Motor 05 - group command |
| `motor_5` | `motor` | Motor 06 - group command |
| `motor_6` | `motor` | Motor 07 - group command |
| `motor_7` | `motor` | Motor 08 - group command |
| `motor_8` | `motor` | Motor 09 - group command |
| `motor_9` | `motor` | Motor 10 - group command |
| `indicator_5` | `indicator` | Ten-Motor Array Startup indicator |
| `training_accessory_7` | `trainingAccessory` | Group Status display - no live data |
| `training_accessory_8` | `trainingAccessory` | Start Sequence display - no live data |
| `switch_9` | `switch` | Group Start |
| `switch_10` | `switch` | All Ready |
| `switch_11` | `switch` | Alarm Clear |
| `indicator_12` | `indicator` | Ten-Motor Array Startup output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Ten conveyor zones retain independent motor IDs and BOOL bindings. Shafts and material follow accepted commands; actual occupancy returns on the next scan. Write your own logic before Run.

### Start conditions

- The current draft compiles and matches the loaded program in the offline controller.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- Accepted commands drive their corresponding conveyor zones and actual occupancy follows material motion. Valid wrong logic has its actual consequence; invalid current logic cannot execute. No automatic delay, alarm interlock or grader is implied.
