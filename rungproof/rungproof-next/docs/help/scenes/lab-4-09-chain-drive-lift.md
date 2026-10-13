# Lab 4.9 - Chain-Drive Lift help

Scene ID: `lab-4-09-chain-drive-lift`  
Scene contract: `res://scenes/migrated/lab-4-09-chain-drive-lift.scene.json`

## Carton route

A carton starts on the two-strand chain infeed. At HOME, horizontal drive moves
it across the lower bridge onto the lift's powered carrying deck. The fully
carried carton rises 2.1 m to the 3.0675 m upper carrying height, crosses the
upper bridge onto the receiving belt, and stops against the receiver plate.
The empty carriage then returns HOME. The carton remains visible on the
receiver until global Reset; it is never deleted or silently recycled.

Two crossmembers support the deck. Fixed lower/upper switches meet the moving
guide shoe at the endpoints. The receiver stop has posts beyond its conveyor
bearings, a crossbar and a center support arm. Shared catalog deliveries remain
unchanged. This is a bounded single-carton simulator, without falling-load,
slip, inertia or flexible-chain dynamics.

## Expected I/O to operate this scene

All points are symbolic, without hardware addresses. PC feedback now comes
from actual modeled load/carriage positions. The previous manual feedback
toggle actions are removed; the three former pushbutton props are feedback
lamps. HOME goes false as the carriage leaves the lower endpoint. An infeed
photoeye shows `box_present`; an upper entry photoeye shows
`receiver_beam_blocked`. Each beam clears when the carton passes it.
`receiver_occupied` describes the occupied receiver zone and stays true while
the completed carton remains there; it is distinct from an entry photoeye.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `box_present` | `BOOL` | **PC** | `True` |
| `lift_home` | `BOOL` | **PC** | `True` |
| `destination_clear` | `BOOL` | **PC** | `True` |
| `chain_run` | `BOOL` | **PLC** | `False` |
| `lift_enable` | `BOOL` | **PLC** | `False` |
| `lift_start` | `BOOL` | **PC** | `False` |
| `carton_on_lift` | `BOOL` | **PC** | `False` |
| `lift_upper` | `BOOL` | **PC** | `False` |
| `carton_at_receiver` | `BOOL` | **PC** | `False` |
| `receiver_occupied` | `BOOL` | **PC** | `False` |
| `receiver_beam_blocked` | `BOOL` | **PC** | `False` |
| `transfer_fault` | `BOOL` | **PC** | `False` |
| `lift_lower` | `BOOL` | **PLC** | `False` |
| `cycle_active` | `BOOL` | **PLC** | `False` |
| `cycle_complete` | `BOOL` | **PLC** | `False` |
| `carton_on_infeed` | `BOOL` | **PC** | `True` |

`carton_on_infeed` stays true through lower bridge loading until the complete
carton footprint enters the deck. `carton_on_lift` is true only when the whole
carton fits on the carriage. `carton_at_receiver` indicates the fully received
carton reaching the stop. `destination_clear` clears when the carton enters the receiver zone and
stays false after it passes the entry beam. These are ideal symbolic position/occupancy sensors.

`chain_run` commands horizontal travel and carrying-surface animation.
`lift_enable` raises; `lift_lower` lowers. Neither command is inferred by a
plant timer. The PLC reference owns sequencing, `cycle_active` and completion.
The plant never writes PLC-owned commands. Conflicting directions, simultaneous
horizontal/vertical commands, or a transfer across an absent platform/surface
latch `transfer_fault`, hold the plant and raise an alarm. Reset is required.
This explicit fault boundary does not simulate collision or falling dynamics.

## Run the editable reference

1. Select Built-in Simulator and this scene. Open
   `programs/examples/chain-lift-installation-reference.rpproj.json` through
   **File -> Open Ladder Agent Project...**. Its retained filename now contains
   the complete `Chain_Lift_Carton_Cycle` reference.
2. Return to Scene and press bottom **Run**. Scans advance; all drives stay off.
3. Press **Start carton cycle (momentary)**. Observe loading, raising, discharge
   and empty-carriage return. The reference scans at 20 ms. The bounded model
   uses 0.3 m/s horizontal travel and a four-second vertical stroke.
4. Completion clears all drive outputs and leaves the carton on the receiver.
   Another Start cannot recycle that carton. Global Reset restores the initial
   infeed carton and HOME/CLEAR feedback and stops scans.
5. During any leg, **Stop** holds every pose. **Run** alone cannot resume; press
   a fresh Start to continue the held route. A fault requires global Reset.

The default generated ladder project remains an exercise. If nothing moves,
first check the loaded reference name, increasing scan number, and a fresh
Start. If `transfer_fault` appears, inspect the three drive commands and the
carton/platform positions before resetting.

## Expected equipment

| ID | Type | Installed purpose |
| --- | --- | --- |
| `conveyor_0` | conveyor | Two-strand chain infeed |
| `liftTable_1` | liftTable | Guided chain-driven vertical lift |
| `box_2` | box | Carton on the chain infeed |
| `indicator_3` | indicator | Chain run status |
| `training_accessory_4` | sceneInstallation | Fixed infeed transfer bridge |
| `training_accessory_5` | sceneInstallation | Fixed upper receiving bridge |
| `training_accessory_6` | sceneInstallation | Frame-mounted lower and upper limit switches |
| `training_accessory_7` | sceneInstallation | Upper receiving conveyor end stop |
| `switch_8` | indicator | Infeed carton feedback |
| `switch_9` | indicator | Lift HOME feedback |
| `switch_10` | indicator | Destination CLEAR feedback |
| `indicator_11` | indicator | Lift enable status |
| `receiving_conveyor` | conveyor | Upper receiving belt conveyor |
| `lift_start_station` | switch | Carton cycle Start (momentary) |
| `infeed_photoeye` | photoeye | Infeed carton optical sensor |
| `receiver_photoeye` | photoeye | Upper receiver entry optical sensor |

## Verification boundary

`--audit-chain-lift-installation` retains its flag name but checks the complete
saved reference, actual offline controller scans, all four route legs,
Stop/Run/fresh Start in each leg, automatic endpoint feedback, drive Reset,
invalid commands and 211 sampled carriage heights. The full cycle is sampled
at 10 ms for bearing-contact envelopes and selected carton/equipment clearance.
Intended guide, chain and optical-beam contacts are excluded from the relevant
clearance screens. These bounds checks are not continuous collision or physical
load/stability proof.

Native QA adds **Hold offline plant clock** and **Step 2.0 s** only with
`--visual-scene-review`. A step runs 100 actual 20 ms controller scans and their
plant steps; it does not assign a fabricated pose. Normal real-time operation
and held multi-angle inspections provide separate rendered evidence. Neither
establishes hardware safety, wiring, live PLC behavior or mechanical ratings.
DB14 remains unchanged.
