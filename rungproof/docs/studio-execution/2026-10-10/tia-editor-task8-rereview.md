# Task 8 scoped independent rereview

Decision: **source corrections accepted; Task 8 acceptance remains pending native evidence**. No new blocking source finding in this scoped rereview. This is not approval of the entire editor or final candidate.

Reviewed `LadderCallAuthoring.cs`, `LadderCallEditor.cs`, the added `CallEditorTests.cs` regressions and the updated implementation handoff. No builds, tests or UI input were executed by this reviewer. Root reports integration35 passed 302 tests after four failing identity regressions were corrected. The additional execution regression awaits root's next run.

| Original finding | Source disposition | Evidence and remaining gate |
| --- | --- | --- |
| P1: newly added globals lose CALL bindings on rename | Closed in source | `LadderCallAuthoring.cs:20-21` now establishes identities in the detached draft when an existing parameterized CALL opens, before form options are built. Scalar/aggregate rename, reopen, Undo and Cancel regressions inspect binding resolution and unchanged source bytes. The new REAL output execution regression additionally checks the reopened binding receives 7.0; root must run it. |
| P1: reused names collide with existing identities | Closed in source | `LadderCallAuthoring.cs:31-50` reserves explicit root and expanded aggregate leaf identities, preserves existing IDs, and gives a colliding new declaration a fresh identity. `:130` retains structural `VC_CALL_ID` diagnostics instead of filtering them out. Regressions distinguish old/new declaration values, aggregate leaf conflicts, and rejected pre-existing duplicate identities. |
| P2: wrong actual field receives error focus | Corrected in source; native closure pending | `LadderCallEditor.cs:121-126` maps each binding to the visible Literal/Symbol control. `:132-145` defers focus across two process frames, scrolls to the control, selects invalid literal text and guards modal/control lifetime. Actual pointer activation, correction and resubmission must demonstrate focus on the intended control after event dispatch. A source `GrabFocus` call alone previously failed, so no native pass is inferred. |
| P2: Escape failed to dismiss modal | Corrected in source; native closure pending | `LadderCallEditor.cs:29,40-46` handles the focused window's input and preserves a visible dropdown's first Escape. Root must verify actual Escape from literal, selector, Apply and error states, with source/history unchanged after Cancel. |

The detached transaction and one-command history path remain intact. Identity allocation changes only the draft until Apply; Cancel does not migrate the live document. Existing IDs are not reassigned to resolve collisions. The new error focus path does not commit or alter actuals itself. No implementation code was changed during rereview.

Required closure: run the pending execution regression; perform the physical focus/Escape checks on the rebuilt app; retain their exact build/log/capture references. Full exported workflow, command-ledger coverage and final visual/capacity qualification remain outside this scoped source acceptance.
