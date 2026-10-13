# Task 1 report: passive repeat-cycle observer

Status: IMPLEMENTED_AND_PURE_TESTS_PASS. Independent task review and root integration remain required; no application build, Godot launch or rendered/native acceptance performed by this task.

## Files and scope

- New `rungproof/rungproof-next/src/Lessons/RepeatCycleObservedTrial.cs`: exact frozen immutable API in `RungProof.Next.Lessons`, passive reducer with no plant/controller handles or mutation capability.
- New `rungproof/rungproof-next/tests/RepeatCycleObservedTrial/Tests.csproj` and `Program.cs`: isolated net10.0 executable, references actual plant model and actual immutable signal-image declaration directly.
- Additive `CaptureRepeatCyclePlantState()` getter only in `src/Scenes/SceneSimulationRuntime.RepeatCycle.cs`. Returns a fresh copied `RepeatCyclePlantImage?`; absent plant returns null. Existing dirty partial preserved.
- No Main changes, no other product files, no commits/push/cleanup/subagents.

## Verification and RED/GREEN evidence

Initial PATH `dotnet run` was infrastructure-only failure: no SDK installed in PATH. Root supplied the existing `.tools/dotnet` SDK. All actual functional runs use:

```powershell
$env:DOTNET_ROOT=(Resolve-Path .tools/dotnet).Path
& .tools/dotnet/dotnet.exe run --project tests/RepeatCycleObservedTrial/Tests.csproj
```

Working directory: `C:/Users/mattn/Documents/Codex/Worktrees/rungproof-studio-20261010/rungproof/rungproof-next`.

Functional RED: with complete frozen API and deliberately inert reducer, compilation succeeded; executable exited 1 with `System.Exception: trigger interval counts` at test line 45. Reducer then implemented; first GREEN was `PASS 38 checks`.

Expanded protocol matrix passed at 20/40/100 ms. Self-review found two further defects and added regression tests before fixing each:

1. Functional RED `observed success survives later stationary scans past deadline`: a trial that already succeeded before the deadline must retain success while later stationary scans continue. Corrected timeout guard, then GREEN.
2. Functional RED `retired scan cannot replace last accepted count`: skipped/duplicate/invalid-time observations must retain the previous historical count, rather than adopting the rejected scan's value. Moved accepted-count update after scan/plant continuity validation, then GREEN.

Final command result: exit 0, `PASS 137 checks`. Warnings-as-errors is enabled. Framework restore/build outputs remain local to `tests/RepeatCycleObservedTrial/obj` and `bin`; no root app build. `git diff --check` scoped to leased paths returned exit 0 (only Git's LF/CRLF informational warning on existing runtime partial).

## Requirements coverage

At all three scan periods, test actual model-generated phase chains, actual Complete edges/return totals, preceding count at N and single increase at N+1, 25/13/5 subsequent stationary intervals, acknowledgement interval excluded, held START failure, count early/missing/double, preset-two failure, completion after two, correct-count held command pending rather than immediate failure, repeated Done not recounted, later low acknowledgement admitting next chain, pending deadline at 20 accepted seconds, count mismatch regardless of command, late N+2 count unable to repair N+1, final active command/fourth departure, changed count/spindle during hold, deadline including hold, waiting 25 accepted seconds before START staying Ready, preinterval deadline origin, Stop retaining historical success/failure and retiring incomplete strokes.

Additional tests cover nullable initial count, DINT nonfinite/fractional/out-of-range rejection and valid exact endpoints, copied signal-image immutability, exact alias use/missing/type collisions/duplicate alias ambiguity, sampled/effective disagreement, actual feedback versus toggled Done disagreement, invalid mappings/forces, nonhome initial plant, stale epoch and each semantic identity field, current draft mismatch, duplicate/skipped scan, exact timestamp and period retirement, nonzero session scan baseline, historical accepted count and sticky invalidation.

## Self-review and integration notes

- Observer reads only exact mapped aliases; PC/PLC declaration ownership/type ambiguity must be resolved by Main into `BindingsValid` because frozen signal-image records do not carry declaration ownership metadata. Reducer also validates primitive value availability/type and duplicate aliases. No invented internal counter names.
- Sampled/effective feedback must match copied BeforePlant; postplant feedback must match copied AfterPlant. Request/enable are required in all input images, but postplant request can differ due pulse cleanup. REAL values remain doubles in actual existing runtime; no float-normalization assumption made.
- Phase checks permit 40 ms Dwell→Return images with movement already begun due actual 10 ms internal substeps. No forced endpoint assumption at every phase boundary.
- Existing `VirtualControllerSession.ExecuteAcceptedCycle` creates postplant history image only when recording is enabled. Root must capture direct before/after and postplant inputs in its callback plumbing regardless of history; getter itself contains no history access.
- `Stop()` makes evidence historical and terminal. Observing the same instance after Stop, invalidation, timeout or concrete failure cannot produce fresh success. A new Reset epoch requires a new observer.
- After a successful current trial, continued accepted scans still detect changed command/count/completion or fourth motion; success is not reclassified as timeout just because further stationary scans pass 20 seconds. Stop preserves the result historically.
- No app compile/native/UI proof claimed. The copied getter's source is reviewed but must be compiled by root with application integration.

Concerns: no known pure-reducer blocker. Binding declaration validation, subscription generation/lifecycle integration, direct accepted captures with history disabled, UI, native invariance and actual disk learner fixtures remain root/QA tasks under the frozen plan. Pure tests establish the bounded model protocol only.

## Independent-review fix round 1 of 5: single START invariant

Review finding accepted: the initial implementation ignored later START requests after the trigger, so three individually requested strokes could incorrectly receive Observed. Source checkpoint before fix is retained by root for scoped re-review; no cleanup/replacement of that checkpoint was performed here.

Regression authored first in the actual-model rig at 20/40/100 ms: each of three complete chains is started by its own true request, then released and acknowledged with its count; the trace ends with valid count 3 and the required stationary hold. The test requires rejection immediately on the second START and forbids Observed at the end. It also verifies the physical model/count independently finish that trace, demonstrating observer rejection does not interfere with execution.

Functional RED command (same working directory and isolated project):

```powershell
$env:DOTNET_ROOT=(Resolve-Path .tools/dotnet).Path
& .tools/dotnet/dotnet.exe run --project tests/RepeatCycleObservedTrial/Tests.csproj
```

Exit 1 after successful compilation: `System.Exception: second START must reject separately requested cycles at 20 ms` at `Program.cs:62`. This is the demonstrated protocol defect, not an infrastructure error.

Minimal fix: after a trigger and initial accepted release, any subsequent effective START request yields NeedsAttention with `Additional START after release: the three-cycle batch must use one momentary START.` No change to cycle_active acknowledgement, plant stepping, images, counter logic, bindings or runtime getter. Added a separate actual-model midstroke repeated START rejection at each supported period.

GREEN rerun using the identical command: exit 0, `PASS 149 checks`. Existing all-period correct-count held cycle_active pending acknowledgement and 20-second timeout tests remain green; START and active command are handled independently. Scoped `git diff --check` exit 0, only existing LF/CRLF informational warning. No application build, Godot launch, other product edits or subagents. Restore/build remains isolated to the same local test bin/obj paths.

Self-review: rejection occurs after identity/image/scan validation and elapsed-time calculation but before phase/count progress, making the failure sticky. A single initial request held across several scans is not misclassified as a second request before its first release; mandatory release by third acknowledgement remains enforced. A fresh Reset requires a new observer as before. No known remaining pure reducer blocker; independent re-review required.
