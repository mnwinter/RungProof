# Siemens TIA evidence workflow

## Authority and claim rule

For TIA Portal main-screen or property-management parity, start with
`rungproof-next/docs/TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md`; it owns the
current scope, requirement statuses, and work order. For LAD instruction or
programming semantics, start with
`rungproof-next/docs/TIA_INSTRUCTION_EVIDENCE_MATRIX.md`. Use
`TIA_PORTAL_PARITY_AUDIT.md` for linked Siemens sources, detailed history, and
exact verifier/capture routes. Treat dated chronology in these documents as
historical unless the current-state section confirms it.

Evidence has separate jobs:

- official Siemens STEP 7/TIA documentation: instruction semantics, pin roles,
  supported data types, instance/storage rules, and vendor terminology;
- source and automated tests: RungProof implementation and deterministic
  behavior;
- RungProof normal-renderer captures: actual current application appearance;
- installed-TIA captures with the same version, instruction, operands, settings,
  zoom, selection, and online state: parity comparison;
- hardware/bench observation: live transport and physical behavior only.

Do not use a RungProof screenshot as TIA evidence. Do not use documentation as
proof that the UI paints or edits correctly. If installed TIA is unavailable,
leave matched-comparison criteria OPEN and say so.

For a disposable normal-renderer screen-control pass, use the supported
`--app-shell --shell-view=ladder` launch when the startup acknowledgement blocks
input. This opens the Ladder workspace without changing the user's safety-notice
setting. Pin any local screen bridge to that exact process ID, use fresh
screenshots to calibrate the displayed image bounds and pixel-to-screen scale,
and keep image-local coordinates separate from browser-page coordinates. An
HTTP success response proves input delivery only; confirm the exact row
selection, heading, dialog, or form transition before recording a click as
successful. If a nearby control activates, stop and recalibrate rather than
inferring that the intended action is broken. On Windows, a bridge's synthetic
`WM_LBUTTONDBLCLK` message may only highlight a Godot tree row; use a native OS
double-click input for tree activation and verify the resulting dialog in a
fresh capture. Close the exact test process, bridge, tab, and listener
afterward. Current screen-control evidence also covers creating and renaming a
custom PLC tag table, showing/selecting a tag row, and Global DB member Add,
Cancel, Undo/Redo, and Remove. Do not infer staged tag-form transaction,
complete User Constants CRUD, tag-table deletion, Save/Load, or tag-to-operand
drag from those results; the 2026-10-20 acceptance-ledger entry lists the
still-open routes.

## Instruction change trace

For an instruction or editor workflow, inspect all affected layers:

1. catalog key, Siemens-facing name, operand roles, and data-type choices;
2. persisted flat and recursive representations and migrations;
3. compiler validation, address rules, and call/instance scope;
4. scan semantics, retentive state, faults, and edge/instance memory;
5. observation values and three-valued state (`true`, `false`, unexecuted or
   unknown as applicable);
6. property/editor transaction, cancel/commit, undo/redo, copy/paste, delete,
   keyboard, pointer, palette, and project-tree routes that apply;
7. unselected, selected, editing, enabled/disabled, fault, and online rendering;
8. wire continuity from the real power rail through branches and instruction
   leads;
9. tag-information modes, symbolic/absolute representation, comments, literals,
   unresolved `???`, and clipping at required zooms;
10. focused tests, headless gate, current normal-renderer capture, and matched
    TIA comparison.

Never infer one renderer key from a similar instruction. Timers, counters,
comparators, arithmetic, conversion, program-control, calls, contacts, coils,
and empty boxes have distinct contracts.

## Known high-risk details

- NO passes operand `1`; NC passes operand `0`; series is AND and parallel is
  OR. Monitor color must follow evaluated continuous flow, not isolated pieces.
- P/N edge contacts require unique writable memory and prior-scan state. Address
  reuse corrupts behavior. Unsupported legacy state is a migration question,
  not permission to silently change semantics.
- Assignment writes the current RLO; Set/Reset modify the target only when RLO
  is true and otherwise retain state.
- IEC timer/counter Q/ET/CV are pin values; do not invent synthetic member tags
  when the chosen TIA display mode hides supplementary tag information.
- Unassigned required operands display the unresolved state at the correct pin;
  do not manufacture a default value.
- Branches begin at and reconnect to actual network conductors. Test root,
  nested, non-root, multiple-open-leg, close, and deletion routes.
- A named CALL and an empty authoring box are different states. FC/FB insertion,
  instance DB identity, parameters, and project-tree gestures need atomic
  transaction and persistence coverage.

## Closing a matrix cell

Record the exact source URL/version, fixture/project, application commit,
settings/state, command, output artifact, and visual inspection. Change only
the cells supported by that evidence. A local regression may close a
RungProof-side cell while leaving installed-TIA comparison or physical editing
OPEN.

Overall wording remains `partial` until every required acceptance criterion is
closed. Do not turn elapsed effort, broad sweep counts, or absence of a known
failure into parity.
