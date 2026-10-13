# Lab 2.17 - Twin-Container Pallet Cell help

Scene ID: `lab-2-17-pallet-robot`  
Migrated source: `prototype/scenes/lab-2-17-pallet-robot.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-17-pallet-robot.scene.json`

## Purpose

A robot transfers two process containers from a staged pallet, then releases the empty pallet to the outbound conveyor.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `pallet_ready` | `BOOL` | **PC** | `True` |
| `robot_run` | `BOOL` | **PLC** | `False` |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `placed_count` | `DINT` | **PC** | `0` |
| `cell_color` | `STRING` | **SIM** | `amber` |
| `cycle_complete` | `BOOL` | **PC** | `False` |
| `robot_at_park` | `BOOL` | **PC** | `True` |
| `operator.start` | `BOOL` | **PC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start pallet unload` | `start` | `unload-cycle` |
| `Stop robot cell` | `stop` | `` |
| `Reset pallet cell` | `reset` | `` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `pallet_ready` | `pallet_ready_sensor` | `photoeye` |
| `robot_run` | `pallet_robot` | `running` |
| `conveyor_run` | `robot_pallet_conveyor` | `running` |
| `conveyor_run` | `robot_pallet_outbound` | `running` |
| `cell_color` | `robot_cell_status` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `robot_pallet_conveyor` | `conveyor` | Pallet staging conveyor |
| `robot_pallet_outbound` | `conveyor` | Empty pallet outbound conveyor |
| `robot_pallet_transfer_bridge` | `trainingAccessory` | Grounded pallet transfer bridge |
| `robot_pallet` | `box` | Staging pallet |
| `container_a` | `box` | Process container A |
| `container_b` | `box` | Process container B |
| `pallet_robot` | `robotArm` | Pallet unloading robot |
| `process_receiver` | `containerReceiver` | Container process receiver |
| `pallet_ready_sensor` | `photoeye` | Pallet ready sensor |
| `robot_cycle_start` | `switch` | Start robot cycle |
| `robot_cell_status` | `indicator` | Robot cell status |

## Stop and safety boundary

The staging pallet's bottom runners meet the 900 mm belt surface. Both
containers are seated on its deck, the receiver is clear of the conveyor,
and the photoeye stands and connected pigtails clear the conveyor braces.
The outbound conveyor shares the 900 mm carrying height, with a grounded
425 mm bridge deck between the belts. The 70/35 mm gaps at the flat belt
tangent points are narrower than each pallet runner. The standalone reference
release covers 5.75 m at 0.65 m/s and finishes with the whole pallet on the
outbound flat belt. Both conveyors use the existing `conveyor_run` command.
The robot is installed on a grounded pedestal beside the staging conveyor.
The receiver is raised on four grounded supports and faces the robot, with
its backstop away from the approaching wrist. The timed reference uses the
imported six-joint hierarchy to approach, grip, lift, carry, lower and release
each tote. A gripped tote follows the actual tool position. Each tote lands
in its own roller bay at X=2.07/0.93 m, Z=2.15 m, with its bottom on the
measured 1.195 m roller tops; placed_count changes only after lowering.
Carrying bottom height is 2.1 m, clearing the other staged/deposited tote and
conveyor. The first carry passes around the robot pedestal. The route stays
in front of the backstop; carrying height alone does not clear its 2.48 m top.
Pallet nail heads are flush with the carrying deck, and staged tote bottoms
meet that deck. The reference takes about 41.45 seconds before completion.
Stop freezes the simulated joints and held load. Reset is required before
restarting a stopped or completed reference, restoring both totes and the
robot's park pose. An unreachable target or malformed robot motion stops the
reference before placement/completion can be counted.
Timed reference motion is separate from the normal controller-owned shell;
its complete controller-driven robot recipe remains open. The imported model
and tool attachment do not prove physical gripping, manufacturer joint limits,
self-collision, cable bend radius, acceleration, slip or load capacity.
The reference contract verifies sequence point values, not physical handling.

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

Declared simulation safe state:

| Point | Value |
| --- | --- |
| `robot_run` | `False` |
| `conveyor_run` | `False` |
| `cell_color` | `red` |

## Machine guide

A robot transfers two process containers from a staged pallet, then releases the empty pallet to the outbound conveyor.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command robot_run, conveyor_run.

### Normal sequence

- approach and grip first
- lift and carry first around the robot pedestal
- lower and release first into its roller bay
- withdraw and park after first
- approach and grip second
- lift and carry second
- lower and release second into its roller bay
- withdraw and park after second
- release pallet
- ready

### Expected observations

- Exactly two containers are placed before the pallet conveyor releases.
