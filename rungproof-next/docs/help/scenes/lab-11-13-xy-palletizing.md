# Lab 11.13 - XY Palletizing Cell help

Scene ID: `lab-11-13-xy-palletizing`  
Migrated source: `prototype/scenes/lab-11-13-xy-palletizing.plcscene`  
Scene contract: `res://scenes/migrated/lab-11-13-xy-palletizing.scene.json`

## Purpose

Four-carton palletizing lesson: one Start runs a complete gantry batch using simulated three-axis encoder feedback, PLC-commanded vacuum, placement feedback and home return.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `system_enable` | `BOOL` | **PC** | `True` |
| `start_command` | `BOOL` | **PC** | `False` |
| `stop_command` | `BOOL` | **PC** | `False` |
| `reset_command` | `BOOL` | **PC** | `False` |
| `safety_trip` | `BOOL` | **PC** | `False` |
| `carton_at_pick` | `BOOL` | **PC** | `True` |
| `gantry_home` | `BOOL` | **PC** | `True` |
| `pallet_position_valid` | `BOOL` | **PC** | `True` |
| `gantry_x_positive` | `BOOL` | **PLC** | `False` |
| `gantry_x_negative` | `BOOL` | **PLC** | `False` |
| `gantry_y_positive` | `BOOL` | **PLC** | `False` |
| `gantry_y_negative` | `BOOL` | **PLC** | `False` |
| `gantry_z_positive` | `BOOL` | **PLC** | `False` |
| `gantry_z_negative` | `BOOL` | **PLC** | `False` |
| `vacuum_pick` | `BOOL` | **PLC** | `False` |
| `palletizer_reset` | `BOOL` | **PLC** | `False` |
| `cell_active` | `BOOL` | **PLC** | `False` |
| `layer_complete` | `BOOL` | **PLC** | `False` |
| `at_pickup` | `BOOL` | **PC** | `False` |
| `at_place` | `BOOL` | **PC** | `False` |
| `carton_attached` | `BOOL` | **PC** | `False` |
| `pick_complete` | `BOOL` | **PC** | `False` |
| `cycle_in_progress` | `BOOL` | **PC** | `False` |
| `palletizer_fault` | `BOOL` | **PC** | `False` |
| `placed_cartons` | `DINT` | **PC** | `0` |
| `gantry_x_encoder_counts` | `DINT` | **PC** | `0` |
| `gantry_y_encoder_counts` | `DINT` | **PC** | `0` |
| `gantry_z_encoder_counts` | `DINT` | **PC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle system enable` | `toggle` | `system_enable` |
| `Start / resume four-carton batch` | `pulse` | `start_command` |
| `Machine Stop` | `pulse` | `stop_command` |
| `Machine Reset` | `pulse` | `reset_command` |
| `Toggle simulated safety trip (training only)` | `toggle` | `safety_trip` |
| `Toggle pallet permissive` | `toggle` | `pallet_position_valid` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `system_enable` | `switch_10` | `switch` |
| `pallet_position_valid` | `switch_11` | `switch` |
| `vacuum_pick` | `indicator_3` | `indicator` |
| `cell_active` | `indicator_12` | `indicator` |
| `layer_complete` | `indicator_13` | `indicator` |
| `start_command` | `switch_9` | `switch` |
| `stop_command` | `switch_14` | `switch` |
| `reset_command` | `switch_15` | `switch` |
| `safety_trip` | `switch_16` | `switch` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_1` | `conveyor` | XY Palletizing Cell conveyor |
| `box_2` | `box` | XY Palletizing Cell box |
| `indicator_3` | `indicator` | XY Palletizing Cell indicator |
| `training_accessory_4` | `trainingAccessory` | XY Palletizing Cell - XY gantry |
| `training_accessory_6` | `trainingAccessory` | XY Palletizing Cell - pallet magazine |
| `training_accessory_7` | `trainingAccessory` | XY Palletizing Cell - carton load |
| `training_accessory_8` | `trainingAccessory` | Conveyor-to-gantry pickup table |
| `switch_10` | `switch` | Palletizer system enable |
| `switch_9` | `switch` | XY Palletizing Cell operator input |
| `switch_14` | `switch` | Palletizer machine Stop |
| `switch_15` | `switch` | Palletizer machine Reset |
| `switch_16` | `switch` | Simulated safety status - training only |
| `switch_11` | `switch` | XY Palletizing Cell operator input |
| `indicator_12` | `indicator` | XY Palletizing Cell output indication |
| `indicator_13` | `indicator` | XY Palletizing Cell output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Four-carton palletizing lesson: one Start runs a complete gantry batch with simulated encoder feedback, PLC-commanded vacuum, placement feedback and home return.

### Start conditions

- Select the built-in offline controller; no PLC connection is required.
- System Enable and pallet permissive are true, simulated trip is clear, the pickup carton is present, and all measured axes are home.

### Normal sequence

- Run, then press Start once to begin the four-carton batch.
- After each home return, the carton-load supply stages the next carton at the supported pickup station until all four positions are filled.
- The gantry lowers onto the carton, dwells 750 ms, picks up, carries to the next pallet slot, releases on the pallet, and returns home.
- The layer-complete lamp follows the fourth completed home return. The X/Y/Z encoder-count watch values track simulated position relative to pickup home.

### Expected observations

- The pickup carton remains on a table until attached. Carried cartons follow the tool; released cartons remain on four separate pallet slots.
- Encoder counts change with simulated travel. Home is generated from the completed return; the placement counter advances after release and home return.
