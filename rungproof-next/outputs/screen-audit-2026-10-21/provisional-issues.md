# Provisional consolidated issues — discovery in progress

Scope reviewed here: targeted current screen evidence/help for Demos 1–4 and Lab 2.1, plus a bounded entry-render/OCR review of all 77 breadth rows. This is not the completed full-app issue list and is not a remediation plan. Persistent diagnostic presentation and one scene START progression blocker are demonstrated; many controls remain untested. IDs are stable; link additional affected entries to these records when evidence supports it. Implementation causes and whether separate findings share a cause remain unknown.

## QA-DOC-002 — Current teaching controls missing from help inventories

- Status/severity: demonstrated documentation omission, S4.
- Affected: Demo 1 / lab-4-01-press-count-lamp; Demo 2 / lab-5-01-delayed-lamp; Demo 4 / lab-9-11-pallet-counting.
- Reproduce: open the scene/help inventory and compare the Expected I/O and Operator actions tables with the current physical operator cabinet and curriculum trace.
- Expected: tables identify the learner-operated controls and displayed teaching values used by the current lesson, including machine controls distinct from app playback.
- Actual: Demo 1 tables list pulse/lamp but omit reset_pressed and the COUNT value, although RESET and COUNT are visible. Demo 2 lists REQUEST/lamp only; STOP, RESET, elapsed value and three of five rendered equipment objects are omitted. Demo 4's action table lists only event/type-valid/count-request, omitting visible machine START/STOP/RESET; its I/O table also omits machine_running even though the equipment-binding table uses it.
- Evidence: docs/help/scenes/lab-4-01-press-count-lamp.md; lab-5-01-delayed-lamp.md; lab-9-11-pallet-counting.md; demo1-repeat-check.png; demo2-curriculum-repeat-pass.png; demo4-board-checks.jpg; demo4-curriculum-progress.png.
- Reproducibility: omission is present in the current checked-in working documents and confirmed against each listed screen. Not a control-operation failure.

Additional documented scope: lab-2-07-dual-contact-permissive help defines stop_contact_nc and nc_button but omits their connection from its Equipment bindings table. Only reset_button and permit_lamp are listed there. This establishes a documentation inventory omission only; it does not establish the actual NC button is unbound. The later targeted test below establishes its operation.

Lab2.7 targeted follow-up: lab207-normal/reset-only/both/nc-only and repeat captures now demonstrate both physical inputs and the correct green-lamp truth table with a GUI-loaded fixture. The NC control is functional despite the missing inventory row. On-screen RENDERER BINDINGS also shows only the two listed rows. This remains an inventory-completeness finding, not a failed-control claim; see evidence-review-lab207.md.

## QA-DOC-003 — Demo 4 procedure describes toggling a pulse input

- Status/severity: demonstrated documentation inconsistency, S4; learner impact not yet measured.
- Affected: Demo 4 / lab-9-11-pallet-counting.
- Reproduce: compare the help Operator actions table and Normal sequence with the cabinet's NEW PALLET control.
- Expected: instructions describe one new momentary event per pallet, consistent with the action table and UI.
- Actual: the action table calls New pallet detection event a pulse, while Normal sequence says to toggle pallet detected OFF and ON for each event. The screen cabinet exposes NEW PALLET; recorded individual clicks advance the count from 1 through 5. The wording can lead learners to model two maintained toggles instead of the current event interaction.
- Evidence: docs/help/scenes/lab-9-11-pallet-counting.md:27 and :76; demo4-valid-first-event.png, demo4-five-events.png, demo4-events.json.
- Reproducibility: current document inconsistency plus current UI labeling; no wrong counter result observed.

## QA-UI-CANDIDATE-001 — Passed curriculum cards disagree on RECOVERY stage marker

- Status/severity: repeated display-consistency candidate, S4; no functional recovery failure established.
- Affected: Demo 1, Demo 2, Demo 3 curriculum cards. Demo 4 supplies the contrasting completed display.
- Reproduce: finish the lesson's machine recovery/repeat procedure, then open its Scenarios curriculum card.
- Expected: completed stage markers consistently agree with RECOVERY PROOF COMPLETE/PASS COMPLETE, or explain why recovery is exempt.
- Actual: Demos 1–3 show a dash before RECOVERY while QUALIFY/OPERATE/PASS have checkmarks, although the card reports pass complete. Demo 4's passed card shows a checkmark before RECOVERY.
- Evidence: demo1-repeat-check.png; demo2-curriculum-repeat-pass.png; demo3-overload-final-curriculum.png; demo4-curriculum-progress.png.
- Reproducibility: observed across three passed demo cards, with a contrasting fourth. Marker semantics and cause remain unverified; do not use this as evidence that physical recovery failed.

## QA-SPEC-001 — Generic Stop/Run wording leaves execution path ambiguous

- Status/severity: specification/naming question; unscored pending intended semantics.
- Affected: Demo 2 / lab-5-01-delayed-lamp and Demo 4 / lab-9-11-pallet-counting.
- Reproduce: compare help Expected observations with physical machine Stop behavior and the separately labeled App Playback controls.
- Expected: instructions explicitly name machine Stop/Start versus app Stop/Run when their policies differ.
- Actual: Demo 2 help:63 says retained ON after Run starts the full delay; acceptance current implementation:125 explicitly requires fresh OFF/ON after machine Stop/Reset. These may describe different paths and are not established contradictions. Demo 4 help:82 says Stop clears numeric output to zero, whereas physical STOP retained COUNT=5 with both lamps off. That help may describe app playback, so the observed physical behavior is not automatically a functional failure.
- Evidence: docs/help/scenes/lab-5-01-delayed-lamp.md:63; docs/DEMO_FUNCTIONAL_ACCEPTANCE.md:25 and :125; docs/help/scenes/lab-9-11-pallet-counting.md:82; demo2-curriculum-progress.png; demo4-machine-stop.png; demo4-board-checks.jpg.
- Reproducibility: wording is present; comparative app-playback cases have not yet been executed in this evidence subset.

Additional Lab2.20 scope: help for lab-2-20-bottle-shuttle declares simulation safe-state status_color=red, while lab220-app-stop-mid.png and lab220-app-stop-stable.png show App Stop with amber status, stopped scan and stable bottle position. The dedicated Stop shuttle action through Operator Console is not yet tested in this subset. Keep this as Stop-path contract clarification rather than a confirmed wrong-color functional failure. See evidence-review-lab220.md.

## Boundaries

Keep unresolved test coverage separate from defects. Exact timer scan boundary, held-event behavior, all invalid/permissive combinations, individual carton destination verification, and app-playback lifecycle are not passed by the available checkpoints. No repair priorities, root-cause clusters, or implementation plan selected before complete discovery.

## QA-UI-002 — Persistent invalid/no-networks diagnostics while authored rung runs

- Status/severity: demonstrated diagnostic-presentation defect, S3. The controller is not shown failing to execute; the contradictory guidance is the observed failure.
- Affected: lab-2-01-workstation-call; general editor scope unverified.
- Reproduce: enter the initially blank lab; click Run and observe EDIT001/no networks; create a network and NO contact with material_call_pressed driving material_call_on through the visible editor; click Run; allow the runtime to settle without further edits.
- Expected: once the visible network is monitoring and the controller is running, current verification/error guidance should reflect that loaded state, or explicitly identify any separate invalid draft.
- Actual: lab-2-01-run-authored.png shows the network monitoring, top runtime RUNNING/scan25 and green monitor status, but the Error List still says NOT VERIFIED and EDIT001/no ladder networks; footer says RUN BLOCKED/current ladder draft invalid. lab-2-01-editor-settled.png retains the same messages at runtime scan5030 and monitor scan5029. This is not just the first frame after Run.
- Evidence: lab-2-01-entry.png, lab-2-01-run.png, lab-2-01-authoring-contact.png, lab-2-01-run-authored.png, lab-2-01-editor-settled.png. lab201-held-checkpoints.jpg additionally shows the authored controller operating the lamp.
- Reproducibility: one controlled sequence with two temporally separated contradictory captures; not yet reproduced from a fresh app/session or in other labs. Cause unknown.

## QA-SPEC-002 — Held/spring-return lesson wording conflicts with maintained-toggle interaction

- Status/severity: demonstrated documentation/interaction-contract ambiguity; functional classification pending.
- Affected: lab-2-01-workstation-call and lab-2-06-ready-attention.
- Reproduce: load/authored direct NO-contact rung, press and release the physical material call button, and compare the purpose/expected-observations prose to the action table.
- Expected: the lesson clearly states whether mouse release releases the symbolic input or a second click toggles it off.
- Actual: help purpose and on-screen description say the lamp follows the button while held and is off when released. Operator actions explicitly define Press/release material call as toggle. Actual held/released checkpoints both show a blue lamp after the mouse-up; app Stop darkens it, app Run relights it with retained input, and app Reset clears it. This is compatible with the documented toggle contract, so it is not automatically a faulty input implementation.
- Evidence: docs/help/scenes/lab-2-01-workstation-call.md; lab201-material-call-held.png, lab201-material-call-released.png, lab201-app-stop.png, lab201-app-restart.png, lab201-app-reset.png; lab201-held.json for delivered OS actions.

Additional Lab 2.6 evidence: the visible scenario description and docs/help/scenes/lab-2-06-ready-attention.md call the button spring-return, but its action table explicitly says toggle. lab206-held-ui.json records native mouse-down held1.1seconds then mouse-up with0.8second observation. lab206-native-held.png and lab206-native-mouseup.png both show amber attention on/white ready off. The eight monitored cases separately prove a second toggle click restores readyWhite and that the PLC outputs follow the maintained request state. This is a demonstrated doc/control-semantic discrepancy, not an invented PLC failure. The intended contract/correction remains unresolved.

## QA-NAV-CANDIDATE-001 — Authored Lab 2.1 controller appears restored after Discard

- Status/severity: candidate navigation/draft semantics issue; unscored pending clean reproduction.
- Affected: navigation from lab-2-01-workstation-call to lab-2-02-dual-confirmation and back.
- Reproduce so far: Lab 2.1 initially has no ladder/controller; author and run one rung; navigate to Lab 2.2; at the Unsaved scene workspace prompt select Discard changes; later return to Lab 2.1 and click Run.
- Expected: Discard changes should have an explicit scope for the unsaved scene/ladder draft; expected scene-local controller caching is not yet established.
- Actual: lab202-entry-or-draft-prompt.png explicitly offers discarding unsaved scene or ladder draft edits. lab202-ready.png confirms the destination scene. The later breadth-lab-2-01-workstation-call-run.png shows RUNNING/scan47 without the blank-network gate encountered initially. That is consistent with restoration of a controller, but it does not independently prove which unsaved edits survived or whether an intentional cached loaded controller is separate from the discarded draft.
- Evidence: initial lab-2-01-entry.png and lab-2-01-run.png; lab-2-01-authoring-contact.png; lab202-entry-or-draft-prompt.png; lab202-ready.png; breadth-lab-2-01-workstation-call-run.png.
- Reproducibility: one indirect return observation; direct editor content on return and a clean discard/reentry repetition remain pending. Do not claim data corruption or confirmed Discard failure.

## QA-UX-CANDIDATE-002 — Gallery app Run directs toward missing ladder despite SIM-only motion help

- Status/severity: candidate workflow/specification mismatch; unscored pending dedicated-control test.
- Affected: equipment-gallery.
- Reproduce so far: open Reusable Equipment Gallery and select app Run.
- Expected: instructions identify the way to start its documented gallery motion without implying a learner must supply PLC logic for SIM-owned gallery points.
- Actual: breadth-equipment-gallery-run.png switches to the empty ladder editor and shows EDIT001/Add your scene logic before Run. Help describes Start/stop gallery motion as a toggle of SIM-owned gallery_animation. The actual dedicated motion control has not been exercised in this audit subset, so inability to animate the gallery is not established.
- Evidence: docs/help/scenes/equipment-gallery.md; breadth-equipment-gallery-entry.png; breadth-equipment-gallery-run.png.
- Reproducibility: one app-Run attempt. Dedicated gallery motion/Stop input and intended playback prerequisite remain pending.

## QA-VIS-CANDIDATE-001 — Lab 2.3 default view clips teaching wall plate

- Status/severity: repeated default-framing/readability candidate, S4; no control-function failure established.
- Affected: lab-2-03-service-marker-inhibit at 1600x900 client size.
- Reproduce: open the lab in the normal scene view; repeat after loading its matching fixture and starting through visible Run, without moving the camera.
- Expected: the lesson's teaching plate is readable in the initial view or has an evident viewing route.
- Actual: the NORMAL / INHIBIT teaching wall plate continues off the right client edge; the heading and symbolic names are cut off. The INHIBIT button, lamp and left description remain fully visible. Camera-reframing access is untested, so this is not a control blocker.
- Evidence: breadth-lab-2-03-service-marker-inhibit-entry.png; fixture-lab-2-03-service-marker-inhibit-running.png. Both are actual 1600x900 native captures with correct scene title.
- Reproducibility: observed in two visits, stopped and running; other resolutions and cameras untested. Cause unknown.

## QA-NAV-CANDIDATE-002 — Upper Scenario menu items absent from client-only capture

- Status/severity: environment/navigation observation; unscored, excluded from confirmed app defects.
- Affected observed context: lab-4-02-counter-reset-lamp with long Scenario popup open; wider scope unverified.
- Reproduce so far: open Scenario menu after earlier navigation; observe captured client area, then Home.
- Expected: select navigation based on current visible rows and verified popup bounds, not a cached coordinate for a first item.
- Actual: lab402-open-menu.png and lab402-menu-home.png start with Demo3 at clienty7; Five-demo Curriculum, Operator Console and Demos1/2 are absent from that capture. Clicking the old first-item coordinate requests another scene and opens the unsaved prompt. The tester canceled and reached Signals via View→Engineering Layout.
- Evidence: lab402-open-menu.png, lab402-menu-home.png, lab402-after-held-first.png, lab402-engineering-signals.png.
- Boundary: this may be retained scroll or a popup extending outside the captured client rectangle. Whole-popup bounds and scrolling have not been verified. The stale-coordinate test-helper assumption is confirmed; inaccessible menu or actual application clipping is not. No functional Signals defect inferred.


## QA-FUNC-001 — Scene start actions blocked by missing command bindings

- Status/severity: demonstrated scene progression blocker, S2, in the tested local built-in controller path.
- UI-confirmed affected entries: lab-2-11-inbound-tote-stop START; lab-2-12-assembly-lift RAISE and LOWER. Related static candidates must not inherit this confirmed status without UI tests.
- Reproduce: open Lab 2.11, explicitly load its matching diagnostic fixture and Run; click physical cabinet START at (1212,574); open View / Engineering Layout / Health event history.
- Expected: the documented Start tote cycle control delivers a usable command to the loaded controller so a learner-authored compatible program can initiate the inbound cycle.
- Actual: Health history records 23:23:48 “Action blocked · this scene action has no command binding in the loaded ladder program,” following 23:23:09 “Built-in controller running.” The same native frame shows selected Lab 2.11 and LOCAL RUNTIME RUNNING / scan2025; PLC DISCONNECTED indicates the local test context. The physical click is recorded by the OS-input action log. This is affirmative command-rejection evidence, not an inference from a stationary tote or a false diagnostic probe output.
- Evidence: fixture-lab-2-11-inbound-tote-stop-running-1792650190677842300.png; lab211-start-history-repro-full.png; lab211-health-history.png; lab211-history-ui.json; ui-action-log.jsonl entries117-120. Help docs/help/scenes/lab-2-11-inbound-tote-stop.md declares Start tote cycle / start / scan-cycle and the physical inbound_start equipment. Read-only scene-contract inspection finds that start action's scan-cycle but no controllerStartBinding.
- Scope boundary: one affirmative physical START rejection with matching running fixture; full feeding/scanning/clearing/ready sequence was not proved. Static inventory reportedly identifies other missing bindings, but this record confirms only Lab 2.11. No repair plan or shared-cause scope inferred.


QA-FUNC-001 Lab 2.12 confirmed extension: `lab212-binding-ui.json` records grounded RAISE click (969,542), Health inspection, return to Scene, LOWER click (999,553), and second Health inspection. Full native `lab212-lower-health.png` identifies Lab2.12 / LOCAL RUNTIME RUNNINGscan2209 and explicitly records two “Action blocked · this scene action has no command binding in the loaded ladder program” events at23:30:18 and23:30:20, after23:29:37 Built-in controller running. `lab212-raise-health.png` supplies the first-action checkpoint; `lab212-lower-health-history.png` is the readable crop, not independent runtime evidence. Expected: documented Raise to work height / Lower to load height commands reach the matching loaded controller; actual: both are explicitly rejected. Current help and scene contract name start/raise and start/lower, with no controllerStartBinding. This adds two confirmed controls in one affected scene to QA-FUNC-001, not two duplicate issues. No inference about lift motion or limit behavior is made; those flows remain untested.


QA-FUNC-001 additional confirmed scope: Lab2.13 coolant-jug-fill START FILL at(1150,568), Lab2.19 service-door OPEN at(1075,591), and Lab2.23 parcel-sorter START SORT at(1236,574) each explicitly reject the action with the same missing-command-binding message. Full native `lab213-start-fill-health.png`, `lab219-open-door-health.png`, and `lab223-start-sort-health.png` identify the selected scene and RUNNING local runtime (scans3425/3118/2587). Their readable history crops show rejection events23:32:10 /23:34:46 /23:35:52 following each scene's built-in-controller-running event. Plans `lab213-binding-plan.json`, `lab219-binding-plan.json`, `lab223-binding-plan.json` ground the physical clicks. Original OCR misses are preserved; append-only `binding-visual-review.jsonl` records manual visual truth and full-native source hashes. No process motion/completion claim is made.

QA evidence exclusion: `lab219-close-door-health.png` is misnamed by the test plan. The second red control at(1106,601) is STOP, and history explicitly says ACTION ACCEPTED / IMMEDIATE STOP / MOMENTARY plus built-in controller stopped at23:34:48. This is a correctly accepted STOP and a QA control-identification error, not a failed CLOSE or wrong app binding. The actual blue CLOSE control remains pending. Lab2.16 initial unmet-permissives rejection is likewise not missing-binding proof and remains excluded until prerequisites are visibly enabled and START retried.


QA-FUNC-001 final binding-probe extension: Lab2.24 START CNC rejects at23:37:05 (`lab224-start-cnc-health.png`, selected Lab2.24 RUNNINGscan3071); Lab2.16 ready START DRILL rejects at23:38:33 (`lab216-ready-start-drill-health.png`, RUNNINGscan3877); and actual blue Lab2.19 CLOSE at(1137,611) rejects at23:39:08 (`lab219-correct-close-door-blue-confirmed-health.png`, RUNNINGscan1222). Readable history crops preserve each explicit missing-command-binding event. `lab216-both-requests.png` / `-points.png` visibly show left_hand_request=True, right_hand_request=True, workpiece_present=True, drill_at_top=True, drill_run=False before START; history separately records accepted maintained left/right toggles. Thus this is distinct from the earlier correct unmet-permissives rejection. The blue CLOSE retry supersedes its prior pending coverage, but does not relabel the earlier red STOP event.

Consolidated UI-confirmed QA-FUNC-001 scope is seven scenes / nine distinct controls: Lab2.11 START; Lab2.12 RAISE and LOWER; Lab2.13 START FILL; Lab2.16 ready START DRILL; Lab2.19 OPEN and actual CLOSE; Lab2.23 START SORT; Lab2.24 START CNC. These are explicit local command rejections with matching GUI-loaded diagnostic fixtures, not completed machine-flow tests. Append-only binding-visual-review.jsonl contains full-native SHA256 provenance; no original OCR result rewritten.


## QA-REF-001 — Supplied Lab 9.3 reference fails current validation

- Status/severity: demonstrated supplied-reference incompatibility / exercise progression blocker, S2. Not classified as a compiler defect.
- Affected: lab-9-03-sum-and-counter-function opt-in supplied reference programs/examples/09-sum-counter-reference.rpproj.json.
- Reproduce: select Lab9.3; use visible Project Open to open the unchanged staged copy existing-09-sum-counter-reference.rpproj.json; inspect verification, then attempt Run.
- Expected: the supplied opt-in reference described by the machine guide can Verify + Load and Run, enabling the documented A2/B5/SUM7/COUNT1 exercise.
- Actual: Project Open shows one error VC008 at $.blocks[0].networks[1].counter.preset: “Preset 2147483647 exceeds the Int counter range.” Top status is STOPPED / NO CONTROLLER LOADED; attempted Run does not enter a running controller. The source file's Count qualified completions rung declares isCounter=True, counterKind=CountUp, counterVariable=completed_calls, counterPreset=2147483647. This is an app-supplied reference rejected by the app, distinct from an invalid reviewer-authored fixture.
- Evidence: fixture-lab-9-03-sum-and-counter-function-project-open.png; fixture-lab-9-03-sum-and-counter-function-running.png (filename does not imply running: native status is STOPPED); fixture-ui-actions.jsonl; test-projects/existing-reference-manifest.json; current source reference and its byte-identical staged copy independently hash-checked during review. Help docs/help/scenes/lab-9-03-sum-and-counter-function.md directs authoring or loading the opt-in reference and documents expected count/result.
- Boundary: exact intended counter type/range repair is not determined. Arithmetic, counting, invalidity, Stop/Reset and motion are untested because the supplied program cannot load. No app/reference edits or repair plan performed.


## QA-VIS-002 — Lab2.15 speed indication stays dark at every running speed

- Status/severity: demonstrated scene feedback/contract mismatch, S3; exact implementation cause unresolved.
- Affected: lab-2-15-fume-extractor local learnerfixture path.
- Reproduce: GUIload matching learnercontroller; run; cycle physicalspeedselector through1/2/3 withhoodlightoff, thenrepeatwithhoodlighton.
- Expected: help Expectedobservations explicitly says off/35/65/100percent and one speed indication. Help/scene declare low_speed,medium_speed,high_speed SIM-owned; equipmentbinding maps these to speed_display green/amber/red. Learnernormalprocedure only directs PLC-owned hood_light_on/fan_run/fan_speed_percent, so missingSIM indication is not automatically an unimplemented learneroutput.
- Actual: native lab215-light-off-speed1/2/3 showselector1/2/3,fan_runTrue,fan_speed_percent35/65/100, but allthreeSIMspeedflagsFalse andphysicalspeedstackdark. lab215-light-on-speed1/2/3 independently repeatssame whilehoodlightTrue/whitepilotlit. All15caseSignals showthreespeedflagsFalse; no speedfeedback appears.
- Evidence: those6nativePNGs; lab215-review-1.jpg and-2.jpg; docs/help/scenes/lab-2-15-fume-extractor.md; scenes/migrated/lab-2-15-fume-extractor.scene.json pointownership/bindings/acceptance. No static/backendtest is used asruntimeproof.
- Boundary: fanrun/speednumericoutputs andhoodlightwork; nofanmotionfailure orwrongbindingcause asserted. Freshappsessionrepro remains pending. No fixplan.

## QA-FUNC-002 — Lab 11.7 physical common-stop station has no action route

- Status/severity: demonstrated advertised physical-control route failure, S2; alternate UI routes not exhaustively excluded.
- Affected: lab-11-07-multi-conveyor-pallet-route, common stop station training_accessory_8.
- Expected: pressing advertised common-stop control toggles PC common_stop_request so the learner ladder can remove all zone commands.
- Actual: two grounded clicks on visible red head(664,438) produce no action event; native common_stop_request staysFalse, allthree zone_runTrue and green stacks lit. Three neighboring zone cabinet controls do produce accepted events. Pallet was already complete, so moving-pallet arrest is not asserted.
- Reproduce/evidence: lab1107-calibration.json fourth click; lab1107-stop-probe.json retry; lab1107-controls-crop.png; lab1107-stop-retry.png RUNNING11335; lab1107-stop-health.png/history crop with onlythree accepted zone toggles. Detailed independent report evidence-review-lab1107-stop.md.
- Corroboration: scene config for training_accessory_8 has no action/sceneControls; SceneControlInteractor.cs166..189 requires explicit fields; declared toggle-common-stop action exists but SimulatorShell.cs4433 hides OperatorActions. Help advertises that action and common-stop behavior. This is distinct from QA-FUNC-001 explicit missing controller command bindings.
- Boundary: actual common_stop_requestTrue and release/fresh-edge PLC response untested; AppPlaybackStop is distinct. No repair plan.

QA-NAV-CANDIDATE-001 additionalboundedobservation: nativeScenario menuhighlightsConveyorInspectionCell and Unsavedsceneworkspacemodalfooter saysOPENINGSCENARIOConveyorInspectionCell; reportedDiscardclick666538 isfollowedbyimageLab9.1/STOPPED/LADDERNOTLOADED. Evidence conveyor-target-highlight-review.png, fixture-conveyor-cell-navigation-intermediate.png, conveyor-after-discard-own-workspace.png; report evidence-review-discard-conveyor-candidate.md. Pre/postimagesdifferentdimensions1600x900 versus2560x1369/layoutsurface, so samewindow/provenance andtargethandling remainunresolved. Candidateonly; no confirmedwrongsceneDiscardbug orsharedcause claimed.

### QA-VIS-002 fix verification — normal route
Fresh built-app native selector sequence 0→1→2→3→0 now shows dark→green→amber→red→dark and matching SIM flags for PLC commands 0/35/65/100/0. See `evidence-review-fix-fume.md` and `fix-fume-r1-review-compact.jpg`. Original normal-route defect resolved in this observed build; light independence and playback lifecycle checks pending.

### QA-VIS-002 closure — screen verified
Seven r2 native follow-up cases confirm speed flags and physical green/amber/red lamps with hood-light output True; Stop clears all speed indications, Run restores retained low, and Reset clears selector/output/flags. Combined 12 native checkpoints resolve the demonstrated speed-stack defect in the tested built app. Details: `evidence-review-fix-fume.md`, `fix-fume-r2-review-compact.jpg`. This is not a measured shaft-speed claim.

### QA-FUNC-002 closure — screen verified
Post-fix physical common-stop click extinguishes all three run lamps and holds the moving pallet; actual Signals confirm common_stop_request True/all three zone outputs False. Proper R3 release retains stop until fresh clear OFF→ON, then completes at position12.3/route_complete True. R4 Reset returns -9.3/all clears and runs False. See `evidence-review-fix-commonstop.md` and compact native sheet. Defect resolved for this tested station route/build; stale-camera R2 attempt excluded.

### QA-UX-CANDIDATE-003 — advertised radar inspection view has no discovered UI route
Radar scene help advertises shell/roof ghosting to inspect beam/surface. Actual body/transmitter attempts retain inspectionFalse and opaque shell (`standalone-radar-r4-*`); no explicit mapped inspection control found in tested views/static inventory. S3 candidate: advertised learning view unavailable through discovered controls; exact intended control contract still unclear. Flow/gauge/lifecycle function independently. See `evidence-review-standalone-radar.md`; do not merge with fixed common-stop defect.

### QA-FUNC-001 additional affected scene — high/low tank
Native `standalone-highlow-r1-health-crop.png` shows explicit missing-command-binding guards after both physical pump/drain attempts while built-in controller is running. Same demonstrated failure category, two additional actions/one scene; earlier seven-lab fix closure does not cover these tank controls. See `evidence-review-standalone-highlow-binding.md`. No normal-flow credit for this probe.

### QA-FUNC-001 second tank confirmation / total scope
Tank Level /4–20mA native `standalone-level-r1-health-crop.png` shows matching explicit missing-binding guards11:36:16/17 after controller running11:35:14 for physical pump/drain attempts. Combined category now9 scenes/13 controls observed over the audit; two tanks/four controls are newly open beyond the earlier seven-lab fix. Both normal fill/drain routes blocked. See combined tank binding evidence review.

### QA-FUNC-002 new open subcase — Lab10.3 class selection
S2: documented cycle-vision_class1–4 action has no exposed physical mapping/visible action rail. Native valid display clicks do not change class; actual Signals retain validTrue/class1 and no class acceptance is recorded. See `evidence-review-vision-class-route.md`/`vision-class-click-review.jpg`. Shared unmapped-operator-action category now partial: Lab11.7 stop fixed; Lab10.3 selection open. Classes2–4 normal routing remain unproven, not failed PLC routing.

### QA-REF-001 closure — repaired supplied reference screen verified
Exact repaired reference opens/runs normally. Independent physical displays show SUM7/COUNT1 held, then fresh COUNT2; R4 native Signals show retained count2 with completionFalse/sum0/validFalse, fresh completion count3/sum7/validTrue, Reset0 and fresh repeat count1/sum7. See `evidence-review-fix-shipped-sum-counter.md` and two compact sheets. Original invalid shipped preset remains historical; closure is for tested repaired artifact.

## QA-PLANT-001 — Lab2.11 command does not move tote
S2 confirmed: native accepted-Start checkpoints1/6s leave tote upstream while actual conveyor_runTrue/scannerFalse/scan_completeFalse. Distinct from fixed request binding. See `evidence-review-lab211-plant-gap.md`/`newbinding211-review-compact.jpg`. Authorized narrow command-driven plant implemented, pending consolidated build/native verification. Six other legacy sequence scenes remain static candidates only.

### QA-PLANT-001 confirmed scope extension — Lab2.12
Native Raise+1/+6s shows unmoved scissor/table/fixture while actual lift_upTrue/lift_downFalse/position0/bottomTrue. Same absent command-driven plant category, distinct from request binding. See `evidence-review-lab212-plant-gap.md`. Narrow bidirectional mechanics implemented pending build/native proof. Confirmed category2 scenes; other five remain static candidates.

## QA-LOAD-001 — Lab2.13 load rejects new request provenance
S2 native-confirmed correct2.13 target followed by Discard leaves old2.12 stopped. Actual log proves Discard callback executes correct target, but Compose throws Station request provenance drift/fill_start due stale expected empty binding. Distinct from QA-NAV candidate; no callback defect inferred. Narrow legend expectations updated for2.13 and static same-table2.23/2.24 risks; pending consolidated build/composition/native proof. See `evidence-review-lab213-navigation-load-failure.md`.

### QA-PLANT-001 Lab2.11 / QA-FUNC-001 Lab2.11 bounded fix verification
Fresh B2 native physical Start now moves tote to completed downstream endpoint (scanCompleteTrue/cycleCompleteTrue/conveyorFalse). Active AppStop freezes pose, Reset restores upstream/all feedbackFalse and fresh repeat completes. Original2.11 absent-motion/request delivery failures resolved for observed routes. Explicit scannerTrue sample/exactdwell not claimed; Lab2.12 plant subcase still pending native verification. See appended2.11 evidence review and compact sheet.

### QA-PLANT-001 Lab2.12 / QA-FUNC-001 Raise-Lower closure
Native scissor/platform/fixture move together up/down; top100/topTrue and bottom0/bottomTrue both commandOff. Active Stop holds intermediate pose; Run alone stays held; separate Lower returns without completed-top (cycleFalse correctly). Fresh R3 Raise-top/Lower-bottom yields cycleTrue, Reset clears it. Both request mapping and missing-plant subcases resolved for this tested build/route. See appended2.12 report and R1/R2/R3 compact sheets. R2 incorrect cycleTrue expectation excluded; UI conflict not tested.

### QA-FUNC-001 high/low tank closure
Native accepted pump/drain requests now produce physical gauge fill→75.12 high cutoff and priority drain→24.96 low cutoff. Active midrange Stop holds64.72/requestTrue/outputsFalse; Run reaches high cutoff correctly, Reset50 clears requests/outputs. Both high/low tank controls resolved for tested routes/build. Late pumpTrue expectation at high limit excluded as QA timing error. See `evidence-review-fix-tankhighlow.md` and two compact sheets.

### QA-FUNC-001 Tank Level closure
Both physical requests accepted: visible gauge fills/high cutoff80.08, priority drains/low cutoff19.96; active Stop holds49.98, Run resumes61.74, Reset42 clears requests/outputs, fresh repeat64.68. Actual sampled current values satisfy4+0.16*level. Two Tank Level controls resolved in this build/tested routes. See `evidence-review-fix-tanklevel.md` and corrected Stop gauge strip. Remaining shared-category subcases tracked separately.

### QA-FUNC-002 Lab10.3 closure — native class2/3/4 routes
Physical class selection now reaches native class/latched2,3,4 with completed distinct receiving branches and both outputsOff. Class1 normal route was independently documented earlier. Unavailable selection defect resolved in tested build/routes. Native class3/4 permit omissions/layout ambiguity excluded as QA errors, not app failures. See appended vision-class report and compact class3/class4 sheets.

### QA-PLANT-001 confirmed scope —2.13/2.16/2.19
Independent native before/after accepted-command comparisons confirm jug at infeed despite conveyorTrue, drill remains top despite drill_runTrue/ready inputs, and door remains closed despite motor_openTrue. See dedicated evidence reports/compact sheets. Confirmed category now five scenes total2.11/12/13/16/19;2.11/12 fixes screen-verified, new three implemented/pending native tests.2.23/24 remain static candidates. QA-LOAD-0012.13 now native-compose verified in B2; do not reopen Discard callback.

### QA-PLANT-0012.23/2.24 native confirmation; QA-LOAD-001 closure
Actual cartons/blank remain upstream across accepted outputTrue intervals in both scenes. See `evidence-review-lab223-224-plant-gaps.md`. Confirmed missing-plant category now all seven original legacy scenes, with2.11/12 fixed/native-verified and other five pending. Correct2.23/24 composition succeeds natively, resolving QA-LOAD-001 tested provenance subcases along with2.13; no workspace callback issue. Parcel proposed fullmechanics/input contract saved separately, no implementation yet.

### QA-PLANT-0012.19 / QA-FUNC-001 OPEN-CLOSE closure
B3 native shutter opens0/NCFalseTrue, closespartially thenstationStopholds28/bothNCTrue/alloff; AppRun+freshClose recovers100/NCTrueFalse/alloff, Resetclearscomplete, freshOpenrepeat0. Missing movement/requestsubcases resolved for tested routes/build. R3 missingRunexcluded QAerror. See `evidence-review-fix-service-door.md` and two compact sheets.

### QA-PLANT-0012.13 post-fix transport works but nozzle stroke mismatch found
B4 native jug reaches fillstation with jugPresentTrue/conveyorFalse/valveTrue/skidTrue, but fill remains0. Exact delivered geometry places closedoutlettip2.14m; old120mmstroke leaves2.02m, above scalednecktop≈1.982m. Strict nozzle-inside-neck eligibility correctly blocks liquid accumulation. Scene-local stroke correctedto180mm (outlet1.96m inside neck); pendingB5native proof. No tolerance bypass/hidden fill sequence. Earlier transport fix is not a fullfill-cycle closure.

### QA-PLANT-0012.16 bounded native verification
B4 feed/retract poses and actual topTrue/cycleTrue/allcommandsOff endpoint now demonstrate normal mechanical response; left-hand loss holds an intermediate pose with both limitsFalse/allcommandsOff. Original absent-feed failure resolved for sampled normal/hand-loss routes; AppStop/Reset/repeat not yet independently reviewed. See appended drill report/compact sheet. No rated drilling or exact dwell claim.
