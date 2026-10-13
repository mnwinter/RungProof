# Task 2 independent code review

Verdict: **REVISE**. Reviewed the atomic-edit specification, design, command ledger, transaction/symbol/array helpers, operand widget, shell staged-form wiring, and native transaction verifier. Source review only: no builds or tests were run by this reviewer. Root reported build 28 clean, integration 28 with 285 passes, and transaction 09 with 28 native assertions; these are bounded evidence, not proof of the missing combinations below.

## Findings

### T2-R1 — P1: Navigation mutates the document before pending changes are resolved

`src/App/SimulatorShell.cs:6842` in `NavigateReference` calls `document.SelectBlock(blockIndex)` before selecting the tree row or invoking any pending-form resolution. `NavigateValidationIssue` does the same at line 8416. `LadderEditorDocument.cs:379` increments the document revision on block selection. Thus, with an instruction/tag/block form pending in block A, activating a search result or diagnostic for block B immediately changes the active block and stales the captured edit. The subsequent tree/canvas signal may show the pending dialog, but Apply now rejects the formerly valid edit; Cancel cannot restore the original revision or active block. The task diagnostic route also calls `LoadTaskControls` directly (shell line 8466), overwriting pending schedule fields and clearing their dirty flag before the tree notification.

Resolve the entire navigation operation before any block, selection, or form mutation. Capture the destination by stable identity, then perform it inside the continuation; cancellation must leave the original document revision, active block, controls and exact selection untouched.

Regression: stage a valid property edit in A; activate a cross-reference in B; Cancel must retain A, the same revision and pending values, and subsequent Apply must succeed. Repeat through a compiler diagnostic. Stage task A fields and navigate a task-B diagnostic; verify both Cancel and Apply paths preserve the appropriate values.

### T2-R2 — P1: Apply of multiple pending forms partially commits and then rejects the remainder

`src/App/SimulatorShell.EditorTransactions.cs:33-35` applies pending forms one at a time. Shell lines 8323-8335 allow independent tag, block, task and interface forms to become pending with the same original document revision. For example the block name and interface parameter fields share the project page and can both be changed without a navigation prompt. Applying the dialog commits the block form first, which records history and increments revision. The interface form then fails `CurrentForm(interfaceFormRevision)` at shell line 7844. The continuation (including Save) is canceled, but the block edit is already committed, and the valid parameter draft is now impossible to apply without discarding/reopening. The analogous property/tag combination has the same problem. This is not a stale external edit; the resolver itself invalidates its own pending forms.

Either enforce one staged form at a time before staging a second, or preflight and commit a coordinated batch with explicit target identities and rollback-safe semantics. Do not simply ignore revision checks or refresh revisions after unrelated mutations.

Regression: change block name and enter a valid new interface parameter, invoke Save, choose Apply. Both intended edits must commit and Save proceed, or the whole resolution must reject before document/history mutation. Add a second case with an invalid second form and assert no first-form partial commit. Include mixed TIA/Logix pending forms because the resolver registry spans both editor environments.

### T2-R3 — P2: Discard can leave discarded field values visible and subsequently applicable

`SimulatorShell.EditorTransactions.cs:41` discards forms in registration order. `DiscardBlock` (shell line 8329) clears its flag and calls `RefreshEditor`, but `RefreshEditor` (line 5509) returns while another task/interface form remains dirty. A later `DiscardInterface` (line 8331) clears its own fields/flag without refreshing the block controls. Therefore, with both a changed block name and a new interface parameter pending, Discard during Save clears the dirty flags but leaves the rejected block name in the textbox. Pressing the block Apply button afterward renames the block using the supposedly discarded value; its stale check only runs when the dirty flag is true (line 7795).

After all pending forms are discarded, refresh every affected form from its original/current stable target, or make each discard restore its own fields independently of the global refresh suppression. Fields and dirty flags must agree.

Regression: stage block name plus interface parameter, invoke Save and choose Discard. Assert original block name is restored in both model and textbox, all pending flags are false, and clicking Apply cannot resurrect the discarded name. Repeat with other forms whose refresh is currently suppressed.

## Positive observations and remaining qualification

- Detached instruction and operand transactions validate before history mutation; invalid property edits remain staged.
- Typed operand suggestions use explicit scoped references in Parameterized blocks and writable filtering for output targets.
- Absolute-index array resizing preserves retained values; type reset requires explicit confirmation and malformed values remain editable.
- Save/Save As entry points resolve pending forms; save-path/baseline updates occur after successful file writing.
- Existing tests cover single-form pending/cancel/stale cases, but not the cross-form resolver combinations or navigation-before-resolution paths above.
- Actual viewport-edge inline placement, physical Enter/Escape/keyboard-menu behavior and final exported workflows remain root qualification gates; this source review does not certify them.
