# Tank transmitter face source audit

Read-only current source/retained GLB review. No edits/build/UI/engine. Diagnostic close renders remain root-owned and pending.

Main.cs:328 loads production.catalog.json. SceneComposer.cs:413–417 maps analog levelSensor to sensing.level.analog-4-20ma.v1; line86 maps radarLevelSensor to sensing.level.radar.v1. Production catalog points to the actual scene_core delivery GLBs below, not similarly named training_accessories assets.

| Scene/equipment | Actual face geometry | Binding/presentation conclusion |
|---|---|---|
| tank-level/level_transmitter (scene label LT-101) | level_transmitter_4_20ma.glb nodes DISPLAY_bezel,DISPLAY_glass,DISPLAY_window; window Optical lens material | Deliberately neutral static window; no numericDisplay binding or live Label3D creation for this transmitter |
| tank-radar/radar_transmitter (scene label LT-301) | radar_level_transmitter.glb nodes DISPLAY_bezel,DISPLAY_key_±0.07,DISPLAY_window,KIN_radar_beam; window Optical lens material | Neutral static window with live illustrative beam geometry; no numericDisplay binding or local numeric label |

Actual GLB face materials are non-textured Optical lens; no face text/tag/value nodes or numeric textures occur in the parsed deliveries. Analog generator `tools/modeling/build_scene_core_assets.py:565–567` explicitly says the neutral protected window avoids fabricated live values, loop type and manufacturer display layout. Radar lines610–615 likewise emit a neutral lens window and keys, not numeric ink.

Runtime SceneSimulationRuntime.cs:1220 onward publishes level_transmitter =4+16*level (PC REAL,mA); radar_level =100*level (PC REAL,%), radar_signal=4+16*level (PC REAL,mA), radar_echo_ok=true and radar_distance from antenna/liquid geometry. These are offline idealized scene feedback, not vendor/hardware assertions. UpdateRadarProjection adjusts KIN_radar_beam; it does not write DISPLAY_window text. General numericDisplay projection at1436 requires an explicit point binding and NumericReadout child, absent here. tank-level binds only drain-valve position; tank-radar binds drain-valve position and two station switch points. Thus live scene I/O feedback exists without a local face readout.

Old analog visual_review.md claiming67.4%,4–20mA and LT-101 face text, and old radar review claiming LT-201/80GHz/localvalue/echo text, are stale relative to current delivered faces. Current radar scene declares LT-301. These historical review statements do not create a missing-value requirement or authorize invented on-device values/vendor specifics. Do not add fake static67.4% or LT-201 to satisfy them.

Delivered SHA256:

- analog GLB: A0CB7799162B8C53A434D2C2F40F30F9FF8460DFEB2C1EF2B0E74380EBB6A2B6
- radar GLB: 53C77FF845A1278792FA9C516B6783397231EDD21C20A75D56DA814DD06F2B7E

Bounded conclusion: both windows are intentionally static/blank. No face-value defect established by source audit. Root close captures should confirm actual delivered face/readability/mounting, while any future truthful live local display would require a separately scoped explicit presentation binding to actual feedback. Handle OPEN; material75 HELD.
