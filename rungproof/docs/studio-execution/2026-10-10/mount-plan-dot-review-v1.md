# Dot mounting plan review v1

Reviewed 2026-10-11, read-only. Product source, build, engine, screen and input were untouched.

## Decision

APPROVED TO PROCEED with the ten identified light-mount repairs across seven scenes under the existing all-scenes repair authorization and the latest explicit floor-light correction. This is approval of the bounded implementation plan, not acceptance of its finished geometry or global coverage. No further human permission is necessary to repair these specifically rejected mounting defects. Wider color/texture rollout across the other 75 scenes remains HELD.

The display/readout inventory is still underway. Do not claim this light inventory establishes display compliance. Combine display findings into the same geometry ownership lease before shared installation files are edited; publish the resulting exact device inventory and support choice. Lab 4.3 visual acceptance remains HELD until its two towers and existing count display pass actual mounted renders.

## Reviewed evidence

The inventory reports all 77 source scenes: 123 candidates, 121 actual lights/signals, one optical curtain, one mislabeled DIN relay. Ten genuine light gaps span gallery, wastewater, fume extractor, repeat-cycle, sequence tower, parking entry and chain lift. These numbers are source classification, not execution evidence. Eight genuine roadway/pedestrian signals retain street mounting; parking availability does not qualify for that exception.

Reviewed SceneComposer.StatusTowerMounts.cs, ControlInstallations.cs, ControlInstallationPlans.cs, TemplateOverrides.cs and Main.StatusTowerReview.cs. The existing tower helper preserves roots, basis and functional lenses; measured head-bottom/coupler contact is preferable to replacing heads. Its strict indicator/multiple-colors check correctly excludes single pilots and the wastewater trainingAccessory. Existing fixture verifier covers Boolean lens projection and reset, but only devices explicitly declared in tower metadata: it cannot detect an omitted inventory device by itself.

## Required implementation constraints

1. Use one leased geometry owner for the composer partials. Add Lab 4.3 indicator_2/indicator_4, Lab 4.4 indicator_2 and parking available_lamp to the tower map. Preserve all configured colors, including the four-tier blue lens. Remove obsolete gallery/parking floor exceptions.
2. Put chain switch_8/switch_9/switch_10 through the existing single-pilot panel branch, despite their IDs. Do not convert multi-tier towers into pilot lenses. For hood_light explicitly choose a cabinet pilot or supported machine lamp; its current standard single-beacon asset does not independently establish an inspection-light mounting.
3. Give gallery_indicator a coherent bench or display-board assembly and a nonempty installation plan. A hidden floor base plus unsupported elevated head is insufficient.
4. Mount wastewater training_accessory_9 through a narrowly identified recognized beacon asset path. Validate its actual mesh namespaces and support contact; do not generically hide arbitrary trainingAccessory parts.
5. Preserve equipment IDs, controller paths, lens names/material state, point ownership/types, typed text and binding definitions. Retain selection and camera visibility. Do not edit source scene poses solely to conceal supports or introduce broad finish changes.
6. Mounted heads are siblings of ControlInstallation, not its children. ApplyTemplateOverride transforms only ControlInstallationPlans EquipmentIds. Current supported templates have bench-mounted devices in that list; a new support association outside that list requires an explicit transform propagation decision and test. Never claim a support transform carries sibling heads automatically.
7. Cabinet width currently derives from selected panel controls, whereas tower spacing is fixed at 0.45 metres. Added towers and combined display inventory can change required enclosure width. Verify every head remains above the enclosure, couplers bear on its roof, panel controls fit, labels do not overlap, and the cabinet remains outside machine/conveyor motion envelopes. Source metadata alone cannot prove these.
8. Classify displays by actual composed asset/controller behavior, not IDs. In particular speed_display is currently an indicator tower; it must not be counted as a numeric readout merely from its name. Typed STRING/ENUM/numeric displays must remain readable cabinet-face or supported HMI/bench components, with meaningful tag feedback preserved.

## Acceptance evidence before closure

Require an independent inventory-driven check of all 77 scenes against actual mounted device metadata and visible supports, including devices omitted from current helper maps. Include five demos, gallery, supported belt/roller defaults, supported templates and copied/reopened/rebuilt layouts; explicitly distinguish unsupported arbitrary future BYO assets.

Capture every affected scene and useful close view on the secondary monitor without focus/input takeover. Inspect actual support contact, no residual industrial floor poles, cabinet/bench attachment, no overlapping controls, legible text and all tower tiers. Root and Dot must inspect the actual resulting renders; a subset does not establish all-scene visual coverage.

Run actual bound output/lens and typed readout checks, input/selection checks where available without reserved input, Stop/Reset pose/state retention, joint reopen and rebuild, and supported installation transform regression. Label direct output fixtures honestly as projection checks, not learner-program scan acceptance. Keep missing native interactive evidence explicit while the primary screen/input are reserved.

The mislabeled guarded-transfer DIN relay remains a separate naming/placement issue. Neither this plan nor the source inventory establishes physical safety, electrical design, commissioning or release approval.

## Combined display inventory addendum

Reviewed the finalized industrial-display-mount-inventory-v1.md and underlying cookie-counter composer, ChickenLabel runtime and drive/checkweigher modeling source. APPROVED TO PROCEED with the eleven plain freestanding heads across nine scenes, plus the separate Chicken checkweigher support and VFD station correction, under the same existing repair authorization. They are distinct from the ten light gaps; do not combine counts into a misleading scene total because scenes overlap. The inventory's affected-scene Markdown table has a malformed separator/header boundary; fix that report formatting independently of product work.

Add only these plain display IDs to the relevant explicit installation plans: Chicken training_accessory_7; vision training_accessory_6; parking occupancy_display; package grouping group_count_display; cookie cookie_counter/package_counter; cable training_accessory_8; luggage training_accessory_6; pallet counting training_accessory_6; EV training_accessory_5/ev_meter_b. Preserve their root IDs and NumericReadout/StaticReadout lookup paths, existing text bindings, format/units, STRING/ENUM semantics and input config.action/collision selection where applicable.

Extend original-mount removal narrowly for the known EV METER_base/METER_pedestal and cookie COUNT_foot/COUNT_mast namespaces. Generic trainingAccessory hiding is rejected: several accessories contain functional field sensor, printer, process-meter or machine geometry. Cabinet layout must measure retained head sizes; fixed panel pitch and uniform 0.70 scale are not acceptance evidence for long formatted text or dual EV meters. Preserve useful operator facing orientation and readability without silently replacing live displays with decorative screens.

Chicken training_accessory_5 is a complete scale assembly. Do not move the whole root to the cabinet, shift the deck/product path, or discard its load-cell/frame geometry. Retain the readout housing/screen under that same root and replace its independent floor foot/mast with a visibly connected machine-frame support outside the product envelope. Its live MeasuredWeight is presently a billboard above the delivered assembly, not screen ink. Geometry repair must not claim that billboard became a functional HMI. Moving live weight text to the physical screen is a separate narrowly scoped presentation change with explicit runtime ownership and acceptance, if chosen.

The VFD training_accessory_3 has real drive body, backboard and mount rails, but its floor stand alone is insufficient for the requested cabinet/HMI rule. Prefer integrating the retained drive/head into a credible control/service enclosure. An alternative operator station must show a purposeful enclosed mounting/support, with drive rails/body preserved; simply renaming the existing stand is insufficient. Do not treat its static DEMO screen as the already bound alarm STRING display, training_accessory_4.

Tote and wastewater floating observation annotations must be explicitly identified as simulation captions or separately designed supported displays. Keep runtime caption movement outside the current geometry lease. Printer body screens, pipe-mounted flow meter, supported sensor-integrated displays and roadway glyphs have distinct ownership; do not remount them from name matching.

The lease may include SceneComposer.TemplateOverrides.cs solely for declared installation-association transform propagation. Existing equipment heads stay root siblings to preserve controller paths. Build a complete explicit associated-head inventory for support transforms rather than relying on parenting or panel IDs alone; include roof-mounted towers and any new separate supported HMI station. Preserve original transforms and use one delta per assembly for reset/undo/redo/joint reopen, without double-transforming heads present in more than one inventory.

Combined acceptance requires all nine display-repair scenes, the Chicken scale and the resolved VFD station in actual normal/detail renders, plus long valid/wrong STRING values, ENUM states, numeric extrema/units and NO DATA. Retest existing 31 panel/bench readouts as comparators through inventory/native projection coverage. Source membership is not rendered readability. No primary-screen or keyboard/mouse access is authorized by this addendum; secondary nonfocus captures and headless checks remain the permitted evidence path.
