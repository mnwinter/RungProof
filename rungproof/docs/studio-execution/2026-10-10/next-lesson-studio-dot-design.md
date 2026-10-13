# Dot design department — one existing lesson and constrained BYO contract

Status: departmental read-only proposal; meeting/root ratification and exact implementation contracts pending. Existing authorization covers bounded work; no new per-feature approval condition is proposed. Human color/texture approval remains separate and wider 75 material propagation stays held.

## Current capability and governance

`src/VirtualController/SceneLadderProject.cs` creates typed PC input / PLC output tags and watches, with no invented ladder solution. `SimulatorShell.LoadSceneLadderDraft` restores exact document, saved baseline, project path and history from scene cache; a fresh non-authored lesson uses that tag-only factory. Eight explicit authored factories, 69 tag-only defaults and 70 prose acceptance entries remain distinct. A prose objective is not a behavioral evaluator.

Actual learner Run must use `Main.RunEditorDraftController`: current compile-valid wrong code executes with its real consequences; invalid current code is rejected without a stale-program fallback. Project Open intentionally resets the authored plant; draft-only restore/current Run can preserve plant state. Joint documents preserve layout plus draft, not transient runtime history. Qualification must exercise these actual interfaces, not substitute a reference program through EnableVirtualControllerProgram.

Existing finite observer patterns (`Main.WorkspaceBeltObservedTrial.cs`, roller counterpart) consume the accepted ScanCompleted boundary after command projection and plant advancement, resolve expanded declared bindings, compare applied outputs to actual command points, and retain program/layout/reset identity. Their Shell cards are optional conceptual hints and read-only results. Pause displays CLOCK HELD separately from trial phase. They are specific bounded protocols, not reusable plant assumptions.

## Selected lesson candidate: Lab 4.3 repeat cycle counter

Engineering selects exact `lab-4-03-repeat-cycle-counter`. Source `SceneSimulationRuntime.RepeatCycle.cs` validates PC BOOL cycle_request/machine_enabled/machine_home/machine_busy/cycle_done, PC REAL head_position, PLC BOOL cycle_active/cycle_complete and PLC DINT batch_count. Imported autonomous adapters are disabled; accepted plant advancement alone owns feed and spindle projection.

`RepeatCyclePlantModel` prescribes 100 mm feed in 1 second, 0.5 second dwell, then 1 second return. Low command holds a partial stroke; Complete/Done persists until an accepted low command clears it. The plant consumes cycle_active only: machine_enabled is a learner input, not an independently synthesized physical permissive. This is a dry-stroke teaching model, not machining, process safety or OEM behavior.

Proposed visible objective: from Reset home, one enabled momentary Start completes exactly three genuine strokes, then learner count 3 and completion indication, without a fourth stroke. Conceptual hints may explain completed-event counting, request retention and completion acknowledgment, but must not provide rungs, insert tags, repair outputs or auto-run a reference solution.

Observer requirements: prove physical departure/busy and actual return before each newly generated Done event; sample that feedback on the following accepted scan. Compare actual completion count against the learner's claimed PLC batch_count. Require the third return, next accepted count=3 / cycle_complete=true / cycle_active=false, and at least 0.5 accepted seconds stationary with no fourth departure. Claim only the disclosed scenario, not arbitrary correctness. Reset begins a new epoch; force, missing/type-ambiguous bindings, current-draft mismatch, non-home start, skipped accepted observations and unsupported configuration yield Unavailable; unfinished Stop/timeout yields Incomplete; concrete premature completion, wrong count or fourth departure yields NeedsAttention. Completed evidence on Stop is historical, not a fresh pass. Disable/enable observer traces must be identical.

Native intended/wrong/invalid current-editor tests at 20/40/100 ms must include wrong CTU-on-request, held active with unacknowledged Done, and preset 2. Existing Main.RepeatCycleReview geometry/reference checks are useful but do not prove this learner workflow. Native actual machine head/spindle/reset and emissive/readout assertions are required. Scene/full split objective/hint/history/invalid captures must be individually inspected.

## Minimal reusable presentation, not universal grading

Use a small immutable lesson-card view model: exact lesson identity/version, objective prose, optional conceptual hint, result phase/detail, accepted scan/time, actual-event count versus learner claimed count, program/layout/reset identity, and currentness reason. Shell displays it without controller/plant handles. Cache raw phase/detail; derive held-clock text from scheduling separately. Hide on unrelated scene; clear identity on replacement; reset hint visibility at new epoch. No progress awards, leaderboards, workers or output writes.

The observer belongs in a pure lesson-specific class; Main constructs copied typed observations using exact declarations and post-plant actual state. UI never calculates completion from widget values or elapsed walltime. This first adapter can establish reusable transport/view-model seams without migrating the finite observers or all 77 lessons.

## BYO department boundary

QA source audit finds connector metadata only on belt material_in/out, roller pallet_in/out and legacy beam_center. Current connector links are compatible-kind visual relationships, not physical flow or accepted feedback. Generic belt/roller adapter inputs are run_command / estop_ok / speed_setpoint; outputs remain authored-only. Never relabel route_position metres as actual_speed metres/second.

Smallest proposed separate BYO contract is one existing generic belt: accepted PLC BOOL run_command, accepted-clock-owned drive ramp, measured PC BOOL running and PC REAL actual_speed (m/s), explicit point ownership/binding/joint persistence. This requires an explicit opt-in accepted-clock adapter, unchanged autonomous behavior elsewhere, detached-input defaults, wrong/invalid current code consequences and no double tick. It does not qualify arbitrary endpoint graphs, carton transport, load queues or beam material detection. Root must choose exact scope after QA deep dive; no implementation readiness claim yet.

## Meeting decisions still needed

Ratify lesson objective protocol and disclosed timeout; decide whether Lab4.3 gains Pause support via explicit supported-ID entry (not broad tag-only enablement); freeze immutable card and observation signatures, ownership and epoch attachment/detachment; choose BYO feedback adapter versus a purely structural connector qualification. Resolve these concrete contracts before edits. Acceptance must keep actual native behavior, typed ownership, observation invariance and rendered readability separate from human artistic approval and whole-catalog completion.

Dot performed read-only source inspection and department messages only. No product edits, engine execution, build, new preset, external transport or material propagation.
