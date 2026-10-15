# Lab 2.15 - Weld Fume Extractor help

Scene ID: `lab-2-15-fume-extractor`  
Migrated source: `prototype/scenes/lab-2-15-fume-extractor.plcscene`  
Scene contract: `res://scenes/migrated/lab-2-15-fume-extractor.scene.json`

## Purpose

A light switch and four-position speed selector independently control an inspection light and a guarded extraction fan.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `hood_light_request` | `BOOL` | **PC** | `False` |
| `hood_light_on` | `BOOL` | **PLC** | `False` |
| `fan_selector_position` | `DINT` | **PC** | `0` |
| `fan_run` | `BOOL` | **PLC** | `False` |
| `fan_speed_percent` | `DINT` | **PLC** | `0` |
| `low_speed` | `BOOL` | **SIM** | `False` |
| `medium_speed` | `BOOL` | **SIM** | `False` |
| `high_speed` | `BOOL` | **SIM** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Toggle hood light request` | `toggle` | `hood_light_request` |
| `Advance fan speed` | `cycle` | `fan_selector_position` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `hood_light_request` | `hood_light_switch` | `switch` |
| `hood_light_on` | `hood_light` | `indicator` |
| `fan_selector_position` | `fan_speed_selector` | `selector` |
| `fan_run` | `extractor_fan` | `running` |
| `fan_speed_percent` | `extractor_fan` | `speedPercent` |
| `low_speed` | `speed_display` | `indicator` |
| `medium_speed` | `speed_display` | `indicator` |
| `high_speed` | `speed_display` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `hood_light_switch` | `switch` | Hood light switch |
| `fan_speed_selector` | `rotarySwitch` | Fan speed selector |
| `hood_light` | `indicator` | Inspection light |
| `extractor_fan` | `fan` | Guarded extraction fan |
| `speed_display` | `indicator` | Speed indication |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

A light switch and four-position speed selector independently control an inspection light and a guarded extraction fan.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.
- The PLC is ready to command hood_light_on, fan_run, fan_speed_percent.

### Normal sequence

- Observe the initial input state.
- Apply hood_light_request, fan_selector_position.
- Verify the PLC produces only the required hood_light_on, fan_run, fan_speed_percent.

### Expected observations

- Light is independent; selector positions command off/35/65/100% and one speed indication.
