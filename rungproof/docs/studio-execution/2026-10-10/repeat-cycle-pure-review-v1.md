### Spec Compliance

- ❌ Issues found: the reducer does not enforce the frozen single-START trial. Additional START pulses during Running can supply the second and third departures and still yield Observed (`src/Lessons/RepeatCycleObservedTrial.cs:60-75`).
- ⚠️ Cannot verify from this scope: Main ownership/type binding validation, direct pre/post accepted capture with history disabled, stale callback generation rejection, Run/Reset/edit/Stop integration, read-only card and hints, other-scene exclusion, native disk fixtures, observer-on/off invariance, and rendered 1600/1280 acceptance. These remain controller/QA integration gates, not pure-task evidence.

### Strengths

- `src/Lessons/RepeatCycleObservedTrial.cs:34-57`: validation precedes adopting rejected scan counts; identity and continuity retirement protect accepted provenance.
- `src/Lessons/RepeatCycleObservedTrial.cs:79-114`: independently checks count at exactly N+1, permits correct-count pending acknowledgement, and excludes the final acknowledgement interval from stationary hold.
- `src/Lessons/RepeatCycleObservedTrial.cs:149-169`: validates finite integral DINT range and exact mapped input aliases against copied plant feedback.
- `src/Scenes/SceneSimulationRuntime.RepeatCycle.cs:11-18`: additive getter returns a copied immutable image or null and neither advances nor modifies the plant.
- `tests/RepeatCycleObservedTrial/Program.cs:13`: the test rig uses the actual plant model; the scoped test program exercises three periods and numerous concrete rejection paths rather than mocking phase behavior.

### Issues

#### Critical (Must Fix)

- None found.

#### Important (Should Fix)

- `src/Lessons/RepeatCycleObservedTrial.cs:60-75`: `rising` is acted upon only while `TriggerScan` is unset. After the initial release, `_released` stays true and any further request edges are ignored until the final acknowledgement/hold checks. Concrete accepted trace: pulse START to run the first actual chain, release and acknowledge with count 1, pulse START again for the second chain, release and acknowledge with count 2, pulse START again for the third chain, release and acknowledge with count 3 and completion true, then hold stationary. Every existing phase/count/hold check passes, so three separately requested single cycles are incorrectly reported as the required repeat cycle initiated by one momentary START. Reject an additional effective START request after the initial request has been released (with a clear NeedsAttention reason), and add a focused real-model regression for this trace at the supported periods. Preserve held-command pending acknowledgement behavior; it is a different signal and requirement.

#### Minor (Nice to Have)

- None recorded.

### Assessment

**Task quality:** Needs fixes.

**Reasoning:** The passive API and timing/count state machine are appropriately scoped, but the missing one-request invariant permits a materially incorrect learner program to obtain Observed. Fix and regress that invariant before task approval.

**Checks:** Read the scoped complete new-file package and additive getter diff; inspected unchanged `src/Scenes/RepeatCyclePlantModel.cs:17-58` for the concrete risk that feedback predicates or phase/acknowledgement assumptions differed from the real model (they agree). Additional START finding follows directly from control flow and the prescribed actual-model sequence. No tests rerun, application builds, engine launches, product edits, or Git mutations performed. The implementer reports PASS 137 checks; this review does not independently rerun that result. Initial missing-SDK failure and Git LF/CRLF informational output are disclosed in the report; neither indicates a final test compiler warning.
