# BYO connection department audit — proposal only

2026-10-11. Read-only source audit of the canonical studio worktree. No app source changes, build, engine run, or new preset. The current support matrix identifies DLL `5067389C01C929C7FEFF1BCD18007C0846715641C080AC95D72A4B24F9B25652` and bounded Pause/Scan acceptance; this audit does not extend that qualification. Wider75 material propagation remains independently held.

## Recommendation

Choose one generic **belt drive command and actual speed feedback contract**, using the existing `material-handling.belt-conveyor.600x6000.v1` asset and its exact signal endpoints. This is the smallest meaningful extension of existing BYO connections. It is a signal connection contract, not a material handoff or another preset. Department meeting must approve its point declaration/persistence and accepted-clock scope before implementation.

If the next milestone specifically requires an asset-to-asset physical connector, the current source does not supply a comparably small complete runtime contract. Do not rename visual connector compatibility as simulated material transport. The belt/photoeye candidate below needs a separately explicit load and detection geometry relation first.

## What exists

`assets/catalog/production.catalog.json` contains connector metadata on only three assets:

| Asset | Exact connectors | Metadata semantics |
|---|---|---|
| `material-handling.belt-conveyor.600x6000.v1` | `material_in`, `material_out`, `power_24vdc` | Material endpoints kind `material_flow`; electrical endpoint kind `electrical_24vdc`. Material-out local metadata `[3,0,1.055]`, orientation `[0,180,0]`. |
| `material-handling.pallet-roller-conveyor.1000x4000.v1` | `pallet_in`, `pallet_out`, `power_3phase` | Pallet endpoints kind `pallet_flow`; electrical endpoint kind `electrical_3phase`. Pallet-out `[2.05,0,1.1]`, orientation `[0,180,0]`. |
| `sensing.photoelectric.through-beam.v1` | `beam_center` | Kind `detection_volume`, local `[0,0,.94]`, compatible with `material_flow`. This is the legacy asset, not the external sensor pair used by the finite roller. |

`Main.CreateConnectorLink` and `WorkspaceRepository.ValidateConnections` accept compatibility declared by **either** endpoint, reject identical self-endpoints, and resolve instance/connector existence. UI rejects duplicate unordered links; repository checks unique link IDs but does not reject duplicate endpoint pairs with different IDs. There is no port direction, degree limit, alignment/tolerance, supported-load, handoff, or sensor-volume validation. Connector rotation metadata is not consumed by `RefreshConnectionVisuals`; it draws a line/markers using positions transformed from catalog axes `(x,y,z)` to Godot `(x,z,-y)`.

`WorkspaceSignalRuntimeAdapter` attaches autonomous controllers only to the two conveyor assets. It applies exact input signals `run_command`, `estop_ok`, `speed_setpoint`. Asset output direction always returns `AUTHORED ONLY`, with no scene-point publication. `LiveFeedback` exists as an enum value but this generic adapter does not return it. A catalog output or a connector line is not runtime feedback.

Signal-link creation/load currently checks primitive type compatibility, not PLC/PC ownership or units. `float32` accepts scene `REAL`; that does not mean route position in metres can drive belt speed in m/s. The detach verifier's `route_position=.4` projection is intentionally a declared-point adapter fixture, not a unit-correct learner program acceptance.

The stale command fix tracks ownership only after a generic input is actually accepted. `Main.WorkspaceMappingLifecycle.cs` clears formerly owned run/permissive to false and speed to zero, then surviving mappings reapply current values. Never-mapped defaults are retained. Link removal permits normal autonomous coast; it is not the finite preset's immediate Stop/frozen plant contract.

## Candidates

| Candidate | Actual endpoint basis | Additional work and risk | Decision |
|---|---|---|---|
| One belt command ↔ measured drive feedback | Existing `run_command` BOOL input, `running` BOOL output, `actual_speed` float32 m/s output; controller exposes `Running`, `ActualSpeedMps`, `ApplyPlantTravel`, `ResetPlantTravel`. | Explicit point owners and unit declarations; single accepted-clock opt-in; output publication lifecycle. No transported object needed. | **Recommend** as the first runtime connection contract. |
| Belt `material_out` → sensor `beam_center` | Already compatible by metadata; `Main` already creates this link in workspace fixtures. | No generic sensor adapter, no defined load membership/path or detection volume. Metadata center alone is not a ray or collision test. Existing finite belt binds four locked objects and rejects all connector links. | Defer physical sensing. Keep present link visibly authored-only. |
| Roller `pallet_out` → second roller `pallet_in` | Both exact endpoints kind `pallet_flow`; existing roller drive animation. | Transfer ownership, contact geometry, gap/heading tolerance, two commands, jam/boundary behavior, exclusive pallet location, feedback, accepted-clock coordination. No generic pallet transport exists. | Defer; too large for the next bounded connection slice. |

## Proposed exact single-belt contract

Use a learner-added generic belt in existing offline `scene-1-conveyor-stop`, with independent instance-scoped bindings; do not alter its authored conveyor/photoeye or finite preset relations, or create a menu preset. The meeting must approve this source scene and establish additive persisted scene-point declarations. Current generic workspace schema has no such declaration field; current runtime has no public generic feedback writer. This is a real prerequisite, not an API to assume. `conveyor-cell.scene.json` has a `conveyor_speed` REAL m/s point, but owner SIM and an existing plant make it unsuitable to borrow as this new instance's PC feedback.

Proposed instance-scoped point identities (names are a proposal, not current API): `<instance>_run` PLC-owned BOOL output; `<instance>_running` PC-owned BOOL input; `<instance>_actual_speed_mps` PC-owned REAL input, unit `m/s`. Map exact asset signals `run_command`, `running`, `actual_speed`. Keep a documented bounded fixed positive speed `.65 m/s` and default simulator permissive true in this first contract; expose neither additional speed-command mapping nor safety logic until approved. `estop_ok` is explicitly a simulator permissive, not a physical safety function. Require the supported belt asset, actual supported controller and valid imported motion nodes; metadata alone is insufficient.

The contract owns only these instance-scoped points and the opt-in belt clock. One accepted scan samples prior PC feedback, executes the current compiled draft, applies effective PLC command, integrates one belt period with acceleration `.75 m/s²`, projects real drums/UV through the existing controller, and publishes measured speed/running as copied post-plant PC feedback. `Running` must follow existing `abs(ActualSpeedMps)>.01`; command OFF and running TRUE during coast is legitimate. Scan N's new speed reaches the input sample at N+1, not retroactively in N. Disable autonomous physics **only for this opted-in instance**; unrelated generic conveyors remain unchanged.

No `beam_blocked` or carton displacement is inferred from this contract. Do not bind its speed to an existing position point, bind running to an existing photoeye point, or overwrite authored scene feedback owned by another model.

## Lifecycle acceptance to agree before code

1. Link ownership is directional: PLC BOOL → supported asset input; measured asset outputs → exclusively PC BOOL/REAL with exact unit. Reject missing endpoints, wrong asset/controller, wrong owner/type/unit, duplicate writers or competing target input. Source scene-owned feedback cannot silently be commandeered. Load validation and interactive editing must enforce the same rules.
2. Removing run mapping clears only that owned command and allows documented accepted-scan deceleration. Removing a feedback link removes publication ownership; it must not leave an old nonzero feedback value presented as live. Agree whether the declared point remains explicitly unavailable or is retired transactionally; zero presented as measured is misleading. Undo/rebind samples current controller state, never restores historical measured speed as live. Placement removal retires all owned bindings atomically.
3. Pause freezes actual speed, drums and UV; Step accepts exactly one PLC/plant period. Stop must be separately specified for this opt-in accepted plant, preferably zero speed and held transforms consistent with qualified learning controls. Reset restores captured authored transforms/UV, scan zero and cleared forces. These proposed behaviors do not describe today's autonomous generic controller.
4. Current valid-wrong draft executes its actual output and is observed without correction. Invalid current Run stops and blocks stale execution; invalid paused Step remains paused with unchanged clock/plant. Draft-only edits and real project Open retain their existing distinct lifecycle semantics. Graph/declaration edits retire accepted/history identity; never execute an old controller against a new binding graph.
5. Joint save/reopen preserves exact layout, binding identities, typed declarations and readable valid/wrong/invalid ladder JSON; physical motion restarts home/stopped, not at a fabricated recovered transient. Failed open rolls back active path, saved baseline, dirty state, graph, draft and undo history. No external PLC profile or transport bridge.

## Meaningful verification

Pure schema tests: supported exact endpoints and assets; missing instance/point; PLC/PC reversal; BOOL/REAL mismatch; m versus m/s; duplicate target and feedback writers; duplicate connector pair parity between interactive/load paths; deterministic identity across save/load; malformed persisted relation rejected without changing the last good project. Keep ordinary visual connector compatibility separate from runtime eligibility. If implemented, extract validation logic from Godot file IO for genuine pure tests.

Actual native learner tests at 20/40/100ms must start from saved joint projects opened through Main and Run the current editor draft. Intended code starts the belt and removes run at an agreed finite timer threshold; assert actual accepted outputs, measured ramp speed, drum rotation and UV travel, command-OFF/running-TRUE coast, later zero, and next-scan feedback causality. Compile-valid wrong code maintains run after that threshold; assert continued positive actual speed and advancing transforms/UV, without automatic correction. Invalid draft during motion must block further scans and obey specified Stop; exact invalid JSON must survive joint reopen.

Exercise real delete/Undo/redo/rebind while moving, unavailable source, target removal/replacement, Stop/Reset, forces and draft mismatch. Observer/history on/off must preserve the entire accepted trace and all actual plant values/transforms. Check declared feedback against actual controller getters, not commands or fixture-injected point values. Read-only status must distinguish LIVE INPUT, LIVE FEEDBACK, AUTHORED ONLY and unavailable; genuine motion is not proof of material transport. Final exact binary needs real editor UI renders and connection removal feedback inspection; no acceptance is claimed by this audit.

## Exact implementation/review surfaces

- `src/Catalog/AssetDefinition.cs`, `assets/catalog/production.catalog.json`: actual connector/signal metadata; no implicit endpoint ownership or connector direction.
- `src/Workspace/WorkspaceDocument.cs`, `WorkspaceRepository.cs`, `WorkspaceProjectBundleRepository.cs`: graph and joint persistence; proposed declaration/contract schema needs explicit review, readable invalid ladder preservation remains.
- `src/Workspace/WorkspaceSignalRuntimeAdapter.cs`, `src/App/Main.WorkspaceMappingLifecycle.cs`, `src/App/Main.cs` (`CreateConnectorLink`, `CreateSignalLink`, `ApplySignalMappings`, workspace restore): routing and ownership lifecycle.
- `src/App/ConveyorController.cs`: actual ramp, running threshold, drums/UV and reset projection; prevent a second physics clock for opted-in instances.
- `src/Scenes/SceneSimulationRuntime.cs`: owner/type dictionaries, `IsPcOwnedPoint`, PC sampling, PLC commits. Generic feedback publication does not currently exist; do not repurpose PLC commit methods for PC feedback.
- `src/Workspace/WorkspaceFiniteBeltPreset.cs`, `WorkspaceFiniteRollerPreset.cs`, `src/Scenes/SceneSimulationRuntime.WorkspaceBeltPreset.cs`, `SceneSimulationRuntime.WorkspaceRollerPreset.cs`, `SceneComposer.WorkspaceBeltPreset.cs`, `SceneComposer.WorkspaceRollerPreset.cs`: reuse concepts only; their fixed relation/graph locks must stay intact.
- `src/App/Main.WorkspaceMappingDetachReview.cs`: current autonomous typed-detach regression; retain it as separate historical proof. `Main.ScanInspectionReview.cs`: accepted-clock/immutable-history fixture patterns; extend narrowly after approved contracts.

Main risk is scope confusion: a convenient two-endpoint visual graph can bypass ownership, units and clocks. The selected contract deliberately resolves those prerequisites on one real belt before promising sensor geometry or material handoff. This recommendation is ready for the department meeting, not implementation authorization or product qualification.
