# Task 8 independent code review

Decision: **REVISE**. Reviewed the frozen CALL authoring implementation against Task 8 and the semantic contract. No builds or tests were run by this reviewer; root owns execution. Root reported integration33 (298 passes), build34 clean, and the six native authoring assertions passing. Those checks do not cover the following routes.

## 1. P1 — A newly added global loses its CALL binding on rename

Location: `src/VirtualController/LadderCallAuthoring.cs:20-29`, `src/App/LadderCallEditor.cs:85-93`.

Opening an already parameterized CALL constructs the detached draft without assigning identities. Identity assignment occurs only when the user selects a target or converts it. `LadderEditorDocument.AddTag` and `AddAggregateTag` still create declarations with empty IDs. The actual selector therefore creates a `global:<name>` binding through `GlobalId` fallback, and Apply accepts it without persisting that identity. A subsequent tag rename retains its empty ID, changes the fallback to the new name, and leaves the CALL actual pointing at the old name. This violates the stable identity contract through an ordinary editing route.

Regression: create and commit a parameterized CALL; add a new scalar global; reopen the existing CALL without changing its target; bind the new global and Apply; rename the global; assert the binding resolves to the same declaration and executes with the same value. Repeat for an aggregate leaf, save/reopen, Undo and Cancel. Allocate/persist identities within the detached edit before publishing binding choices, or allocate stable identities at declaration creation with compatible migration.

## 2. P1 — Name-based identity allocation collides after a legal rename

Location: `src/VirtualController/LadderCallAuthoring.cs:35-36,114-116`.

`EnsureIdentities` assigns `Id = Name` without reserving existing IDs. Convert a global named A (identity A), rename it to B (identity remains A), then add a new tag named A (empty identity). Selecting a parameterized target now assigns the new tag identity A too. The selector resolves both `global:A` references to the first matching declaration, so the row labeled with the new tag can refer to the old tag. The compiler detects duplicate identities at `$.variables`, but authoring validation filters that diagnostic out and permits Apply. A later full compile fails; the form must not commit an identity conflict that it introduced.

Regression: the sequence above, with different initial values on old and new globals, must preserve two distinct identities and binding targets through save/reopen. Include collisions between aggregate leaf identities and scalar identities. Preserve existing qualified references; do not repair a collision by silently retargeting them. Identity allocation must be collision-aware, and structural identity errors introduced by the transaction must block Apply with an actionable diagnostic.

## 3. P2 — Invalid actual navigation does not reliably focus the actual field

Location: `src/App/LadderCallEditor.cs:100-101,121,127-131`.

The field map stores the actual-kind selector for every binding, so missing or invalid Symbol/ENUM actual diagnostics navigate to Symbol/Literal/Discard rather than the offending value control. Root additionally reported physical build34 testing: entering `not_bool` and clicking Apply retains the form and shows the correct BOOL error, but focus remains on Apply. There is a `literal.GrabFocus()` call in source; its presence is not evidence that focus survives the button event sequence.

Regression: use physical or native input events, not only emitted Pressed signals; invalid BOOL literal, missing symbol, wrong ENUM member and stale symbol must leave the document/history unchanged and focus/scroll the relevant visible actual control. Verify keyboard correction and resubmission after focus settles on the next frame.

## 4. P2 — Escape does not dismiss the actual modal in the tested route

Location: `src/App/LadderCallEditor.cs:38-40`.

Root reported that physical Escape after invalid Apply in build34 left the CALL window unchanged. The only Escape handler is `_UnhandledKeyInput`, which does not establish that the embedded modal receives an unconsumed key after focused controls. This contradicts the advertised Cancel/Escape route. This is a root-observed native failure, not a reviewer-executed reproduction.

Regression: Escape from the literal editor, symbol selector (with popup closed), Apply button and error state must close the modal, discard staged conversion/instance/binding changes, and add no undo entry. A dropdown's first Escape may close its own popup; a subsequent Escape must dismiss the modal. Test actual input delivery to the embedded window.

## Positive source findings and limits

The detached transaction protects the source until Apply, checks stale revision and mutation lock, and commits one history entry. Existing formal and instance identities survive the reviewed persistence/rename paths. Explicit conversion qualifies declared global operands and does not capture same-named formals. Other call sites deliberately retain their old mode and receive diagnostics. Type/direction checks and output discard use the compiler's actual binding rules; unsupported FB/FC formal combinations have diagnostics. Interface STRING whitespace is preserved in the current root fix. Instance creation/rename and conversion remain detached on Cancel.

No source claim here establishes full native usability, runtime parity, final export acceptance or completion of the command ledger. The identity regressions and native keyboard/focus failures require correction and fresh evidence before Task 8 approval.
