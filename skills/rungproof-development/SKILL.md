---
name: rungproof-development
description: Develop, review, and verify the canonical RungProof Godot/.NET PLC simulator, including ladder editing, virtual-controller behavior, authored demos, Siemens TIA evidence work, industrial assets, and the guarded external-PLC seam. Use for changes or audits in the RungProof repository; do not use for ControlsTrace or unrelated PLC projects.
---

# RungProof development

Work from evidence and preserve the simulator's controls architecture. Do not
claim Siemens parity, live-PLC behavior, safety performance, or release
readiness from source inspection or offline tests alone.

## Establish the workspace

1. Read the repository `AGENTS.md`, `PROJECT_INFORMATION.md`, `CONTEXT.md`, and
   root `README.md` before changing code.
2. Confirm the Git root, branch, remote, and dirty state. The canonical remote
   is `mnwinter/RungProof` and the default branch is `main`. Stop if the remote
   identifies a different product. Do not create a branch unless the user asks.
3. Preserve unrelated dirty work. Never use broad clean, reset, checkout, or
   rewrite operations to prepare the tree.
4. Treat `rungproof-next/` as the active application. Treat `prototype/` as
   retained contracts/evidence, not a second implementation to revive.

Read [references/project-map.md](references/project-map.md) when locating code
or choosing the governing project document.

## Preserve the non-negotiable model

- The scanned ladder program owns machine sequence decisions and actuator
  commands. Plant models integrate commanded motion and publish measured
  feedback; they must not hide a process sequencer behind a single command.
- Keep input/feedback, command, permissive, interlock, status, and fault signals
  distinct. Keep point ownership explicit: PLC-owned values drive the plant;
  PC-owned values report simulated feedback.
- Keep application playback independent from PLC connection state. `Stop`,
  `Reset`, and sequence completion do not disconnect. Readiness loss stops and
  latches playback; recovery requires a fresh operator `Run`.
- Learner-facing machine controls are `Run`, `Stop`, and `Reset`. Never invent
  or require `stepPassed`. Simulated safety-trip/E-stop inputs are teaching
  devices, never safety-rated claims.
- The virtual controller is the normal offline execution path. Tests must not
  contact a plant. The guarded Python/Snap7 bridge remains the only real S7
  transport path and may operate only through validated profiles, exact
  read/write ownership, explicit operator authorization, and PLC watchdogs.
- Never invent PLC addresses, pin mappings, Siemens behavior, instruction
  variants, or live-machine results.

Read [references/controls-and-safety.md](references/controls-and-safety.md) for
demo ownership, external-PLC, and commissioning boundaries.

## Make changes

- Trace the current path from saved project/schema through compiler/runtime,
  observation state, presentation, and renderer before editing a ladder feature.
  A UI-only patch is insufficient when serialization or execution semantics
  change.
- Make the smallest coherent change that preserves saved projects and existing
  working behavior. Add migration or backward-compatibility handling when a
  persisted schema changes.
- For a demo, trace each visible control through its symbolic input, accepted
  scan, output command, plant step, measured feedback, and next scan. Reject
  decorative controls and hidden plant sequencing.
- For asset work, do not promote primitive or unreviewed geometry. Follow the
  source-to-GLB, scale, collision, provenance, help, render, and independent
  recognition gates in `rungproof-next/docs/ASSET_QUALITY_STANDARD.md`.
- Update the narrow governing document when a conclusion, acceptance state, or
  workflow changes. Do not append another broad chronology when an existing
  matrix or focused document owns the fact.

## Handle Siemens TIA work

Read [references/tia-evidence.md](references/tia-evidence.md) before any TIA
fidelity, LAD instruction, project-tree, online-state, or parity task.

Use `rungproof-next/docs/TIA_INSTRUCTION_EVIDENCE_MATRIX.md` as the acceptance
ledger and `rungproof-next/docs/TIA_PORTAL_PARITY_AUDIT.md` for detailed
rationale and recorded evidence. Re-check current cells; never carry a past
PASS to changed code without rerunning its evidence.

Official Siemens documentation establishes semantics and supported forms.
RungProof captures establish RungProof behavior. Matched installed-TIA captures
establish visual/workflow comparison. These are different evidence classes.
Do not mark overall parity while any required cell is OPEN, PARTIAL, failed, or
based only on simulator-side evidence.

## Verify proportionally

Read [references/verification.md](references/verification.md) before selecting
checks. At minimum for application code, from `rungproof-next/`:

```powershell
& '.tools/dotnet/dotnet.exe' build RungProof.Next.csproj --no-restore
& '.tools/dotnet/dotnet.exe' run --project tests/RungProof.Next.VirtualController.Tests.csproj --no-restore
& '.tools/godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe' --headless --path . -- --verify-app-shell
```

Restore first when packages or the pinned toolchain are absent. Add the focused
headless audit for the changed subsystem. For visible UI/3D changes, also run
the normal renderer, capture the exact state, and inspect the image; headless
success is not visual proof.

Report exactly what ran, what passed, what was not run, and the remaining proof
boundary. Do not use a historical test count as the expected current count.
