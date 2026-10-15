# Lab 4.7 - Parking Garage Entry help

Scene ID: `lab-4-07-parking-garage-entry`  
Migrated source: `prototype/scenes/lab-4-07-parking-garage-entry.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-07-parking-garage-entry.scene.json`

## Purpose

Original offline two-bay parking exercise: supported vehicle routes, a hinged barrier, actual passage feedback and a PLC-owned occupancy readout. Capacity and timing are declared training choices.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `machine_enabled` | `BOOL` | **PC** | `False` |
| `exit_clear` | `BOOL` | **PC** | `False` |
| `entry_detected` | `BOOL` | **PC** | `False` |
| `space_available` | `BOOL` | **PC** | `True` |
| `exit_requested` | `BOOL` | **PC** | `False` |
| `passage_occupied` | `BOOL` | **PC** | `False` |
| `passage_detected` | `BOOL` | **PC** | `False` |
| `entry_passed` | `BOOL` | **PC** | `False` |
| `exit_passed` | `BOOL` | **PC** | `False` |
| `barrier_closed` | `BOOL` | **PC** | `True` |
| `barrier_raised` | `BOOL` | **PC** | `False` |
| `barrier_position` | `REAL` | **PC** | `0` |
| `barrier_open` | `BOOL` | **PLC** | `False` |
| `vehicle_run` | `BOOL` | **PLC** | `False` |
| `garage_available` | `BOOL` | **PLC** | `False` |
| `occupancy_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle driver enable` | `toggle` | `machine_enabled` |
| `Toggle downstream path clear` | `toggle` | `exit_clear` |
| `Enter vehicle (free bay)` | `parkingEnter` | `` |
| `Exit one parked vehicle` | `parkingExit` | `` |
| `Clear departed vehicle` | `parkingClearDeparted` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `machine_enabled` | `enable_switch` | `switch` |
| `exit_clear` | `clear_switch` | `switch` |
| `entry_detected` | `entry_photoeye` | `photoeye` |
| `passage_detected` | `passage_photoeye` | `photoeye` |
| `garage_available` | `available_lamp` | `indicator` |
| `occupancy_count` | `occupancy_display` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `parking_pad` | `trainingAccessory` | Supported two-bay parking pad |
| `barrier` | `trainingAccessory` | Hinged entrance barrier |
| `vehicle_0` | `trainingAccessory` | Parking vehicle 1 |
| `vehicle_1` | `trainingAccessory` | Parking vehicle 2 |
| `entry_photoeye` | `photoeye` | Actual entry beam |
| `passage_photoeye` | `photoeye` | Actual passage beam |
| `occupancy_display` | `trainingAccessory` | PLC occupancy count |
| `available_lamp` | `indicator` | PLC space available |
| `enable_switch` | `switch` | Held driver enable |
| `clear_switch` | `switch` | Held downstream path clear |
| `enter_button` | `switch` | Introduce or re-enter a vehicle |
| `exit_button` | `switch` | Request one parked vehicle to leave |
| `remove_button` | `switch` | Clear only a fully departed vehicle |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Original offline two-bay parking exercise: supported vehicle routes, a hinged barrier, actual passage feedback and a PLC-owned occupancy readout. Capacity and timing are declared training choices.

### Start conditions

- Open an explicitly authored PLC project; the default exercise editor remains empty.
- Run, then set driver enable and downstream path clear.

### Normal sequence

- Press ENTER to introduce one vehicle; watch approach, full barrier opening, crossing and parking.
- Repeat for the second bay; the full garage rejects a third entry.
- Press EXIT to reverse one parked vehicle through the raised barrier. REMOVE clears only the fully departed vehicle.

### Expected observations

- No unsupported route, disappearing threshold vehicle or substitute motor-starter/wall models.
- PLC readout follows actual accepted arrival/departure edges; parked and fully departed vehicles remain visible.
- The capacity, route and timing are original training assumptions, not a manufacturer installation.
