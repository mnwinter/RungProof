# Lab 5.9 - Bag Indexing Conveyor help

Scene ID: `lab-5-09-bag-indexing-conveyor`  
Migrated source: `prototype/scenes/lab-5-09-bag-indexing-conveyor.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-09-bag-indexing-conveyor.scene.json`

## Purpose

Actual bag indexing between ENTRY and EXIT beams. At EXIT the PLC reference pauses; manual PAUSE CLEAR and a fresh START permit the return.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `bag_at_entry` | `BOOL` | **PC** | `True` |
| `bag_at_exit` | `BOOL` | **PC** | `False` |
| `start_request` | `BOOL` | **PC** | `False` |
| `pause_clear` | `BOOL` | **PC** | `False` |
| `motion_inhibited` | `BOOL` | **PC** | `False` |
| `travel_limited` | `BOOL` | **PC** | `False` |
| `bag_position` | `REAL` | **PC** | `-2` |
| `belt_speed` | `REAL` | **PC** | `0` |
| `conveyor_run` | `BOOL` | **PLC** | `False` |
| `conveyor_reverse` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Press START / return restart` | `pulse` | `start_request` |
| `Set manual PAUSE BLOCKED / CLEAR` | `toggle` | `pause_clear` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `bag_at_entry` | `photoeye_2` | `photoeye` |
| `bag_at_exit` | `photoeye_3` | `photoeye` |
| `pause_clear` | `switch_9` | `selector` |
| `conveyor_run` | `indicator_10` | `indicator` |
| `conveyor_reverse` | `indicator_11` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Reversible bag conveyor with integral geared drive |
| `box_1` | `trainingAccessory` | Filled bag on the carrying belt |
| `photoeye_2` | `photoeye` | ENTRY through-beam sensor pair |
| `photoeye_3` | `photoeye` | EXIT through-beam sensor pair |
| `switch_4` | `switch` | START outbound / restart return |
| `switch_9` | `rotarySwitch` | Manual pause acknowledgement BLOCKED / CLEAR |
| `indicator_10` | `indicator` | PLC run-enable command |
| `indicator_11` | `indicator` | PLC reverse-direction command |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Actual bag indexing between ENTRY and EXIT beams. At EXIT the PLC reference pauses; manual PAUSE CLEAR and a fresh START permit the return.

### Start conditions

- Open or author and verify a ladder controller; the default exercise is blank.
- Use --audit-bag-index to generate .tools/plant-review-bag-index.rpproj.json for explicit File Open.
- Bag initially blocks ENTRY and both commands are false.

### Normal sequence

- Run enables accepted 20 ms scans. START moves the bag forward at the original illustrative 0.5 m/s.
- EXIT stops the forward command. A START without PAUSE CLEAR has no effect.
- Select CLEAR and press START again to return; ENTRY stops the return.

### Expected observations

- A rounded filled sack replaces the carton and copied pusher.
- Both sensor pairs cross the belt at 1.1 m and derive feedback from BAG_BODY triangles.
- Prescribed no-slip motion and rendered contact only; no flexible-body physics, actual drive, safety or PLC transport claim.
