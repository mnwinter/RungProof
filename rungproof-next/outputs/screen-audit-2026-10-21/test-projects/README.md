# Disposable learner projects for screen testing

These thirteen minimal declarative Ladder projects supply learner-authored logic for later testing through visible app controls. They are test inputs, not application changes or supplied reference solutions for other labs. Preparation performs no loading, compilation, execution or behavior testing; later UI runs are recorded separately.

## Loading through the normal UI

1. Preserve any wanted current learner draft using the normal Save workflow before replacing it. These fixtures may be discarded after the audit; unrelated learner work should be retained.
2. **Select the matching scene first** through the Scenario menu or browser. Then choose **File > Open Ladder Agent Project…**. A mismatched `sourceSceneId` is explicitly blocked; loading does not automatically switch scenes. The Lab 2.2 attempt from Powder Batch Mixer showed `PROJECT OPEN BLOCKED · SOURCE SCENE MISMATCH · SELECT 'lab-2-02-dual-confirmation' THEN OPEN AGAIN`.
3. In the normal file dialog set the header **Path** to this directory, then set the bottom **File** field to the leaf name such as `lab-2-02-dual-confirmation.rpproj.json`, and choose Open. The tested dialog's Open action remained disabled when the full absolute filename was put into File; folder Path plus file leaf worked. Verify the active scene identity and loaded network(s). If any replacement prompt appears, handle the current draft deliberately.
4. Use the visible **Online > Verify + load offline** route, then app **Run**. Confirm local runtime RUNNING, correct scene, and no validation errors before testing the physical scene input controls. A project open is not proof that it has been loaded into the controller.
5. Exercise the truth table below by clicking the scene controls, observing their input state in normal UI feedback/watch and the physical lamp. Repeat the table after app Stop/Reset/Run; observe actual input reset policy instead of assuming it.

## Lab 2.2: Dual Confirmation

File: `lab-2-02-dual-confirmation.rpproj.json`  
Scene: `lab-2-02-dual-confirmation`  
Logic: two normally open contacts in series, ordinary assignment coil `handoff_ready`.

| operator_confirmed | quality_confirmed | handoff_ready |
|---|---|---|
| false | false | false |
| true | false | false |
| false | true | false |
| true | true | true |

Both controls are documented toggles despite their press/release names. Suggested sequence: neither -> operator only -> both -> quality only -> neither -> both again. The lamp must turn off when either required confirmation is removed.

## Lab 2.3: Service Marker Inhibit

File: `lab-2-03-service-marker-inhibit.rpproj.json`  
Scene: `lab-2-03-service-marker-inhibit`  
Logic: one normally closed contact `marker_inhibit_pressed`, ordinary assignment coil `service_marker_on`.

| marker_inhibit_pressed | service_marker_on |
|---|---|
| false | true |
| true | false |

The scene declares the output initially true. Judge scanned behavior only after Verify + Load and Run: released inhibit should illuminate the white marker, active inhibit should extinguish it, and release should illuminate it again. This software NC contact means Boolean inversion; it is not a claim about physical NC wiring or a safety function.

## Lab 2.4: Two-Station Call Beacon

File: `lab-2-04-two-station-call.rpproj.json`  
Scene: `lab-2-04-two-station-call`  
Logic: parallel one-contact series legs for `north_call_pressed` and `south_call_pressed`, joined to one ordinary assignment coil `assistance_call_on`.

| north_call_pressed | south_call_pressed | assistance_call_on |
|---|---|---|
| false | false | false |
| true | false | true |
| false | true | true |
| true | true | true |

Suggested sequence: neither -> north only -> both -> south only -> neither -> south only -> neither. This checks that releasing one call does not extinguish the beacon while the other remains active.

## Format evidence and assumptions

- Base serialization comes from the checked-in `programs/demos/01-press-count-starter.rpproj.json`, schema 9. The current reader declares schema 10 but explicitly accepts schemas 1 through 10 (`LadderEditorProjectJson.cs:26,129–140`). Schema 9 is retained to match the actual saved example; it does not require schema-10 systemConstants.
- Every project has a unique project identity, its exact scene ID, one OrganizationBlock, one Continuous 20 ms task, one network, distinct recursive element IDs, a stable watch-table ID and unique row identities. All BOOL tags match scene names/bindings; PC-owned scene inputs map to Input and PLC-owned commands map to Output. Initial values come directly from each scene. Physical address fields are empty.
- Recursive node kinds follow `LadderProgram.cs:68–74`: Contact=0, Series=2, Parallel=3. The OR fixture explicitly has two series legs beneath one parallel root; it does not use legacy flat-branch semantics. The coil and network defaults are copied from an ordinary assignment-coil example.
- Exact expectations and action types come from `docs/help/scenes/lab-2-02-dual-confirmation.md`, `lab-2-03-service-marker-inhibit.md`, and `lab-2-04-two-station-call.md`; exact tag ownership/defaults come from their corresponding `scenes/migrated/*.scene.json` declarations.
- Preparation checked JSON syntax and declarative field structure only. App-native deserialization/validation and all screen behavior remain unverified until the visible loading workflow runs. If loading fails, preserve the actual error; do not credit the truth table or scene as tested.

## GUI fallback if a fixture is rejected

Open the target scene, then Logic Editor. Use its existing SceneIO declarations. Add one network with the output coil named above. For Lab 2.2 add the two NO contacts in series. For Lab 2.3 add the one NC inhibit contact. For Lab 2.4 add two parallel branches with one NO call contact on each. Verify + load offline, Run, and exercise the same truth table. Do not substitute forced outputs for the input-to-ladder-to-lamp test.

## Additional explicit Chapter 2 conditions

The next five files follow the same UI loading procedure, schema-9 shape and ownership rules. Their exact expectations come from corresponding `docs/help/scenes/<scene-id>.md` and `scenes/migrated/<scene-id>.scene.json` declarations. Scene `rules` are read only as declaration evidence, not executed as a controller or accepted as proof. Numeric equality uses the saved-project Compare node form (`kind=1`, `compareOperator=0`, numeric `rightOperand` string), with DInt input tags. The checked-in Demo 3 fixture establishes numeric comparison serialization; `LadderProgram.cs` defines Equal as comparison enum 0. Preparation performed only JSON syntax/field inspection.

### Lab 2.5 — Bay Light Selector

File/scene ID: `lab-2-05-bay-light-selector.rpproj.json` / `lab-2-05-bay-light-selector`.

Two ordinary assignment coils have separate equality-condition networks: `selector_position == 1` drives `bay_a_command` and `bay_b_command`. The actual selector action cycles DINT 0/1; do not treat it as a BOOL contact.

| selector_position | bay_a_command | bay_b_command |
|---|---|---|
| 0 | false | false |
| 1 | true | true |

Click Advance selector repeatedly through 0 -> 1 -> 0 -> 1. Observe both physical bay lights and independent symbolic outputs. Other numeric positions are not exposed by this selector and are outside the documented test scope.

### Lab 2.6 — Ready / Attention

File/scene ID: `lab-2-06-ready-attention.rpproj.json` / `lab-2-06-ready-attention`.

Two independent assignment networks: NC `request_held` drives `ready_light`; NO `request_held` drives `attention_light`.

| request_held | ready_light | attention_light |
|---|---|---|
| false | true | false |
| true | false | true |

The description calls the request spring-return, but its declared action is a toggle. Use the actual press/release toggle to observe released -> active -> released -> active. Exactly one lamp should be on in each accepted running state; physical held-mouse semantics require a separate check and are not asserted by this fixture.

### Lab 2.7 — Dual-Contact Permissive

File/scene ID: `lab-2-07-dual-contact-permissive.rpproj.json` / `lab-2-07-dual-contact-permissive`.

One assignment coil: parallel legs NO `reset_request` OR NC `stop_contact_nc` drive `permit_output`. The initial `stop_contact_nc` value is true; active inverted request is false.

| reset_request | stop_contact_nc | permit_output |
|---|---|---|
| false | true | false |
| true | true | true |
| false | false | true |
| true | false | true |

Use Toggle NO reset and Toggle NC contact to cover all four combinations, ending at the initial false/true state. The name does not imply a real safety permissive; this is a Boolean teaching lamp only.

### Lab 2.8 — Inspection Vote

File/scene ID: `lab-2-08-inspection-vote.rpproj.json` / `lab-2-08-inspection-vote`.

Three assignment networks: `vote_a AND NOT vote_b` -> `pass_indication`; `NOT vote_a AND vote_b` -> `hold_indication`; `vote_a AND vote_b` -> `conflict_indication`.

| vote_a | vote_b | pass_indication | hold_indication | conflict_indication |
|---|---|---|---|---|
| false | false | false | false | false |
| true | false | true | false | false |
| false | true | false | true | false |
| true | true | false | false | true |

Toggle inspectors A/B through neither -> A only -> both -> B only -> neither and repeat in reverse. Observe that conflict is red only; previous green/amber channels must turn off rather than remain latched.

### Lab 2.9 — Maintenance Beacon

File/scene ID: `lab-2-09-maintenance-beacon.rpproj.json` / `lab-2-09-maintenance-beacon`.

Three numeric equality-condition assignment networks: `beacon_position == 1` -> `red_beacon`; ==2 -> `amber_beacon`; ==3 -> `green_beacon`.

| beacon_position | red_beacon | amber_beacon | green_beacon |
|---|---|---|---|
| 0 | false | false | false |
| 1 | true | false | false |
| 2 | false | true | false |
| 3 | false | false | true |

Click Advance beacon selector through 0 -> 1 -> 2 -> 3 -> 0 twice. Check exclusivity and clearing of the previous channel. Lockout/service/released names are teaching indications, not proof of equipment isolation or a safety function.

## Deliberately skipped cases

- **Lab 2.10 edge policies:** normal Start/seal/Stop/repeat behavior is now covered by a scoped fixture below. Simultaneous requests, held Start versus Stop recovery priority and retention/restart edge cases remain outside the documented acceptance contract; they do not block the clear normal-flow screen exercise.
- **Lab 2.11 Inbound Tote Stop:** help names feeding/scanning/clearing/ready, but machine actions select a sequence rather than declaring a PC-owned start/reset request. The ownership and trigger route of `scan_complete`, dwell timing, and recovery need a deliberate sequence contract. No generic conveyor coil is substituted.
- **Lab 2.12 Assembly Lift:** documented Raise/Lower actions select named sequences, while the exposed PC-owned points are only top/bottom limits. No separate raise/lower command inputs are declared for a learner controller; command handoff, opposite-command priority, Stop and restart behavior need clarification. No automatic oscillating/default lift logic was invented.

Skipped questions are specification limits, not confirmed app defects. No complex lab receives fake default logic from these files.

## Later UI execution evidence

Lab 2.2 was subsequently opened through the normal visible file dialog after selecting its matching scene. Physical control clicks and the visible Signals monitor established 00=(false,false,false),10=(true,false,false),11=(true,true,true),01=(false,true,false), then00 and11 again. App Stop showed lamp off; Run restored the result with retained inputs; Reset followed by Run showed both inputs and output false. Evidence: `../lab202-monitored-*.png`, `../lab202-stop-active.png`, `../lab202-restart-retained-inputs.png`, and `../lab202-reset-inputs-cleared.png`. The physical lamp and monitored symbolic states were independently inspected. This is real UI evidence for the stated cases, not backend validation or a blanket application pass. Other fixture executions require their own recorded evidence; preparation alone supplies none.

## Additional counter and timer test inputs

These four later fixtures are unexecuted declarative test inputs. Select the exact matching scene before opening each file. Their memory Counter/Timer declarations, CTU, reset, numeric Move and ordinary coil shapes come from the saved Demo 1 and Demo 2 schema-9 files. Equal/GreaterOrEqual/LessOrEqual comparison nodes follow LadderProgram enum values 0/3/5. Only JSON syntax and declarative field/reference reading were checked; app loading and every behavior below still require visible UI evidence.

### Lab 4.2 — Counter Reset Lamp

File: `lab-4-02-counter-reset-lamp.rpproj.json`. Exact source scene has pulse_received/reset_pressed inputs and counter_lamp output; internal qa_count is an unbound Counter memory tag. Three networks count pulse_received with CTU preset3, reset qa_count from reset_pressed, and assign counter_lamp from qa_count.DN. No fabricated count output is bound to this scene.

Use COUNT three separate times, waiting for release between events. Lamp should stay off at counts1/2 and turn on at3. Machine RESET should clear the count/lamp; the next COUNT starts at1. Repeat a complete three-count/reset cycle. Monitor qa_count through the normal Watch UI when needed. Preset3 and this repeat expectation are explicit in docs/help/scenes/lab-4-02-counter-reset-lamp.md. Simultaneous COUNT/RESET is outside the documented test contract; this fixture orders the reset network after counting.

### Lab 4.5 — Dual-Input Count Window

File: `lab-4-05-dual-input-count-window-v2.rpproj.json` (supersedes invalid v1; see correction note below). Two internal CTU memories count channel_a_pulse/channel_b_pulse. Separate reset networks clear both; numeric Move networks publish their .ACC values to channel_a_count/channel_b_count. One series comparison network assigns window_ready when A>=2 AND A<=3 AND B>=3 AND B<=4.

The bounds are the help document's explicitly chosen reference-training values, not original source-specified presets; there is no elapsed-time window. Test A2/B3 -> true; A3/B3 -> true; A3/B4 -> true; A4/B4 -> false. RESET must clear both counts/output. A fresh cycle in a different input order should give the same result. Stop is documented to clear count readouts while retaining counter memory; Run republishes counts; app Reset clears memory and remains stopped. These playback expectations require actual screen testing, not inference from the fixture.

### Lab 4.6 — Multi-Press Confirmation

File: `lab-4-06-multi-press-confirmation-v2.rpproj.json` (supersedes invalid v1; see correction note below). Two independent CTU memories count button_a_pressed/button_b_pressed, reset_pressed clears both, Move networks publish button_a_count/button_b_count, and equality comparisons assign confirmation_valid only when A==2 AND B==3.

Again, 2/3 are the help document's chosen reference-training values. Cover A2/B2 -> false, A2/B3 -> true, A3/B3 -> false; RESET -> zero/zero/false; repeat with B pressed before A to verify order independence. Extra events must remove confirmation. No timing window or automatic physical press sequence is invented.

### Lab 5.2 — Timed Lamp-Off

File: `lab-5-02-timed-lamp-off.rpproj.json`. Scene start_pulse input feeds one Pulse timer qa_pulse_timer with preset3000ms; an assignment coil drives timed_lamp from qa_pulse_timer.Q. The preset is the explicit original offline reference's training choice in docs/help/scenes/lab-5-02-timed-lamp-off.md, not a claimed original source requirement.

Press START once while idle: lamp should turn on after the accepted input and later off at the documented interval. Another START during an active TP interval should not restart/extend it. After expiry a fresh START should start another interval; Stop cancels. UI checkpoints and observed elapsed wall time cannot prove the exact scan boundary. Use visible before/during/after timer and lamp states, keeping any precision claim bounded to the actual evidence.

More complex Chapter 3/4/5 sequences were not filled with generic logic. A scene name or a list of stages is insufficient to choose command ownership, timing, fault/restart policy, or process transitions without a documented contract.

### Lab 2.10 — Dust Collector Seal-In (scoped normal flow)

File: `lab-2-10-dust-collector-seal-in.rpproj.json`. Exact scene ID and all four points are preserved: collector_start_request and collector_stop_request are Input bindings; run_latched and collector_run are PLC-owned Output bindings according to the current SceneLadderProject owner mapping (the scene describes run_latched as memory). There is no physical PLC address or hidden plant sequencer.

Two scanned assignment networks implement `run_latched = NOT collector_stop_request AND (collector_start_request OR run_latched)` then `collector_run = run_latched`. Stop priority is a conventional **fixture implementation choice**, not a claim that an unspecified simultaneous-input policy has been accepted. No additional edge-memory or plant sequence was invented.

Screen procedure: select Lab2.10, open this fixture normally, Run; confirm both requests and both outputs initially false. Press the physical START once; after its pulse returns false, verify run_latched and collector_run remain true, with the fan/running lamp responding. Press STOP once; after its pulse returns false verify both outputs false. Press fresh START and repeat STOP for a second normal cycle. Monitor actual visible symbolic inputs/outputs and equipment; preparation establishes no pass.

Do not claim simultaneous Start/Stop, a held Start through Stop, app-reset retention, auto-restart inhibition, or safety behavior from this normal-flow fixture. Those require their own specified policy and separate screen cases. This fixture is unexecuted as prepared.

# Staged existing opt-in references

These files are byte-for-byte copies of the current local programs/examples projects. Each hash was checked against its source; JSON was read only to identify sourceSceneId and format. This is integrity/shape evidence, not a load/compile/runtime pass. Select the exact scene first, then File > Open Ladder Agent Project using the normal dialog Path folder and File leaf. They are optional learner references, not installed default logic.

| Staged filename | Required source scene | Source project |
|---|---|---|
| existing-02-bottle-shuttle-reference.rpproj.json | lab-2-20-bottle-shuttle | rungproof-next/programs/examples/02-bottle-shuttle-reference.rpproj.json |
| existing-09-box-volume-reference.rpproj.json | lab-9-10-box-volume | rungproof-next/programs/examples/09-box-volume-reference.rpproj.json |
| existing-09-function-selector-reference.rpproj.json | lab-9-04-function-selector | rungproof-next/programs/examples/09-function-selector-reference.rpproj.json |
| existing-09-product-function-reference.rpproj.json | lab-9-02-product-function | rungproof-next/programs/examples/09-product-function-reference.rpproj.json |
| existing-09-sum-counter-reference.rpproj.json | lab-9-03-sum-and-counter-function | rungproof-next/programs/examples/09-sum-counter-reference.rpproj.json |
| existing-09-sum-function-reference.rpproj.json | lab-9-01-sum-function | rungproof-next/programs/examples/09-sum-function-reference.rpproj.json |
| existing-barrel-fill-reference.rpproj.json | lab-4-11-barrel-fill-station | rungproof-next/programs/examples/barrel-fill-reference.rpproj.json |
| existing-cable-cut-reference.rpproj.json | lab-4-12-cable-cut-length | rungproof-next/programs/examples/cable-cut-reference.rpproj.json |
| existing-chain-lift-installation-reference.rpproj.json | lab-4-09-chain-drive-lift | rungproof-next/programs/examples/chain-lift-installation-reference.rpproj.json |
| existing-cookie-packaging-reference.rpproj.json | lab-4-10-cookie-packaging | rungproof-next/programs/examples/cookie-packaging-reference.rpproj.json |
| existing-dual-spindle-round-trip-review.rpproj.json | lab-2-22-dual-spindle | rungproof-next/programs/examples/dual-spindle-round-trip-review.rpproj.json |
| existing-robot-cell-restart-reference.rpproj.json | lab-3-02-robot-cell-safe-restart | rungproof-next/programs/examples/robot-cell-restart-reference.rpproj.json |
| existing-sequence-light-tower-reference.rpproj.json | lab-4-04-sequence-light-tower | rungproof-next/programs/examples/sequence-light-tower-reference.rpproj.json |
| existing-tote-fill-cap-review.rpproj.json | lab-2-21-tote-finishing | rungproof-next/programs/examples/tote-fill-cap-review.rpproj.json |
| existing-tote-finishing-process-review.rpproj.json | lab-2-21-tote-finishing | rungproof-next/programs/examples/tote-finishing-process-review.rpproj.json |

Exact SHA256, project identity and schema fields are in existing-reference-manifest.json. Staging changes no app/source file, does not invoke generators, and establishes no expected behavior beyond the existing reference documentation. Read each corresponding scene help/review document before choosing screen cases.

## Dual-counter fixture correction (v2)

The first Lab 4.5 fixture was rejected by the app's normal UI validation with VC016: both independent counter memories had inherited the saved single-counter example's DB1/IEC_Counter_0_DB. Lab 4.6 had the same declarative preparation error. This is a test-fixture error, not an app defect or failed plant behavior.

The original .rpproj.json files are unchanged, and exact evidence copies are preserved as lab-4-05-dual-input-count-window-invalid-v1.json and lab-4-06-multi-press-confirmation-invalid-v1.json. **Load the new -v2.rpproj.json leaves**. They assign qa_count_a and its reset to DB101/QA_CountA_DB, and qa_count_b and its reset to DB102/QA_CountB_DB. Instance numbers and symbolic names are distinct; each reset references its counter's owning DB. counter-fixture-v2-manifest.json records hashes. No backend load/compile/runtime verification was performed; successful UI loading and behavior still require new screen evidence.
