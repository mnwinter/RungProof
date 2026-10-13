# Task 1 independent code review

Reviewed 2026-10-11. Scope: command routing, immutable targets, clipboard, diagnostics, named creation, dock/member navigation. Read-only product review; no build, UI execution, or product edits. Phase 2 staged-form deficiencies are excluded. Current source includes concurrent recursive-tree integration, so the integration findings below must be resolved before accepting Task 1 with that model.

## Findings

### 1. P2 — Selected output cannot be deleted

`rungproof-next/src/VirtualController/LadderEditorCommands.cs:88` permits Delete only for Network or Contact. The canvas and shell correctly identify a terminal as Output, but context Delete is disabled, and toolbar/keyboard Delete returns unsupported-target. This leaves a required instruction-deletion route nonfunctional. The recursive model now supports `HasTerminalAction=false`, so deletion should remove only the terminal action, preserve conditions and stable identities, and be undoable. Add output deletion/undo tests for coil, CTU and CALL and exercise all three routes.

### 2. P2 — Moving focus to another block invalidates otherwise-current diagnostics

`rungproof-next/src/App/SimulatorShell.cs:8289` rejects a diagnostic when `validationRevision != document.Revision`; `:8305` then calls SelectBlock while navigating a valid diagnostic. `src/VirtualController/LadderEditorDocument.cs:373` increments Revision whenever the active block changes. Therefore verifying errors in multiple blocks and activating the first error in a different block makes every subsequent diagnostic stale even though the program has not changed. Ordinary project-tree navigation does the same. Keep popup/selection invalidation separate from content/compile provenance, or otherwise preserve diagnostic validity across navigation without weakening invalidation after edits. Regression: verify errors in blocks A and B, navigate A/B/A, then edit and confirm stale rejection.

### 3. P2 — A rejected boundary move clears monitoring and invalidates challenge draft state

`rungproof-next/src/App/SimulatorShell.cs:7902` runs BeginEdit before Execute; CanExecute accepts MoveLeft/MoveRight for any Contact. Only `src/VirtualController/LadderEditorCommands.cs:123-126` checks the boundary. Pressing Move Left on the first contact thus clears monitoring, sets NOT VERIFIED, and emits ChallengeDraftChanged before returning boundary with no document/history change. Reject all predictable no-ops before mutation side effects, or apply those side effects only after Applied=true. Regression must compare loaded-match, monitor snapshot, challenge invalidation count, document and history, not only Applied.

### 4. P2 — Output paste omits terminal-presence state

`rungproof-next/src/VirtualController/LadderEditorCommands.cs:189-200` copies action fields but not `HasTerminalAction`. An imported/schema-2 network may have HasTerminalAction=false; its output position remains targetable. Pasting a copied CTU/coil/CALL there reports success and fills action fields, but BuildProgram omits the action because `src/VirtualController/LadderEditorDocument.cs:977` yields no actions while that flag remains false. Copy terminal presence with the payload (or explicitly set true for a valid action payload), and define copying an absent terminal as rejected rather than a fake action. Regression: import actionless network, paste real output, verify emitted action and undo to actionless state.

## Positive observations and limits

Targets capture revision, block/network/node identity, and insertion slot. Popup dispatch does not fall back to current selection after target expiry. Output-only clipboard leaves conditions separate; network deletion uses explicit role. Named creation checks blank/duplicate names before history, and cancellation dispatches nothing. These are source observations, not native acceptance claims.

The known watch-table undo native failure remains open under root investigation. Existing tests cover ordinary stale targets and output copy, but do not cover the four sequences above. No phase-wide acceptance is claimed. Line numbers reflect this review snapshot and may shift during concurrent integration.

## Scoped re-review after command fixes

Reviewed the four findings against the updated source (no builds/UI run by reviewer; root reports integration-11: 223 passing).

- Finding 2 resolved in source: diagnostics capture and compare ContentRevision; SelectBlock increments only popup Revision. Actual edits/history restoration still invalidate content provenance.
- Finding 3 resolved in source: boundary checks occur in CanExecute, and shell BeginEdit side effects now occur only after Execute reports Applied=true.
- Finding 4 resolved in source: CopyOutput carries HasTerminalAction, and copying/pasting absent terminal payloads is rejected. Paste/undo tests cover coil, counter and call variants.
- Finding 1 command routing is fixed: Output is accepted for Delete, removal clears HasTerminalAction, and conditions/identity/history are preserved. **End-to-end deletion remains open at this snapshot:** BuildNetwork gates only Coil on HasTerminalAction (`LadderEditorDocument.cs:1000-1027`), so a deleted timer/counter/CALL/other non-coil action still appears in executable IR. This is the known Task 3 terminal-presence overlap already assigned by root, not a new independent issue. OutputDeletePreservesConditionsAndUndo currently checks the flag and history but does not assert absence of emitted actions; add executable-IR/runtime assertions for every terminal family before closing it.

No other issue found in this narrowly scoped re-review. Native route and rendering acceptance remain separate.
