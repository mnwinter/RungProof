# Task 2 implementation checkpoint

Implementation in progress; not accepted and not a native ergonomics pass.

## Implemented foundation

- Detached `LadderEditTransaction` preserves the source document/history on typing, blur, cancellation, validation rejection, mutation lock and stale document revision. Commit restores a validated full draft with one undo entry.
- `AggregateArrayDraft` stores pending values by absolute PLC index and retains malformed text. Element type changes require an explicit reset; the aggregate form keeps changes staged until its owning tag is applied.
- `LadderOperandSymbols` derives typed choices from global declarations, expanded aggregate leaves and supported read-only instance members. Legacy interface metadata is excluded. Parameterized declarations use stable `global:` / `local:` identities through Task 7 binding helpers; Input formals and instance members remain read-only.
- F2 uses `LadderOperandEditor`: suggestions, typed validation, Enter/Apply, Escape/Cancel, explicit create-and-assign. Undefined member names cannot accidentally create aggregate leaf declarations.
- Instruction property forms collect complete draft values and apply together. Dropdown and preset changes no longer write the document immediately; focus loss does not commit.
- Existing tag/block/task/interface forms now participate in pending Apply/Discard/Cancel resolution. Save, Save As, destructive project actions and selected navigation routes share the resolver. Navigation captures its intended destination before Apply can refresh controls; canvas and tag-row cancellation restore selection.
- Flat contact, output and network Cut preserve the prior clipboard on rejection. Existing recursive subtree Cut is reused.

## Evidence and open work

Root recorded `transactions-red.log`, `operand-symbols-red.log` and the failing scoped-identity regression before implementation. Transaction routes passed through 22 cases in root build 22; build 24 was clean, and legacy editor verification narrowed to monitor attachment and numeric destination regressions. Those have source fixes pending root rerun. Current transaction hook contains 28 route assertions, including invalid complete-form Apply, stale tag rejection, no phantom draft after tag deletion, project New cancellation, tag navigation cancellation, and aggregate absolute-index retention/type-reset cancellation.

Numeric palette insertion now selects a real writable declaration instead of a preserved stale property entry. Existing invalid operands remain visible for explicit correction; insertion cannot treat that UI preservation as a declaration.

The `VerifyEditorTransactions` hook is synthetic native Godot routing evidence only. Physical keyboard/pointer acceptance and rendered readability remain required. Root owns verification, native inspection and acceptance. Save As cancellation/failure and organization-tree visual restoration remain explicit review areas; this checkpoint is not a claim of whole-editor completion.

## Independent review revisions

- T2-R1: reference and diagnostic navigation resolve pending edits before touching active block, revision, task controls or canvas selection. Destinations are captured before resolution using block/task/network identities. Native regression now covers reference and block diagnostic Cancel followed by successful Apply, and task diagnostic Cancel/Apply paths.
- T2-R2: multiple independent pending forms cannot be combined by sequential Apply. The dialog explains this limitation and disables combined Apply; the handler also rejects before any mutation. Drafts remain pending. Native cases cover valid forms, an invalid second form, and mixed TIA/Logix drafts.
- T2-R3: all editor controls refresh after the entire discard batch clears its pending flags, preventing discarded values from remaining applicable. Root build 34 recorded the expected red DiscardRestoresAllVisibleFields after 40 successful assertions; source correction awaits rerun.
- Native F2 finding: the operand panel now lives in a separate CanvasLayer outside the scrolling ladder, anchors in viewport coordinates, and moves above the operand when needed. Viewport bounds are clamped. Root physical visual reinspection remains required.

Root build 35 recorded 42 passing behavior assertions for the review fixes. The synchronous placement assertion was invalid because headless used a 64x64 viewport before layout; it was moved to a separate async valid-viewport hook, `VerifyOperandEditorPlacementAsync`, which awaits layout and inspects the panel and its actual controls. Root subsequently observed the native build-35 F2 panel fit above the contact with all controls visible, but found background bleed-through. A solid opaque panel style and readable padding were added; this final visual change still needs root reinspection.
