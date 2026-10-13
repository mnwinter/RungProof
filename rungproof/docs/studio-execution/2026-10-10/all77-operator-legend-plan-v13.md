# All77 operator physical-legend plan V13

Read-only source audit of all 77 migrated scene JSON files: 151 switch records; 96 already supply faceLabel; one missing-label emergency switch uses the separate E-stop asset; 54 ordinary switches retain asset START. **35 misleading START cases across 13 scenes** are mapped below; 19 legitimate start/run controls stay unchanged. These are offline input/fixture legends, not claims of generated feedback or physical safety.

## Exact presentation map

Literal `\\n` below means a two-line Label3D string. Each row must match the exact scene ID, equipment ID and config.action; no inference from a generic action prefix or point name.

| Scene | Equipment | Required config.action | Proposed physical legend |
|---|---|---|---|
| lab-10-03-vision-package-sorter | switch_9 | toggle-package_present | PACKAGE\nPRESENT |
| lab-10-03-vision-package-sorter | switch_10 | toggle-vision_result_valid | VISION\nVALID |
| lab-10-03-vision-package-sorter | switch_11 | toggle-destination_clear | DEST\nCLEAR |
| lab-11-07-multi-conveyor-pallet-route | switch_9 | toggle-zone_1_clear | ZONE 1\nCLEAR |
| lab-11-07-multi-conveyor-pallet-route | switch_10 | toggle-zone_2_clear | ZONE 2\nCLEAR |
| lab-11-07-multi-conveyor-pallet-route | switch_11 | toggle-zone_3_clear | ZONE 3\nCLEAR |
| lab-11-11-service-elevator | switch_2 | toggle-call_valid | CALL\nVALID |
| lab-11-11-service-elevator | switch_8 | toggle-doors_closed | DOORS\nCLOSED |
| lab-11-11-service-elevator | switch_9 | toggle-landing_clear | LANDING\nCLEAR |
| lab-11-19-powder-batch-mixer | switch_11 | toggle-recipe_valid | RECIPE\nVALID |
| lab-11-19-powder-batch-mixer | switch_12 | toggle-dose_complete | DOSE\nDONE |
| lab-11-19-powder-batch-mixer | switch_13 | toggle-mixer_ready | MIXER\nREADY |
| lab-2-18-pallet-pickup | manual_jog | manual-jog | JOG |
| lab-3-01-guarded-pallet-transfer | switch_4 | toggle-guard_closed | GUARD\nCLOSED |
| lab-3-01-guarded-pallet-transfer | switch_9 | toggle-entry_clear | ENTRY\nCLEAR |
| lab-3-01-guarded-pallet-transfer | switch_10 | toggle-exit_clear | EXIT\nCLEAR |
| lab-6-07-luggage-weight-sort | switch_8 | load-next-bag | LOAD\nBAG |
| lab-6-07-luggage-weight-sort | switch_9 | toggle-scale_ready | SCALE\nREADY |
| lab-6-07-luggage-weight-sort | switch_10 | cycle-fixture_mass_kg | NEXT\nMASS |
| lab-6-08-hand-dryer | switch_7 | toggle-hands_inserted | HANDS\nIN |
| lab-9-11-pallet-counting | switch_7 | toggle-pallet_detected | PALLET\nPRESENT |
| lab-9-11-pallet-counting | switch_8 | toggle-pallet_type_valid | TYPE\nVALID |
| lab-9-11-pallet-counting | switch_9 | toggle-count_request | COUNT\nREQUEST |
| lab-9-12-ev-charging-manager | switch_1 | toggle-bay_occupied | A BAY\nOCCUPIED |
| lab-9-12-ev-charging-manager | switch_8 | toggle-customer_authorized | A AUTH\nVALID |
| lab-9-12-ev-charging-manager | switch_9 | toggle-charger_ready | A CHARGE\nREADY |
| lab-9-12-ev-charging-manager | fixture_connector_inserted | toggle-connector_inserted | A PLUG\nIN |
| lab-9-12-ev-charging-manager | fixture_bay_b_occupied | toggle-bay_b_occupied | B BAY\nOCCUPIED |
| lab-9-12-ev-charging-manager | fixture_bay_b_customer_authorized | toggle-bay_b_customer_authorized | B AUTH\nVALID |
| lab-9-12-ev-charging-manager | fixture_bay_b_charger_ready | toggle-bay_b_charger_ready | B CHARGE\nREADY |
| lab-9-12-ev-charging-manager | fixture_bay_b_connector_inserted | toggle-bay_b_connector_inserted | B PLUG\nIN |
| tank-high-low | hl_drain_station | toggle-drain | DRAIN |
| tank-level | drain_station | toggle-drain | DRAIN |
| tank-radar | radar_pump_station | toggle-pump | AUTO\nCYCLE |
| tank-radar | radar_drain_station | toggle-drain | DRAIN\nREQUEST |

## Semantics and retained START cases

Vision, zone-clear, elevator, powder, guarded-transfer, pallet and EV rows are toggle fixtures for the named Simulation action point. They do not start machinery directly. `manual-jog` is declared type=start but its action label explicitly says one pallet increment; JOG preserves that distinction. Luggage LOAD is luggageLoad; NEXT MASS cycles fixture_mass_kg through 12/22/32; HANDS IN toggles hands_inserted (click again withdraws). Tank drains toggle drain_valve_open; radar DRAIN REQUEST toggles manual_drain_request requiring enabled cycle. Radar config action toggle-pump actually toggles cycle_request: AUTO CYCLE reflects the exact action definition rather than its misleading identifier.

Unchanged missing-face START cases: conveyor-cell/operator_station; equipment-gallery/gallery_switch; scene-1-conveyor-stop/scene1_station; scene-2-conveyor-pusher/scene2_station; tank-high-low/hl_pump_station and tank-level/pump_station (inlet pump run); Lab2 collector_start, inbound_start, fill_start, sump_start, robot_cycle_start, auto_start, shuttle_start, line_start, dual_drill_start, sorter_start, cnc_cell_start; Lab4 cookie switch_8 and barrel switch_8. Existing explicit faces, emergency style and rotary-switch labels are outside this map. Table count35 + retained19 = all54 ordinary missing faces; no JSON/config/action/binding mutations proposed.

## Small implementation contract and text fit

Source seam: SceneComposer.cs:67 currently calls CreateSwitchAsset(equipment,candidates). Pass scene.Id into that function (currently :662), then exact presentation lookup only when configured faceLabel is empty and style is not emergency. An explicit configured faceLabel always wins. Guard each mapped row against exact config.action AND matching Simulation action contract (type/point as audited); fail qualification on drift, do not silently apply another label. Add map in a small dedicated SceneComposer partial. Keep existing KIN_pushbutton/color, model/root, pick target and BUTTON_start_label source geometry; hide only original label mesh and use the existing OperatorFaceLabel creation path :675–688. No evaluator/default solution or runtime action changes.

Current Label3D font32/pixelSize .0006 is about .0192m nominal character height, but font advance, line height and original plate extent must be measured. Proposed longest lines are eight letters (OCCUPIED), most five-seven; two lines avoid long single strings. This is a proposed fit strategy, **not rendered fit verification**. Native QA must measure actual font string width/line height against actual button plate bounds with margin, preserve readable contrast and ensure both lines stay inside the physical plate. Reduce font for this mapped label if necessary within readable limits; never enlarge/shift the button root or hide its active lens. Physical operator/detail renders must inspect every distinct longest-label geometry and all affected scenes, including EV eight-button crowding. Existing fitting tests on numeric readouts do not establish this label fit.

Required regression: all35 exact labels present/no START mesh visible; all19 retained START unchanged; all96 explicit faces unchanged; emergency/rotary untouched; missing/changed action lookup rejected; all151 switch IDs/config/action/Simulation actions/bindings and reset/pick behavior unchanged. Validate supported workspace default/copy/rebuild composition consumes the same exact scene-scoped presentation map without serializing changes into JSON. Shared label visibility must remain stable after layout transforms.

Evidence producers: scenes/migrated/*.scene.json equipment.config.action + simulation.actions; tools/modeling/build_scene_core_assets.py:144/167 authors START meshes; src/Scenes/SceneComposer.cs:662–688 applies existing explicit faces. No build/engine/source writes performed. Dot design review and root lease remain prerequisites for implementation. Material75 HELD; Handle OPEN.

