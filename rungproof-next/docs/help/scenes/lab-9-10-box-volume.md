# Lab 9.10 - Box Volume Calculation help

Scene ID: `lab-9-10-box-volume`  
Migrated source: `prototype/scenes/lab-9-10-box-volume.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-10-box-volume.scene.json`

## Purpose

Set manual length, width and height in mm, then confirm each validity flag. Your loaded ladder program calculates volume in mm3. The fixture is static.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `length_valid` | `BOOL` | **PC** | `False` |
| `width_valid` | `BOOL` | **PC** | `False` |
| `height_valid` | `BOOL` | **PC** | `False` |
| `volume_result_valid` | `BOOL` | **PLC** | `False` |
| `length_mm` | `DINT` | **PC** | `0` |
| `width_mm` | `DINT` | **PC** | `0` |
| `height_mm` | `DINT` | **PC** | `0` |
| `volume_mm3` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle length valid` | `toggle` | `length_valid` |
| `Toggle width valid` | `toggle` | `width_valid` |
| `Toggle height valid` | `toggle` | `height_valid` |
| `Next length (mm)` | `cycle` | `length_mm` |
| `Next width (mm)` | `cycle` | `width_mm` |
| `Next height (mm)` | `cycle` | `height_mm` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `length_valid` | `switch_5` | `switch` |
| `width_valid` | `switch_6` | `switch` |
| `height_valid` | `switch_7` | `switch` |
| `volume_result_valid` | `indicator_2` | `indicator` |
| `length_mm` | `numeric_display_0` | `numericDisplay` |
| `width_mm` | `numeric_display_1` | `numericDisplay` |
| `height_mm` | `numeric_display_2` | `numericDisplay` |
| `volume_mm3` | `training_accessory_4` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `box_0` | `box` | Box Volume Calculation box |
| `indicator_2` | `indicator` | Box Volume Calculation indicator |
| `training_accessory_3` | `trainingAccessory` | Static dimension fixture with carrying bench |
| `switch_5` | `switch` | Length valid manual input |
| `switch_6` | `switch` | Width valid manual input |
| `switch_7` | `switch` | Height valid manual input |
| `numeric_display_0` | `trainingAccessory` | L mm NEXT live numeric readout |
| `numeric_display_1` | `trainingAccessory` | W mm NEXT live numeric readout |
| `numeric_display_2` | `trainingAccessory` | H mm NEXT live numeric readout |
| `training_accessory_4` | `trainingAccessory` | VOLUME mm3 live numeric readout |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Set manual length, width and height in mm, then confirm each validity flag. Your loaded ladder program calculates volume in mm3. The fixture is static.

### Start conditions

- Local offline runtime selected; load the reference or author a program.

### Normal sequence

- Open programs/examples/09-box-volume-reference.rpproj.json with Project > Open, verify and load offline, then Run.
- Set length=850, width=720, height=720 mm and enable all three validity flags. VOLUME mm3 displays 440640000 and its lamp turns green.

### Expected observations

- All dimensions must be positive and at most 1000 mm. Qualified volume is length*width*height in mm3; 1000*1000*1000=1000000000 fits DINT.
- The carton remains supported on the bench; its three heads are static mounting illustrations, not simulated measuring sensors.
- An empty exercise does not calculate until a program is loaded.
