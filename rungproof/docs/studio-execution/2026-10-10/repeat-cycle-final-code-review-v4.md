# Lab 4.3 final bounded code review v4

Status: CHANGES_REQUESTED. One Important/P2 lifecycle issue remains. This is source review of the bounded repeat-cycle implementation, not approval of the dirty branch, native results, rendered output, physical behavior, or release.

## Finding

**P2 — Preserve an explicit measured Reset when the next Run loads the edited current draft.** `src/App/Main.RepeatCycleObservedTrial.cs:29` clears `_repeatCyclePendingReset` on every retirement. In the real edit -> Reset -> Run path this destroys a deliberately staged new epoch. `Main.cs:1476-1479` physically resets the plant and existing controller, then calls `ResetRepeatCycleTrial`; that method clears the old observer and stages pending=true (`Main.RepeatCycleObservedTrial.cs:34-39`). Its immediate Bind rejects the edited draft because the loaded program is still the old program (`:42-43`; `Main.ScanInspection.cs:50-56`). Current-editor Run subsequently replaces that old program (`Main.cs:1373-1374`). `EnableVirtualControllerProgram` calls `DisableVirtualController` at `Main.cs:1068`, which retires the observer at `:1104` and clears the staged pending epoch. The post-attachment Bind at `:1089` therefore returns without constructing any trial. The valid edited program executes, but the observed-trial card stays Unavailable with no accepted view until the learner performs a second Reset.

Minimal real-UI reproduction:

1. Load a valid repeat-cycle draft A and Run it so a controller is attached. A partial stroke is a useful stronger starting state.
2. Make a valid semantic edit to obtain draft B, for example add one memory BOOL tag. The current-draft hook correctly retires A's evidence.
3. Press actual Reset. Confirm measured Idle/home, cleared Done/busy and reset session clock.
4. Press actual current-editor Run. B is now loaded and executes, but `_repeatCycleTrial` is null because the pending explicit Reset was discarded.
5. Enable and pulse START. The physical plant advances under B while the card remains Unavailable; no new accepted trial is observed.

Expected bounded behavior: the explicit Reset should authorize a fresh measured-home epoch for the current valid draft when it is attached by the subsequent Run. The frozen plan says Reset creates a new epoch at measured home and the card says Reset starts a new trial. This is distinct from merely editing and Running a replacement program without Reset: that negative case must remain retired, even if the plant happens to be at home. Do not generally preserve a pending epoch through arbitrary replacements or create one on every Run. Use a narrowly validated explicit-Reset handoff/current-draft identity, or another equally bounded lifecycle solution. Preserve invalid-draft blocking, force retirement, Stop/Run historical evidence and joint reopen behavior.

Required regression: at 20/40/100 ms, exercise actual loaded A -> valid edited B -> ResetActiveController -> RunEditorDraftController, require a fresh Ready identity for B and first enabled START becoming Running, with genuine physical home/reset evidence. Keep edit -> Run without Reset as a negative assertion. Additional draft changes between Reset and Run must not silently reuse stale authorization. The current native suites reset only after the replacement is already loaded in the relevant paths; they do not cover this order.

This finding is confirmed by source control flow, not a claimed native reproduction. Root independently checked the same path and accepted the finding; the original integration owner will add actual native RED and the minimal correction. A completed v4 native matrix can remain useful evidence for its existing assertions but cannot close this missing case.

## Remaining source assessment

No other blocking source finding was identified in the scoped review.

- The passive reducer checks actual copied phase transitions and return edges, N versus N+1 count causality, the single START/release invariant, pending low-command acknowledgement, three returns and the subsequent 0.5-second hold. It uses accepted elapsed time and a 20-second deadline. Current Observed continues to assess later scans and rejects changed final command/count/completion/pose or a fourth departure; terminal failures and historical Stop do not revive.
- Bindings resolve exact unique program aliases against declared scene ownership/types. Sampled and effective feedback must agree with actual copied preplant state; post feedback agrees with copied postplant state. Accepted outputs are compared with independently copied projected runtime points. DINT values are finite, integral and range checked; no internal counter name or fabricated unavailable count is required.
- The runtime getter is additive and copied. Main captures immediately around the existing single AdvanceSimulation and consumes a generation/session/runtime/scan-qualified pair on ScanCompleted. The pair and QA trace do not depend on history recording or reconstruct before-state from a consumed observer pair. No observer plant step, command assignment, hidden default reference, counter repair, worker or persistence was found.
- Fixed authored composition/equipment root identity includes numeric transform components and authored workspace/config identity, while excluding KIN descendants. Current card provenance checks semantic draft and layout; assessed returns and nullable accepted count are separate, with elapsed and frozen/updating wording. Scene visibility is confined to the intended offline lesson.
- Diagnostic fixture construction is explicit and separate from factory initialization. QA uses disk Open, joint save, navigation, exact reopen and current Run; aliases, wrong-count/preset/held-command consequences, all-period lifecycle cases, observer/history invariance, actual readout/lamp state and native poses are asserted. The isolated test project compiles the actual model, reducer and immutable signal image declaration.
- The capture wrapper checks fresh output paths, actual ready/status markers, held diagnostic clock and exact DLL identity, PNG signature/IHDR and requested dimensions. Those checks do not establish image readability or source-to-build identity without the separate freeze/build qualification and individual rendered inspection.

The previously reported minor diagnostic limitation remains: some lifecycle assertion names repeat across periods. The loop does run 20/40/100; this is log readability, not a new blocker.

## Scope and evidence

Read the authoritative implementation plan and owning execution tasks; Task 1/2/3 reports, original reviews and all fix reviews; complete scoped reducer, observer integration/card, lifecycle/learner/capture diagnostics and isolated tests; narrow Main lifecycle/UI hooks, actual accepted callback/session ordering, runtime copied getter and capture validation. No massive HEAD diff, unrelated cleanup, commit, push, application build, native launch, test run, or subagent was performed. Only this review document was written.

Root reports v4 build zero warnings/errors and DLL `229D7BAC4164F1026BD2744A345A7869D6493090F3C8708B9CC21BE58E5F4006`, tied to `repeat-cycle-source-freeze-v4.json` (421 files). Native v4 was running at assignment; neither its filename nor source approval establishes PASS. Any fresh fix requires a new source freeze/build identity and regression evidence. Actual scene/split/card/hint/history/invalid and wrong-case rendered inspection at the required sizes remains a separate gate.

Prior Handler GCHandle investigation remains OPEN; material 75 remains HELD; the 69 fresh tag-only scope and all-77 release decision are not broadened by this review. No physical commissioning, vendor hardware or whole-product release approval is granted.

Reviewed SHA256:

- `src/Lessons/RepeatCycleObservedTrial.cs`: `97FA6DCEA4631D07DE8056982FA5D7965CFFCB71C98FE56A6A707D2B1B9D1BB1`
- `src/App/Main.RepeatCycleObservedTrial.cs`: `270661790FEE619C82C59B588304D659463DE53693CBC89C37CF958BA800468A`
- `src/App/SimulatorShell.LessonObservedTrial.cs`: `6C7A8103C594BACF2570E1AF6614F491D407A12CD8D568E41877979B7B982490`
- `src/App/Main.cs`: `659F96670C4A870736E85C6B2D9B1CA5BC59BB361B504CCE1669557EE5B9E966`
