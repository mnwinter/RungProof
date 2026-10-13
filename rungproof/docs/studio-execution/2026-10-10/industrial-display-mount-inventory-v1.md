# Industrial display mounting inventory v1

Read-only source and delivered-GLB inventory, 2026-10-11. All 77 scenes covered. No application edits, builds, engine runs or UI interaction. Runtime mount metadata is inferred from exact composer plan membership, not measured from executing all scenes. Companion JSON records source files, tags/types, original positions/config, delivered geometry node names and typed point bindings.

## Result and exact repair set

54 candidate records: 13 cabinet-panel readouts, 18 training-bench readouts, 11 freestanding counter/display heads requiring repair across 9 scenes, one separate checkweigher readout requiring machine-support design, one stand-backed drive screen requiring explicit operator-station review, one printer-integrated screen, one pipe-integrated process meter, four integrated/supported sensor heads, two runtime floating observation annotations and two roadway signal glyphs excluded from process-display repairs. The light inventory remains 10 gaps/7 scenes; this does not revise that count.

Correction to preliminary message: plain freestanding readout gaps are ELEVEN, not twelve; the VFD screen is separately classified for station review. A supporting floor pedestal alone is insufficient evidence of a credible industrial HMI station. Mounted classes likewise require rendered readability/support verification before visual acceptance.

| Affected scene | Exact display IDs | Source position (x,y,z) |
|---|---|---|
| lab-10-01-drive-alarm-code-string | training_accessory_3 (stand-backed-drive-screen-station-review) | 4.3, 0, -1 |
| lab-10-02-chicken-label-print | training_accessory_5 (checkweigher-separate-floor-readout-needs-machine-support) | 0, 0, -1.3 |
| lab-10-02-chicken-label-print | training_accessory_7 (unmounted-floor-display-needs-fix) | 5, 0, -1.3 |
| lab-10-03-vision-package-sorter | training_accessory_6 (unmounted-floor-display-needs-fix) | 1.1, 0, 1.8 |
| lab-4-07-parking-garage-entry | occupancy_display (unmounted-floor-display-needs-fix) | 8.3, 0, -6.5 |
| lab-4-08-package-grouping | group_count_display (unmounted-floor-display-needs-fix) | 4.9, 0, 3.6 |
| lab-4-10-cookie-packaging | cookie_counter (unmounted-floor-counter-needs-fix) | -1.5, 0, 2.5 |
| lab-4-10-cookie-packaging | package_counter (unmounted-floor-counter-needs-fix) | 1.5, 0, 2.5 |
| lab-4-12-cable-cut-length | training_accessory_8 (unmounted-floor-display-needs-fix) | -2.5, 0, 1.7 |
| lab-6-07-luggage-weight-sort | training_accessory_6 (unmounted-floor-display-needs-fix) | -0.05, 0, 1.6 |
| lab-9-11-pallet-counting | training_accessory_6 (unmounted-floor-display-needs-fix) | 1.1, 0, 1.8 |
| lab-9-12-ev-charging-manager | training_accessory_5 (unmounted-floor-display-needs-fix) | -3.4, 0, 2.9 |
| lab-9-12-ev-charging-manager | ev_meter_b (unmounted-floor-display-needs-fix) | 2.6, 0, 2.9 |
## Geometry and source contracts

`src/Scenes/SceneComposer.cs:342` adds configured NumericReadout/StaticReadout to retained mapped equipment. Runtime text/numeric binding projection is at `SceneSimulationRuntime.cs:1422`–1444; preserve STRING/ENUM text, numeric format/units and placeholder behavior. Clickable numeric selectors are actual input devices, not decorative readouts: preserve config.action and `SceneControlInteractor.cs:55` behavior.

`SceneComposer.ControlInstallations.cs:15` uses explicit plan IDs; it hides only recognized original mount namespaces, scales and places retained heads by actual delivered bounds, and attaches their rear face to cabinet fascia or supports them on training tiers. Mount namespaces at lines 240–246 include COUNT_DISPLAY, STATIC_READOUT, LENGTH_DISPLAY, LABEL_DISPLAY and STAND base/mast. METER_base/METER_pedestal and cookie COUNT_foot/COUNT_mast are NOT covered; adding these IDs without a narrow extension leaves floor supports on the cabinet.

Delivered GLB node names were read directly, not inferred only from filenames. Count heads contain COUNT_DISPLAY_base/mast/housing/screen; formatted text display contains LABEL_DISPLAY_base/mast/enclosure/screen; energy meter contains METER_base/pedestal/enclosure/screen. Modeling sources corroborate these shapes: `tools/modeling/build_pallet_count_training_props.py:47`, `build_label_print_training_props.py:86`, `build_ev_training_assets.py:66`, and `build_static_training_readouts.py`. Existing alarm-string display is already cabinet-integrated. The drive's separate VFD diagnostic panel is mounted by STAND_backboard and VFD_mount_rail on its own floor stand (`build_drive_alarm_training_props.py:38`); determine whether to integrate it into the same cabinet or establish a visibly enclosed purposeful operator/service station.

Cookie counters explicitly construct COUNT_foot/mast/display (`SceneComposer.CookiePackaging.cs:55`); mount the retained counter head to machine frame or cabinet. Barrel meter (`SceneComposer.BarrelFill.cs:64`) instead has display/stem on its actual flow bore and is a credible process-integrated readout, not an orphan pedestal.

Chicken checkweigher has WEIGH_readout_foot/mast/housing separate from its deck (`build_label_print_training_props.py:57`). Connect that head structurally to the weighing machine or move it to a credible operator panel without moving product/scale geometry. Actual MeasuredWeight is presently a billboard above delivered bounds (`SceneSimulationRuntime.ChickenLabel.cs:68`), not on its physical readout screen. PrintedInk is a physical product label; CapturedLabelText is an observation caption. Preserve both captured-string semantics; these are not cabinet HMI mounting candidates. Printer_control_screen is already part of printer body on its bench.

Tote FinishingInspectionReadout (`SceneSimulationRuntime.ToteFinishingProcess.cs:48`) is floating above vision equipment; wastewater WastewaterReadout (`SceneSimulationRuntime.Wastewater.cs:29`) floats above a probe's measured bounds. These are explicit observation annotations, not existing mounted HMI screens. Design must decide whether to present them as clear simulation captions or actual supported operator readouts; do not label floating text as a cabinet-mounted display. Analog level/radar displays are integral to sensor housing; gallery transmitter has explicit supported sensor-fitting display fixture, not an orphan HMI pedestal. Pedestrian display-named meshes are roadway signal glyphs, not numeric process readouts.

## Smallest repair ownership

One geometry owner should extend `SceneComposer.ControlInstallationPlans.cs` with the explicit eleven gap IDs, selecting cabinet or machine ownership per scene. Reuse `SceneComposer.ControlInstallations.cs` retained-head bounds/fascia placement; narrowly add METER_base/pedestal and cookie COUNT_foot/mast support namespaces only for selected known assets. Large formatted STRING head may require measured cabinet width/row spacing; do not assume the existing .70 scale/pitch makes it readable. Chicken checkweigher head needs a small machine-support helper rather than moving the entire scale to the cabinet. Resolve VFD station separately. No runtime, typed point or evaluator rewrite is necessary for ordinary mount repairs. Any decision to move runtime annotations into actual screens belongs in a separate explicit presentation change, preserving data ownership.

## Demo, workspace, copy and rebuild coverage

Normal authored scenes, five demos and same-scene copies/rebuilds flow through shared Compose and installation plans; no separate mount writer is needed. Existing typed STRING/ENUM/numeric head roots and paths must survive joint saved-layout reopening. Lab 4.3 batch_count_display is already in its cabinet plan: the light defect does not imply that its count display is unmounted. Supported training templates are only workstation-call, press-count-lamp and delayed-lamp (`WorkspaceTemplatePolicy.cs:13`), whose complete installation assembly moves together. Finite belt/roller workspace preset early branches have no display equipment; added-belt drive authoring adds no physical display. Workspace shell watch/history panes are 2D software UI, not freestanding scene HMIs. Unknown future imported BYO heads have no demonstrated automatic mounting coverage. Gallery has no independent numeric HMI but does have a supported analog sensor head display fixture.

## Acceptance requirements

Render all nine repair scenes plus Chicken scale and the resolved VFD station in normal/detail views. Confirm enclosure/frame contact, no residual floor stand/base, no doubled support, no clipped text, no machine/control obstruction, and useful operator orientation. Render long valid and wrong STRING labels, ENUM state strings, numeric extrema/units and NO DATA; preserve actual click targets for numeric input selectors. Verify Stop/Reset, bindings, lens/screen states, and runtime child lookup paths without changing values to make screenshots pass. Reopen joint/current custom saved layouts and supported templates and verify mounts follow the complete assembly. Existing mounted 31 panel/bench readouts are regression comparators. Source membership alone cannot establish actual visual readability or physical safety.

## Complete 77-scene ledger
| Scene (all 77) | Display/readout inventory and source-derived class |
|---|---|
| conveyor-cell | No display/readout identified in scene equipment/composer/runtime source |
| equipment-gallery | gallery_transmitter: gallery-sensor-supported-display-fixture |
| lab-10-01-drive-alarm-code-string | training_accessory_3: stand-backed-drive-screen-station-review; training_accessory_4: cabinet-panel-mounted |
| lab-10-02-chicken-label-print | training_accessory_5: checkweigher-separate-floor-readout-needs-machine-support; training_accessory_6: printer-machine-integrated; training_accessory_7: unmounted-floor-display-needs-fix |
| lab-10-03-vision-package-sorter | training_accessory_6: unmounted-floor-display-needs-fix |
| lab-10-04-motor-enum-state | training_accessory_7: cabinet-panel-mounted |
| lab-10-05-motor-struct-data | training_accessory_3: cabinet-panel-mounted; training_accessory_4: cabinet-panel-mounted; training_accessory_5: cabinet-panel-mounted |
| lab-10-06-ten-motor-array-startup | training_accessory_7: cabinet-panel-mounted; training_accessory_8: cabinet-panel-mounted |
| lab-11-06-wastewater-collection | training_accessory_7: process-sensor-integrated-head-display; training_accessory_7: runtime-floating-observation-annotation-needs-explicit-design |
| lab-11-07-multi-conveyor-pallet-route | No display/readout identified in scene equipment/composer/runtime source |
| lab-11-11-service-elevator | No display/readout identified in scene equipment/composer/runtime source |
| lab-11-12-mobile-traffic-lights | No display/readout identified in scene equipment/composer/runtime source |
| lab-11-13-xy-palletizing | No display/readout identified in scene equipment/composer/runtime source |
| lab-11-19-powder-batch-mixer | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-01-workstation-call | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-02-dual-confirmation | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-03-service-marker-inhibit | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-04-two-station-call | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-05-bay-light-selector | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-06-ready-attention | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-07-dual-contact-permissive | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-08-inspection-vote | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-09-maintenance-beacon | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-10-dust-collector-seal-in | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-11-inbound-tote-stop | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-12-assembly-lift | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-13-coolant-jug-fill | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-14-sump-pump | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-15-fume-extractor | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-16-safe-drill | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-17-pallet-robot | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-18-pallet-pickup | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-19-service-door | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-20-bottle-shuttle | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-21-tote-finishing | vision_inspector: runtime-floating-observation-annotation-needs-explicit-design |
| lab-2-22-dual-spindle | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-23-parcel-sorter | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-24-robot-cnc | No display/readout identified in scene equipment/composer/runtime source |
| lab-2-25-inspection-toggle | No display/readout identified in scene equipment/composer/runtime source |
| lab-3-01-guarded-pallet-transfer | No display/readout identified in scene equipment/composer/runtime source |
| lab-3-02-robot-cell-safe-restart | No display/readout identified in scene equipment/composer/runtime source |
| lab-4-01-press-count-lamp | No display/readout identified in scene equipment/composer/runtime source |
| lab-4-02-counter-reset-lamp | No display/readout identified in scene equipment/composer/runtime source |
| lab-4-03-repeat-cycle-counter | batch_count_display: cabinet-panel-mounted |
| lab-4-04-sequence-light-tower | No display/readout identified in scene equipment/composer/runtime source |
| lab-4-05-dual-input-count-window | count_a_display: training-bench-mounted; count_b_display: training-bench-mounted |
| lab-4-06-multi-press-confirmation | count_a_display: training-bench-mounted; count_b_display: training-bench-mounted |
| lab-4-07-parking-garage-entry | occupancy_display: unmounted-floor-display-needs-fix |
| lab-4-08-package-grouping | group_count_display: unmounted-floor-display-needs-fix |
| lab-4-09-chain-drive-lift | No display/readout identified in scene equipment/composer/runtime source |
| lab-4-10-cookie-packaging | cookie_counter: unmounted-floor-counter-needs-fix; package_counter: unmounted-floor-counter-needs-fix |
| lab-4-11-barrel-fill-station | training_accessory_6: process-meter-pipe-mounted |
| lab-4-12-cable-cut-length | training_accessory_8: unmounted-floor-display-needs-fix |
| lab-5-01-delayed-lamp | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-02-timed-lamp-off | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-03-rotary-flasher | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-04-alternating-lamps | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-05-variable-flash-rate | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-06-running-light-tower | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-07-pedestrian-crossing | training_accessory_4: roadway-signal-glyph-not-process-display; pedestrian_south: roadway-signal-glyph-not-process-display |
| lab-5-08-drawbridge-control | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-09-bag-indexing-conveyor | No display/readout identified in scene equipment/composer/runtime source |
| lab-5-10-coating-line | No display/readout identified in scene equipment/composer/runtime source |
| lab-6-07-luggage-weight-sort | training_accessory_6: unmounted-floor-display-needs-fix |
| lab-6-08-hand-dryer | training_accessory_6: cabinet-panel-mounted |
| lab-9-01-sum-function | training_accessory_4: training-bench-mounted; numeric_display_0: training-bench-mounted; numeric_display_1: training-bench-mounted |
| lab-9-02-product-function | training_accessory_4: training-bench-mounted; numeric_display_0: training-bench-mounted; numeric_display_1: training-bench-mounted |
| lab-9-03-sum-and-counter-function | numeric_display_0: training-bench-mounted; numeric_display_1: training-bench-mounted; numeric_display_2: training-bench-mounted; numeric_display_3: training-bench-mounted |
| lab-9-04-function-selector | numeric_display_0: training-bench-mounted; numeric_display_1: training-bench-mounted; numeric_display_2: training-bench-mounted; numeric_display_3: training-bench-mounted |
| lab-9-10-box-volume | numeric_display_0: cabinet-panel-mounted; numeric_display_1: cabinet-panel-mounted; numeric_display_2: cabinet-panel-mounted; training_accessory_4: cabinet-panel-mounted |
| lab-9-11-pallet-counting | training_accessory_6: unmounted-floor-display-needs-fix |
| lab-9-12-ev-charging-manager | training_accessory_5: unmounted-floor-display-needs-fix; ev_meter_b: unmounted-floor-display-needs-fix |
| scene-1-conveyor-stop | No display/readout identified in scene equipment/composer/runtime source |
| scene-2-conveyor-pusher | No display/readout identified in scene equipment/composer/runtime source |
| tank-high-low | No display/readout identified in scene equipment/composer/runtime source |
| tank-level | level_transmitter: process-sensor-integrated-head-display |
| tank-radar | radar_transmitter: process-sensor-integrated-head-display |


