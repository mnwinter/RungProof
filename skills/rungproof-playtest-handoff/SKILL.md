---
name: rungproof-playtest-handoff
description: Resume the RungProof screen-driven playtest and repair handoff after the October 2026 laptop migration. Locate current evidence, distinguish remaining failures from untested cases, and restore the project without repeating the audit or trusting stale session handles.
---

# RungProof playtest continuation

Use the repository's existing `skills/rungproof-development/SKILL.md` for its
architecture and verification rules. This skill supplies the narrower audit
continuity that a fresh Codex session would otherwise lose.

## Start with the actual checkout

1. Confirm the Git root, `origin`, branch, status and latest commit. Canonical
   remote is `https://github.com/mnwinter/RungProof.git`; the handoff branch is
   `main`. Do not confuse the older `PLC Visual Simulator` folder or
   ControlsTrace with this product.
2. Read [current-state.md](references/current-state.md). Resolve its project
   paths relative to the confirmed repository root, not the old C: location.
3. For a restored machine, read [restore-and-backup.md](references/restore-and-backup.md).
   For resumed testing or fixes, read [lessons-learned.md](references/lessons-learned.md).
4. Read the current77-entry compendium,616-cell matrix and `repair-plan.md` in
   the October23 QA folder. The original24 finite cases are accounted for;
   six failed/partial cells are not six new tests. Reconcile latest reviews
   before rerunning anything. Preserve earlier evidence as history.

## Continue the user's actual work

The full objective was a studio-style audit of all **77 unique scene IDs**:
visible loading, exposed controls, normal result, Stop/Reset/restart,
permissive/fault recovery, repeatability, feedback and navigation. Five demo
aliases share scene identities; they are not five extra scenes. Overall
acceptance remains incomplete. The full issue list is compiled and the
effort/impact-prioritized repair plan is prepared. Preserve failures and
observation limits separately; the plan is not an implemented repair.

The user authorized a local screen bridge when native computer control was
unavailable, and later allowed fixes plus a timed Git backup. Do not treat
this as authorization to control a real PLC, publish unrelated private files,
or bypass new-machine access controls. The last application implementation was
frozen at the deadline backup; later work added QA evidence, not application
repairs. Establish the user's current work order before resuming source fixes.

Screen testing must send actual mouse/keyboard input and inspect real native
pixels. Scripts may batch those inputs and OCR captures; they must not change
simulator state through hidden APIs or substitute headless renders for clicks.
Use one UI input owner. Independent agents can inventory evidence or review
pixels without sending competing input.

Keep ladder sequence decisions and PLC commands in executable LAD. Plant code
integrates commanded mechanics and publishes measured feedback. Do not conceal
a sequence in renderer scripts to make a scene appear to pass.

## Evidence and handoff discipline

- Credit only the behavior actually visible in the cited original frame and
  input log. OCR misses and captions such as `final` are not application failures
  or proof of completion.
- Keep source/model experiments, deterministic tests, native behavior and
  live-equipment proof distinct. This audit contacted no real PLC.
- Prefer compact local OCR/cropped comparisons and original hashes over sending
  every full screenshot to the model. Keep originals recoverable.
- All process IDs, bridge ports, capability tokens, coordinates and window
  sizes in old logs are historical. Establish a new session; never replay them
  blindly or publish live tokens.
- Record results in the narrow current evidence report and update the remaining
  requirements, without rewriting the original evidence history. Do not mark
  the persistent goal complete while any required behavior is unverified.

Suggested companion skills: `rungproof-development` for the project;
`plc-tia-wincc` for controls semantics; `skill-creator` when changing this skill.
