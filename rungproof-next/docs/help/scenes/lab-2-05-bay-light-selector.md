# Lab 2.5 - Bay Light Selector help

Scene ID: `lab-2-05-bay-light-selector`  
Migrated source: `prototype/scenes/lab-2-05-bay-light-selector.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-05-bay-light-selector.scene.json`

## Purpose

A two-position maintenance selector controls two independent LED bay lights: isolated in position 0 and both energized in position 1.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `selector_position` | `DINT` | **PC** | `0` |
| `bay_a_command` | `BOOL` | **PLC** | `False` |
| `bay_b_command` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Advance selector` | `cycle` | `selector_position` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `selector_position` | `mode_selector` | `selector` |
| `bay_a_command` | `bay_light_a` | `indicator` |
| `bay_b_command` | `bay_light_b` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `mode_selector` | `rotarySwitch` | Maintenance selector |
| `bay_light_a` | `indicator` | Bay A LED light |
| `bay_light_b` | `indicator` | Bay B LED light |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A two-position maintenance selector controls two independent LED bay lights: isolated in position 0 and both energized in position 1.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command bay_a_command, bay_b_command.

### Normal sequence

- Observe the initial input state.
- Apply selector_position.
- Verify the PLC produces only the required bay_a_command, bay_b_command.

### Expected observations

- Position 0 de-energizes both lights; position 1 energizes both.
