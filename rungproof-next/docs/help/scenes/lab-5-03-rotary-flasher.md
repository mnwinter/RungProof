# Lab 5.3 - Rotary Flasher help

Scene ID: `lab-5-03-rotary-flasher`  
Migrated source: `prototype/scenes/lab-5-03-rotary-flasher.plcscene`  
Scene contract: `res://scenes/migrated/lab-5-03-rotary-flasher.scene.json`

## Purpose

A maintained OFF/FLASH selector enables PLC-owned periodic timing. Only raw mode selection is supplied by the PC.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `flash_mode_selected` | `BOOL` | **PC** | `False` |
| `flash_lamp` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Turn mode OFF / FLASH` | `toggle` | `flash_mode_selected` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `flash_mode_selected` | `switch_0` | `selector` |
| `flash_lamp` | `indicator_1` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `switch_0` | `rotarySwitch` | Mode selector OFF / FLASH |
| `indicator_1` | `indicator` | PLC flashing lamp |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A maintained OFF/FLASH selector enables PLC-owned periodic timing. Only raw mode selection is supplied by the PC.

### Start conditions

- Built-in offline controller with an explicitly opened reference or user-authored program.
- Initial selector OFF, timers zero and lamp off.

### Normal sequence

- The default exercise editor is empty. Build PLC flasher logic, or explicitly open .tools/plant-review-rotary-flasher.rpproj.json after --audit-rotary-flasher.
- Run and turn MODE to FLASH. The original offline reference starts with 0.5 s OFF, then alternates 0.5 s ON / OFF on accepted 20 ms scans.
- Turn MODE OFF to clear the lamp and phase/timers on the next accepted scan. A new FLASH starts a full OFF half-period.

### Expected observations

- Explicit original reference: 25 scans OFF, 25 ON, repeating only in FLASH. OFF cancels next scan; Stop clears lamp/timers and freezes scan; Reset clears phase.
