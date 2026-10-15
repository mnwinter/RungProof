# Lab 2.8 - Inspection Vote Stacklight help

Scene ID: `lab-2-08-inspection-vote`  
Migrated source: `prototype/scenes/lab-2-08-inspection-vote.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-08-inspection-vote.scene.json`

## Purpose

Two inspectors enter independent votes. One vote shows the corresponding disposition; simultaneous votes force a red conflict indication.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `vote_a` | `BOOL` | **PC** | `False` |
| `vote_b` | `BOOL` | **PC** | `False` |
| `pass_indication` | `BOOL` | **PLC** | `False` |
| `hold_indication` | `BOOL` | **PLC** | `False` |
| `conflict_indication` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle inspector A` | `toggle` | `vote_a` |
| `Toggle inspector B` | `toggle` | `vote_b` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `vote_a` | `left_vote` | `switch` |
| `vote_b` | `right_vote` | `switch` |
| `pass_indication` | `vote_stacklight` | `indicator` |
| `hold_indication` | `vote_stacklight` | `indicator` |
| `conflict_indication` | `vote_stacklight` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `left_vote` | `switch` | Inspector A vote |
| `right_vote` | `switch` | Inspector B vote |
| `vote_stacklight` | `indicator` | Inspection result stacklight |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Two inspectors enter independent votes. One vote shows the corresponding disposition; simultaneous votes force a red conflict indication.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command pass_indication, hold_indication, conflict_indication.

### Normal sequence

- Observe the initial input state.
- Apply vote_a, vote_b.
- Verify the PLC produces only the required pass_indication, hold_indication, conflict_indication.

### Expected observations

- Single votes select green or amber; simultaneous votes select red only.
