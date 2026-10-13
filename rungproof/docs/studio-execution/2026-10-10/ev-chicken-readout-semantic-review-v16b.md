# EV/Chicken readout identity diagnosis

Read-only source and actual V12 image review. Individually inspected industrial-mount-renders-v12/ev-readouts.png and chicken-machine-support.png. These are historical images, not current V16b renders. No source edit/build/engine. Handle OPEN; material75 HELD.

## EV9.12: correct independent values, missing physical bay identity

Exact equipment training_accessory_5 declares Bay A PLC accumulated kWh; ev_meter_b declares Bay B PLC accumulated kWh. Both use training.accessory.energy_meter.v1 and config.displayLabel=kWh. Scene bindings are energy_kwh→training_accessory_5 and bay_b_energy_kwh→ev_meter_b, numericDisplay, label kWh, formatF3. Both points are PLC REAL, unlike separate PC REAL meter_energy_kwh/bay_b_meter_energy_kwh feedback. Do not relabel these readouts as actual measured meter feedback.

SceneComposer.cs:353 creates NumericReadout; SceneSimulationRuntime.cs:1436–1447 rewrites it from the exact binding label and current numeric point each projection. Changing only displayLabel would be overwritten. V12 actual render shows two identical ENERGY kWh housings and kWh/0.000 values, with no visible A/B association. The data path is distinct; the physical identity is ambiguous.

Minimal presentation fix: exact guarded passive BAY A / PLC kWh and BAY B / PLC kWh identification on their respective retained meter housings or adjacent measured cabinet fascia. Keep NumericReadout/value/unit/F3 and both bindings unchanged. Guards: scene9.12, exact two IDs/type/catalog asset, unique numericDisplay point/label/format, each PLC REAL owner. Preserve housing/lens/display/pick roots. Avoid generic position-based A/B inference. Native test should independently project unequal A/B values, verify each correct label/value, and actual operator/detail render must show bay identity at normal readable scale. This adds no counter/energy behavior or solution fixtures.

## Chicken10.2: imported static text already hidden; live value detached from screen

Scene training_accessory_5 is the checkweigher, with no numericDisplay pointBinding/config display. ResetChickenLabelPlant (`SceneSimulationRuntime.ChickenLabel.cs:18–24`) explicitly hides WEIGH_readout_static_text. The apparent concern is therefore not a stale imported numeric value: V12 actual screen is blank while floating MEASURED1.237kg appears above it.

Existing MeasuredWeight Label3D is created at line68 as billboard FontSize36/PixelSize.003; line72 positions it above the delivered equipment envelope. PublishChickenLabelPlant line100 publishes plant MeasuredKg to PC weight_kg, and line122 sets the same existing label to LOAD PRODUCT, WEIGHING..., or MEASURED <F3> kg from actual WeighStable/ProductPresent. This already supplies truthful plant-owned content; no new numeric binding/model is needed.

Delivered GLB exact WEIGH_readout_screen is .490×.170×.018m, centered local(0,1.410,−.702), front+Z plane−.693. Housing remains .580×.310×.160m and machine-support repair preserves it. Minimal repair: anchor existing MeasuredWeight label to this exact retained screen face, with appropriate non-billboard orientation, centered measured inset and readable fixed text scale that fits all three existing strings. Preserve publisher strings/F3/kg, PC weight_kg timing, reset semantics and all physical/motion transforms. Keep hidden imported text hidden. This is relocation/fit of existing live presentation, not a synthesized PLC output or new process behavior.

Optional conceptual caption may remain only if necessary for overview readability; avoid two conflicting readouts. Native acceptance must check live screen reset=LOAD PRODUCT, loading=WEIGHING..., stable=actualF3kg and changed product mass, plus retained screen contact/font bounds/readable actual detail and normal views. No claim that historical caption size fits the .49×.17m screen; measure font metrics before implementation rather than shrink until text merely passes.
