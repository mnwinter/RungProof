# TIA parity batch execution plan

Updated: 2026-10-15

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

1. Execute the controlled installed-TIA capture pack and pair results by exact
   instruction, state, and configuration.
2. Retain the v6 structured artifacts durably when the source tree is placed
   under version control; current `.tools` evidence remains local and ephemeral.

## External blockers

- same-state installed-TIA offline-unselected, selected, edit, and online
  captures at a recorded TIA version, CPU family, zoom, and tag-display
  configuration;
- TIA project interchange;
- CPU-specific fault-output confirmation where the manuals are incomplete;
- live S7-1500 execution and commissioning evidence;
- deliberately unsupported Siemens features already named in the matrix.

Overall Siemens parity remains **OPEN**.
