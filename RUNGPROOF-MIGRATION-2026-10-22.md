# RungProof migration backup - 2026-10-22

The user requested a complete Git backup, a reusable next-Codex handoff skill,
and a D: copy of all local RungProof work before wiping this laptop.

Start with [the continuation skill](skills/rungproof-playtest-handoff/SKILL.md).
It identifies the latest native test evidence, remaining failures, lessons and
restoration steps. The full 77-scene functional audit remains **incomplete**.
Application implementation remains at deadline backup
`41188c70bb86e3305f7cd7c8759f000f9c396729`; subsequent migration commits preserve
QA records, fixtures, skills and recovery tools.

## Local recovery copy

`D:\RungProof-Backup-2026-10-22` is on the separate physical USB SSD labelled
`Winter_Bkup`. `RungProof/` contains the canonical working tree, Git history,
ignored tools, build outputs and raw native captures. Saved Godot workspaces,
local supporting skills and private task context are preserved alongside it.
The old `PLC Visual Simulator` folder is separately preserved; its remote is
ControlsTrace and must not be published as RungProof.

The full regular-file copy passed SHA-256 comparison for 119,398 files
(26,349,194,104 bytes); native Godot userdata separately passed for 144 files.
Final handoff and Git changes are covered by the subsequent delta verification.
The D: Git copy passed `fsck --full`. Copied .NET and Godot executables launched
and reported their versions. This is backup/tool proof, not new application
acceptance. Consult D:'s `FINAL-BACKUP-STATUS.json` and its referenced reports
for the completed backup status and final source commit.

Eighteen internal audit-folder junctions are recorded as relative mappings;
their targets are backed up once. exFAT cannot store these aliases. Restore to
NTFS and use the skill's rehearsed junction helper. The main checkout has its
own assets and tools. Both native and distinct Python-observed Godot userdata
views were retained; native userdata is the application restoration source.

## GitHub recovery copy

Canonical repository: [mnwinter/RungProof](https://github.com/mnwinter/RungProof).
Source, curated evidence, execution records and this skill are on `main`.
[The laptop-migration release](https://github.com/mnwinter/RungProof/releases/tag/backup-2026-10-22-laptop-migration)
contains 84 multipart archive volumes totalling 6,494,497,278 bytes, plus
checksum, coverage and restoration metadata. All 84 remote archive sizes and
server-provided SHA-256 digests matched the local manifests. Each archive
family passed 7-Zip integrity testing.

Raw image, evidence-record and local-extra archives extract into the checkout.
The content-addressed toolchain archive preserves 101,062 original paths using
38,022 verified unique blobs. The skill's rehearsed restore helper verifies and
reconstructs independent files. A source-only clone omits these ignored tools
and raw captures. Follow the release's `RESTORE-PUBLIC.md` and `SHA256SUMS`.

This repository is public. Private task transcripts, Godot userdata and local
capability-bearing state remain in the complete D: backup. Original versions
of two publicly redacted QA files were separately preserved on D:. Public
scope and redactions are recorded in the release metadata.

## Restore after reinstall

Open D:'s `RESTORE-FIRST.md`, verify the final status, and copy `RungProof/` to a
writable NTFS path. Read the skill before resuming tests or repairs. Re-establish
screen control and validate the restored environment; historical test results
are not new-machine acceptance. The parcel-count stall and misleading
diagnostics footer remain open in the handoff.

C: originals were retained. Disconnect and preserve the D: USB SSD during the
laptop wipe. Do not erase all disks.
