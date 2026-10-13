# EV meter identity source handoff V17

Dot-approved bounded repair implemented ONLY in src/Scenes/SceneComposer.InstallationLegends.cs. Final SHA2C34A120CE67AC22359B8B60B03FD21A8BDFF063A7562868973631192DC534CA; beforeD6DF7385DFFD91A21F85129CEDEBE677FAF17BF2808EBDD610B81EA09114CFEA retained at .superpowers/sdd/repeat-cycle-execution-tasks/before/ev-meter-identity-v17 with before-hash.json. Narrow before/after whitespacecheck exit0. Root owns build/native/render; no engine/input/build used here.

Read semantic review ev-chicken-readout-semantic-review-v16b.md and individually inspected actual v16b-ev-before.readouts.png: both existing housings show ENERGY kWh and numeric kWh/0.000 with ambiguous bay identity. This before image is not repaired-render acceptance.

## Exact source delta

ConfigureInstallationIdentification now invokes new exact-scene ConfigureEvMeterIdentification after existing station identity construction and before final bench/pilot work. No generic BOOL guard changed. Two guarded identities:

| Equipment | Existing PLC REAL point | Passive physical text |
|---|---|---|
| training_accessory_5 | energy_kwh | BAY A / PLC kWh |
| ev_meter_b | bay_b_energy_kwh | BAY B / PLC kWh |

Guards require exact equipment trainingAccessory/type/scene label/catalogAssetId training.accessory.energy_meter.v1/display energyKwh/displayLabel kWh; exactly1binding for each equipment AND exactly1binding using its point; numericDisplay mode,label kWh,formatF3; unique source point typeREAL ownerPLC. Exact retained visible METER_enclosure/METER_screen and existing NumericReadout required. PC meter_energy_kwh and bay_b_meter_energy_kwh are not substituted or relabeled.

Measure housing/screen union in existing support frame. Reuse PlaceAdditionalIdentification above the head with20mmgap,.16mheight and widthmin(.48m,measured pitch-.025m), on actual cabinet Door front. This validates fullfascia containment and no projected overlap with neighboring heads or earlier strips. AddIdentificationStrip retains fixed readable font64/.00065 and measured line wrap/ink fit, never shrink-until-pass. Strip+ink live on ControlInstallation; device root only receives passive identification metadata. No numeric label, point, value, F3, equipment transform, runtime model, reset/action/controller or ownership changed.

Expected new plate/ink paths under ControlInstallation:

- InstallationIdentification_training_accessory_5 and _Ink.
- InstallationIdentification_ev_meter_b and _Ink.

Plate metadata installationLegendKind=evPlcEnergyIdentity; sourcePoint exact point; sourcePointTypeREAL; sourcePointOwnerPLC; sourceBindingLabelkWh; sourceDisplayFormatF3. Existing common metadata equipmentId/sourceBindings/passivePresentation/measuredHeadGap/aboveHead/measuredDoorFront retained. This kind must not be tested as BOOL input or pilot provenance.

## Root verification

Build current combined candidate; run existing mount/control/native typed presentation gates with exact currenthash. Independently project unequal PLC REAL values (for example A7.125/B2.375) using existing verification mechanism; prove each NumericReadout remains kWh plus its ownF3value, while each physical identity remains BAY A/B plus PLC kWh. Use independent differing PC meter feedback too if practical to catch wrong-owner projection; do not change sourceJSON or write learner solutions. Reset both and verify correct zero formatting/stale-value clearing through existing runtime path.

Native inspection should compare before/after retained meter root transforms/mesh names/pick roots/binding identity, actual strip-door rear contact,20mmmeasured head clearance, fullplate and glyph containment and adjacentidentity/readout nonoverlap. Then actual ev-readouts/operator render must show BOTH bay identities and unequal readable numeric values in the same frame. Existing beforeframe is very bright and lowerinputstrips behindHUD; those widerlook/HUD limits are separate from this identity repair. No claim of visual completion until root/Dot inspect repaired actualpixels. Material75 HELD; Chicken live readout belongs to another agent and is not changed here.
