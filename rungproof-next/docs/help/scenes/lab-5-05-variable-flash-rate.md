# Lab 5.5 - Variable Flash Rate help

Scene ID: `lab-5-05-variable-flash-rate`  
Migrated source: `prototype/scenes/lab-5-05-variable-flash-rate.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-05-variable-flash-rate.scene.json`

## Purpose

Maintained ENABLE, FAST and SLOW selectors supply raw requests. PLC timing runs exactly one selected rate; both or neither selected leaves the lamp off.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `flash_enable` | `BOOL` | **PC** | `False` |
| `fast_rate_selected` | `BOOL` | **PC** | `False` |
| `slow_rate_selected` | `BOOL` | **PC** | `False` |
| `rate_lamp` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Turn ENABLE OFF / RUN` | `toggle` | `flash_enable` |
| `Turn FAST OFF / FAST` | `toggle` | `fast_rate_selected` |
| `Turn SLOW OFF / SLOW` | `toggle` | `slow_rate_selected` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `flash_enable` | `switch_0` | `selector` |
| `fast_rate_selected` | `switch_1` | `selector` |
| `slow_rate_selected` | `switch_3` | `selector` |
| `rate_lamp` | `indicator_2` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `rotarySwitch` | ENABLE maintained selector OFF / RUN |
| `switch_1` | `rotarySwitch` | FAST maintained selector OFF / FAST |
| `indicator_2` | `indicator` | PLC rate lamp - green |
| `switch_3` | `rotarySwitch` | SLOW maintained selector OFF / SLOW |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Maintained ENABLE, FAST and SLOW selectors supply raw requests. PLC timing runs exactly one selected rate; both or neither selected leaves the lamp off.

### Start conditions

- Built-in offline controller with user-authored or explicitly opened reference logic.
- Initial selectors OFF, timers/phase zero and lamps off.

### Normal sequence

- Default exercise editor is empty. Build PLC timer logic or explicitly open .tools/plant-review-variable-flash-rate.rpproj.json after --audit-flash-pair.
- Run with ENABLE RUN and exactly one of FAST/SLOW selected. The original offline reference uses FAST 0.2 s and SLOW 0.5 s half-periods, starting with the lamp off.
- Both selected or neither selected clears the lamp, phase and timers on the next accepted scan; correcting the selection starts a full OFF half-period.
- A direct valid rate change between accepted scans retains the current phase and starts a full half-period at the new rate. ENABLE OFF clears phase/timers.

### Expected observations

- Explicit original reference: FAST turns on at scan 10, off at 20; SLOW on at 25, off at 50. Both/neither/disabled are dark; correcting the request starts fresh OFF timing.
