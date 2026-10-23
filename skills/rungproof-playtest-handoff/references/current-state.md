# Current playtest and migration state

## Subsequent time limit

The user resumed discovery and requested deeper analysis of the harder issues
until **2026-10-23 04:00 America/Los_Angeles (11:00 UTC)**. At that instant stop
testing, discovery, repairs and delegated work; only final evidence/skill and
verified D:/Git backup closeout may continue. Then pause the goal, leaving the
77-scene audit incomplete if requirements remain. A recurring goal continuation
is not fresh human authorization to work past this cutoff. The task's cutoff
automation is `rungproof-4-a-m-stop-and-backup`; its final disposition and work
order are recorded under `outputs/screen-audit-2026-10-23/`.

## Provenance and entry points

- Repository: `mnwinter/RungProof`, `main`.
- Previous deadline backup: `41188c70bb86e3305f7cd7c8759f000f9c396729`.
- That snapshot contains the last application changes and curated QA evidence.
  The migration backup adds newer evidence, this skill and restoration records.
- Active application: `rungproof-next/` (Godot .NET plus integrated LAD editor).
- Project guidance: `AGENTS.md`, `PROJECT_INFORMATION.md`, `CONTEXT.md`, root and
  application `README.md`, `skills/rungproof-development/SKILL.md`.
- Current QA root: `rungproof-next/outputs/screen-audit-2026-10-23/`.
- Current checkpoint: `native-continuation-checkpoint.md` under that root.
- Current exhaustive cells/finite queue: `coverage-reconciliation.json` and
  `native-test-queue.json`. Check their later individual reviews before replay.
- Historical QA root: `rungproof-next/outputs/screen-audit-2026-10-21/`.
- Exact catalogue identities: `route-reconciliation.md` under that QA root.
- Remaining cases: `postbackup-remaining-requirements.md`. Some cells lag newer
  reports below; reconcile them rather than treating the whole file as current.
- Latest native position/fixture history:
  `postbackup-native-continuation-checkpoint.md`. Session handles in it are dead
  after migration. `SESSION-STATE.json` is older and contains local capability
  state; it is not an acceptance ledger or a safe public upload.

## Confirmed implementation/test boundaries

The B5 application build finished with zero warnings/errors. The last full
deterministic suite recorded **424 passed, 2 failed**: Watch schema expected9
versus10 and a TIA local-artifact hash mismatch. These are historical results;
do not report them as a test run on a restored machine. Full native acceptance
is incomplete even though all77 Scenario-menu entries were visited.

Deadline changes included physical input bindings, common Stop and vision
class controls, the Lab9.3 INT preset correction, fume-speed feedback,
diagnostic-list refresh, help corrections, and command-driven plant models
for tote/lift/door/jug/drill/parcel/CNC. CNC attachment and jug stroke corrections
were included. Refer to the commit diff for exact code scope.

## Confirmed open problems

1. **Parcel Lab2.23 Reset/repeat fails.** Historical V3 count1 and V4 count2
   stalls remain outcomes of those exact LAD controllers. Frozen diagnostic V7
   completes all three supported receiver deliveries on a fresh scene load,
   with complete/receivedTrue and commandsOff; this has been observed twice.
   The same V7 after real AppReset/Run/physicalStart stalls at count1; native
   Watch identifies medium outbound qa_step50/qa_class2. The controlled Reset
   comparison reproduces the failure without a Stop probe. Read the current
   `parcel-v7-native-review.md`, `parcel-v7-repeat-review.md` and their later
   controlled-comparison addendum. Do not repeat the blanket claim that normal
   completion is impossible. `parcel-sensor-reset-decision-table.md` traces a
   source-supported Reset ordering hazard: retained parcel yaw is measured as
   world-AABB dimensions before the visual root is restored. Static asset/contact
   math supplies a counterexample, not a native private-dimension measurement.
   V7 remains diagnostic learner LAD, not a shipped application repair.
2. **Diagnostics footer QA-UI-002 remains misleading.** Blank Run reports
   EDIT001; authoring a valid Network+NO rung and running it clears ErrorList
   to0ERRORS and executes the ladder, while the outer footer still says
   RUNBLOCKED/current draft invalid. List refresh is partial, not a closed fix.
   Read `postbackup-diagnostics-checkpoint.md` and
   `postbackup-diagnostics-review.md` for the exact native reproduction.
3. **Gallery empty conveyor animation remains visually unproved.** CNC, fan
   and mapped motor motion/stop have bounded native proof. Empty belt is dark;
   unchanged wide pixels do not prove a defect. Source has scrolling belt UVs
   and largely symmetric rollers; the separate splice marker is static.

## Later native evidence superseding older blocked/gap cells

- Demo1 AppStop at actual count1 clears projected output while stopped; Run
  restores the retained count without an extra event, further pulses reach2/3,
  and AppReset returns stopped0. Current `demo1-appstop-review.md`; no extra
  count2 Stop test is required for this bounded closure.
- Drawbridge5.8 actual descending STOPPED-acknowledgement loss holds the tilted
  deck at45.85; restoration reaches home, opens barriers and releases traffic.
  Current `drawbridge-descent-review.md`; the old pending case below is closed.
- Current October23 reviews additionally close parcel phase30/class2 AppStop
  and Run-alone completion, Demo2 retained-REQUEST AppStop/full timer restart,
  Lab5.2 acknowledged active pulse nonextension, marker2.3 active-input Reset,
  and coating5.10 initial-index Stop/Run recovery. Their exact reviews are linked
  from the current checkpoint and queue. Parcel Reset/repeat still fails.
- Jug2.13:100percent fill/high signal and supported discharge; partial-fill
  AppStop hold, Run completion, Reset and fresh repeat.
  `postbackup-jug-review.md`.
- CNC2.24: physical completion/outfeed, active infeed Stop/Run, actual qualified
  active cut, Reset/repeat with commands off at completion.
  `postbackup-cnc-review.md`, `postbackup-small-cnc-review.md`.
- Drill2.16: active feed Stop hold/Run completion, Reset/repeat, hand-loss hold,
  no automatic restart on hand restoration, blocked midstroke Start and
  Reset/fresh qualified recovery. This is the QA V3 ladder policy.
  `postbackup-small-drill-review.md`.
- Elevator11.11: actual upper feedback and descent, descending door loss holds
  position, restoration returns lower. QA V2 has an observation dwell, not a
  new application timing requirement. `postbackup-small-elevator-review.md`.
- Array10.6: actual occupancy feedback for all10 zones, supported final
  conveyor endpoint and bounded held pose. Zone8/9 were missed by OCR but are
  visible in originals `small-array-r2-zone8-6.png` and `...zone8-10.png`.
  No automatic sequencing or completion tag is implied.
  `postbackup-small-array-review.md`.
- Door2.19: open/close, midclose Stop hold, restart, endpoints and Reset/repeat.
  `evidence-review-fix-service-door.md`.
- Lab4.5: A2/B4 valid/green; one more B givesA2/B5 invalid/dark.
  `postbackup-small-count45-review.md`.
- Lab4.6: A2/B3 valid/green; one more B givesA2/B4 invalid/dark.
  `postbackup-small-count46-review.md`.
- Gallery CNC: moving internal component enabled, held disabled.
  `postbackup-small-gallery-review.md`.
- Representative Viewer demo/lab and Browser demo/lab selections, owned draft
  Cancel/Discard, Save/reopen and fresh functional run have native evidence.
  Read `postbackup-small-navigation-review.md` and
  `postbackup-navigation-save-reopen-review.md`. The saved test workspace is
  `test-projects/postbackup-navigation-count46-owned.rungproof.json`.
  Saved LAD reopens STOPPED with count0; live counter retention is not claimed.

## Resume efficiently

The user resumed deeper discovery until the explicit cutoff above. Preserve
the verified migration base, and use the current finite native queue rather
than another77-entry menu sweep. The drawbridge descent case is closed. Moving
parcel handoff Stop/recovery is separate from the confirmed Reset failure; read
the latest native checkpoint/reviews for whether that case subsequently ran.
Do not implement application repairs during discovery. Candidate subsystem
boundaries are recorded separately from the deferred final all-scene repair plan.

Current `qa-ui-002-causal-analysis.md` traces the stale footer to separate outer
and ladder status fields: successful local Run clears only the latter. Existing
automated diagnostic coverage omits the outer status. Current
`reset-transform-impact-analysis.md` found no equally direct duplicate of the
parcel transform/geometry ordering seam in the other inspected plant adapters.

The verified D:/Git base at d0c4566b does not yet cover subsequent October23
evidence/userdata/skill changes. The final closeout must create a separate
verified incremental addendum and pause the goal/automation at the cutoff.

Before devising the final prioritized all-scene remediation plan, reconcile
the full remaining requirements and issue list. No full-application PASS,
release readiness, installed-TIA parity or live-PLC conclusion is established.
