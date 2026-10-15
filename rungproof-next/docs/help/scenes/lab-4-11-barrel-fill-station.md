# Lab 4.11 - Barrel Fill Station help

Scene ID: `lab-4-11-barrel-fill-station`  
Migrated source: `prototype/scenes/lab-4-11-barrel-fill-station.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-11-barrel-fill-station.scene.json`

## Purpose

One supported barrel indexes under a connected nozzle, receives a measured 150 L batch and remains on the outfeed.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `barrel_at_fill` | `BOOL` | **PC** | `False` |
| `fill_complete` | `BOOL` | **PC** | `False` |
| `downstream_clear` | `BOOL` | **PC** | `True` |
| `barrel_parked` | `BOOL` | **PC** | `False` |
| `fill_beam_blocked` | `BOOL` | **PC** | `False` |
| `exit_beam_blocked` | `BOOL` | **PC** | `False` |
| `fill_fault` | `BOOL` | **PC** | `False` |
| `batch_start` | `BOOL` | **PC** | `False` |
| `barrel_litres` | `REAL` | **PC** | `0.0` |
| `source_litres` | `REAL` | **PC** | `200.0` |
| `flow_lps` | `REAL` | **PC** | `0.0` |
| `infeed_run` | `BOOL` | **PLC** | `False` |
| `fill_valve_open` | `BOOL` | **PLC** | `False` |
| `cycle_active` | `BOOL` | **PLC** | `False` |
| `cycle_complete` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start / resume barrel batch` | `pulse` | `batch_start` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `infeed_run` | `conveyor_0` | `running` |
| `infeed_run` | `indicator_4` | `indicator` |
| `fill_valve_open` | `indicator_11` | `indicator` |
| `fill_beam_blocked` | `photoeye_3` | `photoeye` |
| `exit_beam_blocked` | `exit_photoeye` | `photoeye` |
| `barrel_litres` | `training_accessory_6` | `numericDisplay` |
| `batch_start` | `switch_8` | `switch` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Indexed barrel conveyor |
| `tank_1` | `tank` | Elevated 200 L source cutaway |
| `valve_2` | `valve` | Connected quarter-turn fill valve |
| `photoeye_3` | `photoeye` | Fill-entry photoeye |
| `exit_photoeye` | `photoeye` | Outfeed occupancy photoeye |
| `indicator_4` | `indicator` | Feed command |
| `training_accessory_5` | `trainingAccessory` | Open-top barrel cutaway |
| `training_accessory_6` | `trainingAccessory` | Connected volumetric meter |
| `training_accessory_7` | `trainingAccessory` | Supported overhead fill nozzle |
| `switch_8` | `switch` | Start / resume barrel batch |
| `indicator_11` | `indicator` | Fill valve command |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

One supported barrel indexes under a connected nozzle, receives a measured 150 L batch and remains on the outfeed.

### Start conditions

- Open the editable reference or author equivalent ladder.
- Run starts offline scans; fresh Start authorizes the finite batch.

### Normal sequence

- Index the supported empty barrel at X=0.
- Transfer liquid at prescribed 20 L/s until 150 L is measured.
- Close fill, discharge while authorized and retain the barrel at X=3.

### Expected observations

- Litres are conserved; source plus barrel remains 200 L.
- No hydraulic head, valve transit, fluid slosh, slip or replenishment is modeled.
