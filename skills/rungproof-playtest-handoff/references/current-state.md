# Current state at the 2026-10-22 migration

## Provenance and entry points

- Repository: `mnwinter/RungProof`, `main`.
- Previous deadline backup: `41188c70bb86e3305f7cd7c8759f000f9c396729`.
- That snapshot contains the last application changes and curated QA evidence.
  The migration backup adds newer evidence, this skill and restoration records.
- Active application: `rungproof-next/` (Godot .NET plus integrated LAD editor).
- Project guidance: `AGENTS.md`, `PROJECT_INFORMATION.md`, `CONTEXT.md`, root and
  application `README.md`, `skills/rungproof-development/SKILL.md`.
- QA root: `rungproof-next/outputs/screen-audit-2026-10-21/`.
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

1. **Parcel Lab2.23 full batch fails.** V3 native attempt stalls at count1;
   the versioned diagnostic V4 LAD fixture reaches count2 then stalls with
   table alignedTrue, loadedFalse/clearFalse, transferTrue/inhibitedTrue and
   completionFalse. V4 is not a successful workaround. Read
   `postbackup-parcel-v4-isolation.md`, `postbackup-parcel-boundary-analysis.md`
   and `postbackup-parcel-review.md`. A pure actual-source model experiment
   reproduces gate over-admission with20mm queue gap,15mm clearance and18mm
   travel per accepted scan; floating-touch was ruled out for the authored
   geometry. This is cause evidence, not native observation of private positions.
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

The user switched priority to wipe-safe backup and a next-Codex skill. Do not
restart testing before verifying the migration/backup handoff. After restoring
and agreeing the current repair work order, address the parcel failure and
stale footer using their exact repros. Complete only genuinely outstanding
native cases, not another77-entry menu sweep or already-proved truth tables.
A concrete remaining screen case is drawbridge5.8: during actual descent,
remove exposed STOPPED acknowledgement, verify lowering stops/pose holds,
restore acknowledgement and observe home. Existing fixture:
`test-projects/fixture-lab-5-08-drawbridge-control.rpproj.json`.

Before devising the final prioritized all-scene remediation plan, reconcile
the full remaining requirements and issue list. No full-application PASS,
release readiness, installed-TIA parity or live-PLC conclusion is established.
