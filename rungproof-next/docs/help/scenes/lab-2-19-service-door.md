# Lab 2.19 - Service Door Shutter help

Scene ID: `lab-2-19-service-door`  
Migrated source: `prototype/scenes/lab-2-19-service-door.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-19-service-door.scene.json`

## Purpose

Open, close, and stop commands move a symbolic service-door shutter between simulated normally-closed limit inputs.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `open_request` | `BOOL` | **PC** | `False` |
| `close_request` | `BOOL` | **PC** | `False` |
| `motor_open` | `BOOL` | **PLC** | `False` |
| `motor_close` | `BOOL` | **PLC** | `False` |
| `open_limit_nc` | `BOOL` | **PC** | `True` |
| `closed_limit_nc` | `BOOL` | **PC** | `False` |
| `door_position` | `REAL` | **SIM** | `100` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Open service door` | `start` | Loaded controller: pulse `open_request` (legacy preview: `open`) |
| `Close service door` | `start` | Loaded controller: pulse `close_request` (legacy preview: `close`) |
| `Immediate stop` | `stop` | `` |
| `Reset door` | `reset` | `` |

With a loaded ladder controller, each Start action pulses the named PC-owned BOOL request binding; the learner program decides actuator commands. A legacy preview sequence name does not imply that the loaded controller automatically executes that sequence.

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `motor_open` | `service_shutter` | `running` |
| `motor_close` | `service_shutter` | `running` |
| `open_limit_nc` | `door_open_lamp` | `indicator` |
| `closed_limit_nc` | `door_closed_lamp` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `service_shutter` | `rollerShutter` | Service door shutter |
| `door_open_button` | `switch` | Open door |
| `door_stop_button` | `switch` | Stop door |
| `door_close_button` | `switch` | Close door |
| `door_open_lamp` | `indicator` | Open limit NC signal |
| `door_closed_lamp` | `indicator` | Closed limit NC signal |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `motor_open` | `False` |
| `motor_close` | `False` |

## Machine guide

Open, close, and stop commands move a symbolic service-door shutter between simulated normally-closed limit inputs.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command motor_open, motor_close.

### Normal sequence

- opening
- open limit

### Expected observations

- Each direction stops at its limit; open and close outputs are never on together.

## Loaded-controller shutter physics

OPEN/CLOSE stations pulse separate `open_request` / `close_request` PC inputs. Learner ladder owns `motor_open` / `motor_close`; an accepted request alone does not move the shutter. In controller mode the actual shutter moves only while exactly one motor command is true. Both commands true or both false holds its position. The simulation full stroke is 3 seconds (model parameter, not a hardware rating).

`door_position` is actual normalized travel: 0% open, 100% closed. `open_limit_nc` is FALSE only at fully open; `closed_limit_nc` is FALSE only at fully closed. Midtravel both are TRUE. Endpoint feedback follows modeled position, never a reference timer. Stop freezes travel; Reset closes the shutter and restores initial limits. `cycle_complete` indicates a commanded travel reached an endpoint; no automatic reversal or return sequence is supplied.

