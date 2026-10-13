# Shared Pause / one accepted Scan / accepted history — engineering proposal

Status: read-only studio proposal; no implementation or native acceptance. Scope offline authored demos, supported lessons and finite belt/roller workspaces. External PLC exchange remains outside this slice.

## Source contracts

- VirtualControllerSession.Advance currently owns elapsed backlog, fixed ScanPeriod, maximum1000 scans per call. It samples Boolean/numeric/text inputs, overlays operator inputs, executes runtime.Scan, verifies scanNumber increment, commits output images, advances plant exactly ScanPeriod, consumes momentary pulses, then publishes ScanCompleted and SnapshotPublished.
- VirtualControllerRuntime.Stop clears output values, due tasks/edges and nonretentive timer state. Main.StopActiveController commits this stopped image. Stop is therefore unsuitable as an inspection Pause: it changes control consequences and can fault attached Demo5 carry.
- Main.RunEditorDraftController validates the current draft; changed valid execution identity loads without implicit plant reset, invalid current invokes existing Stop. Main._PhysicsProcess currently calls session.Advance directly, with separate diagnostic clock holds. Reset/source replacement deliberately invalidate histories and home the actual plant.
- Runtime snapshots clone dictionaries. ScanCompleted provides accepted pre-plant sampled/effective inputs and committed outputs after the plant tick; its time/scan identity is authoritative. Actual poststep geometry feedback belongs to next scan, not the same sampled image.

## Smallest architecture

1. Add a session-owned inspection latch (`IsPaused`) without treating runtime Stop as Pause. `Pause` retains execution memory/outputs/forces and actual plant geometry, retains existing fractional/backlog unchanged, and emits no accepted scan. Normal Advance while paused does not accumulate time or consume pulses. Do not modify authored controller logic, permissives or successful/wrong outcomes.
2. Extract the existing accepted-cycle body into one private method used by Advance and `StepOneAcceptedScan`. Stepping is permitted only while paused, runtime Running and current loaded execution identity remains valid. It executes exactly one configured period, without borrowing/adding elapsed backlog and without temporarily enabling normal physics. Assert return accepted count exactly1 or0 on rejection. Operator momentary input queued while paused is consumed only by that accepted step; Stop/Reset retain existing pulse-discard semantics.
3. Main handlers distinguish Pause/Resume from Run/Stop. Step first validates current editor identity. Invalid draft rejects using existing stopped-output policy; changed valid draft requires explicit current Run/load, then pause before step. Run resumes unchanged identity with no reset; changed valid Run keeps existing actual wrong-code consequences. Session pause gates plant clock through the existing single Advance path, not by independently calling plant/motor callbacks. Ambient motion can pause separately; functional nodes stay under accepted clock.
4. Store a bounded accepted-history ring (for example128 entries) of immutable scalar/copied images plus scan/time/program/layout/reset identity and observed poststep feedback/model telemetry. History selection changes only UI selection, never the controller snapshot, plant points, forces, watch identity or current draft. Distinguish historic sampled input from poststep physical feedback. Epoch changes retire/clear active selection; historical rows may remain explicitly historical, never navigable as current element states under mismatched program identity.

## Timing and lifecycle decisions

- Configure periods20/40/100ms; stepping uses actual ScanPeriod. Runtime simulated time advances exactly one TimeSpan period. Keep existing fixed-period timer/task semantics.
- Pause retains pre-pause fractional/backlog for scheduling parity (Dot review); no paused wall time accrues. Step bypasses that accumulator without consuming it. Resume drains any pre-existing backlog under the existing1000-cycle cap. Stop/Reset still clear backlog. Do not replace elapsed timing globally with float approximations.
- One step must sample once, commit once and advance plant once. Check lifecycle generation/state after sampling and before committing to prevent Stop or replacement during callback from advancing stale plant. Existing previousScan acceptance guard must survive extraction.
- Pulse queued before Pause remains pending for the next explicitly accepted step; no paused physics frame consumes it. Repeated request handling remains existing momentary contract, not a new pulse queue.
- Manual Stop remains output-clearing and may produce genuine plant fault consequences. Pause freezes carry without fabricating a fault; stepping wrong vacuum code may then legitimately fault the attached load.
- Source replacement, project load, Reset and invalid current Run clear inspection latch and invalidate selected accepted history by identity. No automatic program repair or success evaluator.

## Qualification gates

Pure session tests: paused elapsed0/huge causes0samples/commits/plant/events; exact1step at20/40/100; immediate next feedback timing; single pulse consumption; unchanged pre-pause backlog/fraction after each manual step and Resume; retained timers/counters/forces/outputs; lifecycle rejection inside sampling; invalid no commit; immutable bounded history and eviction/identity retirement.

Native: Demo3 feedback stop and valid wrong run; Demo5 pause during genuine attached carry without pose/fault changes, single wrong step obeys current program; finite belt intended and wrong endpoint; roller PLC OFF remains coasting during next steps, Pause freezes coast, Resume continues same speed state. Compare automatic N steps with N manual accepted steps and observers enabled/disabled traces. Verify current/stale/invalid draft controls and actual read-only history UI navigation against full snapshot content.

Visual: actual Pause/Scan controls and history readout full/split, unambiguous current versus historical scan/time and input/output phases, no clipped actionable status. Existing material gate remains separate.

## Limits / unresolved decisions

No general physical simulation, live PLC stepping, arbitrary catalog adapter feedback, universal assessment or history-based output injection. Existing generic autonomous catalog controllers require separate handling if presented as part of this Pause contract; supported authored/finite accepted-clock plants are the initial scope. Root/Dot should ratify pulse retention and preserved-backlog semantics before code; they are explicit design choices, not current behavior claims.
