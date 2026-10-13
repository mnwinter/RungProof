# Dot Pause / one accepted scan / history design

Read-only source-grounded design, not implementation or approval of rendered UI. Scope: existing offline authored demos and accepted finite workspaces; no new preset, external transport or material propagation.

## Learner contract

Run validates current draft and resumes unchanged controller memory; Pause suspends accepted scheduling without removing commands or changing plant speed/pose. Scan advances exactly one accepted cycle while remaining paused. Stop retains its existing immediate command-clearing/freeze semantics; Reset restores homes/memory and clears pending inputs. These controls must visibly distinguish PAUSED from STOPPED and show accepted scan/time even when held.

Pause cannot reuse VirtualControllerSession.Stop, Main.StopActiveController or SceneRuntime.SetControllerPlaybackRunning(false): source currently clears pulse backlog/accumulator and stops roller transport there. Keep accepted runtime Running and add explicit session scheduling pause state, or an equivalent separate exact contract. UI must not infer executing walltime from that runtime snapshot Running value. Decorative motion should remain held while paused without altering functional plant state.

Scan uses the SAME input sample -> accepted runtime scan -> output commit -> one plant period -> pulse cleanup -> ScanCompleted path as ordinary execution. Refactor only this shared seam; no second clock, direct plant tick or transient Run/Stop. Preserve pre-Pause fractional accumulator/backlog; paused walltime contributes zero. One Step neither drains nor consumes accumulated backlog, and Resume must not accrue paused walltime. Define repeat-click/reentrancy guard so one intent accepts at most one cycle and cancelled/mismatched scan never commits output or advances plant.

Ordinary PC input actions may be queued while paused and are sampled on Step; momentary pulse lasts exactly one accepted scan. Maintain command-vs-feedback distinction. Force changes update current pending/effective state, not old history. Unsupported direct process/output actions must not mutate plant under a misleading frozen label. Engineering must inventory actual action routing before promising universally frozen editable input actions.

## Truthful bounded history

VirtualControllerScanCompleted already copies primitive SampledInputs/EffectiveInputs/Outputs and diagnostics after plant advance. Its SampledInputs includes operator input overlays before forcing; do not call it raw physical input. Outputs are accepted/applied controller image, distinct from copied post-plant PC/SIM points. The post-plant beam produced at scan N becomes sampled input at scan N+1; never label it already accepted as input N. Include program/layout/reset epoch, scan/time/period, accepted diagnostics and immutable copied post-plant point types. Do not retain live SceneRuntime.Points aliases, nested mutable aggregates or controller references.

Use an explicit bounded ring (root-ratified128 rows; aggregate leaf handling remains explicit). Display oldest/newest retained scan and dropped-row count. Selecting an old row is inspection only, labelled HISTORICAL; current live monitor/device-reference navigation must not silently switch to old Elements or plant pose. No fake causal inference or replay/rewind claim. Default follow-latest unless user selects historical row; do not jump away on new scan. No exported persistent trace feature implied.

New controller/program/layout/reset retires old trace epoch, with clear new-empty presentation or explicitly separate historical epoch. Watch-only edits need existing semantic identity treatment; compiler-invalid/changed-current drafts disable Resume/Step until valid matching load, preserving honest historical rows and physical stopped/paused state according to existing invalid-run contract. Navigation unloads subscriptions and rejects stale callbacks.

## External boundaries and accessibility

Enable Pause/Scan/history only for an accepted built-in controller. Existing ExternalScenePlayback.Pause stops scene playback while external exchange continues; it is not PLC Pause and must keep current semantics/labels. Step disabled with explicit reason for external PLC, no program, invalid/mismatched draft, challenge worker activity or unsupported runtime. Do not contact a bridge to make Step appear supported.

Expose buttons/menu intents with stable names, keyboard focus and noncolor state labels; root defines shortcuts avoiding editor collisions. Show selected history versus current accepted scan plainly. Compact controls should not consume rung canvas or hide objective/error panels. Required actual renders: paused timer, carried Demo5 carton, coasting roller, selected-old history and invalid-current blocked scan.

## Required evidence / blockers before PASS

1. Pure session tests prove paused walltime0, one scan/period/plant, retained backlog and pulse consumption; Stop/Reset unchanged; invalid/reentrant sampling rejects effects.
2. Native real editor workflows preserve timer/counter/FB memory, Demo5 carry/axes, roller coast speed/phase and finite belt pose; correct/wrong/invalid drafts still behave honestly.
3. Copied history cannot mutate controller/plant; selected old row cannot change live reference/pose; post-plant N -> sampled N+1 trace verified. Observer enabled/history enabled versus disabled traces identical.
4. External/missing/current-mismatch paths reject Step with no transport; stale epoch callbacks and navigation clear safely.
5. Same-binary actual renders show PAUSED, STEP result, HISTORICAL selection and invalid diagnostic without overlap. Tests alone do not qualify UI.

Studio design risks to resolve: all action routing while paused; full primitive aggregate/text point-copy schema; history cap/overflow presentation; exact pause-state UI versus accepted Running snapshot; challenge isolated worker arbitration; safe same-session Resume validation. No general human approval hold is introduced; material human gate remains separate.

## Meeting ratification
Root ratified128 rows, retained fractional/backlog and exact Step bypass. Dot concurs Run becomes RESUME only while paused, separate PAUSE/STEP/SCAN HISTORY intents and prominent PAUSED override without altering accepted runtime state. External/challenge/gallery Step unsupported. Before code, classify paused actions: allow queued PC inputs only; reject direct plant mutation (load carton, placement, reset actions) or route explicit lifecycle exit/epoch retirement. History callbacks require current session/epoch guards; history selection never changes live monitor or reference navigation. This last action-routing contract must be concrete, not guessed from action label.

