# Lab 6.7 - Luggage Weight Sort help

Scene ID: `lab-6-07-luggage-weight-sort`  
Migrated source: `prototype/scenes/lab-6-07-luggage-weight-sort.plcscene`  
Scene contract: `res://scenes/migrated/lab-6-07-luggage-weight-sort.scene.json`

## Purpose

Supported luggage transport and illustrative fixture-weight feedback. Write PLC logic to weigh each bag, classify it once and increment only its category counter.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `bag_at_entry` | `BOOL` | **PC** | `False` |
| `bag_at_scale` | `BOOL` | **PC** | `False` |
| `bag_at_normal_exit` | `BOOL` | **PC** | `False` |
| `bag_at_reject_exit` | `BOOL` | **PC** | `False` |
| `weight_valid` | `BOOL` | **PC** | `False` |
| `motion_inhibited` | `BOOL` | **PC** | `False` |
| `travel_limited` | `BOOL` | **PC** | `False` |
| `diverter_ready` | `BOOL` | **PC** | `False` |
| `scale_ready` | `BOOL` | **PC** | `True` |
| `fixture_mass_kg` | `REAL` | **PC** | `12` |
| `weight_kg` | `REAL` | **PC** | `0` |
| `bag_x` | `REAL` | **PC** | `-3.2` |
| `bag_z` | `REAL` | **PC** | `0` |
| `bag_speed` | `REAL` | **PC** | `0` |
| `infeed_run` | `BOOL` | **PLC** | `False` |
| `discharge_run` | `BOOL` | **PLC** | `False` |
| `weigh_cycle` | `BOOL` | **PLC** | `False` |
| `reject_select` | `BOOL` | **PLC** | `False` |
| `class_result` | `DINT` | **PLC** | `0` |
| `class_1_count` | `DINT` | **PLC** | `0` |
| `class_2_count` | `DINT` | **PLC** | `0` |
| `class_3_count` | `DINT` | **PLC** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Load next bag after exit` | `luggageLoad` | `` |
| `Toggle scale ready` | `toggle` | `scale_ready` |
| `Next fixture mass: 12 / 22 / 32 kg` | `cycle` | `fixture_mass_kg` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `weight_kg` | `training_accessory_6` | `numericDisplay` |
| `scale_ready` | `switch_9` | `switch` |
| `weigh_cycle` | `indicator_3` | `indicator` |
| `class_result` | `indicator_11` | `indicator` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `conveyor_0` | `conveyor` | Luggage Weight Sort conveyor |
| `indicator_3` | `indicator` | Luggage Weight Sort indicator |
| `training_accessory_4` | `trainingAccessory` | Grounded four-cell weighing platform |
| `training_accessory_5` | `trainingAccessory` | Illustrative suitcase load |
| `training_accessory_6` | `trainingAccessory` | Luggage Weight Sort - weight display |
| `training_accessory_7` | `trainingAccessory` | Retained swing-arm diverter, installed without decorative carton |
| `switch_8` | `switch` | Load next fixture bag after exit |
| `switch_9` | `switch` | Manual scale ready permissive |
| `switch_10` | `switch` | Select next fixture mass (12 / 22 / 32 kg) |
| `indicator_11` | `indicator` | Category result available lamp |
| `luggage_transfer_bridge` | `trainingAccessory` | Grounded reject receiving bridge and scale transition |
| `reject_outfeed` | `conveyor` | Supported reject outfeed |
| `luggage_entry` | `photoeye` | LUGGAGE ENTRY actual mesh beam |
| `luggage_station` | `photoeye` | LUGGAGE STATION actual mesh beam |
| `luggage_normal_exit` | `photoeye` | LUGGAGE NORMAL EXIT actual mesh beam |
| `luggage_reject_exit` | `photoeye` | LUGGAGE REJECT EXIT actual mesh beam |

## Stop and safety boundary

A normal Stop removes PLC-owned commands according to the scene runtime. This document does not prove a safety function, a real E-stop circuit, a PLC watchdog, or live-machine commissioning.

## Machine guide

Supported luggage transport and illustrative fixture-weight feedback. Write PLC logic to weigh each bag, classify it once and increment only its category counter.

### Start conditions

- The common PLC/watchdog foundation is healthy.
- All required simulator inputs are at their documented initial state.

### Normal sequence

- Index to bag_at_scale, stop travel and command weigh_cycle with scale_ready.
- Classify weight_kg once while weight_valid; increment only the selected category counter.
- End weigh_cycle, select the route, discharge to the appropriate exit and stop.

### Expected observations

- One valid bag yields one category and increments only that class counter when your loaded PLC program implements this logic.
- Live weight is the latched fixture mass only while the base is wholly on the weighing deck, weighing is commanded and no travel is commanded.
- A completed bag remains visibly on its receiving surface. No contact, calibrated weight or rated mechanism is simulated.
