# Lab 10.2 - Chicken Label Print help

Scene ID: `lab-10-02-chicken-label-print`  
Migrated source: `prototype/scenes/lab-10-02-chicken-label-print.plcscene`  
Scene contract: `res://scenes/migrated/lab-10-02-chicken-label-print.scene.json`

## Purpose

Measured product weighing, PLC-authored label text, timed printing and separately commanded application. No controller is selected automatically.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `product_weighed` | `BOOL` | **PC** | `False` |
| `printer_ready` | `BOOL` | **PC** | `False` |
| `label_data_valid` | `BOOL` | **PC** | `False` |
| `print_request` | `BOOL` | **PLC** | `False` |
| `label_applied` | `BOOL` | **PLC** | `False` |
| `weight_kg` | `REAL` | **PC** | `0` |
| `product_present` | `BOOL` | **PC** | `False` |
| `print_complete` | `BOOL` | **PC** | `False` |
| `application_complete` | `BOOL` | **PC** | `False` |
| `apply_request` | `BOOL` | **PLC** | `False` |
| `label_text` | `STRING` | **PLC** | `` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Load and weigh product` | `chickenLoad` | `product_weighed` |
| `Toggle printer ready` | `toggle` | `printer_ready` |
| `Toggle label data valid` | `toggle` | `label_data_valid` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `product_weighed` | `switch_8` | `switch` |
| `printer_ready` | `switch_9` | `switch` |
| `label_data_valid` | `switch_10` | `switch` |
| `print_request` | `indicator_3` | `indicator` |
| `label_applied` | `indicator_11` | `indicator` |
| `label_text` | `training_accessory_7` | `textDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Input conveyor (static training layout) |
| `box_2` | `box` | Staged carton on input conveyor |
| `indicator_3` | `indicator` | Print request output |
| `training_accessory_4` | `trainingAccessory` | Chicken Label Print - food product load |
| `training_accessory_5` | `trainingAccessory` | Chicken Label Print - checkweigher |
| `training_accessory_6` | `trainingAccessory` | Chicken Label Print - label printer |
| `training_accessory_7` | `trainingAccessory` | Chicken Label Print - formatted label display |
| `switch_8` | `switch` | Load and weigh product |
| `switch_9` | `switch` | Toggle printer ready |
| `switch_10` | `switch` | Toggle label data valid |
| `indicator_11` | `indicator` | Label-applied output |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Measured product weighing, PLC-authored label text, timed printing and separately commanded application. No controller is selected automatically.

### Start conditions

- Load or author an offline controller; unauthored Run does not synthesize PLC commands.
- Printer-ready and data-valid fixtures begin false.

### Normal sequence

- Wait for product_weighed and require printer_ready and label_data_valid.
- Supply nonempty label_text (maximum 255 characters), then raise print_request.
- Wait for print_complete before raising apply_request; observe application_complete.

### Expected observations

- Measured weight is zero until stable, then default product measures 1.237 kg.
- Paper extrudes while printing and travels to tray only on separate application command.
- Printed text remains the exact accepted PLC text even if label_text later changes.
