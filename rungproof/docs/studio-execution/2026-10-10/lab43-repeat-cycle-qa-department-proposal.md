# Lab 4.3 repeat-cycle passive objective: QA department proposal

Status: source-inspected proposal, no implementation, build, engine run or new qualification. Scope is only `lab-4-03-repeat-cycle-counter`, offline current editor execution. Keep its fresh starter tag-only and Pause/Step unsupported. Do not generalize this evidence to all lessons, machinery or live PLCs.

## Actual source contracts

`src/App/Main.RepeatCycleReview.cs` already builds an explicit reference with `LadderEditorDocument`, compiles it and directly enables it. Its 20 ms manual Main physics ticks cover three actual returns, acknowledgement, enable loss, Stop/resume, Reset, native head/spindle and sampled geometry. It writes a project JSON but does not open that file through the current editor/joint workspace workflow. It does not establish wrong/invalid saved-current-draft behavior at 40/100 ms or passive-observer invariance.

`scenes/migrated/lab-4-03-repeat-cycle-counter.scene.json` declares PC BOOL request/enable/home/busy/done, PC REAL `head_position` (percent, not metres), PLC BOOL `cycle_active`/`cycle_complete`, PLC DINT `batch_count`. Actions are `pulse-cycle_request` and `toggle-machine_enabled`. Rules and edgeRules are empty. `cycle_count_complete` is absent. The batch count and batch completion are controller outputs, not independent proof of physical completion.

`src/Scenes/RepeatCyclePlantModel.cs` prescribes 0.1 m travel, 1 s feed, 0.5 s dwell, 1 s return and 3200 rpm spindle. A low command holds a partial stroke; at completion a low command acknowledges Done and returns phase to Idle. A high held command cannot restart a completed stroke. Enable is a learner-program input; the plant directly obeys `cycle_active`, so QA must not assume an independent hardware enable interlock.

`src/Scenes/SceneSimulationRuntime.RepeatCycle.cs` advances that model from the accepted plant callback, projects native head normalized position and spindle basis, and disables imported machine controller physics. Native nodes are `machine_0/KIN_repeat_cycle_head` and descendant `KIN_spindle`. Count readout is `batch_count_display/NumericReadout`; indicators are `indicator_2` (command amber) and `indicator_4` (completion green). Read exact display text and indicator API before implementing assertions, rather than assume a visual-state accessor.

`SimulatorShell.cs` uses `SceneLadderProject.Create(...)` for this scene: `AuthoredLessonLadderPrograms.cs` has no Lab 4.3 entry. That factory adds declared tags/watches and no rungs. Assert a genuinely fresh scene draft has zero rungs before creating QA drafts. `Main.ScanInspection.cs` currently excludes this scene from Pause eligibility; preserve that contract. The separate gantry inspection hold is an existing diagnostic facility, not learner Pause support.

## Implementable draft assembly

Copy the explicit assembly in `VerifyRepeatCycleWorkflow`, parameterize period only, and keep it confined to QA. Existing APIs are `ResetProject`, `AddTag`, `AddCounterRung`, `AddRung`, `AddContact`, `AddParallelBranch`, `AddNumericOperationRung`, and `LadderEditorProjectJson.Save/Load`.

Declare the nine scene tags with the roles/types above. Add Counter memory `completed_cycles`, BOOL memory `batch_active`, and BOOL memory `publish_count` initially true. Rung order matters:

1. CTU `completed_cycles`, preset 3, contact `cycle_done` normally open.
2. Coil `batch_active`: `(cycle_request AND machine_enabled AND NOT completed_cycles.DN) OR (batch_active AND machine_enabled AND NOT completed_cycles.DN)`.
3. Coil `cycle_active`: `batch_active AND NOT cycle_done`.
4. Coil `cycle_complete`: `completed_cycles.DN`.
5. MOVE `completed_cycles.ACC` to `batch_count`, enabled by `publish_count`.

Produce distinct compiler-valid wrong drafts by single edits, retaining all other rungs:

| Draft | Exact edit | Required actual consequence |
|---|---|---|
| Intended | None | One enabled request yields three physical returns, three PLC increments, COUNT 3, completion true and command off |
| Request-count wrong | Rung 0 contact `cycle_request` instead of `cycle_done` | Single request gives ACC/COUNT 1 before any completed return; physical cycles continue because target is not reached. Bound at fourth actual return; never relabel displayed count as measured completion |
| Preset-two wrong | Rung 0 `CounterPreset = 2` | Two actual returns, ACC/COUNT 2, command off and completion true; observer identifies unmet three-return objective without editing code |
| Held-active wrong | Remove only rung 2 normally-closed `cycle_done` contact | First return gives one increment; command remains high, Done stays high, native head/spindle hold and no second stroke. No fabricated third completion |
| Invalid current | Add rung targeting undeclared `missing_current_output` | Actual current Run blocked; no stale reference execution; exact invalid JSON survives joint save/open |

Verify every wrong draft through actual editor compilation against scene declarations, not only `LadderCompiler.Compile` without the scene contract. Invalid draft is an additional negative, never a substitute for valid wrong behavior.

## Native workflow matrix

Each row above runs at 20, 40 and 100 ms from an actual saved joint file using existing `WorkspaceProjectBundleRepository.Save`, `LoadWorkspaceFromPath`, `CaptureIntegratedLadderDraft`, `RestoreIntegratedLadderDraft` and `RunEditorDraftController`. Use an ordinary workspace document without presets/DriveConnection. Preserve exact source, period, tags, watches, rung JSON and saved path/baseline. Reopen is fresh stopped/home physical state; it does not restore an in-flight stroke. Do not install the audit reference program behind the editor.

Before enable, one actual request cannot move/count under the intended draft. Enable alone cannot start. Then one request starts the batch. Sample each accepted callback and actual points/native poses, not elapsed walltime alone. Bound by accepted time (e.g. 12 s for intended/two/held, 15 s for fourth-return request-count), with separate walltime timeout for engine progress.

For each physical completion at scan N: pre-scan sampled Done is false, post-plant Done becomes true and native head is home; count must not increment merely because that post-plant value became true. At N+1 sampled Done is true, CTU increments exactly once, output/count readout updates, intended command becomes false, and post-plant Done clears by acknowledgement. At N+2 the intended command can start the next stroke only if counter is below preset. Final third completion turns batch_active/cycle_active off. Compare `VirtualControllerScanCompleted` sampled/effective/applied images plus snapshot counter, runtime points, full native transforms and readout. Assert busy holds during an interrupted partial stroke; do not infer it means moving.

Stop mid-feed must hold head/spindle and accepted clock immediately, clear output image/display count to zero while retaining CTU memory, and require current draft Run for resumption. Reset must restore captured native head and spindle home, disabled enable, no Done/busy, count zero and scan/time zero. Invalid edit during mid-feed must stop stale execution and hold accepted clock/native poses; saved invalid joint reopen preserves exact unresolved draft and Run remains blocked. Also assert learner Pause and Step reject without changing live state/history.

## Passive objective and invariance

No repeat-cycle observer API exists yet. Proposed observer consumes copied accepted sampled/effective/applied inputs and post-plant PC feedback; it must not own a worker clock, outputs, tags or reset the learner counter. Objective evidence is independently counted Done rising transitions plus command/acknowledgement causality; controller ACC/COUNT and completion are compared against that evidence. Force use, missing bindings/current-draft mismatch or unsupported execution must make assessment unavailable rather than count a fabricated success. Reporting a wrong consequence must not rewrite or reload the program.

Run identical fresh saved drafts/actions with proposed observer enabled/disabled at all periods. Capture the entire accepted callback payload, snapshots/counters, runtime point images, native head/spindle/all Node3D poses and display state for every accepted scan; compare strict traces and final state. History recording is a separate toggle, not equivalent to observer on/off. Preserve stable structural identity only for proven Godot autogenerated names, retaining authored names and raw diagnostic paths. Stop at the same accepted scan boundary with the existing diagnostic held clock or deterministic manual accepted ticks; report this diagnostic cadence separately from ordinary real-frame Run evidence.

## Ownership and remaining checks

Proposed new QA partial `Main.RepeatCycleLearnerReview.cs` owns fixture assembly/disk lifecycle/trace assertions; root owns flag/hooks and native build/run. Pure observer/model owner adds bounded reducer tests for completion-edge causality, repeated Done, acknowledgement, preset-two, request-count and held-active without changing plant semantics. Shell owner adds read-only objective presentation only after observer contract is agreed. Keep existing reference/geometry audit as separate evidence.

Implementation blockers are concrete: agree observer copied post-plant contract and lifecycle epochs; inspect actual indicator accessor/readout format; ensure deterministic callback collection happens after plant projection (session publishes ScanCompleted after advancePlant, with post-plant history available only when recording enabled). Read fresh factory/current draft and saved-baseline APIs in the eventual QA fixture. None requires another preset, generic observer, Pause eligibility extension or transport/PLC profile. Native evidence and actual rendered objective readability remain unverified until root runs the combined binary.
