### Spec Compliance

- ✅ Provisional source compliance for Task2 fix round 2: assessed-count/elapsed presentation and fixed physical-root layout identity address the specified defects (`src/App/SimulatorShell.LessonObservedTrial.cs:16,33-38`; `src/App/Main.RepeatCycleObservedTrial.cs:64,129,136-185`).
- ⚠️ Approval is source-only until root establishes GREEN on this exact source/DLL. The retained v3 RED proves the pre-fix failures; it does not prove these changes pass. Full Task3 causal/invariance and rendered acceptance remain separate gates.

### Strengths

- Null presentation now says unavailable rather than inventing zero returns, and accepted count remains nullable. Elapsed time comes directly from accepted reducer evidence with invariant formatting (`src/App/SimulatorShell.LessonObservedTrial.cs:16,36-37`).
- Frozen assessment is limited to null, historical, NeedsAttention, Incomplete, and Unavailable states; current Observed remains updating (`src/App/SimulatorShell.LessonObservedTrial.cs:34-37`). This agrees with the unchanged reducer's continued accepted elapsed/hold updates after Observed (`src/Lessons/RepeatCycleObservedTrial.cs:74,80,102-108`).
- The same layout helper serves epoch capture, accepted observation, and presentation provenance (`src/App/Main.RepeatCycleObservedTrial.cs:64,129,182`). It hashes authored workspace/source identity and fixed root global transforms, so moving the actual machine root changes accepted identity even when no authored-editor operation occurs (`:148-175`).
- Explicit origin/basis numeric components avoid relying on Godot transform JSON field behavior. Equipment ordering and nested config-object keys are stable; actual root lookup does not include engine clone names (`src/App/Main.RepeatCycleObservedTrial.cs:140-175`).
- Only composition and declared equipment roots are read; modeled KIN descendants are not traversed. Missing/invalid/queued-for-deletion roots produce a sentinel without dereferencing freed objects (`src/App/Main.RepeatCycleObservedTrial.cs:155-170`). A previously present root becoming missing changes its fingerprint.
- Presentation checks semantic draft and physical layout before saying the accepted identity matches; it retains the assessed epoch/fingerprint when historical (`src/App/Main.RepeatCycleObservedTrial.cs:178-185`). No added mutation of outputs, plant, controller or reducer was found in the fix.

### Issues

#### Critical (Must Fix)

- None found in the scoped source changes.

#### Important (Should Fix)

- None found in the scoped source changes.

#### Minor (Nice to Have)

- No separate change requested. Source naming helpers differ generally, but all six declared Lab 4.3 equipment IDs contain only letters, digits and underscores; the new lookup matches the actual composer names for this approved lesson scope (`src/Scenes/SceneComposer.cs:128-138,904`; `src/App/Main.cs:5056-5059`; `scenes/migrated/lab-4-03-repeat-cycle-counter.scene.json`). Reassess that contract if this helper is ever generalized.

### Assessment

**Task quality:** Provisionally approved for source review; exact-source native GREEN and rendered acceptance pending.

**Reasoning:** The changes make assessment provenance explicit and include the actual fixed physical geometry in the execution identity while excluding modeled motion. The implementation is read-only and narrowly scoped; runtime stability and fit must be established by the pending native and visual gates.

### Checks and Evidence Boundaries

- Compared both specified before snapshots with the two corresponding current source files and read the latest Task2 report entry plus the authoritative `rungproof/docs/studio-execution/2026-10-10/repeat-cycle-studio-implementation-plan.md` lifecycle/provenance constraints. No production writes, builds, engine launches, tests or subagents.
- Named risk: lookup fingerprints a missing sentinel because source naming differs. Inspected composer equipment root naming, Main SafeNodeName, and this lesson's six equipment IDs; no mismatch in current lesson scope.
- Named risk: modeled motion pollutes the identity. Inspected the helper's root-only access, repeat-cycle composition, and reducer continuous Observed behavior; no descendant traversal introduced.
- Named risk: asserted layout mutation is merely a test-only draft mutation. Inspected `src/App/Main.RepeatCycleLearnerReview.cs:72-76`: QA moves actual `machine_0.Position`, then accepts a scan and requires historical Unavailable. This is the actual fixed physical root covered by the ruling.
- Read retained `.tools/studio-verification/repeat-cycle-learner-native-v3-red.log`: six assessed/elapsed card failures at 20/40/100 ms with history on/off, three actual-root layout retirement failures, final FAIL failures=9; ERROR/Exception filter returned no matches. Root identifies this pre-fix DLL as `9C0609BB9F7298DA1FF936DC2EF95A11473DDB5424BBC00C44616CDD024BF40F` and reports 239 passing checks. No GREEN was inferred or rerun.
