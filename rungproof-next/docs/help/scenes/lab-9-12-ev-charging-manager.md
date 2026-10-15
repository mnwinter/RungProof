# Lab 9.12 - EV Charging Manager help

Scene ID: `lab-9-12-ev-charging-manager`  
Migrated source: `prototype/scenes/lab-9-12-ev-charging-manager.plcscene`  
Scene contract: `res://scenes/migrated/lab-9-12-ev-charging-manager.scene.json`

## Purpose

A shared controller allocates charging permission and accumulates energy pulse events for occupied bays.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `bay_occupied` | `BOOL` | **PC** | `False` |
| `customer_authorized` | `BOOL` | **PC** | `False` |
| `charger_ready` | `BOOL` | **PC** | `False` |
| `charge_enable` | `BOOL` | **PLC** | `False` |
| `energy_session_active` | `BOOL` | **PLC** | `False` |
| `connector_inserted` | `BOOL` | **PC** | `False` |
| `connector_connected` | `BOOL` | **PC** | `False` |
| `energy_pulse` | `BOOL` | **PC** | `False` |
| `delivered_kw` | `REAL` | **PC** | `0` |
| `meter_energy_kwh` | `REAL` | **PC** | `0` |
| `meter_pulse_count` | `DINT` | **PC** | `0` |
| `allocated_kw` | `REAL` | **PLC** | `0` |
| `energy_count` | `DINT` | **PLC** | `0` |
| `energy_kwh` | `REAL` | **PLC** | `0` |
| `bay_b_occupied` | `BOOL` | **PC** | `False` |
| `bay_b_customer_authorized` | `BOOL` | **PC** | `False` |
| `bay_b_charger_ready` | `BOOL` | **PC** | `False` |
| `bay_b_charge_enable` | `BOOL` | **PLC** | `False` |
| `bay_b_energy_session_active` | `BOOL` | **PLC** | `False` |
| `bay_b_connector_inserted` | `BOOL` | **PC** | `False` |
| `bay_b_connector_connected` | `BOOL` | **PC** | `False` |
| `bay_b_energy_pulse` | `BOOL` | **PC** | `False` |
| `bay_b_delivered_kw` | `REAL` | **PC** | `0` |
| `bay_b_meter_energy_kwh` | `REAL` | **PC** | `0` |
| `bay_b_meter_pulse_count` | `DINT` | **PC** | `0` |
| `bay_b_allocated_kw` | `REAL` | **PLC** | `0` |
| `bay_b_energy_count` | `DINT` | **PLC** | `0` |
| `bay_b_energy_kwh` | `REAL` | **PLC** | `0` |
| `allocation_inhibited` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle bay occupied` | `toggle` | `bay_occupied` |
| `Toggle customer authorized` | `toggle` | `customer_authorized` |
| `Toggle charger ready` | `toggle` | `charger_ready` |
| `A connector fixture` | `toggle` | `connector_inserted` |
| `B occupancy fixture` | `toggle` | `bay_b_occupied` |
| `B authorization fixture` | `toggle` | `bay_b_customer_authorized` |
| `B ready fixture` | `toggle` | `bay_b_charger_ready` |
| `B connector fixture` | `toggle` | `bay_b_connector_inserted` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `bay_occupied` | `switch_1` | `switch` |
| `customer_authorized` | `switch_8` | `switch` |
| `charger_ready` | `switch_9` | `switch` |
| `charge_enable` | `indicator_2` | `indicator` |
| `energy_session_active` | `indicator_10` | `indicator` |
| `connector_inserted` | `fixture_connector_inserted` | `switch` |
| `bay_b_occupied` | `fixture_bay_b_occupied` | `switch` |
| `bay_b_customer_authorized` | `fixture_bay_b_customer_authorized` | `switch` |
| `bay_b_charger_ready` | `fixture_bay_b_charger_ready` | `switch` |
| `bay_b_connector_inserted` | `fixture_bay_b_connector_inserted` | `switch` |
| `energy_kwh` | `training_accessory_5` | `numericDisplay` |
| `bay_b_energy_kwh` | `ev_meter_b` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_1` | `switch` | Bay A occupancy fixture |
| `indicator_2` | `indicator` | EV Charging Manager indicator |
| `training_accessory_3` | `trainingAccessory` | Bay A vehicle and illustrative charger |
| `training_accessory_4` | `trainingAccessory` | Bay A connector at vehicle inlet |
| `training_accessory_5` | `trainingAccessory` | Bay A energy display - PLC accumulated kWh |
| `training_accessory_6` | `trainingAccessory` | Bay A pulse module (static until connected) |
| `training_accessory_7` | `trainingAccessory` | Bay A authorization reader |
| `switch_8` | `switch` | Bay A authorization fixture |
| `switch_9` | `switch` | Bay A charger-ready fixture |
| `indicator_10` | `indicator` | EV Charging Manager output indication |
| `ev_bay_b` | `trainingAccessory` | Bay B vehicle and illustrative charger |
| `ev_connector_b` | `trainingAccessory` | Bay B connector at vehicle inlet |
| `ev_meter_b` | `trainingAccessory` | Bay B energy display - PLC accumulated kWh |
| `ev_pulse_b` | `trainingAccessory` | Bay B pulse module (static until connected) |
| `ev_reader_b` | `trainingAccessory` | Bay B authorization reader |
| `fixture_connector_inserted` | `switch` | A connector fixture |
| `fixture_bay_b_occupied` | `switch` | B occupancy fixture |
| `fixture_bay_b_customer_authorized` | `switch` | B authorization fixture |
| `fixture_bay_b_charger_ready` | `switch` | B ready fixture |
| `fixture_bay_b_connector_inserted` | `switch` | B connector fixture |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A shared controller allocates charging permission and accumulates energy pulse events for occupied bays.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Apply the requested input condition.
- Verify only the documented PLC outputs respond.

### Expected observations

- Charging is enabled only for an occupied, authorized, ready bay.
