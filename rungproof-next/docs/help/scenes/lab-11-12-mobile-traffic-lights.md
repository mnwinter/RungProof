# Lab 11.12 - Mobile Traffic Lights help

Scene ID: `lab-11-12-mobile-traffic-lights`  
Migrated source: `prototype/scenes/lab-11-12-mobile-traffic-lights.plcscene`  
Scene contract: `res://scenes/migrated/lab-11-12-mobile-traffic-lights.scene.json`

## Purpose

Two common-command signal heads display six PLC-owned lamps. Manual READY/CLEAR fixtures are retained. Explicit queued 50%-scale training vehicles derive crossing occupancy; an explicitly loaded educational ladder owns 6 s green / 1 s amber / minimum 1 s all-red timing.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `controller_ready` | `BOOL` | **PC** | `False` |
| `road_a_clear` | `BOOL` | **PC** | `False` |
| `road_b_clear` | `BOOL` | **PC** | `False` |
| `road_a_green` | `BOOL` | **PLC** | `False` |
| `road_b_green` | `BOOL` | **PLC** | `False` |
| `road_a_red` | `BOOL` | **PLC** | `False` |
| `road_a_amber` | `BOOL` | **PLC** | `False` |
| `road_b_red` | `BOOL` | **PLC** | `False` |
| `road_b_amber` | `BOOL` | **PLC** | `False` |
| `road_a_occupied` | `BOOL` | **PC** | `False` |
| `road_b_occupied` | `BOOL` | **PC** | `False` |
| `crossing_clear` | `BOOL` | **PC** | `True` |
| `signal_conflict` | `BOOL` | **PC** | `False` |
| `road_a_position_m` | `REAL` | **PC** | `-3.6` |
| `road_b_position_m` | `REAL` | **PC** | `3.6` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle controller ready` | `toggle` | `controller_ready` |
| `Toggle road a clear` | `toggle` | `road_a_clear` |
| `Toggle road b clear` | `toggle` | `road_b_clear` |
| `Queue road A training vehicle` | `trafficRequestA` | `` |
| `Queue road B training vehicle` | `trafficRequestB` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `controller_ready` | `switch_2` | `switch` |
| `road_a_clear` | `switch_6` | `switch` |
| `road_b_clear` | `switch_7` | `switch` |
| `road_a_green` | `indicator_0` | `indicatorChannel` |
| `road_b_green` | `indicator_1` | `indicatorChannel` |
| `road_a_red` | `indicator_0` | `indicatorChannel` |
| `road_a_amber` | `indicator_0` | `indicatorChannel` |
| `road_b_red` | `indicator_1` | `indicatorChannel` |
| `road_b_amber` | `indicator_1` | `indicatorChannel` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `indicator_0` | `trainingAccessory` | Road A red / amber / green signal head |
| `indicator_1` | `trainingAccessory` | Road B red / amber / green signal head |
| `switch_2` | `switch` | Manual READY |
| `training_accessory_4` | `trainingAccessory` | Horizontal roadway with raised shoulders and crossing |
| `switch_6` | `switch` | Manual A CLEAR |
| `switch_7` | `switch` | Manual B CLEAR |
| `traffic_vehicle_a` | `trainingAccessory` | Queued road A training vehicle (50% scale) |
| `traffic_request_a` | `switch` | Queue road A vehicle |
| `traffic_vehicle_b` | `trainingAccessory` | Queued road B training vehicle (50% scale) |
| `traffic_request_b` | `switch` | Queue road B vehicle |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Two synchronized mobile signal heads coordinate a safe alternating vehicle-flow state.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Load the explicit training reference, enable READY and both manual CLEAR fixtures, then queue A and B.
- One-second startup all-red precedes A green; 6 s green, 1 s amber and minimum 1 s all-red alternate the direction.
- Keep all-red until crossing_clear. Retain departed vehicles visibly; use QUEUE to explicitly reintroduce a completed vehicle.

### Expected observations

- Only one road receives a green command at a time, and both roads stop on a fault.
- Actual occupancy and crossing_clear follow vehicle footprints, not PLC output echoes.
- No opposing vehicle entry while the other vehicle route remains committed. Both red on entry-fixture fault while scanning the explicit reference.
