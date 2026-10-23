# Current playtest and migration state

Project testing and hard-issue analysis have ended. The explicit cutoff is
2026-10-23 04:00 America/Los_Angeles (11:00 UTC). Only backup and handoff closeout
may continue afterward. Pause the goal after that closeout; obtain a fresh human
work order before resuming development. Old app/bridge handles are historical.

Canonical project: `mnwinter/RungProof`, branch `main`, application `rungproof-next/`.
The final application implementation remains `41188c70bb86e3305f7cd7c8759f000f9c396729`. The complete raw evidence
snapshot is tied to `db9eb2b293ef97553fa108509ca5d1a38b963fe0`. Later commits change handoff/backup metadata and
skills, without new application repairs.

The current QA directory is `rungproof-next/outputs/screen-audit-2026-10-23/`.
Read `scene-issue-compendium.md`, `coverage-reconciliation.json`,
`current-compilation-validation.json`, `repair-plan.md` and its challenge review.
All 77 unique IDs and 616 cells are accounted for: **610 SCOPED_PASS, 3 FAIL,
3 PARTIAL, 0 READY**. All 24 original finite cases have dispositions: 18 native
closures, five loaded-LAD lifecycle alias reconciliations and Q23 split.
An additional Demo5 AppStop/Run-alone case completed four supported cartons/home.

Confirmed issues: QA-PLANT-001 parcel Lab2.23 same-instance Reset/repeat (S2),
QA-UI-003 Demo3 Engineering Frame Process hiding equipment (S2), QA-UI-002 Lab2.1
stale outer footer (S3), and QA-SPEC-002 maintained-toggle wording in 2.1/2.6 (S4).
Radar inspection access remains a candidate; gallery empty-belt visibility remains
an observation limit. Six incomplete cells are not six independent defects.
The prioritized plan and hard causal analyses are ready; they are proposals.

The historical B5 deterministic suite was 424 passed/2 failed, and was not rerun
here. Source supports a stale Watch schema9 expectation against current schema10;
assertions after that failure still need execution. The TIA local evidence hash
is inconsistent with its summary; current C/D/historical bytes agree. Removing
the raw file would skip the assertion and create misleading green. Preserve it.
Read `post-freeze-handoff-checks.md` for exact evidence and focused runner filters.

Actual isolated main C# build passed 15 gates with 427 copied inputs, including
423 C# sources, and zero warnings/errors. A minimal bootstrap separately passed
12 gates. Both used only bundled local packages and empty caches on the old OS.
No engine/import/test/GUI or post-wipe proof follows. Main build used the actual
backed-up full PowerShell7.6.5 runtime; normal CMD launch needs PowerShell7 on PATH.

The immutable parent COPY_ALL now passed 124,150 files/24,563,710,597 bytes with
zero linked files. Independent direct-native verification passed all 123,822
canonical regular files, with the 18 recorded aliases excluded. The new 6,636-file
public delta passed actual fresh extraction and remote asset SHA256 verification:
https://github.com/mnwinter/RungProof/releases/tag/backup-2026-10-23-playtest-closeout
The earlier historical release and current source Git are still required.
Native userdata's early immutable D copy passed 144 files/33,592,160 bytes;
the distinct Python97-file view is preserved separately.

Final current-file/private-context/Git-bundle receipts are identified by the
latest VERIFIED `D:/RungProof-Backup-2026-10-22/LATEST-CHECKPOINT.json` and
`LATEST-RESTORE-FIRST.md`. Older d0/a2 receipts remain history. This source commit
does not certify its own later final snapshot. The companion private repository
is `mnwinter/RungProof-Migration-Private`; private transcripts/userdata/capabilities
must never enter the public repository. Check its actual final release receipts.

D's exFAT Winter_Bkup volume reported Warning/Full Repair Needed and a dirty bit;
disk hardware reported Healthy. No cause or repair is established. Keep C originals
until independent recovery checks pass. Consult the final D record for the last
volume check; matching file hashes alone are not a healthy-volume certificate.

The continuation skill has nine files, including `restore_selected_tree.py` and
`verify_public_delta.py`. Follow the restore reference and final index for exact
pins. Re-establish a supported screen session and new-machine environment proof
before further testing. Do not repeat the 77-entry menu sweep or reopen closed
cases from superseded prose. No whole-app, installed-TIA or live-PLC acceptance.
