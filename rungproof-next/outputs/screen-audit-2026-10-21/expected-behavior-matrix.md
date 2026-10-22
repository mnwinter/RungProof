# Expected behavior for screen audit

Prepared 2026-10-21 from current local documentation and source routing. This is a specification matrix, not executed behavior evidence. All 77 scenes remain untested in this preparation task. Operator action labels below come from help documents; visibility and effectiveness must be checked on screen.

Reference logic classification: five demo scenes have authored programs; three additional labs have authored reference programs; conveyor-cell has a supplied example. Other controller exercises create SceneIO tags without solution networks (SimulatorShell.cs:705-745; SceneLadderProject.cs:15-36; AuthoredLessonLadderPrograms.cs:9-18). A restored user draft can change the initial editor contents. Equipment Gallery is a SIM-only visual display; no learner ladder is required for its documented gallery controls.

## Five authored demo acceptance checks

- **1 — Press count:** Press 1–2 times, verify lamp off; press 3, verify lamp on; pulse Reset and verify CV/lamp clear; repeat the full cycle. A held button counts once per accepted rising edge. No E-stop is added to this non-motion exercise.
- **2 — Delayed lamp:** Verify the exact scan boundary, request-off timer reset, Stop output removal, Reset timer clear, and a repeat cycle without app Reset. No E-stop is added to this non-motion exercise.
- **3 — Height sorting pusher cell:** Exercise short, tall, and boundary-height cartons. Trace measured height → PLC compare/state → conveyor and pusher FB/DB outputs → plant movement → destination feedback. Verify short cartons pass, tall cartons transfer once, pusher retracts before the next feed, blocked sensors inhibit restart, trip requires clear+Reset+fresh Start, and Reset restores a repeatable test state. The simulated trip is not safety-rated.
- **4 — Batch inspection and accumulation:** Verify invalid products do not count; enabling a permissive while detection is held does not invent an edge; only a new accepted event increments the count. Stop policy and Reset policy are explicit, repeatable, and visible; FB instances are called from Main with inspectable DBs where stateful. Verify batch completion and start a second batch using machine Reset.
- **5 — Integrated palletizing cell:** Prove each sequence transition is decided by scanned LAD and called blocks; each actuator command reaches the plant and feedback returns on a later scan. Exercise all axes/directions, grip/release, four placements, Stop/restart, trip/permissive/fault inhibition, Reset and a second complete layer. No plant-owned phase enum or high-level `gantry_cycle` command may sequence motion.

Source: rungproof-next/docs/DEMO_FUNCTIONAL_ACCEPTANCE.md, Intended progression and meaningful behavior table. Completion targets shown by SimulatorShell.cs curriculum: Demo 1 threshold_lamp=true; Demo 2 delayed_lamp=true after 2000 ms; Demo 3 parts_completed>=3; Demo 4 pallet_count>=5; Demo 5 placed_cartons>=4.

## Scene matrix

### conveyor-cell — Conveyor Inspection Cell

- Operator steps: Use documented actions: `Start / stop inspection conveyor` (`toggle`: `conveyor_run`); `Emergency stop conveyor` (`emergencyStop`: `estop_ok`); `Reset emergency stop` (`reset`: ``)
- Expected result/completion: No explicit completion criterion. Purpose: Three reusable cartons circulate on a motor-driven conveyor. The through-beam photoeye changes state as each carton crosses it.
- Documented action controls: `Start / stop inspection conveyor` (`toggle`: `conveyor_run`); `Emergency stop conveyor` (`emergencyStop`: `estop_ok`); `Reset emergency stop` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Start / stop inspection conveyor` (`toggle`: `conveyor_run`); `Emergency stop conveyor` (`emergencyStop`: `estop_ok`); `Reset emergency stop` (`reset`: ``)
- Learner-authored logic: No new learner logic required for supplied conveyor example; Verify + Load is required.
- Source: rungproof-next/docs/help/scenes/conveyor-cell.md

### equipment-gallery — Reusable Equipment Gallery

- Operator steps: Use documented actions: `Start / stop gallery motion` (`toggle`: `gallery_animation`); `Emergency stop gallery motion` (`stop`: ``)
- Expected result/completion: No explicit completion criterion. Purpose: A visual inventory of reusable catalog models created by the runtime scene composer. Level instruments are shown on illustrative display stands.
- Documented action controls: `Start / stop gallery motion` (`toggle`: `gallery_animation`); `Emergency stop gallery motion` (`stop`: ``)
- Documented fault/Stop/Reset/permissive actions: `Start / stop gallery motion` (`toggle`: `gallery_animation`); `Emergency stop gallery motion` (`stop`: ``)
- Learner-authored logic: No ladder needed for SIM-only gallery controls.
- Source: rungproof-next/docs/help/scenes/equipment-gallery.md

### lab-10-01-drive-alarm-code-string — Lab 10.1 - Drive Alarm-Code String

- Operator steps: F003 matches the default selected_alarm_code; F030, F0030, prefixes, lowercase and empty messages do not. Edit selected_alarm_code in the tag table, Apply then Verify + Load to select a different code.
- Expected result/completion: The live display shows received STRING text; alarm_code_found and both alarm lamps are derived by the loaded program.
- Documented action controls: `Toggle drive alarm string valid` (`toggle`: `drive_alarm_string_valid`); `Next received alarm message` (`cycle`: `drive_alarm_text`); `Toggle alarm reset` (`toggle`: `alarm_reset`)
- Documented fault/Stop/Reset/permissive actions: `Toggle drive alarm string valid` (`toggle`: `drive_alarm_string_valid`); `Next received alarm message` (`cycle`: `drive_alarm_text`); `Toggle alarm reset` (`toggle`: `alarm_reset`)
- Learner-authored logic: No new learner logic required: authored lesson reference available; Verify + Load is required.
- Source: rungproof-next/docs/help/scenes/lab-10-01-drive-alarm-code-string.md

### lab-10-02-chicken-label-print — Lab 10.2 - Chicken Label Print

- Operator steps: Wait for product_weighed and require printer_ready and label_data_valid. Supply nonempty label_text (maximum 255 characters), then raise print_request. Wait for print_complete before raising apply_request; observe application_complete.
- Expected result/completion: Measured weight is zero until stable, then default product measures 1.237 kg. Paper extrudes while printing and travels to tray only on separate application command. Printed text remains the exact accepted PLC text even if label_text later changes.
- Documented action controls: `Load and weigh product` (`chickenLoad`: `product_weighed`); `Toggle printer ready` (`toggle`: `printer_ready`); `Toggle label data valid` (`toggle`: `label_data_valid`)
- Documented fault/Stop/Reset/permissive actions: `Toggle printer ready` (`toggle`: `printer_ready`)
- Learner-authored logic: No new learner logic required: authored lesson reference available; Verify + Load is required.
- Source: rungproof-next/docs/help/scenes/lab-10-02-chicken-label-print.md

### lab-10-03-vision-package-sorter — Lab 10.3 - Vision Package Sorter

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: Sorting is enabled only for a present package with a valid vision result and clear destination.
- Documented action controls: `Select simulated vision class (1-4)` (`cycle`: `vision_class`); `Toggle package present` (`toggle`: `package_present`); `Toggle vision result valid` (`toggle`: `vision_result_valid`); `Toggle destination clear` (`toggle`: `destination_clear`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-10-03-vision-package-sorter.md

### lab-10-04-motor-enum-state — Lab 10.4 - Motor Operating-State Enum

- Operator steps: Start changes motor_state from Stopped to Running; the shaft follows motor_running derived from that named state. Stop returns Stopped. A fault enters Fault and remains latched after the fault input clears. Clear the fault input and press FAULT RESET, then release and press Start again.
- Expected result/completion: The live state display and motor_state watch show the actual loaded ENUM value. Changing the Running transition MOV to Stopped, then Verify + Load, prevents the motor from running on Start.
- Documented action controls: `Toggle start request` (`toggle`: `start_request`); `Toggle stop request` (`toggle`: `stop_request`); `Toggle fault active` (`toggle`: `fault_active`); `Reset motor fault` (`pulse`: `reset_request`)
- Documented fault/Stop/Reset/permissive actions: `Toggle stop request` (`toggle`: `stop_request`); `Toggle fault active` (`toggle`: `fault_active`); `Reset motor fault` (`pulse`: `reset_request`)
- Learner-authored logic: No new learner logic required: authored lesson reference available; Verify + Load is required.
- Source: rungproof-next/docs/help/scenes/lab-10-04-motor-enum-state.md

### lab-10-05-motor-struct-data — Lab 10.5 - Motor STRUCT Data

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: The motor record is accepted only when its required fields are valid and alarms are clear.
- Documented action controls: `Toggle motor record valid` (`toggle`: `motor_record_valid`); `Toggle temperature valid` (`toggle`: `temperature_valid`); `Toggle alarm clear` (`toggle`: `alarm_clear`)
- Documented fault/Stop/Reset/permissive actions: `Toggle alarm clear` (`toggle`: `alarm_clear`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-10-05-motor-struct-data.md

### lab-10-06-ten-motor-array-startup — Lab 10.6 - Ten-Motor Array Startup

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: Accepted commands drive their corresponding conveyor zones and actual occupancy follows material motion. Valid wrong logic has its actual consequence; invalid current logic cannot execute. No automatic delay, alarm interlock or grader is implied.
- Documented action controls: `Toggle group start request` (`toggle`: `group_start_request`); `Toggle all motors ready` (`toggle`: `all_motors_ready`); `Toggle group alarm clear` (`toggle`: `group_alarm_clear`)
- Documented fault/Stop/Reset/permissive actions: `Toggle all motors ready` (`toggle`: `all_motors_ready`); `Toggle group alarm clear` (`toggle`: `group_alarm_clear`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-10-06-ten-motor-array-startup.md

### lab-11-06-wastewater-collection — Lab 11.6 - Wastewater Collection

- Operator steps: Enable inflow and observe normalized bank level and modeled 4-20 mA feedback rise. With treatment ready and outlet clear, PLC logic commands transfer from high to low threshold. Disable inflow to drain the bank; inspect hysteretic source-high drop at 20%.
- Expected result/completion: All four equal-size vessels share the common-header bank level. Actual transfer depends on pump command, delivered valve opening and both downstream permissives. A full bank clamps at 100% and reports rejected inflow as overflow; no external receiving tank is modeled.
- Documented action controls: `Toggle incoming wastewater` (`toggle`: `inflow_enabled`); `Toggle treatment ready` (`toggle`: `treatment_ready`); `Toggle outlet clear` (`toggle`: `outlet_clear`)
- Documented fault/Stop/Reset/permissive actions: `Toggle incoming wastewater` (`toggle`: `inflow_enabled`); `Toggle treatment ready` (`toggle`: `treatment_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-11-06-wastewater-collection.md

### lab-11-07-multi-conveyor-pallet-route — Lab 11.7 - Multi-Conveyor Pallet Route

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: A blocked zone removes the affected run command while preserving a diagnosable handoff state.
- Documented action controls: `Toggle common stop request` (`toggle`: `common_stop_request`); `Toggle zone 1 clear` (`toggle`: `zone_1_clear`); `Toggle zone 2 clear` (`toggle`: `zone_2_clear`); `Toggle zone 3 clear` (`toggle`: `zone_3_clear`)
- Documented fault/Stop/Reset/permissive actions: `Toggle common stop request` (`toggle`: `common_stop_request`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-11-07-multi-conveyor-pallet-route.md

### lab-11-11-service-elevator — Lab 11.11 - Service Elevator

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: The elevator receives one direction command only when the doors are closed and the landing is clear.
- Documented action controls: `Toggle call valid` (`toggle`: `call_valid`); `Toggle doors closed` (`toggle`: `doors_closed`); `Toggle landing clear` (`toggle`: `landing_clear`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-11-11-service-elevator.md

### lab-11-12-mobile-traffic-lights — Lab 11.12 - Mobile Traffic Lights

- Operator steps: Load the explicit training reference, enable READY and both manual CLEAR fixtures, then queue A and B. One-second startup all-red precedes A green; 6 s green, 1 s amber and minimum 1 s all-red alternate the direction. Keep all-red until crossing_clear. Retain departed vehicles visibly; use QUEUE to explicitly reintroduce a completed vehicle.
- Expected result/completion: Only one road receives a green command at a time, and both roads stop on a fault. Actual occupancy and crossing_clear follow vehicle footprints, not PLC output echoes. No opposing vehicle entry while the other vehicle route remains committed. Both red on entry-fixture fault while scanning the explicit reference.
- Documented action controls: `Toggle controller ready` (`toggle`: `controller_ready`); `Toggle road a clear` (`toggle`: `road_a_clear`); `Toggle road b clear` (`toggle`: `road_b_clear`); `Queue road A training vehicle` (`trafficRequestA`: ``); `Queue road B training vehicle` (`trafficRequestB`: ``)
- Documented fault/Stop/Reset/permissive actions: `Toggle controller ready` (`toggle`: `controller_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-11-12-mobile-traffic-lights.md

### lab-11-13-xy-palletizing — Lab 11.13 - XY Palletizing Cell

- Operator steps: Run, then press Start once to begin the four-carton batch. After each home return, the carton-load supply stages the next carton at the supported pickup station until all four positions are filled. The gantry lowers onto the carton, dwells 750 ms, picks up, carries to the next pallet slot, releases on the pallet, and returns home. The layer-complete lamp follows the fourth completed home return. `STEP` shows the PLC sequence state while the X/Y/Z encoder displays track simulated position relative to pickup home.
- Expected result/completion: The pickup carton remains on a table until attached. Carried cartons follow the tool; released cartons remain on four separate pallet slots. `STEP` advances through the PLC-owned sequence while encoder counts change with simulated travel. Home is generated from the completed return; the placement counter advances after release and home return.
- Documented action controls: `Toggle system enable` (`toggle`: `system_enable`); `Start / resume four-carton batch` (`pulse`: `start_command`); `Machine Stop` (`pulse`: `stop_command`); `Machine Reset` (`pulse`: `reset_command`); `Toggle simulated safety trip (training only)` (`toggle`: `safety_trip`); `Toggle pallet permissive` (`toggle`: `pallet_position_valid`)
- Documented fault/Stop/Reset/permissive actions: `Toggle system enable` (`toggle`: `system_enable`); `Machine Stop` (`pulse`: `stop_command`); `Machine Reset` (`pulse`: `reset_command`); `Toggle simulated safety trip (training only)` (`toggle`: `safety_trip`); `Toggle pallet permissive` (`toggle`: `pallet_position_valid`)
- Learner-authored logic: No new learner logic required: authored demo available; verify selected/loaded program on screen.
- Source: rungproof-next/docs/help/scenes/lab-11-13-xy-palletizing.md

### lab-11-19-powder-batch-mixer — Lab 11.19 - Powder Batch Mixer

- Operator steps: Apply the requested input condition. Verify the feeder gate opens during dosing, the mixer drive turns during mixing, and the discharge valve opens only after the batch permissives are satisfied.
- Expected result/completion: Input actions toggle recipe_valid, dose_complete and mixer_ready. PLC output indicators display dose_run, mixer_run and discharge_valve_open from the user's controller. The same outputs visibly command the slide gate, mixer motor and discharge valve; they do not create simulated material or fabricated process feedback. A correct controller should enforce dosing before mixing and discharge after batch completion; the initial-safe-state case alone does not prove that sequence.
- Documented action controls: `Toggle recipe valid` (`toggle`: `recipe_valid`); `Toggle dose complete` (`toggle`: `dose_complete`); `Toggle mixer ready` (`toggle`: `mixer_ready`)
- Documented fault/Stop/Reset/permissive actions: `Toggle mixer ready` (`toggle`: `mixer_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-11-19-powder-batch-mixer.md

### lab-2-01-workstation-call — Lab 2.1 - Workstation Call Lamp

- Operator steps: Observe the initial input state. Apply material_call_pressed. Verify the PLC produces only the required material_call_on.
- Expected result/completion: The call lamp follows the button state: off when released and on when pressed.
- Documented action controls: `Press / release material call` (`toggle`: `material_call_pressed`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-01-workstation-call.md

### lab-2-02-dual-confirmation — Lab 2.2 - Dual Confirmation Lamp

- Operator steps: Observe the initial input state. Apply operator_confirmed, quality_confirmed. Verify the PLC produces only the required handoff_ready.
- Expected result/completion: The ready lamp is on only when both confirmation inputs are active.
- Documented action controls: `Press / release operator confirm` (`toggle`: `operator_confirmed`); `Press / release quality confirm` (`toggle`: `quality_confirmed`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-02-dual-confirmation.md

### lab-2-03-service-marker-inhibit — Lab 2.3 - Service Marker Inhibit

- Operator steps: Observe the initial input state. Apply marker_inhibit_pressed. Verify the PLC produces only the required service_marker_on.
- Expected result/completion: The marker is on with the button released and off while the inhibit input is active.
- Documented action controls: `Press / release marker inhibit` (`toggle`: `marker_inhibit_pressed`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-03-service-marker-inhibit.md

### lab-2-04-two-station-call — Lab 2.4 - Two-Station Call Beacon

- Operator steps: Observe the initial input state. Apply north_call_pressed, south_call_pressed. Verify the PLC produces only the required assistance_call_on.
- Expected result/completion: The beacon is off with neither input and on when either or both call inputs are active.
- Documented action controls: `Press / release north call` (`toggle`: `north_call_pressed`); `Press / release south call` (`toggle`: `south_call_pressed`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-04-two-station-call.md

### lab-2-05-bay-light-selector — Lab 2.5 - Bay Light Selector

- Operator steps: Observe the initial input state. Apply selector_position. Verify the PLC produces only the required bay_a_command, bay_b_command.
- Expected result/completion: Position 0 de-energizes both lights; position 1 energizes both.
- Documented action controls: `Advance selector` (`cycle`: `selector_position`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-05-bay-light-selector.md

### lab-2-06-ready-attention — Lab 2.6 - Ready / Attention Button

- Operator steps: Observe the initial input state. Apply request_held. Verify the PLC produces only the required ready_light, attention_light.
- Expected result/completion: Exactly one indication is on in both released and held states.
- Documented action controls: `Press / release request` (`toggle`: `request_held`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-06-ready-attention.md

### lab-2-07-dual-contact-permissive — Lab 2.7 - Dual-Contact Permissive

- Operator steps: Observe the initial input state. Apply reset_request, stop_contact_nc. Verify the PLC produces only the required permit_output.
- Expected result/completion: Normal state is off; either logical request turns the permit lamp on.
- Documented action controls: `Toggle NO reset` (`toggle`: `reset_request`); `Toggle NC contact` (`toggle`: `stop_contact_nc`)
- Documented fault/Stop/Reset/permissive actions: `Toggle NO reset` (`toggle`: `reset_request`); `Toggle NC contact` (`toggle`: `stop_contact_nc`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-07-dual-contact-permissive.md

### lab-2-08-inspection-vote — Lab 2.8 - Inspection Vote Stacklight

- Operator steps: Observe the initial input state. Apply vote_a, vote_b. Verify the PLC produces only the required pass_indication, hold_indication, conflict_indication.
- Expected result/completion: Single votes select green or amber; simultaneous votes select red only.
- Documented action controls: `Toggle inspector A` (`toggle`: `vote_a`); `Toggle inspector B` (`toggle`: `vote_b`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-08-inspection-vote.md

### lab-2-09-maintenance-beacon — Lab 2.9 - Maintenance Beacon Selector

- Operator steps: Observe the initial input state. Apply beacon_position. Verify the PLC produces only the required red_beacon, amber_beacon, green_beacon.
- Expected result/completion: Each selector position produces exactly the documented beacon state.
- Documented action controls: `Advance beacon selector` (`cycle`: `beacon_position`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-09-maintenance-beacon.md

### lab-2-10-dust-collector-seal-in — Lab 2.10 - Dust Collector Seal-In

- Operator steps: Observe the initial input state. Apply collector-start, collector-stop. Verify the PLC produces only the required collector_run.
- Expected result/completion: Start seals in the run command; stop removes it.
- Documented action controls: `Pulse collector start` (`pulse`: `collector_start_request`); `Pulse collector stop` (`pulse`: `collector_stop_request`)
- Documented fault/Stop/Reset/permissive actions: `Pulse collector stop` (`pulse`: `collector_stop_request`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-10-dust-collector-seal-in.md

### lab-2-11-inbound-tote-stop — Lab 2.11 - Inbound Tote Stop

- Operator steps: feeding scanning clearing ready
- Expected result/completion: The tote stops at the scanner, records completion, clears, and leaves the conveyor stopped.
- Documented action controls: `Start tote cycle` (`start`: `scan-cycle`); `Stop conveyor safely` (`stop`: ``); `Reset tote` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop conveyor safely` (`stop`: ``); `Reset tote` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-11-inbound-tote-stop.md

### lab-2-12-assembly-lift — Lab 2.12 - Ergonomic Assembly Lift

- Operator steps: raising at top
- Expected result/completion: Raise stops at the top limit; lower stops at the bottom limit; opposed outputs are never on together.
- Documented action controls: `Raise to work height` (`start`: `raise`); `Lower to load height` (`start`: `lower`); `Stop lift` (`stop`: ``); `Reset lift` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop lift` (`stop`: ``); `Reset lift` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-12-assembly-lift.md

### lab-2-13-coolant-jug-fill — Lab 2.13 - Coolant Jug Filling Cell

- Operator steps: indexing filling level reached discharging ready
- Expected result/completion: The valve cannot remain open after the high probe; the filled jug exits and the conveyor stops.
- Documented action controls: `Start jug fill` (`start`: `fill-cycle`); `Stop fill cell` (`stop`: ``); `Reset jug` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop fill cell` (`stop`: ``); `Reset jug` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-13-coolant-jug-fill.md

### lab-2-14-sump-pump — Lab 2.14 - Sump Dewatering Pump

- Operator steps: level rising pump latched pumping down low level stop
- Expected result/completion: High float starts the pump; it remains on through the deadband and stops only at low float.
- Documented action controls: `Simulate sump cycle` (`start`: `level-cycle`); `Stop pump safely` (`stop`: ``); `Reset sump` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop pump safely` (`stop`: ``); `Reset sump` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-14-sump-pump.md

### lab-2-15-fume-extractor — Lab 2.15 - Weld Fume Extractor

- Operator steps: Observe the initial input state. Apply hood_light_request, fan_selector_position. Verify the PLC produces only the required hood_light_on, fan_run, fan_speed_percent.
- Expected result/completion: Light is independent; selector positions command off/35/65/100% and one speed indication.
- Documented action controls: `Toggle hood light request` (`toggle`: `hood_light_request`); `Advance fan speed` (`cycle`: `fan_selector_position`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-15-fume-extractor.md

### lab-2-16-safe-drill — Lab 2.16 - Fixture-Safe Drill Station

- Operator steps: spindle start drilling bottom dwell retracting cycle complete
- Expected result/completion: Start is blocked without both requests; a valid cycle drills, retracts, and stops at top.
- Documented action controls: `Toggle left hand request` (`togglePoint`: `left_hand_request`); `Toggle right hand request` (`togglePoint`: `right_hand_request`); `Start guarded drill cycle` (`start`: `drill-cycle`); `Stop spindle and hold` (`stop`: ``); `Reset drill` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Start guarded drill cycle` (`start`: `drill-cycle`); `Stop spindle and hold` (`stop`: ``); `Reset drill` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-16-safe-drill.md

### lab-2-17-pallet-robot — Lab 2.17 - Twin-Container Pallet Cell

- Operator steps: approach and grip first lift and carry first around the robot pedestal lower and release first into its roller bay withdraw and park after first approach and grip second lift and carry second lower and release second into its roller bay withdraw and park after second release pallet ready
- Expected result/completion: The timed reference lowers each container into its receiver roller bay before incrementing placed_count and releasing the pallet. The imported robot joints drive the tool, and the gripped load follows it. The timed reference does not establish physical gripping, manufacturer joint limits or safety.
- Documented action controls: `Start pallet unload` (`start`: `unload-cycle`); `Stop robot cell` (`stop`: ``); `Reset pallet cell` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop robot cell` (`stop`: ``); `Reset pallet cell` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-17-pallet-robot.md

### lab-2-18-pallet-pickup — Lab 2.18 - Shipping Pallet Accumulation

- Operator steps: automatic travel pickup position
- Expected result/completion: Auto stops at the pickup sensor; manual jog moves only one bounded increment.
- Documented action controls: `Toggle auto mode` (`togglePoint`: `auto_mode`); `Start automatic travel` (`start`: `automatic`); `Jog pallet one increment` (`start`: `manual`); `Stop conveyor` (`stop`: ``); `Reset pallet` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop conveyor` (`stop`: ``); `Reset pallet` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-18-pallet-pickup.md

### lab-2-19-service-door — Lab 2.19 - Service Door Shutter

- Operator steps: opening open limit
- Expected result/completion: Each direction stops at its limit; open and close outputs are never on together.
- Documented action controls: `Open service door` (`start`: `open`); `Close service door` (`start`: `close`); `Immediate stop` (`stop`: ``); `Reset door` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Immediate stop` (`stop`: ``); `Reset door` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-19-service-door.md

### lab-2-20-bottle-shuttle — Lab 2.20 - Bottle Shuttle Conveyor

- Operator steps: travel right right detected travel left left detected
- Expected result/completion: Right sensor reverses travel; left sensor stops the completed round trip.
- Documented action controls: `Start bottle shuttle` (`start`: `round-trip`); `Stop shuttle` (`stop`: ``); `Reset bottle` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Start bottle shuttle` (`start`: `round-trip`); `Stop shuttle` (`stop`: ``); `Reset bottle` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-20-bottle-shuttle.md

### lab-2-21-tote-finishing — Lab 2.21 - Chemical Tote Finishing Line

- Operator steps: index to fill fill index to cap cap index to label label index to inspect inspect discharge ready
- Expected result/completion: Stations execute in order and every actuator is off after a passing tote discharges.
- Documented action controls: `Start tote finishing` (`start`: `finish-cycle`); `Stop line safely` (`stop`: ``); `Reset tote line` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop line safely` (`stop`: ``); `Reset tote line` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-21-tote-finishing.md

### lab-2-22-dual-spindle — Lab 2.22 - Dual-Spindle Plate Cell

- Operator steps: parallel drilling spindle dwell retracting both home transfer plate complete
- Expected result/completion: Transfer begins only after both spindles are home; all motion commands finish off.
- Documented action controls: `Start dual-spindle cycle` (`start`: `dual-cycle`); `Stop both spindles` (`stop`: ``); `Reset plate cell` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop both spindles` (`stop`: ``); `Reset plate cell` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-22-dual-spindle.md

### lab-2-23-parcel-sorter — Lab 2.23 - Parcel Size Sorter

- Operator steps: load large onto primary table route large large complete load medium onto primary table index medium transfer medium to secondary table route medium medium complete return primary table for small parcel load small onto primary table index small route small batch complete
- Expected result/completion: One parcel reaches each lane and the sorted count ends at three. The cartons pause at the table center before indexing. Scripted queue holds and size feedback do not prove physical sensing, friction or throughput.
- Documented action controls: `Sort three-parcel batch` (`start`: `sort-batch`); `Stop sorter` (`stop`: ``); `Reset parcels` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop sorter` (`stop`: ``); `Reset parcels` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-23-parcel-sorter.md

### lab-2-24-robot-cnc — Lab 2.24 - Robot CNC Tending Cell

- Operator steps: infeed blank pickup machining machine complete unload outfeed ready
- Expected result/completion: Preview action opens access and retracts the tool before the gripped stock crosses the opening. Released stock remains on the vise support; the robot parks and access closes before the machining timer. The same stock is regripped and delivered to outfeed. Reset is required after completion or interruption. Timed reference states do not prove sensed completion, clamp feedback or PLC interlock operation.
- Documented action controls: `Start robot/CNC cycle` (`start`: `cnc-cycle`); `Stop cell safely` (`stop`: ``); `Reset CNC cell` (`reset`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop cell safely` (`stop`: ``); `Reset CNC cell` (`reset`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-24-robot-cnc.md

### lab-2-25-inspection-toggle — Lab 2.25 - Inspection Light Toggle

- Operator steps: Observe the initial input state. Apply toggle button pulse. Verify the PLC produces only the required inspection_light_on.
- Expected result/completion: Odd pulses turn the light on; even pulses return it off.
- Documented action controls: `Pulse toggle button` (`pulse`: `toggle_button_pressed`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-2-25-inspection-toggle.md

### lab-3-01-guarded-pallet-transfer — Lab 3.1 - Guarded Pallet Transfer

- Operator steps: PLC logic evaluates the three manual permissive inputs and owns transfer_permissive and transfer_run. The prescribed carton crosses the entry and exit optical routes, reporting separate PC occupancy feedback. At the supported exit, transfer_complete becomes true; the explicit review PLC removes transfer_run.
- Expected result/completion: The carton remains supported and visible throughout travel and at completion. Only all three manual permissives allow the review PLC transfer; completed travel remains stopped until Reset.
- Documented action controls: `Toggle guard closed` (`toggle`: `guard_closed`); `Toggle entry clear` (`toggle`: `entry_clear`); `Toggle exit clear` (`toggle`: `exit_clear`)
- Documented fault/Stop/Reset/permissive actions: `Toggle guard closed` (`toggle`: `guard_closed`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-3-01-guarded-pallet-transfer.md

### lab-3-02-robot-cell-safe-restart — Lab 3.2 - Robot Cell Safe Restart

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: A restart request cannot enable motion until the gate is closed and the robot is ready.
- Documented action controls: `Toggle gate closed` (`toggle`: `gate_closed`); `Press reset (momentary)` (`pulse`: `reset_complete`); `Toggle robot ready` (`toggle`: `robot_ready`); `Press Start motion (momentary)` (`pulse`: `motion_request`)
- Documented fault/Stop/Reset/permissive actions: `Press reset (momentary)` (`pulse`: `reset_complete`); `Toggle robot ready` (`toggle`: `robot_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-3-02-robot-cell-safe-restart.md

### lab-4-01-press-count-lamp — Lab 4.1 - Press-Count Lamp

- Operator steps: Run the authored starter program. Press PULSE three times, allowing the input to release between presses. Verify threshold_lamp stays off for the first two pulses and turns on after the third.
- Expected result/completion: A Ladder CTU counts pulse_received rising edges and turns the lamp on at its preset.
- Documented action controls: `Pulse count button` (`pulse`: `pulse_received`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: No new learner logic required: authored demo available; verify selected/loaded program on screen.
- Source: rungproof-next/docs/help/scenes/lab-4-01-press-count-lamp.md

### lab-4-02-counter-reset-lamp — Lab 4.2 - Counter Reset Lamp

- Operator steps: Author or explicitly load a compatible counter/reset ladder project and Run. Press COUNT three times, waiting for the input to return false between presses. Press RESET to clear the counter and lamp; the next COUNT starts a new batch.
- Expected result/completion: Three separate count pulses light the reference lamp; reset clears count and lamp, and a subsequent new count starts at one.
- Documented action controls: `Count pulse` (`pulse`: `pulse_received`); `Reset counter` (`pulse`: `reset_pressed`)
- Documented fault/Stop/Reset/permissive actions: `Reset counter` (`pulse`: `reset_pressed`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-02-counter-reset-lamp.md

### lab-4-03-repeat-cycle-counter — Lab 4.3 - Repeat-Cycle Counter

- Operator steps: Enable and press START once. Watch actual head feed/dwell/return and machine_home, machine_busy and cycle_done feedback. The explicit reference publishes COUNT 1, 2, 3 and then batch complete; application Reset is required for a new batch.
- Expected result/completion: One accepted START with enable gives three actual feed/dwell/return cycles, COUNT=3 and batch completion; a held request or command must not add an extra cycle.
- Documented action controls: `Start batch` (`pulse`: `cycle_request`); `Toggle machine enable` (`toggle`: `machine_enabled`)
- Documented fault/Stop/Reset/permissive actions: `Toggle machine enable` (`toggle`: `machine_enabled`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-03-repeat-cycle-counter.md

### lab-4-04-sequence-light-tower — Lab 4.4 - Sequence Light Tower

- Operator steps: Accept a fresh Start only while inactive and reset the step counter. Advance one state on each fresh Step while active; decode only one of four color outputs. After the fourth Step, turn all tower colors off and indicate completion separately. Stop removes commands; require a fresh Start rather than resuming the previous state.
- Expected result/completion: Red -> amber -> green -> blue -> off; four separate Step presses, exactly one color while active, no idle advance.
- Documented action controls: `Press sequence Start (momentary)` (`pulse`: `sequence_start`); `Press sequence Step (momentary)` (`pulse`: `sequence_step_due`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-04-sequence-light-tower.md

### lab-4-05-dual-input-count-window — Lab 4.5 - Dual-Input Count Window

- Operator steps: Author or explicitly open a compatible ladder project, then Run. Press A twice and B three times, waiting for the momentary input to return false between presses. Verify both PLC count displays and the green indication; extra presses beyond the reference limits remove the indication. Press RESET to clear both PLC counters and the indication. Stop clears both PLC count readouts to zero while retaining counter memory; Run republishes the retained counts. Application Reset clears counter memory and stays stopped.
- Expected result/completion: The original offline reference enables window_ready only at A=2..3 and B=3..4, inclusive; extra presses beyond the limits remove it. These values are chosen training assumptions, not source-specified presets. Press order is unrestricted; there is no elapsed-time window.
- Documented action controls: `Press A` (`pulse`: `channel_a_pulse`); `Press B` (`pulse`: `channel_b_pulse`); `Reset both counters` (`pulse`: `reset_pressed`)
- Documented fault/Stop/Reset/permissive actions: `Reset both counters` (`pulse`: `reset_pressed`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-05-dual-input-count-window.md

### lab-4-06-multi-press-confirmation — Lab 4.6 - Multi-Press Confirmation

- Operator steps: Author or explicitly open a compatible ladder project, then Run. Press A twice and B three times, waiting for the momentary input to return false between presses. Verify both PLC count displays and the green indication; extra presses beyond the reference limits remove the indication. Press RESET to clear both PLC counters and the indication. Stop clears both PLC count readouts to zero while retaining counter memory; Run republishes the retained counts. Application Reset clears counter memory and stays stopped.
- Expected result/completion: The original offline reference enables confirmation_valid only at exactly A=2 and B=3; extra presses beyond the limits remove it. These values are chosen training assumptions, not source-specified presets. Press order is unrestricted; there is no elapsed-time window.
- Documented action controls: `Press A` (`pulse`: `button_a_pressed`); `Press B` (`pulse`: `button_b_pressed`); `Reset both counters` (`pulse`: `reset_pressed`)
- Documented fault/Stop/Reset/permissive actions: `Reset both counters` (`pulse`: `reset_pressed`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-06-multi-press-confirmation.md

### lab-4-07-parking-garage-entry — Lab 4.7 - Parking Garage Entry

- Operator steps: Press ENTER to introduce one vehicle; watch approach, full barrier opening, crossing and parking. Repeat for the second bay; the full garage rejects a third entry. Press EXIT to reverse one parked vehicle through the raised barrier. REMOVE clears only the fully departed vehicle.
- Expected result/completion: No unsupported route, disappearing threshold vehicle or substitute motor-starter/wall models. PLC readout follows actual accepted arrival/departure edges; parked and fully departed vehicles remain visible. The capacity, route and timing are original training assumptions, not a manufacturer installation.
- Documented action controls: `Toggle driver enable` (`toggle`: `machine_enabled`); `Toggle downstream path clear` (`toggle`: `exit_clear`); `Enter vehicle (free bay)` (`parkingEnter`: ``); `Exit one parked vehicle` (`parkingExit`: ``); `Clear departed vehicle` (`parkingClearDeparted`: ``)
- Documented fault/Stop/Reset/permissive actions: `Toggle driver enable` (`toggle`: `machine_enabled`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-07-parking-garage-entry.md

### lab-4-08-package-grouping — Lab 4.8 - Package Grouping Station

- Operator steps: Wait for the loaded carton to reach its accumulation position before loading another. Three actual incoming beam edges give PLC count three; actual staged-group feedback permits stop retraction. Watch all three cartons move onto the connected powered receiver and remain visible.
- Expected result/completion: The readout follows PLC CTU count from actual beam feedback, not a PC precomputed ready toggle. Cartons remain supported on aligned infeed/receiver rollers throughout the transfer. Prescribed accumulation positions exclude slip, contact forces and collision dynamics; there is no hardware safety or commissioning claim.
- Documented action controls: `Toggle conveyor enable` (`toggle`: `machine_enabled`); `Toggle downstream clear` (`toggle`: `release_clear`); `Load one carton (idle feed)` (`groupingLoad`: ``)
- Documented fault/Stop/Reset/permissive actions: `Toggle conveyor enable` (`toggle`: `machine_enabled`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-08-package-grouping.md

### lab-4-09-chain-drive-lift — Lab 4.9 - Chain-Drive Lift

- Operator steps: Use a fresh Start to load at HOME, raise a fully carried carton, discharge at UPPER, then lower the empty carriage. Feedback is PC-owned and position-derived; PLC logic owns chain_run, lift_enable, lift_lower and cycle status. Stop holds every load/carriage/belt pose. Run alone must not resume; fresh Start may resume the held cycle.
- Expected result/completion: Start commands horizontal loading at HOME until carton_on_lift. lift_enable raises the carton and carriage to lift_upper; horizontal drives stop during vertical travel. chain_run discharges across the upper bridge to the receiver stop; lift_lower returns the empty carriage. cycle_complete is a PLC result after carton_at_receiver and lift_home; invalid transfer commands latch PC transfer_fault without rewriting PLC commands.
- Documented action controls: `Start carton cycle (momentary)` (`pulse`: `lift_start`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-09-chain-drive-lift.md

### lab-4-10-cookie-packaging — Lab 4.10 - Cookie Packaging Cell

- Operator steps: Feed the next tray into the stationary sealing position. Stop feed, lower the ring jaws, hold, and raise them. Advance the sealed tray and index the next. After six seals, advance 150 mm and retain all packages on the belt.
- Expected result/completion: COOKIES increments on actual leading-edge photoeye crossings; SEALED increments only after a complete head cycle. There is no automatic replenishment, deletion, recycling, film mechanics or heat-process simulation.
- Documented action controls: `Start / resume cookie batch` (`pulse`: `batch_start`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-10-cookie-packaging.md

### lab-4-11-barrel-fill-station — Lab 4.11 - Barrel Fill Station

- Operator steps: Index the supported empty barrel at X=0. Transfer liquid at prescribed 20 L/s until 150 L is measured. Close fill, discharge while authorized and retain the barrel at X=3.
- Expected result/completion: Litres are conserved; source plus barrel remains 200 L. No hydraulic head, valve transit, fluid slosh, slip or replenishment is modeled.
- Documented action controls: `Start / resume barrel batch` (`pulse`: `batch_start`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-11-barrel-fill-station.md

### lab-4-12-cable-cut-length — Lab 4.12 - Cable Cut-Length Cell

- Operator steps: Press Run, then Start. Encoder/reel rotations follow actual feed length at prescribed constant radii. At 3 m, feed stops; knife travels down then returns home. A 60 mm separation reveals the cut.
- Expected result/completion: LENGTH m finishes at 3.00. stock_remaining_m finishes at 7.0. The piece stays supported on the receiver. No automatic recycling occurs. Prescribed no-slip feed and constant-radius reel/rollers; no tension, layering, cutting force, elasticity or collision solver.
- Documented action controls: `Start / resume cable cut` (`pulse`: `batch_start`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-4-12-cable-cut-length.md

### lab-5-01-delayed-lamp — Lab 5.1 - Delayed Lamp

- Operator steps: Select Demo 2, Run, then turn REQUEST ON. The authored PLC TON keeps the lamp off for 100 accepted 20 ms scans (2 s), then on while REQUEST remains ON. Turning OFF clears the timer and lamp on the next accepted scan; a new ON needs the full delay.
- Expected result/completion: Two-second TON in existing Demo 2; OFF resets it, Stop resets it, Run with retained ON starts the full delay again.
- Documented action controls: `Turn request OFF / ON` (`toggle`: `timer_request`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: No new learner logic required: authored demo available; verify selected/loaded program on screen.
- Source: rungproof-next/docs/help/scenes/lab-5-01-delayed-lamp.md

### lab-5-02-timed-lamp-off — Lab 5.2 - Timed Lamp-Off

- Operator steps: Default exercise editor is empty. Build a PLC pulse-timer program or explicitly open .tools/plant-review-timed-lamp-off.rpproj.json after --audit-timer-lessons. Run and press START. The original offline reference uses a three-second TP, a declared training choice absent from the source lesson. START pulses during an active interval are ignored; after expiry, a fresh START triggers a new interval. Clicks before the same accepted scan coalesce.
- Expected result/completion: Explicit original reference TP: lamp immediately on after accepted START, off at three-second accumulated time, extra active presses ignored, fresh press after expiry restarts; Stop cancels.
- Documented action controls: `Press START (one accepted scan)` (`pulse`: `start_pulse`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-02-timed-lamp-off.md

### lab-5-03-rotary-flasher — Lab 5.3 - Rotary Flasher

- Operator steps: The default exercise editor is empty. Build PLC flasher logic, or explicitly open .tools/plant-review-rotary-flasher.rpproj.json after --audit-rotary-flasher. Run and turn MODE to FLASH. The original offline reference starts with 0.5 s OFF, then alternates 0.5 s ON / OFF on accepted 20 ms scans. Turn MODE OFF to clear the lamp and phase/timers on the next accepted scan. A new FLASH starts a full OFF half-period.
- Expected result/completion: Explicit original reference: 25 scans OFF, 25 ON, repeating only in FLASH. OFF cancels next scan; Stop clears lamp/timers and freezes scan; Reset clears phase.
- Documented action controls: `Turn mode OFF / FLASH` (`toggle`: `flash_mode_selected`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-03-rotary-flasher.md

### lab-5-04-alternating-lamps — Lab 5.4 - Alternating Lamps

- Operator steps: Default exercise editor is empty. Build PLC timer logic or explicitly open .tools/plant-review-alternating-lamps.rpproj.json after --audit-flash-pair. Run and turn ENABLE to RUN. PLC starts with A amber and alternates A/B every 0.5 s of accepted 20 ms scans. Exactly one lamp is commanded on after each enabled scan. OFF clears lamps, phase and timers on the next scan; RUN starts with A again.
- Expected result/completion: Explicit original reference: A at first enabled scan through scan 24, B at scan 25, A at 50, B at 75. Enabled accepted scans command exactly one lamp; disabled scans command neither.
- Documented action controls: `Turn ENABLE OFF / RUN` (`toggle`: `alternate_enable`)
- Documented fault/Stop/Reset/permissive actions: `Turn ENABLE OFF / RUN` (`toggle`: `alternate_enable`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-04-alternating-lamps.md

### lab-5-05-variable-flash-rate — Lab 5.5 - Variable Flash Rate

- Operator steps: Default exercise editor is empty. Build PLC timer logic or explicitly open .tools/plant-review-variable-flash-rate.rpproj.json after --audit-flash-pair. Run with ENABLE RUN and exactly one of FAST/SLOW selected. The original offline reference uses FAST 0.2 s and SLOW 0.5 s half-periods, starting with the lamp off. Both selected or neither selected clears the lamp, phase and timers on the next accepted scan; correcting the selection starts a full OFF half-period. A direct valid rate change between accepted scans retains the current phase and starts a full half-period at the new rate. ENABLE OFF clears phase/timers.
- Expected result/completion: Explicit original reference: FAST turns on at scan 10, off at 20; SLOW on at 25, off at 50. Both/neither/disabled are dark; correcting the request starts fresh OFF timing.
- Documented action controls: `Turn ENABLE OFF / RUN` (`toggle`: `flash_enable`); `Turn FAST OFF / FAST` (`toggle`: `fast_rate_selected`); `Turn SLOW OFF / SLOW` (`toggle`: `slow_rate_selected`)
- Documented fault/Stop/Reset/permissive actions: `Turn ENABLE OFF / RUN` (`toggle`: `flash_enable`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-05-variable-flash-rate.md

### lab-5-06-running-light-tower — Lab 5.6 - Running-Light Tower

- Operator steps: Default exercise editor is empty. Build PLC sequence logic or explicitly open .tools/plant-review-running-light-tower.rpproj.json after --audit-running-tower. Run, then turn ENABLE to RUN. The first enabled accepted scan selects red; each fresh STEP edge advances amber, green, red. Idle scans never advance. OFF clears lamps and phase on the next accepted scan; re-enable starts red. A simultaneous first-enable STEP is discarded.
- Expected result/completion: Exactly one PLC color while enabled: red -> amber -> green -> red on separate Step edges. Disabled, held and unsampled repeated clicks cannot accumulate deferred advances.
- Documented action controls: `Turn ENABLE OFF / RUN` (`toggle`: `tower_enable`); `Press STEP (momentary)` (`pulse`: `step_pulse`)
- Documented fault/Stop/Reset/permissive actions: `Turn ENABLE OFF / RUN` (`toggle`: `tower_enable`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-06-running-light-tower.md

### lab-5-07-pedestrian-crossing — Lab 5.7 - Pedestrian Crossing

- Operator steps: Open .tools/plant-review-pedestrian-crossing.rpproj.json generated by --audit-pedestrian-crossing. Run; red/hand startup clearance lasts 3 s. Set PATH CLEAR to release vehicle green. Press CROSS: amber, all-red, white WALK, then orange hand/red clearance. PATH BLOCK prevents vehicle release.
- Expected result/completion: Two opposing vehicle heads and two far-side pedestrian heads have independent PLC channels. ROAD and sidewalks physically support the controls; no people or vehicle motion is simulated. This simplified sequence omits the public-road flashing-hand change interval and is not a road controller or timing design.
- Documented action controls: `Press CROSS (momentary)` (`pulse`: `crossing_request`); `Turn PATH BLOCK / CLEAR (manual raw feedback)` (`toggle`: `clear_to_finish`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-07-pedestrian-crossing.md

### lab-5-08-drawbridge-control — Lab 5.8 - Drawbridge Control

- Operator steps: Run the six-rung reference. CLOSE at HOME opens the barriers, then releases green. Select OPEN, acknowledge STOPPED YES; barriers close in 2 s, bridge raises in 4 s to 70 degrees. Select CLOSE: bridge lowers in 4 s to HOME, barriers open in 2 s, then green releases.
- Expected result/completion: Bridge deck and limit cam share their actual hinge angle; HOME and RAISED lamps follow limits, not elapsed timers. The manual traffic-stopped input is an operator simulation acknowledgement; no vehicles, boats or traffic occupancy detector are simulated. Original geometry/travel times do not establish load capacity, actuator sizing, road compliance or real bridge safety.
- Documented action controls: `Turn BRIDGE CLOSE / OPEN (maintained)` (`toggle`: `bridge_request`); `Set manual STOPPED feedback NO / YES` (`toggle`: `traffic_stopped`)
- Documented fault/Stop/Reset/permissive actions: `Set manual STOPPED feedback NO / YES` (`toggle`: `traffic_stopped`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-08-drawbridge-control.md

### lab-5-09-bag-indexing-conveyor — Lab 5.9 - Bag Indexing Conveyor

- Operator steps: Run enables accepted 20 ms scans. START moves the bag forward at the original illustrative 0.5 m/s. EXIT stops the forward command. A START without PAUSE CLEAR has no effect. Select CLEAR and press START again to return; ENTRY stops the return.
- Expected result/completion: A rounded filled sack replaces the carton and copied pusher. Both sensor pairs cross the belt at 1.1 m and derive feedback from BAG_BODY triangles. Prescribed no-slip motion and rendered contact only; no flexible-body physics, actual drive, safety or PLC transport claim.
- Documented action controls: `Press START / return restart` (`pulse`: `start_request`); `Set manual PAUSE BLOCKED / CLEAR` (`toggle`: `pause_clear`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-09-bag-indexing-conveyor.md

### lab-5-10-coating-line — Lab 5.10 - Coating Line

- Operator steps: Fresh START indexes to actual STATION. Reference holds index and runs vent/spray for 2 s. Spray stops; ventilation continues for a 1 s purge, then index discharges to actual EXIT. One supported workpiece remains visible; Reset reloads home.
- Expected result/completion: PART stays on the belt, spray is visible only over the actual stationed body with both readiness inputs and vent command. Fan/damper are symbolic actuator projections; no measured airflow or coating quality is claimed.
- Documented action controls: `START cycle` (`pulse`: `start_request`); `Manual spray ready` (`toggle`: `spray_ready`); `Manual ventilation ready` (`toggle`: `ventilation_ready`)
- Documented fault/Stop/Reset/permissive actions: `Manual spray ready` (`toggle`: `spray_ready`); `Manual ventilation ready` (`toggle`: `ventilation_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-5-10-coating-line.md

### lab-6-07-luggage-weight-sort — Lab 6.7 - Luggage Weight Sort

- Operator steps: Index to bag_at_scale, stop travel and command weigh_cycle with scale_ready. Classify weight_kg once while weight_valid; increment only the selected category counter. End weigh_cycle, select the route, discharge to the appropriate exit and stop.
- Expected result/completion: One valid bag yields one category and increments only that class counter when your loaded PLC program implements this logic. Live weight is the latched fixture mass only while the base is wholly on the weighing deck, weighing is commanded and no travel is commanded. A completed bag remains visibly on its receiving surface. No contact, calibrated weight or rated mechanism is simulated.
- Documented action controls: `Load next bag after exit` (`luggageLoad`: ``); `Toggle scale ready` (`toggle`: `scale_ready`); `Next fixture mass: 12 / 22 / 32 kg` (`cycle`: `fixture_mass_kg`)
- Documented fault/Stop/Reset/permissive actions: `Toggle scale ready` (`toggle`: `scale_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-6-07-luggage-weight-sort.md

### lab-6-08-hand-dryer — Lab 6.8 - Timed Hand-Dryer

- Operator steps: Insert hands; a user-authored PLC program starts its timer and outputs. The PLC calculates remaining seconds; completion removes outputs. Withdraw and reinsert to rearm.
- Expected result/completion: The mounted fan and heater project eligible commands. The readout displays PLC-owned remaining_seconds; the scene does not manufacture a timer result.
- Documented action controls: `Insert / withdraw hands` (`toggle`: `hands_inserted`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-6-08-hand-dryer.md

### lab-9-01-sum-function — Lab 9.1 - Sum Function Block

- Operator steps: Open programs/examples/09-sum-function-reference.rpproj.json in Project > Open, verify and load offline, then Run. Set A=2 and B=5, then enable both valid inputs and CALCULATE. Expect SUM 7.
- Expected result/completion: A=2 and B=5 produce SUM 7 only with all three permissives; sum_result_valid clears when any permissive is lost. An empty exercise has no calculation controller until a program is loaded.
- Documented action controls: `Toggle operand a valid` (`toggle`: `operand_a_valid`); `Toggle operand b valid` (`toggle`: `operand_b_valid`); `Toggle calculate request` (`toggle`: `calculate_request`); `Cycle operand_a (0, 1, 2, 5, 10)` (`cycle`: `operand_a`); `Cycle operand_b (0, 1, 2, 5, 10)` (`cycle`: `operand_b`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-9-01-sum-function.md

### lab-9-02-product-function — Lab 9.2 - Product Function Block

- Operator steps: Open programs/examples/09-product-function-reference.rpproj.json in Project > Open, verify and load offline, then Run. Set A=2 and B=5, then enable both valid inputs and CALCULATE. Expect PRODUCT 10.
- Expected result/completion: A=2 and B=5 produce PRODUCT 10 only with all three permissives; product_result_valid clears when any permissive is lost. An empty exercise has no calculation controller until a program is loaded.
- Documented action controls: `Toggle factor a valid` (`toggle`: `factor_a_valid`); `Toggle factor b valid` (`toggle`: `factor_b_valid`); `Toggle calculate request` (`toggle`: `calculate_request`); `Cycle factor_a (0, 1, 2, 5, 10)` (`cycle`: `factor_a`); `Cycle factor_b (0, 1, 2, 5, 10)` (`cycle`: `factor_b`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-9-02-product-function.md

### lab-9-03-sum-and-counter-function — Lab 9.3 - Sum and Counter Function

- Operator steps: Choose A and B with the A NEXT and B NEXT readouts or sidebar actions. Enable INPUTS VALID and CALCULATE; toggle manual CALL COMPLETE off/on for each new completion. With the reference, SUM shows the qualified arithmetic result and COUNT advances once per valid completion edge.
- Expected result/completion: A NEXT and B NEXT immediately show their PC input values. SUM and COUNT stay zero with the empty exercise program. With the opt-in reference: A=2, B=5 and one valid completion yield SUM=7, COUNT=1; event_counted is a one-scan pulse, result_valid follows the three Boolean inputs.
- Documented action controls: `Toggle inputs valid` (`toggle`: `inputs_valid`); `Toggle calculate request` (`toggle`: `calculate_request`); `Toggle call complete` (`toggle`: `call_complete`); `Cycle operand_a (0, 1, 2, 5, 10)` (`cycle`: `operand_a`); `Cycle operand_b (0, 1, 2, 5, 10)` (`cycle`: `operand_b`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-9-03-sum-and-counter-function.md

### lab-9-04-function-selector — Lab 9.4 - Function Selector

- Operator steps: Open programs/examples/09-function-selector-reference.rpproj.json in Project > Open, verify and load offline, then Run. Set A=2 and B=5, enable OPERANDS, FUNC VALID and CALCULATE. Choice 1 gives RESULT 7; choice 2 gives RESULT 10.
- Expected result/completion: Only the selected supported path executes: 1 SUM produces 7 and 2 PRODUCT produces 10 for A=2, B=5. Unsupported choices never assert selected_result_valid. An empty exercise has no calculation controller until a program is loaded.
- Documented action controls: `Toggle operand set valid` (`toggle`: `operand_set_valid`); `Toggle function select valid` (`toggle`: `function_select_valid`); `Toggle calculate request` (`toggle`: `calculate_request`); `Next operand_a` (`cycle`: `operand_a`); `Next operand_b` (`cycle`: `operand_b`); `Next function (1 SUM, 2 PRODUCT)` (`cycle`: `function_choice`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-9-04-function-selector.md

### lab-9-10-box-volume — Lab 9.10 - Box Volume Calculation

- Operator steps: Open programs/examples/09-box-volume-reference.rpproj.json with Project > Open, verify and load offline, then Run. Set length=850, width=720, height=720 mm and enable all three validity flags. VOLUME mm3 displays 440640000 and its lamp turns green.
- Expected result/completion: All dimensions must be positive and at most 1000 mm. Qualified volume is length*width*height in mm3; 1000*1000*1000=1000000000 fits DINT. The carton remains supported on the bench; its three heads are static mounting illustrations, not simulated measuring sensors. An empty exercise does not calculate until a program is loaded.
- Documented action controls: `Toggle length valid` (`toggle`: `length_valid`); `Toggle width valid` (`toggle`: `width_valid`); `Toggle height valid` (`toggle`: `height_valid`); `Next length (mm)` (`cycle`: `length_mm`); `Next width (mm)` (`cycle`: `width_mm`); `Next height (mm)` (`cycle`: `height_mm`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-9-10-box-volume.md

### lab-9-11-pallet-counting — Lab 9.11 - Pallet Count Function

- Operator steps: Enable pallet type valid and count request. Toggle pallet detected off and on for each new valid event. The DINT count and 3D display advance once per rising detection edge. The batch-valid lamp turns on at five valid events. Invalid events are not counted; held detection does not recount.
- Expected result/completion: The 3D readout and DINT pallet_count match the accumulated valid detection events. The batch-valid bit requires at least five events and both permissives. Stop clears the numeric output image to zero while retaining internal counter memory. Run republishes the retained count on its next scan. Reset clears inputs, memory and outputs. Pallet type is a manually supplied validity bit; the optical fixture does not classify pallets or generate detection events.
- Documented action controls: `New pallet detection event` (`pulse`: `pallet_detected`); `Toggle pallet type valid` (`toggle`: `pallet_type_valid`); `Toggle count request` (`toggle`: `count_request`)
- Documented fault/Stop/Reset/permissive actions: None listed; common app playback Stop/Reset is distinct from machine controls.
- Learner-authored logic: No new learner logic required: authored demo available; verify selected/loaded program on screen.
- Source: rungproof-next/docs/help/scenes/lab-9-11-pallet-counting.md

### lab-9-12-ev-charging-manager — Lab 9.12 - EV Charging Manager

- Operator steps: Apply the requested input condition. Verify only the documented PLC outputs respond.
- Expected result/completion: Charging is enabled only for an occupied, authorized, ready bay.
- Documented action controls: `Toggle bay occupied` (`toggle`: `bay_occupied`); `Toggle customer authorized` (`toggle`: `customer_authorized`); `Toggle charger ready` (`toggle`: `charger_ready`); `A connector fixture` (`toggle`: `connector_inserted`); `B occupancy fixture` (`toggle`: `bay_b_occupied`); `B authorization fixture` (`toggle`: `bay_b_customer_authorized`); `B ready fixture` (`toggle`: `bay_b_charger_ready`); `B connector fixture` (`toggle`: `bay_b_connector_inserted`)
- Documented fault/Stop/Reset/permissive actions: `Toggle charger ready` (`toggle`: `charger_ready`); `B ready fixture` (`toggle`: `bay_b_charger_ready`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/lab-9-12-ev-charging-manager.md

### scene-1-conveyor-stop — Conveyor Stop

- Operator steps: Use documented actions: `Start / stop conveyor` (`toggle`: `conveyor_running`)
- Expected result/completion: No explicit completion criterion. Purpose: The established beginner scene: the PLC runs one conveyor until the simulated package blocks the photoeye, then the conveyor stops with the package held at the sensor.
- Documented action controls: `Start / stop conveyor` (`toggle`: `conveyor_running`)
- Documented fault/Stop/Reset/permissive actions: `Start / stop conveyor` (`toggle`: `conveyor_running`)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/scene-1-conveyor-stop.md

### scene-2-conveyor-pusher — Conveyor Pusher

- Operator steps: Use documented actions: `Machine Start` (`pulse`: `start_command`); `Machine Stop` (`pulse`: `stop_command`); `Machine Reset` (`pulse`: `reset_command`); `System Enable` (`toggle`: `system_enable`); `Simulated safety trip` (`toggle`: `safety_trip`); `Simulated drive overload` (`toggle`: `drive_overload`)
- Expected result/completion: No explicit completion criterion. Purpose: This offline training cell measures each carton at the pusher station. The authored ladder compares the measured height against a documented **600 mm boundary**: cartons above 600 mm are diverted to the side takeaway conveyor; cartons at or below 600 mm continue on the main line. The repeatable simulated sequence is 420 mm, 820 mm, then 600 mm. The infeed conveyor continues through a full room wall into a connected staging room. A dark, framed tunnel makes that penetration visible in the normal lesson view; cartons begin inside that source room rather than appearing at the exposed end of the conveyor. The ladder program owns machine run state, height classification, divert memory, conveyor and pusher commands, fault/interlock response, and Reset. The plant model only integrates those commands and publishes measurement, limits, and exit feedback.
- Documented action controls: `Machine Start` (`pulse`: `start_command`); `Machine Stop` (`pulse`: `stop_command`); `Machine Reset` (`pulse`: `reset_command`); `System Enable` (`toggle`: `system_enable`); `Simulated safety trip` (`toggle`: `safety_trip`); `Simulated drive overload` (`toggle`: `drive_overload`)
- Documented fault/Stop/Reset/permissive actions: `Machine Stop` (`pulse`: `stop_command`); `Machine Reset` (`pulse`: `reset_command`); `System Enable` (`toggle`: `system_enable`); `Simulated safety trip` (`toggle`: `safety_trip`); `Simulated drive overload` (`toggle`: `drive_overload`)
- Learner-authored logic: No new learner logic required: authored demo available; verify selected/loaded program on screen.
- Source: rungproof-next/docs/help/scenes/scene-2-conveyor-pusher.md

### tank-high-low — Water Tank — High/Low Switches

- Operator steps: Use documented actions: `Run simulated process` (`run`: ``); `Toggle inlet pump (simulated)` (`toggle`: `inlet_pump_run`); `Toggle drain valve (simulated)` (`toggle`: `drain_valve_open`); `Stop simulated process` (`stop`: ``)
- Expected result/completion: No explicit completion criterion. Purpose: A discrete level-control example with separate low and high point level switches. Run the scene, then operate the inlet pump and drain to cross each switch elevation.
- Documented action controls: `Run simulated process` (`run`: ``); `Toggle inlet pump (simulated)` (`toggle`: `inlet_pump_run`); `Toggle drain valve (simulated)` (`toggle`: `drain_valve_open`); `Stop simulated process` (`stop`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop simulated process` (`stop`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/tank-high-low.md

### tank-level — Tank Level / 4–20 mA

- Operator steps: Use documented actions: `Run simulated process` (`run`: ``); `Toggle inlet pump (simulated)` (`toggle`: `inlet_pump_run`); `Toggle drain valve (simulated)` (`toggle`: `drain_valve_open`); `Stop simulated process` (`stop`: ``)
- Expected result/completion: No explicit completion criterion. Purpose: Run the scene, start the inlet pump, and open or close the drain. Fluid level, discrete low/high switches, and a linear 4–20 mA transmitter update together.
- Documented action controls: `Run simulated process` (`run`: ``); `Toggle inlet pump (simulated)` (`toggle`: `inlet_pump_run`); `Toggle drain valve (simulated)` (`toggle`: `drain_valve_open`); `Stop simulated process` (`stop`: ``)
- Documented fault/Stop/Reset/permissive actions: `Stop simulated process` (`stop`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/tank-level.md

### tank-radar — Water Tank — Radar Level

- Operator steps: Use documented actions: `Run simulated process` (`run`: ``); `Toggle automatic cycle request` (`toggle`: `cycle_request`); `Toggle manual drain request` (`toggle`: `manual_drain_request`); `Stop simulated process` (`stop`: ``); `Inspection view: ghost shell and roof` (`toggle`: `tank_inspection_view`)
- Expected result/completion: No explicit completion criterion. Purpose: Symbolic non-contact radar example. The roof-mounted transmitter reports distance from its antenna lens to the simulated surface, level and idealized 4-20 mA feedback. The opaque tank hides the internal beam; echo health is idealized.
- Documented action controls: `Run simulated process` (`run`: ``); `Toggle automatic cycle request` (`toggle`: `cycle_request`); `Toggle manual drain request` (`toggle`: `manual_drain_request`); `Stop simulated process` (`stop`: ``); `Inspection view: ghost shell and roof` (`toggle`: `tank_inspection_view`)
- Documented fault/Stop/Reset/permissive actions: `Stop simulated process` (`stop`: ``)
- Learner-authored logic: Yes: author/load suitable controller networks; fresh exercise supplies SceneIO tags only.
- Source: rungproof-next/docs/help/scenes/tank-radar.md

## Specification questions (not app defects)

- conveyor-cell: no Normal sequence section; purpose/actions do not define a full completion test.
- equipment-gallery: no Normal sequence section; purpose/actions do not define a full completion test.
- lab-4-01-press-count-lamp: Operator actions help table omits machine Reset although demo acceptance requires it; confirm actual visible controls and reconcile help.
- lab-5-01-delayed-lamp: Operator actions help table omits machine Reset although demo acceptance requires it; confirm actual visible controls and reconcile help.
- lab-9-11-pallet-counting: Operator actions help table omits machine Reset although demo acceptance requires it; confirm actual visible controls and reconcile help.
- scene-1-conveyor-stop: no Normal sequence section; purpose/actions do not define a full completion test.
- scene-2-conveyor-pusher: no Normal sequence section; purpose/actions do not define a full completion test.
- tank-high-low: no Normal sequence section; purpose/actions do not define a full completion test.
- tank-high-low: help documents toggling PLC-owned actuator points directly, while fresh exercise routing requires learner ladder. Clarify intended operator path before calling non-response a defect.
- tank-level: no Normal sequence section; purpose/actions do not define a full completion test.
- tank-level: help documents toggling PLC-owned actuator points directly, while fresh exercise routing requires learner ladder. Clarify intended operator path before calling non-response a defect.
- tank-radar: no Normal sequence section; purpose/actions do not define a full completion test.
- DEMO_FUNCTIONAL_ACCEPTANCE.md contains an older instruction stating Demo 3 lacks a safety-trip input, but later current-implementation rows and scene help document safety_trip and its toggle. Use current runtime screen evidence; reconcile this historical/current wording separately.

Coverage: 77/77 help documents found; 7 lack a Normal sequence section. Five authored demo hard-acceptance rows extracted. No scene behavior was executed and no application issue is established by this document.
