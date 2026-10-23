# RungProof migration recovery

Canonical source: [mnwinter/RungProof](https://github.com/mnwinter/RungProof), main.
Start with [the continuation skill](skills/rungproof-playtest-handoff/SKILL.md).
The compiled 77-scene audit has 610 scoped-pass, three failed and three partial
cells out of 616. The prioritized repair plan is prepared; application repairs
remain at `41188c70bb86e3305f7cd7c8759f000f9c396729`. Later commits preserve evidence and recovery metadata.

## Locate the latest verified backup

Local backup root: `D:/RungProof-Backup-2026-10-22`. Use its VERIFIED
`LATEST-CHECKPOINT.json` and `LATEST-RESTORE-FIRST.md`, which name the final source
commit, all-refs bundle and portable chained receipt/pins. Historical root
`FINAL-BACKUP-STATUS.json`/`RESTORE-FIRST.md` describe the older d0 base. The a2
interim is historical as well. Never infer latest state from an older receipt.

The immutable db9 parent passed 124,150 files/24,563,710,597 bytes with zero linked
files. Independent direct-native verification passed its 123,822 canonical files.
The native userdata early D copy passed 144 files/33,592,160 bytes. The final
current-file addendum also preserves later metadata/skill/private context and
external recovery tools; its actual verified receipt is in the latest D index.
The flat D mirror is convenient; immutable receipts define exact restorations.

Public recovery requires current source Git, [the historical archive release](https://github.com/mnwinter/RungProof/releases/tag/backup-2026-10-22-laptop-migration)
and [the new raw delta](https://github.com/mnwinter/RungProof/releases/tag/backup-2026-10-23-playtest-closeout). All 84 historical archive volumes
were independently tested/extracted into a 110,504-path hash proof. The new 6,636-file
delta passed fresh extraction and remote asset SHA256 verification. Its raw commit
is `db9eb2b293ef97553fa108509ca5d1a38b963fe0`; later tracked source must take precedence over older raw overlays.
Use the skill's content-addressed toolchain, junction and package verification
helpers. Source-only clone omits ignored evidence/toolchains.

Private recovery: [mnwinter/RungProof-Migration-Private](https://github.com/mnwinter/RungProof-Migration-Private).
Check its actual latest release and the final D index for publication/verification
receipts. This private supplement preserves original private state, selected task
context, userdata and external recovery work. It complements public Git/base/delta;
never upload its assets into public RungProof. Keep exact private manifest pins.
The older PLC Visual Simulator/ControlsTrace folder is separately preserved on D
and is not part of canonical RungProof Git.

D is a separate physical exFAT SSD, Winter_Bkup. It reported Warning/Full Repair
Needed and dirty; hardware reported Healthy. No cause or repair is established.
Matching file hashes do not certify volume health. C originals are retained.
Use the final volume check and independently verified cloud recovery before a wipe;
preserve/disconnect that D disk during any reinstall, rather than erase all disks.

The isolated actual main build passed 15 gates/427 inputs/423 C# sources with zero
warnings/errors, and a minimal bootstrap passed 12 gates. These are old-OS compile
proofs, not engine/import/GUI or post-wipe proof. Normal source launch needs the
full Godot.NET/.NET10 tools and PowerShell7 on PATH. Preserve the whole backed-up
PowerShell7.6.5 folder/license. Rebuild and establish fresh native behavior after
restoration. Historical B5 full tests remain 424 passed/two failed.

At 2026-10-23 04:00 Pacific, project work stops. Only backup/skill closeout follows,
then the goal and cutoff automation are paused. Resume only on a fresh human order.
