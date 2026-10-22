# TIA parity batch execution plan

Updated: 2026-10-21

This is the current execution dashboard. `TIA_INSTRUCTION_EVIDENCE_MATRIX.md`
remains the requirement ledger and `TIA_PORTAL_PARITY_AUDIT.md` remains the
chronological engineering record.

## Audit result

- The catalog contains 54 keys: 50 Siemens-facing and four intentionally local.
- All 50 Siemens-facing rows remain `PARTIAL` because each status currently
  combines locally proven behavior with installed-TIA or live-PLC blockers.
- Local progress must therefore be tracked separately from vendor parity.
- The checked-in evidence registry now validates all 54 catalog keys, four
  stable state gates per key, local/vendor status separation, source scope,
  artifact references, and exact-instruction URL uniqueness. Artifact
  references, hashes, and exact executable witnesses now cover all 200 Siemens
  local gates. Installed-TIA same-state pairing remains incomplete.
- `.tools` is an ignored scratch directory. Current screenshots are useful local
  review evidence, but they are not a portable evidence archive.
- On 2026-10-21 the user selected installed TIA Portal V17 as the authoritative
  UI/workflow baseline. Historical V21 blocker statements are superseded; V21
  documentation remains reference-only where useful.

## Main-screen and property-management sequence

The detailed acceptance ledger is
[`TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md`](TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md).
Keep the work in this order and defer the demo-logic remediation batch until
the GUI/property exit criteria are closed.

| Order | Status | Exit condition |
| --- | --- | --- |
| Screen-control matrix for Global DB, PLC tags, Watch controls, and project Open | Partial | **Latest evidence (2026-10-20):** the fresh normal-renderer `interaction` profile passed all 15 commands; controller/editor tests and all interaction gates passed, including editor-control 7/7 (36 assertions) and project-tree 14/14 (103 assertions). Artifacts are recorded in `docs/tia/tia-local-run-evidence.json` and remain local/offline. Source gates verify User Constants type-compatible defaults and compact edit layout. A bounded current-source 1600×900 screen pass created a session-local scratch User Constant, opened that existing row, changed its name, applied the edit, reopened it to confirm the committed name, and used Undo to restore the prior value; the visible status reported `UNDO · UPDATE PLC USER CONSTANT · OFFLINE` and enabled Redo. The bridge repeated submitted text batches, so the probe spelling is excluded from text-input evidence, but the form transaction and history transition are visible. Earlier passes exercised User Constants Add, Edit-open, Cancel, Delete, Undo, and Redo and kept Apply/Cancel visible. Screen control also rejected invalid `%I0.8`, preserved the five-row Watch table, opened/canceled Save Workspace As, and showed the System Constants hardware-unavailable disclosure. A fresh native-input pass created, renamed, and deleted an empty custom table; Delete returned to Default without confirmation. A measured-image-local DB pass opened `+ MEMBER`, canceled `cancel_probe`, applied `applied_probe` as BOOL/FALSE, and verified Undo/Redo; a final Undo left the scratch DB without that row. Prior fresh screen passes recorded Demo 5 DB101/DB102's six-column grouped layout and the visible custom-table lifecycle. Current source fixes the Watch rejection-feedback ordering defect: rejected cells are restored before the persistent 14 px high-contrast message is published. A fresh 1600×900 `ladder-watch-format-feedback` capture shows the rejected BOOL `Float` format directly above the unchanged `Auto` row. The `ladder-watch-running` state then invokes the production Verify + Load and Run handlers; its regenerated 1600×900 frame shows Demo 3 running at scan 660 with live BOOL values, `SIMULATOR ONLY`, `NO PHYSICAL PLC`, and the full global transport visible after clipping the large project tree to the Ladder viewport. The file-backed `ladder-watch-persistence` route saves a named Watch table, destroys its in-memory content, and reopens it through the production project Open path; a fresh 1600×900 frame shows the reopened filename, table, rows, `Bool` format, `TRUE` modify value, persisted comment, and identity-preserved status. **Still partial:** installed TIA V21 pairing, staged-form prompts, a running-scan OS-level pointer walkthrough, and physical tag-to-operand drag. Watch row reorder/remove, Add All, supported `Bool` format, invalid-address feedback, unsupported-format feedback, simulator-only modify, normal-renderer running monitor state, and file-backed Watch persistence have local visible evidence. Source and offline gates cover valid same-scene Open preserving the running program/scan, Verify + Load replacing it, and rejecting unknown/mismatched scenes before mutation. The referenced manual scratch sessions remained PLC DISCONNECTED; the deterministic routes also used no physical PLC transport. |
| Tag-reference lifecycle integrity | Fixed in source; local regression suite passes | Symbolic CALL actuals block referenced-tag deletion and follow rename, including aggregate leaves, while literals, discards, stable IDs, and unresolved references remain unchanged. |
| Remaining declared property features | Partial | Tag-table add/rename/empty-delete/duplicate, block duplicate, watch-table lifecycle, and member/tag transactions have source coverage. Duplication regenerates stable identities; copied tag tables receive unique CPU symbols and intentionally lose I/O bindings/absolute addresses, while generated `SceneIO` is protected. User Constants are executable immutable LAD inputs, including typed literal lowering, read-only destination enforcement, rename following, referenced-delete blocking, and CALL Input selection; visible edit/Apply/reopen/Undo is now screen-verified in a session-local current-source project. Hardware-sourced System constants are now modeled as read-only project metadata with validated identity, type, value, comment, hardware provenance, history, and schema-10 persistence; the normal-renderer source gate passes. Watch-table file persistence now has a current normal-renderer Save/destroy/Open proof with stable table and row identities plus row metadata. Remaining work includes physical tag-to-operand drag and installed-TIA comparison. Watch monitor/modify was visibly exercised with an offline program loaded and no physical PLC. Keep undeveloped behavior disclosed. |
| Instance DB presentation and semantics | Layout locally verified; V17 editor captured; editing remains read-only | Pair the six-column grouped table with the installed TIA V17 `Pulse_Cycle_DB [DB6]` declaration/property capture; add overrides only as a complete identity, validation, persistence, history, and runtime-initialization slice. |
| Installed TIA V17 comparison | Partial; seven matched corrections rendered | V17 Project View, project tree, instance DB, PLC tag table, and Watch table are captured offline. The RungProof project dock now uses the verified 250 px V17-style default at 1600×900, hides duplicate OPEN/PROPS header actions, and preserves full elided identities in tooltips. The document and Global DB headers also hide redundant LOCATE/PROPS actions while retaining compact declaration editing controls. The Watch document removes duplicate table navigation/lifecycle controls and collapses absolute-address entry until requested; the global `SCENARIO` and `LOGIC EDITOR` switches retain their complete labels at 1600×900. The PLC-tag document similarly leaves table lifecycle in the project tree and selected-row Properties/Cross-reference on its context menu, retaining compact Add/Edit/Verify actions plus compact declaration selectors and a concise offline boundary. The instance-DB and User constants views remove duplicate permanent property/lifecycle controls while preserving double-click/right-click property access. `VERIFY + LOAD` is now scoped to Tags instead of appearing in the User/System constants views. Focused assertions and property-route captures pass. Continue same-object/same-state pairing using the controlled installed-TIA capture pack. |
| Demo sequence and ladder-owned safety logic | Offline acceptance complete; visual/TIA/live-PLC evidence remains separate | At the user's explicit revised work order, all five demos were reconciled before parity resumed. Fixture checks pass for all five; `--verify-demo-sequence` passes 25/25; `--verify-five-demo-learner` reports zero failures; and `--audit-palletizer` reports zero failures. Demo 3 is the conveyor/pusher cell, Demo 4 uses parameterized counter FB state in DB101, and Demo 5 calls three encoder FB instances in DB105–DB107 in addition to axis/gripper DB101–DB104. This is local offline evidence only. |

An independent screen-control retry after the latest calibrated DB1 pass tried
**Add new block** and **File** from the OB workspace. The bridge acknowledged
input delivery, but fresh captures showed no UI transition, so the agent stopped
without reaching DB or property actions. This retry is inconclusive input
evidence; it does not overturn the earlier calibrated DB1 member interactions.
The app, bridge, tab, and listener were closed without Save, Load, Run, or PLC
connection.

A subsequent native-input pass found that the helper's synthetic double-click
only selected the tree row; a real Windows double-click opened the block dialog.
The disposable DB member screen visibly confirmed Cancel, Apply, Undo, Redo, and
selected-row Remove with a surviving `KeepFlag` declaration. This confirms
RungProof-side behavior only. Tag/table and User constants controls, project
Save/Load and staged prompts, a running-scan OS-level pointer walkthrough, and
installed-TIA V21 pairing remain open. A separate deterministic production-
handler route now covers the local rendered running Watch state.

## Work lanes

| Lane | Batch | Exit condition | Boundary |
| --- | --- | --- | --- |
| A | Evidence registry and runner | Every Siemens-facing key/state names a stable gate, command, source, artifact hash, local status, vendor status, and limitation. One runner emits a consolidated result. | Does not create missing installed-TIA evidence. |
| B | Universal renderer sweep | All 50 Siemens-facing keys are rendered at 75/100/150%, selected and unselected, with representative tag modes; bounds, clipping, selector overlap, and wire endpoints are asserted. | Visual equivalence still requires matched TIA captures. |
| C | Physical editor/control sweep | Real viewport input covers all property-form families, nonnumeric operands, CALL drag/bindings, empty-box chooser, network/palette gestures, and project-tree mutations with Apply/Cancel/Undo/Redo. | Godot viewport injection is not OS-level human-input proof. |
| D | Local monitor gaps | Missing RT unknown, TP falling-input pulse, TAN near-singularity, SIN invalid input, and family-wide flow endpoints have named tests and captures. | Live S7 behavior and CPU-specific fault results remain external. |

Use no more than three audit/implementation agents at once. Give them
non-overlapping ownership of lanes A, B, and C/D; the lead integrates, runs the
full suite, inspects captures, and updates the matrix once per completed batch.

## Completed in this batch

`--verify-operand-placement` is now a consolidated physical-input gate rather
than only a numeric-pin check. It uses `Viewport.PushInput` and verifies exact
property-form dispatch for 13 families:

- contact and comparator;
- assignment coil;
- timer and RT;
- counter, LD, and counter reset;
- numeric;
- RET, JMP, LABEL, and CALL.

The same run still covers every native numeric data pin, all comparator operands,
timer/counter pins, coil output, and all supported type selectors. Headless
verification passed with every property-form result `True`. Build completed with
zero warnings and zero errors.

This closes broad local pointer-routing coverage only. Per-form physical field
changes, Apply/Cancel, stable identity, and Undo/Redo remain in lane C.

Lane A now has a checked-in registry (`docs/tia/evidence-registry.json`) and a
bounded runner (`tools/Invoke-TiaParitySuite.ps1`) with `fast`, `interaction`,
`visual`, and `full` profiles. The final interaction batch result at
`.tools/tia-parity-interaction-final-v2.json` passed the build, all 368
controller/editor tests, transaction and editor routes, the physical operand
and project-tree sweeps, empty-box checks, and TIA/Studio block view-state
restoration. Its result explicitly records `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

The runner also exposed and corrected two verifier defects: TIA project-tree
selection was being mistaken for block activation, and the view-state check was
being run on an unsupported headless renderer. The real TIA activation path is
now shared by the double-click handler and verifier, including pending-form
protection; accidental headless invocation fails immediately instead of
hanging. The view-state command runs on the bounded native 1600x900 path.

The visual result at `.tools/tia-parity-visual-final-v2.json` passed and its
1600x900 nested-ladder capture was visually inspected. That inspection found
and fixed a missing positive-edge memory operand in the review fixture; the
regenerated image shows the explicit `Start_request_edge_memory` operand with
continuous, unclipped branch and coil geometry. This is local rendered evidence,
not an installed-TIA match.

Lane B now has one fail-closed production-canvas sweep instead of separate
mnemonic-specific probes. `--verify-tia-renderer-sweep` derives the exact 50
Siemens-facing catalog keys by excluding only the four documented local
instructions, constructs and compiles a representative document for every key,
and executes 900 states: three zoom levels, selected and unselected, and three
representative TIA operand/tag-information modes. It checks selection identity,
network containment, measurable editor-field overlap, conductor exposure, and
zero terminal/body clearance. The FB CALL witness has Input, Output, and InOut
bindings plus an assigned instance DB; the RT witness reuses its owning timer's
instance DB.

The sweep exposed two shared production defects instead of producing 50
one-off fixes. EXPT and CONV used the standard numeric half-width for their
conductors even though their bodies are wider. NORM_X and SCALE_X placed three
22-pixel editor rows on a 20-pixel pitch inside a 112-pixel body. The production
geometry now uses the actual EXPT/CONV widths and gives NORM_X/SCALE_X a
28-pixel row pitch in a 144-pixel body. The focused sweep passes all 900 states.

The consolidated interaction result at
`.tools/tia-parity-interaction-renderer-sweep.json` passes all nine commands,
including all 368 controller/editor tests and the universal renderer sweep. The
renderer command owns exactly 100 unique registry gates: offline and selected
for each of the 50 Siemens-facing keys. The result explicitly records
`offlineOnly=true`, `installedTiaExecuted=false`, and `livePlcExecuted=false`.
The regenerated visual result at `.tools/tia-parity-visual-renderer-sweep.json`
also passes; original-resolution inspection of its nested-network capture found
continuous branch joins, an explicit unclipped edge-memory operand, a bounded
selection, and connected coil/termination geometry.

`docs/tia/TIA_INSTALLED_CAPTURE_PACK.md` now defines four controlled human
capture sessions with required TIA build, CPU/firmware, zoom, display settings,
archive hash, state, filenames, and manifest fields. It requires no production
PLC, download, force, or commissioning activity.

The visual lane now emits two deterministic audit sets from the production
canvas, one selected and one unselected. Each set contains 150 full-resolution
instruction details (50 keys at 75/100/150 percent), 24 family contact sheets,
and a SHA-256 manifest. The generator fails closed on missing keys, duplicate
key/zoom pairs, missing files, save failures, and unexpected sheet/detail
counts. Contact sheets are navigation aids; retained details remain the
inspection evidence, and generated manifests deliberately keep
`humanReviewStatus=pending` because generation cannot certify its own pixels.

The first detail review found a shared CALL presentation defect: parameterized
bindings displayed internal `global:<stable-id>` references and a false missing-
address marker. The common TIA operand-presentation path now resolves stable
global and local identities to their user-facing symbols while preserving the
stable reference for execution. The universal sweep asserts that the CALL input
renders as `enable`, `%I0.0` and never exposes `global:`. The regenerated CALL
detail was inspected at original resolution and shows symbolic names, absolute
addresses, comments, FB name, instance DB, and Input/Output/InOut pins.

The consolidated full batch result at
`.tools/tia-parity-full-site-audit.json` passed all ten commands in 132 seconds:
build, 368 controller/editor tests, transaction/editor routes, physical operand
and project-tree routes, empty-box behavior, view-state behavior, the 900-state
renderer sweep/contact-sheet generation, and the bounded nested zoom review.
It remains explicitly local (`offlineOnly=true`, `installedTiaExecuted=false`,
`livePlcExecuted=false`).

The next cross-cutting audit found a transaction defect in the shared project-
tree FC/FB-to-CALL route. Dropping a block previously mutated the live rung
before the detached CALL form opened. Cancel could therefore leave an
incomplete CALL behind, Apply could create two history entries, and one Undo
was not guaranteed to restore the byte-identical project. The drop route now
builds the entire candidate CALL inside `LadderCallAuthoring`'s detached draft.
Cancel is an exact no-op; Apply commits once while retaining the rung and
terminal IDs; one Undo restores the exact baseline and one Redo restores the
exact applied document.

This is covered at two levels. `--verify-call-authoring` is a focused detached-
transaction regression. The project-tree verifier resolves a real FC row,
validates the production tree payload, executes the production canvas
acceptance/drop methods at the visible terminal, then drives the actual Cancel
and Apply button handlers and checks exact history state. Native synthetic drag
motion is deliberately not claimed: on headless Windows, Godot accumulates
injected physical-pixel `Relative` motion against viewport-coordinate control
geometry and produces impossible canvas positions. Manual OS-level drag remains
an installed-interaction item.

The consolidated result at
`.tools/tia-parity-interaction-atomic-call-tree-drop.json` passes all ten
commands: zero-warning build, all 368 controller/editor tests, transaction and
editor routes, atomic CALL authoring, operand placement, project-tree routing,
empty-box behavior, 900 renderer cases, and editor view state. It records
`offlineOnly=true`, `installedTiaExecuted=false`, and `livePlcExecuted=false`.

Lane C now has one table-driven transaction gate for all 12 non-CALL property
families: contact, comparator, coil, timer, RT, counter, LD, counter reset,
numeric, RET, JMP, and LABEL. Every row uses the visible production form and
must pass the same protocol: invalid Apply leaves the serialized document and
Undo stack unchanged; Cancel is exact; valid Apply creates one history entry;
the selected node/terminal/label ID stays stable; one Undo and one Redo are
byte-exact; reopening displays the committed value; closing the reopened form
does not mutate the applied project.

The first matrix run exposed a shared production validation defect. Output-
property diagnostics were routed only when the editable rung already had a
nonempty terminal ID. Newly constructed drafts can legitimately derive that ID
only during program export, so invalid timer PT, CTU reset, numeric, and RET
operands could be committed even though the compiler had emitted exact errors.
Output forms now reject any compiler diagnostic whose captured target is the
edited output network; contact and comparator forms remain scoped to their exact
tree node. The fixture also uses distinct CTU/CTD owners and valid TIA label
grammar so unrelated DB/label defects cannot contaminate a row.

`PROPERTY_TRANSACTION_MATRIX_VERIFY PASS` reports all 12 rows true. The
consolidated result at
`.tools/tia-parity-interaction-property-transaction-matrix.json` passes all ten
commands, all 368 controller/editor tests, and all 900 renderer cases. It
remains local offline evidence only.

The same lane now covers every visible palette mutation control in one physical-
input matrix rather than isolated command tests. All 48 controls are activated
through `Viewport.PushInput`, including contact/edge/compare, network and branch,
coil/set/reset, IEC timer and counter, MOVE/arithmetic/math/conversion/scaling,
JMP/LABEL/RETURN, and the three explicitly separated `RungProof local (not
Siemens)` counter/text helpers. Each row proves the intended mutation, stable
rung identity, exactly one history entry, byte-exact Undo and Redo, and exact
cleanup. Hidden generic CALL, assignment, and remove-branch helpers are not
misrepresented as TIA palette controls.

Project-tree mutations use the same protocol. The verifier physically double-
clicks `Add new block`, `Add new tag table`, and `Add new watch table`; rejects
a blank name without changing the serialized project or Undo stack; creates the
named object and visible row with one history entry; then proves exact one-step
Undo, Redo, and cleanup. The focused gates report
`TIA_PALETTE_MUTATION_MATRIX_VERIFY PASS cases=48 localSeparated=True` and
`TIA_PROJECT_TREE_MUTATION_MATRIX_VERIFY PASS block=True tagTable=True watchTable=True`.

The consolidated result at
`.tools/tia-parity-interaction-mutation-matrices.json` passes all ten commands:
zero-warning build, all 368 controller/editor tests, transaction/editor/CALL
routes, the operand and project-tree physical gates, empty-box and view-state
behavior, and all 900 renderer cases. It records `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

The complete visual catalog has now passed a hash-bound human review rather
than a spot check. Two bounded reviewers split the 50 Siemens-facing keys and
inspected all 150 original 1600x1080 details at 75/100/150 percent plus all 24
family contact sheets. The lead independently rechecked corrected high-risk
controls. All 174 PNG hashes matched the generated manifest.

That review found and fixed shared renderer defects before accepting the set:
zoom-dependent camera movement caused black bands and missing headers; the
520-pixel crop left RT/JMP companion networks partial; coil/SET/RESET and RT
selection outlines crossed their operand/DB text; counter tag comments crossed
the CU/CD conductor; and CALL actual rows were too tightly pitched. The final
capture keeps the production canvas at its normal origin in a 1600x1080 native
viewport, asserts both the witness and target network are fully visible, uses
human-facing TIA names, and keeps the generated manifest fail-safe `pending`.

`docs/tia/tia-renderer-visual-review.json` records the separate `visual-pass`
verdict and binds it to manifest SHA-256
`6CF209B0427189655B3001A34F0AC8A594EA459D4CFAF93BBB11801E29ACA581`.
The controller/editor suite now validates that record against the exact 50-key
catalog scope, three zoom levels, counts, hash syntax, relative path, and
offline-only boundary. The suite passes 369/369. The pixels remain ignored
`ephemeral-local` artifacts; this does not create installed-TIA evidence.

The final consolidated result at
`.tools/tia-parity-full-reviewed-catalog.json` passes all 11 commands in 138
seconds: zero-warning build, 369 controller/editor tests, editor transactions,
ladder and atomic CALL authoring, physical operand/property routes, project-tree
routes, empty-box and view-state behavior, the 900-state renderer plus reviewed
contact-sheet generation, and bounded zoom review. It records
`offlineOnly=true`, `installedTiaExecuted=false`, and `livePlcExecuted=false`.

The evidence-registry audit then corrected a cross-cutting overclaim. The
reviewed v6 images are selected-state captures only, so the 50 offline gates can
claim automated renderer coverage but not visual inspection. The existing form,
palette, and runtime suites are broad production-route evidence, but they do not
resolve to an exact edit and monitor result for each of the 50 Siemens-facing
keys. The registry therefore now records 50 `offline=automated-pass`, 50
`selected=automated-and-visual`, and 100 `edit/monitor=not-assessed` gates.

`docs/tia/tia-local-run-evidence.json` binds the exact 100 proved renderer gates
to the full-run hash, the reviewed artifact-set hash, and the hashed visual
review record. Each Siemens entry references this source-tree summary; selected
gates also reference the visual verdict. Direct `.tools` artifact references
were removed. The controller/editor suite recomputes source-tree artifact
hashes, enforces exact state-to-artifact bindings, and passes 370/370. Because
the whole source tree is still untracked, those records remain explicitly
`ephemeral-local` rather than falsely claiming checked-in durability.

The next full result at `.tools/tia-parity-full-monitor-sweep.json` passes all
12 commands in 144 seconds, including the new exact
`--verify-tia-monitor-sweep` command. That command owns exactly 50 monitor gate
IDs and emits 200 unique cases: every Siemens-facing key in evaluated-false,
evaluated-true, unknown-earlier-cycle, and never-executed state. It checks exact
runtime observation identity, lifecycle/load identity, production flow styles,
earlier-cycle saturation, load-boundary value clearing, and production terminal
pin projection. The work exposed and fixed a real runtime defect where a
coexisting terminal action could shadow the network LABEL observation; the
runtime now publishes both stable observations independently.

The registry therefore records 50 `monitor=automated-pass` gates bound to the
hashed local-run summary. This is automated semantic evidence only: no monitor
pixels were human-reviewed, no installed TIA Portal comparison occurred, and
vendor statuses did not change. The only locally unproved Siemens state is now
the exact 50-key physical edit sweep. The controller/editor suite remains
370/370 after the evidence contract was strengthened to validate the monitor
artifact, counts, four-state set, and all 150 proved registry gates.

The exact physical-edit batch is now implemented and integrated. The full result
at `.tools/tia-parity-full-edit-sweep.json` passes 13/13 commands, including the
normal-Vulkan `--verify-tia-edit-sweep` bound to exactly 50 edit gate IDs. Every
case opens the production workbench target, applies a valid edit, preserves the
stable target identity, compiles, survives file-backed Save/Load/Save byte-
exactly, passes physical toolbar Undo/Redo, reopens with the committed value, and
restores its baseline. Forty-nine form/operand cases also reject invalid input
without document/history mutation. Network insertion is the one structural case
with no malformed operand; its artifact labels that boundary instead of
inventing an invalid value. CALL additionally proves typed binding change and
FB instance-DB number/name persistence.

All 50 Siemens edit gates are now local `automated-pass`, bringing the source-
tree summary to 200 locally exercised gates. Edit pixels are not a retained
human-reviewed per-key set, no installed-TIA comparison occurred, and vendor
statuses remain unchanged. This closes the local exact-state automation gap,
not overall TIA parity.

The evidence-contract follow-up removed an ambiguity in that edit artifact.
Schema 2 now records `invalidApplicable=false` and `invalidRejected=null` for
the structural network insertion instead of claiming that an unrepresentable
malformed operand was rejected. It also enumerates both production input paths:
`Viewport.PushInput` and `Window.PushInput:Enter`. The first consolidated rerun
then reproduced a post-pass Godot/.NET finalizer access violation in the monitor
command. All 200 cases had passed and the artifact had been written, but the
process exited nonzero because its final CanvasLayer was queued after tree quit.
The verifier now releases that layer for two process frames before quitting.
Two isolated reproductions and the original full sequence pass after the fix.
The current consolidated result is `.tools/tia-parity-full-contract-v3.json`:
13/13 commands passed with the corrected edit schema and clean monitor exit.

The remaining CALL provenance gap is also closed at the source-only level.
Siemens STEP 7 V21's official LAD procedure explicitly requires dragging an
existing FC/FB from the project tree into a network, then choosing single,
multi-, or parameter-instance options for an FB. The registry now carries that
exact V21 source and all 50 Siemens-facing keys are `source-only`. This does not
promote any gate to `matched`; an installed-TIA same-state comparison is still
required.

The unselected visual lane is now closed locally. The v3 normal-Vulkan set
contains 150 original 1600x1080 details and 24 contact sheets for the exact 50
Siemens-facing keys at 75/100/150 percent using `SymbolicAndAbsolute`, `Show`,
and `OnOperands`. All 174 hashes were verified and every image was reviewed.
Inspection found a CALL InOut output-actual/main-conductor collision at 150
percent. The shared CALL layout now uses a 48 px formal-row pitch and anchors
complete actual-annotation stacks above or below their pin rows.
`ReviewTiaCallOutputOperandRowsClearConductors` guards every renderer sweep
case. Regenerated CALL details and Program sheets passed all three zooms;
unchanged artifacts were byte-identical.
`docs/tia/tia-renderer-unselected-visual-review.json` records `visual-pass`,
manifest SHA-256
`A7C6E12BAB1E1789046C82E42C41E2C812464D48E0143C11DABBF7C1A2DD1E26`, and
artifact-set SHA-256
`7ECF09D337FA245F62A0C5F01BF5EBFDD7F5A22B771BA508198EFA6C223A1588`.

The shared fix changed six selected artifacts: the CALL detail and Program
sheet at each zoom. Those six artifacts were re-reviewed and passed; the other
168 selected artifacts were byte-identical. Offline and selected rendering are
therefore both `automated-and-visual`. Edit and monitor remain automated-only.
No installed-TIA or live-PLC status changed.

The new non-catalog editor-control registry is a separate closure track. It
defines seven LAD editor gates and nine project-tree gates. Both production-path
structured sweeps now pass: 7/7 editor gates with 36/36 assertions and 9/9
project-tree gates with 45/45 assertions. The v6 full profile binds their exact
gate IDs, validates both JSON schemas, and records `externalTia=false` and
`livePlc=false`. All 16 local gates are therefore `automated-pass`; vendor
status remains independent and is not promoted.

## Next batches

The complete acceptance ledger for the main workbench and property-management
scope is [`TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md`](TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md).
It owns the detailed requirements and dependencies below.

1. **Main workbench parity:** finish TIA V17-style shell, project tree, document
   tabs, editor/inspector panes, diagnostics, and online/offline status. Capture
   the same screens at the same window size and record all intentional
   differences.
2. **Transactional property workflows:** exercise existing property forms
   through select, load, validate, Apply/Cancel, Undo/Redo, and Save/Load.
   Include declaration row isolation across blocks and tables.
3. **DB declaration management:** complete the supported Global DB editor and
   visual contract; explicitly choose which declaration columns and access
   modes RungProof supports. Decide whether per-instance start overrides and
   online Monitor values are in scope; implement them end-to-end or keep them
   clearly disclosed as unsupported.
4. **PLC tags and constants:** add PLC-tag properties, User constants, and the
   Default/All-tags System constants view, with typed validation, table
   membership, drag-to-operand, persistence, and Undo/Redo.
5. **Watch table:** add addresses, type-valid Display format selection, and
   modify values evaluated only through the offline simulator. Preserve clear
   quality and no-physical-write boundaries.
6. **Independent review and screen walkthrough:** close high-severity code
   findings, then exercise each visible navigation, declaration, tag,
   watch-table, and property function with screen control. CUA has no native
   window discovery API, so a session-only loopback browser bridge now mirrors
   the Godot window and forwards browser input to it. Screen control has
   verified Global DB creation/member editing and custom tag-table creation,
   rename, tag-row display, and selection. In that tag-table pass, User
   Constants did not visibly transition; a separate prior pass did exercise
   User Constants Undo/Redo. Custom-table deletion, staged tag-form
   Cancel/Apply, Save/Load, tag-to-operand drag, and broader walkthrough
   coverage remain open. The bridge is local-only and is not a browser build
   of RungProof.
7. **Installed TIA V17 comparison:** execute the controlled capture pack and
   pair by exact screen/object/state/settings. Do not claim a visual match from
   RungProof captures or automated gates.
8. Retain structured evidence durably when the source tree is placed under
   version control; current `.tools` artifacts remain local and ephemeral.

## Current property-management batch result

The detailed checkpoint below is historical; its 9-gate / 48-assertion counts
are superseded by the later current-source result in
`TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md` (14 gates / 103 assertions).

The project-tree gate previously reported Global DB `fields-persist` as failed
because its test looked one child too far into a `HideRoot` declaration Tree.
The gate now selects the actual row. This exposed a real production risk: a
selected declaration index could survive a block switch and target another
block's row. The editor now scopes the selected declaration to its owning block
and name, clears the form on block changes, and verifies that a second DB with
the same row index remains unchanged. The current local build passes with zero
warnings/errors; `--verify-tia-project-tree-gates` passes all 9 gates and 48
assertions. A native-screen walkthrough also found that the DB screen pointed
to a Siemens-mode Project inspector whose tabs were hidden, leaving member
editing without a discoverable route. The DB view now exposes Edit members /
Project tree actions and widens the inspector dock while the declaration form
is active. Local Windows mouse/keyboard input verified creating a DB, adding a
BOOL member, editing its comment and Retain state, and returning to the tree.
This is local app behavior, not installed-TIA comparison.

The remaining local gaps are physical tag-to-operand drag; current-screen
block/tag-table duplication and User Constant verification; full screen
coverage for property validation, history, and project persistence; the
instance DB Comment column at constrained widths; and matched installed-TIA
V21 captures. Track these by the requirement IDs in the linked acceptance
ledger, not as isolated fixes. The Watch display-format feedback observation
also needs a current-source screen retest before it can be marked resolved.
Watch-table file persistence and running-scan Monitor now have later current-
source normal-renderer evidence recorded at the top of this plan.

## External blockers

- same-state installed-TIA offline-unselected, selected, edit, and online
  captures at a recorded TIA version, CPU family, zoom, and tag-display
  configuration;
- TIA project interchange;
- CPU-specific fault-output confirmation where the manuals are incomplete;
- live S7-1500 execution and commissioning evidence;
- deliberately unsupported Siemens features already named in the matrix.

The Watch-table workspace keeps secondary row lifecycle commands under ROW
ACTIONS. In the Siemens workbench, table selection and lifecycle now remain in
the project tree and editor tabs instead of being repeated by a selector and
TABLE ACTIONS control inside the document. Symbol authoring and SIM MODIFY stay
directly visible; absolute-address authoring expands only after `ADDRESS…` is
selected and collapses again after a successful or blank submission. A fresh
1600x900 local capture confirms the reduced control density and full primary
view-switch labels. This is a GUI usability improvement only: Watch-table disk persistence screen evidence and
installed-TIA comparison remain open.

Overall Siemens parity remains **OPEN**.

On 2026-10-21 the current Demo 3 `ladder-watch-running` frame was regenerated
and visually inspected. Its workbench, Watch document, and global runtime
statuses consistently show the loaded offline runtime running at scan 660; the
older contradictory `PROGRAM LOADED` / `PROGRAM NOT LOADED` combination did
not reproduce. A real-pointer retry could not reach the Godot surface because
the execution desktop captured wallpaper at the responsive window's rectangle,
so physical tag-to-operand drag remains open and was not inferred from input
delivery. Installed TIA Portal V17 is now the user-selected comparison baseline.

Current-source duplication now also has direct normal-renderer evidence. The
`ladder-duplication` route drives the existing block and tag-table duplication
handlers, checks regenerated identities and cleared copied I/O ownership, and
leaves both copied objects visible in the project tree and active tag grid. The
fresh 1600x900 frame shows `FB_ConveyorDrive Copy`, `Default tag table Copy`,
unique copied symbols, and blank copied addresses. This closes the local visible
duplication slice only; matched TIA V21 comparison remains open.

The staged-form Project Open guard now also has direct current-source renderer
evidence. `ladder-pending-tag-open` stages a real PLC-tag comment change, invokes
the production Open command, and requires the named Apply/Discard/Cancel prompt
before the file picker. The readable 1600x900 frame closes the local rendered
prompt gap; an OS-level pointer walkthrough, installed TIA V21 comparison, and
live-PLC proof remain separate.

PLC-tag and User Constant CRUD now have a direct current-source production-
handler route. `ladder-tag-constant-crud` creates, edits, undoes, redoes, and
deletes an unreferenced PLC tag, then creates and edits an existing BOOL User
Constant through visible Apply and Undo/Redo controls. The inspected 1600x900
frame shows the selected constant with its committed TRUE value and completion
status. The same route now saves through the production serializer, removes the
constant from memory, reopens the saved project, and requires the same stable
row, value, type, and complete readable comment before capture. This closes
those local rendered and file-backed persistence slices only; physical pointer
drag, the OS-level walkthrough, installed TIA V21 comparison, and live PLC remain
separate.

The final reopened frame also revealed the User Constants action row clipped by
the Diagnostics splitter. Reducing only the non-edit grid minimum from 180 px to
120 px preserves its expandable table while keeping **+ CONSTANT**, **EDIT**,
**PROPS**, **DELETE**, and the scope disclosure fully visible at 1600x900. The
fresh current-build capture was inspected after the change.

Installed TIA Portal V17 is the user-selected comparison baseline. The V17-native
`Winters_TIA_Bench.ap17` project opened offline without Save, download,
connection, force, or PLC action. Current captures cover Project View, the
expanded PLC tree, `Pulse_Cycle_DB [DB6]`, Default tag table, and Watch table 1.
The remaining capture work is same-object/same-state pairing to RungProof, not
acquisition of TIA V21.

The tag-to-operand source drawer now has a focused drag-ready state and readable
column sizing: the tag-name column receives 190 px and Data type a fixed 92 px,
so the selected source is unambiguous beside the visible BOOL target. Native
window-message and `SendInput` attempts did not produce the application-owned
post-drop evidence image because the current desktop provider cannot expose the
Godot window on its interactive capture desktop. Physical pointer acceptance
therefore remains open; transport delivery alone was not counted as success.

## 2026-10-18 recovery and verification update

The current task's screen-control blocker is resolved for this workstation:
the CUA native-app provider remains empty, but a session-only loopback browser
bridge bound to the exact scratch-process PID provides screen control. The
separate screen-control pass exercised tag/watch creation modal cancellation,
tag selection and visible property fields, Watch table 1 status/actions, DB
navigation, Start/Stop blocked messaging without a loaded controller, and
simulated E-stop/reset state changes. After starting the local simulator,
scene Start changed `conveyor_run` false-to-true and cartons moved; a second
click stopped the conveyor. Bottom STOP returned LOCAL RUNTIME to STOPPED,
with scan progression visible while running. No scratch project was saved.

The independent read-only review identified and the implementation fixed two
Global DB form issues: the New action could erase pending member edits, and
member labels/defaults could remain stale across DB↔FB navigation. The New
action now invokes the common Apply/Discard/Cancel resolver; the DB form uses
member-specific labels and resets defaults when its owner block changes. A
visible Cancel action now discards an unapplied member form, and changing a
new member's type replaces an untouched BOOL default with a valid type default.
ENUM selects a matching member/start value (seeding `Value1` for an empty
domain), and Retain-only changes now participate in pending-form resolution.
Independent source review then found a loader gap: malformed JSON could place
Input/Output/InOut/Temp members in a Global DB although the authoring API
rejects them. The loader now rejects those documents, with a regression test.
The prior claim that the DB row traversal skipped rows was stale; current
source walks direct declaration-row siblings and the gate confirms all five
cells lock during a pending edit and restore on Cancel.

The separate screen pass visibly added a Global DB member and edited Name,
Start value, Retain, and Comment; the BOOL start value was restored after
inspection. Alternate-type editing was not verified. Remove caused an
ambiguous redraw. A focused screen retry confirmed existing-row Edit when
selection is explicit: click the Name cell without typing, press Escape to
leave inline editing, then click EDIT. The member pane loaded the selected
member's name, BOOL type, FALSE start value, and Retain state; Cancel returned
to a blank form without changing the row. The earlier blank form was a
selection-state issue, not a confirmed Edit defect. The DB project remained
unsaved.

### Fresh-process CUA recovery and instance-DB walkthrough

The CUA native-app inventory still reports `apps: []`; its Windows binding is
not exposing Godot windows. The browser-only workaround is a session-local
loopback input/screenshot relay pinned to a specific visible Godot PID. The
first relay target had started before the current `RungProof.Next.dll` build,
which explained why that window could not prove the current DB layout. The
older scratch session was left open and untouched. A new current-build process
was launched beside it and given a separate process-pinned relay; no key or
session token is recorded here.

On that fresh process, screen control dismissed the startup simulator notice,
opened the Scenario Browser, loaded Demo 5 without a confirmation prompt,
entered Logic Editor, and opened both `X_Axis_DB [DB101]` and
`Y_Axis_DB [DB102]`. The live tabs show the assigned FB, a six-column
declaration table (Name, Data Type, Default Value, Start Value, Retain,
Comment), grouped interface sections, and the explicit read-only/offline
capability boundary. Switching between the two DB tabs restored the correct
heading. Clicking Start Value produced no edit, consistent with the disclosed
read-only behavior. No project changes were saved.

This resolves the screen-control blocker and the specific instance-DB layout
confusion for the current build: the earlier paragraph-style “Interface
storage” image is not the current live screen. One bridged older process was
indeed stale relative to the current assembly. Source/rendered/live evidence
now agrees on the six-column layout. TIA V21 visual parity is still open; the
current instance DB remains read-only, and per-instance Start/Retain/Comment
semantics require an end-to-end model, persistence, history, and runtime slice
before those cells can be offered for editing. The old scratch process and its
unsaved draft remain intact.

The current source builds with 0 warnings/errors. The visible 2048x1152
project-tree gate passes 9/9 gates and 62/62 assertions; the virtual-controller
suite passes 376/376; and the app-shell verifier passes for 77 scenes, five
demos, and 295 assets. The headless project-tree geometry sample is invalid at
its synthetic viewport and is not used as a dock-fit verdict. Matched TIA V21
capture comparison, alternate-type/Remove/Edit DB behavior, invalid-value
recovery and Save/Load workflows, remaining function-by-function screen
control, and ladder-owned demo remediation remain open.

## 2026-10-19 gate follow-up

The current source advances the project-tree sweep to 10/10 gates and 69/69
assertions, with a dedicated watch-symbol Remove gate and exact ordinary
Global DB Remove survivor/Undo/Redo assertions. The LAD editor-control sweep
passes 7/7 gates and 36/36 assertions; the virtual-controller suite passes
377 checks. An independent review also found and fixed an Undo/Redo history
restoration hazard by moving isolated-history replacement inside the gate's
`try/finally` scope. These are offline source checks only. Focused screen control
now confirms Global DB Remove deletes the selected member and preserves its
sibling; inline Global DB cell commits, instance-DB editing, remaining
screen-control coverage, and matched installed-TIA V21 comparison remain open.

## 2026-10-21 Global DB scope-band visual follow-up

The production `ladder-global-db` surface now presents its offline capability
boundary as a 42 px bordered information band with 14 px high-contrast text.
The fresh 1600×900 normal-renderer capture
`tia-global-db-scope-band.png` keeps the five-column declaration table readable
and closes the local helper-note contrast issue. Project Save/Load through the
visible screen and matched installed-TIA V21 comparison remain open; this is
local renderer evidence only.

## 2026-10-19 constrained DB layout and splitter-cache review

The independent diff review found that the two vendor workbenches shared one
project-dock width cache. The cache is now separate for TIA Portal and Studio
5000, and the focused gate sets a sentinel in the inactive workbench while it
opens and restores the TIA member editor. At a 1000×900 window the TIA splitter
has 521 px available against a 560 px request; it clamps to 521, all member
actions and the Project Tree shortcut remain inside the visible dock, and
returning to the tree restores the prior 240 px width. At 2200×1200 the editor
uses 600 px and also restores 240 px. The gate passes 13/13 gates and 88/88
assertions. This is normal-renderer automation, not pointer-click evidence.

After these source changes, the build passed with 0 warnings/errors and the
virtual-controller/editor suite passed 383/383. A fresh Demo 5 screen pass at
1000×900 confirmed DB101's Name, Data Type, Default Value, Start Value, Retain,
and Comment columns when the project tree is collapsed. With the tree expanded,
Comment requires horizontal scrolling. Instance DBs remain read-only. The
screen pass could not reach a Global DB member editor from Demo 5 and did not
verify Add/Edit/Remove/Apply/Undo/Redo or the member-editor TREE return. The
scratch project stayed unsaved and PLC-disconnected; its app and bridge were
closed afterward.

The 2026-10-20 current-source `interaction` profile completed successfully in
265 seconds with all 15 commands passing. It passed build, controller/editor
tests, editor transactions, ladder editor, motor aggregates, CALL authoring,
operand placement, project-tree pointer mutations, empty-box behavior,
renderer, monitor, edit, editor-control, project-tree, and view-state gates.
The structured project-tree artifact records 14 gates / 103 assertions and the
editor-control artifact records 7 gates / 36 assertions; both are current-source
offline evidence. Earlier stalled runs remain historical and are superseded by
this completed run. Matched installed-TIA V21 comparison, remaining
screen-control coverage, and ladder-owned demo remediation remain open.

## 2026-10-20 hardware System constants continuation

The local property model now accepts hardware-sourced System constants only as
read-only project metadata. Each row carries a stable ID, CPU-wide unique name,
data type, value, comment, and required hardware-source provenance. Validation
rejects missing provenance and collisions with PLC tags, User constants, and
block names. History and schema-10 project Save/Load preserve the complete row.
The Default/All-tags System constants grid renders Name, Data type, Value, and
Comment. Selecting a row exposes exact hardware provenance and comment in a
persistent high-contrast read-only panel; no edit action is provided. Projects
without hardware metadata retain the explicit unavailable disclosure instead
of receiving invented constants.

The focused persistence regression passed, the build completed with 0 warnings
and 0 errors, and the normal-renderer project-tree sweep passed 15/15 gates and
105/105 assertions. Its local artifact is
`.tools/tia-project-tree-system-constants.json`; it records
`installedTiaExecuted=false` and `livePlcExecuted=false`. This closes the local
source/persistence/rendering slice only. Physical tag-to-operand drag,
Watch-table persistence screen evidence, matched installed-TIA V21 comparison,
and live-PLC proof remain open.

The visible PLC-tag detail-row gate now invokes the rendered row's production
`_GetDragData` method instead of constructing its payload through a helper. The
normal-renderer sweep passed 15/15 gates and 105/105 assertions and recorded one
production drag request with the exact stable `pulse_received` identity. Ladder
target validation and Apply/Undo/Redo remain covered. This strengthens the local
application route but does not replace the still-open OS-level pointer drag or
matched installed-TIA comparison.

The normal demo operator rail is intentionally separate from the engineering
property workbench. Curriculum/report/navigation/trace/monitor/engineering
controls remain hidden from the default run surface, and the duplicate Machine
Guide button now lives with those secondary routes because Curriculum already
owns the complete guide. The visible run surface is limited to the current
lesson action, lesson inputs, runtime state, and PLC-logic handoff.

## 2026-10-21 Global DB transaction and isolation continuation

The deterministic `ladder-global-db-transaction` route now exercises the
production Global DB form through invalid-value rejection, case-insensitive
duplicate-name rejection, Cancel, Apply, actual ladder Undo/Redo, production
Save/Open, and stable block/member identity. It also creates two Global DBs with
a same-index `BatchTarget`, proves a stale form from the first DB cannot mutate
the second, then applies a second-DB-only edit through that DB's visible form.
Both in-memory declaration collections are destroyed before Open so the final
state must come from disk.

The inspected 1600×900 normal-renderer capture
`outputs/tia-global-db-property-transaction.png` visibly shows both DB objects
and tabs, the readable high-contrast selected row, and the complete passing
transaction status. The focused build passed with 0 warnings and 0 errors. This
closes the deterministic local Global DB transaction/two-DB-isolation slice;
physical pointer repetition and matched installed-TIA V21 comparison remain
open.

That transaction now also uses the production Tree cell editor for Global DB
Name, Start Value, and Comment. The same stable member reopens visibly as
`LineSpeed`, REAL `1300`, with comment `Inline edit persisted` after both DB
member collections were destroyed. This closes the current-build local inline
cell route without treating normal-renderer automation as physical-pointer or
installed-TIA evidence.
