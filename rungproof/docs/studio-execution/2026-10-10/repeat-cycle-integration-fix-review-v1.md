### Spec Compliance

- ✅ Spec compliant for Task2 fix round 1. The original joint-open pending-epoch finding is addressed by staging the existing observer reset after the loader's final detach, exact draft/layout restoration, and SetIntegratedSceneIdentity (`src/App/Main.cs:4098-4106`). Ordinary controller replacement retirement is unchanged.
- ⚠️ Full Task3 causality, observer-on/off complete-trace invariance, history-on/off equivalence, timeout/wrong-program scenarios, physical pose/readout/indicator agreement, and rendered acceptance at 1600/1280 remain pending. This fix review approves only the bounded integration correction and diagnostic.

### Strengths

- The product correction is one existing-method call at the concrete loader lifecycle boundary; it does not add plant stepping, output assignment, a reference-program factory path, or general exceptions to historical retirement (`src/App/Main.cs:4103-4106`).
- The diagnostic uses actual disk editor Open, actual joint save, scene navigation, exact joint reopen and current-editor Run without an intervening Reset (`src/App/Main.RepeatCycleLifecycleReview.cs:50-81`). It asserts both a new Ready epoch and first accepted START becoming Running, directly exercising the previously missing behavior.
- Adjacent lifecycle checks verify unchanged Stop/Run retains the historical trial and same session, ordinary changed-program replacement remains Unavailable, and invalid exact reopen remains blocked with the real card label Unavailable (`src/App/Main.RepeatCycleLifecycleReview.cs:83-100`). Fresh factory zero rungs is explicitly asserted (`:63`).
- Diagnostic execution is opt-in through its dedicated command flag/deferred dispatch (`src/App/Main.cs:201,724-727`); exact accepted intervals are controlled only within the diagnostic, which exits with an assertion-derived code (`src/App/Main.RepeatCycleLifecycleReview.cs:46-49,59,102-104`).

### Issues

#### Critical (Must Fix)

- None found in the fix-scoped package.

#### Important (Should Fix)

- None found in the fix-scoped package. The original Important finding is resolved.

#### Minor (Nice to Have)

- None requiring a separate change in this scoped fix.

### Assessment

**Task quality:** Approved for this bounded Task2 source/lifecycle gate.

**Reasoning:** The fix restores the pending epoch precisely after the loader operation that previously destroyed it, while preserving retirement for ordinary program replacements. Existing native evidence directly demonstrates the failing-to-passing lifecycle behavior; it does not establish the remaining lesson acceptance gates.

### Checks and Evidence

- Read `task-2-fix1-review-package.md` and the appended `task-2-report.md`; reviewed only the fix, dispatch hooks, and new diagnostic against the original finding. No source edits, builds, engine launches, subagents or test reruns.
- Inspected retained `.tools/studio-verification/repeat-cycle-task2-lifecycle-red-native-v1.log`: four failed assertions, including new Ready epoch and first accepted START observation, with final FAIL failures=4. The other two failures were downstream historical/replacement checks.
- Inspected retained `.tools/studio-verification/repeat-cycle-task2-lifecycle-green-native-v1.log`: fourteen true assertions, zero false assertions, final PASS failures=0; the ERROR/Exception filter returned no matches. Root additionally reports process exit 0.
- Inspected retained `.tools/studio-verification/repeat-cycle-task2-lifecycle-green-build-v1.log`: Build succeeded, 0 warnings, 0 errors. Root reports the RED build also had 0 warnings/errors. Neither compiler result is treated as visual or full causal acceptance.
