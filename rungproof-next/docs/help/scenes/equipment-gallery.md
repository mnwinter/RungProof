# Reusable Equipment Gallery help

Scene ID: `equipment-gallery`  
Migrated source: `prototype/scenes/equipment-gallery.json`  
Scene contract: `res://scenes/migrated/equipment-gallery.scene.json`

## Purpose

A visual inventory of reusable catalog models created by the runtime scene composer. Level instruments are shown on illustrative display stands.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `gallery_animation` | `BOOL` | **SIM** | `False` |
| `industrial_fan_run` | `BOOL` | **SIM** | `False` |
| `emergency_stop` | `BOOL` | **SIM** | `False` |
| `demo_tank_level` | `REAL` | **SIM** | `50` |
| `demo_level_signal` | `REAL` | **SIM** | `12` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start / stop gallery motion` | `toggle` | `gallery_animation` |
| `Emergency stop gallery motion` | `stop` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `gallery_animation` | `gallery_motor` | `running` |
| `gallery_animation` | `gallery_conveyor` | `running` |
| `gallery_animation` | `gallery_fan` | `running` |
| `gallery_animation` | `gallery_machine` | `running` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `gallery_motor` | `motor` | AC motor |
| `gallery_conveyor` | `conveyor` | Belt conveyor |
| `gallery_box` | `box` | Product carton |
| `gallery_photoeye` | `photoeye` | Through-beam photoeye |
| `gallery_switch` | `switch` | Operator pushbutton |
| `gallery_estop` | `switch` | Emergency stop |
| `gallery_indicator` | `indicator` | Three-color stack light |
| `gallery_pump` | `pump` | Centrifugal pump |
| `gallery_tank` | `tank` | Level tank |
| `gallery_low_sensor` | `levelSensor` | Discrete level switch |
| `gallery_transmitter` | `levelSensor` | 4–20 mA level transmitter |
| `gallery_pipe` | `pipe` | Flanged process pipe |
| `gallery_fan` | `fan` | Industrial ventilation fan |
| `gallery_selector` | `rotarySwitch` | Four-position selector |
| `gallery_lift` | `liftTable` | Scissor lift table |
| `gallery_valve` | `valve` | Animated process valve |
| `gallery_drill` | `drillPress` | Drill press |
| `gallery_robot` | `robotArm` | Articulated robot |
| `gallery_shutter` | `rollerShutter` | Roller shutter |
| `gallery_turntable` | `rotaryTable` | Indexed rotary table |
| `gallery_machine` | `machine` | Enclosed process machine |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `gallery_animation` | `False` |
| `industrial_fan_run` | `False` |
| `emergency_stop` | `True` |
