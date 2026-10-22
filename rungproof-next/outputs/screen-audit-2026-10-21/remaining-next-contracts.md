# Remaining next UI contracts

Static preparation only. No new fixtures, inputs, runtime calls or app edits. Use current screenshots to ground coordinates; no trustworthy per-control coordinates were found in prior evidence. breadth-<scene-id>-entry/run/reset.png establishes earlier navigation/playback checkpoints only, not functional controls. Fixtures under test-projects are NOT runtime proof. Select exact scene, normal Open, Verify+Load, Run; check actual scan progression. Health event history retains action guard feedback after view switches.

## Lab9.12 EV — lab-9-12-ev-charging-manager

Use fixture-lab-9-12-ev-charging-manager.rpproj.json. Cabinet installation order (not projected left-to-right): lower row A BAY/A AUTH/A CHARGE/A PLUG; next row B BAY/B AUTH/B CHARGE/B PLUG; final centered row A/B meter readouts. Perspective can reverse rows; calibrate actual input changes.

| Physical ID / visible face | Exact input/action |
|---|---|
|switch_1 / A BAY OCCUPIED|bay_occupied / toggle-bay_occupied|
|switch_8 / A AUTH VALID|customer_authorized / toggle-customer_authorized|
|switch_9 / A CHARGE READY|charger_ready / toggle-charger_ready|
|fixture_connector_inserted / A PLUG IN|connector_inserted / toggle-connector_inserted|
|fixture_bay_b_occupied / B BAY OCCUPIED|bay_b_occupied|
|fixture_bay_b_customer_authorized / B AUTH VALID|bay_b_customer_authorized|
|fixture_bay_b_charger_ready / B CHARGE READY|bay_b_charger_ready|
|fixture_bay_b_connector_inserted / B PLUG IN|bay_b_connector_inserted|

B action IDs are toggle- followed by exact input name. All maintained toggles. Actual connector_connected/bay_b_connector_connected are plant feedback, not input toggle substitutes.

Minimum normal path: allfalse and zero allocation →A BAY ON→A PLUG IN while grant/allocation0→A AUTH and READY ON. Expect charge_enable/energy_session_active TRUE and allocated_kw6, actual connector_connected TRUE, delivered_kw positive, cumulative meter_pulse_count/meter_energy_kwh rise. Wait for actual ledger>=1; PLC energy_count follows acknowledged cumulative events and energy_kwh=count/1000. Add occupied/plugged/authorized/ready B: both allocations3, both grants TRUE, total6. Remove A authorization: A grant/allocation0, B allocation6. Remove B readiness: both allocations0. These6/3kW and .001kWh/pulse are documented illustrative QA policy, not hardware ratings.

Lifecycle: APP STOP zeroes PLC images and freezes accepted integration; note actual ledger/private count baseline. RUN republishes retained count without duplication and resumes eligible allocation. Deauthorize/verify allocation0 before unplug; unplug before vehicle removal. Try illegal connector/occupancy action only through actual control and record explicit guard, never infer from no motion. RESET clears fixtures/meters/counts and stays stopped; repeat one valid bay. allocation_inhibited should remain FALSE in correct normal allocation. Meter display is PLC accumulation; raw ledger is independent PC feedback.

## Lab10.3 Vision — lab-10-03-vision-package-sorter

Use fixture-lab-10-03-vision-package-sorter.rpproj.json. Cabinet inventory: switch_9 package_present, switch_10 vision_result_valid, switch_11 destination_clear, training_accessory_6 readout (single four-item row). Physical labels may be generic; confirm exact input via Signals. All three are maintained toggles. vision_class initially1; declared cycle-vision_class values1,2,3,4. Source JSON has no matching physical equipment action for that cycle: locate a real exposed route before claiming class-cycle coverage; never invoke hidden OperatorActions. Class1 normal path is independently testable.

Minimum path: package ON with validity/clear OFF must leave both sort_conveyor_run/diverter_enable FALSE. Enable validity only still false; enable destination_clear last permits both outputs while supported class and NOT actual sort_complete. Observe actual incoming carton, table routing and supported exit; sort_latched_class/sort_phase are SIM observations; sort_complete actual PC feedback terminates commands. No fixture lane mapping is invented: plant latches supported class and owns physical route. Remove each permissive in independent fresh/repeat run and verify commands stop; restoring permits unfinished motion by fixture policy. APP STOP holds poses, RUN restores eligible commands; RESET resets job/inputs before next class/repeat. If class selector route unavailable, record that as coverage limitation while still testing initial class1. Mid-job class changes must be compared with latched class, not newest input.

## Six non-lab scenes

See nonlab-ui-contracts.md and test-projects/nonlab-fixture-manifest.json for source detail. Earlier breadth screenshots exist for all6; no known calibrated coordinates supplied here.

| Scene / current input | Grounded physical IDs | Minimum independent path and expected actual observation |
|---|---|---|
|equipment-gallery / no learner controller required for SIM animation toggle|gallery_switch and gallery_selector→toggle-gallery; gallery_estop→stop-all|Toggle motion ON/OFF; inspect multiple actual fan/conveyor/motor samples. SIM gallery_animation is observation only. stop-all stops gallery/playback; Reset/repeat separately. Do not load fabricated PLC bindings for gallery_animation.|
|conveyor-cell / fixture-conveyor-cell-normal.rpproj.json|operator_station→conveyor_run; main_conveyor child CTRL_estop_button→conveyor-estop|APP RUN then station pulse Start: conveyor_run TRUE, cartons move, actual photoeye_blocked changes, parts_completed SIM increments. Same station while running routes explicit operator.stop→runFALSE. E-stop should stop, estop_ok SIM FALSE; use exposed reset route/APP RESET then freshRUN/Start. No direct SIM estop tag binding.|
|scene-1-conveyor-stop / fixture-scene-1-conveyor-stop-normal.rpproj.json|scene1_station→scene-toggle|APP RUN then stationStart moves one package until actual simulated_photoeye TRUE drops conveyor_running; package stays supported at sensor. Stop/Run alone must not evade blocked sensor. RESET+RUN+freshStart repeats. object_position/component_state are SIM observations only.|
|tank-high-low / probe-tank-high-low-manual-output-guard.rpproj.json|hl_pump_station→toggle-pump;hl_drain_station→toggle-drain|GUARD ONLY: valid probeRUNNING, physically pump and drain then inspect Health for exact missing-command-binding block. Both controls target PLC outputs with absent bindings. False-output probe stationary behavior is expected, no normal fill/drain credit. Documented future process contract25/75percent switches not tested by this probe.|
|tank-level / probe-tank-level-manual-output-guard.rpproj.json|pump_station→toggle-pump;drain_station→toggle-drain|Same guard-only procedure; no normal-flow credit. Initial42%, level_transmitter10.72mA; observe baseline only. Future process contract low/high20/80%, signal4+0.16*level%; cannot claim it from stationary probe.|
|tank-radar / fixture-tank-radar-normal.rpproj.json|radar_pump_station→cycle_request toggle;radar_drain_station→manual_drain_request toggle|RUN, CYCLE ON/DRAIN OFF→inlet TRUE, level rises/distance falls; DRAIN ON while CYCLE ON→pumpFALSE/drainTRUE,level falls/distance rises. CYCLE OFF→bothFALSE. Actual radar_signal=4+0.16*radar_level%, echo initiallyTRUE. Inspection ghost view via real exposed toggle-inspection if available; its SIM flag must not be wired as PLC. This QA two-mode controller does NOT prove automatic high/low cycling. Stop holds liquid, Run restores request logic, Reset restores35% and requestsfalse; repeat.|

Conveyor Start/Stop are declared bound aliases already in existing normal fixtures; tank raw-output controls lack aliases and must not be patched by inventing them. Gallery and radar inspection routes may be scene-local SIM actions; always distinguish their visible response from local controller Run status. Current reports/Health evidence may supersede this static plan; never relabel a scene as failed solely because a pending route has no coordinates.
