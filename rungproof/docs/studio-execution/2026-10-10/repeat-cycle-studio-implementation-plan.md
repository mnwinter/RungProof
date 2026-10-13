# Lab 4.3 repeat-cycle studio implementation plan

Status: APPROVED_FOR_BOUNDED_IMPLEMENTATION_WITH_LIMITS. This is the departmental design ratification, not runtime, visual, physical or human style acceptance. Scope is only `lab-4-03-repeat-cycle-counter`. Existing authorization covers this next lesson milestone; root owns integration and execution. Fresh default remains tag-only, learner Pause/Step remains unsupported, other lessons and wider material rollout remain unchanged.

## Meeting and authoritative behavior

Dot reviewed `RepeatCyclePlantModel.cs`, the accepted session callback pipeline, the lesson department proposal and QA department proposal. Departments agree: the existing prescribed offline model feeds 100 mm for 1 s, dwells 0.5 s and returns for 1 s. Only `cycle_active` advances it. A low command holds a partial stroke; Complete persists until low acknowledges it. `machine_enabled` is an input the learner must use, not an independent plant interlock. Busy denotes the active phase even when command removal holds movement. No vendor CNC, machining, load, safety or commissioning claim is made.

Accepted pipeline order is sampled inputs, effective inputs after forces, PLC execution, output commit, one plant advance, post-plant capture, then ScanCompleted. Newly generated Done at scan N cannot be used by that scan's controller; N+1 is its first accepted sample. Observer integration must work with history recording disabled and must copy actual post-plant state directly. It may not infer state from history availability or manually toggled feedback.

## Frozen protocol

One enabled momentary START from measured Reset-home must yield three genuine Feed -> Dwell -> Return -> Complete chains and stop without a fourth departure. Waiting before the first effective enabled request rising edge remains Ready; the 20 accepted-second deadline begins at the pre-interval timestamp of that edge, `SimulatedTime - ScanPeriod`. The first triggering interval counts toward elapsed time; elapsed at its completion is exactly one period. Accepted simulated time, not wall time, determines the deadline. A separate native runner wall-clock watchdog reports infrastructure failure only.

Each chain requires phase order, departure from home, return home and a new actual Complete edge. At N, pre-plant phase is Return, sampled/effective Done is false and post-plant phase is Complete/Done true. The mapped PLC batch_count may retain its preceding value at N. At exactly N+1, sampled/effective Done must be true, mapped batch_count must increase once to the independently counted return total, and count is checked independently of acknowledgement. For returns 1 and 2, active false with post-plant Idle/Done false acknowledges completion. If the count is correct but active remains true and the plant remains Complete/Done true, enter a pending acknowledgement state: do not require immediate failure, do not admit a new chain, and do not recount repeated Done. A later accepted low command with actual Idle clears that pending state; failure to acknowledge by the deadline yields Incomplete. A later Feed is a new chain only after this acknowledgement. Count mismatch at exactly N+1 is NeedsAttention regardless of the command.

After the third N+1 acknowledgement, count must be exactly 3, completion true and active false. START must already have been released. Require at least 0.5 accepted seconds in subsequent accepted intervals at unchanged home/spindle, with no fourth departure, no count change and the completion/command conditions retained. Do not credit the acknowledgement interval as elapsed stationary hold. At 20/40/100 ms this requires respectively 25/13/5 subsequent intervals (40 ms yields 0.52 s). Tolerance applies only to numerical position comparisons, never to scan identity or count equality.

Concrete early count, count mismatch at N+1, completion after two returns, a fourth departure or a nonzero active command after final acknowledgement yields NeedsAttention. A correct-count held command at the first or second completion remains pending acknowledgement and cannot start another chain; it remains incomplete and reaches Incomplete at 20 accepted seconds. No observer intervention clears Done or fixes count. The timeout includes the final hold; success cannot occur after the deadline.

## Exact proposed immutable API

These are new implementation signatures, not claims of existing APIs. Place the pure reducer in `src/Lessons/RepeatCycleObservedTrial.cs`; use explicit usings and immutable copied records.

```csharp
public enum RepeatCycleTrialStatus { Ready, Running, Observed, NeedsAttention, Incomplete, Unavailable }
public sealed record RepeatCycleTrialIdentity(long Epoch, string SceneId, string PlantVersion,
    string ProgramFingerprint, string BindingFingerprint, string LayoutFingerprint);
public sealed record RepeatCycleTrialBindings(string Request, string Enabled, string Home,
    string Busy, string Done, string HeadPosition, string Active, string Complete, string Count);
public sealed record RepeatCyclePlantImage(RepeatCyclePlantModel.CyclePhase Phase,
    double HomeFraction, double SpindleRadians, bool Home, bool Busy, bool Done);
public sealed record RepeatCycleAcceptedObservation(RepeatCycleTrialIdentity Identity,
    long ScanNumber, TimeSpan SimulatedTime, TimeSpan ScanPeriod,
    VirtualControllerSignalImage SampledInputs, VirtualControllerSignalImage EffectiveInputs,
    VirtualControllerSignalImage AppliedOutputs, VirtualControllerSignalImage PostPlantInputs,
    RepeatCyclePlantImage BeforePlant, RepeatCyclePlantImage AfterPlant,
    bool CurrentDraftMatches, bool BindingsValid, bool ForcesPresent);
public sealed record RepeatCycleTrialView(RepeatCycleTrialStatus Status, string Detail,
    int ActualReturns, long? ClaimedCount, long? TriggerScan, TimeSpan AcceptedElapsed,
    TimeSpan StationaryHold, bool Historical);
public sealed class RepeatCycleObservedTrial
{
    public RepeatCycleTrialView View { get; }
    public RepeatCycleObservedTrial(RepeatCycleTrialIdentity identity, RepeatCycleTrialBindings bindings);
    public RepeatCycleTrialView Observe(RepeatCycleAcceptedObservation observation);
    public RepeatCycleTrialView Stop();
    public RepeatCycleTrialView Invalidate(string reason);
}
```

If namespace dependencies require an independent phase enum, copy the five explicit phase values through a checked mapping; do not silently cast unrelated enums. The view is presentation only. `ClaimedCount` is nullable and populated only from the validated last accepted mapped PLC DINT (including its DINT range check), never current UI points or a default zero. It is null before the first valid observation or when binding/value availability is invalid. The immutable accepted value is retained for historical display with its identity; a new unavailable observation must not present a fabricated current count. Internal reducer state may be immutable replacement records; it must expose no mutable controller/plant handle. Runtime getter `CaptureRepeatCyclePlantState()` returns a copied `RepeatCyclePlantImage?`, null when that plant is absent. Capture BeforePlant immediately before the existing plant advance and AfterPlant immediately after projection. Existing plant Step and integration remain unchanged.

Main resolves exact scene point declarations to actual program binding aliases once per epoch and passes the immutable RepeatCycleTrialBindings; images retain their original accepted alias keys. The reducer reads those exact aliases, never synthesizes renamed feedback or default values. No assumed internal counter name; DINT batch_count is checked for finite integral range before conversion. Missing, wrong-owner/type, duplicate or ambiguous mappings are Unavailable. Preserve sampled versus effective and actual applied output agreement; force use makes assessment Unavailable. A non-home initial plant, skipped/duplicate scan, unexpected period/time, stale epoch, scene/layout/program/binding change or current-draft mismatch retires current observation. First scan may have a nonzero session number; establish its baseline, then require strict +1 and exact period increments.

## Lifecycle and presentation

Reset creates a new epoch at measured home; scene replacement, program replacement, layout change and binding change retire old subscriptions. Capture subscriber generation and reject stale callbacks. Valid Run with unchanged semantic identity may resume an incomplete physical stroke but cannot become a new Reset-home trial; show historical/incomplete evidence until Reset. Stop before success yields Incomplete; existing NeedsAttention/Unavailable/Observed remain preserved as historical, without fresh success. Invalid current Run stops stale execution and retains exact invalid draft. Merely editing a draft invalidates currentness without modifying outputs. No observer clock, worker, force, reset, output assignment, counter repair, hidden reference program, score or progress persistence is allowed.

One read-only lesson card shows actual modeled returns separately from learner count, status, current/historical identity and reason. Optional conceptual hints explain completion edges, retained request, low acknowledgement and target stop; never supply rungs or silently populate the editor. Hide on the other 76 scenes. Pause/Step must remain visibly unsupported here. Current run/history provenance must stay clear; history selection is read-only and cannot mutate live state or navigate against stale accepted identity.

## Owners and verification gates

Engineering: pure reducer and isolated tests plus additive runtime getter. Root: Main accepted pre/post capture, lifecycle hooks, build/native flags and captures. Shell owner: read-only card/hint partial only. QA: `Main.RepeatCycleLearnerReview.cs`, actual learner fixture/joint workflow, strict trace comparisons. Dot: independent source and individual rendered-image review. Root coordinates file leases before edits; no shared Main overwrite.

Pure tests at 20/40/100 ms cover genuine three-chain success, mandatory release, one-scan lag, early/missing/double count, repeated Done, premature completion, fourth motion, hold duration, deadline, force/mismatch/nonhome/epoch/scan retirement and Stop preservation. Native tests use actual disk Open -> joint save -> navigate -> exact reopen -> current editor Run, with intended, request-count wrong, preset-two wrong, held-active wrong and invalid drafts. Fresh default must have zero rungs. QA references remain explicitly diagnostic fixtures.

Native proof includes actual head/spindle/readout/indicators, count N versus N+1, partial stroke Stop/resume, Reset, invalid midfeed freeze, unsupported Pause/Step and exact saved JSON/path/history. Run observer enabled/disabled with identical actions and compare every accepted sampled/effective/applied/postplant image, actual phases, counters, points and complete native poses. History on/off is separately checked; observer absence must not alter command/plant behavior. Normalize only proven engine-generated clone names, retaining authored IDs.

Actual scene/split/card/hint/history/invalid renders must be inspected at 1600 and a bounded 1280 case. Count/readout correctness from native tests does not establish overview text legibility. Source/native/pure passes do not approve other lessons, all77 visuals, material styling, live PLC or physical installations.

No design blocker remains. Getter placement, binding resolution and actual native/display accessors must be verified during implementation rather than invented. Any failed native causality or invariance assertion blocks this lesson milestone; preserve the failing evidence and fix the smallest demonstrated defect.



