# Planning baseline — 2026-10-10

- Canonical checkout: `C:/Users/mattn/OneDrive/Documents/ChatGPT/Siemens-PLC-PC-Interface`.
- Branch inspected: `main`; HEAD: `1aeee4d7ed23fa938f5227942e7727b4ef6981dc`.
- Old `RungProof` working directory contains only `.git` metadata; do not implement there.
- Working source contains substantial pre-existing modifications and untracked files, including offline-demo packaging/policy and UI/controller/help changes. HEAD alone does not identify the inspected application.
- `baseline-working-files.json` captures status and SHA-256 fingerprints of existing modified/untracked nonignored files. This is an inventory, not a recoverable preservation backup and not a frozen build.
- Planning may create documents only. Do not clean, reset, stash, commit, push, install, alter assets/source/config, or contact a PLC during planning.
- Implementation authorization must select a working-state preservation/integration strategy. A new worktree at HEAD alone would omit the WIP relied on by the plan. Obtain a recoverable byte-verified WIP snapshot and reconcile concurrent changes before implementation.

## Evidence boundaries

Root inspected applicable instructions, current documentation, Git state and focused source. No build, tests, runtime launch, visual qualification, export or live-PLC interaction occurred in this planning review. Existing verification reports are historical evidence attributed to their recorded artifact, not fresh proof.

`CONTEXT.md` includes a legacy Qt migration boundary. Prefer current Godot source plus the dated top-level handoff and demo review when determining current behavior; reconcile contradictory historical documentation explicitly.

The screenshot is a reference for accessible machine/logic puzzle presentation. It is not a requirements document or evidence of the other game's internal implementation. Controls-learners-first, conveyor-first and campaign progression remain proposals, not human-approved requirements.

## Architectural review finding

`VirtualControllerSession` and `Main.cs` must be inspected for scan/event ordering before challenge evaluation is integrated. `Main.cs` documents that `SnapshotPublished` precedes output projection. `SceneSimulationRuntime.Points` exposes a read-only interface to a mutable dictionary. A subscriber cannot assume either is an immutable post-plant observation. Define and verify a copied observation at a completed scan boundary; do not introduce a competing simulation clock.

## Acceptance and authorization

Departmental deep dives precede design synthesis. Cross-department critique precedes Dot's review. Dot may approve the quality/coherence of a plan while declaring human scope decisions and implementation/release evidence outstanding. Human authorization is required before implementation. No scheduled automation is authorized.
