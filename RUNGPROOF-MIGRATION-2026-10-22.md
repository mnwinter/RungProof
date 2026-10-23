# RungProof migration backup — 2026-10-22

User requested a complete Git backup, a reusable next-Codex handoff skill, and
transfer of all local RungProof work to D: before a laptop wipe.

Start with `skills/rungproof-playtest-handoff/SKILL.md`. It links the latest
test evidence, confirmed unresolved problems, lessons and restoration workflow.
The full77-scene functional audit is **incomplete**. Source was frozen at
`41188c70bb86e3305f7cd7c8759f000f9c396729`; this migration adds documentation,
skills, fixtures and evidence rather than a new application repair.

Local migration root: `D:\RungProof-Backup-2026-10-22` on the separate physical
`Winter_Bkup` disk. The complete source tree with Git internals, ignored local
tools and raw captures is copied under `RungProof/`. Runtime workspaces,
supporting skills and the separately identified legacy folder are preserved
alongside it. The legacy `PLC Visual Simulator` remote is ControlsTrace and is
not published into RungProof.

The repository is public. Credentials and local capability/session state are
not suitable public Git data. Local originals are preserved on D:; GitHub
source/curated evidence and archive manifests describe the public backup scope.

Backup operations are in progress until the final status below is written.
Use D:'s `RESTORE-FIRST.md`, copy/hash reports and archive checksum manifest as
the authority for verified copy claims. Preserve D: when reinstalling: wiping
all disks would also destroy the local backup. C: originals are retained.
