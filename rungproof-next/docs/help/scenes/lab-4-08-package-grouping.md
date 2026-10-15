# Lab 4.8 - Package Grouping Station help

Scene ID: `lab-4-08-package-grouping`  
Migrated source: `prototype/scenes/lab-4-08-package-grouping.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-08-package-grouping.scene.json`

## Purpose

Original offline three-carton grouping exercise: actual optical count feedback, guided accumulation stop and a connected powered receiving surface.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `machine_enabled` | `BOOL` | **PC** | `False` |
| `release_clear` | `BOOL` | **PC** | `False` |
| `package_detected` | `BOOL` | **PC** | `False` |
| `group_staged` | `BOOL` | **PC** | `False` |
| `stop_raised` | `BOOL` | **PC** | `False` |
| `receiver_occupied` | `BOOL` | **PC** | `False` |
| `receiver_detected` | `BOOL` | **PC** | `False` |
| `transfer_complete` | `BOOL` | **PC** | `False` |
| `stop_lowered` | `BOOL` | **PC** | `True` |
| `stop_position` | `REAL` | **PC** | `0` |
| `group_conveyor_run` | `BOOL` | **PLC** | `False` |
| `group_release` | `BOOL` | **PLC** | `False` |
| `group_ready` | `BOOL` | **PLC** | `False` |
| `group_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle conveyor enable` | `toggle` | `machine_enabled` |
| `Toggle downstream clear` | `toggle` | `release_clear` |
| `Load one carton (idle feed)` | `groupingLoad` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `machine_enabled` | `enable_switch` | `switch` |
| `release_clear` | `clear_switch` | `switch` |
| `package_detected` | `entry_photoeye` | `photoeye` |
| `receiver_detected` | `receiver_photoeye` | `photoeye` |
| `group_ready` | `group_ready_lamp` | `indicator` |
| `group_count` | `group_count_display` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `group_line` | `trainingAccessory` | Aligned powered infeed and receiving rollers |
| `carton_0` | `box` | Grouping carton 1 |
| `carton_1` | `box` | Grouping carton 2 |
| `carton_2` | `box` | Grouping carton 3 |
| `group_stop` | `trainingAccessory` | Guided carton group stop |
| `entry_photoeye` | `photoeye` | Actual incoming carton beam |
| `receiver_photoeye` | `photoeye` | Actual receiving beam |
| `group_count_display` | `trainingAccessory` | PLC incoming group count |
| `group_ready_lamp` | `indicator` | PLC target group count reached |
| `enable_switch` | `switch` | Held conveyor enable |
| `clear_switch` | `switch` | Held downstream clear |
| `load_button` | `switch` | Load one carton into the idle infeed |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Original offline three-carton grouping exercise: actual optical count feedback, guided accumulation stop and a connected powered receiving surface.

### Start conditions

- Open an explicitly authored PLC project; the default exercise editor remains empty.
- Run, set conveyor enable and downstream clear, then LOAD one carton.

### Normal sequence

- Wait for the loaded carton to reach its accumulation position before loading another.
- Three actual incoming beam edges give PLC count three; actual staged-group feedback permits stop retraction.
- Watch all three cartons move onto the connected powered receiver and remain visible.

### Expected observations

- The readout follows PLC CTU count from actual beam feedback, not a PC precomputed ready toggle.
- Cartons remain supported on aligned infeed/receiver rollers throughout the transfer.
- Prescribed accumulation positions exclude slip, contact forces and collision dynamics; there is no hardware safety or commissioning claim.
