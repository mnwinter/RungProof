# Constrained connection studio meeting — ready for ratification

2026-10-11. Prepared from `byo-connection-department-audit.md` and `byo-connection-implementation-plan.md`. Independent design work is authorized; no additional user approval hold is introduced. Root coordinates final ratification and implementation after the three existing typed lessons close. Native typed-v1 reported408 passing checks; expanded Chicken v2 is in progress at this meeting record. Those results do not qualify this connection.

## Decision to ratify

Exactly one learner-added generic `material-handling.belt-conveyor.600x6000.v1` in existing offline `scene-1-conveyor-stop` opts into accepted-scan drive execution and measured feedback. This is a new bounded **connection capability**, not a new preset/menu scene or transported-load graph. Original authored scene equipment and all finite preset contracts remain intact. Material propagation to the other75 is independent and held.

The exact existing asset signals are `run_command` BOOL input, `running` BOOL output and `actual_speed` float32 output unit m/s. Persist an explicit relation registering instance-scoped PLC BOOL output → `run_command`, PC BOOL input ← measured `running`, and PC REAL input m/s ← measured `actual_speed`. Require one supported asset/controller, unit scale and zero rotation. Use existing `.65m/s` speed, `.75m/s²` ramp and simulator permissive true; no extra speed/permissive mapping or physical-safety claim. Reject conflicts with generic mappings on this opted-in instance.

Visual connector links remain authored-only. No beam, carton/pallet displacement, motor feedback or physical handoff is inferred. No vendor alarm interpretation, arbitrary graph execution, external PLC profile or transport bridge.

## Source facts versus required work

**Already exists:** `WorkspaceRuntimeAdapter`; `ConveyorController.RunCommand`, `Running`, `ActualSpeedMps`, `ApplyPlantTravel`, `ResetPlantTravel`; typed scene owner/type validation and PC sampling; shared session accepted-cycle ordering; immutable history128; real joint project persistence and current-draft gating.

**Must implement:** persisted derived point/connection declarations; one effective scene definition shared by shell/runtime/compiler; exact owner/unit/runtime eligibility policy; exclusive accepted projection using the current conveyor discrete ramp; validated PC feedback publication; Main shared accepted plant wrapper; connection identity retirement and unavailable-feedback gating. These are technical tasks, not existing APIs or external blockers.

Generic controllers currently run autonomous physics. Generic signal outputs currently return AUTHORED ONLY. Existing mapping detach tests project fixture values directly and are not learner-program acceptance. Do not relabel these as completed bidirectional connections.

## Ownership and integration sequence

| Department | Owned slice and concrete files | Handoff |
|---|---|---|
| Root/core engineering | `src/Workspace/WorkspaceDocument.cs`, `WorkspaceRepository.cs`, `WorkspaceProjectBundleRepository.cs`; new narrowly named pure connection policy; effective scene augmentation and eligibility. Root may allocate a dedicated engineering agent when implementation starts. | Freeze schema, derivation, exact validation errors and pure tests before consumers. Preserve readable invalid ladder projects. |
| Root/runtime integration | `src/App/Main.cs`, `Main.WorkspaceMappingLifecycle.cs`, `Main.ScanInspection.cs`; `src/App/ConveyorController.cs`; `src/Scenes/SceneSimulationRuntime.cs` and new scoped runtime partial if useful. | Sole accepted clock and coherent measured publication; typed callbacks shared by normal Advance and paused Step; lifetime/current-binding guards. Root owns combined build and all engine/capture runs. |
| Dot/editor and independent review | `src/App/SimulatorShell.cs`, related workspace/scan UI partials. Binding choices and owner/unit labels use the same effective declarations. Review schema/runtime semantics and final captures. | Explicit opt-in action, LIVE INPUT / LIVE FEEDBACK / AUTHORED ONLY / unavailable distinctions, current versus historical display; no solution load or observer output writes. |
| Native QA department | New narrowly named Main connection-review partial and pure-test fixtures, filenames allocated at implementation kickoff. Preserve `Main.WorkspaceMappingDetachReview.cs` and finite/Scan inspection regressions. | Actual saved joint intended/wrong/invalid projects, accepted payload/actual motion comparisons, exact next-scan feedback and lifecycle tests. No independent build/engine/capture. |
| Art | No geometry/material work required for this connection milestone. Existing asset is reused. | Wider75 material approval remains separate; no propagation or asset revision is implied. |

Each department freezes its assigned files before root builds. No competing edits to Main or shared schema; changes to public seams are announced before QA consumes them. No reset/clean/commit/publication of preserved studio WIP.

## Acceptance contract

1. **Ownership and persistence:** interactive edits and joint load enforce identical asset/endpoint/type/owner/unit rules, one input writer and one feedback producer, no collision with authored points. Saved relation/declarations/mappings and exact valid/wrong/invalid ladder JSON survive reopen. Failed load preserves active graph/draft/path/baseline/dirty state/undo history. Fresh reopen is stopped/home, not restored transient motion.
2. **One clock:** sample prior feedback → execute actual current draft/forces → apply effective run → integrate exactly one current discrete conveyor period → project real drums/UV → publish measured PC speed/running → record copied post-plant image. Scan N+1 samples N's feedback. Opt-in disables autonomous physics only on that belt; StateChanged, observer and history callbacks never advance it.
3. **Real wrong behavior:** at20/40/100ms, actual disk-open intended code runs then removes run at a bounded timer threshold. Assert measured ramp, commandOFF/runningTRUE coast, later speed0 and actual drum/UV travel. Compile-valid wrong code retains run and continues actual motion; no automatic correction. Invalid Run stops/no stale fallback; invalid paused Step accepts zero scans and preserves plant.
4. **Lifecycle:** command detach clears acquired command and decelerates on later accepted scans only while feedback relation and current compiled identity remain valid. Feedback detach becomes explicitly unavailable, retires identity and Stops/freezes accepted execution; no accepted coast follows that retirement and no false measured zero is published. Rebind regenerates declarations and validates the current draft before Run. Undo/rebind uses current real state. Stop immediately zeroes speed while holding pose/UV; Reset restores exact captured home and scan0/clear forces. Pause freezes wall time and Step accepts exactly one period while remaining paused. Placement replacement, draft/graph edits and stale callbacks cannot inherit old ownership.
5. **Observation and UI:** history/observer on/off full accepted payloads, points and native transforms/UV are identical. Historical rows are immutable and selection cannot change live motion/bindings. Actual final-binary renders demonstrate typed binding, paused/live labels, measured feedback and unavailable detach. No all77, transport, physical safety or release qualification follows.

Pure tests cover schema version compatibility, missing/unsupported endpoint, declaration collisions, owner/type/unit reversal, duplicate/conflicting mappings, malformed relation and rollback. Keep existing generic detach, finite belt/roller, five-demo and Pause regressions. Build success alone is insufficient; root runs the new native verifier and appropriate regressions, then Dot reviews evidence and reports bounded PASS_WITH_LIMITS or concrete blockers.

## Ratification status and next action

QA design is ready. Dot ratified the one-belt design with the lifecycle clarification incorporated above: feedback detach retires identity and Stops/freezes execution; command-detach coast requires still-valid feedback/current identity; rebind validates current draft; running retains the measured >.01m/s predicate. Root selected the persisted/declaration prerequisite after typed closure; source freeze/captures remain coordinated by root. This is design ratification, not connection implementation or qualification. No additional human approval is required to continue the already authorized software work.

## Root and Dot ratification — 2026-10-11
Design ratified for the existing authorized independent software work after the typed milestone. Dot requires this lifecycle clarification: feedback detach or identity retirement stops/freezes the accepted session and marks measured feedback unavailable. Command-detach coast is valid only while feedback ownership and the compiled current draft remain eligible. Rebind validates the current draft against rebuilt effective declarations before Run; no stale point or feedback substitution.

Implementation file allocation: root owns workspace persisted shape/repository/derived-scene policy plus Main lifecycle integration; plant engineering owns a narrowly scoped accepted-clock conveyor projection and PC publication adapter after root supplies the pure contract; QA owns pure policy and actual current-editor native wrong/invalid/joint/lifecycle fixtures; Dot independently reviews ownership/clock/visual evidence. No concurrent source mutation during a frozen build/capture epoch.

Implementation has not begun. The technical prerequisites in the implementation plan remain open; design ratification is not a completed runtime connection or new approval hold. No extra preset, transported load, arbitrary graph or external PLC path is included.
