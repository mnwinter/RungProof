# Six non-lab catalog contracts and fixture use

Read-only source preparation; actual UI testing pending. No backend behavior/compiler execution. Default SceneLadderProject.Create builds tags without logic; AuthoredDemoLadderPrograms.TryCreate does not list these six IDs. Do not assume conveyor-cell has an authored starter. Select exact scene before normal File>Open/VerifyLoad/Run. SIM points are internal: never bind them as PLC inputs.

## equipment-gallery

A visual inventory of reusable catalog models created by the runtime scene composer. Level instruments are shown on illustrative display stands.

Actions: toggle-gallery [Start / stop gallery motion] type=toggle, point=gallery_animation, startBinding=, stopBinding=; stop-all [Emergency stop gallery motion] type=stop, point=, startBinding=, stopBinding=

Points: gallery_animation:BOOL/SIM, initial=False; industrial_fan_run:BOOL/SIM, initial=False; emergency_stop:BOOL/SIM, initial=False; demo_tank_level:REAL/SIM, initial=50; demo_level_signal:REAL/SIM, initial=12

No learner fixture required to request declared SIM gallery_animation via toggle-gallery. Exercise physical motion toggle, compare motor/conveyor/fan/machine samples, then toggle off and stop-all; capture Health/Run state as applicable. Internal demo_tank_level/demo_level_signal/industrial_fan_run are observations, not PLC ownership. A blocked no-controller Run is separate from whether gallery toggle works.

Sources: scenes/migrated/equipment-gallery.scene.json; docs/help/scenes/equipment-gallery.md; src/VirtualController/SceneLadderProject.cs:24-40,66-74; src/App/Main.cs:5235-5283.

## conveyor-cell

Three reusable cartons circulate on a motor-driven conveyor. The through-beam photoeye changes state as each carton crosses it.

Actions: conveyor_run [Start / stop inspection conveyor] type=toggle, point=conveyor_run, startBinding=operator.start, stopBinding=operator.stop; conveyor-estop [Emergency stop conveyor] type=emergencyStop, point=estop_ok, startBinding=, stopBinding=; conveyor-estop-reset [Reset emergency stop] type=reset, point=, startBinding=, stopBinding=

Points: conveyor_run:BOOL/PLC, initial=False; estop_ok:BOOL/SIM, initial=True; photoeye_blocked:BOOL/PC, initial=False; conveyor_speed:REAL/SIM, initial=0; parts_completed:DINT/SIM, initial=0

fixture-conveyor-cell-normal.rpproj.json: Stop-priority seal-in output from explicit Input bindings operator.start/operator.stop; no fabricated estop_ok tag (SIM). Run then physical start toggle should pulse start, circulate three cartons with actual photoeye changes; toggle again while running routes stop. Test simulated E-stop, explicit reset and fresh Run/start separately. Chosen seal-in is learner controller, not an app-wide restart policy.

Sources: scenes/migrated/conveyor-cell.scene.json; docs/help/scenes/conveyor-cell.md; src/VirtualController/SceneLadderProject.cs:24-40,66-74; src/App/Main.cs:5235-5283.

## scene-1-conveyor-stop

The established beginner scene: the PLC runs one conveyor until the simulated package blocks the photoeye, then the conveyor stops with the package held at the sensor.

Actions: scene-toggle [Start / stop conveyor] type=toggle, point=conveyor_running, startBinding=operator.start, stopBinding=operator.stop

Points: conveyor_running:BOOL/PLC, initial=False; simulated_photoeye:BOOL/PC, initial=False; object_position:REAL/SIM, initial=0; component_state:STRING/SIM, initial=stopped_loaded; parts_completed:DINT/SIM, initial=0

fixture-scene-1-conveyor-stop-normal.rpproj.json: same explicit start/stop seal-in plus NC simulated_photoeye, so one physical Start moves carton until actual blocked feedback drops conveyor_running. Observe supported carton held at sensor, Stop/Run and Reset/freshrepeat separately. No fake object_position/component_state inputs.

Sources: scenes/migrated/scene-1-conveyor-stop.scene.json; docs/help/scenes/scene-1-conveyor-stop.md; src/VirtualController/SceneLadderProject.cs:24-40,66-74; src/App/Main.cs:5235-5283.

## tank-high-low

A discrete level-control example with separate low and high point level switches. Run the scene, then operate the inlet pump and drain to cross each switch elevation.

Actions: run-simulation [Run simulated process] type=run, point=, startBinding=, stopBinding=; toggle-pump [Toggle inlet pump (simulated)] type=toggle, point=inlet_pump_run, startBinding=, stopBinding=; toggle-drain [Toggle drain valve (simulated)] type=toggle, point=drain_valve_open, startBinding=, stopBinding=; stop-simulation [Stop simulated process] type=stop, point=, startBinding=, stopBinding=

Points: inlet_pump_run:BOOL/PLC, initial=False; drain_valve_open:BOOL/PLC, initial=False; tank_level:REAL/SIM, initial=50; low_level_switch:BOOL/PC, initial=False; high_level_switch:BOOL/PC, initial=False

No fixture supplied for advertised manual pump/drain route: both actions target PLC outputs but declare neither controllerStartBinding nor controllerStopBinding. Main.cs5257-5283 requires a declared command binding for a loaded controller output action. This is a STATIC route concern only, not a confirmed UI defect; physically attempt legitimate route and record exact rejection before classifying. Do not invent bindings or use backend output injection. Once legitimate command route is available, cross actual25/75percent switches and compare fluid/pump/drain.

Sources: scenes/migrated/tank-high-low.scene.json; docs/help/scenes/tank-high-low.md; src/VirtualController/SceneLadderProject.cs:24-40,66-74; src/App/Main.cs:5235-5283.

## tank-level

Run the scene, start the inlet pump, and open or close the drain. Fluid level, discrete low/high switches, and a linear 4–20 mA transmitter update together.

Actions: run-simulation [Run simulated process] type=run, point=, startBinding=, stopBinding=; toggle-pump [Toggle inlet pump (simulated)] type=toggle, point=inlet_pump_run, startBinding=, stopBinding=; toggle-drain [Toggle drain valve (simulated)] type=toggle, point=drain_valve_open, startBinding=, stopBinding=; stop-simulation [Stop simulated process] type=stop, point=, startBinding=, stopBinding=

Points: inlet_pump_run:BOOL/PLC, initial=False; drain_valve_open:BOOL/PLC, initial=False; tank_level:REAL/SIM, initial=42; level_transmitter:REAL/PC, initial=10.72; low_level_switch:BOOL/PC, initial=False; high_level_switch:BOOL/PC, initial=False

Same STATIC manual-output binding concern as tank-high-low; no fixture fabricated. Physically test route before issue claim. Normal plant contract: level42percent initial, low/high20/80percent, transmitter=4+16*levelFraction mA; compare rising/falling switches and analog readings with actual fill/drain. Exact physical signal fidelity unproven until UI.

Sources: scenes/migrated/tank-level.scene.json; docs/help/scenes/tank-level.md; src/VirtualController/SceneLadderProject.cs:24-40,66-74; src/App/Main.cs:5235-5283.

## tank-radar

Symbolic non-contact radar example. The roof-mounted transmitter reports distance from its antenna lens to the simulated surface, level and idealized 4-20 mA feedback. The opaque tank hides the internal beam; echo health is idealized.

Actions: run-simulation [Run simulated process] type=run, point=, startBinding=, stopBinding=; toggle-pump [Toggle automatic cycle request] type=toggle, point=cycle_request, startBinding=, stopBinding=; toggle-drain [Toggle manual drain request] type=toggle, point=manual_drain_request, startBinding=, stopBinding=; stop-simulation [Stop simulated process] type=stop, point=, startBinding=, stopBinding=; toggle-inspection [Inspection view: ghost shell and roof] type=toggle, point=tank_inspection_view, startBinding=, stopBinding=

Points: inlet_pump_run:BOOL/PLC, initial=False; drain_valve_open:BOOL/PLC, initial=False; tank_level:REAL/SIM, initial=35; radar_level:REAL/PC, initial=35; radar_distance:REAL/PC, initial=3.1003565788269; radar_signal:REAL/PC, initial=9.6; radar_echo_ok:BOOL/PC, initial=True; tank_inspection_view:BOOL/SIM, initial=False; cycle_request:BOOL/PC, initial=False; manual_drain_request:BOOL/PC, initial=False

fixture-tank-radar-normal.rpproj.json: purpose-limited learner fill/drain plant controller: inlet=cycle_request AND NOT manual_drain_request; drain=cycle_request AND manual_drain_request. This explicitly chosen two-mode implementation tests physical AUTO/CYCLE and DRAIN REQUEST input route, levels/distance/signal and inspection ghost view. It DOES NOT implement or prove automatic high/low cycling; source does not specify hysteresis thresholds. Expected ideal signal=4+0.16*radar_level(percent), distance decreases with rising liquid, echo initiallyTrue. Observe Stop/Run, requestloss, Reset, repeat separately. No SIM tank_level/inspection flag PLC binding.

Sources: scenes/migrated/tank-radar.scene.json; docs/help/scenes/tank-radar.md; src/VirtualController/SceneLadderProject.cs:24-40,66-74; src/App/Main.cs:5235-5283.

Manifest: test-projects/nonlab-fixture-manifest.json. Fixture parameters:20ms scan, conventional Stop-priority seal-in; no new process timing assumptions. JSON checked only, UI validation/functional outcomes remain pending. No app changes or remediation plan.
