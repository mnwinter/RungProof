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
- Compiled problems and global plan: `scene-issue-compendium.md`,
  `current-compilation-validation.json` and `repair-plan.md`. All77 IDs/616
  cells and all24 original finite cases are accounted for:18 native closures,
  five source/native alias reconciliations and Q23 split. Six cells remain
  failed/partial; there is no blanket acceptance or implemented new repair.
- Native input ended; owned app/bridge were identity-checked and stopped at
  2026-10-23T08:09:01Z. All session handles are historical. Source/design work
  continues only until the explicit cutoff above.
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

## Open findings and observation limits

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
   A later finite macro physical-toggle ON/AppStop comparison still has no
   readable belt witness, while independently showing rear-motor motion/hold.
   `gallery-close-belt-review.md`; keep its two PARTIAL cells, not a new bug.
4. **QA-UI-003 S2:Demo3 Engineering Frame Process hides the machinery.** The
   route repeatedly shows a flat wall; Immersive restores process and controls.
   `demo3-frame-process-visibility-review.md`, `camera-framing-decision-note.md`
   and `camera-fit-feasibility-analysis.md` distinguish native failure from
   conditional camera/room/offset hypotheses. Do not remove the accepted room
   enclosure or clamp distance and crop the process.
5. **QA-SPEC-002 S4:Lab2.1/2.6 wording contradicts maintained toggles.** Correct
   presentation prose/two generated help files while preserving proven input
   behavior, IDs and bindings. `status-and-control-wording-decision-note.md`.
6. **QA-UX-CANDIDATE-003:radar inspection lacks a visible route.** Source
   supports the access gap, not a failed ghost renderer. The recommended
   narrow route is View > Tank inspection view using the existing SIM action,
   with actual state/checkmark/lifecycle guards. `radar-visible-route-decision.md`.

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
The discovery list is now compiled. Read the prepared global `repair-plan.md`
and `repair-plan-challenge.md` before any later implementation; this window
still contains no new application repairs. A restored/resumed chat must honor
the human cutoff rather than automatically starting another case.

Current `qa-ui-002-causal-analysis.md` traces the stale footer to separate outer
and ladder status fields: successful local Run clears only the latter. Existing
automated diagnostic coverage omits the outer status. Current
`reset-transform-impact-analysis.md` found no equally direct duplicate of the
parcel transform/geometry ordering seam in the other inspected plant adapters.

The verified D:/Git base at d0c4566b has a later verified interim at a2ac82fb,
with its own D receipt and latest-checkpoint pointer:4122 selected files,
1,907,784,001bytes,matching heads/skill and fsck PASS at07:10UTC. Newer work
still needs its own final backup. Read the current native checkpoint for scoped closures,
including chain-lift upstroke, pedestrian active retrigger and authored Demo4
retention/full repeat. Do not repeat them based on older queue prose.

Current later reviews also close authored Demo5 moving ENABLE loss/recovery and
Demo3 ENABLE loss plus actual420-main/820-side placement. Demo3 is one recycled
mesh with continuous cycles; side transfer is not far-end takeaway completion.
QA-UI-003 remains a confirmed Demo3 Engineering Frame Process visibility issue;
Immersive is a demonstrated workaround. Keep this distinct from controller
acceptance. Read `demo3-frame-process-visibility-review.md` and its seven hashes.

All84 historical archive parts have been independently tested/extracted into
an exact stored-path hash proof, now preserved on D under
`incremental-2026-10-23/archive-proof-20261023/`. This enables a byte-based final
raw-evidence delta; it does not itself upload that delta. The reviewed helper
and instructions are `tools/migration-backup-2026-10-22/incremental-closeout/`.
The final closeout must refresh native userdata, private root/descendant task
context, skill, Git and D with new receipts, then pause the goal/automation.

Before devising the final prioritized all-scene remediation plan, reconcile
the full remaining requirements and issue list. That ordering is now satisfied
by the compiled77-entry list and prepared `repair-plan.md`; do not repeat the
audit to satisfy this historical gate again. No full-application PASS, release
readiness, installed-TIA parity or live-PLC conclusion is established.

Later native closures:Q08 occupiedREADY/allred/committed vehicle, Q12 supported
stationary scanning/release, Q18 genuinely manual endpoint JOG rejection, and
the extra Demo5 applicationStop/held/Run-alone four-supported-carton/home/
step100 case. See their exact named reviews in the current checkpoint. Demo5
Memory-role retention differs from ENABLE loss; no extra machineStart was
needed after AppStop, while ENABLE restoration did require one.

Nativeuserdata currently has a real144-file versus Python97-file namespace
split. Direct native PowerShell staging of144files/33,592,160bytes plus an
independent Python readback passed at08:11UTC; that is only C staging until the
final D payload is verified. Final COPY_ALL includes the exact RootOwned
private extra. The reviewed staging helper and copy/readback procedure are
under `tools/migration-backup-2026-10-22/incremental-closeout/`.
