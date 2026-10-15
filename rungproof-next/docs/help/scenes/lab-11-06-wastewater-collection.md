# Lab 11.6 - Wastewater Collection help

Scene ID: `lab-11-06-wastewater-collection`  
Migrated source: `prototype/scenes/lab-11-06-wastewater-collection.plcscene`  
Scene contract: `res://scenes/migrated/lab-11-06-wastewater-collection.scene.json`

## Purpose

Illustrative four-vessel common-header wastewater bank with normalized inflow/transfer, hysteretic source-high feedback and modeled 4-20 mA level. PLC logic owns pump/valve commands; rates are not calibrated hydraulics.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `source_level_high` | `BOOL` | **PC** | `False` |
| `treatment_ready` | `BOOL` | **PC** | `False` |
| `outlet_clear` | `BOOL` | **PC** | `False` |
| `transfer_pump_run` | `BOOL` | **PLC** | `False` |
| `outlet_valve_open` | `BOOL` | **PLC** | `False` |
| `inflow_enabled` | `BOOL` | **PC** | `False` |
| `source_level_percent` | `REAL` | **PC** | `35.0` |
| `source_level_ma` | `REAL` | **PC** | `9.6` |
| `source_empty` | `BOOL` | **PC** | `False` |
| `source_full` | `BOOL` | **PC** | `False` |
| `actual_inflow_percent_per_second` | `REAL` | **PC** | `0.0` |
| `actual_transfer_percent_per_second` | `REAL` | **PC** | `0.0` |
| `overflow_percent_per_second` | `REAL` | **PC** | `0.0` |
| `transfer_inhibited` | `BOOL` | **PC** | `False` |
| `outlet_valve_position_percent` | `REAL` | **PC** | `0.0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle incoming wastewater` | `toggle` | `inflow_enabled` |
| `Toggle treatment ready` | `toggle` | `treatment_ready` |
| `Toggle outlet clear` | `toggle` | `outlet_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `inflow_enabled` | `switch_10` | `switch` |
| `treatment_ready` | `switch_11` | `switch` |
| `outlet_clear` | `switch_12` | `switch` |
| `transfer_pump_run` | `indicator_5` | `indicator` |
| `outlet_valve_open` | `indicator_13` | `indicator` |
| `transfer_pump_run` | `pump_3` | `running` |
| `outlet_valve_open` | `valve_4` | `position` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `tank_0` | `tank` | Wastewater Collection tank |
| `tank_1` | `tank` | Wastewater Collection tank |
| `tank_2` | `tank` | Wastewater Collection tank |
| `pump_3` | `pump` | Wastewater Collection pump |
| `valve_4` | `valve` | Wastewater Collection valve |
| `indicator_5` | `indicator` | Wastewater Collection indicator |
| `training_accessory_6` | `trainingAccessory` | Collection vessel 4 - full-size tank |
| `training_accessory_7` | `levelSensor` | Source bank modeled analog level probe |
| `training_accessory_8` | `sceneInstallation` | Wastewater process collector and discharge piping |
| `training_accessory_9` | `trainingAccessory` | Wastewater Collection - alarm beacon |
| `switch_10` | `switch` | Incoming wastewater fixture |
| `switch_11` | `switch` | Wastewater Collection operator input |
| `switch_12` | `switch` | Wastewater Collection operator input |
| `indicator_13` | `indicator` | Wastewater Collection output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Illustrative four-vessel common-header wastewater bank with normalized inflow/transfer, hysteretic source-high feedback and modeled 4-20 mA level. PLC logic owns pump/valve commands; rates are not calibrated hydraulics.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Enable inflow and observe normalized bank level and modeled 4-20 mA feedback rise.
- With treatment ready and outlet clear, PLC logic commands transfer from high to low threshold.
- Disable inflow to drain the bank; inspect hysteretic source-high drop at 20%.

### Expected observations

- All four equal-size vessels share the common-header bank level.
- Actual transfer depends on pump command, delivered valve opening and both downstream permissives.
- A full bank clamps at 100% and reports rejected inflow as overflow; no external receiving tank is modeled.
