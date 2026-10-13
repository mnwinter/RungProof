# Task 2 independent rereview

Verdict: **REVISE — one narrow discard route remains**.

Scope: fresh source inspection of the changes addressing T2-R1/R2/R3, including the added production-route native verifier assertions. No build, test execution or physical UI operation by this reviewer. Root reports build 37 clean and transaction run 17 with 42 passing assertions, plus physical F2/create/assign/undo evidence. Those reported results were not independently rerun.

## Closed source findings

- **T2-R1 closed:** `NavigateReference` resolves pending forms before entering `NavigateResolvedReference` (SimulatorShell.cs:6836–6843). Search targets use stable block/network/tag identity. Diagnostic navigation captures stable destinations before resolving and performs block/task/control changes only inside the continuation (8426–8511). Cancel therefore does not first change the revision or overwrite task fields. Added verifier cases cover search cancellation and subsequent Apply, diagnostic cancellation and subsequent Apply, and task diagnostic Apply-before-navigation.
- **T2-R2 closed for the resolver:** `SimulatorShell.EditorTransactions.cs:33–44` checks the pending-form count before calling any Apply. Multiple forms reject as a whole with an explanation and no continuation. The dialog disables combined Apply and retains Discard/Cancel. This is explicit rejection, not coordinated batch support. The verifier asserts unchanged serialized document and no continuation.
- **T2-R3 closed for whole-dialog Discard:** the resolver now clears every pending form before refreshing all editor environments (`SimulatorShell.EditorTransactions.cs:50–54`), and the verifier covers both TIA/Logix block fields plus no resurrection after this route.

## Remaining finding

### T2-R3b — P2: Individual Escape discard still leaves a discarded block name applicable

`SimulatorShell.cs:8358` implements `DiscardBlock` as clearing `blockFormDirty` followed by `RefreshEditor()`. The newly added Escape route calls it directly from the block fields (`8378`). If an interface or task form is also pending, `RefreshEditor` returns at its existing global dirty guard, leaving the canceled block name/type displayed. Unlike whole-dialog Discard, this route does not subsequently clear the sibling forms and refresh all controls.

Reproduction: edit block name; enter a pending interface parameter; return focus to block name and press Escape. The block name should revert immediately, while the interface draft remains pending. Currently its dirty flag clears but the edited name remains. Pressing block Apply then accepts that name because the stale check is conditional on `blockFormDirty`. The same problem can occur when following the new dialog advice to cancel and discard extra forms before applying one.

Fix: restore the block form's own values directly under the loading guard when discarding, independent of whether other forms remain pending. Do not discard the unrelated interface/task draft or bypass the global pending protection.

Regression: `EscapeBlockWithSiblingPendingRestoresOwnFields`: stage block name/type and interface name; Escape in block field; assert block controls equal the original model, interface text and pending state are preserved, then direct block Apply cannot resurrect discarded values. Add a mixed-style variant if the other editor's pending forms also affect refresh suppression.

## Qualification boundaries

Opaque operand overlay rendering, physical context menus/viewport edges and final exported workflow remain root qualification gates. This report does not upgrade those gates based on source or synthetic signals alone.
