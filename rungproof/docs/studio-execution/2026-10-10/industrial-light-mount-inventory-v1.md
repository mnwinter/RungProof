# Industrial light mounting inventory v1

Read-only source audit, 2026-10-11. This supersedes the old gallery/pedestal exceptions: industrial/status lights belong on a machine, cabinet, or training bench/display board. This is an offline visual classification, not a physical installation or safety approval. No application source, build, engine, or UI was changed.

## Coverage and result

All 77 catalog scenes were inspected through their actual scene JSON equipment and shared composer installation plans. The companion JSON contains every scene, source path, equipment tag/type, source position/config, point bindings, and source-derived mount metadata. Metadata describes the composer branch; it was not obtained by executing every scene.

123 candidate devices comprise 121 actual indicators/signals plus one optical curtain and one mislabeled relay. Of the actual indicators/signals: 61 use cabinet status-tower mounts, 14 cabinet panel mounts, 28 training-bench mounts, 8 roadway signal street mounts, and 10 require industrial mounting repairs across 7 scenes.

| Scene | Equipment tags | Required treatment |
|---|---|---|
| equipment-gallery | gallery_indicator | Add a real bench/display-board support; no floor-pedestal exemption. Empty current installation plan means the cabinet helper never runs. |
| lab-11-06-wastewater-collection | training_accessory_9 | Actual single-tier alarm beacon; explicit accessory-to-cabinet support. Type is trainingAccessory, so it cannot be added blindly to the indicator-only tower helper. |
| lab-2-15-fume-extractor | hood_light | Single white industrial inspection/status lamp; machine mounting or explicit cabinet panel pilot. Existing hood context does not support it. |
| lab-4-03-repeat-cycle-counter | indicator_2, indicator_4 | Add these exact two tags to existing cabinet tower map; retain both controllers/heads and hide their floor poles/bases. |
| lab-4-04-sequence-light-tower | indicator_2 | Existing cabinet tower helper, preserving its four-color sequence assembly including the blue tier. |
| lab-4-07-parking-garage-entry | available_lamp | Availability/status tower; mount to operator cabinet or purposeful entry equipment. Its existing mast exception is not a roadway traffic-signal classification. |
| lab-4-09-chain-drive-lift | switch_8, switch_9, switch_10 | Despite tag names, these are single-color indicator equipment. Add explicit panel-plan IDs; do not use a multi-tier tower helper. |

The Lab 4.3 actual render `.tools/studio-verification/repeat-cycle-final-renders-v7/intended-scene.png` was visually inspected: both towers are unmistakably freestanding with floor bases. Its mounted visual acceptance remains open.

## Actual composition and smallest ownership

`src/Scenes/SceneComposer.cs:184` invokes `ConfigureControlInstallation`; finite workspace belt/roller presets instead take early branches at lines 26–27. `SceneComposer.ControlInstallations.cs:11` obtains the explicit plan, mounts bench/panel controls, and calls `MountCabinetStatusTowers` at line 227 only for non-bench installations. `SceneComposer.ControlInstallationPlans.cs:13` gives the gallery an empty plan; Lab 4.3/4.4/parking plans are at lines 55/56/59. `SceneComposer.StatusTowerMounts.cs:60` owns the existing tower mount helper; its line 13 gallery/parking exception comment is superseded.

One geometry owner should edit the shared composer partials, with no second geometry writer:

- `SceneComposer.StatusTowerMounts.cs`: exact tower inventory additions for Lab 4.3, Lab 4.4, and parking; remove obsolete exceptions. Preserve the existing measured cabinet-top mounting and coupler/plate support.
- `SceneComposer.ControlInstallationPlans.cs`: explicit chain-feedback single-pilot IDs; choose and document fume lamp panel versus machine support. Gallery needs a nonempty explicit bench/display plan.
- `SceneComposer.ControlInstallations.cs` or a new small shared accessory-mount partial: narrowly support the wastewater recognized beacon asset. Do not widen the tower helper to arbitrary training accessories.

Current tower helper requires `Type == indicator` and at least two configured colors. It hides POLE/BASE meshes and relocates the existing assembly; it retains lens/controller child paths, orientation and scale, and annotates cabinetStatusTower metadata. Single pilots use a different branch with their preserved single lens and front-panel orientation. Do not replace lights with decorative geometry or alter binding ownership, click targets, reset state, lens-state semantics, or scene JSON positions merely to conceal the floor base.

## Variants, demos, gallery and BYO

Normal authored scenes, existing five demos, joint reopened layouts and rebuilt scene clones use the same composition path and scene IDs; exact-ID shared mounting therefore propagates without additional preset-specific geometry writers. Existing demo indicators already fall into mounted classes in the inventory. Runtime custom layouts still need regression because moving a whole installation must carry the mounted lights.

The two supported finite workspace belt/roller preset composers have no industrial lamps in their four-asset definitions. Generic added-belt drive authoring reuses the scene-1 conveyor scene and its already mounted stacklight; the added belt itself introduces no lamp. `src/Workspace/WorkspaceTemplatePolicy.cs:13` permits only workstation-call, press-count-lamp and delayed-lamp templates; their existing bench indicators move with the control-installation assembly. Arbitrary future/imported BYO equipment is not proven by these supported paths: do not claim automatic support for an unknown scene ID or arbitrary lamp accessory.

## Classification boundaries and unresolved cases

The eight street-mounted roadway signals are mobile-traffic indicator_0/indicator_1; pedestrian-crossing training_accessory_3, traffic_west, training_accessory_4, pedestrian_south; and drawbridge indicator_1/indicator_8. These traffic-direction/pedestrian signals are distinct from parking availability status. Street support is retained, not treated as an industrial floor exception.

Guarded-transfer training_accessory_6 is an optical light-curtain sensor, not an indicator. Guarded-transfer training_accessory_8 is labeled safety relay/status beacon, but its catalog basis and independent recognition identify a byte-identical dual-channel DIN relay. Its misleading name/placement needs separate resolution; it is not counted as a lamp mounting repair. Wastewater training_accessory_9 recognition instead confirms a single-section industrial beacon and is counted. Fume hood_light is presently composed from the standard single-tier beacon asset, despite its inspection-light name; choose its intended visual mounting explicitly.

## Required regression evidence

Verify all 77 compose without missing explicit IDs; compare source inventory against mounted root metadata and visible supports. Capture every affected scene in normal and useful detail views, including gallery, four-tier Lab 4.4, parking and wastewater accessory. Confirm no visible floor poles/bases, support intersects the cabinet/machine/bench, no light/control overlap, and readable/clickable controls. Reopen supported joint/custom layouts and templates to verify whole-assembly support remains attached. Exercise on/off and each configured color, actual click input where supported, Stop/Reset and current draft bindings; compare semantic source before/after. Recheck existing mounted scenes for duplicate supports or unintended single-pilot conversion. A passing metadata test alone is not rendered placement evidence.

## Complete scene ledger
| Scene (all 77) | Device inventory and source-derived mount |
|---|---|
| conveyor-cell | cell_stacklight: cabinet-status-tower |
| equipment-gallery | gallery_indicator: unmounted-industrial-needs-fix |
| lab-10-01-drive-alarm-code-string | indicator_2: cabinet-status-tower; indicator_9: cabinet-status-tower |
| lab-10-02-chicken-label-print | indicator_3: cabinet-status-tower; indicator_11: cabinet-status-tower |
| lab-10-03-vision-package-sorter | indicator_4: cabinet-status-tower; indicator_12: cabinet-status-tower |
| lab-10-04-motor-enum-state | indicator_2: cabinet-status-tower; indicator_5: cabinet-status-tower |
| lab-10-05-motor-struct-data | indicator_2: cabinet-status-tower; indicator_9: cabinet-status-tower |
| lab-10-06-ten-motor-array-startup | indicator_5: cabinet-status-tower; indicator_12: cabinet-status-tower |
| lab-11-06-wastewater-collection | indicator_5: cabinet-status-tower; training_accessory_9: unmounted-industrial-needs-fix; indicator_13: cabinet-status-tower |
| lab-11-07-multi-conveyor-pallet-route | indicator_12: cabinet-status-tower; indicator_13: cabinet-status-tower; indicator_14: cabinet-status-tower |
| lab-11-11-service-elevator | indicator_3: cabinet-status-tower; indicator_10: cabinet-status-tower |
| lab-11-12-mobile-traffic-lights | indicator_0: roadway-signal-street-mount; indicator_1: roadway-signal-street-mount |
| lab-11-13-xy-palletizing | indicator_3: cabinet-status-tower; indicator_12: cabinet-status-tower; indicator_13: cabinet-status-tower |
| lab-11-19-powder-batch-mixer | indicator_5: cabinet-status-tower; indicator_14: cabinet-status-tower; indicator_15: cabinet-status-tower |
| lab-2-01-workstation-call | material_call_lamp: training-bench-mounted |
| lab-2-02-dual-confirmation | handoff_ready_lamp: training-bench-mounted |
| lab-2-03-service-marker-inhibit | service_marker_lamp: training-bench-mounted |
| lab-2-04-two-station-call | assistance_beacon: training-bench-mounted |
| lab-2-05-bay-light-selector | bay_light_a: training-bench-mounted; bay_light_b: training-bench-mounted |
| lab-2-06-ready-attention | ready_lamp: training-bench-mounted; attention_lamp: training-bench-mounted |
| lab-2-07-dual-contact-permissive | permit_lamp: training-bench-mounted |
| lab-2-08-inspection-vote | vote_stacklight: training-bench-mounted |
| lab-2-09-maintenance-beacon | maintenance_beacon: training-bench-mounted |
| lab-2-10-dust-collector-seal-in | collector_run_lamp: cabinet-panel-mounted |
| lab-2-11-inbound-tote-stop | inbound_status: cabinet-status-tower |
| lab-2-12-assembly-lift | top_limit_lamp: cabinet-panel-mounted; bottom_limit_lamp: cabinet-panel-mounted |
| lab-2-13-coolant-jug-fill | fill_status: cabinet-status-tower |
| lab-2-14-sump-pump | sump_status: cabinet-status-tower |
| lab-2-15-fume-extractor | hood_light: unmounted-industrial-needs-fix; speed_display: cabinet-status-tower |
| lab-2-16-safe-drill | drill_status: cabinet-status-tower |
| lab-2-17-pallet-robot | robot_cell_status: cabinet-status-tower |
| lab-2-18-pallet-pickup | pickup_status: cabinet-status-tower |
| lab-2-19-service-door | door_open_lamp: cabinet-panel-mounted; door_closed_lamp: cabinet-panel-mounted |
| lab-2-20-bottle-shuttle | shuttle_status: cabinet-status-tower |
| lab-2-21-tote-finishing | line_status: cabinet-status-tower |
| lab-2-22-dual-spindle | dual_drill_status: cabinet-status-tower |
| lab-2-23-parcel-sorter | sorter_status: cabinet-status-tower |
| lab-2-24-robot-cnc | cnc_cell_status: cabinet-status-tower |
| lab-2-25-inspection-toggle | inspection_light: training-bench-mounted |
| lab-3-01-guarded-pallet-transfer | indicator_5: cabinet-status-tower; training_accessory_6: optical-sensor-not-indicator; training_accessory_8: relay-not-beacon-name-mismatch; indicator_11: cabinet-status-tower |
| lab-3-02-robot-cell-safe-restart | indicator_3: cabinet-status-tower; indicator_10: cabinet-status-tower |
| lab-4-01-press-count-lamp | indicator_1: training-bench-mounted |
| lab-4-02-counter-reset-lamp | indicator_1: training-bench-mounted |
| lab-4-03-repeat-cycle-counter | indicator_2: unmounted-industrial-needs-fix; indicator_4: unmounted-industrial-needs-fix |
| lab-4-04-sequence-light-tower | indicator_2: unmounted-industrial-needs-fix; indicator_4: cabinet-panel-mounted |
| lab-4-05-dual-input-count-window | indicator_2: training-bench-mounted |
| lab-4-06-multi-press-confirmation | indicator_2: training-bench-mounted |
| lab-4-07-parking-garage-entry | available_lamp: unmounted-industrial-needs-fix |
| lab-4-08-package-grouping | group_ready_lamp: cabinet-panel-mounted |
| lab-4-09-chain-drive-lift | indicator_3: cabinet-status-tower; switch_8: unmounted-industrial-needs-fix; switch_9: unmounted-industrial-needs-fix; switch_10: unmounted-industrial-needs-fix; indicator_11: cabinet-status-tower |
| lab-4-10-cookie-packaging | indicator_3: cabinet-status-tower; indicator_11: cabinet-status-tower |
| lab-4-11-barrel-fill-station | indicator_4: cabinet-status-tower; indicator_11: cabinet-status-tower |
| lab-4-12-cable-cut-length | indicator_3: cabinet-status-tower; indicator_12: cabinet-status-tower |
| lab-5-01-delayed-lamp | indicator_1: training-bench-mounted |
| lab-5-02-timed-lamp-off | indicator_1: training-bench-mounted |
| lab-5-03-rotary-flasher | indicator_1: training-bench-mounted |
| lab-5-04-alternating-lamps | indicator_1: training-bench-mounted; indicator_2: training-bench-mounted |
| lab-5-05-variable-flash-rate | indicator_2: training-bench-mounted |
| lab-5-06-running-light-tower | indicator_1: training-bench-mounted |
| lab-5-07-pedestrian-crossing | training_accessory_3: roadway-signal-street-mount; traffic_west: roadway-signal-street-mount; training_accessory_4: roadway-signal-street-mount; pedestrian_south: roadway-signal-street-mount |
| lab-5-08-drawbridge-control | indicator_1: roadway-signal-street-mount; indicator_8: roadway-signal-street-mount |
| lab-5-09-bag-indexing-conveyor | indicator_10: cabinet-panel-mounted; indicator_11: cabinet-panel-mounted |
| lab-5-10-coating-line | indicator_4: cabinet-panel-mounted; indicator_12: cabinet-panel-mounted; indicator_13: cabinet-panel-mounted |
| lab-6-07-luggage-weight-sort | indicator_3: cabinet-status-tower; indicator_11: cabinet-status-tower |
| lab-6-08-hand-dryer | indicator_2: cabinet-panel-mounted; indicator_9: cabinet-panel-mounted |
| lab-9-01-sum-function | indicator_2: training-bench-mounted |
| lab-9-02-product-function | indicator_2: training-bench-mounted |
| lab-9-03-sum-and-counter-function | indicator_2: training-bench-mounted; indicator_5: training-bench-mounted |
| lab-9-04-function-selector | indicator_2: training-bench-mounted |
| lab-9-10-box-volume | indicator_2: cabinet-status-tower |
| lab-9-11-pallet-counting | indicator_3: cabinet-status-tower |
| lab-9-12-ev-charging-manager | indicator_2: cabinet-status-tower; indicator_10: cabinet-status-tower |
| scene-1-conveyor-stop | scene1_stacklight: cabinet-status-tower |
| scene-2-conveyor-pusher | scene2_stacklight: cabinet-status-tower |
| tank-high-low | hl_stacklight: cabinet-status-tower |
| tank-level | tank_stacklight: cabinet-status-tower |
| tank-radar | radar_stacklight: cabinet-status-tower |

