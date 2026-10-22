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
| `start_sort_request` | `BOOL` | **PC** | `False` |
| `conveyors_run` | `BOOL` | **PLC** | `False` |
| `infeed_run` | `BOOL` | **PLC** | `False` |
| `table_transfer_run` | `BOOL` | **PLC** | `False` |
| `infeed_gate_open` | `BOOL` | **PLC** | `False` |
| `primary_table_loaded` | `BOOL` | **PC** | `False` |
| `primary_table_clear` | `BOOL` | **PC** | `True` |
| `primary_table_aligned` | `BOOL` | **PC** | `False` |
| `infeed_gate_clear` | `BOOL` | **PC** | `False` |
| `transfer_inhibited` | `BOOL` | **PC** | `False` |
| `misroute_detected` | `BOOL` | **PC** | `False` |
| `all_parcels_received` | `BOOL` | **PC** | `False` |
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
| `Sort three-parcel batch` | `start` | Loaded controller: pulse `start_sort_request` (legacy preview: `sort-batch`) |
| `Stop sorter` | `stop` | `` |
| `Reset parcels` | `reset` | `` |

With a loaded ladder controller, each Start action pulses the named PC-owned BOOL request binding; the learner program decides actuator commands. A legacy preview sequence name does not imply that the loaded controller automatically executes that sequence.

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

Illustrative three-parcel transfer: load, index the flat primary table, then send each parcel onto its supported takeaway path. A secondary table carries the medium route. Loaded-controller operation uses the command-driven mechanism contract below; legacy standalone preview remains scripted geometry.

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
- The learner stops each carton at the table-center sensor before indexing. The explicit escapement meters the queue. Rigid-load support samples and height classification do not prove hardware sensing, friction or throughput.


## Command-driven transfer contract

Loaded-controller operation adds independently commanded infeed and primary transfer drives plus a visible retractable escapement at x=-2.7. The height bank is mounted at x=-2.75 so a queued parcel retains its geometry-derived size indication. `conveyors_run` enables the transfer/takeaway belt group; `infeed_run` enables the infeed belt; `table_transfer_run` enables parcel motion while on the primary deck. `infeed_gate_open` retracts the visible stop. Opening a gate does not start a belt. The original common command and legacy sequence remain declared; existing ladder projects must explicitly add the new actuator outputs and feedback to run this full cell.

LAD must latch the observed class, open the gate, wait for rear-clear, close it, stop at the primary center sensor, command angle 0/0.5/1 (0/45/90 degrees), wait for actual alignment, transfer until the primary is clear, then return the table home before feeding the next parcel. The plant does not choose a route from size. The secondary table is the fixed diagonal transfer datum. Transfer at an unsupported intermediate angle is inhibited without changing PLC commands. A centered parcel follows the actual commanded table angle; full 90-degree slew takes 1.2 s.

`primary_table_loaded` is the 25 mm radius center sensor, not a motor-stop command. `primary_table_clear` means no parcel intersects the primary deck. `infeed_gate_clear` resets on each opening and sets after the released parcel rear clears the stop. `all_parcels_received` reports three correctly received parcels, allowing LAD to stop the group explicitly. SIM count/completion never issue stop or route commands. Wrong-lane arrival sets `misroute_detected`. The scene uses a bounded rigid-carton contact/support approximation, not calibrated friction or rated conveyor throughput. Active Stop freezes mechanisms; Reset restores the queue/table/counts. No private timed sequence runs in controller mode.
