# Repeat-cycle passive observation department proposal

Read-only proposal after the typed and connection milestones. No lesson implementation, build or engine execution in this audit. Fresh Lab4.3 remains tag-only; no new preset, authored default, hardware claim or evaluator intervention is proposed.

## Existing authoritative offline contract

- `rungproof-next/scenes/migrated/lab-4-03-repeat-cycle-counter.scene.json:130-192` declares PC request/enable plus generated home/busy/done/REAL position; PLC active/complete/count. It explicitly labels enable a manual process permissive and position a modeled percentage.
- `src/Scenes/RepeatCyclePlantModel.cs:6-57` prescribes 100mm feed1s, dwell0.5s, return1s. Its only process command is `cycle_active`; enable gating is learner responsibility. Removing command holds a partial stroke. Complete/Done persists until an accepted low command; a held high cannot start another stroke.
- `src/Scenes/SceneSimulationRuntime.RepeatCycle.cs:22-34` validates exact owners/types;45-46 disables autonomous adapters;52 advances the model;59-62 projects head/spindle and generated feedback. The observer must read this state, not assign it.
- `src/VirtualController/VirtualControllerSession.cs:165-189` samples, executes, commits, advances plant, records post-plant image, then emits ScanCompleted. Thus newly generated completion on scanN becomes controller input onN+1.
- `src/App/SimulatorShell.cs:440-451` uses SceneLadderProject for a fresh non-authored lab; cached custom drafts retain their exact document. Do not add Lab4.3 to the authored factory to make QA easier.
- `src/App/Main.RepeatCycleReview.cs:73-108` contains a diagnostic reference ladder but directly enables the compiled program. Its native geometry/Stop/Reset assertions are reusable evidence patterns, not current-editor learner acceptance.
- `src/App/Main.ScanInspection.cs:46-49` excludes Lab4.3 from product Pause/Step support. Keep it unsupported unless a separate meeting explicitly ratifies that bounded extension.

These local implementations are the authority for the named offline lesson. Counter edge semantics must be verified against the actual offline runtime/compiler and regression tests. No particular vendor PLC/CNC behavior, spindle specification, material removal, physical feedback sensor, safety function or commissioning is inferred.

## Bounded objective and observation contract

From Reset-home, one enabled momentary START produces exactly three genuine modeled dry strokes, then count3/completion and a stationary hold without a fourth departure. Display actual modeled completion events separately from the learner's claimed PLC batch_count.

Capture immutable phase, HomeFraction and SpindleRadians from the existing private repeat model through an additive read-only getter. Record declaration-resolved sampled/effective inputs and applied outputs from each accepted scan. Do not infer physical phase from a toggled PC tag, UI lamp, manually assigned count, named internal counter or wall time.

For every stroke require observed Feed -> Dwell -> Return -> Complete, departure from home and actual return. At completion scanN require pre-input cycle_done false, post-model Done true and home true. At exactlyN+1 require sampled/effective cycle_done true and the mapped PLC count increment exactly once to the number of actual completed returns. Permit the expected one-scan lag atN; an ahead count or a mismatch atN+1 is concrete NeedsAttention. Verify next-scan low command acknowledges Done rather than allowing the observer to clear it.

After the third return, exactlyN+1 must publish count3, completiontrue and activefalse. Require at least0.5 accepted seconds at unchanged home/spindle with no fourth departure. A claimed completion after two returns or motion after observation is NeedsAttention. A held active command that completes one stroke and remains stuck yields Incomplete after20 accepted seconds; it cannot pass merely because a Done input is true.

Identity includes scene/plant version, exact semantic program/bindings, layout and Reset epoch. Non-home start, forced input, missing/ambiguous/wrong-type binding, draft mismatch, discontinuous accepted scan/time or scene replacement makes observation Unavailable. Reset starts a fresh epoch. Stop before completion is Incomplete; established completion retained on Stop is labeled historical, not a new pass. Enable loss should be a separate named interlock scenario, not silently assumed part of every learner algorithm. Observer enabled/disabled must produce identical accepted controller/plant traces.

Hints are optional conceptual prose: count completed events, retain the request through movement, acknowledge completion with a low command, stop at the target. They never populate or repair ladder code or command outputs.

## Minimal files and qualification

1. New `src/Lessons/RepeatCycleObservedTrial.cs` immutable lesson-specific observer and copied observation/view state; new isolated `tests/RepeatCycleObservedTrial` project. Pure tests cover fabricated/early completion rejection, exactN+1 timing, identity retirement, timeout and stable hold.
2. Additive getter only in existing `SceneSimulationRuntime.RepeatCycle.cs`; no new plant or integration algorithm.
3. New `Main.RepeatCycleObservedTrial.cs` for lifecycle, resolved accepted image capture and read-only event subscription; narrow Main Reset/Stop/Open/draft/force hooks. Use existing finite observer subscription generation guards as a pattern without migrating their contracts.
4. New `SimulatorShell.LessonObservedTrial.cs` for one immutable card, optional hint and historical/current reason; hide on other76 scenes. The shell holds no mutable controller/plant handle.
5. New `Main.RepeatCycleLearnerReview.cs` for actual disk Open -> joint save -> navigate away -> exact reopen -> current-editor Run at20/40/100ms. Diagnostic intended projects are explicit QA fixtures and never fresh defaults.

Native cases: intended3 strokes; valid wrong CTU-on-request (ahead claim), preset2 (premature completion), held active ignoring Done (one stroke then stall); invalid-current midfeed freeze with no prior-program fallback; exact intended/wrong/invalid custom joint reopen; Stop/resume/Reset actual head/spindle/count readout; next-scan generated completion history and observer-on/off behavioral equivalence. Native actual renders of scene/split/card/hint/history/invalid states require inspection. No universal77-lesson correctness claim follows.

Related earlier design: `next-lesson-studio-dot-design.md`; inventory boundary: `whole-product-lesson-coverage-dot.md`. Those are design/source scopes, not replacement runtime acceptance.
