# Lab 11.6 - Wastewater Collection help

Scene ID: `lab-11-06-wastewater-collection`  
Migrated source: `prototype/scenes/lab-11-06-wastewater-collection.plcscene`  
Scene contract: `prototype/scenes/lab-11-06-wastewater-collection.plcscene`

## Purpose

Collection vessels inhibit intake when full and transfer wastewater only when the treatment path is ready.

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
| `Toggle source level high` | `toggle` | `source_level_high` |
| `Toggle treatment ready` | `toggle` | `treatment_ready` |
| `Toggle outlet clear` | `toggle` | `outlet_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `source_level_high` | `switch_10` | `switch` |
| `treatment_ready` | `switch_11` | `switch` |
| `outlet_clear` | `switch_12` | `switch` |
| `transfer_pump_run` | `indicator_5` | `indicator` |
| `outlet_valve_open` | `indicator_13` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `tank_0` | `tank` | Wastewater Collection tank |
| `tank_1` | `tank` | Wastewater Collection tank |
| `tank_2` | `tank` | Wastewater Collection tank |
| `pump_3` | `pump` | Wastewater Collection pump |
| `valve_4` | `valve` | Wastewater Collection valve |
| `indicator_5` | `indicator` | Wastewater Collection indicator |
| `training_accessory_6` | `trainingAccessory` | Wastewater Collection - collection tank bank |
| `training_accessory_7` | `trainingAccessory` | Wastewater Collection - level transmitters |
| `training_accessory_8` | `trainingAccessory` | Wastewater Collection - pipe manifold |
| `training_accessory_9` | `trainingAccessory` | Wastewater Collection - alarm beacon |
| `switch_10` | `switch` | Wastewater Collection operator input |
| `switch_11` | `switch` | Wastewater Collection operator input |
| `switch_12` | `switch` | Wastewater Collection operator input |
| `indicator_13` | `indicator` | Wastewater Collection output indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Collection vessels inhibit intake when full and transfer wastewater only when the treatment path is ready.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- The pump and outlet valve run only when a source requires service and treatment is ready.
