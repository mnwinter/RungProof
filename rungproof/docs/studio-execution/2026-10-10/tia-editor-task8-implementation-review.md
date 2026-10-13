# Task 8 implementation handoff

Implemented against the approved semantic contract; runtime/compiler unchanged.

## Source ownership

- `src/VirtualController/LadderCallAuthoring.cs`: detached transaction; explicit conversion; global/formal identity persistence; typed binding diagnostics with stable field IDs; FB instance create/rename; one Apply and one Undo.
- `src/App/LadderCallEditor.cs`: production modal CALL form, target/mode action, explicit conversion, compatible typed actual selector, Input literal, Output discard, FB instance picker/create/rename, exact-formal error focus, Apply/Cancel/Escape.
- `src/App/SimulatorShell.CallAuthoring.cs`: production route, pending-form integration, block conversion control, native control-signal verifier and full-shell review fixture.
- `tests/CallEditorTests.cs`: nine cases, including all five Task 8 named plan cases.
- Localized `SimulatorShell.cs` integration: CALL properties entry; supported interface choices and ENUM domain field; parameterized section/type validation before mutation; stable interface IDs; honest scope copy; enum field pending/discard/Escape handling.

## Design decisions

Conversion is explicit and staged. Existing declared operands become explicit `global:<stable-id>` references, including aggregate leaves and timer/counter members; same-name formals never capture them. Unresolved operands remain compiler diagnostics. Other call sites retain their old mode and require an explicit target-mode action plus bindings/instance; no actual or instance is inferred. Existing global identity fallback is persisted as its ID, preventing existing qualified references from being broken by identity allocation. All new formals and instances receive stable identities. Instance renaming changes the display name only.

The CALL form owns one `LadderEditTransaction`; conversion, new identities, instances, and bindings commit together or are discarded together. The old target-only CALL property form is bypassed. Type options follow the defined simulator subset, not a claim of complete vendor compatibility.

## Evidence reported by root

- Initial five authoring tests failed at explicit not-implemented stubs; root integration30-red.
- Root integration31 passed 291 including initial five authoring tests.
- Root build32 clean; integration33 passed 298 including nine authoring tests.
- Root `call-authoring-01.log`: staged=True invalidRejected=True applied=True persisted=True undo=True cancelled=True; PASS.

Later small UI edits (mode mismatch action, genuine enum pending controls, pending-registration cleanup, Escape, missing-target selection, full-shell review fixture) require the root's next build/regression run. No build or UI process was launched by this implementation agent.

## Native qualification entry points

- `SimulatorShell.VerifyCallAuthoring()`: production Window controls via their emitted signals, not direct model-only acceptance. Root wired `--verify-call-authoring`.
- `SimulatorShell.SetupCallAuthoringReview(openForm: true)`: leaves full production TIA shell and CALL form open. `false` leaves selected CALL for physical context-menu Properties activation. Root owns Main hook, launch, capture and actual input.
- Fixture contains a typed FB with BOOL Input Pulse, BOOL Output threshold, Static COUNTER PV3 and two distinct Station A/B instances.

## Pending qualification

Independent source review; actual full-shell rendered capture/readability at 1280/1600; physical input; block-conversion/interface routes; exported end-to-end workflow. The 167-row ledger and whole-editor release qualification remain root-owned. This handoff does not claim full TIA parity or completion of Task 9.

## First independent review corrections

Review `tia-editor-task8-code-review.md` identified four defects; root reproduced four identity regressions red (`integration34-red`: 298 pass, 4 fail).

- Already-parameterized CALL opening now assigns identities in its detached transaction before choices are published. Scalar and aggregate bindings retain that identity through rename/reopen; Cancel preserves source bytes.
- Allocation reserves explicit roots and expanded aggregate leaf identities; a reused name gets a fresh identity rather than capturing an earlier declaration. Compiler structural `VC_CALL_ID` diagnostics block Apply instead of being filtered out.
- Binding error navigation targets the currently visible Symbol or Literal control, scrolls it into view and defers focus for two process frames after Apply pointer dispatch. Lifecycle guards drop pending focus if the modal closes.
- Escape is handled at the focused Window's `WindowInput` signal, ahead of focused control consumption. Visible selector popups retain their first Escape; the next Escape cancels the form.

Root `integration35` passed 302 after the four model corrections; build35 passed. A further runtime regression now verifies a new REAL global bound by reopening a typed FC CALL continues receiving 7.0 after rename/save/reopen. This fifth test and the last focus lifecycle guard await the next root run. Native physical focus/Escape correction still needs root evidence; it is not established by source handlers.

The full-shell fixture now requests deferred canvas refresh and resets its containing scroll position, with `CALL_REVIEW_CANVAS` geometry logged. Blank-canvas behavior reported after Cancel remains under root/renderer investigation until inspected.
