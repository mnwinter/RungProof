# Migration and restoration

The user requested all RungProof work backed up to Git and transferred to D:
before wiping the laptop. The migration root is
`D:\RungProof-Backup-2026-10-22`. Consult its final `RESTORE-FIRST.md`, manifests,
copy/hash verification reports and the repository migration record for actual
completed backup claims; this document alone is not proof of a verified copy.

## Local backup contents

- `RungProof/`: complete canonical working tree, `.git`, raw native QA captures,
  ignored `.tools` toolchains/caches and local build outputs.
- `Godot-userdata-RungProof/`: saved workspaces, review projects and application
  settings formerly under `%APPDATA%\Godot\app_userdata\RungProof`.
- `legacy-PLC-Visual-Simulator/`: preserved older folder separately. Its remote
  identifies `PLC_Code_Visualizer`/ControlsTrace, not canonical RungProof. Do not
  change or push that history as RungProof.
- `codex-skills/`: this continuation skill plus the existing project skill and
  relevant locally installed supporting skills, where listed in the manifest.
- `thread-visualizations/`: this thread's local visual artifacts if any exist.
- `backup-tools/`, manifests and reports: copy/archive/checksum evidence and
  restoration helpers. Private local state remains local.

Eighteen old audit folders use junctions to shared assets/toolchains. Physical
data is backed up once; `junction-map.json` records relative aliases. exFAT
cannot store Windows junctions. After restoring to NTFS, preview this skill's
`scripts/restore-junctions.ps1 -Root <checkout>` and use `-Apply` only when its
preview is correct. It refuses to overwrite existing paths. The main checkout
has its own tools/assets; the aliases belong to old audit checkouts. Expanded
logical file counts are larger than physical backup counts.

Installed Python and native PowerShell exposed different Godot AppData contents
during migration. Both views are preserved separately; native application
userdata was independently hashed with PowerShell. Use native
`Godot-userdata-RungProof` for the application. The Python-view copy is
supplementary evidence, not a replacement.

Preserve C: originals until a verified backup and restore check are complete.
The D: volume was a separate physical disk, exFAT, labelled `Winter_Bkup` at
migration time. A future reinstall must preserve that disk/volume; a full
all-disk erase would destroy this copy as well. Do not infer another device's
drive letters from these historical values.

## Restore on the next machine

1. Verify the backup manifests/checksums and identify the canonical checkout.
   Copy `RungProof/` to a writable local NTFS project directory, preserving `.git`.
   Git may request a safe-directory entry after owner changes; scope any entry
   to that actual checkout, never a blanket wildcard.
2. Confirm `git remote -v`, branch, log, status and refs against the published
   backup record. Do not reset away local recovered evidence. If using GitHub
   alone, clone `mnwinter/RungProof` and follow the recorded archive release
   instructions; source-only clone does not imply raw captures/toolchains exist.
3. Restore saved Godot userdata to the current user's matching app_userdata
   location if needed. Review settings and old absolute paths. Do not restore
   arbitrary old credentials, session tokens or auto-start helpers.
4. Install/copy `skills/rungproof-playtest-handoff` into the current Codex skill
   directory to make it discoverable, or explicitly read the repository skill.
   The required `SKILL.md` and its `references/` travel together. Existing
   project guidance remains authoritative for application development.
5. Use `rungproof-next/README.md` and `rungproof-next/docs/DEVELOPMENT_SETUP.md`
   to launch/build.
   Bundled Godot must be the .NET build. Restore NuGet packages when missing;
   old caches/virtual environments may embed user-specific paths and are
   preserved for recovery, not guaranteed portable installations.
6. Establish a fresh supported screen-control session and do a representative
   load/control/Stop/Reset/repeat check before continuing the audit. Rebuild and
   rerun appropriate tests on the restored machine; historical B5 results are
   not proof of the new environment.

The handoff does not authorize a real PLC connection or any external message,
deployment or unrelated repository mutation.
