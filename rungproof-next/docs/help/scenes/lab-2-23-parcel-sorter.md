# Lab 2.23 - Parcel Size Sorter help

Scene ID: `lab-2-23-parcel-sorter`  
Migrated source: `prototype/scenes/lab-2-23-parcel-sorter.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-23-parcel-sorter.scene.json`

## Purpose

Illustrative three-parcel transfer: load, index the flat primary table, then send each parcel onto its supported takeaway path. A secondary table carries the medium route.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `conveyors_run` | `BOOL` | **PLC** | `False` |
| `large_detected` | `BOOL` | **PC** | `False` |
| `medium_detected` | `BOOL` | **PC** | `False` |
| `small_detected` | `BOOL` | **PC** | `False` |
| `route_position` | `REAL` | **PLC** | `0` |
| `sorted_count` | `DINT` | **SIM** | `0` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Sort three-parcel batch` | `start` | `sort-batch` |
| `Stop sorter` | `stop` | `` |
| `Reset parcels` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `conveyors_run` | `sort_infeed` | `running` |
| `conveyors_run` | `large_lane` | `running` |
| `conveyors_run` | `medium_lane` | `running` |
| `conveyors_run` | `small_lane` | `running` |
| `large_detected` | `size_sensor_bank` | `photoeye` |
| `route_position` | `sort_turntable_a` | `position` |
| `status_color` | `sorter_status` | `indicator` |
| `conveyors_run` | `medium_transfer` | `running` |
| `conveyors_run` | `sort_turntable_a` | `running` |
| `conveyors_run` | `sort_turntable_b` | `running` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `sort_infeed` | `conveyor` | Parcel infeed |
| `large_lane` | `conveyor` | Large parcel lane |
| `medium_lane` | `conveyor` | Medium parcel lane |
| `small_lane` | `conveyor` | Small parcel lane |
| `sort_turntable_a` | `rotaryTable` | Primary routing table |
| `sort_turntable_b` | `rotaryTable` | Secondary routing table |
| `medium_transfer` | `conveyor` | Medium parcel transfer between tables |
| `large_parcel` | `box` | Large parcel |
| `medium_parcel` | `box` | Medium parcel |
| `small_parcel` | `box` | Small parcel |
| `size_sensor_bank` | `sizeSensorBank` | Three-height size sensor bank |
| `sorter_start` | `switch` | Start parcel batch |
| `sorter_status` | `indicator` | Sorter status |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyors_run` | `False` |
| `status_color` | `red` |

## Machine guide

Illustrative three-parcel transfer: load, index the flat primary table, then send each parcel onto its supported takeaway path. A secondary table carries the medium route. This is scripted plant geometry, not a validated conveyor contact or real PLC process model.

### Start conditions

- For the operator shell, author/load the required ladder and command bindings; no parcel-sorter reference ladder is supplied.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command conveyors_run, route_position.

### Normal sequence

- load large onto primary table
- route large
- large complete
- load medium onto primary table
- index medium
- transfer medium to secondary table
- route medium
- medium complete
- return primary table for small parcel
- load small onto primary table
- index small
- route small
- batch complete

### Expected observations

- One parcel reaches each lane and the sorted count ends at three.
- The cartons pause at the table center before indexing. Scripted queue holds and size feedback do not prove physical sensing, friction or throughput.
