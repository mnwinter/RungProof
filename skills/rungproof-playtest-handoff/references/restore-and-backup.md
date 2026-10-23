# Migration and restoration

The user requested all RungProof work backed up to Git and transferred to D:
before wiping the laptop. The migration root is
`D:\RungProof-Backup-2026-10-22`. Start with its verified `LATEST-CHECKPOINT.json`
and named later immutable receipt/restore addendum. Historical root
`FINAL-BACKUP-STATUS.json`/`RESTORE-FIRST.md` deliberately describe the d0c4566b
base; they are not proof that later work was included. Check exact manifests,
copy/hash reports and repository migration records. This skill alone is not a
verified-copy claim.

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

The demonstrated namespace difference is144 native files versus97 Python files. An early immutable native D backup now passed144 files/33,592,160 bytes; it is no longer merely C-only staging. The immutable COPY_ALL and independent canonical native verification subsequently passed; consult the final index for current-file addenda. Launching
PowerShell from Python inherits the Python view. Later native copies may be
staged in a receipt-owned `private-owned-launch/native-userdata-*/userdata`
tree inside the immutable COPY_ALL snapshot. Its private native receipt has
the full logical relative-path/size/SHA set; independent Python and D readback
must match it. Prefer the latest verified native payload for application
userdata. Keep the separate Python-view history rather than overlaying its
different bytes blindly. The staging scripts are migration helpers for the old
workstation, not new-machine restoration commands.

COPY_ALL receipts contain independent regular files under
`private-snapshot/TREE/RELATIVE_PATH` and a separately verified Git bundle for
the receipt's source commit. Restore Git from that matching bundle, overlay
the receipt's `RungProof` tree into the matching checkout, then restore the
documented junctions/private extras. The flat D mirror is convenient but later
updates must not redefine an older immutable snapshot. Check portable receipt
paths and any final metadata addendum; never mix one payload with a different
commit's Git history and call it an exact restoration.

Preserve C: originals until a verified backup and restore check are complete.
The D: volume was a separate physical disk, exFAT, labelled `Winter_Bkup` at
migration time. A future reinstall must preserve that disk/volume; a full
all-disk erase would destroy this copy as well. Do not infer another device's
drive letters from these historical values.

## Restore on the next machine

For GitHub-only restoration, clone the latest `main` first. Raw images,
evidence records and local extras extract into their repository-relative
paths. Toolchains use the smaller content-addressed archive:
`rungproof-toolchain-content.7z.*`. Download every numbered volume and extract
the first into a temporary folder. Preview
`scripts/restore-toolchain-content.ps1 -Root <checkout> -BundleRoot <extracted/public-toolchain-content>`
from this skill, then use `-Apply` to recreate all original toolchain paths.
It verifies blob/destination hashes and refuses to overwrite different files.
The map retains every path while identical contents are stored once. Restore
junction aliases afterward. The full normal folder copy on D: needs no blob
expansion. The final release checksum/index identifies the exact archive set.

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

## Final current-files restoration

Read LATEST-RESTORE-FIRST.md and the VERIFIED LATEST-CHECKPOINT.json on the backup
root. They name the exact final source commit, all-refs bundle, immutable parent,
chained final receipt and their SHA256 pins. The later current-file receipt owns
changed bytes and references only immutable parent copies for unchanged bytes.
Old C provenance paths are not required on a replacement computer.

For a D restoration, clone the matching final bundle with --no-checkout into a
NEW NTFS target containing only .git. Preview scripts/restore_selected_tree.py
with --incremental-root, --receipt-relative, --receipt-sha256, --tree RungProof,
--target, --protected-root (entire backup root) and --expected-head from the final
index. The preview validates every selected source hash. Apply only to that same
new target after a successful preview. The helper refuses existing ordinary files
and protects the backup. Then git read-tree HEAD establishes the matching index.
Restore the recorded aliases separately after ordinary files, on NTFS.
Use a short destination path and verify long-path support for the replacement
OS/Python/Git/7z tools; old-OS long-path rehearsals are not new-OS proof.

For public GitHub recovery, keep raw historical/base/delta extraction in separate
staging. Verify the new delta with scripts/verify_public_delta.py, explicit receipt
and 7z executable SHA256 pins, and a NEW workroot. It tests/extracts and hashes the
actual complete file set. Its PASS is local package proof; remote verification
has a separate receipt. Preserve the final Git tracked source when merging older
raw staging; do not blindly overlay older tracked metadata onto newer main.
The historical content-addressed toolchain still needs its path-map restore step.

Use the private companion release for original redacted state, selected task
context, native userdata and external recovery work. It complements the public
Git/base/delta artifacts; it is not the complete public application by itself.
Use its plain verifier/support scripts and pinned private receipt to test a fresh
download without old D/C paths. Follow the final index for all required assets.
Do not automatically extract intentionally malformed nested negative-test archives.

The current native userdata corpus is 144 files, not the separate Python97 view.
Prefer the final verified RootOwned native userdata payload and review old paths
before copying to the new user's Godot app_userdata folder. Install the complete
nine-file continuation skill and full PowerShell7 runtime (not pwsh.exe alone).
Put PowerShell7 on PATH for normal CMD launch. RungProofPowerShell is an existing
explicit build property, not an automatic CMD setting.

D reported exFAT Warning/Full Repair Needed/dirty while hardware reported Healthy.
No repair was performed and no cause inferred. Preserve C originals and independent
cloud recovery; use the final status for the last check. The 15-gate isolated main
build and 12-gate bootstrap were performed on the old OS. A full clean-machine
engine/import/GUI restoration remains untested.
