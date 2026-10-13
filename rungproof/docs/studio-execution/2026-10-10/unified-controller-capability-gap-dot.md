# Dot unified controller capability and gap audit

Read-only source audit after Matt explicitly resumed unified demos, lessons and build-own. No source edit, build, capture or UI operation performed. Earlier STOP was respected until this renewed instruction. This audit does not revive final77 visual approval.

## Existing capabilities

Main.cs RunActiveController (~1183) compiles and validates the current scene editor on first Run. SimulatorShell.cs TryBuildCurrentLadderProgram (~9291) combines LadderCompiler, empty-network rejection and SceneIoBindingValidator. EnableVirtualControllerProgram (~923) constructs a compiled VirtualControllerSession, attaches output projection and sets the scene to controller clock ownership. SceneRequiresControllerClock (~954) already selects controller-owned clock for every nongallery scene. SceneIoImageMapper maps typed input/output aliases rather than guessing learner variable names.

VirtualControllerSession.Advance (~103) samples input image, executes scan, projects outputs and advances plant by scan period. ScanCompleted carries copied sampled/effective/output images. Plant-generated feedback is sampled on the following scan; current command images must not be relabeled as measured feedback. Main CommitVirtualControllerSnapshot (~1064) handles immediate stop/force/reset projection separately.

SimulatorShell LoadSceneLadderDraft (~411) restores exact per-scene document/history/saved baseline/path or creates authored demo/lesson templates and tag-ready empty exercises. These in-memory drafts are not durable project saves. Both compiler-invalid drafts and valid wrong logic need preservation.

ChallengeSessionCoordinator.cs uses a frozen program and injected runner on Task.Run. ChallengeTestRunner.cs creates isolated case plants/controllers and bounded traces; the live ScanCompleted handler in Main.ChallengeLabels.cs only caches the live observation. Grading should remain a read-only observer or isolated test, never a corrective plant/output writer. Existing runner is explicitly conveyor-only with seven cases, not a universal campaign evaluator.

## Gaps requiring a bounded change

1. Normal Run rebuilds current editor only when no controller is loaded. Editing to wrong or invalid code can leave the prior loaded program active. Invalid project-open explicitly retains the prior controller (SimulatorShell.cs ~4333). Monitor mismatch disclosure is useful but does not satisfy Matt's requested invalid-current-code no execution. Establish an explicit dirty/load/run policy that retires stale execution, compiles the exact current draft and never substitutes authored solutions.
2. Verify+Load uses EnableVirtualControllerProgram(program), which resets the plant by default; first Run uses resetScene:false. Choose explicit load/restart semantics and show them to the learner. A load must not silently repair feedback or discard learner work.
3. Advance invokes output projection and advancePlant before verifying accepted scanNumber+1. Stopped Runtime.Scan returns an unchanged snapshot (~301); SceneSimulationRuntime.AdvanceSimulation clears pending pulse bookkeeping before its playback guard (~251-264). A strict accepted-scan boundary should exclude rejected/stopped side effects and report accepted scans only.
4. Generic EquipmentMotionController shafts still tick in their own Godot physics callback, though run-gated by scene playback. Plant elapsed time and visual rotation are not universally identical accepted-scan deltas. Separate decorative animation from deterministic equipment motion and drive supported functional motion from accepted plant ticks.
5. Stop clears commands, pulses and scan backlog. It is not a state-preserving Pause. Session has no general paused single-scan API or user-accessible immutable live scan ring history. Existing challenge trace and editor Undo are different histories.
6. Build-own needs explicit scene bindings and validated typed contracts. Empty tag-ready exercise creation already exists; arbitrary scene assembly is not thereby proven to have a deterministic plant model or assessment contract.

## Next bounded milestone

Use one existing conveyor lesson as the tracer slice. Unify demo/template/learner loading into one exact-current-draft compile/binding path, with valid incorrect code genuinely driving incorrect behavior and invalid code producing zero accepted scans/output commits/plant movement. Preserve authored reference as an optional explicit template, never an automatic replacement.

Add state-preserving Pause, one accepted scan while paused, and bounded immutable live history recording raw/effective inputs, commands, post-plant state and next sampled feedback with scan/time identities. Keep Stop/Reset semantics distinct. Gate every plant tick and functional equipment animation at the accepted scan boundary. The assessment reads the same copied observations and cannot change behavior.

Focused acceptance: two different valid solutions; valid wrong solution visibly fails without correction; invalid edited draft cannot continue old controller; alias validation fails closed; paused elapsed time performs zero scans; one-step performs exactly one scan and one plant tick; pulse consumed once; Stop drops pending pulse/backlog; Reset restores typed controller and plant home; history cannot mutate runtime; evaluator enabled/disabled produces identical simulation traces; save/load retains invalid draft and per-scene editor history boundary. Compare one large elapsed call versus equivalent scan-sized calls. Expand scene support only after this slice proves the shared contract, with each plant's commanded versus generated feedback ownership declared.

This is a capability/gap proposal grounded in source, not fresh runtime qualification or permission to publish/live-PLC work.