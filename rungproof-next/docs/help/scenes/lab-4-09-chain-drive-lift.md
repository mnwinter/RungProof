# Lab 4.9 - Chain-Drive Lift help

Scene ID: `lab-4-09-chain-drive-lift`  
Migrated source: `prototype/scenes/lab-4-09-chain-drive-lift.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-09-chain-drive-lift.scene.json`

## Purpose

A PLC-commanded carton cycle: chain infeed, guided lift, upper receiving belt and empty-carriage return. Feedback follows actual load and lift positions.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `box_present` | `BOOL` | **PC** | `True` |
| `lift_home` | `BOOL` | **PC** | `True` |
| `destination_clear` | `BOOL` | **PC** | `True` |
| `chain_run` | `BOOL` | **PLC** | `False` |
| `lift_enable` | `BOOL` | **PLC** | `False` |
| `lift_start` | `BOOL` | **PC** | `False` |
| `carton_on_lift` | `BOOL` | **PC** | `False` |
| `lift_upper` | `BOOL` | **PC** | `False` |
| `carton_at_receiver` | `BOOL` | **PC** | `False` |
| `receiver_occupied` | `BOOL` | **PC** | `False` |
| `receiver_beam_blocked` | `BOOL` | **PC** | `False` |
| `transfer_fault` | `BOOL` | **PC** | `False` |
| `lift_lower` | `BOOL` | **PLC** | `False` |
| `cycle_active` | `BOOL` | **PLC** | `False` |
| `cycle_complete` | `BOOL` | **PLC** | `False` |
| `carton_on_infeed` | `BOOL` | **PC** | `True` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start carton cycle (momentary)` | `pulse` | `lift_start` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `box_present` | `switch_8` | `indicator` |
| `lift_home` | `switch_9` | `indicator` |
| `destination_clear` | `switch_10` | `indicator` |
| `chain_run` | `indicator_3` | `indicator` |
| `lift_enable` | `indicator_11` | `indicator` |
| `chain_run` | `conveyor_0` | `running` |
| `lift_enable` | `liftTable_1` | `running` |
| `lift_start` | `lift_start_station` | `switch` |
| `chain_run` | `receiving_conveyor` | `running` |
| `box_present` | `infeed_photoeye` | `photoeye` |
| `receiver_beam_blocked` | `receiver_photoeye` | `photoeye` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Two-strand chain infeed |
| `liftTable_1` | `liftTable` | Guided chain-driven vertical lift |
| `box_2` | `box` | Carton on the chain infeed |
| `indicator_3` | `indicator` | Chain run status |
| `training_accessory_4` | `sceneInstallation` | Fixed infeed transfer bridge |
| `training_accessory_5` | `sceneInstallation` | Fixed upper receiving bridge |
| `training_accessory_6` | `sceneInstallation` | Frame-mounted lower and upper limit switches |
| `training_accessory_7` | `sceneInstallation` | Upper receiving conveyor end stop |
| `switch_8` | `indicator` | Infeed carton feedback |
| `switch_9` | `indicator` | Lift HOME feedback |
| `switch_10` | `indicator` | Destination CLEAR feedback |
| `indicator_11` | `indicator` | Lift enable status |
| `receiving_conveyor` | `conveyor` | Upper receiving belt conveyor |
| `lift_start_station` | `switch` | Carton cycle Start (momentary) |
| `infeed_photoeye` | `photoeye` | Infeed carton optical sensor |
| `receiver_photoeye` | `photoeye` | Upper receiver entry optical sensor |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A PLC-commanded carton cycle: chain infeed, guided lift, upper receiving belt and empty-carriage return. Feedback follows actual load and lift positions.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Use a fresh Start to load at HOME, raise a fully carried carton, discharge at UPPER, then lower the empty carriage.
- Feedback is PC-owned and position-derived; PLC logic owns chain_run, lift_enable, lift_lower and cycle status.
- Stop holds every load/carriage/belt pose. Run alone must not resume; fresh Start may resume the held cycle.

### Expected observations

- Start commands horizontal loading at HOME until carton_on_lift.
- lift_enable raises the carton and carriage to lift_upper; horizontal drives stop during vertical travel.
- chain_run discharges across the upper bridge to the receiver stop; lift_lower returns the empty carriage.
- cycle_complete is a PLC result after carton_at_receiver and lift_home; invalid transfer commands latch PC transfer_fault without rewriting PLC commands.
