# Lab 2.18 - Shipping Pallet Accumulation help

Scene ID: `lab-2-18-pallet-pickup`  
Migrated source: `prototype/scenes/lab-2-18-pallet-pickup.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-18-pallet-pickup.scene.json`

## Purpose

Automatic mode advances a loaded shipping pallet to the pickup photoeye; a separate jog sequence demonstrates manual positioning.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `auto_start_request` | `BOOL` | **PC** | `False` |
| `manual_jog_request` | `BOOL` | **PC** | `False` |
| `auto_mode` | `BOOL` | **PC** | `True` |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `pickup_sensor` | `BOOL` | **PC** | `False` |
| `pallet_position` | `REAL` | **SIM** | `0` |
| `status_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle auto mode` | `togglePoint` | `auto_mode` |
| `Start automatic travel` | `start` | `automatic` |
| `Jog pallet one increment` | `start` | `manual` |
| `Stop conveyor` | `stop` | `` |
| `Reset pallet` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `auto_mode` | `pickup_mode` | `selector` |
| `conveyor_run` | `pickup_conveyor` | `running` |
| `pickup_sensor` | `pickup_end_sensor` | `photoeye` |
| `status_color` | `pickup_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `pickup_conveyor` | `conveyor` | Shipping pallet conveyor |
| `shipping_pallet` | `palletLoad` | Loaded shipping pallet |
| `pickup_end_sensor` | `photoeye` | Forklift pickup sensor |
| `pickup_mode` | `rotarySwitch` | Auto / manual selector |
| `auto_start` | `switch` | Automatic start |
| `manual_jog` | `switch` | Manual jog |
| `pickup_status` | `indicator` | Pickup conveyor status |

## Stop and safety boundary

The shipping pallet's three bottom boards sit on the 900 mm belt, fully
inside its flat carrying span throughout the automatic reference. Its root
starts at X=-3.6 m, Y=0.8625 m and stops at the first case crossing,
X=2.5626 m. The photoeye stands are 3.6 m apart,
grounded and clear of the conveyor hardware/cable bounds.

The standalone reference requires AUTO for Run and MANUAL for Jog. Each jog
advances one quarter of the 6.1626 m route from the current pose, giving
25/50/75/100% over four jogs. Nominal speed is 0.75 m/s: about 2.0542 s per
quarter and 8.217 s for automatic travel from home. Actual case triangles
crossed by the lens-to-lens ray supply pickup feedback; progress is continuous.
The old X=3.1 m endpoint aligns the beam with a gap between case columns.
Stop holds pose and feedback, Run resumes from the held pose, and a mode change
stops on the next tick. Once at pickup, another start is blocked until Reset.
Reset returns home in AUTO with zero progress and clear pickup feedback.

Native Windows mid-route and pickup views were inspected from five angles;
four jogs, blocked fifth, Stop/resume, mode loss, Reset and automatic completion
were exercised. Geometry samples the route every 10 ms. The preview displays
six significant digits while retaining full feedback precision. Conveyor
acceleration/slip equivalence and full mechanical acceptance are unaccepted.
The rebuilt load seats nine blocks on the lower boards, three stringers on
the blocks, seven deck boards on the stringers and four lower cases on the
deck; upper cases contact thin seated sealing tape. Five focused imported
mesh checks cover these bearing planes. Home and pickup were inspected from
five close native angles. Two closed 35 mm wide, 1.2 mm thick strap bands
follow the evaluated load cross-section and return beneath the upper stringers
inside the fork openings. Their lowest point is 128.8 mm above the belt.
Four added checks cover band closure, return height and sampled nonpenetration
against convex load parts. Native home/pickup five-view and low underside
inspection supplement those checks. The opt-in standalone preview's Pallet
underside button changes only the camera; recessed case faces are bridged by
the bands. This geometry does not establish restraint strength or dynamics.
The reference does not run when a virtual/external controller is selected;
normal loaded-controller lesson acceptance remains open.

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `conveyor_run` | `False` |
| `status_color` | `red` |

## Machine guide

Automatic mode advances a loaded shipping pallet to the pickup photoeye; a separate jog sequence demonstrates manual positioning.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command conveyor_run.

### Normal sequence

- automatic travel
- pickup position

### Expected observations

- Auto stops at the pickup sensor; manual jog moves only one bounded increment.
