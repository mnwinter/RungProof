# Lab 4.10 - Cookie Packaging Cell help

Scene ID: `lab-4-10-cookie-packaging`  
Migrated source: `prototype/scenes/lab-4-10-cookie-packaging.plcscene`  
Scene contract: `res://scenes/migrated/lab-4-10-cookie-packaging.scene.json`

## Purpose

Six supported cookie trays are optically counted, indexed and sealed; completed packages remain on the outfeed.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `product_present` | `BOOL` | **PC** | `False` |
| `packaging_ready` | `BOOL` | **PC** | `True` |
| `batch_complete` | `BOOL` | **PC** | `False` |
| `packaging_busy` | `BOOL` | **PC** | `False` |
| `count_beam_blocked` | `BOOL` | **PC** | `False` |
| `packaging_fault` | `BOOL` | **PC** | `False` |
| `batch_start` | `BOOL` | **PC** | `False` |
| `cookie_count` | `INT` | **PC** | `0` |
| `wrapped_count` | `INT` | **PC** | `0` |
| `infeed_run` | `BOOL` | **PLC** | `False` |
| `packaging_enable` | `BOOL` | **PLC** | `False` |
| `cycle_active` | `BOOL` | **PLC** | `False` |
| `cycle_complete` | `BOOL` | **PLC** | `False` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Start / resume cookie batch` | `pulse` | `batch_start` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `infeed_run` | `conveyor_0` | `running` |
| `infeed_run` | `indicator_3` | `indicator` |
| `packaging_enable` | `indicator_11` | `indicator` |
| `count_beam_blocked` | `photoeye_2` | `photoeye` |
| `batch_start` | `switch_8` | `switch` |
| `cookie_count` | `cookie_counter` | `numericDisplay` |
| `wrapped_count` | `package_counter` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Six-cookie indexing conveyor |
| `machine_1` | `machine` | Cookie tray sealing station |
| `photoeye_2` | `photoeye` | Physical cookie count photoeye |
| `cookie_0` | `trainingAccessory` | Cookie tray 1 |
| `cookie_1` | `trainingAccessory` | Cookie tray 2 |
| `cookie_2` | `trainingAccessory` | Cookie tray 3 |
| `cookie_3` | `trainingAccessory` | Cookie tray 4 |
| `cookie_4` | `trainingAccessory` | Cookie tray 5 |
| `cookie_5` | `trainingAccessory` | Cookie tray 6 |
| `cookie_counter` | `trainingAccessory` | COOKIES physical feedback display |
| `package_counter` | `trainingAccessory` | SEALED physical feedback display |
| `switch_8` | `switch` | Start / resume cookie batch |
| `indicator_3` | `indicator` | Feed command |
| `indicator_11` | `indicator` | Sealer command |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Six supported cookie trays are optically counted, indexed and sealed; completed packages remain on the outfeed.

### Start conditions

- Open the editable reference or author equivalent ladder.
- Run starts offline scans. Fresh Start authorizes motion.

### Normal sequence

- Feed the next tray into the stationary sealing position.
- Stop feed, lower the ring jaws, hold, and raise them.
- Advance the sealed tray and index the next.
- After six seals, advance 150 mm and retain all packages on the belt.

### Expected observations

- COOKIES increments on actual leading-edge photoeye crossings; SEALED increments only after a complete head cycle.
- There is no automatic replenishment, deletion, recycling, film mechanics or heat-process simulation.
