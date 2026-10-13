# Lab 4.3 final lifecycle fix re-review v5

Status: SOURCE_APPROVED_FOR_BOUNDED_FIX. The v4 P2 edit -> Reset -> Run finding is resolved. No new blocking issue identified in this scoped re-review. Comprehensive exact-v5 native and rendered acceptance remain separate gates.

## Finding resolution

The previously lost explicit Reset now has a one-use immutable handoff. `src/App/Main.RepeatCycleObservedTrial.cs:23-51` records and validates the current semantic draft fingerprint, runtime reference, prior controller reference, epoch, fixed layout and copied actual plant state. `ResetRepeatCycleTrial` stages this only for a compiler-valid current draft at measured Idle/home with zero spindle (`:65-75`). Watch metadata is consistently excluded from the semantic fingerprint.

`src/App/Main.cs:1068` captures matching authorization before the existing controller detach. The existing detach still retires ordinary evidence and clears stored pending authorization. Only after successful compilation, normal new-session reset and current shell attachment does `:1091` restore/revalidate the captured handoff and invoke Bind. Bind verifies the handoff again and consumes it before constructing the observer (`Main.RepeatCycleObservedTrial.cs:78-87`). A replacement without explicit matching authorization still cannot obtain Ready merely because the plant happens to be home.

The handoff does not add a physical Reset, AdvanceSimulation, PLC output assignment, default reference program, or run worker. The existing controller replacement reset remains unchanged. Runtime/epoch/layout/plant/draft equality is checked before and after replacement; the previous-controller check specifically qualifies capture before that replacement. Retirement and Stop clear the staged state. Pending draft edits are checked even while the observer itself is null. Invalid drafts cannot stage authorization, and force use follows the existing immediate retirement path. Continuous current Observed assessment is unchanged.

## Regression review and retained evidence

Compared the exact three `before/task2-reset-handoff-*` snapshots to current Main.cs, Main.RepeatCycleObservedTrial.cs and Main.RepeatCycleLifecycleReview.cs. Read the Task 2 fix-round-3 report and the original v4 finding. No unrelated HEAD diff or broad branch review was performed.

The new regression uses actual disk Open/joint save/reopen for A, starts a real partial stroke, restores valid semantic B, calls actual Reset, then current-editor Run. At 20/40/100 ms it requires measured home and reset clock, exact B draft/fingerprint and a fresh Ready epoch, then real enabled START producing Running and physical departure. It separately asserts no-Reset replacement retirement, invalid Reset/Run blocking and exact invalid-draft preservation, force-clear retirement, fixed-root layout change after Reset, another semantic edit after Reset, and Stop after staged Reset. The negatives do not authorize a new epoch by resetting it inside the assertion.

Read retained `.tools/studio-verification/repeat-cycle-lifecycle-fix3-red-native-v1.log`: terminal FAIL failures=6 and preserved ROOT_GODOT_EXIT=1. Root identifies its built DLL as `324AB3AFBDD5BE862F1CB7A326DD9BDBC5B18AC03B4AB341010648BB4DA1A734`, with zero build warnings/errors; the six failures were the new Ready/START assertions at all three periods.

Read retained `.tools/studio-verification/repeat-cycle-lifecycle-final-native-v5.log` and counted 47 True, 0 False, 0 ERROR/EXCEPTION; terminal PASS failures=0 and preserved ROOT_GODOT_EXIT=0. Root reports the v5 build had zero warnings/errors and DLL `65107EE176DD7BF2B1556101D583289FFDA18DA345787914963DC0CBFB8A5950`. This closes the concrete lifecycle regression and its included negatives. The reviewer did not rebuild or rerun the engine.

## Boundaries

Only this review document was written; no product edits, builds, engine execution, subagents, Git mutations or cleanup. Full v5 causal/invariance native matrix was still running at assignment. Individual actual scene/split/card/hint/history/invalid and wrong-case rendered inspection remains pending. Neither this review nor the 47 lifecycle assertions establishes all required visuals, all 77 lessons, material approval, hardware behavior or release approval. Prior Handler GCHandle OPEN and material 75 HELD remain unchanged.

Reviewed SHA256:

- `src/App/Main.cs`: `9E319A4A3861663A6B1F941C2CAF17C6359360BE9B12F2D54A638953836433D5`
- `src/App/Main.RepeatCycleObservedTrial.cs`: `2757348A002F79749CAECF15E0A3C80FAD85878CD4BDCC56FB978EF677AB55DF`
- `src/App/Main.RepeatCycleLifecycleReview.cs`: `A100FB1843BF6EC7534BDB0A688B58690E8DE373C714C5DBD2D7EADF66FF3E08`
