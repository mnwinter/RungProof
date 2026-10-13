### Spec Compliance

- ❌ Issues found: valid joint Open loses its staged measured-reset epoch before the first current-editor Run; `src/App/Main.RepeatCycleObservedTrial.cs:29,42` and `src/App/Main.cs:4074,4093`.
- ⚠️ Native and rendered acceptance remain unverified: actual disk Open/save/navigate/reopen, invalid reopen, N/N+1 causal counts, partial-stroke Stop/resume, force retirement, observer on/off complete-trace invariance, history on/off, actual head/spindle/readout/indicator agreement, retained-history selection, and scene/split/card/hint/history/invalid renders at 1600 and 1280. Compiler PASS FF890961 is compiler evidence only (`task-2-report.md:3,29`).

### Strengths

- Accepted observation is attached only to ScanCompleted; copied pre/post capture surrounds the existing single plant advance, and the pair checks session, runtime, generation and scan before use (`src/App/Main.RepeatCycleObservedTrial.cs:73-80,91-131`; `src/App/Main.WorkspaceDriveConnection.cs:153-155`). Focused pipeline inspection confirms runtime.Scan precedes output commit and plant advance, and ScanCompleted follows them independently of HistoryRecordingEnabled (`src/VirtualController/VirtualControllerSession.cs:173-188`).
- Current binding resolution checks unique declared point ownership/type and program alias roles; actual output points are independently copied and compared with accepted PLC outputs (`src/App/Main.RepeatCycleObservedTrial.cs:46-70,103-112,123-127`). Signal-image construction privately copies dictionaries (`src/VirtualController/VirtualControllerScanCompleted.cs:7-14`).
- Stop does not construct a replacement observer; unchanged editor Run bypasses program replacement and the reducer preserves historical evidence (`src/App/Main.RepeatCycleObservedTrial.cs:88-90`; `src/App/Main.cs:1357-1362`; `src/Lessons/RepeatCycleObservedTrial.cs:37-38,148`). Semantic draft comparison omits watch metadata (`src/App/Main.ScanInspection.cs:50-56`).
- The lesson card separates actual returns from nullable accepted count, exposes provenance and a conceptual hint, and states Pause/Step and history boundaries; scene visibility remains limited to the intended offline lesson (`src/App/SimulatorShell.LessonObservedTrial.cs:9-37`; `src/App/Main.RepeatCycleObservedTrial.cs:22,135-140`).

### Issues

#### Critical (Must Fix)

- None identified in this task-scoped source review.

#### Important (Should Fix)

- **Joint reopen discards the pending reset epoch.** `src/App/Main.RepeatCycleObservedTrial.cs:29` clears `_repeatCyclePendingReset` for every retirement, while `:42` requires it before binding. `LoadWorkspaceFromPath` first invokes `AddMigratedScene` (`src/App/Main.cs:4074`), whose new attachment hook stages the measured-reset epoch (`:4249`), then unconditionally invokes `DisableVirtualController` (`:4093`). The new disable hook calls retirement (`:1091`) even though the freshly composed scene has no controller. Restoring the exact saved draft (`:4094`) does not restore the pending epoch. Consequently the valid editor Run loads with resetScene:false and `BindRepeatCycleTrial` returns without a trial; a valid joint reopen cannot become Ready/Running until an extra Reset. This directly violates the mandated fresh joint-reopen epoch. Stage/reset the observer after the joint loader finishes detaching and restoring the draft/layout, or preserve pending state only for this known fresh-attachment transaction. Do not preserve it for ordinary program replacement. Add the planned native assertion that a valid joint reopen followed by current-editor Run obtains a new epoch without an additional Reset; invalid reopen must remain Unavailable.

#### Minor (Nice to Have)

- None requiring a separate change.

### Assessment

**Task quality:** Needs fixes.

**Reasoning:** Accepted-scan capture and read-only presentation are well separated from controller outputs, but the integration misses a concrete existing loader lifecycle edge. Fix that edge before native acceptance; the reported build cannot establish the missing lifecycle behavior.

### Focused checks and evidence boundaries

- Read the supplied scoped review package and task brief/report; no Git operations, app/engine launch, build, suite rerun or production edit performed.
- Named risk: staged reset survives existing controller/scene/joint-load ordering. Inspected only Main Enable/Run/Stop/LoadWorkspaceFromPath paths; found the Important issue above.
- Named risk: pair scan stamp and copied image provenance are valid and independent of history. Inspected accepted session pipeline, signal-image copy constructor and mapped-input helper calls; verified ordering/copying statically.
- Named risk: unchanged Run and watch-only edits cannot revive historical evidence. Inspected existing semantic comparison and reducer retirement/Stop entry guards; source contract preserves historical state.
- Named risk: layout fingerprint accidentally contains live controller/watch state. Inspected CreateWorkspaceDocument (`src/App/Main.cs:4020-4034`); its serialized fields contain workspace scene/placements/links/groups/template/drive state, not the current ladder/watch image.
- The supplied report explicitly defers behavioral/native/render evidence; those are cross-task gates, not inferred passing results. Root-reported pure checks and compiler output were not rerun.
