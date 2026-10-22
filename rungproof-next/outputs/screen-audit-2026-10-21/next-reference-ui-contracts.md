# Next unchanged-reference UI contracts

Static source/help preparation,2026-10-22. No execution or PASS credit. Select matching scene first, File>Open folder+leaf, then Verify/Load/Run; observe accepted controller and Signals before controls. Preserve staged hashes. Every action below must be located and physically exercised; declarations alone do not prove a clickable face.

## Lab 3.2 - Robot Cell Safe Restart

Project: test-projects/existing-robot-cell-restart-reference.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Toggle gate closed [toggle-gate_closed]: toggle gate_closed; Press reset (momentary) [toggle-reset_complete]: pulse reset_complete; Toggle robot ready [toggle-robot_ready]: toggle robot_ready; Press Start motion (momentary) [start-robot-motion]: pulse motion_request |
| PC Signals (declared initial) | gate_closed:BOOL=False; reset_complete:BOOL=False; robot_ready:BOOL=False; motion_request:BOOL=False |
| PLC Signals (declared initial) | robot_enable:BOOL=False; cell_ready:BOOL=False |
| Normal | Gate closed + ready, fresh RESET then separate START; observe cell_ready and robot_enable. Gate/ready loss must prevent restart; fresh reset/motion qualification belongs to the supplied reference, not a simple AND substitute. |
| Recovery/repeat | Gate/ready loss and restoration; prove no unrequested restart, fresh RESET/START; AppStop/Run and AppReset separately. Simulated visual E-stop reference is not a safety input. |

Sources: scenes/migrated/lab-3-02-robot-cell-safe-restart.scene.json (simulation.points/actions); docs/help/scenes/lab-3-02-robot-cell-safe-restart.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 4.4 - Sequence Light Tower

Project: test-projects/existing-sequence-light-tower-reference.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Press sequence Start (momentary) [toggle-sequence_start]: pulse sequence_start; Press sequence Step (momentary) [toggle-sequence_step_due]: pulse sequence_step_due |
| PC Signals (declared initial) | sequence_start:BOOL=False; sequence_step_due:BOOL=False |
| PLC Signals (declared initial) | tower_active:BOOL=False; sequence_complete:BOOL=False; tower_red:BOOL=False; tower_amber:BOOL=False; tower_green:BOOL=False; tower_blue:BOOL=False |
| Normal | START while inactive, then four separate STEP pulses: red → amber → green → blue → off/completion. STEP while idle must not advance. |
| Recovery/repeat | Stop removes commands; Run alone must not resume previous step, fresh START required. Reset then repeat full four-step sequence. |

Sources: scenes/migrated/lab-4-04-sequence-light-tower.scene.json (simulation.points/actions); docs/help/scenes/lab-4-04-sequence-light-tower.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 2.21 - Chemical Tote Finishing Line

Project: test-projects/existing-tote-fill-cap-review.rpproj.json; test-projects/existing-tote-finishing-process-review.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Start tote finishing [start-line]: start operator.start; Stop line safely [stop-line]: stop ; Reset tote line [reset-line]: reset  |
| PC Signals (declared initial) | inspection_ok:BOOL=False; tote_position:REAL=-5.8; tote_at_fill:BOOL=False; tote_at_cap:BOOL=False; tote_at_label:BOOL=False; tote_at_inspection:BOOL=False; tote_at_exit:BOOL=False; fill_percent:REAL=0; actual_fill_percent_per_second:REAL=0; fill_inhibited:BOOL=False; fill_complete:BOOL=False; cap_extension_percent:REAL=0; cap_applied:BOOL=False; cap_busy:BOOL=False; cap_inhibited:BOOL=False; cap_home:BOOL=True; tote_transfer_inhibited:BOOL=False; label_applied:BOOL=False; label_extension_percent:REAL=0; label_busy:BOOL=False; label_home:BOOL=True; label_inhibited:BOOL=False; inspection_busy:BOOL=False; inspection_done:BOOL=False; inspection_inhibited:BOOL=False; inspection_defect_code:DINT=0; inspection_view_clear:BOOL=False; tote_label_aligned:BOOL=False |
| PLC Signals (declared initial) | conveyor_run:BOOL=False; fill_valve_open:BOOL=False; capper_run:BOOL=False; labeler_run:BOOL=False; inspection_run:BOOL=False |
| Normal | One START: index/fill → cap → label → inspect → discharge/ready. Verify each actuator follows its station and all off after passing tote discharges. |
| Recovery/repeat | Capture mid-process AppStop, held pose/output state, Run and a distinct fresh START. Capture Reset baseline and second cycle; unspecified resume policy is observation rather than failure. |

Sources: scenes/migrated/lab-2-21-tote-finishing.scene.json (simulation.points/actions); docs/help/scenes/lab-2-21-tote-finishing.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 2.22 - Dual-Spindle Plate Cell

Project: test-projects/existing-dual-spindle-round-trip-review.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Start dual-spindle cycle [start-dual-drill]: start operator.start; Stop both spindles [stop-dual-drill]: stop ; Reset plate cell [reset-dual-drill]: reset  |
| PC Signals (declared initial) | drill_a_home:BOOL=True; drill_b_home:BOOL=True; drill_a_at_depth:BOOL=False; drill_b_at_depth:BOOL=False; transfer_home:BOOL=True; transfer_at_end:BOOL=False; transfer_inhibited:BOOL=False |
| PLC Signals (declared initial) | drill_a_run:BOOL=False; drill_b_run:BOOL=False; transfer_extend:BOOL=False; drill_a_feed:BOOL=False; drill_a_retract:BOOL=False; drill_b_feed:BOOL=False; drill_b_retract:BOOL=False; transfer_retract:BOOL=False |
| Normal | One START: parallel drilling → dwell → both retract/home → plate transfer → complete. Transfer only after both home, all motion commands off at completion. |
| Recovery/repeat | Capture drilling/retract/transfer interruption and all outputs; AppStop/Run versus fresh START, Reset and second cycle. Do not assert an undocumented resume policy. |

Sources: scenes/migrated/lab-2-22-dual-spindle.scene.json (simulation.points/actions); docs/help/scenes/lab-2-22-dual-spindle.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 4.9 - Chain-Drive Lift

Project: test-projects/existing-chain-lift-installation-reference.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Start carton cycle (momentary) [start-lift-installation]: pulse lift_start |
| PC Signals (declared initial) | box_present:BOOL=True; lift_home:BOOL=True; destination_clear:BOOL=True; lift_start:BOOL=False; carton_on_lift:BOOL=False; lift_upper:BOOL=False; carton_at_receiver:BOOL=False; receiver_occupied:BOOL=False; receiver_beam_blocked:BOOL=False; transfer_fault:BOOL=False; carton_on_infeed:BOOL=True |
| PLC Signals (declared initial) | chain_run:BOOL=False; lift_enable:BOOL=False; lift_lower:BOOL=False; cycle_active:BOOL=False; cycle_complete:BOOL=False |
| Normal | START: supported horizontal load at HOME → raise fully carried carton → discharge at UPPER to receiver → lower empty carriage HOME. Complete only with receiver carton and home feedback. |
| Recovery/repeat | AppStop holds all load/carriage/belt poses. Run alone must not resume; fresh START may resume held cycle. Reset baseline/repeat. PC transfer_fault requires invalid transfer commands: normal reference may not expose that injection, so leave untested rather than fake output. |

Sources: scenes/migrated/lab-4-09-chain-drive-lift.scene.json (simulation.points/actions); docs/help/scenes/lab-4-09-chain-drive-lift.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 4.10 - Cookie Packaging Cell

Project: test-projects/existing-cookie-packaging-reference.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Start / resume cookie batch [start-cookie-batch]: pulse batch_start |
| PC Signals (declared initial) | product_present:BOOL=False; packaging_ready:BOOL=True; batch_complete:BOOL=False; packaging_busy:BOOL=False; count_beam_blocked:BOOL=False; packaging_fault:BOOL=False; batch_start:BOOL=False; cookie_count:INT=0; wrapped_count:INT=0 |
| PLC Signals (declared initial) | infeed_run:BOOL=False; packaging_enable:BOOL=False; cycle_active:BOOL=False; cycle_complete:BOOL=False |
| Normal | START: feed tray → stop at sealing position → head down/hold/up → advance; six actual completed seals followed by150mm final advance. COOKIES photoeye count differs from SEALED head-cycle count. |
| Recovery/repeat | Capture interruption in feed/head/final advance, commands and retained packages; compare Run alone/fresh START, Reset and new batch. No automatic replenishment/deletion/recycling expected. |

Sources: scenes/migrated/lab-4-10-cookie-packaging.scene.json (simulation.points/actions); docs/help/scenes/lab-4-10-cookie-packaging.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 4.11 - Barrel Fill Station

Project: test-projects/existing-barrel-fill-reference.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Start / resume barrel batch [start-barrel-batch]: pulse batch_start |
| PC Signals (declared initial) | barrel_at_fill:BOOL=False; fill_complete:BOOL=False; downstream_clear:BOOL=True; barrel_parked:BOOL=False; fill_beam_blocked:BOOL=False; exit_beam_blocked:BOOL=False; fill_fault:BOOL=False; batch_start:BOOL=False; barrel_litres:REAL=0; source_litres:REAL=200; flow_lps:REAL=0 |
| PLC Signals (declared initial) | infeed_run:BOOL=False; fill_valve_open:BOOL=False; cycle_active:BOOL=False; cycle_complete:BOOL=False |
| Normal | START: supported empty barrel at X0 → fill20L/s until150L → close fill → discharge to X3. Source+barrel=200L throughout. |
| Recovery/repeat | Interrupt filling/discharge, capture measured liquid conservation and supported pose. Test Run/fresh START and Reset/repeat separately; no calibrated fluid-dynamics claim. |

Sources: scenes/migrated/lab-4-11-barrel-fill-station.scene.json (simulation.points/actions); docs/help/scenes/lab-4-11-barrel-fill-station.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

## Lab 4.12 - Cable Cut-Length Cell

Project: test-projects/existing-cable-cut-reference.rpproj.json.

| Check | Contract / exact Signals |
|---|---|
| Controls | Start / resume cable cut [start-cable-cut]: pulse batch_start |
| PC Signals (declared initial) | cable_present:BOOL=True; length_reached:BOOL=False; cutter_home:BOOL=True; cut_done:BOOL=False; cut_complete:BOOL=False; cut_fault:BOOL=False; measured_length_m:REAL=0; stock_remaining_m:REAL=10; feed_speed_mps:REAL=0; cutter_position:REAL=0; batch_start:BOOL=False |
| PLC Signals (declared initial) | feed_run:BOOL=False; cutter_fire:BOOL=False; cycle_active:BOOL=False; cycle_complete:BOOL=False |
| Normal | Run then START: actual feed to3m → stop feed → knife down/home →60mm separation; final length3.00m, stock7.0m, supported retained piece. |
| Recovery/repeat | Interrupt feed/knife/separation, capture measured length and supported stock. Test Run/fresh START, Reset/repeat separately; no automatic recycling expected. |

Sources: scenes/migrated/lab-4-12-cable-cut-length.scene.json (simulation.points/actions); docs/help/scenes/lab-4-12-cable-cut-length.md; exact staged source/hash in test-projects/existing-reference-manifest.json. Initial values are declarations, not observed runtime readings.

For each case capture PC input changes separately from PLC commands and actual plant feedback. Pending/unreadable channels remain unproven. Missing source policy does not block a documented ordinary path; record narrow edge-policy questions. These are test expectations, not a remediation plan.
