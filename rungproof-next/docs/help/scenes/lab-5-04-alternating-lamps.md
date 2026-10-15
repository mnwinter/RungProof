# Lab 5.4 - Alternating Lamps help

Scene ID: `lab-5-04-alternating-lamps`  
Migrated source: `prototype/scenes/lab-5-04-alternating-lamps.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-04-alternating-lamps.scene.json`

## Purpose

A maintained OFF/RUN selector enables PLC-timed alternation of lamp A (amber) and lamp B (green). Only raw enable comes from the PC.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `alternate_enable` | `BOOL` | **PC** | `False` |
| `lamp_a` | `BOOL` | **PLC** | `False` |
| `lamp_b` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Turn ENABLE OFF / RUN` | `toggle` | `alternate_enable` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `alternate_enable` | `switch_0` | `selector` |
| `lamp_a` | `indicator_1` | `indicator` |
| `lamp_b` | `indicator_2` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `rotarySwitch` | ENABLE maintained selector OFF / RUN |
| `indicator_1` | `indicator` | Lamp A - amber |
| `indicator_2` | `indicator` | Lamp B - green |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A maintained OFF/RUN selector enables PLC-timed alternation of lamp A (amber) and lamp B (green). Only raw enable comes from the PC.

### Start conditions

- Built-in offline controller with user-authored or explicitly opened reference logic.
- Initial selectors OFF, timers/phase zero and lamps off.

### Normal sequence

- Default exercise editor is empty. Build PLC timer logic or explicitly open .tools/plant-review-alternating-lamps.rpproj.json after --audit-flash-pair.
- Run and turn ENABLE to RUN. PLC starts with A amber and alternates A/B every 0.5 s of accepted 20 ms scans.
- Exactly one lamp is commanded on after each enabled scan. OFF clears lamps, phase and timers on the next scan; RUN starts with A again.

### Expected observations

- Explicit original reference: A at first enabled scan through scan 24, B at scan 25, A at 50, B at 75. Enabled accepted scans command exactly one lamp; disabled scans command neither.
