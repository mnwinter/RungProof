# Mount repairs department report v1

2026-10-11. Implementation source frozen for root-owned build and verification. No build, engine, screen, keyboard, mouse, PLC, publication or material rollout was performed by this department. These are implemented geometry changes awaiting execution and rendered acceptance, not a global completion claim.

## Exact changes

The reconciled baseline is ten industrial light gaps across seven scenes, not the preliminary eleven/eight. All ten now have explicit support ownership:

| Scene | IDs | Implemented support |
|---|---|---|
| equipment-gallery | gallery_indicator | Real student bench/backboard at original exhibit location |
| lab-11-06-wastewater-collection | training_accessory_9 | Cabinet roof coupler; exact recognized alarm_beacon asset |
| lab-2-15-fume-extractor | hood_light | Cabinet fascia single pilot, retaining its lens controller |
| lab-4-03-repeat-cycle-counter | indicator_2, indicator_4 | Cabinet roof plates/couplers |
| lab-4-04-sequence-light-tower | indicator_2 | Cabinet roof plate/coupler; all four tiers retained |
| lab-4-07-parking-garage-entry | available_lamp | Cabinet roof plate/coupler |
| lab-4-09-chain-drive-lift | switch_8, switch_9, switch_10 | Existing single-pilot cabinet branch despite switch-prefixed IDs |

Eleven free-standing readouts across nine scenes now join their existing control-cabinet fascia plan: Chicken training_accessory_7; vision training_accessory_6; parking occupancy_display; grouping group_count_display; cookie cookie_counter/package_counter; cable training_accessory_8; luggage training_accessory_6; pallet training_accessory_6; EV training_accessory_5/ev_meter_b. Their roots, children, NumericReadout/StaticReadout names, configuration and bindings remain intact. Narrow known namespaces hide cookie COUNT_foot/COUNT_mast and energy_meter METER_base/METER_pedestal.

Drive alarm training_accessory_3 now joins the same measured service enclosure as its bound STRING alarm display and controls. Its independent STAND_base/mast are hidden, while the actual drive body, backboard, mount rails and static DEMO screen remain. This is an exterior service-panel mounting, not a representation of certified electrical cabinet internals or a claim that DEMO is live alarm text.

Chicken training_accessory_5 stays at its existing root transform. Delivered readout housing/screen, scale deck/load cells/frame and product geometry do not move. Only WEIGH_readout_foot/mast are hidden. New WEIGH_readout_frame_post intersects the nearest delivered machine side frame, and WEIGH_readout_frame_arm bridges that post to the retained housing's underside. Support dimensions derive from accumulated local mesh bounds. Its existing runtime MeasuredWeight billboard stays an observation caption; no functional HMI ink was invented.

## Fit and assembly contracts

Affected cabinets measure retained visible device width/height after known floor support removal at the same presentation scale/orientation used during placement. Column and row pitch include .18 m/.15 m clearances, and cabinet width/height derive from these pitches. Existing compact dryer layout and unrelated teaching layouts retain their specialized sizing.

Cabinet roof capacity now measures retained tower head widths, rather than assuming .45 m spacing fits all heads. Roof width includes plate capacity and edge margin; heads are spaced by measured width plus .16 m. This shared correction can alter spacing or widen existing tower enclosures as well as the newly repaired ones. All original head bases, functional lenses and tier children are retained; only known pole/base meshes are hidden. The wastewater exception requires its exact scene, device ID, type and catalogAssetId before accepting that single beacon into the roof inventory.

ControlInstallation stores installationEquipmentIds as the deduplicated explicit union of panel/bench equipment and roof heads. ApplyTemplateOverride uses this complete associated inventory and the existing stored original transforms with one assembly delta. Heads remain root siblings, preserving controller paths. The separate Chicken support is a machine-owned child assembly, not a cabinet sibling or supported template device.

Metadata contracts: roof installationKind=cabinetStatusTower and statusTowerSupport=StatusTowerCoupler_<id>; panel installationKind=operatorPanel; gallery installationKind=studentBench. Chicken root installationKind=machineFrameReadout and machineReadoutSupport=WEIGH_readout_frame_arm. New machine supports are WEIGH_readout_frame_post and WEIGH_readout_frame_arm.

## Files and preservation

Changed only the leased SceneComposer.ControlInstallationPlans.cs, ControlInstallations.cs, StatusTowerMounts.cs and TemplateOverrides.cs; added narrow SceneComposer.MachineReadoutMounts.cs. Before copies and SHA-256 records are in .superpowers/sdd/repeat-cycle-execution-tasks/before/mount-repairs-v1. No scene semantic configuration, source pose, tags, point type, binding, evaluator, plant timing, controller state or runtime annotation was edited. Existing unrelated dirty work was preserved.

## Checks and remaining gates

Delivered beacon and scale GLB node names were read directly; the scale side-frame and housing namespaces match the new narrow helper. Source whitespace check passed. Build and native verification belong to root and have not yet run on this source.

The repaired inventory spans sixteen distinct affected scenes when lights, readouts and the VFD station are combined. Actual cabinet fit, enclosure/head contact, machine motion/access clearance, selection targets, camera visibility, text extremes and rendered readability remain acceptance checks. Native inventory-driven verification must cover all 77 scenes, supported defaults/templates and rebuilt/reopened copies; actual renders must cover every affected scene and appropriate detail views. Root and Dot must inspect those renders before Lab 4.3 or global mounting acceptance. Unknown imported future BYO assets are not automatically covered.

Tote/wastewater floating simulation captions remain explicitly outside this geometry lease. The mislabeled guarded-transfer DIN relay remains a separate naming/placement issue. Main screen and all input remain reserved for the user's game. Wider color/texture rollout remains held; no release, physical safety or commissioning claim follows from this repair.
