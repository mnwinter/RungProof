# Lab 5.6 - Running-Light Tower help

Scene ID: `lab-5-06-running-light-tower`  
Migrated source: `prototype/scenes/lab-5-06-running-light-tower.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-06-running-light-tower.scene.json`

## Purpose

A maintained ENABLE selector and momentary STEP request drive a PLC-owned red -> amber -> green running-light sequence.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `tower_enable` | `BOOL` | **PC** | `False` |
| `step_pulse` | `BOOL` | **PC** | `False` |
| `tower_red` | `BOOL` | **PLC** | `False` |
| `tower_amber` | `BOOL` | **PLC** | `False` |
| `tower_green` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Turn ENABLE OFF / RUN` | `toggle` | `tower_enable` |
| `Press STEP (momentary)` | `pulse` | `step_pulse` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `tower_enable` | `switch_0` | `selector` |
| `step_pulse` | `switch_2` | `switch` |
| `tower_red` | `indicator_1` | `indicatorChannel` |
| `tower_amber` | `indicator_1` | `indicatorChannel` |
| `tower_green` | `indicator_1` | `indicatorChannel` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `rotarySwitch` | ENABLE maintained selector OFF / RUN |
| `indicator_1` | `indicator` | PLC three-color running-light tower |
| `switch_2` | `switch` | Momentary sequence Step |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A maintained ENABLE selector and momentary STEP request drive a PLC-owned red -> amber -> green running-light sequence.

### Start conditions

- Built-in offline controller with user-authored or explicitly opened reference logic.
- Initial ENABLE OFF, STEP released, phase zero and all lenses dark.

### Normal sequence

- Default exercise editor is empty. Build PLC sequence logic or explicitly open .tools/plant-review-running-light-tower.rpproj.json after --audit-running-tower.
- Run, then turn ENABLE to RUN. The first enabled accepted scan selects red; each fresh STEP edge advances amber, green, red.
- Idle scans never advance. OFF clears lamps and phase on the next accepted scan; re-enable starts red. A simultaneous first-enable STEP is discarded.

### Expected observations

- Exactly one PLC color while enabled: red -> amber -> green -> red on separate Step edges. Disabled, held and unsampled repeated clicks cannot accumulate deferred advances.
