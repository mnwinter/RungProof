# Height Sorting Pusher Cell help

Scene ID: `scene-2-conveyor-pusher`  
Migrated source: `prototype/scenes/scene-2-conveyor-pusher.plcscene`  
Scene contract: `res://scenes/migrated/scene-2-conveyor-pusher.scene.json`

## Purpose

This offline training cell measures each carton at the pusher station. The
authored ladder compares the measured height against a documented **600 mm
boundary**: cartons above 600 mm are diverted to the side takeaway conveyor;
cartons at or below 600 mm continue on the main line. The repeatable simulated
sequence is 420 mm, 820 mm, then 600 mm.

The infeed conveyor continues through a full room wall into a connected staging
room. A dark, framed tunnel makes that penetration visible in the normal lesson
view; cartons begin inside that source room rather than appearing at the exposed
end of the conveyor.

The ladder program owns machine run state, height classification, divert
memory, conveyor and pusher commands, fault/interlock response, and Reset. The
plant model only integrates those commands and publishes measurement, limits,
and exit feedback.

## Expected I/O to operate this scene

All entries below are symbolic scene points. They are not `%I`, `%Q`, DB, or hardware addresses. PC-owned points are simulator feedback; PLC-owned points are commands supplied by the controller; SIM points are internal and should not be wired as external I/O.

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `part_at_pusher` | `BOOL` | **PC** | `False` |
| `carton_height_mm` | `DINT` | **PC** | `0` |
| `main_line_exit` | `BOOL` | **PC** | `False` |
| `side_line_exit` | `BOOL` | **PC** | `False` |
| `system_enable` | `BOOL` | **PC** | `False` |
| `start_command` | `BOOL` | **PC** | `False` |
| `stop_command` | `BOOL` | **PC** | `False` |
| `reset_command` | `BOOL` | **PC** | `False` |
| `safety_trip` | `BOOL` | **PC** | `False` |
| `drive_overload` | `BOOL` | **PC** | `False` |
| `pusher_extended` | `BOOL` | **PC** | `False` |
| `pusher_retracted` | `BOOL` | **PC** | `True` |
| `conveyor_running` | `BOOL` | **PLC** | `False` |
| `pusher_extend` | `BOOL` | **PLC** | `False` |
| `sorter_reset` | `BOOL` | **PLC** | `False` |
| `pusher_position` | `REAL` | **SIM** | `0` |
| `component_state` | `STRING` | **SIM** | `stopped_loaded` |
| `parts_completed` | `DINT` | **SIM** | `0` |

## Operator actions

| Action | Type | Bound point/sequence |
| --- | --- | --- |
| `Machine Start` | `pulse` | `start_command` |
| `Machine Stop` | `pulse` | `stop_command` |
| `Machine Reset` | `pulse` | `reset_command` |
| `System Enable` | `toggle` | `system_enable` |
| `Simulated safety trip` | `toggle` | `safety_trip` |
| `Simulated drive overload` | `toggle` | `drive_overload` |

## Equipment bindings

| Symbolic point | Equipment | Mode |
| --- | --- | --- |
| `conveyor_running` | `scene2_conveyor` | `running` |
| `part_at_pusher` | `scene2_photoeye` | `photoeye` |
| `main_line_exit` | `scene2_main_exit` | `photoeye` |
| `side_line_exit` | `scene2_side_exit` | `photoeye` |
| `carton_height_mm` | `scene2_height_display` | `numericDisplay` |
| `parts_completed` | `scene2_completed_display` | `numericDisplay` |

## Expected equipment

| ID | Type | Label |
| --- | --- | --- |
| `scene2_conveyor` | `conveyor` | Scene 2 conveyor |
| `scene2_product` | `box` | Scene 2 package |
| `scene2_height_portal` | `sizeSensorBank` | Three-beam carton height measurement portal |
| `scene2_photoeye` | `photoeye` | Part-at-pusher photoeye |
| `scene2_pusher` | `pusher` | Single-solenoid spring-return pusher |
| `scene2_receiver` | `conveyor` | Tall-carton side takeaway conveyor |
| `scene2_main_exit` | `photoeye` | Main-line exit sensor |
| `scene2_side_exit` | `photoeye` | Side-line transfer sensor |
| `scene2_station` | `switch` | Machine Start |
| `scene2_stacklight` | `indicator` | Height sorter cell status tower |
| `scene2_height_display` | `trainingAccessory` | `HEIGHT MM` measured-height display |
| `scene2_completed_display` | `trainingAccessory` | `DONE` completed-parts display |

The cabinet tower uses independent channels: green follows `conveyor_running`, red follows the simulated `safety_trip`, and amber follows the simulated `drive_overload`. Simultaneous fault channels remain visible. The fault inputs remain training simulation only and are not safety-rated functions.

The `HEIGHT MM` display follows the existing PC-owned `carton_height_mm`
measurement at the sorter. The `DONE` display follows the existing SIM-owned
`parts_completed` count. These are readouts of modeled feedback; neither one
chooses a route or advances the PLC sequence.

## Stop and safety boundary

A normal machine Stop drops the ladder run seal and removes both actuator
commands. Clearing a simulated trip or overload does not restart the machine;
a fresh Start is required. Machine Reset restores the first carton and clears
the offline plant state. These controls are separate from the application
Run/Stop/Reset controls.

The simulated trip is explicitly instructional and non-certified. This scene,
its standard ladder logic, and its offline controller do not implement or prove
a safety function, safety PLC, real E-stop circuit, watchdog, or commissioning.
