# TIA Portal parity audit and acceptance criteria

> Current execution order and local/external blocker split:
> [`TIA_PARITY_BATCH_PLAN.md`](TIA_PARITY_BATCH_PLAN.md). This file is the
> chronological evidence record; do not infer current priority from an older
> checkpoint below.

Updated 2026-10-14 from the user-provided TIA Portal screenshots and Siemens
official STEP 7 documentation. This is the working acceptance source for the
TIA-style editor and authored demo controls. A green build or unit-test run is
not visual-parity evidence.

The 2026-10-14 Demo 5 follow-up is recorded as a confirmed blocker, not a new
shortcut or cosmetic ticket: the learner must see operator controls, safety
status, permissions, interlocks, sequence/state decisions, and per-device
commands in the scanned ladder; the scene must not advance a hidden gantry
cycle. The same review reports two editor concerns that remain open for direct
comparison: network/branch entry at the start of a network, and supplementary
tag/member information in TIA's hide/show modes. A fresh source/capture audit
found the current RungProof branch origin aligned at the same x-coordinate as
the TIA root rail. This corrects the earlier claim that a detached inner rail
is confirmed; parity remains unverified until a same-state capture from the
installed TIA build is compared. Tag visibility also remains a per-instruction,
per-mode acceptance item.

**Current implementation correction (later 2026-10-14):** the source-confirmed
Demo 5 blocker described above has now been repaired in the local offline build.
Main owns a visible numeric sequence, calls three parameterized X/Y/Z axis FB
instances and one vacuum/gripper FB instance with DB101–DB104, and emits six
axis-direction commands. `PalletizerPlantModel` no longer contains a phase enum
or consumes `gantry_cycle`; it integrates those commands and reports measured
feedback. The automated joint audit passes. This advances the Demo 5 control-
ownership gate only; native visual inspection of the new controls/editor blocks,
matched installed-TIA comparison, and the other authored-demo control gaps keep
the overall goal **OPEN**.

The Siemens STEP 7 V21 keyboard reference exposed another concrete editor gap:
F9/F10 insert NO/NC contacts on a selected LAD rung. The current editor now
routes those unmodified shortcuts through its normal insertion and undo path;
`--verify-ladder-editor` confirms both contact types at the selected slot.
Return, first-character entry, and F2 now edit the operand at its point of use
with the existing validated transaction; `--verify-editor-transactions` and
`--verify-operand-placement` cover entry, cancellation, commit, and placement.
The in-place editor has been inspected in RungProof's rendered canvas, but a
matching installed-TIA interaction capture has not been made. See the
per-control evidence rows in `TIA_INSTRUCTION_EVIDENCE_MATRIX.md`.

On 2026-10-14 the operand-placement verifier initially failed for two verifier
reasons: it navigated away with the output editor still pending, then compared
pre-transaction rung object references after a successful edit replaced the
document snapshot. The verifier now cancels the prior editor and re-finds each
rung by stable ID; canvas hit-testing chooses the nearest matching input pin
when adjacent pin regions overlap. Current evidence: build clean and
`--verify-operand-placement` PASS for CTU R and ADD input B open/edit/commit.
This is automated interaction evidence only. A requested screenshot capture
through the headless dummy renderer produced no viewport texture, and the
installed-TIA visual comparison remains OPEN.

**2026-10-15 follow-up:** `--verify-operand-placement` now routes complete
left-button press/release/double-click sequences through `Viewport.PushInput`
at the calculated rendered operand centers; it no longer calls the canvas
input handler directly. The normal-Vulkan gate passes CTU `R`/`PV`, CTD
`LOAD`/`PV`, TON `PT`, TONR `R`/`PT`, ADD `IN2`, all three SCALE_X inputs and
its `OUT`, comparator upper/lower operands, and the bit-coil output. Expanding
the gate exposed and fixed two real editor defects: SCALE_X inline suggestions
now use the REAL input type for `VALUE` and the selected output/range type for
`MIN`/`MAX`, and the numeric `OUT` hit region now covers the complete rendered
field. `.tools/tia-operand-physical-all-v1.png` was inspected at original
1600x900 resolution. Build is clean; 367/367 controller/editor tests,
53/53 editor-transaction routes, and the full native ladder-editor route pass.
This proves RungProof's current physical operand routing, type filtering, and
commit behavior for the listed classes. Exact matched installed-TIA
interaction and broader unsupported Siemens operand/data types remain OPEN.

The same 2026-10-15 gate was then extended across the remaining distinct
numeric port families instead of treating ADD/SCALE_X as sufficient proxies.
Normal-Vulkan physical sequences now also edit and commit MOVE `IN`/`OUT1`,
CONV `IN`/`OUT`, NORM_X `MIN`/`VALUE`/`MAX`/`OUT`, EXPT `IN1`/`IN2`/`OUT`,
SQRT `IN`/`OUT`, and ROUND `IN`/`OUT`.
This covers the lower-data-row, dual-selector, three-input range, and
fixed-base/typed-exponent layouts. The expansion exposed and fixed a third
production type-filter defect: EXPT `IN1` now remains REAL when `IN2` selects
INT or DINT, rather than incorrectly inheriting the exponent type. MOVE source
inference also follows its destination when the current input is a literal.
Fixed-REAL unary and rounding/conversion inputs now infer their only supported
type even while the visible selector is unassigned. Fresh
`.tools/tia-operand-physical-families-v2.png` was visually inspected at
1600x900. The build, 367/367 controller/editor tests, 53/53 transaction routes,
and full native ladder-editor route pass after the change. This remains local
offline evidence; matched installed-TIA interaction is still OPEN.

The 2026-10-15 normal-Vulkan operand gate now also exercises the rendered
data-type controls instead of calling `ItemSelected` directly. Physical mouse
press/release opens each real popup, focused popup search selects a rendered
row, and Enter commits through the production event path. The expanded gate
covers CTU and CTD, all six numeric comparators, all sixteen single-selector
numeric mnemonics, and both input/output selectors on all seven dual-selector
numeric mnemonics. Every route observes the exact popup `IndexPressed` value,
the selected row, popup closure, and the resulting stored type. Fixed-REAL
inputs re-select their only supported non-unknown row while the other witnesses
change type. The verifier respects Godot's configured incremental-search reset
interval when consecutive targets start with different letters, preventing
hidden search-buffer state from weakening the interaction proof. The final
normal-renderer capture `.tools/tia-type-selector-per-mnemonic-v1.png` was
inspected at original 1600x900 resolution and shows the last FLOOR witness with
contained, disjoint `Real to Real` controls and clean pin/conductor placement.
This closes direct popup interaction for every currently supported
selector-bearing mnemonic. Wider Siemens types, exact installed-TIA
interaction, OS-level/manual pointer evidence, and live PLC evidence remain
OPEN.

The same 2026-10-15 normal-Vulkan gate now physically edits every rendered
data pin on all 24 native TIA-targeted numeric rows: MOVE; ADD/SUB/MUL/DIV/MOD;
ABS/NEG/SQRT/EXPT; LN/SIN/COS/TAN/ASIN/ACOS/ATAN; and
TRUNC/NORM_X/SCALE_X/CONVERT/ROUND/CEIL/FLOOR. It separately opens and commits
both operands for all six supported numeric comparators. Timer coverage now
includes the PT pin on TON, TOF, TP, and TONR plus TONR R; counter coverage
retains CTU R/PV and CTD LD/PV. Every route uses full viewport mouse
press/release/double-click sequences, requires the inline editor to open at the
calculated rendered pin, commits a type-valid replacement, and verifies the
exact stored field. `.tools/tia-operand-physical-all-mnemonics-v1.png` was
inspected at original 1600x900 resolution; its terminal FLOOR witness shows
edited symbolic IN/OUT values, contained selectors, and continuous conductors.
This closes the current native numeric/comparator/timer data-pin inventory, not
other editor controls, installed-TIA interaction, OS-level/manual input, or
live PLC execution.

The three new call/network screenshots add a hard distinction to the editor
acceptance case: the TIA screenshot with `???`, `IN`, and `OUT` is an **empty
box awaiting instruction selection**, not a generic CALL instruction and not
yet a populated FB call. The populated RungProof card labeled `CALL` is a
different state. For the S7-1500 profile, the editor must insert the specific
FC/FB from Program blocks (or resolve an empty box), then render the selected
block name, its declared pins, EN/ENO, and selected FB instance DB. Do not label
that box with the generic `CALL` mnemonic. Siemens documents a standalone
`CALL` LAD instruction for S7-300/400, while its general LAD documentation
describes code-block calls as EN/ENO boxes; the call insertion workflow for
current TIA LAD is drag-and-drop of a named FC/FB. The screenshot's two branch
legs must start at and rejoin continuously on the network power rail before
the instruction position. The three states (empty box, chooser open, populated
named call) require separate fixtures and visual review.

The screenshots also keep the project-tree/control layout open: a selected
PLC tag table must use the central editor work surface, and selecting OB1 must
open its LAD network directly. The current tabbed PLC-tag panel and custom
"Blocks / tasks" pane are not accepted as TIA-equivalent controls. No interface
parity claim is permitted until those interactions are matched against the
installed TIA project tree and central workspace.

## Scope

The active working goal covers both sides of the teaching experience:

1. Match the Siemens TIA Portal LAD workflows represented by the supplied
   screenshots for every instruction family and editor operation RungProof
   claims to support. Screenshots are the visual reference for those exact
   states; Siemens documentation defines instruction, project, and monitoring
   semantics. Do not invent an alternate visual treatment. Mark unsupported
   features as unsupported instead of drawing controls that only look
   functional.
2. Make every demo currently presented to learners teach its stated control
   objective through PLC-owned logic. The ladder and called blocks must expose
   the actual permissions, interlocks, seal-in or state logic, operator
   controls, commands, and feedback at a level appropriate to that objective.
   Scene code may model plant physics and publish measured sensor/encoder
   feedback, but it must not supply hidden sequence logic or actuator commands.

The earlier Demo 5 evidence was an explicit failure case for PLC ownership: the
main rung sealed in `gantry_cycle`, and scene code selected the pick/place phases.
The current local source replaces that shortcut with scanned PLC logic and block
calls; the dated implementation audit at the end records the bounded evidence.
The reported network-origin concern remains an unverified visual comparison;
do not repeat the earlier detached-rail diagnosis as fact without matched TIA
evidence.

The supplied Siemens project screenshot identifies an S7-1500 CPU 1512SP-1 PN.
Its TIA Portal/STEP 7 version is not visible, so V20/V21 online documentation is
currently a semantic reference, not proof of an exact version-matched screen.
Keep visual acceptance open until captures are compared against the user's
installed TIA build and target CPU profile; do not silently generalize one TIA
version's editor geometry to every CPU/version.

## Current governing goal: TIA-faithful editor and PLC-owned demos

The latest user review reconfirms that both the editor and every demo must teach
the real structure visible in TIA. Treat these as hard gates, not independent
style suggestions:

1. **TIA reference before implementation.** Use Siemens documentation for LAD,
   block-call, instance-DB, and execution semantics; use the user's installed
   TIA build for exact visual behavior. Do not invent a RungProof-specific
   substitute for TIA controls, instruction symbols, network starts, operand
   presentation, or monitoring. Keep a versioned evidence matrix for every
   instruction and editor workflow claimed as supported.
2. **Correct network topology.** Every rung/network begins at the actual left
   power rail. Series elements and parallel branch legs must connect at the
   TIA junctions with no gap, detached inner rail, unexplained stub, or
   substitute start marker. Compare the whole first network and branch at the
   same zoom/state in TIA and RungProof.
3. **Correct tag mode.** Follow TIA's operand and supplementary tag-information
   settings separately. In hidden mode remove optional address/comment/member
   labels; retain actual instruction pins, assigned operands, and instance DB
   identity where TIA shows them. No clipped, overlapping, or invented labels.
4. **PLC owns each machine lesson.** Every demo has visible machine-level
   Start/Stop/Reset controls consumed by the displayed program, with reset
   behavior appropriate to that lesson. Motion lessons expose System Enable,
   permissions, interlocks, seal-in/state, faults, reset/restart inhibition,
   and an explicitly
   simulation-only safety-trip input. A standard-LAD training input is not a
   safety-rated E-stop; stationary counter/timer lessons must not receive a
   decorative safety control with no meaningful machine function.
5. **Demo 5 is scanned logic, not a scene macro.** Remove `gantry_cycle` as a
   whole-process command and remove plant-owned phase selection. Main must own
   sequence transitions and call separately inspectable X/Y/Z motor/axis FBs
   and the vacuum/gripper FB, with assigned instance DBs, visible interfaces,
   and feedback-driven commands. The plant may integrate output commands and
   publish measured encoder/limit/process feedback only. Every visible
   functional asset needs a traceable command/feedback path.
6. **Evidence is layered.** Pass source/model review, controller scan traces,
   scene command/feedback behavior, editor interaction, RungProof visual
   inspection, and same-state installed-TIA comparison as separate checks.
   A build, test count, source diff, RungProof-only image, or successful
   animation cannot stand in for another gate. Record unsupported features and
   simulation limits plainly.

This scope is grounded in Siemens' descriptions of the left power rail, rung
origins, and parallel branches ([LAD rungs](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/rungs-in-lad/basic-information-on-rungs-in-lad));
FB calls storing instance state in DBs ([instance DBs](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg));
and calling a block with its instance DB ([CALL](https://docs.tia.siemens.cloud/r/en-us/v20/stl-s7-1500/stl-mnemonic-s7-1500/program-control-operations-s7-1500/code-blocks-s7-1500/call-call-block-s7-1500)).
Siemens safety instructions and F-I/O have fail-safe requirements outside this
offline simulator ([fail-safe sensor/actuator requirements](https://docs.tia.siemens.cloud/r/simatic_s7_1500_et_200mp_manual_collection_enus_20/basic-information/automation-system/wiring/additional-rules-and-regulations-for-operation-of-the-s7-1500/et-200mp-with-fail-safe-modules/requirements-of-sensors-and-actuators-for-fail-safe-modules?contentId=pHRg6LlPdNOAhW5bhOdvzQ));
therefore the simulated trip is only an instructional inhibit.

The Codex goal service currently holds an editor-only objective and exposes no
in-place objective-edit operation. This section is the repository's expanded
governing goal for the active task; the Codex goal remains open and must not be
marked complete to replace it. Historical sections below retain the confirmed
pre-repair Demo 5 failure; the latest dated section supersedes that source
status. The unclosed visual parity and other-demo comparisons remain current.

## Revised active work goal

### Failure analysis and governing goal rewrite — 2026-10-14

The prior work failed for three concrete reasons visible in the project and
its earlier acceptance records:

1. The active task objective was limited to TIA editor parity. Demo work was
   therefore treated as adjacent, even though the user expects the editor and
   every demo to teach the real PLC structure represented on screen.
2. Previous Demo 5 checks proved that the scene could complete transfers and
   count four cartons. They did not prove that the loaded ladder selected each
   motion transition. The source confirms that `gantry_cycle` handed control
   to `PalletizerPlantModel`, so successful animation was mistaken for a
   successful PLC lesson.
3. Editor and demo checks were sometimes judged from RungProof's own render or
   runtime behavior. That establishes internal consistency only. It cannot
   establish TIA visual parity, native editor interaction, or correct PLC
   ownership without matched TIA captures, Siemens semantic references, and
   scan-by-scan traces.

Use this as the governing replacement goal for project work:

> Reproduce the supported TIA Portal LAD editing, instruction, project-tree,
> and online-monitoring workflows from Siemens documentation and matched
> captures of the user's installed TIA Portal. Audit every instruction and
> editor control RungProof claims to support. For every learner-facing demo,
> make the authored PLC program and called blocks own machine decisions,
> permissions, interlocks, sequence state, reset/restart behavior, and output
> commands; keep only physical motion integration and measured feedback in the
> scene model. In Demo 5, Main must sequence the four placements using visible
> System Enable, Start/Stop/Reset, simulated safety status, motor permissions,
> separate X/Y/Z and vacuum/gripper FB instances with DBs, and encoder/limit/
> process feedback. Remove the hidden `gantry_cycle` sequence. Verify editor
> interaction, instruction behavior, scan decisions, plant feedback, and
> rendered parity as separate gates. Do not declare a gate passed from source
> review, a successful animation, or RungProof-only screenshots; record
> unsupported behavior and simulation limits explicitly.

The Codex goal service does not expose an operation to edit an active goal's
objective in place. This project-level acceptance statement is the governing
scope until the active goal can be replaced without falsely marking unfinished
work complete. The active goal remains open.

The work goal is the combined acceptance contract below, not just a renderer
cleanup. It applies to every learner-facing demo in the current gallery and
every authored exercise presented as a PLC lesson, not only the five starter
demos. (1) Reproduce the supplied TIA Portal LAD/project-tree workflows for
every RungProof feature claimed as supported, using Siemens documentation for
semantics and same-state captures from the user's installed TIA for exact
visual comparisons. (2) Make every stateful lesson's machine Reset a visible
operator input consumed by its authored logic; add simulated trip/interlock
controls only to lessons where motion or hazardous-energy concepts make them
meaningful, and never present those inputs as safety-rated. (3) Put machine
permissions, interlocks, seal-in/state, sequencing, reset/restart decisions,
and actuator commands in the scanned program and called blocks; keep plant
code to commanded motion integration and measured feedback. Each separately
controlled motor/axis in Demo 5 must have an inspectable FB instance with its
own DB, called from Main, while Main owns the four-box sequence. (4) Verify
editor interactions, controller scans, and scene feedback as separate gates.
Unreviewed features remain open. A build, source review, successful animation,
or historical demo test cannot be called a TIA-parity or PLC-architecture pass
by itself.

### Goal clarification from the latest user review — 2026-10-14

Treat the four supplied screenshots and the user's written correction as new
acceptance evidence. They add these non-negotiable checks to the active goal:

| Requirement | Pass condition | Failure condition |
| --- | --- | --- |
| TIA network entry and branch | Every rung visibly starts at the left power rail; branch split, parallel legs, join, instruction pins, coil, and right rail are continuously connected in the same topology as the TIA reference. Compare identical branch and first-network states at matched zoom. | A detached inner rail, a branch floating away from the rail, a stub/gap, or a substitute rung start symbol. A RungProof-only image is insufficient to declare the current geometry defective or correct. |
| Tag and operand visibility | Apply the operand representation and supplementary tag-information settings independently. Keep actual operands and instruction pins; hide supplementary addresses/comments/member labels when TIA's tag-information mode is hidden. No synthetic IEC `.Q`/`.ET` labels, clipped text, or overlaps. | Tags remain visible after the matching hide setting, or an operand/pin is removed to conceal supplementary information. |
| Authored machine controls and learning depth | Inventory every learner-facing demo. Trace its operator controls (including logic-connected Reset where state is retained) from scene action to symbolic input, visible ladder logic, output effect, feedback, and restart behavior. Build the intended capability in the authored program and called blocks; keep each decision inspectable and controls separate from simulator playback. Add a simulated trip/interlock to motion lessons and label it non-safety-rated. | A decorative button, an app-level Run/Stop/Reset used as a substitute, an input without consuming logic, or an output/equipment sequence produced by script instead of scanned PLC behavior. Do not add an irrelevant E-stop to stationary lamp/count lessons or meaningless complexity unsupported by the lesson. |
| Demo 5 sequence and actuators | Main visibly sequences the four-box operation and calls individually inspectable X/Y/Z motor/axis FB instances plus a vacuum/gripper FB with assigned instance DBs. PLC-visible enable, permissions/interlocks, seal-in/state, stop/reset/restart inhibit, and encoder/limit/sensor feedback determine each next command. | A `gantry_cycle` request, a background phase machine, or scene code that decides when a carton is picked, moved, placed, released, or counted. |
| Scene/plant boundary | The scene advances physical position/attachment only from the latest PLC output commands, then publishes measured encoder, limit, carton, and vacuum feedback for later PLC scans. Each visible moving asset has a command/feedback path or is removed/repositioned so it is not presented as functional. | A hidden sequence, completion shortcut, or fake feedback generated to make the lesson appear successful without the PLC logic. |

The user-requested per-axis/per-motor FB structure is the chosen teaching design
for Demo 5. Siemens documents FB calls as instances whose state is stored in
instance data blocks; Siemens also documents S7-1500 Motion Control technology
objects and motion instructions. Do not claim that Siemens universally requires
one FB per motor, or label this virtual axis model as a commissioned Motion
Control technology object. The simulator has no physical drive, certified
safety controller, or commissioned encoder. Any E-stop/safety-trip button here
is an instructional input only; it can demonstrate command inhibition and
restart logic but cannot represent a safety-rated E-stop circuit.

| User evidence | New gate it reinforces | Required proof |
| --- | --- | --- |
| `codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`, `codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png` | TIA root-rail entry and a real parallel seal-in branch. Check the network start and full branch against TIA; the current RungProof capture/source agree on the root origin, so a detached-rail defect is not confirmed. | Matched-state full-rung captures plus functional seal-in scan trace. |
| `codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`, `codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png` | Timer Q/ET values belong on their own pin rows; an unassigned PT stays visibly unassigned; no external member labels when hidden. | Assigned/unassigned and online/offline captures for every claimed timer family and tag-information setting. |
| User's Demo 5 controls/architecture report | No controller logic is replaced by a scene-level gantry cycle; machine operation is taught through visible PLC logic. | Current-build per-scan trace through all four placements, axis FB calls/DBs, stop/trip/reset/restart cases, and measured feedback. |

### Latest goal integration — four follow-up screenshots, 2026-10-14

These examples refine the active acceptance contract; they do not justify
another Demo 5-only cosmetic patch:

1. The Network 7 seal-in example is a negative acceptance case until its
   branch origin, root-rail connection, and rung start are compared against the
   equivalent circuit in the user's installed TIA. A valid seal-in must be
   represented as an actual parallel logic path in the authored ladder. Do not
   assume that adding an inner rail or branch-shaped stroke is equivalent to
   TIA's rung entry. The matched visual review must include the entire network,
   not a crop that hides its rail and right-side destination.
2. A timer's `Q` and `ET` are distinct output pins with distinct values. Each
   operand/value must stay on the same horizontal row as its pin and connected
   wire; an unset `PT` must remain visibly unset. When supplementary tag
   information is hidden, do not retain fabricated or clipped member labels
   beside the right rail. Review both hidden and shown modes at the same zoom.
3. Demo 5's `gantry_cycle` coil and scene-owned phase sequence are rejected as
   the machine controller. Main must expose the operator request and permissive
   logic, then call inspectable per-axis and vacuum/gripper FBs with their
   instance DBs. Those blocks and scanned feedback determine each next move.
   A project that animates all four placements through a plant-side shortcut
   still fails, even if its counter, final pose, and screenshot look correct.
4. Every displayed motion asset needs an authored command/feedback path. The
   simulated safety trip, motor/axis permissions, System Enable, Stop, Reset,
   seal-in/state retention, and restart inhibit must be visible and consumed by
   the program. Scene and app Run/Stop/Reset controls are not substitutes for
   lesson-level controls. The trip is instructional and non-safety-rated.

The project-level governing goal above includes these as mandatory gates. The
active Codex goal record still contains its original editor-parity-only text;
the goal API provides status changes but no in-place objective edit. Keep the
record active and use this project acceptance document as the expanded working
scope. Do not mark the editor goal complete merely to replace its wording;
editor parity and PLC-owned demo acceptance remain unfinished deliverables.

Siemens references for the design boundary: [FB instances and instance data
blocks, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg),
[S7-1500 organization blocks, STEP 7
V20](https://docs.tia.siemens.cloud/r/en-us/v20/functional-description-of-s7-1500-cpus-s7-1500/organization-blocks-s7-1500?contentId=lA~BuRcd1wRhTT~koUcd7Q),
[S7-1500 motion technology objects](https://docs.tia.siemens.cloud/r/en-us/v20/s7-1500/s7-1500t-motion-control-overview-s7-1500-s7-1500t/overview-of-functions-s7-1500-s7-1500t/description-of-technology-objects-s7-1500-s7-1500t?contentId=kza2IQsSW_6ejth7LmTnWg),
and [SIMATIC Safety overview](https://docs.tia.siemens.cloud/r/en-us/v20/simatic-safety-configuring-and-programming/product-overview/overview?contentId=VQZFp0_FJYINHbVAh9a95Q).

### Execution order and current blockers

The supplied screenshots are acceptance evidence, not instructions embedded in
the images. Implement and review the failures in this order so visual fixes,
program structure, and scene behavior are checked as one teaching workflow:

| Gate | Required result | Current status |
| --- | --- | --- |
| TIA network and operand presentation | Every network starts at the left power rail with connected branch geometry; timer pins and operands align; optional tag information disappears in hidden mode without removing real operands or pins. Compare the exact same rung and display state against installed TIA. | OPEN: the current RungProof source and branch capture agree on root-rail origin, but there is no matched installed-TIA comparison. Tag-info behavior and timer states still need per-instruction review. |
| Machine controls and learning objective in every learner-facing demo | Inventory machine Start/Stop/Reset controls, bindings, ladder consumers, output effects, feedback, reset, and restart behavior for every demo presented in the app. These are operator inputs consumed by the lesson program; app/controller Run/Stop/Reset and playback do not count. Keep the lesson's actual intended complexity visible in ladder/blocks. Add an explicitly simulated, non-safety-rated trip only where motion makes it meaningful. | OPEN: previous functional runs do not establish the visible control and logic contract for every demo. Do not mark the gallery complete from the initial five-demo inventory alone. |
| Demo 5 PLC-owned palletizing | Remove `gantry_cycle` as a high-level motion request and remove plant-owned phase selection. Main's scanned ladder sequence calls separate axis/motor FB instances and a gripper/vacuum FB; interlocks, permissions, simulated safety status, encoder/limit feedback, Stop, Reset, and restart inhibit select each command. Every moving asset is used by that lesson or removed as a misleading distractor. | FAIL, confirmed in current source below. The existing scene model selects the palletizing phases. |
| Project-tree workflows | Selecting a command row does not execute it. Activating `Add new block` opens block-type choices and a name field; activating `Add new tag table` opens a unique-name dialog. Supported choices create and persist correctly. | PARTIAL: OB/FB/FC/DB choices and tag-table create/undo/persistence pass the native editor route; `.tools/tia-add-block-dialog.png` was visually inspected. Exact installed-TIA dialog layout/properties remain open. |
| TIA project tree and object workflows | Match the user's TIA hierarchy and supported object lifecycle: OBs appear once as program blocks; DB creation exposes supported DB kinds and declaration storage; instance DBs open or navigate to their actual definition; named watch tables can be created, opened, saved, and selected independently. | OPEN/PARTIAL: OBs appear once under Program blocks; IEC/FB instance-DB rows open dedicated read-only definition tabs; the supported Global DB creation path identifies its type, opens a declaration document, persists scalar name/type/start-value rows, and cannot contain LAD networks or become a CALL/task/program-entry target; named watch tables can be independently selected/opened, populated, deleted, undone, and persisted. ARRAY DB, PLC/system-type DB, manual instance-DB creation, additional declaration columns, runtime DB-member addressing, multi-instance, and parameter-instance storage remain unsupported. Native command routes plus normal-renderer physical Enter and double-click gates prove the supported RungProof behavior; matched installed-TIA layout remains open. |
| Evidence and closure | Rebuild, run targeted controller/scene traces, and inspect native editor/screens at matched TIA zoom and state. Record limitations for unsupported CPU, instruction, and safety behavior. | BLOCKED BY OPEN GATES ABOVE; no overall parity or demo-completeness claim is allowed yet. |

Do not begin by adding decorative controls or another visual-only pass over the
existing Demo 5 sequence. First map each operator action to a tag and rung,
then map each rung output to an actuator command and measured feedback. In
Demo 5, advance one PLC-controlled motion decision per scan and let the plant
integrate commanded motion; never let the plant choose the next operation.

This also governs the current Demo 5 review: do not implement a `gantry_cycle`
output that delegates motion sequencing to the scene. The learner must be able
to follow System Enable, Start/Stop/Reset, safety-trip status, motor permissions,
seal-in/sequence rungs, individual motor/axis FB calls with their instance DBs,
and encoder/limit feedback through the main OB and called blocks. For this
motion lesson, expose a simulated E-stop/safety-trip control and its PLC-visible
inhibit/restart behavior, while labeling it as instructional simulation rather
than a safety-rated function. The plant model may integrate commanded motion
and report measured position/limits; it may not choose the next sequence step,
complete a transfer, or advance a hidden phase machine.

## Reference-backed visual rules

| Area | Acceptance criteria | Evidence source |
| --- | --- | --- |
| Project tree | `Program blocks` contains `Add new block`; double-click/activation opens a TIA-style creation dialog with supported OB/FB/FC/DB choices and a name field. Both standalone/global DBs and generated FB/timer/counter instance DBs appear under `Program blocks`, alongside the code blocks. `PLC tags` exposes `Show all tags`, `Add new tag table`, and selectable named tables with their tag counts; double-click/activation of `Add new tag table` opens a unique-name dialog. Single selection must not run either creation command. Creation, navigation, and persistence are checked. Current subset: OB/FB/FC and global DB; FB instance DBs are assigned from CALL properties. TIA ARRAY DB, PLC-data-type DB, software-unit release, extra OB event properties, and automatic add-and-open behavior are not modeled and must not be implied. | User project-tree screenshots; [Siemens project tree](https://docs.tia.siemens.cloud/r/en-us/v20/introduction-to-the-tia-portal/user-interface-and-operation/layout-of-the-user-interface/project-tree/function-and-structure-of-the-project-tree); [creating FCs and FBs, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-and-managing-blocks/creating-blocks/creating-functions-and-function-blocks); [creating DBs, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/creating-data-blocks); [creating PLC tag tables, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/declaring-plc-tags/creating-and-managing-plc-tag-tables/creating-plc-tag-tables). |
| Network/rung topology | Every network begins from the TIA-style left power rail; the first rung/branch conductor is connected to it, with no detached inner rail, stub, or invented branch-start symbol. Series paths, parallel split/join, instruction pins, coil, and right termination stay electrically continuous with no visual gaps. Branch geometry and ordering follow the exact screenshot state; no custom decorative framing. | User TIA screenshots, including the Demo 5 seal-in branch-start close-up; [Siemens LAD elements](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A). |
| Monitoring colors | Online status is rendered per evaluated wire and instruction: satisfied power flow green solid; unsatisfied blue dashed; unknown/not executed gray solid; unconnected black. A satisfied continuous path must color every connected path segment, not only contact/coil fragments. Do not infer a wire's status from an adjacent instruction's local result alone. | [Siemens LAD program status](https://docs.tia.siemens.cloud/r/en-us/v20/testing-the-user-program/testing-with-program-status/examples-of-program-status-display/program-status-display-for-lad-programs); [program editor status](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/online-and-diagnostic-tools/monitoring-and-modifying-values-in-the-cpu/displaying-status-in-the-program-editor?contentId=dh~oxoyUejm7iZH7UUAymQ). |
| CTU/CTD and timers | Match the screenshots' block dimensions, title/type selector, pin order and alignment. The counter data-type selector uses the screenshot order `???`, `Int`, `SInt`, `DInt`, `USInt`, `UInt`, `UDInt`, `LInt`, `ULInt`; a new untyped IEC counter displays `???` and cannot Verify + Load until typed. Type selection determines the IEC counter instance-data type and call options. Instance DB address/name belongs above the instruction block. Pin operands/online values align to the matching pin. For IEC instance outputs, do not add external `instance.Q`, `instance.CV`, `instance.ET` tag labels where TIA shows the block pin/member value. CTD uses `CD`, `LD`, `PV`; `LD` is not a separate LOAD box. A missing `PT` is represented as missing at its pin and validation reports it. TIA also has CTUD; it is unsupported here and must be identified as such, not approximated. | User TIA CTU/TON screenshots; [Siemens CTU](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/counter-operations-s7-1200-s7-1500-s7-1200-g2/ctu-count-up-s7-1200-s7-1500-s7-1200-g2); [IEC counters](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/basic-instructions/counter-operations/counter-instructions-iec-counters?contentId=Z7GUVKMxG~wmbRyRUV_j6g); [Siemens IEC timers](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/timer-operations-s7-1200-s7-1500?contentId=KaGadBtklM9mEmLQHvoibQ). |
| Numeric boxes | In Siemens view, preserve each instruction's actual pin identifiers: `MOVE` uses `IN`/`OUT1`; arithmetic and `EXPT` use `IN1`/`IN2`/`OUT`; unary functions use `IN`/`OUT`; `NORM_X` and `SCALE_X` use `MIN`/`VALUE`/`MAX`/`OUT`. Show `EN`/`ENO` on the rung-power row and data operands on their own lower pin rows. The conductor entering the box uses EN state; the conductor leaving it uses ENO state. If the operation errors, EN may remain green while ENO and downstream flow are false/blue-dashed. MOVE's IEC-checked numeric subset requires matching declared source/destination types; TIA's additional MOVE outputs and broader type-check modes are unsupported. Local STRING/ENUM copy behavior must not be presented as native TIA MOVE. Operand labels/values sit outside the box with clear pin conductors; output operands must not be clipped at the right rail. The current model does not support ADD's extendable `IN3...INn` inputs, so that TIA editing feature remains unsupported. `FORMAT_TEXT` is local and must not be styled or described as a Siemens instruction. | [Siemens EN/ENO behavior](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/programming-concepts/programming-language/en-and-eno-for-lad-fbd-and-scl); [Siemens MOVE, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/move-operations-s7-1200-s7-1500-s7-1200-g2/move-move-value-s7-1200-s7-1500-s7-1200-g2?contentId=vfU0TXQbkFX4gEKSA1XYaw); [ADD](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/add-add-s7-1200-s7-1500); [NORM_X](https://docs.tia.siemens.cloud/r/en-us/v20/fbd-s7-1200-s7-1500/conversion-operations-s7-1200-s7-1500/norm_x-normalize-s7-1200-s7-1500); [EXPT](https://docs.tia.siemens.cloud/r/en-us/v20/fbd-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/expt-exponentiate-s7-1200-s7-1500); [CONVERT](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/conversion-operations-s7-1200-s7-1500/convert-convert-value-s7-1200-s7-1500?contentId=u1e7K21vTkA9dWoVfsPWlQ). |
| Other instructions | Compare every catalogued TIA instruction family and sub-type against its TIA symbol, body, pin names/order, operand/tag placement, wire routing, sizing, and offline/online states. The catalog is the inventory; local RungProof operations must be explicitly marked as local and must not masquerade as Siemens instructions. | `LadderInstructionCatalog`; official instruction reference for the target CPU/version; user screenshots where supplied. |
| Selection/edit state | Selection affordances must match TIA's selected-object state for the same object and mode. Selection must not obscure pins, wires, values, or operands, and must be absent when no instruction is selected. Compare coil selection separately from boxed instruction selection. | Supplied TIA screenshots; Siemens editor behavior for the selected instruction type. |
| Tags and instance data | Show declared tags and instance data in the project tree and editor where TIA does. Show instance DB identity above an IEC instruction. Do not render redundant symbolic member names next to pins in TIA mode. Tag table filtering and `Show all tags` must work, not merely display rows. | User screenshots; [Siemens tag usage](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/using-and-addressing-operands/using-tags-within-the-program); [single FB instances](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/block-calls/instances/single-instances); [creating instance DBs](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/creating-data-blocks). |
| Operand/tag information controls | Provide separate TIA-style controls for operand representation (symbolic, absolute, or both) and tag information (hide, show, hierarchical comments), plus tag-information position at operands or beneath the network. Keep instruction-value inspection separate. | [Siemens general program-editor settings](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/general-settings-for-the-plc-programming/overview-of-the-general-settings); [showing/hiding tag information](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information). |

**Interpretation rule:** Siemens has a separate operand-representation setting
(symbolic, absolute, or both) and supplementary tag-information settings
(hide/show, simple or hierarchical comments, and position). Do not use the
tag-information setting to erase the actual operand or instruction pins; the
selected operand representation governs how that operand is named. IEC instance outputs are
instruction members (`Q`, `CV`, `ET`), not extra symbolic tags to print beside
the box. Keep RungProof's "Instruction values" inspector separate from these
editor view settings.

The latest screenshots add explicit comparison cases: a parallel seal-in rung
and its zoomed branch start, IEC timer output pins, a TIA TON with an unassigned
`PT`, and Demo 5's multi-block palletizing control. The branch begins directly
at the left power rail; top and bottom paths split and rejoin as continuous
conductors before the output coil. Do not add an extra left-side vertical rung
stub or detached branch origin. The visual comparison must use the same actual
seal-in logic, not a similar-looking pair of branches. Timer `Q` and `ET`
operands/values align to their own output pins. An unassigned `PT` remains
unassigned (`???`) at the `PT` pin; no project default or fabricated preset is
acceptable. Compare the same project and display state, including
invalid/missing-operand states. When tag information is hidden, remove
supplementary address/comment/member annotations everywhere; do not leave stale
external labels such as `pick_timer.Q` or `pick_timer.ET`. Keep actual symbolic
operands and instruction pin names according to their independent TIA display
settings.

The delegated source audit enumerated **52 program-instruction keys across
seven families**. That means the 54 help-catalog entries minus the two editor
commands (`rung` and `branch`), which are not standalone instruction symbols.
The supplied TIA captures directly cover six families; an FC call counts as
the seventh comparison only where its call form is being claimed as
TIA-equivalent. This count is an audit inventory, not a pass. Keep a per-key
result for normal, selected, edit, and monitored states, and mark each
local-only or unsupported key explicitly.

## PLC-control acceptance for demos

1. Inventory all five authored demos and distinguish app-level Run/Stop/Reset
   from operator-station controls and PLC logic. Each demo must expose its
   intended local operator commands and associated rungs, including reset and
   an emergency-stop lesson where appropriate to the cell.
2. Programs show System Enable, permissive/interlock conditions, seal-in or
   explicit sequence-state logic, reset/fault recovery, and actual input/output
   behavior. Motor/axis commands are owned by independently inspectable FBs
   called from the main organization block; each has its own assigned instance
   data, command and feedback rather than a single hidden high-level motion bit.
   Siemens distinguishes single-instance DBs from multi-instances; the authored
   training design must make the chosen instance model visible and internally
   consistent. See [single instances](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/block-calls/instances/single-instances).
3. Demo 5 uses encoder/sensor feedback to prove gantry position and pallet
   placement. The visible PLC logic owns requests and phase transitions. Plant
   code advances the model from those outputs and returns measured feedback; it
   does not synthesize a gantry cycle or choose the sequence outcome.
   Each motion axis/motor has its own inspectable FB instance called by Main;
   its call, instance DB, enable/permissive, run/direction or position command,
   and feedback must be visible. A top-level one-bit `gantry_cycle` command is
   not an acceptable substitute. All four box transfers must result from
   scanned ladder decisions using encoder/limit/sensor feedback, and every
   visible mechanism must be either controlled or clearly identified as
   nonfunctional scenery.
4. An emergency-stop lesson must not represent ordinary standard-PLC logic as a
   certified safety function. Keep the simulated safety circuit and fail-safe
   logic concept distinct from standard motor permissives. Label the offline
   simulation boundary. See [SIMATIC Safety overview](https://docs.tia.siemens.cloud/r/en-us/v20/simatic-safety-configuring-and-programming/product-overview/overview?contentId=VQZFp0_FJYINHbVAh9a95Q) and [F-CPU configuration](https://docs.tia.siemens.cloud/r/en-us/v21/simatic-safety-configuring-and-programming/configuring/configuring-an-f-cpu/configuring-an-f-cpu).
5. Demonstrate controls and faults through the actual scene, controller, and
   rendered ladder: start, permissive loss, E-stop, reset, restart inhibition,
   sensor/encoder transitions, Stop, and Reset. No hidden script may bypass the
   displayed program.

### Latest goal refinement: PLC-owned machine control and TIA-authentic networks

The four screenshots supplied on 2026-10-14 are a hard correction to the
current goal, not a request for another cosmetic pass. The project must teach
the control path in the LAD program and render that path with TIA's network and
operand conventions. Keep this as a release gate alongside the per-instruction
TIA audit:

1. **Operator controls are part of each lesson.** Every demo gets a local,
   visible Reset control that is bound to a PLC input and consumed by the
   displayed logic. Add local Start/Stop where the process has a run state.
   These are separate from app playback controls. Add an instructional
   simulated safety-trip/E-stop status only to motion lessons; never label
   ordinary standard-PLC code or this offline simulator as a safety-rated
   function.
2. **Main and called blocks own every machine decision.** Demo 5's OB/Main
   logic owns sequence state, permissives, seal-in, interlocks, reset and
   restart inhibit. It calls separately inspectable X/Y/Z motor/axis FBs and a
   vacuum/gripper FB with visible interfaces and distinct instance DBs. Those
   blocks own their device commands and feedback handling. The user requested
   this per-device FB structure as the teaching design; Siemens supports
   reusable FB calls with separate instance data, but this is not a claim that
   every real machine must use one FB per motor.
3. **The plant is an actuator/sensor model only.** The plant integrates the
   most recent PLC commands and reports measured encoder counts, limits,
   carton presence/attachment, and vacuum feedback. It cannot choose the next
   sequence state, create a sensor edge, or decide that a carton was placed.
   All four placements must be attributable to accepted controller scans.
4. **Networks start and branch like TIA.** The first rung begins at the left
   power rail. A parallel branch connects directly to that rail when the
   reference topology does; branch legs join continuously before the shared
   output. No detached inner rail, leading stub, gap, or invented origin is
   acceptable. Review the exact branch screenshot, not only Boolean
   equivalence.
5. **Tag visibility follows the separate TIA settings.** Keep operand
   representation independent from supplementary tag information. Hiding tag
   information removes optional addresses, comments, and external instance
   member labels such as `pick_timer.Q`/`.ET`; it does not remove the actual
   operand or instruction pin. An unassigned timer `PT` remains unassigned.

The user's latest written review confirms these requirements and adds an
anti-shortcut review gate: every network's initial conductor and every branch
must be inspected against the TIA reference; a logically equivalent rung with
a detached inner rail is still wrong. Each machine action must be traced in
both directions (`scene input → PLC scan → output → plant response` and
`measured plant feedback → PLC transition`); labels such as “Gantry Cycle” or
“Sequence Supervisor” do not count as exposed logic if they hide the decisions
or if scene code executes them. A network screenshot must also be checked with
tag information hidden and shown, including instance instruction pins, so
optional `Q`/`ET` member labels cannot leak into the hidden state.

The screenshots expose an implementation failure that the existing goal must
not soften: current Demo 5's `gantry_cycle` seal-in coil is a high-level motion
request consumed by `PalletizerPlantModel`, which owns the phase progression;
current encoder tags observe plant pose rather than closing the PLC motion
sequence. Current controls, tags, and network geometry therefore remain open
until replaced and demonstrated end-to-end.

**Required work order:** (a) retain the TIA screenshot cases as matched-state
visual fixtures; (b) map each scene control and actuator through input tag,
network/block, output, plant response, and measured feedback; (c) remove
plant-owned sequencing and author the PLC blocks/DB calls; (d) exercise stop,
trip, reset, permissive loss, restart inhibit, and four placements with
scan-by-scan traces; (e) compare the same networks and tag settings against
installed TIA and inspect captures for cuts, overlaps, gaps, and residual
labels. A source check or green build alone cannot pass these gates.

This distinction is supported by Siemens documentation: LAD branches connect
to the power rail and have explicit insertion/termination rules ([LAD branch
rules, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/rules-for-branches-in-lad?contentId=jsBAKV~wzHUUwt657xLTzw)); FB instances retain per-call state in instance DBs ([FB
documentation](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/programming-concepts/using-blocks-to-structure-your-program/function-block-fb));
tag information is independently shown/hidden and positioned ([tag information,
STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information));
and Siemens' E-stop instruction is a STEP 7 Safety function with fail-safe
requirements, acknowledgement, and F-I/O considerations ([ESTOP1,
STEP 7 Safety V20](https://docs.tia.siemens.cloud/r/en-us/v20/step-7-safety-v20-instructions/safety-functions/estop1-emergency-stop/off-up-to-stop-category-1-step-7-safety-v20)).

### Five-demo source baseline and required control contracts

This table is a source-backed failure inventory, not a claim that any demo
already meets the expanded goal. The scope is all five authored demos. A
machine control is a physical scene control mapped to a symbolic input that the
displayed ladder reads; the application's Run/Stop/Reset buttons only control
the simulator and do not satisfy this requirement.

| Demo | Current authored behavior confirmed in source | Required learning/control contract | Safety-trip scope |
| --- | --- | --- | --- |
| 1 — Press Count Starter | `AuthoredDemoLadderPrograms.CreatePressCount` counts `pulse_received` and drives `threshold_lamp`; there is no reset input or reset rung. | Add a learner-operated reset input/button, PLC-visible counter reset, documented pulse/count behavior, and visible output/status. Stop behavior must be explicit even though this demo has no motion. | No E-stop is required for the lamp-only scene; do not add a decorative E-stop. |
| 2 — Delayed Lamp Timer | `CreateDelayedLamp` runs a TON from `timer_request` and drives `delayed_lamp`; there is no distinct reset control/rung. | Add learner-operated Stop and Reset behavior to the PLC program. Show how a reset or request removal resets timer state and inhibits/requires a fresh start as defined by the lesson. | No E-stop is required for the lamp-only scene; do not add a decorative E-stop. |
| 3 — Conveyor Sequence | `CreateConveyorSequence` has Start/Stop, a photoeye, and a seal-in branch, but no Reset or simulated trip input/logic. | Add local Reset and simulated trip controls. Show System Enable, run permission, stop/trip inhibition, latch reset, and restart inhibition in the LAD. Conveyor output must be driven only by the scanned program. | Required; explicitly label it instructional simulation, not a safety-rated E-stop. |
| 4 — Batch Process Blocks | `CreateBatchCounter` accepts pallet/type/count inputs and calls counter/validation blocks; no operator Start/Stop/Reset input or counter-reset rung is authored. | Add local Start/Stop/Reset with clear PLC logic for accepting a batch, stopping acceptance, clearing count/state, and preventing unintended restart. Keep pallet/type signals as process inputs, not substitutes for operator controls. | Only if the actual scene includes hazardous/moving actuation; otherwise document why this is a stationary counting lesson and omit a decorative E-stop. |
| 5 — Integrated Palletizing Cell | Main currently calls permissive, pick/place, sequence, and math blocks, but outputs `gantry_cycle`; the plant model owns the pick/dwell/transfer/place/release/return phases. No machine Stop/Reset/trip controls or axis-specific FB calls exist. | Replace the high-level cycle request and plant-owned phase machine with scanned sequence/state logic. Add System Enable, distinct operator Start/Stop/Reset, simulated trip, reset/restart inhibition, permissives/interlocks, and feedback-driven transitions. Main calls individual X/Y/Z axis or motor FBs and gripper/vacuum FB, each with a visible interface and distinct instance DB. Encoder/limit feedback determines transitions; plant code only integrates commands and reports measured feedback. All four carton placements must be caused by controller scans. | Required; simulated trip is a training signal only and must not be presented as a real safety function. |

The names and behavior above are the initial inventory from
`AuthoredDemoLadderPrograms.cs`; before implementing each demo, cross-check its
scene action bindings and actual ladder/controller trace. Do not infer that a
named button, block, or displayed rung is connected just because it is present
in a screenshot. Record the full path `operator control → scene action → PLC
input tag → rung/block decision → output tag → plant response → measured
feedback` for each control and actuator.

### Architecture and visual rejection criteria

- A demo fails if the plant script starts, sequences, or completes machine
  operations that the displayed PLC program does not command. Plant simulation
  may apply motion limits/response, attachment mechanics, and feedback, but may
  not decide what the next PLC action should be.
- For Demo 5, an individual drive/axis block must own its own command and
  feedback interface and be called from Main with a distinct instance DB.
  A wrapper that only translates `gantry_cycle` into a scripted motion is not
  an acceptable replacement. The PLC sequence must use encoder counts and
  limit/sensor states to decide when to advance.
- Each network starts at the actual TIA left power rail. A branch is a
  continuous electrical split/join from that rail/rung; a detached inner rail,
  orphan stub, or mid-rung branch origin fails review even if the Boolean
  expression is equivalent.
- Tag visibility is governed by the selected TIA-style display settings.
  Operand names and instruction pins remain; supplementary tag/address/comment
  labels disappear when hidden. No clipped, overlapping, or synthetic member
  labels are permitted. Review every supported instruction family and both
  hidden/shown tag-information states, not one representative counter.
- The editor capture must show the same project, rung, zoom, mode, selection,
  online state, and tag settings as the TIA reference. Static builds and source
  assertions cannot pass a visual gate.

### Required end-to-end evidence per demo

For every authored demo, capture an inventory of scene controls, bindings,
symbolic I/O, blocks, reset behavior, and moving assets. Exercise local Start,
Stop, and Reset through the scene and show the same scan in the ladder. For
motion demos, additionally show trip, permissive loss, reset, restart blocked,
and deliberate fresh restart. For Demo 5, retain a trace of the four cartons
being picked and placed with per-axis commands, instance DB state, encoder and
limit feedback, PLC state transitions, and output changes visible. These are
offline instructional simulations; they do not certify machine safety or
commissioning behavior.

## Additional gates from the latest user evidence

These requirements expand the active parity objective. They are not optional
visual polish. The supplied screenshots are visual evidence; Siemens manuals
define instruction and safety semantics.

The latest user change set makes the following four observations explicit
acceptance cases, rather than relying on a general visual review:

| Observed in supplied evidence | Required correction | Pass evidence |
| --- | --- | --- |
| The supplied Demo 5 image appears to show an extra inner branch origin before the seal-in paths and drives a `gantry_cycle` coil. This geometry is not confirmed defective without a matched installed-TIA capture; source and current RungProof capture align the branch origin to the root rail. | Compare the whole network at matched zoom/state. Preserve the direct root-rail rung entry and actual parallel split/join topology shown by TIA. Independently replace the high-level cycle bit with scanned sequence logic and separate axis/vacuum FB calls. | Same-state TIA/RungProof branch captures, then a scan trace from machine Start through each feedback-driven move. |
| Timer `Q`/`ET` pins have detached or misaligned values/tags; a missing `PT` is rendered unlike TIA. | Align each live value to its own pin row; represent an unassigned operand at its pin as `???`; never invent a preset. Remove optional external member labels when tag information is hidden. | TON online/offline captures with assigned and unassigned `PT`, plus timer tag-information hidden/shown states at normal and 100% zoom. |
| Demo assets and transitions appear unrelated to the ladder being taught. | Every visible mechanism must have a documented command/feedback relationship to the scanned PLC project, or be removed/clearly identified as scenery. Scene code may integrate commanded motion and report measurements, not choose machine sequence. | Asset-to-I/O ledger and controller-driven scene trace on the same project/build as the visible ladder. |
| The demos lack learner-facing machine reset and meaningful stop/safety behavior. | Add machine Start/Stop/Reset and authored logic in each demo. Add a clearly labeled simulated E-stop/safety-trip input only for motion or safety lessons; never imply standard ladder logic is safety-rated. | Operate the controls and trace symbolic input, ladder/block, outputs, status, fault/reset, and restart inhibit. App playback controls do not count. |

These cases are hard gates in the expanded goal below. A capture of one
instruction, a compile, a historical test run, or source inspection alone
cannot pass the editor, demo-architecture, or integrated acceptance gate.

### TIA network and operand presentation

- Begin each network with the TIA power rail and connected wire topology. Series
  logic starts on the rail-to-rung path; parallel paths split and rejoin as
  electrical wires. Keep branch rail, split/join, contact, instruction, coil,
  and continuity connected. Do not introduce extra rails, decorative boxes, or
  spacing that changes the rung's apparent electrical order.
- At the left edge, draw one continuous power rail with the first rung connected
  directly to it. A branch at the beginning of a network must be a real branch
  off that rail/rung; do not show a detached short lead, a second inner rail,
  an orphan vertical, or a branch that appears to start after an invisible
  instruction. Compare the whole branch and its start against the supplied TIA
  screenshots at normal and 100% zoom.
- Match the reference branch geometry and the whole rung's status display. For
  an evaluated true path, every connected segment from the left rail through the
  final instruction and to the right rail is green. A false path uses TIA's
  false-state styling; unknown or unexecuted paths remain gray. Derive each
  segment from incoming power plus the local instruction result, including
  parallel-branch combination. An adjacent Boolean value alone is not proof of
  wire power.
- Operand symbols and live values belong at their TIA pins. Timer outputs (`Q`,
  `ET`) and counter outputs (`Q`, `CV`) align to their corresponding pins, not
  float over or under the wire. Show assigned instance DB identity above IEC
  blocks when TIA does. Respect TIA's tag-information visibility and placement
  controls; do not leave member names or clipped values visible when that
  display option is off.
- When supplementary tag information is hidden, remove extra tag/address/
  comment labels everywhere, including timer/counter members such as
  `pick_timer.Q` and `pick_timer.ET`. The instruction's actual pin identifiers,
  DB identity (when TIA shows it), and configured operand remain governed by
  their own display settings. The latest user captures specifically identify
  those external timer labels as unwanted; hiding them only for a CTU fixture
  does not pass this check.
- Review every catalogued instruction in each supported vendor presentation,
  including normal, selected, edit, and online states. Verify instruction type,
  pins, values, instance identity, terminals, and dimensions as a complete
  instruction family, not one example at a time. Unsupported Siemens functions
  remain explicitly unsupported until their logic and visual representation are
  implemented and reviewed.
- Network headers, rails, and operands must follow the TIA reference for the
  current editor mode. Preserve operands/pins and apply symbolic/absolute
  representation separately from tag-information annotations.
  Never clip a tag/value at the right rail or let it overlap a pin, wire, or
  adjacent instruction. CTU/CTD/timer terminal values must align with their
  own pin row.
- A parallel network's left power rail spans the complete branch extent and
  connects electrically to the split; there is no center-only rail, detached
  top branch, or orphan vertical segment. The paths join before the terminal
  instruction as shown in TIA. Check normal editor zoom and 100% zoom.
- IEC timer/counter outputs never render invented external operands such as
  `timer.Q`, `timer.ET`, `counter.Q`, or `counter.CV` beside the right rail.
  Draw the instruction pin names (`Q`, `ET`, `CV`) and monitored values only in
  the TIA positions for the selected display/monitor state. If `PT` is
  unassigned, show that missing operand at the `PT` pin; do not silently
  substitute a preset or hide the invalid state.
- Audit RT separately from IEC timer boxes. Siemens LAD renders `RT` as a
  coil-style reset-timer instruction: it passes rung power through and resets
  the assigned timer structure when energized. A generic `IN`/`ENO` box without
  the assigned timer DB is not equivalent. Siemens also permits RT in a
  mid-line position; the current RungProof model only represents it as a rung
  terminal, so mid-line RT remains explicitly unsupported until the network
  model can represent the through-flow instruction at an interior position.
- Audit the 52 current renderable keys individually. Source inspection and
  successful compilation establish neither TIA fidelity nor visual correctness.
  Capture the actual running editor for each key/state class; any unreviewed key
  remains open and must not be represented as passed.
  The per-key ledger is [`TIA_INSTRUCTION_EVIDENCE_MATRIX.md`](TIA_INSTRUCTION_EVIDENCE_MATRIX.md);
  all keys currently remain open except for explicitly labeled partial captures.

### Operator controls, interlocks, and PLC ownership

- Every demo exposes learner-facing machine Start, Stop, and Reset controls
  with matching symbolic I/O and visible logic/indication. Demos with moving
  actuators, and any dedicated safety lesson, also expose an E-stop/safety-trip
  control and show its status/interlock in the program. A timer/counting-only
  lesson must not add a decorative E-stop with no meaningful machine function.
  App-level simulator Run/Stop/Reset are not machine operator controls or PLC
  reset logic.
- Each taught motor/axis has an inspectable FB with its own instance data,
  run/jog request, System Enable and motor permission, interlocks, fault/reset
  behavior, command output, and feedback. Main OB calls each FB in a visible,
  documented order. A shared permission is an explicit interface signal; one
  high-level `gantry_cycle` coil cannot stand in for separate axis commands.
- Demo 5's sequence belongs in PLC networks and reusable blocks. Main owns
  start/stop/reset, System Enable, safety/permissive checks, seal-in or explicit
  state transitions, batch/layer state, and recovery. X/Y/Z motion and
  vacuum/gripper control use separate PLC blocks and instance DBs. Encoder
  position, at-position windows, home/limit sensors, vacuum feedback,
  carton-present, and layer-complete feedback participate in the sequence.
- The scene plant may integrate commanded motion, attach/release cartons, and
  publish measured encoder/sensor values. It must not calculate phase order,
  decide the next move, latch a PLC request, or infer successful placement in
  place of ladder logic. Remove or migrate script-owned sequence state such as
  `gantry_cycle`; acceptance loads the same authored logic learners see.
- Teach E-stop as a separate simulated fail-safe circuit / safety-controller
  concept with monitored status to the standard PLC. Standard ladder permissives
  can prevent restart and report E-stop state, but must never be presented as a
  certified safety function or permission to bypass a safety device. Restore
  safety, clear the initiating fault, deliberately reset, then require a fresh
  Start where the sequence requires it.

## Requirement-to-work traceability

| New evidence | Required change | Evidence required before acceptance |
| --- | --- | --- |
| Demo 5 seals `gantry_cycle` as one output | Replace it with PLC-owned sequence state and separate axis/motor FB calls from Main; route those commands to plant motion | Ladder block/tree inspection, call/instance DB list, scan trace, encoder/sensor transitions, stopped/faulted/restarted cycles |
| Demos lack machine reset/E-stop controls and logic | Implement machine Start/Stop/Reset controls and visible symbolic logic in all five; implement E-stop/safety-trip controls and interlocks for motion-capable demos and dedicated safety lessons; keep simulator lifecycle controls separate | Visible scene controls and tag table, rungs, reset/start-interlock cases in all five, and safety-trip/restart cases in every applicable demo |
| Network starts, branches, and status do not follow TIA | Use the reference power rail, split/join topology, and per-wire flow projection | Same-logic TIA screenshot comparison; energized and unenergized branch captures with no gaps |
| Timer output tags float/misalign; unwanted tags persist | Centralize operand visibility/placement settings and align live values to terminals | Every timer/counter: tags off/on, supported positions, online/offline, no clipping or rail overlap |
| CTD visual review showed cramped LD/PV and Q/CV rows, a hidden `load` operand, and rung-wire gaps at the instruction edges | Increase IEC counter pin pitch, separate symbolic operand labels from live values, suppress redundant BOOL type text, and distinguish block half-width from output-label space | Re-render CTD in selected/unselected states; verify labels and values remain readable, chips clear the main rung conductor, and wire meets both block edges |
| Gantry batch is represented by a `gantry_cycle` output and motor ownership is hidden | Replace the shortcut with visible main-block sequence/state logic plus independent axis and vacuum/gripper FB calls and instance DBs; retain only plant integration in scene code | Inspect every relevant network and FB interface; trace commands and measured feedback through start, stop, E-stop, reset, and recovery |
| Safety/reset and machine controls are absent or conflated with app controls | Add learner-facing machine controls, corresponding PLC tags/rungs, reset and restart inhibition, and a separately labeled simulated fail-safe boundary | Operate controls in each scene and verify controller status, visible ladder path, and feedback; app Run/Stop/Reset do not satisfy this gate |
| Network start and branch drawings differ from TIA and operands appear in the wrong states | Keep the rung rail and every branch/wire continuous, and apply the TIA tag-information settings consistently at network, pin, and value level | Compare matching TIA states with tags disabled/enabled and true/false/unknown power flow; no standalone rail fragments or extra tags |
| Latest TIA references show a parallel seal-in branch, timer Q/ET pins, and an unassigned TON PT operand | Verify full branch rail/split/join geometry; remove external IEC member tags; align timer values with pin rows; represent missing PT at its pin | Compare the same energized and de-energized parallel rung plus TON valid/unassigned PT states at normal and 100% zoom |
| Delegated comparison found 52 program-instruction keys across seven families with incomplete visual evidence | Track each key, presentation states, and Siemens reference individually; identify local-only/unsupported operations | 52-key evidence matrix with source references and actual running-window captures for each supported comparison |

The Codex goal record does not support editing its objective in place. This
document is the durable acceptance contract for the expanded scope; keep the
goal active until each gate is implemented and visually/behaviorally verified.

Expanded working objective: **Match the supported Siemens TIA Portal LAD
editor, instruction, project-tree, tag-display, and monitoring behavior from
official Siemens references and the supplied screenshots. All five demos must
teach machine controls through visible PLC logic: System Enable, permissions,
interlocks, seal-in or sequence-state behavior, reset/fault recovery, and a
clearly bounded simulated safety concept. Each motor/axis owns an inspectable
FB and instance data, called by Main. Demo 5's visible ladder owns its gantry,
vacuum/gripper, encoder-feedback, and four-carton sequence; scene code models
plant response and reports feedback only. Verify rendered states and actual
ladder/controller/scene behavior. Explicitly identify unsupported features and
unverified states, and do not claim parity until every acceptance gate passes.**

## Execution order for the expanded goal

Use these as dependent gates, not parallel cosmetic tasks. A later gate cannot
be called complete while an earlier shared foundation is still visibly wrong.

| Gate | Work | Exit evidence |
| --- | --- | --- |
| 0. Reference state | For each screenshot comparison, identify the TIA version, CPU, exact project/block/network, zoom, operand representation, tag-information setting, selected/edit/online state, and runtime values. Use the user's screenshots as exact examples and Siemens documentation for semantics. | Reproducible TIA and RungProof captures of the same state; any unavailable TIA state is recorded as unverified, not inferred. |
| 1. Shared LAD canvas | Fix network entry, power rail, branch split/join, continuous conductors, instruction sizing, pin rows, and complete-rung online flow. Correct shared geometry before tuning individual instruction families. | Seal-in/parallel cases with mixed, false, true, and unknown paths inspected at normal and 100% zoom; no gaps, orphan rails, label overlap, or clipped operands. |
| 2. View and operand settings | Independently exercise symbolic/absolute/both operands and hidden/shown/hierarchical tag info at operand/below-network positions. Do not hide real operands or pins when supplementary tag info is off. | Side-by-side captures for each supported setting combination, including timer/counter outputs; hidden means no stale comments, addresses, or synthetic `.Q`/`.ET` labels. |
| 3. Instruction catalog | Review every one of the 52 renderable instruction keys across the applicable offline, selected, edit, and monitored states; separately review project-tree and rung/branch commands. | Completed evidence matrix with a cited Siemens source and inspected running-editor capture for every claimed supported state. Unsupported/local behavior remains labeled. |
| 4. Demo controls and ownership | Audit each authored demo's operator controls, symbolic inputs, controlling rungs, output/feedback path, stop/reset/fault response, and restart rules. Keep app playback controls distinct. | Per-demo controls matrix and operated proofs. E-stop is required where the cell has motion or the lesson teaches safety; do not add a decorative E-stop to a non-motion timer/counting exercise. |
| 5. Demo 5 architecture | Replace the hidden plant-owned phase machine with visible PLC sequence logic, main-block calls, axis/motor and vacuum/gripper FBs with assigned instance data, and feedback-driven transitions. | Scan-level trace from Start through four confirmed placements, including Stop, permissive/safety loss, reset, restart inhibition, encoder/sensor changes, and recovery. Plant code only advances physical state from scanned outputs and reports measurements. |
| 6. Integrated acceptance | Re-run visual and behavioral checks against one identified build and project state; reconcile all failures and limitations in this document and matrix. | All prior gates pass with reviewed evidence. A build, test suite, code review, or RungProof-only capture is not a visual TIA-parity pass by itself. |

### Demo control audit ledger

`OPEN` means this goal has not yet collected a complete current-build evidence
set for that demo. It does not assert that no partial implementation exists.
Demo 5 is marked `FAIL` only for the source-confirmed plant-owned sequence
shortcut below. The current source audit did not find a Demo 3 safety-trip
input/operator route; treat that requirement as missing and open.

| Demo | Current evidence status | Required review |
| --- | --- | --- |
| 1 — Press-count lamp | OPEN | Machine Start/Stop/Reset applicability and wiring; visible input, counter, output, reset behavior, and app-control separation. |
| 2 — Delayed lamp | OPEN | Machine Start/Stop/Reset applicability and wiring; TON behavior, reset/retrigger behavior, output feedback, and app-control separation. |
| 3 — Conveyor sequence | OPEN | Verify Start/Stop/Reset, E-stop/safety trip, seal-in, permissives, motor FB/DB, output and conveyor feedback, fault clear and fresh-start behavior. Existing E-stop-path evidence is not a full pass. |
| 4 — Batch process blocks | OPEN | Verify machine controls, System Enable/permissives, visible block ownership/calls/instances, batch reset and fault recovery, and actual scene feedback. |
| 5 — Integrated palletizing cell | FAIL: source-confirmed hidden sequence ownership | Replace `gantry_cycle` phase control with PLC-owned X/Y/Z and vacuum/gripper logic, visible interlocks/seal-in/reset/restart behavior, instance DBs, and encoder/sensor-closed transitions; account for every visible mechanism. |

The Demo 5 controls review must also show machine-level System Enable, a
learner-facing Start, Stop, Reset, and simulated safety-trip/E-stop control,
and the corresponding visible ladder inputs/rungs. Prove that a stop or trip
removes the affected motion commands, that trip recovery requires the intended
reset/acknowledgment, and that held Start cannot cause an automatic restart.
Keep the scene's plant integration below the PLC command/feedback boundary.

## Source findings requiring correction

- `LadderEditorCanvas.DrawRecursiveConditions` previously drew every topology
  wire using the static `wire` color. The current repair projects each leaf's
  incoming `NetworkEnable` and local result, composes series/parallel outputs,
  and draws each segment from its own flow state. Fresh production-canvas
  captures inspect independent parallel branch states: mixed flow at 100%
  (`.tools/tia-flow-mixed-current.png`), both branches false with an energized
  upstream path at 100% (`.tools/tia-flow-false-current.png`), and a JMP-skipped
  parallel network rendered gray at 100%
  (`.tools/tia-flow-unknown-network4-current.png`). The mixed capture shows a
  continuous green path through the true branch, joined output, coil and right
  rail; the false capture shows a blue dashed false leg; the skipped network
  shows gray conductors. Value badges are `TRUE`/`FALSE` without redundant
  `(BOOL)`. These are current RungProof-only visual evidence, not a TIA
  side-by-side pass or proof of Siemens stale/earlier-cycle value rendering. The
  review fixture uses independent branch tags and valid one-contact paths; its
  capture defaults to no selection so selection affordances cannot contaminate
  an unselected-state image.
- `ObservationIsEnergized` still reports an element-level Boolean. It is not the
  edge-status source used by the new wire renderer. Keep element status and
  conductor power-flow separate; validate branch join behavior and unknown
  propagation before claiming TIA status parity.
- A fresh inspection found that `.tools/tia-branch-start-current.png` still had
  a horizontal mid-rung stub between the short left rail and a detached inner
  branch rail. The earlier audit called this a pass incorrectly. `TreeLeft`
  now aligns a root-parallel split with the TIA network rail, and the renderer
  no longer draws a duplicate short rail over that branch conductor. The
  visually inspected 100% capture `.tools/tia-branch-start-fixed-root-rail.png`
  shows one shared rail/split, continuous branch legs and join, the energized
  coil, green true path, and blue-dashed false seal-in path. This closes only
  the RungProof render check for this topology/state; the installed-TIA
  comparison, insertion/edit/selection states, and other branch states remain
  open.
- TIA selection is now rendered with a dashed blue outline for the reviewed
  CTU fixture. Only that instruction/state was visually checked; selected coils,
  other boxed instructions, and edit mode remain unverified.
- Confirmed current Demo 5 source: the main block seals in one `gantry_cycle`
  output and passes that point, `vacuum_pick`, and pallet permission into the
  palletizer plant model (`AuthoredDemoLadderPrograms.cs` and
  `SceneSimulationRuntime.Palletizer.cs`). The current called FBs do not own
  independent X/Y/Z axis or motor commands. This is the exact hidden-sequence
  shortcut the user rejects; it is not a completed PLC-owned gantry design.
- Source inspection of `scenes/migrated/lab-11-13-xy-palletizing.scene.json`
  confirms the learner-facing machine actions are only a Start pulse and a
  pallet-position-valid toggle. It has no machine Stop, machine Reset, or
  safety-trip/E-stop action and no corresponding input bindings. The global
  simulator playback controls are separate and do not satisfy this requirement.
- `PalletizerPlantModel.Phase` currently owns `PickDown`, `VacuumDwell`,
  `PickUp`, `Transfer`, `PlaceDown`, `Release`, `PlaceUp`, and `Return`.
  `PalletizerPlantModel.Step` advances those phases, attaches/releases the
  carton, increments placement, and restages the next carton. That is sequence
  logic in the plant model, not merely kinematic response. The replacement
  contract is that PLC scan outputs request axis/gripper actions and sequence
  transitions; the plant advances only physical position/attachment from those
  outputs and publishes encoder, limit, carton, vacuum, and placement feedback.
  A four-carton lesson is not accepted until all four transitions are selected
  by the visible ladder and the controller trace proves that relationship.
- The supplied TIA screenshots are now explicit shared-canvas regression
  cases, not generic style inspiration: (a) a parallel seal-in network whose
  branch starts directly at the left power rail and has continuous split/join
  wires with no extra rung stub,
  (b) timer `Q`/`ET` names and values aligned to their own output pins, (c) an
  unassigned TON `PT` shown as `???`, and (d) supplementary tag annotations
  removed everywhere when TIA tag information is hidden. Capture the same
  network/settings in TIA and RungProof; do not infer acceptance from the
  current RungProof-only render or from a build/test result.
- The TIA `View` menu now has separate operand representation, tag-information
  mode, and tag-information position controls. Aggregate STRUCT members also
  carry comment data. The setting-routes verifier and aggregate-comment test
  cover these controls/data paths. This is implementation evidence only: the
  below-network state needs a fresh unclipped capture, and all supported
  instruction families still need state-by-state visual review. Supplementary
  tag information must disappear in Hide mode without removing instruction
  pins or the selected operand representation.
- TON `PT` now has an explicit assigned/unassigned draft state. It persists in
  editor projects (old documents default to assigned), survives cloning, and
  emits `VC007` so an unassigned preset cannot Verify + Load. The instruction
  property panel exposes a direct operand text field, where blank means
  unassigned and renders one red `???` at the PT pin. TIA-style timer input
  rendering now connects every lower `R`/`PT` operand to its own pin with a
  short external lead, without routing the lead through the operand text/chip.
  The missing-`PT` lead uses the amber error color visible in the supplied TIA
  screenshot; assigned lower operands keep the normal conductor color.
  A fresh missing-PT review capture also suppresses monitor values from the
  typed compile fixture so the invalid draft cannot display stale `Q`/`ET`
  results. It includes TIA's `Time` type label and assigned instance DB header.
  The literal parser accepts
  positive `T#` and `TIME#` values with `d/h/m/s/ms` components, optional
  underscores, decimal components that resolve to integer milliseconds, plus
  the legacy bare-ms form. Symbolic TIME tags were not supported in the
  earlier implementation; the current bounded implementation is recorded
  below. Full TIA operand editing is still open. Siemens documents TIME
  constants such as `T#5m_30s`,
  `TIME#10d20h30m20s630ms`, and signed limits; see the official
  [TIME data type reference](https://docs.tia.siemens.cloud/r/en-us/v21/data-types/timers/time-iec-time).
- CTD layout repair now uses a 24 px counter pin pitch, removes redundant BOOL
  type text, gives the symbolic `load` operand its own clear position, and keeps
  the CV chip below the main rung conductor. The rung wire now meets each CTD
  block edge; the TIA block half-width is separated from output-label space.
  CTD selected/unselected and CTU selected captures were visually inspected at
  the captured zoom. Timer families, other instructions, zoom levels, and edit
  states remain unreviewed.
- The selected CTU type field now exposes the TIA screenshot order, including
  the initial `???` state, followed by `Int`, `SInt`, `DInt`, `USInt`, `UInt`,
  `UDInt`, `LInt`, and `ULInt`. A new Siemens CTU/CTD is visibly untyped until
  a type is selected; Verify + Load reports `VC007` instead of silently
  assuming `Int`. Current Godot 100% capture: `.tools/tia-counter-untyped-types.png`;
  controller regression: `TIA counter data-type choices preserve Siemens order
  and untyped state`. The user-supplied TIA screenshot and Siemens CTU reference
  establish the target choices and semantics. The open popup, follow-on Call
  options dialog, and direct side-by-side capture remain unverified.
- A fresh CTU capture exposed one remaining TIA-value formatting leak: contact
  observation badges bypassed `DrawExternalTag`, so they still showed
  `TRUE (BOOL)`. A shared formatter now serves both drawing paths. The refreshed
  `.tools/tia-counter-output-current.png` visibly shows `TRUE`, `FALSE`, and
  numeric chips without redundant BOOL type text; output `Q`/`CV` member labels
  remain absent. The formatter regression test preserves the BOOL suffix in
  Studio 5000 mode and leaves non-BOOL values unchanged.
- CTD model/runtime now exposes `CD`, `LD`, and `PV`; when `LD` is true, it loads
  `PV` into `CV` and ignores `CD`. This behavior has build/test coverage. The
  first CTD capture failed visual review; the later CTD captures pass the
  limited pin/value/wire layout checks listed above. CTUD remains unsupported
  and must not be implied by the catalog as supported.
- Numeric instruction presentation now uses a TIA-specific port map:
  MOVE `IN`/`OUT1`; binary arithmetic and EXPT `IN1`/`IN2`/`OUT`; unary
  operations `IN`/`OUT`; NORM_X/SCALE_X `MIN`/`VALUE`/`MAX`/`OUT`; and EN/ENO
  on the rung-power row. Operand labels now align with their individual data
  pins and the output area reserves space for the destination tag. The helper
  tests all currently modeled numeric kinds, while Godot viewport captures
  inspected offline MOVE, ADD, and NORM_X fixtures at 100%. These are partial
  RungProof renders checked against Siemens documentation and the supplied
  MOVE reference; they are not current-build side-by-side TIA screenshot proof.
  Numeric type selectors, edit states, selected states, online values, other
  numeric keys, and all 75%/150% states remain open. ADD's extendable inputs are
  not modeled; FORMAT_TEXT is local-only.
- The project-tree actions and tag-table model were added in the current
  worktree, but visual interaction and persistence acceptance are still pending.
- FB calls now carry a formal-parameter map and an editable single-instance DB
  number/symbol; missing legacy FB DB identities are migrated deterministically,
  and assigned identities are validated against timer/counter DB collisions.
  `.tools/tia-fb-instance-db-current.png` was captured from the running editor
  and visually inspected: `%DB2` and its quoted symbolic name appear above the
  FB call, and the project tree lists its instance DBs. This is a partial
  RungProof capture only; input/output/InOut pin placement, normal versus
  selected state, and a direct matching TIA image remain open. The scripted
  `--verify-call-authoring` path confirms invalid input rejection, apply,
  persistence, undo, and cancel, not mouse-driven or visual parity.
- The starter challenge emits `CHALLENGE_CONTENT_DISABLED CH_INVALID_CHALLENGE`
  at startup because its stored byte length/SHA-256 does not match the actual
  starter content. Track separately from TIA parity; it can disable scenario
  content and should not be mistaken for successful demo acceptance.

## Verification record

- Current source build: bundled .NET build **0 warnings, 0 errors**.
  `--verify-operand-placement` passes for the current viewport and instruction
  property panel geometry; this check is not instruction-symbol parity evidence.
  `--verify-editor-view-state` passes its scripted TIA/Studio block navigation,
  pending-edit guards, selection restoration, scroll, and zoom checks. A first
  headless attempt could not emit `FramePostDraw`; the passing rerun used the
  windowed Godot renderer.
- Bundled .NET build after the missing-PT implementation:
  **0 warnings, 0 errors**.
- Full virtual-controller suite after TIME literal support:
  **327 passed, 0 failed**. The legacy schema-v1 round-trip regression was
  fixed earlier by omitting instance/type metadata that schema v1 does not
  define; modern schema projects retain those fields.
- `--verify-ladder-editor` reached `EDITOR_COMMAND_ROUTES_VERIFY PASS`. Startup
  still emits `CHALLENGE_CONTENT_DISABLED CH_INVALID_CHALLENGE` because the
  starter challenge byte-length/SHA-256 does not match; this is independent of
  LAD rendering and remains a repository issue.
- `--verify-ladder-editor` now exercises the TIA operand representation, tag
  information mode/position routes, and hierarchical-comment setting. The
  aggregate editor also round-trips a STRUCT member comment. These checks prove
  the setting and data paths only; they do not prove TIA visual parity.
- Current branch-review continuation: renamed the overlapping `height` local in
  `TerminalBounds` to `callHeight` after a fresh build exposed `CS0136`. Repaired
  the monitoring fixture's invalid empty series paths and expanded it to create
  mixed, all-false, and JMP-skipped branch states from actual virtual-controller
  scans. Inspected all three captures listed above: every branch segment is
  continuous; mixed mode has a green true path and blue dashed false path;
  all-false mode has green upstream power and blue dashed paths through the
  output; the skipped branch is gray solid. The review harness now leaves the
  output unselected unless explicitly requested. Bundled .NET build passes with
  zero warnings/errors; `--verify-editor-monitoring` passes. Direct matching
  captures from the installed TIA build, other instruction families, and
  Siemens stale/earlier-cycle shading remain open.
- Current verification after adding editable Siemens TIME literal operands:
  bundled .NET build **0 warnings, 0 errors**; virtual-controller suite
  **327 passed, 0 failed**; `--verify-ladder-editor` and
  `EDITOR_COMMAND_ROUTES_VERIFY` pass. The scripted editor interaction stages
  and applies `TIME#1s500ms`; parser/persistence tests cover Siemens literal
  forms and keep blank `PT` invalid. This is scripted event-path evidence, not
  physical mouse/keyboard acceptance.
- `.tools/tia-tag-info-hidden.png` and
  `.tools/tia-tag-info-operands-final.png` were inspected. The hidden capture
  removes supplementary annotations, and the combined operand/comment capture
  no longer duplicates an address or places output metadata over the coil.
  `.tools/tia-tag-info-below-network.png` is an older capture with tag entries
  compressed into one baseline; it is not evidence for the later row-based
  source change. Capture and inspect the current version before this position
  passes.
- `.tools/tia-timer-missing-pt-final.png` was captured from the running Godot
  editor at 100% zoom and visually inspected alongside the earlier
  `.tools/tia-timer-missing-pt-v2.png` and
  `.tools/tia-timer-missing-pt-combined.png` captures. The TON has `%DB8` and its
  symbolic instance name above the block, `Time` under `TON`, and one red
  `???` aligned to the PT pin in symbolic and combined operand modes. The
  project and executable-program round-trip/compiler tests prove literal PT
  operands persist and the missing draft is rejected with `VC007`. A scripted
  Godot editor interaction verifies a TIME literal is staged then applied in
  the TIA property field. Physical pointer/keyboard input is not yet verified;
  symbolic TIME operands were added later as a bounded global-tag feature and
  are verified only to the extent listed below. Other timer-family states
  remain open.
- `.tools/tia-parity-ctd-selected-v5.png` and
  `.tools/tia-parity-ctd-layout-v5.png` were captured and visually inspected:
  CD/LD/PV operands/values are readable, Q/CV values do not cross the rung
  center conductor, and input/output conductors connect continuously to the
  block. The full CTD acceptance gate still needs address/comment display
  review, other zooms, and edit/offline states.
- `.tools/tia-parity-ctu-after-terminal-geometry.png` was visually inspected;
  CTU pins remain readable and its rung conductors also meet both block edges.
  These captures verify only CTU/CTD monitoring at one zoom; they do not prove
  full instruction-family parity.
- Fresh current-build captures `.tools/tia-timer-output-current.png` and
  `.tools/tia-counter-output-current.png` were visually inspected at 100% in the
  running Godot renderer. TON displays `%DB1`, `"IEC_Timer_0_DB"`, and the
  `Q`/`ET` live values on their respective pin rows; CTU displays `%DB1`,
  `"IEC_Counter_0_DB"`, and the `Q`/`CV` values on their pin rows. Contact
  monitor chips display `TRUE`/`FALSE` without `(BOOL)`. Neither capture renders
  redundant `timer.Q`/`timer.ET` or `count.Q`/`count.CV` output labels. This
  proves the current build's narrow unselected online rendering state only; it
  is not a matched Siemens capture and does not pass either instruction's full
  state gate.
- After sharing the TIA monitor-value formatter across contact badges and
  external value chips, the bundled .NET build passes with **0 warnings, 0
  errors** and the virtual-controller suite passes **333 tests, 0 failures**.
  `--verify-editor-monitoring` also passes for both styles, exact-context
  indications, cadence, and culling. This checks value normalization and
  preserves Rockwell BOOL text; fresh timer and counter captures were made after
  that build and visually inspected. Godot startup still reports the unrelated
  `CHALLENGE_CONTENT_DISABLED CH_INVALID_CHALLENGE` manifest mismatch.
- The initial nested flow fixture failed compilation because empty series paths
  violate `VC003`; that attempt was superseded by the repaired, executable
  fixture and the three later branch captures described above.
- Project-tree action/tag-table interaction and persistence are unverified.
  Physical PLC connection and safety certification are outside this offline
  editor/demo verification.
- Earlier `.tools/tia-parity-ctd-after.png` inspection identified operand and
  wire defects addressed above; that failing image is historical evidence only.

## Objective addendum from latest user review

The task goal service exposes status changes but no way to edit an active goal's
objective. This repository acceptance document is therefore the durable,
expanded working goal for the rest of the task:

> Match the supported Siemens TIA Portal LAD editor behavior using official
> Siemens references and the user's same-state screenshots. Audit every claimed
> instruction and editor control; implement and visually inspect reference-
> backed results, and document unsupported features. At the same time, make the
> authored demos teach visible PLC-owned control: learner-facing machine
> Start/Stop/Reset, meaningful safety-trip exercises for motion lessons,
> System Enable, permissions/interlocks, seal-in or sequence logic, reusable
> called FBs with instance data, and feedback-driven behavior. No background
> script may supply the sequence or actuator commands shown as PLC behavior.
> Verify each demo from scene action through symbolic input, scanned ladder,
> output, and measured plant feedback. Keep the simulated safety boundary
> explicit. Do not claim TIA parity or demo completion until its defined visual
> and functional evidence gates pass.

This addendum extends the repo acceptance contract to the connected training
demos because the user explicitly identified hidden sequence scripts and
missing operator controls as failures of the same teaching objective. Do not
treat the editor goal as satisfied by a visually similar canvas while authored
demos teach an unrealistic control architecture.

### Goal integration: operator controls, network starts, and displayed operands

The user's latest review adds three cross-cutting acceptance gates. They apply
to the current goal and supersede any earlier demo pass or screenshot that did
not inspect these states:

1. **Operator controls are part of the PLC lesson.** Each demo must expose the
   machine controls that make sense for its equipment, bind each control to a
   symbolic input, and show the PLC logic that consumes it. At minimum, prove
   machine Start/Stop/Reset behavior. Add an E-stop/safety-trip exercise to
   motion-capable or otherwise hazardous lessons where meaningful; it must
   remove motion commands, latch/inhibit restart as designed, and require a
   deliberate reset/acknowledgment followed by a fresh Start. Do not imply that
   ordinary LAD or this simulator is a safety-rated function. App-level
   Run/Stop/Reset controls do not satisfy this gate.
2. **Program structure must be visible in the scanned blocks.** Main OB should
   call reusable motor/axis and gripper FBs with explicit instance identities
   and DBs. System enable, drive/motor permissions, interlocks, seal-in or
   sequence state, trip handling, reset conditions, and feedback transitions
   must be inspectable in the relevant LAD networks. Scene scripts may convert
   PLC outputs into simulated physical movement and publish measured
   encoders/sensors; they may not advance hidden sequence phases or synthesize
   the next actuator command.
3. **Every TIA comparison includes the start and operand-display state.** A
   network begins at its left power rail; its rung and parallel branches are
   electrically continuous and connected, with no detached rail stubs, gaps,
   or decorative wires. Operand representation and supplementary tag
   information are independent display choices: changing or hiding tag
   information must not remove a real operand, invent an operand, or leave a
   duplicate/stale member name or clipped value. Record the exact TIA display
   mode before comparing captures.

These requirements do not assert that Siemens mandates one FB per motor or
that every TIA project uses this exact architecture. The independently called
axis/motor FB layout is the requested RungProof teaching model. TIA supports
single, multi, and parameter instances; each single instance stores its own
working state in its instance DB. Siemens' LAD reference defines each network
as a power rail with at least one rung and permits connected parallel branches
([LAD elements, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A),
[instances, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/block-calls/instances/basics-of-instances),
[instance DBs, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg)).
Siemens describes emergency-stop behavior in STEP 7 Safety, including
acknowledgment/restart behavior; RungProof can teach the standard-program
response to a simulated safety-system status, but does not implement or certify
that safety function ([ESTOP1, STEP 7 Safety V21](https://docs.tia.siemens.cloud/r/en-us/v21/step-7-safety-v21-instructions/safety-functions/estop1-emergency-stop/emergency-off-up-to-stop-category-1-step-7-safety-v21)).

**Evidence gate:** code inspection and automated checks alone do not close
these requirements. Capture the matching TIA and RungProof project/block,
network start, operand/tag mode, and online state; then operate the visible
controls and correlate symbolic inputs, scanned outputs, measured plant
feedback, and motion. Keep an explicit limitation for any TIA behavior not
implemented.

The latest four user screenshots are new failure cases, not approval captures:

| Evidence | Acceptance requirement | Proof still required |
| --- | --- | --- |
| TIA seal-in rung and RungProof branch comparison (`codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`, `codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png`) | A network begins at the TIA power rail. The top permissive path and lower seal-in path must be continuous parallel conductors that split from and rejoin the same rung before the output. Do not create an isolated short rail, a detached branch, or a gap at the branch entrance. The branch must implement the same seal-in behavior as the reference, not merely resemble it. | Capture the same energized and de-energized seal-in circuit in TIA and RungProof at the same zoom; inspect the full rail, split, both paths, join, output, and power-flow colors. |
| TIA timer `Q`/`ET` and RungProof output comparison (`codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`, `codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png`) | Timer outputs and monitored values line up with their own `Q` and `ET` pins. An unassigned `PT` remains visibly unassigned at the `PT` input; do not fabricate a preset or move the missing operand elsewhere. No tag/value may overlap a pin, block, conductor, or rail. | For TON, TOF, TP, and RTO, compare assigned and unassigned presets, online/offline values, and tag display modes against matching TIA states. |
| User report that tags remain visible when they should be hidden | Apply Siemens' independent settings correctly: operand representation controls the operand form; tag-information Hide removes the supplementary tag information. Do not confuse an actual operand with an address/comment annotation, and do not leave stale IEC member names such as `timer.Q`/`timer.ET` beside output pins. | Use a four-state comparison (symbolic/absolute/both operand form × tag info hidden/shown) for each relevant instruction and verify every annotation is unclipped. |
| Demo 5 gantry and control architecture described in the user's request | Main OB visibly calls independently instantiated axis/motor FB logic and a vacuum/gripper FB. The requested per-axis FB layout is a RungProof teaching architecture, not a claim that Siemens requires one FB per motor. PLC networks own System Enable, motor permissions/interlocks, safety-system status, seal-in/sequence state, explicit reset/fault recovery, and encoder/sensor-based transitions. A single `gantry_cycle` actuator output or background-script phase machine does not meet this requirement. Scene code only applies PLC actuator commands and returns measured encoder/sensor feedback. **Current source audit: fails.** `AuthoredDemoLadderPrograms.CreateIntegratedPalletizer` authors a single `gantry_cycle` coil; `SceneSimulationRuntime.AdvancePalletizerPlant` advances motion from that signal and `vacuum_pick`. Encoder tags report plant position but do not yet close per-axis PLC command/feedback loops. | Inspect Main, axis and gripper FB interfaces, and each instance DB; trace Start, Stop, permissive loss, simulated safety trip, reset, restart inhibition, encoder transitions, pick/place, and four-box layer completion through rendered ladder, scan data, and scene. Do not accept the prior scripted palletizer sequence or its historical functional checks as proof of this expanded architecture. |
| User reports missing machine controls across demos | Audit every demo's operator controls instead of assuming all demos are equally incomplete. Add appropriate visible machine controls (including Reset) with symbolic tags and visible PLC logic in all five. Motion-capable demos and dedicated safety lessons also need an E-stop/safety-trip control and interlock; do not add a nonfunctional E-stop to a timer/counting-only lesson. These controls remain separate from app-level simulator Run/Stop/Reset. Current source did not confirm a Demo 3 E-stop route. A simulated safety-status input may inhibit standard logic for training but must be labeled non-certified and must not imply ordinary PLC logic replaces a safety relay/F-CPU/safety-rated output path. | Operate each required machine control; operate and verify safety trip in every applicable demo. Confirm input, latch/interlock logic, actuator command, feedback, reset permissive, and restart behavior in one identified running build. |
| Demo 5 unused arm and auxiliary equipment | The robot arm and auxiliary stand/pump shown in the scene are either assigned explicit PLC-controlled functions and feedback within the lesson, or removed/repositioned so they are not misleading distractors. Each animated actuator must have a traceable command and feedback path. | Inspect the scene I/O map and trace each displayed mechanism from network/block output through plant movement and back to sensor/tag feedback. |

### Current source cross-check against the new captures and control requirements

This is a source-level defect report, not a pass:

- `src/VirtualController/AuthoredDemoLadderPrograms.cs` still declares and
  coils the single output `gantry_cycle`, then uses it to gate vacuum and
  cell-active logic. `src/Scenes/SceneSimulationRuntime.Palletizer.cs` passes
  that output directly into `_palletizer.Step(...)`. Demo 5 therefore does not
  expose separately commanded X/Y/Z axes or prove that PLC-observed feedback
  selects each next move. Historical palletizer tests cover the old cycle
  contract and cannot close this architecture gate.
- The Demo 5 program source has no separate scanned machine Stop, Reset,
  E-stop/safety-status, drive-ready, per-axis enable/interlock, or motor-FB
  call/instance-DB network set. The app controller's Run/Stop/Reset lifecycle
  is not a substitute. Add operator controls only where each has a real tag,
  visible logic, and demonstrated scene response. The simulated safety status
  must not be presented as certified safety.
- The latest TIA screenshots call for the network to begin at the left power
  rail, for real branch geometry and uninterrupted conductors, and for timer
  `Q`/`ET` values to sit on their pin rows. Fresh production captures
  `.tools/tia-timer-unassigned-current.png` and
  `.tools/tia-timer-assigned-current.png` now show the missing-PT marker and
  assigned PT operand on separate lower-pin leads (amber for the missing
  operand), keep `Q`/`ET` values at their
  pins, and omit synthetic `timer.Q`/`timer.ET` labels. The same-state installed
  TIA comparison and other timer display/edit states remain open. In particular,
  supplementary member labels such as `pick_timer.Q`/`.ET` must disappear when
  tag information is hidden.
- The delegated timer source audit found an RT mismatch. The current TIA
  renderer now draws a coil-style `RT`, its assigned DB heading, and continuous
  power-flow conductors. Production-canvas captures at 100% were visually
  inspected: `.tools/tia-rt-coil-fixed.png` (powered),
  `.tools/tia-rt-coil-unpowered.png` (false blue-dashed flow),
  `.tools/tia-rt-coil-offline.png` (unmonitored), and
  `.tools/tia-rt-coil-selected.png` (selected). A fresh 2026-10-13 production
  capture, `.tools/tia-rt-current.png`, independently confirms the powered RT
  coil, assigned `%DB1`/`"IEC_Timer_0_DB"` heading, and continuous green
  conductor through the coil. Siemens STEP 7 V20's [RT LAD reference](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/timer-operations-s7-1200-s7-1500/rt-reset-timer-s7-1200-s7-1500?contentId=AKFPqRIboeN2pXJD3JZD~w)
  confirms that RT resets the assigned IEC timer when RLO is true and does not
  alter RLO. Same-state comparison with the installed TIA build remains open;
  the current data model supports RT only at the network terminal, not Siemens'
  mid-line placement.
- Keep prior wire-gap findings scoped to evidence: current TIA conductor-gap
  sizing no longer has the previously suspected 12 px discontinuity; that gap
  remains only in a non-TIA fallback. This does not close left-edge/branch
  geometry or false/unknown branch-state captures.
- The current flow fixture now uses independent parallel-branch inputs and
  includes both an all-false branch state and a separate unexecuted network
  after `JMP`. Its matching captures are recorded in the branch-monitoring
  section; keep all other instruction states open until separately inspected.
- Timer editing previously rejected a symbolic TIME tag at `PT`. The current
  slice adds persisted global `TIME` tags, literal initial values, compiler
  type checks, and scan-time resolution of a symbolic TIME preset. The offline
  runtime stores TIME as whole milliseconds and supports only nonnegative S7
  TIME values through `T#2147483647ms`; `LTIME`, nanosecond precision, and TIME
  formal parameters inside called blocks remain unsupported. Capture the
  running tag editor, timer property editor, and timer monitor with a changed
  TIME input before closing this item.

`docs/DEMO_FUNCTIONAL_ACCEPTANCE.md` records bounded functional results for the
old Demo 5 model. Retain them as historical evidence only. Four-carton
completion is not proof of per-axis FBs, operator controls, safety status, or
PLC-owned sequencing. Re-run acceptance only after the learner-visible LAD and
plant interface have been changed to the new model.

The screenshots and their governing source references are specific acceptance
inputs: Siemens states that a LAD network has a power rail with at least one
rung, that programming starts at the left edge of the rung, and that parallel
branches are part of the rung topology ([LAD elements, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A)). Siemens also defines tag information (tag name/address/comments) as separately showable or hideable, with a selected position at the operand or below the network ([tag information, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information)). FB instance working data belongs to the instance; Siemens distinguishes single, multi, and parameter instances ([instances, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/block-calls/instances/basics-of-instances)).

### Demo control architecture gate

Treat Demo 5 as a PLC programming lesson, not a background gantry animation. Its
acceptance model is:

1. Main OB calls the cell permissive/enable logic, three independently addressed
   gantry axis/motor FB instances, a vacuum/gripper FB, and the sequence logic.
   Each persistent FB instance has its own declared instance DB. The X/Y/Z
   command and status tags remain separately inspectable.
2. A system-enable rung and each axis' motor-permissive rung combine Start/run
   request, stop chain, safety-system status, drive-ready/fault feedback, limits,
   and the conditions needed for that axis. Seal-in state is visible in ladder.
   Reset is a separate command; it clears a latched fault only when its cause
   is clear and does not itself issue a motion command.
3. The sequence advances from encoder and station feedback, vacuum/grip proof,
   and placement confirmation. A full layer requires four confirmed transfers.
   Loss of a permissive removes commands and requires a fresh, valid restart.
4. Scene code maps controller outputs to plant motion and plant sensors/encoder
   values back to controller inputs. It may model acceleration, limits, and
   collision response; it may not choose sequence steps, fabricate completion,
   or run a `Gantry Cycle` state machine independent of scanned PLC logic.
5. The visible arm and auxiliary equipment must be used in the lesson or removed
   from the work area. The student can follow each physical-looking action to a
   named command, FB instance, monitored feedback, and ladder transition.

This is a simulator teaching model. Siemens' S7-1500 documentation describes
axis control with motion-control instructions such as `MC_Power` and
`MC_MoveAbsolute`, technology objects, and instruction instances; that behavior
is not currently implemented by RungProof. If the lesson claims native Siemens
motion-control fidelity, replace the discrete-axis subset with the documented
technology-object model and expose its limits. See [MC_Power](https://docs.tia.siemens.cloud/r/en-us/v21/using-s7-1500-motion-control-s7-1500-s7-1500t/using-s7-1500/s7-1500t-axis-functions-s7-1500-s7-1500t/axis-functions-s7-1500-s7-1500t/enabling-a-technology-object-s7-1500-s7-1500t/enabling-a-technology-object-with-the-motion-control-instruction-mc_power-s7-1500-s7-1500t?contentId=x0Pyo4LCctVSKyQMgxdZFw), [MC_MoveAbsolute](https://docs.tia.siemens.cloud/r/en-us/v20/s7-1500-motion-control-s7-1500-s7-1500t/s7-1500-motion-control-v9-s7-1500-s7-1500t/mc_moveabsolute-v9-s7-1500-s7-1500t/mc_moveabsolute-position-axis-absolutely-v9-s7-1500-s7-1500t?contentId=MJzk4IyUKpGvRYBIOtiPhA), and [cyclic OB execution](https://docs.tia.siemens.cloud/r/en-us/v21/functional-description-of-s7-1200-cpus-s7-1200/organization-blocks-s7-1200/cyclic-obs-s7-1200).

The E-stop/safety-trip exercise is a simulated input contract only. It may teach
how a standard program responds to a safety-system status and prevents automatic
restart, but a simulator or ordinary PLC logic cannot establish a safety-rated
emergency-stop function. Siemens documents dedicated emergency-stop functions
inside STEP 7 Safety (for example, [ESTOP1](https://docs.tia.siemens.cloud/r/en-us/v20/step-7-safety-v20-instructions/safety-functions/estop1-emergency-stop/off-up-to-stop-category-1-step-7-safety-v20));
it notes further measures may be necessary for standards compliance. Real design
requires risk assessment, suitable safety-rated devices/architecture, and
validation against the applicable standards; those are outside RungProof's
current simulation claim.

The all-demo audit must record, per demo, the operator control, bound scene
action/input, symbolic tag, controlling rung/block, output/feedback path,
interruption behavior, reset conditions, and restart test. Any control that is
decorative, handled outside the scanned program, or has no proven scene response
remains an open defect. Existing Demo 3 evidence is not silently generalized to
other demos.

These cases remain **open** until the requested rendered and behavioral evidence
exists. Automated build/test results, source inspection, a scripted editor event,
or a screenshot of only one instruction do not close them. Do not describe a
demo as PLC-controlled while its script still sequences motion, and do not call
the editor TIA-matched while any supported instruction or state remains
unreviewed.

The new [`TIA_INSTRUCTION_EVIDENCE_MATRIX.md`](TIA_INSTRUCTION_EVIDENCE_MATRIX.md)
was checked against the current catalog: all 52 program-instruction keys are
present, with `branch` and `rung` listed separately as editor operations. This
is inventory validation only; the visual state gates remain open. The current
turn's targeted capture evidence is recorded below.

### Current turn: network chrome and tag-information visual check

The Siemens canvas now uses a white network work area, a neutral gray network
header, and black offline conductors, matching the supplied TIA screenshots.
It no longer carries the cyan active-network wash or decorative teal stripe
from the generic RungProof presentation. Empty composite nodes no longer print
RungProof evaluator prose such as `EMPTY SERIES · TRUE` in Siemens view; their
editable wire topology remains. The tag-information review fixture is now a
normal TIA-style seal-in rung with Start, permissive, NC layer-complete, and a
holding contact, so the hide/show captures test relevant content.

Post-change evidence from the current source:

- `.tools/tia-seal-final-hidden.png`,
  `.tools/tia-seal-final-shown.png`, and
  `.tools/tia-seal-below-network.png` were captured from the production
  `LadderEditorCanvas` at 100% and visually inspected. Hiding tag information
  removes address/comment annotations while retaining actual symbolic
  operands; showing it adds annotations at operands; Below network moves the
  annotations into a list under the conductors. Network colors and basic
  seal-in branch topology were inspected in all three states.
- `--editor-zoom-review` completed its scripted 75%, 100%, and 150% routing,
  scroll, selection-target, and document-immutability checks for TIA and Logix.
- `dotnet build RungProof.Next.csproj --no-restore` passed with 0 warnings and
  0 errors; the virtual-controller suite passed 330 checks with 0 failures.
- This remains a **partial RungProof-only visual review**, not proof of exact
  TIA parity: the images have not been captured side by side from the user's
  installed TIA version, and instruction selection, operand edit, online
  true/false/unknown, and all tag display modes still need their own review.

### Current continuation: monitoring flow and tag-information review

Fresh 100% captures from the current build's production `LadderEditorCanvas`
review harness were visually inspected:

- `.tools/tia-tag-hidden-current.png`: address/comment annotations are absent;
  symbolic operands and the seal-in topology remain visible.
- `.tools/tia-tag-shown-current.png`: address/comment annotations appear above
  operands without replacing the symbolic operand labels.
- `.tools/tia-tag-below-current.png`: the annotations move into the list below
  the network; no conductor or operand is removed.

These establish only the RungProof rendering for those three settings. The
installed TIA version was not captured in the same state, and absolute/both
operand forms, hierarchical comments, selected/edit states, 75%/150% zoom, and
other setting combinations remain open. In particular, Hide must remove
supplementary tag information; it must not erase a real symbolic operand or an
IEC block's pins. This follows Siemens' separate operand-representation and
tag-information controls ([program-editor settings, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/general-settings-for-the-plc-programming/overview-of-the-general-settings), [display/hide tag information, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information)).

Current verification on this Windows worktree:

- `.tools/dotnet/dotnet.exe build RungProof.Next.csproj --no-restore --nologo`:
  **0 warnings, 0 errors**.
- Godot `--verify-editor-view-state`: **EDITOR_VIEW_STATE_VERIFY PASS**.
- `dotnet run --project tests/RungProof.Next.VirtualController.Tests.csproj
  --no-restore`: **333 passed, 0 failed**. This includes existing palletizer
  behavior tests; those do not prove the requested PLC-owned axis/sequence
  architecture, which remains a confirmed failure.
- Godot `--verify-editor-monitoring`: **EDITOR_MONITORING_VERIFY PASS** for
  exact-context indications, shared counter state, refresh cadence, and visible
  instruction culling. Current mixed/false/unknown captures were generated
  separately and visually inspected; the verifier itself is not TIA visual
  parity evidence.
- Godot startup still warns
  `CHALLENGE_CONTENT_DISABLED CH_INVALID_CHALLENGE` because of a starter-content
  hash/length mismatch. This is a separate content issue and not evidence for
  or against the TIA renderer.

The current canvas was run in a visible Godot window for capture, but these are
review-harness captures, not same-version side-by-side captures from the user's
installed TIA Portal. Visual parity remains unproven.

### Current continuation: TIA coil power-flow continuity

The prior coil renderer colored the external rung from the evaluated state
but drew the short conductor leads and coil glyph as dark when false or
unexecuted. In TIA monitoring mode, ordinary, set, and reset coils now use the
terminal instruction's evaluated network flow for both internal leads and the
coil symbol; Logix and offline colors retain their existing behavior. The
review harness also asserts that assignment-coil input flow agrees with the
fixture's mixed/all-false state.

Fresh visible production-canvas captures were inspected:

- `.tools/tia-coil-false-current.png`: the de-energized assignment-coil path,
  both short leads, and coil glyph are blue dashed; an independent true path
  remains green.
- `.tools/tia-coil-true-current.png`: green power flow remains continuous
  through the energized assignment coil; the false parallel branch remains
  blue dashed.
- `.tools/tia-coil-unknown-current.png`: the JMP-skipped network, including
  contacts, branch conductors, terminal leads, coil glyph, and rail, remains
  gray solid as unexecuted/unknown.

This fixes one monitored-output rendering defect. It does not prove same-state
TIA visual parity, all coil types/instructions, or the full supported
instruction matrix. The installed TIA comparison and remaining instruction
states remain open.

The same review exposed an EN/ENO wire-state defect for numeric boxes. The
network-level conductor immediately before a box now uses its EN state, while
the conductor after the box/right rail uses ENO. `.tools/tia-div-en-eno-fault-current.png`
was captured and visually inspected from the current production canvas with
the DIV-by-zero review fixture: the scanned path and EN are green, while the
faulted ENO and downstream conductor/right rail are blue dashed. The fixture
asserts that the DIV receives enabled power and records an execution fault.
This matches the Siemens rule that instructions execute when EN is true and
terminate power flow at ENO when an enabled instruction errors ([EN/ENO for
LAD/FBD/SCL, STEP 7 V20](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/programming-concepts/programming-language/en-and-eno-for-lad-fbd-and-scl)).
Exact same-version TIA geometry and other numeric instruction error states
remain unverified.

### Branch-state review reconciliation

An independent review reported two missing cases, but reinspection of the
current checkout disproved that finding: `Main.EditorMonitoringReview.cs`
defines independent normally-open `branch_a` and `branch_b` paths, sets both
false for the `false` state while the upstream enable is true, and places a
nonempty parallel network after the taken `JMP`. The inspected captures
`.tools/tia-flow-false-current.png` and
`.tools/tia-flow-unknown-network4-current.png` show those cases: both false
legs/right-side flow are blue dashed, while the skipped network is gray. These
close the **RungProof-only fixture coverage** for those two states; they do not
establish parity against the user's installed TIA version.

The remaining monitoring comparison is current-cycle versus prior-cycle values:
compare Siemens' saturation cue with RungProof. If RungProof has no equivalent
state, record that as a limitation instead of inferring parity from color alone.

### IEC TON pin-row correction

The current TON renderer had centered two pin rows around the rung conductor,
which put `IN`/`Q` above the rung and made the timer body taller than the user's
TIA reference. It now uses the reference's compact 126 by 70 px body, with
`IN`/`Q` at the rung and `PT`/`ET` on the lower row. The assigned instance DB
label remains above the body; an unassigned `PT` remains `???` at that pin.

Current-build captures from the production editor were visually inspected:

- `.tools/tia-timer-output-realigned.png`: monitored TON, assigned `PT`, false
  `Q`, elapsed `ET`, and DB identity.
- `.tools/tia-timer-missing-pt-realigned.png`: offline TON with unassigned
  `PT`; the missing operand is visible at the `PT` pin and the rung stays
  continuous.

This is a RungProof rendering correction based on the user's TIA screenshot,
not an exact-parity claim. The capture is not side-by-side with the installed
TIA build. Siemens' S7-1500 V21 LAD reference confirms TON pins `IN`, `PT`, `Q`,
and `ET`, and that an IEC timer call is assigned instance data; TOF and TP use
the same four parameter names ([TON](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/ton-generate-on-delay-s7-1200-s7-1500-s7-1200-g2), [TOF](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/tof-generate-off-delay-s7-1200-s7-1500-s7-1200-g2?contentId=tduU5NPra2zAa2IVt4F4Sg), [TP](https://docs.tia.siemens.cloud/r/en-us/v21/fbd-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/tp-generate-pulse-s7-1200-s7-1500-s7-1200-g2)). TOF, TP, and TONR still require their own rendered-state inspection; do not assume the TON capture passes those keys.

Verification for this change: `.tools/dotnet/dotnet.exe build
RungProof.Next.csproj --no-restore --nologo` succeeded with 0 warnings and 0
errors. The production monitor and missing-PT review fixtures completed, and
both screenshots were inspected. Matching TIA-version captures, selection and
operand-edit states, and the other IEC timer forms remain open.

The focused runtime checks also passed: Godot `--verify-editor-monitoring`
reported `EDITOR_MONITORING_VERIFY PASS`, and `--verify-editor-view-state`
reported `EDITOR_VIEW_STATE_VERIFY PASS`. Godot still emits the known
`CHALLENGE_CONTENT_DISABLED CH_INVALID_CHALLENGE` starter-content hash/length
warning; this is unrelated to the timer geometry change. No virtual-controller
suite was rerun for this render-only adjustment.

### IEC timer-family review captures

The production monitoring review harness now accepts `timer-tof`, `timer-tp`,
and `timer-tonr`, so each IEC timer can be captured through the same canvas and
instance-data path. Current-state images were inspected at 100%:

- `.tools/tia-timer-tof-current.png`: TOF, assigned instance DB, active input,
  `Q=TRUE`, `ET=0 ms`, and lower PT/ET rows.
- `.tools/tia-timer-tp-current.png`: TP, assigned instance DB, active pulse,
  `Q=TRUE`, `ET=10 ms`, and lower PT/ET rows.
- `.tools/tia-timer-tonr-current.png`: TONR, assigned instance DB, and the
  additional `R` input row alongside `IN`, `PT`, `Q`, and `ET`.

These are RungProof captures only. Siemens V21 documentation supplies TOF
parameters; the found TP page is FBD and is used only for semantic pins. The
TONR reference is Siemens STEP 7 V20 LAD. No one-state capture closes a timer
key: each still requires matching TIA layout, reset/expiry transitions,
operand-edit, selected, and online states. In particular, the extra TONR `R`
row needs a direct TIA layout comparison before its geometry is accepted.

### Goal update from Demo 5 and timer screenshots

The user's latest four screenshots are additional acceptance evidence for the
active goal, not approval of the existing implementation. They add these
specific checks:

| Evidence | Required behavior | Verification |
| --- | --- | --- |
| Demo 5 Network 7 and branch close-up | Render a conventional TIA LAD network: left power rail at the network origin; the start/permissive path and seal-in path are connected parallel branches that split and rejoin before the coil. The first network/branch must not begin on a detached inner rail or with a gap. For Demo 5 itself, replace the single `gantry_cycle` seal-in/output with PLC-authored sequence state and individually commanded axis/motor FB calls from Main. Keep System Enable, Start/Stop/Reset, simulated safety status, motor permissions, interlocks, seal-in, faults, and encoder/limit feedback visible in the authored blocks. | Inspect the entire rendered first and subsequent networks at 100% and 150%; run traces for start, stop, permissive loss, safety trip, reset, restart inhibit, encoder transitions, pick/place, and four-box completion. Inspect Main calls, per-motor FBs, instance DBs, scan commands, and plant feedback. |
| IEC timer output-pin screenshot | Place `Q` and `ET` values at their own timer output pins; preserve timer-instance DB identity above the block. In TIA view, do not add unsupported/stale member annotations beside the pins; only render symbolic/address/comment information when the selected TIA editor settings call for it. Time values use TIA TIME notation, e.g. `T#0ms`, and remain aligned without overlapping wires, pins, or rails. | Capture TON/TOF/TP/TONR with false/true/unknown states and hidden/shown/below tag information. Compare the timer body and pin rows with the installed TIA reference at the same zoom. |
| TIA TON with missing `PT` | The instance DB label remains above the timer; the undefined preset appears at `PT` (`???` in the supplied state) rather than being filled with a fabricated literal or shifted to another row. Validation must explain that the PT operand is not assigned. | Inspect offline and monitored editor captures, then exercise Verify + Load and confirm the missing-PT diagnostic points to that timer/network. |
| User report: tags remain visible when they should not | Separate the actual timer/member pins and connected operands from optional operand representation and supplementary tag information. Hide suppresses only supplementary tag information. Do not leave stale or clipped annotations in any mode. | Compare symbolic, absolute, and both operand representations crossed with hidden, shown, and below-network tag information; inspect all relevant output pins and tag-value rows for clipping. |

These criteria follow the supplied TIA images plus the Siemens STEP 7
references: Siemens defines the power rail, rung origin, series/parallel
connections, and execution order in its [LAD rung overview](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/rungs-in-lad/basic-information-on-rungs-in-lad)
and [LAD element-placement rules](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/rules-for-the-use-of-lad-elements); an IEC TON has `IN`, `PT`, `Q`, and `ET` and requires assigned instance data ([TON, S7-1500](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/ton-start-on-delay-timer-s7-1200-s7-1500-s7-1200-g2)); and the `PT` value is `TIME` ([IEC timer data and behavior](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/basic-instructions/timer-operations/timer-instructions-iec-timers)). The user's installed TIA build remains the visual authority because its exact version is unknown.

The active goal's PLC-architecture gate remains open and currently fails in
source: `AuthoredDemoLadderPrograms.CreateIntegratedPalletizer` still authors
`gantry_cycle`, and `SceneSimulationRuntime.AdvancePalletizerPlant` sends that
high-level signal to plant sequencing. Adding visible coils or controls around
that path is not sufficient. The replacement must move state transitions and
actuator commands into scan-executed Main/FB logic; scene code may integrate
motion and return measured feedback only. Simulated E-stop is an instructional
input and must not be represented as safety-rated logic. No part of these
screenshots authorizes a physical PLC connection or safety-system change.

The bounded symbolic-TIME implementation was rechecked against the new
acceptance item: the current production-canvas capture
`.tools/tia-timer-symbolic-time-current.png` shows the running symbolic PT as
`T#60ms` and ET as `T#10ms`. The virtual-controller suite passes **335/335**;
the focused test changes the TIME preset between scans and verifies the timer
uses the new value. `RungProof.Next.csproj` builds with zero warnings/errors.
This verifies RungProof's dynamic TIME behavior and formatting only; exact pin
placement, display-setting parity, and a same-state comparison with the
installed TIA editor remain open. The scope is global scalar TIME tags with
whole-millisecond values; it does not implement LTIME, multi-instance local
timer declarations, or every TIA timer/editor option.

### Goal update from the latest Demo 5 and TIA screenshots

These four captures extend the existing acceptance contract; they do not
approve the current rendering or demo architecture. They are added to the
active goal so later implementation and review use the same concrete target.

| Evidence | Added acceptance requirement | Verification required |
| --- | --- | --- |
| Demo 5 Network 7 and branch-origin close-up (`codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`, `codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png`) | Render the TIA network origin and seal-in branch as one connected ladder structure. The left power rail is the network origin; both parallel paths split from and rejoin the same rung. No additional inner rail, detached branch start, unexplained stub, or conductor gap. The same requirement applies to the first rung of every network, not only Network 7. | Compare each branch and first-rung state with the user's TIA reference at matched zoom; verify connectivity, scan result, and true/false/unknown online wire status. |
| Timer output-pin crop (`codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`) | Keep `Q` and `ET` operands/values aligned with their own pins. Apply TIA operand representation and tag-information visibility independently. If supplementary tag information is hidden, remove extra labels such as `pick_timer.Q` and `pick_timer.ET`; do not remove actual operands or pin names. No label/value may overlap or be clipped by the block, wire, or rail. | Compare the same timer in symbolic, absolute, and both operand modes, crossed with tag information hidden/shown/below-network; inspect offline, selected, and monitored states. |
| TIA TON with unassigned `PT` (`codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png`) | Preserve the assigned instance DB heading above the block. Show the missing preset at `PT` as unassigned (`???` in this TIA state), without inventing a default or relocating the missing value. Validation must identify the missing PT operand. | Inspect matching TIA/RungProof captures and verify the program cannot Verify + Load until PT is assigned; confirm the diagnostic identifies the correct timer and network. |
| User report: Demo 5 uses hidden `Gantry Cycle` sequencing and has no visible machine controls | Do not use a plant-owned phase enum or a high-level `gantry_cycle` bit to select the next machine movement. Main owns the visible sequence and calls independently instantiated X/Y/Z axis or motor FBs plus a vacuum/gripper FB. Networks expose System Enable, safety-system status, start/stop/reset, motor permissions, interlocks, seal-in/state transitions, faults, and feedback-driven restart inhibition. Each output command is tied to a scanned PLC decision. Add a simulated E-stop/safety-trip operator control and explicitly identify it as non-safety-rated instruction. | Inspect Main, every called FB/interface and instance DB, the tag/I/O map, and all relevant networks. Trace start, stop, permissive loss, safety trip, reset, restart inhibition, encoder/limit transitions, vacuum pickup/release, and all four placements through PLC scans and scene feedback. |
| User report: visible robot arm and auxiliary stand/pump are not used in the palletizing lesson | Every animated mechanism must have an explicit instructional role and traceable PLC command/feedback path, or be removed/repositioned as nonfunctional scenery. Do not imply the arm or auxiliary equipment participates in the gantry process if it does not. | Inventory every visible moving/tool asset against the scene I/O map, ladder outputs, called blocks, and feedback. Verify motion is caused by scanned outputs. |
| User report: machine controls and reset/E-stop logic are missing or hidden across demos | Audit each authored demo independently. Show learner-facing machine Start/Stop/Reset and the logic/tags that consume them. Add safety-trip controls only to motion-capable or otherwise appropriate lessons, and keep them distinct from app playback controls. No control may be decorative or bypass the program. | Build a per-demo control inventory; operate each input and trace it through the rendered ladder, scan data, actuator/output response, and feedback. Prior demo checks for a different control contract do not pass this gate. |

### Source confirmation and status

The latest screenshots are consistent with, and sharpen, the earlier criteria;
they do not create a new narrower goal. The current source remains a confirmed
failure for Demo 5: `AuthoredDemoLadderPrograms.CreateIntegratedPalletizer`
authors a single `gantry_cycle` output, while
`PalletizerPlantModel.Step` owns the `PickDown`, `VacuumDwell`, `PickUp`,
`Transfer`, `PlaceDown`, `Release`, `PlaceUp`, and `Return` transitions.
`SceneSimulationRuntime.AdvancePalletizerPlant` advances that model from
`gantry_cycle`, `vacuum_pick`, and a pallet-valid input. Encoder counts are
published from the plant pose but do not select PLC sequence transitions or
close independent axis command loops. This is hidden sequence ownership, even
though the old test suite checks the resulting simulated cycle. That test
evidence cannot satisfy the new architecture gate.

The current renderer/settings must also be reviewed against the exact operand
and tag-information modes before retaining any symbolic member labels. Earlier
timer captures that omit `timer.Q`/`timer.ET` do not establish that all
instructions and all view modes are correct. The first-rung/branch-origin
comparison, machine-control inventory for all demos, and same-state visual
comparison against the user's installed TIA build remain open. The existing
active goal therefore stays open; this update records scope and acceptance
evidence, not implementation completion.

### Control-ownership interpretation added from the latest user review

For the five authored demos, a control is accepted only when it can be followed
from a visible scene/operator input to its PLC tag, a consuming rung or called
block, the resulting scanned output, and the corresponding plant feedback.
Application playback controls and background scene callbacks are not substitutes
for that path. Document each demo's machine Start/Stop/Reset behavior separately;
for motion-capable lessons, include a simulated safety-trip/E-stop path where it
fits the lesson, with command removal and deliberate restart behavior visible in
the PLC program. This is instructional simulation, not a safety function.

For Demo 5 specifically, Main must call separately instantiated axis/motor
function blocks (with visible instance DBs) and a vacuum/gripper block. PLC
logic owns axis enables, commands, sequence state, permissives, interlocks,
fault response, and restart conditions. The plant may integrate the commands
into gantry/tool motion and publish measured encoder/limit/vacuum/attachment
feedback; it may not own the Pick/Transfer/Place phase machine or decide when a
motor command changes. The current code is still known to fail this criterion:
the authored project exposes `gantry_cycle`, while
`SceneSimulationRuntime.AdvancePalletizerPlant` advances
`PalletizerPlantModel` from that command and the model owns the phase transitions.
Existing palletizer motion checks prove the old scene cycle works, not that the
PLC owns it. Rework acceptance must exercise the learner-authored control path
for Start, Stop, Reset, permissive loss, simulated safety trip, restart inhibit,
encoder/limit feedback, vacuum pickup/release, and four-carton completion.

The same evidence-first rule applies to visuals: first-rung origins and branch
joins must be continuous TIA ladder geometry, and optional tag information must
follow the selected TIA display mode. A source assertion, passing controller
test, or a RungProof screenshot alone does not establish a TIA match; retain the
installed-TIA same-state comparison and known unsupported behaviors as open
acceptance items until checked.

### Current continuation: inline TIA comparator symbols

Siemens' S7-1200/1500 V21 LAD reference and the user's heartbeat comparison
capture (`codex-clipboard-928c201c-3a9b-40ed-a0b6-7fc0a9492603.png`) show
comparison operands outside an inline relation symbol: operand 1 above,
operand 2 below, and the selected comparison data type at the symbol. The
comparator RLO participates in series/parallel logic. The previous RungProof
renderer instead drew a gray function box and printed a third generic consumed
value badge. TIA mode now draws `==`, `<>`, `>`, `>=`, `<`, or `<=` inline,
places the two monitored values with their operands, infers and displays a
declared scalar type when known, and omits the duplicate generic badge. The
RungProof-only `CODE MATCH` operation remains explicitly local and is not
rendered as a native TIA comparator.

The new production review fixture was scanned and captured at 100%:

- `.tools/tia-comparator-dint-true.png`: `pc_heartbeat <> last_heartbeat`
  displays DInt operands/values, a compact inline `<>`, and continuous green
  power flow through the true result.
- `.tools/tia-comparator-dint-false.png`: equal DInt values leave the same
  symbol/operand layout intact and show false flow as blue dashed through the
  following coil and rail.

Both captures were visually inspected. The fixture asserts that the comparison
result matches the requested true/false state. Siemens specifies that both
values must share a data type and places operand 1 above and operand 2 below
([CMP >, STEP 7 V21 LAD for S7-1200/1500](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/comparator-operations-s7-1200-s7-1500-s7-1200-g2/cmp-greater-than-s7-1200-s7-1500-s7-1200-g2)).

This is partial renderer evidence, not closure of the comparator family. The
current model has no persisted comparator data-type field or TIA-style type
dropdown, and the controller does not validate that both operand declarations
match. The displayed type is inferred from captured operand metadata or a
declared tag; `???` means it could not be inferred. Editing the type, missing
or mismatched type behavior, all six relation glyphs, offline/selected/edit
states, and side-by-side comparison with the user's installed TIA remain open.

### Latest source review: network origins, IEC timer body, and tag visibility

The user-provided network/TON screenshots were compared with current rendering
code. `LadderEditorCanvas` has a left-rail bridge and explicit series and branch
split/join geometry; static inspection did not establish a missing wire at the
branch origin. Keep first-rung continuity open until the full running view is
captured against TIA, including the user's reported Demo 5 Network 7 and first
network of other programs.

Static geometry review found that IEC timer bodies ended 8 px below the rung
center while their second `PT`/`ET` pin row was drawn 24 px below it. The TIA
timer body height is now 102 px with its existing top position retained, to
contain both rows. Fresh production-renderer captures
`.tools/tia-timer-unassigned-after-bound-fix.png` and
`.tools/tia-timer-assigned-monitor-after-bound-fix.png` were visually
inspected: the DB heading remains above, unassigned `PT` stays at its own pin,
and the `PT`/`ET` row plus monitored `Q`/`ET` values fit inside the body without
overlapping the rung. This is partial evidence for TON at 100%; TOF/TP/TONR,
selection/edit states, additional zooms, and a same-state comparison against
the installed TIA version remain open.

The renderer's TIA tag-information setting currently suppresses supplementary
address/comment annotations and suppresses synthetic IEC output labels such as
`timer.Q`/`timer.ET`. It retains actual symbolic operands and live values; those
are separate TIA display concepts, so hiding supplementary tag information
must not erase real operands or pin values. This static code observation is not
a pass for every instruction family or display mode. Require same-state TIA and
RungProof captures across symbolic/absolute/both operands and hidden/shown tag
information before closing the family-wide gate.

### Current goal extension: Demo 5 PLC ownership and all-demo controls

The user’s latest instruction makes the control architecture and teaching
quality explicit hard gates. The screenshots are concrete regression examples:
the Demo 5 batch rung seals in `gantry_cycle`; the close-up exposes its branch
origin; the timer crop shows extra member tags beside `Q`/`ET`; and the TIA TON
reference shows an unassigned `PT` at its pin. These images must be compared as
visual states, not treated as approval of the current source.

**Confirmed current failure:** Demo 5's authored ladder drives a single
`gantry_cycle` BOOL, while `PalletizerPlantModel.Step` owns the pick, dwell,
transfer, place, release, and return phases. The plant advances those phases
from that bit and `vacuum_pick`; its X/Y/Z counts are measurements of the
resulting pose, not feedback that determines the next PLC action. This hides
the machine sequence from learners. The existing tests prove that hidden cycle
works, not that the displayed PLC program owns motion. Replace that contract
before calling Demo 5 a PLC-controlled palletizer lesson.

The expanded acceptance contract is:

1. **Make the sequence PLC-owned.** Main must visibly execute the sequence from
   scanned state and field feedback. Remove `gantry_cycle` as a whole-cycle
   command. Use the relevant carton, pickup/place, home/limit, encoder,
   gripper/vacuum, pallet, and fault feedback in ladder transitions; each of
   the four placements must be caused by those scanned decisions. The plant
   may integrate axis motion in response to PLC-issued per-axis commands and
   publish measured position/sensor state. It must not choose the next phase,
   synthesize a later axis command, or infer placement from elapsed time.
2. **Teach reusable equipment control.** Main calls an inspectable FB for each
   independent motion axis/motor and a separate gripper/vacuum FB where
   applicable. Show each call, formal interface, instance identity/DB, command,
   permissive, feedback, ready/running state, and fault response in the block
   view and program tree. Siemens defines an FB call as an instance whose state
   is stored in an instance DB; TIA shows the assigned DB above the LAD call.
   See [instance data blocks, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg)
   and [single instances, STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/block-calls/instances/single-instances).
3. **Show machine operation and interlocks.** Each demo has its own learner-
   facing machine Start, Stop, and Reset controls bound to symbolic I/O and
   consumed by visible logic; playback controls do not count. Each motion demo
   shows System Enable, motor/axis permissions, interlocks, seal-in or explicit
   sequence state, trip/fault handling, and restart inhibition. A reset must
   clear only the intended latched condition after its reset conditions are
   met; it must not silently restart motion. Require a fresh Start after
   Stop/trip as specified by the lesson.
4. **Keep safety claims honest.** Add an operator E-stop/safety-trip exercise
   for motion-capable demos, but label it as an offline instructional input.
   Standard LAD permissives and this simulator are not a safety-rated function.
   The lesson must distinguish the standard PLC's status/permissive response
   from a real SIMATIC Safety program, F-CPU/F-OB, F-I/O, and safety-rated
   hardware. See [Siemens safety instructions](https://docs.tia.siemens.cloud/r/en-us/v20/step-7-safety-v20-instructions/overview-of-instructions)
   and [fail-safe block structure](https://docs.tia.siemens.cloud/r/en-us/v20/simatic-safety-configuring-and-programming/programming/overview-of-programming/fail-safe-blocks).
5. **Keep every rung and displayed operand reference-backed.** Every network
   starts at the left power rail with connected TIA topology, including the
   first rung and every branch. No detached inner rail, unexplained lead,
   conductor gap, or invented contact. Follow the chosen TIA operand/tag
   display settings: hide supplementary member/address/comment labels when
   hidden, and never clip or overlap values at instruction pins. Check the
   actual running editor at normal and 100% zoom and against the same state in
   the user's TIA version.
6. **Give the demo catalog a real learning progression.** Audit each authored
   demo against its stated task and equipment. Make its key control complexity
   visible in the program instead of hiding it in a plant script or a one-bit
   convenience output. Remove or clearly identify scenery that has no command
   or feedback role. Do not describe a demo as advanced/integrated unless its
   blocks, tags, interlocks, state, faults, and measured feedback demonstrate
   that level.

**Required proof before the architecture gate passes:** produce a per-demo
control/I/O inventory; inspect Main and each called block/interface/instance
DB; capture the live editor with the configured operand/tag-information modes;
and trace scene Start, Stop, Reset, trip, permissive loss, restart inhibit,
encoder/limit transitions, pickup/release, and each placement through input,
PLC scan, per-axis output, and measured feedback. Inspect the actual application
capture rather than relying on source review or test output alone. The active
goal remains open until these artifacts exist and all listed cases pass.

This extension is a working-goal record because the goal service does not edit
an active objective in place. It does not change the unrelated worktree state,
claim a physical TIA download, or claim a safety validation.

### Independent editor and project-tree audit findings (2026-10-14)

The acceptance inventory covers all 52 current program instruction keys plus
the `rung` and `branch` editor operations. Each item remains OPEN or PARTIAL
until its own TIA visual, edit/selection, operand, and online-monitor states
are evidenced; family-level or RungProof-only screenshots do not pass sibling
instructions. The evidence matrix is the per-key checklist.

The project-tree audit expands the TIA-controls gate beyond the already working
Add new block and Add new tag-table routes. Current gaps to review are: (1) an
OB is shown twice, once under a pseudo Organization blocks/tasks node and again
under Program blocks; (2) Add new DB lacks TIA's global/instance/ARRAY/PLC-data
type choices and declaration editor; (3) instance-DB tree entries navigate to
the call rather than opening a DB declaration view; and (4) Watch tables opens
one fixed table rather than managing named, persistent tables. The native
command harness exercises synthetic select/activate signals, so Enter,
double-click, focus, and exact dialog parity remain unproven. Do not represent
these workflows as complete; document unsupported object kinds explicitly.

The output-flow audit also found a specific hypothesis for the monitoring
gate: the current timer/counter continuation wire and right rail appear to use
the instruction's incoming network-enable state, even when the displayed
Boolean `Q`/Done value is false. Confirm the behavior in installed TIA before
changing it. Compare CTU with `CV < PV` and `CV >= PV`, and TON before/after
`PT` expiry; record Q, downstream conductor, and right-rail status in both
states. `CV` and `ET` are data outputs and must not be confused with Boolean
power flow. Siemens describes online status separately from instruction values
and defines TON/CTU Q as Boolean outputs; direct matched-state TIA evidence is
still required to settle the renderer mapping.

Official references: [TIA tag-information display and placement](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information), [FB instance data](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg), [single FB instances in LAD](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/block-calls/instances/single-instances), [S7-1500 program execution](https://docs.tia.siemens.cloud/r/simatic_s7_1500_et_200mp_manual_collection_enus_21/comprehensive-information/cpu-function-manuals/cycle-and-response-times/program-execution/principle-of-operation?contentId=wn2OFRnZBF2F8IolU804Qw), and [S7-1500 CALL parameters/instance DB](https://docs.tia.siemens.cloud/r/en-us/v20/stl-s7-1500/stl-mnemonic-s7-1500/program-control-operations-s7-1500/code-blocks-s7-1500/call-call-block-s7-1500).

### Goal clarification from the four latest screenshots (2026-10-14)

These references are now explicit rejection criteria for the active editor and
demo goal, rather than optional polish. They reinforce the existing gates above
and do not count as evidence that any gate has passed:

| Evidence | Required implementation and proof |
| --- | --- |
| Demo 5 Network 7 and branch-origin crops (`codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`, `codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png`) | Render a normal TIA network whose left power rail is the actual rung origin. Start/permissive and seal-in paths connect through TIA-style split/join points. Inspect first networks as well as later networks. Source and current RungProof capture agree on the root x-origin, so the reported defect is not confirmed until a matched TIA comparison finds a difference. |
| Timer output crop (`codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`) | `Q` and `ET` values stay on their own output-pin rows. Show no supplementary `instance.Q`/`instance.ET` text when the selected TIA tag-information setting hides it. Check all supported operand modes and tag-information modes; text may not collide with another value, the block, or the right rail. |
| TIA TON with unassigned `PT` (`codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png`) | Keep the instance DB heading above TON and show the unresolved preset at `PT` in the reference state. Do not fabricate a preset. Verify + Load must reject the missing required operand with a diagnostic that names the timer/network. |
| User's architecture correction in this request | Demo 5 must not call a whole-cycle `Gantry Cycle` and let scene code choreograph motion. Main owns machine enable, Start/Stop/Reset, permissions, interlocks, seal-in/state transitions, trip/fault response, and restart inhibition; it calls separately instantiated motor/axis FBs and the applicable vacuum/gripper FB. Ladder transitions consume encoder/limit/sensor feedback, and each command changes only after a PLC scan. Plant code integrates those commands and publishes measured feedback only. |

Apply the machine-control gate to **each** authored demo, not just Demo 5:
inventory scene Start/Stop/Reset (and a simulation-only safety-trip control
where motion makes it appropriate), prove each control is bound to an input
tag read by the lesson's displayed PLC program, and trace input → scan → output
→ plant response → feedback. The application's Run/Stop/Reset and playback
buttons do not count as machine controls. Do not add decorative buttons or
E-stop labels without corresponding program behavior. A simulated trip in
ordinary LAD teaches status/interlock response only; it is not a safety-rated
function and cannot replace SIMATIC Safety architecture.

The review must also inspect the whole rung and all visible operands in the
running application at matched zoom and mode. Source review, a successful build,
or a passing controller test alone cannot close visual or functional parity.
For unsupported Siemens behavior, document the boundary instead of substituting
a RungProof-specific shortcut or visual approximation.

### Current turn: rebuilt tag-information and branch-origin captures

The bundled .NET build was run from `rungproof-next` and completed with zero
warnings and zero errors. The older `.tools/current-tag-hidden*.png` captures
showed `EMPTY SERIES · TRUE`, but the exact nested fixture was rebuilt and
captured from the current production canvas as `.tools/tia-recursive-nested-fresh.png`.
At TIA style and 100%, it has no `EMPTY SERIES` annotation; the left power rail
connects to the parallel/nested branch geometry. This means the old image does
not reproduce in the current source/build. Do not call the wider branch gate
passed from this one fixture.

Fresh editor captures `.tools/tia-tag-info-hidden-fresh.png`,
`.tools/tia-tag-info-shown-fresh.png`, and
`.tools/tia-tag-info-below-network-fresh.png` were visually inspected at 100%.
With Hide, symbolic operands and pin names remain while address/comment
annotations disappear. Show adds address/comment text at each operand. Below
network places those entries in separate rows under the network. These are
production-canvas RungProof captures. Siemens V21 describes tag information as
tag names, addresses, and comments that can be shown or hidden and positioned
on operands or below the network ([Displaying or hiding tag information,
STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information)).
The user's TIA screenshot and Siemens' LAD overview define the rail/branch
semantics: each network has a power rail, starts at the left edge, and branches
form parallel circuits ([Overview of the LAD elements,
STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A),
[Basic information on branches,
STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/basic-information-on-branches-in-lad)).

Fresh 100% online captures `.tools/tia-ton-output-hidden-fresh.png` and
`.tools/tia-ctu-output-hidden-fresh.png` were also inspected. TON shows `%DB1`,
the IEC instance name, Q/FALSE, and ET/T#10ms at their pin rows, with no
synthetic `timer.Q`/`timer.ET` member tags. CTU shows `%DB1`, the IEC instance
name, Q/FALSE, and CV/1 at their own rows, with no `count.Q`/`count.CV` tags.
The CTU `R` and `PV` operands remain at their corresponding pins. This closes
only these current RungProof monitor captures at 100%; it does not close offline,
edit, selection, visible-tag, other-zoom, other-instruction, or matched TIA
states.

`--verify-ladder-editor` exited successfully with
`EDITOR_COMMAND_ROUTES_VERIFY PASS`, including the scripted tag display setting
routes. `--editor-zoom-review` passed its scripted 75%, 100%, and 150% checks
for both styles and produced the three tag captures above. These event-path
checks do not verify physical mouse/keyboard interaction. Godot still reports
the independent invalid starter-challenge manifest warning and RID leak
warnings during verification; neither is evidence for or against TIA rendering.
The installed-TIA same-state captures and full instruction-family matrix remain
open, so overall parity remains unproven.

### Current goal extension: Demo control architecture and latest visual evidence (2026-10-13)

This update incorporates the user's latest Demo 5 and editor screenshots into
the active acceptance goal. They add a control-architecture gate alongside the
existing per-instruction TIA comparison; they do not waive the requirement to
inspect the running editor or claim the target is already met.

**PLC ownership and equipment control**

- For every actuator-driven machine demo, the PLC project must visibly own
  machine enable, motor/axis permissives, interlocks, seal-in or sequence state,
  stop/reset behavior, faults, and command transitions. Each independently
  controlled motor/axis must have its own inspectable program block and
  instance DB, called from Main with its command, permissive, feedback, ready,
  running, and fault interface visible. The gantry also needs a separate
  gripper/vacuum block where applicable.
- Demo 5 may not use a `gantry_cycle` bit or another generic request to let
  `PalletizerPlantModel` choose pick, dwell, transfer, place, release, return,
  or carton-count transitions. Main and called PLC blocks must select each
  next action from scanned encoder, limit, carton, gripper/vacuum, pallet, and
  fault feedback. The plant may integrate the commanded motion and publish
  measured feedback only. Prove all four placements through that scan path.
- Every demo must expose learner-facing machine controls appropriate to its
  task, with their symbolic inputs consumed by authored logic. App-level Run,
  Stop, and Reset are separate lifecycle controls and do not satisfy this
  requirement. Reset must clear only the intended state after its conditions
  are met and must not restart motion; a fresh Start is required after a stop
  or trip where specified.
- Add simulated safety-trip/E-stop exercises to demos with actuator hazards.
  The UI and lesson must label these as instructional status inputs. Do not
  model an ordinary PLC bit as a safety-rated emergency-stop circuit or imply
  that this software-only simulator validates F-CPU/F-I/O, a safety relay, or
  machine safety. Non-motion lessons should not receive an unexplained fake
  E-stop; they need the appropriate reset/stop logic for their own objective.
- Audit every demo's equipment against its logic: each visible machine asset
  must have a command or feedback role in the lesson, or be removed/identified
  as scenery. State the actual learning progression and limits; do not hide
  complexity in scene scripts or describe basic single-bit control as an
  integrated motion-control lesson.

**Screenshot-derived TIA visual gates**

- Compare Demo 5's seal-in branch to the supplied TIA screenshot at the same
  zoom. The network starts at the TIA power rail; the start and holding paths
  are connected parallel paths, with explicit, correctly placed split/join
  geometry. Reject detached inner rails, unexplained vertical stubs, gaps,
  or fabricated empty logic. Inspect the entire rung and the first network,
  not only a crop around the branch.
- In tag-information Hidden mode, do not show synthetic member tags such as
  `pick_timer.Q`/`pick_timer.ET` beside timer pins. Keep true instruction pin
  names and live values. In Shown mode, show only the supplementary tag
  information allowed by the selected TIA setting, and never clip or overlap
  it with pins, wires, or rails.
- Reproduce the supplied TON with unassigned `PT`: the `PT` pin remains on its
  own input row with TIA's unassigned-operand indication at the operand
  location; `Q` and `ET` outputs/value fields align with their pin rows. Compare
  assigned and unassigned states, monitor on/off, and tag information hidden
  and shown against the same installed TIA version.
- For every supported instruction family, keep a matrix of its Siemens-defined
  pins, operand rows, block/DB presentation, editing options, monitoring
  behavior, and known unsupported cases. Record source inspection, automated
  checks, RungProof visual capture, and matched TIA inspection as separate
  evidence; a pass in one category cannot substitute for another.

These criteria expand the active goal but do not alter its repository/branch
scope or authorize Git operations. The software remains an offline symbolic
simulator, not a connected PLC or safety system.

### Current goal turn: assignment-coil catalog correction (2026-10-13)

Source inspection confirmed that the TIA toolbar already used Siemens' plain
assignment-coil symbol, but the authoritative instruction-help catalog said
`Assignment coil —(=)—`. The catalog now says `Assignment coil —( )—`, and the
virtual-controller test suite has an exact-string regression assertion.

Verification from `rungproof-next`:

- `dotnet build RungProof.Next.csproj --no-restore`: succeeded with 0 warnings
  and 0 errors.
- `dotnet run --project tests/RungProof.Next.VirtualController.Tests.csproj`:
  338 passed, 0 failed; physical PLC transport construction and connection
  were both false.
- Godot `--verify-ladder-editor`: exit 0 and
  `EDITOR_COMMAND_ROUTES_VERIFY PASS`. The known invalid challenge manifest
  warning and RID leak warnings still appear; they are outside this catalog
  label correction.
- A headless monitoring-image capture executed its flow fixture but stalled
  while awaiting the rendered frame and did not save a PNG; that invocation
  was interrupted. Therefore this turn has no new visual-capture evidence.
  The earlier coil canvas capture is RungProof-only evidence and does not
  establish same-state TIA parity. The coil visual/state gates remain open.

This correction closes one documented text mismatch, not the assignment-coil
key. At that checkpoint, P/N contacts still used hidden per-instruction state.
The later update below records the explicit edge-memory implementation and its
still-open visual/TIA comparison gates.

### Current goal extension: PLC-owned machine lessons and network/instruction defects (2026-10-13)

This is the controlling acceptance record for the user's latest four
screenshots and follow-up. The active goal service does not provide an
objective-edit operation; this project document is therefore the durable
expanded acceptance contract. The goal remains active and does not pass until
these checks have implementation and evidence.

**Confirmed Demo 5 architecture failure**

The current authored Main still seals `gantry_cycle`; source shows
`PalletizerPlantModel.Step` consumes that bit and owns the phases that lower,
pick, transfer, place, release, and return the gantry. Its encoder values are
derived from plant pose and do not close the PLC decision loop. Therefore the
robot/gantry behavior is scene-script sequencing, not PLC-controlled motor
logic. Existing four-box cycle results do not qualify the architecture.

The replacement has to show each command decision in scanned ladder logic:

1. Add machine System Enable, Start, Stop, Reset, a simulated safety-status
   trip, reset permissives, and restart inhibition as named symbolic inputs and
   visible networks. App Run/Stop/Reset do not count as machine controls.
2. Main owns a visible state/transition sequence. Every motion transition uses
   scanned limit/encoder, carton, pallet, vacuum/gripper, and fault feedback;
   a held Start or restored permissive cannot restart without the required
   fresh Start. Reset clears only the intended fault/state after its reset
   conditions are satisfied.
3. Main calls separately inspectable X-, Y-, and Z-axis/motor FBs, each with a
   distinct instance DB and explicit enable, permission, command, feedback,
   ready/running, and fault interface. It also calls a separate vacuum/gripper
   FB. No generic `gantry_cycle` request or plant-owned phase enum may choose
   the next movement. Plant code may integrate those commands and publish
   measured values only.
4. The scene controls, tags, program tree, called block interfaces/DBs, and
   each rung must all agree. Every moving asset must participate in a command
   or feedback path, or be removed/clearly identified as scenery. The safety
   status is an offline training input and must not be described as a validated
   E-stop or safety function.
5. Prove all four carton placements by tracing operator input -> accepted PLC
   scan -> per-axis/gripper command -> plant motion -> measured feedback ->
   next PLC decision. Include Stop, trip, permissive loss, reset, restart
   inhibition, and restart after fault clearance.

The control progression gate still applies to all five demos: machine-level
controls and logic-connected reset in each lesson; motion-specific enable,
permissions, interlocks, and simulation-only trip only where meaningful;
equipment control complexity must be visible in the authored program rather
than hidden in scene scripts. The earlier static inventory remains the
source-confirmed baseline and is not a pass.

**Latest visual defects and acceptance conditions**

- Compare the complete Demo 5 seal-in network to the supplied TIA view. The
  first rung must originate on the network's actual left power rail; Start and
  seal-in paths must split/rejoin as connected branches. Reject an inner
  floating rail, detached branch origin, unexplained stub, conductor gap, or
  extra fabricated contact. Also inspect each network's first rung, not only
  Network 7.
- With tag information Hidden, retain actual instruction pins and the DB
  identity where TIA shows it, but suppress extra symbolic member/address/
  comment annotations that the chosen TIA mode hides. In particular,
  `pick_timer.Q` and `pick_timer.ET` must not appear as synthetic adjacent
  tags. Apply this independently to every instruction family and display mode.
- In the unassigned TON, `PT` and its `???` operand belong at the PT pin row;
  Q and ET values align to their respective output pin rows; the instruction
  body contains the lower PT/ET row. Compare the assigned/unassigned, online/
  offline, selected/unselected, and tag-information Hidden/Shown states.
- Live power flow colors the entire evaluated continuous rung and branch
  path, including the rail/conductors and instruction connection where TIA
  shows it. Mixed or unknown flow must stay distinguishable from energized
  flow. Validate false/true/unknown states on the whole network.
- Do not label the network-origin or timer correction passed from source alone.
  Inspect fresh running RungProof captures and same-state captures in the
  installed TIA version at the same zoom. Record source, functional, RungProof
  visual, and TIA comparison evidence separately.

**Current implementation evidence and limits**

- P/N contacts now have a persistent, unique BOOL memory operand through the
  runtime/editor/project JSON, property editor, compiler, project index,
  monitoring observation, and copy/paste paths. Runtime uses the configured
  memory value at first evaluation and updates it on each scan. The typed motor
  lesson explicitly initializes its Start edge-memory baseline high so a
  Start held at initial scan does not create an unwanted command; this is a
  program-specific initialization decision, not a runtime override of TIA
  memory semantics. The compiler now rejects duplicate symbolic memory,
  duplicate normalized absolute M/DBX addresses, and explicit absolute
  addresses outside those Siemens storage areas. Blank/symbolic addresses
  cannot be resolved to hardware storage in this offline project and remain an
  explicit limitation. Automated controller tests pass 339/339.
- `.tools/tia-edge-memory-hidden-100.png` and
  `.tools/tia-edge-memory-show-100.png` were captured with the normal renderer
  and visually inspected. Both P and N show the signal above the contact and
  its own memory tag below, with continuous conductor/rail geometry. Hidden
  suppresses address/comment annotations; Show places each address/comment
  with its own operand and no visible clipping at 100%. These are RungProof
  offline captures only. Matched installed-TIA, selected/edit, online,
  parameterized FB local-memory, and other zoom states remain open.
- The TIA timer-box bounds were extended so the non-retentive PT/ET row is
  inside the instruction body. `.tools/tia-timer-missing-pt-goal-update.png`
  was captured through the normal renderer and visually inspected at 100%:
  DB/IEC identity is above the block; IN/Q and PT/ET occupy their own rows;
  the missing operand marker is at PT; lower rows fit inside the box; and the
  hidden-tag state has no synthetic timer member labels. This closes only the
  RungProof offline 100% missing-PT geometry case, not online monitoring or
  same-state TIA parity.
- `--verify-ladder-editor` passes the scripted editor routes.
  `--editor-zoom-review` passes with the normal renderer at 75%, 100%, and
  150%. In headless mode it fails the `tia75SelectorVisible` check, and a
  separate headless image attempt returned a null viewport texture under
  Godot's dummy renderer; use the normal renderer for screenshot review. Those
  headless limitations do not invalidate the successful normal-renderer
  review.
- Demo 5 remains FAIL until the PLC-owned architecture above is implemented
  and traced end to end. Overall TIA visual parity remains OPEN; no physical
  PLC or safety validation is claimed.

The four user screenshots are retained as review evidence for the branch
origin/seal-in structure, TON Q/ET row labels, and missing-PT operand. The
screenshots describe desired TIA behavior; their presence does not certify the
current RungProof rendering.

### 2026-10-13 goal clarification: PLC-authored machine complexity

The latest four screenshots (`codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`,
`codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png`,
`codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`, and
`codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png`) tighten the
existing machine-control and visual parity gates; they do not create a
separate visual redesign objective. Use the following order when deciding
whether a demo is ready:

1. **PLC owns decisions.** A cyclic Main/OB calls inspectable program blocks;
   each independently controlled axis or motor has a called FB, its own
   instance DB, and visible command/permission/feedback/fault interface.
   Sequence advancement is caused by scanned sensor/encoder/gripper feedback.
   A plant script may integrate commands and report measured state, but may not
   select phases, fabricate completion, or turn `gantry_cycle` into hidden
   motion. Siemens documents cyclic OB execution, block-call processing, and
   instance data storage in its [cyclic OB reference](https://docs.tia.siemens.cloud/r/en-us/v21/functional-description-of-s7-1500-cpus-s7-1500/organization-blocks-s7-1500/description-of-all-types-of-organization-blocks-s7-1500/cyclic-obs-s7-1500),
   [block-call reference](https://docs.tia.siemens.cloud/r/en-us/v20/stl-s7-1500/stl-mnemonic-s7-1500/program-control-operations-s7-1500/code-blocks-s7-1500/call-call-block-s7-1500),
   and [instance DB reference](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg).
2. **Operator controls are machine logic.** Provide visible machine Start,
   Stop, and Reset controls with symbolic input tags and authored logic routes;
   separate these from the app's playback controls. Motion lessons additionally
   need System Enable, permissions/interlocks, a simulated safety-trip status,
   seal-in/restart inhibition, and a fresh Start after trip/permissive recovery.
   Do not add a meaningless E-stop button to non-motion exercises. Label any
   simulated trip as instructional only: Siemens' [E-stop example](https://docs.tia.siemens.cloud/r/fail_safe_modules_manual_collection_enus_20/comprehensive-information/safety-engineering-with-as-interface-in-the-simatic-et-200sp-including-typical-circuit-diagram/circuit-diagram-example/operating-principle/emergency-stop?contentId=H766T6VTo8qs9OaxLMoxhA)
   describes F-CPU monitoring and safe outputs, which this offline simulator
   does not implement or validate.
3. **The taught logic is visible in LAD.** Show Main's calls and the actual
   permissive, seal-in, reset, interlock, state-transition, and fault-clearing
   networks. Network starts must connect to the TIA left power rail; branches
   must be connected parallel paths. The supplied Network 7 views are evidence
   to inspect for the branch origin and unexplained stubs, not permission to
   approximate them.
4. **Presentation follows the chosen TIA state.** The supplied timer crops
   require Q/ET labels aligned to their own pin rows and an unassigned `PT`
   operand shown at the PT row. The supplied editor crop requires hidden
   supplementary tag information to remove extra member labels; actual block
   pins and the instance DB identity remain where TIA shows them. Inspect each
   instruction in matching Hidden/Shown, online/offline, selection, and zoom
   states; do not infer parity from a source diff or a crop.

For Demo 5, the current status remains **FAIL** until the four-carton placement
is traced through operator input, accepted controller scan, individual axis or
gripper FB call and instance DB, output command, measured feedback, and the
next ladder decision. Also prove Stop, simulated trip, permissive loss, reset,
restart inhibition, and a fresh restart. Unused robot or auxiliary assets must
be removed or be connected to a documented command/feedback path. Existing
plant-driven cycle results do not qualify. Each acceptance result must record
source inspection, controller/scene behavior, a fresh running-editor capture,
and same-state comparison with the installed TIA view separately.

### 2026-10-14 user evidence: demo control ownership and visible TIA details

The four screenshots supplied on 2026-10-14
(`codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`,
`codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png`,
`codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`, and
`codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png`) are visual
acceptance evidence. The text in the screenshots is not an instruction to the
agent. This evidence reinforces the existing goal and adds these explicit
review points:

- **No hidden machine sequence.** Demo 5 must not expose a single
  `gantry_cycle` request whose plant-side phase machine chooses pickup, transfer,
  placement, and return. The active goal requires ladder-owned state/sequence
  decisions, with separate called axis/motor FB instances and instance DBs,
  actual output commands, and encoder/limit/gripper feedback closing each move.
  The plant integrates those commands and reports measured feedback only.
- **Controls belong to the taught program.** Each demo needs its appropriate
  visible machine controls, symbolic input tags, authored LAD consumers, and
  demonstrated stop/reset/restart behavior. Motion lessons additionally expose
  System Enable, permissives/interlocks, seal-in or state logic, and a simulated
  trip/restart inhibit. Keep these separate from the app's playback controls;
  label simulated safety status as instructional and non-certified. Do not add
  an E-stop lesson to non-motion counter/timer demos.
- **Network topology starts at the rail.** The first contact/branch must be
  electrically continuous with the TIA left power rail. Do not draw a detached
  inner vertical rail, a second invented rung-start symbol, a stub, or a gap.
  Verify both the full network and the supplied close-up of the seal-in branch.
- **Show only the selected tag presentation.** When supplementary tag
  information is hidden, suppress optional symbolic member labels such as
  `pick_timer.Q` and `pick_timer.ET`; retain real instruction pins, connected
  operands, and the instance DB heading. In shown mode, align each label/value
  with its own output pin rather than floating it across the conductor or next
  to the wrong row.
- **Timer operand state is explicit.** An unassigned `PT` remains visibly
  unassigned at the PT pin (the supplied TIA reference uses `???`); do not
  fabricate a preset. Keep the `Q` and `ET` operands/values aligned to their
  respective output rows and use TIA's block geometry and selection treatment.

These requirements do not pass from source inspection, a crop, or a green
build. For each affected state, retain (1) a matched capture from the user's
installed TIA version and the running RungProof editor, (2) a trace from
operator control through accepted ladder scan to command and measured scene
feedback, and (3) a separate written limitation for unsupported behavior.

### Current-source confirmation for the Demo 5 failure

The source confirms the architectural failure rather than merely suggesting it:
`PalletizerPlantModel.Phase` contains `PickDown`, `VacuumDwell`, `PickUp`,
`Transfer`, `PlaceDown`, `Release`, `PlaceUp`, and `Return`, and
`PalletizerPlantModel.Step()` advances those phases based on elapsed time,
`gantry_cycle`, and `vacuum_pick`. `SceneSimulationRuntime.AdvancePalletizerPlant()`
passes those two PLC outputs directly into the plant. The authored
`CreateIntegratedPalletizer()` ladder has no separate X/Y/Z motion commands or
per-axis FB/instance DB calls. Encoder counts are projected from the plant pose.
So the earlier successful four-box animation proves only this plant-driven
model, not PLC-controlled gantry motion; Demo 5 remains **FAIL** until the
architecture and feedback gates above pass.

### 2026-10-14 CTU and IEC pin-row correction

Direct comparison with the supplied TIA CTU crop showed the current block body
was too wide and too short. The TIA canvas now uses a 126 x 128 logical-unit
CTU body and 26-unit counter pin spacing; the regular rung still passes through
the `CU`/`Q` row. The `CV` value is placed on its own lower output row. A
100%-zoom review-fixture assertion reports `TIA_CTU_GEOMETRY_PASS width=126
height=128`, and `.tools/tia-ctu-body-dimensions-verified.png` was visually
inspected against the supplied crop. This is a renderer measurement, not proof
of Windows DPI equivalence or a complete matched-state TIA review.

The fresh timer captures exposed the same lower-row alignment issue for `PT`
and `ET`. Assigned values are now drawn on their own pin rows; the unassigned
`PT` remains `???` at the `PT` pin. `.tools/tia-timer-operand-rows-aligned.png`,
`.tools/tia-timer-unassigned-operand-row.png`, and
`.tools/tia-timer-q-et-aligned.png` were inspected at 100%. The root-parallel
branch capture `.tools/tia-branch-start-network-rail-revised.png` confirms the
branch begins at the left rail. All are production-canvas review fixtures;
same-state comparison against the user's installed TIA build remains open.

Verification after these canvas changes: Godot/.NET build succeeded with zero
warnings/errors; the virtual-controller/editor suite reported 340 passed and 0
failed; `--verify-ladder-editor` reported `EDITOR_COMMAND_ROUTES_VERIFY PASS`.
That shell route also logged `CHALLENGE_CONTENT_DISABLED` due to a starter hash
mismatch and renderer resource-leak warnings at shutdown; neither is treated as
parity evidence. Other instruction families, selection/edit states, and full
TIA visual comparison remain open.

### 2026-10-14 ADD data-type selector implementation slice

The ADD instruction now persists an optional `LadderNumericDataType`, displays
a type selector on the selected TIA-style ADD block, exposes `???`, `Int`,
`DInt`, `Real`, and `Auto (<inferred type>)`, and applies the selected numeric
range/precision rules during execution. Invalid integer-range results preserve
the prior destination and fault ENO through the existing runtime diagnostic
path. Missing type metadata in older projects remains unspecified on load.

Evidence: `numeric-add-tia` runs in Godot's regular Vulkan renderer,
asserts selector visibility/order/selected type and the actual executed INT
result, then saves `.tools/numeric-add-tia.png`; a separate
`.tools/numeric-add-tia-unselected.png` capture confirms the selector hides and
the selected type remains as the block label when deselected. Both rendered
images were inspected. The 0-warning build and all 340 virtual-controller/editor checks
pass, including INT overflow, REAL precision, and JSON round trips. This is
limited proof for ADD and the current INT/DINT/REAL value domain. It does not
prove full TIA parity: SInt/UInt/UDInt/LInt/ULInt/LReal, ADD's extra input pins,
all other math instruction blocks, a complete editor-shell rendering, and a
same-state capture from the user's installed TIA version remain open. Do not
mark the numeric instruction family accepted from this slice.

Official references consulted:

- [Defining the data type of an instruction, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/selecting-the-data-type-of-a-lad-element/defining-the-data-type-of-an-instruction)
- [Selecting a data type, STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/creating-lad-programs/inserting-lad-elements/selecting-the-data-type-of-a-lad-element/selecting-a-data-type?contentId=_pVYXZ2qBg7kZtE3DlXHMQ)
- [ADD instruction, S7-1200/S7-1500 STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/add-add-s7-1200-s7-1500)

### 2026-10-14 arithmetic selector parity extension: ADD, SUB, MUL, DIV

The audit found that the type dropdown and typed runtime behavior previously
existed only for ADD, even though Siemens documents a `???` type selector for
SUB, MUL, and DIV as well. The editor now exposes the existing selector for
those four binary arithmetic boxes. Its supported choices remain deliberately
limited to `???`, `Int`, `DInt`, `Real`, and destination-based `Auto`; unsupported
Siemens integer/real types were not added as cosmetic choices. SUB/MUL/DIV now
pass selected type through the model, compiler type validation, and runtime
range/precision checks. Typed integer DIV truncates its quotient in this
simulator subset. Auto resolves from the destination declaration.

The 2026-10-14 build completed with zero warnings/errors. All 343
virtual-controller/editor checks passed, including typed SUB underflow fault
and write preservation, typed MUL and DIV results, DINT Auto, type mismatch
rejection, and the existing ADD overflow/REAL/persistence cases. The normal
Vulkan editor captures `.tools/goal-numeric-sub-selected.png`,
`.tools/goal-numeric-sub-unselected.png`, and
`.tools/goal-numeric-sub-online.png` were visually inspected. Selected SUB
shows the type control; deselected SUB retains the type label; the online
fixture shows the energized path and typed result with Hidden supplementary
tag information.

This is a bounded RungProof implementation result, not TIA parity sign-off.
Selected/offline and unselected/online captures for MUL and DIV were also
visually inspected at 100% and are recorded in the evidence matrix. Every
arithmetic operation still needs same-state comparison with the user's
installed TIA build. In particular, RungProof keeps its protective
divide-by-zero diagnostic and does not yet reproduce Siemens' documented DIV
zero result/ENO behavior. The complete Siemens type sets, extra arithmetic
inputs, all zoom/selection/edit states, and matched online monitoring remain
OPEN in the evidence matrix.

Siemens sources confirming the operation-specific selector and operand/result
types are [SUB, LAD S7-1200/S7-1500 STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/sub-subtract-s7-1200-s7-1500),
[MUL, LAD S7-1200/S7-1500 STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/mul-multiply-s7-1200-s7-1500),
and [DIV, LAD S7-1200/S7-1500 STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/div-divide-s7-1200-s7-1500?contentId=Qzd5sDgma2urK8UCtpCMAw).
The product pages document a broader type set than this offline controller
currently supports; the UI and validation continue to disclose that boundary.

### 2026-10-14 direct-to-rail branch geometry correction

The supplied TIA seal-in example and the regular-renderer capture exposed a
systemic network-start mismatch: a tall parallel branch caused RungProof to
extend the left power rail from the top branch row to the bottom branch row.
That made the outer rail look like a second branch junction. TIA's screenshot
shows the short rail at the rung origin, a horizontal connection into the
internal branch split, and a separate internal vertical through the branch
rows. Siemens STEP 7 V21 describes networks as having a left power rail where
rungs originate, and explicitly allows a parallel branch directly on that
rail.

The fresh inspection disproved the earlier claim that the branch-start image
was correct: `.tools/tia-branch-start-current.png` showed the branch split
offset from the left rail and connected by a mid-rung stub. `TreeLeft` now
places a TIA root-parallel branch at the rail x-coordinate, and `DrawRungLogic`
does not redraw a second short rail over it. The regular-renderer capture
`.tools/tia-branch-start-fixed-root-rail.png` was visually inspected: one
vertical rail/split spans both branch rows, both branch paths join continuously,
the upper path and output coil are energized green, and the false seal-in leg is
blue-dashed. This fixes the observed RungProof geometry for the direct-to-rail
parallel case. Ordinary rungs, other branch/edit states, and a same-state
installed-TIA comparison remain open.

References: Siemens [LAD elements and power rail, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A), [branch rules, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/rules-for-branches-in-lad?contentId=jsBAKV~wzHUUwt657xLTzw), and [basic information on rungs, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/rungs-in-lad/basic-information-on-rungs).

### 2026-10-14 IEC timer/counter output-label correction

A fresh 100% regular-renderer TON capture in tag-information Show mode
reproduced a defect: `timer.Q` was hidden under the next output's value chip,
while `timer.ET` appeared below the block. The supplied TIA screenshots show
the instruction member pins (`Q`, `ET`, `CV`) with live values and no external
`instance.member` operand labels. Siemens documents tag information as a
separate Show/Hide/position setting for tag names, addresses, and comments; it
is not an instruction to fabricate symbolic operands for IEC instance outputs
([Displaying or hiding tag information, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information)).

`LadderEditorCanvas.Monitoring` now omits the synthetic output member names for
TIA-style IEC timers/counters in every tag-information mode and uses the normal
pin-row spacing. This preserves the actual `Q`/`ET`/`CV` formal pin labels,
monitored value chips, and the instance DB heading. Non-TIA presentation paths
retain their own output labels.

Evidence from the normal Vulkan renderer at 100%, visually inspected:

- TON: `.tools/tia-ton-shown-fixed.png`, `tia-ton-hidden-fixed.png`, and
  `tia-ton-below-fixed.png`.
- CTU: `.tools/tia-ctu-shown-fixed.png`,
  `tia-ctu-hidden-selected-fixed.png`, and `tia-ctu-below-fixed.png`.
- Before/after reproduction: `.tools/tia-ton-shown-repro.png` versus the
  `shown-fixed` capture. The former visibly overlaps/hides Q and prints ET;
  the latter has separate Q/ET values and no synthetic member labels.

The .NET build completed with zero warnings/errors and the virtual-controller
and editor suite passed 340/340; physical PLC transport remained unconstructed
and unattempted. This closes only the RungProof-renderer instance-label defect
for these TON/CTU captures. It does not establish same-state TIA comparison,
all supported timer/counter types, operand-address modes, all zooms, or selected,
editable, and all online states; those remain open in the evidence matrix.

### 2026-10-14 assignment-coil rectangle state audit

The report that an assignment coil has a box was checked in both offline and
online renderer states instead of removing the selection affordance blindly.
The review fixture now accepts `--review-monitor=offline` so the same network
can be captured without a monitoring snapshot.

- `.tools/tia-coil-offline-none.png`: the unselected offline assignment coil
  has no rectangular instruction body; its symbolic operand is above the coil,
  and both conductors meet the coil.
- `.tools/tia-coil-offline-output.png`: selecting the output draws the existing
  dashed blue selection boundary around the terminal bounds; the coil and
  operand remain visible. This is RungProof-only selection evidence.
- `.tools/tia-coil-none-current.png`: the energized online coil has no
  unselected rectangle; the top path/output are green and the false seal-in
  path is blue-dashed.
- `.tools/tia-coil-output-current.png`: selected online state retains the same
  dashed selection boundary.
- `.tools/tia-set-coil-online.png` and `.tools/tia-reset-coil-online.png`:
  online set/reset coils render as unboxed coil symbols with `S`/`R` markers
  inside; destination values remain above the symbols and the rung stays
  connected.

The current capture therefore does not reproduce a box around an **unselected**
coil. Whether the selected boundary matches TIA still requires a same-state
capture from the user's installed TIA; do not change its shape based on the
unselected screenshot alone. Siemens describes assignment as the standard coil
`—( )—`, passing RLO through, and distinguishes coils from boxed LAD
instructions. The Siemens S/R instruction pages use the corresponding `S` and
`R` coil forms ([Assignment, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/assignment-s7-1200-s7-1500-s7-1200-g2?contentId=FgENtd5pfZ6iFzY0DhAB6g), [overview of LAD elements, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A), [S set coil](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/s-set-output-s7-1200-s7-1500-s7-1200-g2?contentId=Wh9qyAlMOIlzh6OBxbOEYA), [R reset coil](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/r-reset-output-s7-1200-s7-1500-s7-1200-g2?contentId=C2FGQIC3pRy1AxpaDUjqVg)).

The build completed with zero warnings/errors, all 340 virtual-controller and
editor checks passed, and the focused offline/online renders were visually
inspected. The coil key remains partial until offline, selected, edit, and
online states are compared against the matching TIA object/state.

### 2026-10-14 branch-start and IEC timer acceptance clarification

The latest four user screenshots (`codex-clipboard-a13b8132-71dc-4cd0-856f-1f3277ed0e08.png`,
`codex-clipboard-69c2090d-305f-49d0-9007-be77e787eaca.png`,
`codex-clipboard-060fb1f8-cbc4-4d57-b0c6-29baa0a965b4.png`, and
`codex-clipboard-f743c66b-0585-4471-a34d-1647ca3c80e3.png`) are explicit
acceptance evidence for the existing TIA editor goal. They refine network,
tag-display, and timer gates; they are not approval to add a RungProof visual
convention.

- **A network that starts with parallel logic has no lead-in stub.** Its split
  meets the network's left power rail, each leg starts at that split, and the
  branch rejoins before the output. No disconnected inner rail, gap, separate
  start symbol, or unexplained wire segment is acceptable. The current
  `.tools/tia-branch-start-network-rail-revised.png` is RungProof-only evidence;
  comparison against the user's exact TIA version and matching branch state
  remains open.
- **Timer/counter conductors must meet the instruction body.** Source review
  found a real 3 px gap on both sides: the IEC boxes are 126 px wide (63 px
  half-width), while `TerminalGap` placed the wires at 66 px from center. TIA
  timer/counter gap geometry now uses the 63 px half-width. The review fixture
  asserts zero wire clearance on both edges. A current build succeeded with
  zero warnings/errors; running-editor captures for TON, TOF, TP, TONR, CTU,
  and CTD all reported `TIA_TERMINAL_WIRE_CONTINUITY_PASS`. The CTU/TON Q-false
  images `.tools/tia-ctu-wire-continuity-after.png` and
  `.tools/tia-ton-wire-continuity-after.png` were visually inspected at 100%
  and show the incoming/outgoing conductors touching the box edges without a
  gap or overlap. This fixes a RungProof drawing defect; matched TIA visual
  comparison remains open. `--verify-ladder-editor` exited 0 and reported
  `LADDER_EDITOR_INTERACTION_VERIFY PASS` and `EDITOR_COMMAND_ROUTES_VERIFY
  PASS`. It also emitted the existing `CH_INVALID_CHALLENGE` starter
  byte-length/SHA warning and Godot resource-leak shutdown warnings; those are
  outside this geometry change and were not treated as parity evidence.
- **Timer Q/ET output rows stay readable.** Q's status value belongs to the Q
  row and ET's value belongs to the ET row. Both must stay clear of the rung
  conductor/right rail, each other, and canvas bounds at supported zooms.
  `instance.Q` and `instance.ET` are not extra physical operands: show them
  only if the selected TIA tag-information setting calls for that supplementary
  information. Hidden mode removes them; pin names and the IEC instance DB
  heading remain. Existing `.tools/tia-timer-q-et-aligned.png` demonstrates
  the RungProof hidden-tag rendering, not parity for every TIA view mode.
- **An unassigned TON `PT` stays unassigned.** Render `???` at the PT input row
  and do not substitute an initial/default or inferred time. The supplied TIA
  missing-PT crop is the reference. `.tools/tia-timer-unassigned-operand-row.png`
  is only a RungProof capture; selected/edit and same-state TIA checks remain
  open.
- **Evidence must cover complete context.** Inspect the full first network and
  a close-up of its branch origin, plus the whole timer and a close-up of the
  Q/ET rows. Capture hidden and shown tag-information modes, normal and 100%
  zoom, and online/offline states. A crop that hides a rail endpoint or a
  setting that hides the symptom cannot pass the acceptance gate.

The matrix at `docs/TIA_INSTRUCTION_EVIDENCE_MATRIX.md` is the per-state
checklist for these criteria. A successful build or RungProof-only capture is
not an installed-TIA visual match. The active Codex goal service does not
support editing an unfinished goal's objective in place; this audit is the
expanded project acceptance source for the active goal, which remains open.

### 2026-10-14 active-goal scope contract: TIA editor fidelity and PLC-authored demos

The active goal is broader than making the canvas look plausible. Its acceptance
has two coupled, independently tracked gates:

1. **TIA editor parity:** supported instructions, rails/branches, operands,
   instance DBs, monitoring colors, tag-information modes, project-tree actions,
   edit/selection states, and controls must match the corresponding TIA
   behavior. Every screenshot symptom is a regression case. Hidden tag
   information must not leak synthetic member tags; a partial rung colored
   green while the evaluated path is true is not enough; timer/counter values
   must stay on their actual pin rows; and every network/branch must connect
   from the left power rail as TIA does.
2. **PLC-owned teaching architecture:** demo complexity and equipment motion
   must be visible in the loaded program and controlled by accepted PLC scans.
   A clean editor render or a scene that completes its motion is not sufficient
   if background code owns the machine sequence.

The four screenshots from this request add these specific checks:

- Network 7's seal-in arrangement must use connected TIA branch topology from
  the left power rail. Preserve the contact's electrical meaning when placing
  the parallel start/hold paths and interlocks; no detached inner rail, stub,
  gap, or invented network-start decoration.
- The timer's Q and ET tags/values must align with their own pins. Supplementary
  `instance.Q` / `instance.ET` labels are controlled by the TIA tag-information
  setting and must be absent when that setting is Hidden. An unassigned `PT`
  remains visibly unresolved at the PT pin; do not infer a preset.
- Demo 5 may not present `gantry_cycle` as if it were a real PLC motor-control
  architecture. Main must call separate X/Y/Z axis or motor FB instances and a
  gripper/vacuum FB, with a distinct instance DB for each call, explicit
  interfaces, and rung-level enable/permission/interlock/Start/Stop/Reset and
  trip/restart-inhibit logic. Encoder, limit, and gripper feedback must drive
  the next PLC decision. Each scanned output commands only its corresponding
  modeled actuator.
- `PalletizerPlantModel` may integrate commanded actuator motion and publish
  measured position/switch/process feedback. It may not select pick, dwell,
  transfer, place, release, return, or the next carton; a plant-side phase
  enum, generic cycle request, or script-issued later command fails this gate.
- Audit every visible robot, vacuum/gripper, conveyor, and pallet asset against
  a PLC output/feedback or clearly identified scenery role. A visible asset
  with no taught or observed purpose is an open demo defect.
- Give every demo controls appropriate to its lesson and wire them to the
  displayed program. Motion demos require machine-level Start/Stop/Reset,
  System Enable, permissions/interlocks, and a clearly labeled instructional
  safety-trip status with reset/restart inhibition. Non-motion lessons need
  their own reset/recovery behavior and must not gain a decorative E-stop.
  The app's playback controls do not satisfy these machine controls.

Do not represent a standard LAD safety-trip bit as a safety-rated E-stop. The
offline teaching input is only an interlock exercise. A real safety function
requires an appropriately designed and validated fail-safe architecture; see
the Siemens [ESTOP1 emergency STOP instruction documentation, STEP 7 Safety
V21](https://docs.tia.siemens.cloud/r/en-us/v21/step-7-safety-v21-instructions/safety-functions/estop1-emergency-stop/emergency-off-up-to-stop-category-1-step-7-safety-v21).
The PLC block structure above follows Siemens' documentation for [function
blocks and instance DBs, STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/blocks-in-the-user-program/function-blocks-fb),
[single instances in LAD](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/block-calls/instances/single-instances),
[OB/FB/FC/DB program structure, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-and-managing-blocks/creating-blocks/block-folder),
and [DB declarations and instance assignments, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/basic-principles-for-programming-of-data-blocks).

The proposed simulator axis FBs are teaching abstractions for explicit
enable/command/feedback ownership; they are not Siemens Technology Objects or
Motion Control instructions. Do not claim `MC_Power`, `MC_MoveAbsolute`, or
drive/encoder commissioning parity unless those instructions and their
technology-object behavior are separately implemented and verified. Siemens
defines the actual [S7-1200 Motion Control instruction set, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/using-s7-1200-motion-control-s7-1200/programming-s7-1200/overview-of-the-motion-control-statements-s7-1200)
as CPU/technology-object instructions that start axis commands and expose
command status/errors. This is a documented implementation limit, not a reason
to hide the PLC-owned sequence in the plant model.

| Gate | Current disposition |
| --- | --- |
| Demo 5 PLC-owned axis/gripper sequence | **FAIL confirmed**: Main still drives `gantry_cycle`; `PalletizerPlantModel` still owns phase selection. |
| Controls and PLC-owned behavior across all learner demos | **OPEN**: the full gallery inventory and input-to-scan-to-output traces are incomplete. |
| Screenshot-driven network, timer, and tag display | **OPEN**: RungProof-only fixes/captures exist for selected states; same-state comparison to the installed TIA version and full state coverage remain required. |
| Visible asset-to-PLC purpose | **OPEN**: per-demo asset/control/feedback inventory and native review remain required. |

The goal service can report and change status but cannot edit an unfinished
goal's objective in place. Therefore this scope contract and
`docs/DEMO_FUNCTIONAL_ACCEPTANCE.md` are the maintained acceptance definition
for the existing active goal; the goal remains open until all gates above and
the per-instruction matrix pass. No source or machine behavior is declared
fixed by recording these requirements.

### Latest user direction: controls must be PLC-authored and visible — 2026-10-14

This is an explicit expansion of the active project goal, not a request for a
cosmetic revision. The user's latest Demo 5 and editor screenshots add these
hard acceptance points, which take precedence over the older functional demo
passes:

1. **No scene-owned sequence.** Remove the `gantry_cycle` abstraction and any
   scene-side phase/state machine that decides pick, travel, place, release,
   return, or batch completion. `Main` and its called PLC blocks decide these
   transitions from feedback over successive scans. The scene integrates the
   current output commands and reports measured position, limits, and process
   feedback.
2. **Per-motor PLC structure.** Main calls independently inspectable X/Y/Z
   axis or motor FBs and a vacuum/gripper FB, each with an assigned instance
   DB and explicit command, permission, status, and feedback interface. A
   cycle request or one aggregate gantry coil does not satisfy this requirement.
3. **Real machine controls in every lesson.** Inventory every learner-facing
   demo and provide controls that belong to that machine lesson, including a
   logic-connected Reset wherever the lesson has retained state. Motion lessons
   also expose System Enable, Start/Stop, permissions/interlocks, simulated
   trip response, and reset plus fresh-start inhibition. Bind each scene
   control to a symbolic input and prove its effect in the scanned ladder and
   corresponding output/feedback. App playback controls do not count. Do not
   add an irrelevant trip/E-stop to a stationary lesson.
4. **Safety boundary.** The simulator may teach a clearly labeled simulated
   safety-trip interlock. It must not label standard LAD or the offline
   controller as an E-stop or safety-rated function; this is not a safety
   controller, F-I/O, or validated fail-safe design.
5. **TIA network/tag presentation.** A network begins from its actual left
   power rail and uses TIA branch topology without an inner-rail stub or
   floating branch. Operand display and supplementary tag-information modes
   are independent: in Hidden mode, remove optional address/comment/member
   labels such as synthetic `pick_timer.Q` / `pick_timer.ET`, while preserving
   actual operands, instruction pins, and the instance DB heading where shown
   by TIA. Compare the complete first network and the same tag mode against the
   user's installed TIA build; RungProof-only renders do not close this gate.

**Required implementation and proof order:** (a) inventory all demos, scene
controls/assets, symbols, rungs/blocks, outputs, and feedback; (b) implement
and scan-verify missing machine controls/reset behavior in each demo; (c)
replace Demo 5's aggregate cycle/plant sequencer with Main-owned sequence and
the separate FB/DB calls; (d) prove stop, permissive loss, trip, reset,
restart-inhibit, encoder/limit feedback, and all four placements from current
build scan traces; (e) inspect scene command/feedback wiring; and (f) close the
TIA renderer/editor screenshot matrix against matched installed-TIA states.
Each result stays OPEN/FAIL until that specific evidence exists. Do not use a
successful scene animation, old demo-cycle test, source-only review, or
RungProof-only screenshot as a substitute.

This design is grounded in Siemens' description that LAD networks contain a
left power rail where rungs originate and execute in order
([LAD rungs, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/rungs-in-lad/basic-information-on-rungs-in-lad));
that cyclic OBs execute repeatedly and update process-image outputs/inputs
([cyclic OBs, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/functional-description-of-s7-1500-cpus-s7-1500/organization-blocks-s7-1500/description-of-all-types-of-organization-blocks-s7-1500/cyclic-obs-s7-1500));
and that each FB instance requires instance data
([function blocks and instances, STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/blocks-in-the-user-program/function-blocks-fb)).
Siemens' ESTOP1 is part of STEP 7 Safety and requires assigned instance data
and fail-safe behavior; a regular BOOL permissive in this simulator does not
implement it ([ESTOP1, STEP 7 Safety V20](https://docs.tia.siemens.cloud/r/fail_safe_modules_manual_collection_enus_20/comprehensive-information/simatic-safety-configuring-and-programming/step-7-safety-v20-instructions/safety-functions/estop1-emergency-stop/off-up-to-stop-category-1-step-7-safety-v20)).

### 2026-10-14 TIA operand entry field correction

Siemens' [STEP 7 V21 program-editor keyboard reference](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/using-the-keyboard-in-the-program-editor)
specifies that Return activates an empty first-operand text box, typing begins
the entry, F2 activates the selected operand, Return accepts the text, and
Escape restores the prior operand. It also documents Ctrl+I autocomplete and
Ctrl+Shift+I tag definition. RungProof had the keyboard entry points but placed
the editor in a large floating card over the rung.

The operand editor now attaches to the scrolling ladder canvas and replaces
the selected contact/compare/coil or supported instruction operand at its
rendered point of use. The validated staged transaction remains in force;
Return starts an empty first operand, typed entry starts with the typed
character, F2 loads the existing operand, Enter commits, Escape discards, and
Ctrl+I reveals suggestions. Ctrl+Shift+I reveals the existing create-and-assign
action; this is not yet Siemens' exact tag-definition dialog.

Current-source verification: build completed with zero warnings/errors;
`--verify-editor-transactions` passed 53 routes, including the keyboard and
tag-action routes; `--verify-operand-placement` passed for contact and coil
operands; and `--verify-ladder-editor` passed its shortcut and project-tree
routes. Normal rendered TIA-style captures
`.tools/tia-operand-entry-inline-review.png` and
`.tools/tia-operand-output-inline-review.png` were visually inspected. This
proves the RungProof rendering and scripted input routes only. Physical keyboard
input, other timer/counter/numeric/call pin targets, matched installed-TIA
captures, and full-shell parity remain open. The source still has no evidence
for an exact Siemens style match, so operand entry stays **PARTIAL**.

### 2026-10-14 User correction: Demo 5 must teach PLC-owned machine control

The user's latest screenshots and correction make the acceptance scope
explicit: Demo 5 is not acceptable as a PLC-controlled gantry lesson while a
single `gantry_cycle` output lets the scene model decide motion phases. This
also reinforces that the ladder must look like a normal TIA network from its
left power rail; no decorative/offset network-start rail, detached branch,
stub, or wire gap is acceptable. The supplied timer crop also confirms that
supplementary instance-member tags must follow TIA's tag-information setting;
they must not appear when that information is hidden.

Current source inspection in this turn reconfirmed the failure:
`AuthoredDemoLadderPrograms.CreateIntegratedPalletizer` still latches
`gantry_cycle` in Main, and `SceneSimulationRuntime.AdvancePalletizerPlant`
passes that aggregate bit to `PalletizerPlantModel.Step`. The plant model still
owns `PickDown`, `VacuumDwell`, `PickUp`, `Transfer`, `PlaceDown`, `Release`,
`PlaceUp`, and `Return` phase selection and increments its placement count.
The encoder values are projected from that plant pose, so they are feedback
observations, not inputs that close a PLC motion loop. Existing source and
legacy demo pass records therefore do not qualify Demo 5.

This evidence does not mean every demo should receive the same controls or a
decorative E-stop. The acceptance target is a gallery-wide, lesson-appropriate
controls audit: every retained-state lesson needs an authored reset; motion
lessons need PLC-scanned System Enable, Start/Stop, permissions/interlocks,
trip response, reset conditions, and restart inhibition. Only lessons with a
motion hazard get a clearly labeled, non-certified simulated safety-status
input. Main must visibly call separate FB instances/DBs for independently
controlled motors or axes and the gripper/vacuum; scanned feedback must select
the next action. The plant may integrate those commands and publish measured
feedback, but may not choose sequence transitions.

**Why the earlier work kept missing the user's target:** the editor's local
tests and RungProof-only captures verified implementation behavior, but they
did not establish same-state visual parity with installed TIA. In parallel,
the Demo 5 acceptance previously gave credit to a working animation and
encoder display even though scene-side phase logic still owned the sequence.
Those are insufficient proof methods. From this point, separate evidence is
required for (1) Siemens-reference-backed instruction/editor behavior,
(2) matched-state visual inspection, (3) scanned PLC logic and block/DB
architecture, and (4) scene command-to-feedback behavior. A pass in any one
category cannot stand in for another.

**Updated goal order:** inventory all demos and their controls/assets/tags;
close the connected-network, hidden-tag, and instruction-state visual defects
against same-state installed-TIA references; then make machine controls and
resets visible and PLC-authored across the gallery; replace Demo 5's aggregate
gantry sequencer with Main-owned sequence logic and separate FB/DB calls; and
prove start/stop, permission loss, simulated trip/reset/restart inhibit,
encoder/limit feedback, and four placements from accepted scan traces plus
scene feedback. Report unsupported technology-object, drive, and safety
behavior as limitations. The overall goal remains **OPEN**; Demo 5 remains
**FAIL** until this evidence exists. This entry records scope and current
source findings only; it does not claim an implementation fix.

### 2026-10-14 ABS/NEG type-selector parity slice

Official Siemens documentation confirms that ABS and NEG in LAD expose EN/ENO,
unary `IN`/`OUT` pins, and a `???` data-type dropdown. IN and OUT share the
selected type. Siemens' [S7-1200 ABS reference](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/basic-instructions/math-functions/abs-form-absolute-value)
and [S7-1200/S7-1500 NEG V21 reference](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/neg-create-twos-complement-s7-1200-s7-1500-s7-1200-g2)
also document invalid-result/overflow effects on ENO. Siemens' supported
integer widths vary by CPU; the current offline value model cannot represent
SINT, LINT, or LREAL and must not show those choices as if executable.

The TIA selector is now available on ABS and NEG with the honestly bounded
choices `???`, `Int`, `DInt`, and `Real`. The selector stores the selected type;
an unresolved choice infers the declared destination type as the editor already
does visually. The compiler validates input and destination declarations
against the selected or inferred type. Runtime applies single-precision REAL
behavior, checks INT/DINT output range, preserves the previous destination on
overflow, and reports `ABS`/`NEG`-named diagnostics. The renderer uses the
captured fault diagnostic to keep EN green while dropping ENO and the outgoing
conductor. Windowed `.tools/tia-neg-int-overflow-eno.png` was visually
inspected and shows that split state. Same-state installed-TIA confirmation
remains open.

Verification: pinned .NET 10 build succeeded with zero warnings/errors; the
virtual-controller suite passed 344/344, including new selector ordering,
unsupported-Auto rejection, INT/DINT/REAL execution, program and editor JSON
persistence, tag-type mismatch rejection, and typed plus inferred INT/DINT
minimum overflow/no-write checks. Windowed Godot captures
`.tools/tia-abs-type-selector-final.png`, `.tools/tia-abs-type-selector.png`, and
`.tools/tia-neg-type-selector.png` were inspected; they show selected ABS/NEG,
the type control inside the block, and operand/value labels aligned to the
unary pins at the capture's normal canvas scale. The NEG harness also confirmed
the DINT selector interaction route. These are RungProof captures, not
same-state installed-TIA comparisons. Direct physical selector use, DINT/REAL
visual states, TIA-matched captures, and CPU-specific broader types remain
open; ABS and NEG remain **PARTIAL**.

### 2026-10-14 Reaffirmed goal: visible PLC ownership across the gallery

The latest Demo 5 and LAD screenshots reaffirm the existing expanded goal;
they do not narrow it to a renderer-only fix or a single network cleanup. The
screenshots are regression evidence for both network entry/tag visibility and
the machine-control teaching model. Keep these acceptance requirements in the
active goal:

1. **Audit every learner-facing demo.** For each lesson, trace scene controls
   to symbolic PLC inputs, scanned contacts/blocks, output commands, and
   measured feedback. Retained state requires a machine-logic Reset and a
   demonstrated restart path. Motion lessons require visible System Enable,
   Start/Stop, permissives/interlocks, fault/trip response, and reset/restart
   inhibition. Do not add meaningless controls to lessons where they do not
   teach the stated objective.
2. **Teach real block structure.** Demo 5's Main must call the per-axis/motor
   and gripper/vacuum FBs with explicit interfaces and instance DBs. The
   scanned program owns permissions, interlocks, seal-in/state, sequencing,
   reset, and command decisions; scene code owns only physical integration and
   reports position, limits, and process feedback. A generic `gantry_cycle`
   coil or background phase machine is a failure even if all four cartons
   animate successfully.
3. **Keep the TIA network and monitor gates.** Compare the complete first
   network and branch at the same zoom/state as the installed TIA version.
   Rungs originate from the actual left power rail with TIA branch topology.
   Hidden tag-information mode removes supplementary addresses, comments, and
   synthetic member labels while retaining actual operands, pins, and DB
   identity as TIA displays them. Timer/counter output values stay on their
   own pin rows. Treat the supplied four screenshots as regressions; do not
   infer a parity pass or defect root cause from a cropped RungProof-only view.
4. **Separate proof.** Track gallery inventory, PLC scan traces, scene
   command/feedback checks, RungProof interaction/render inspection, and
   same-state installed-TIA comparison independently. A source change, passing
   unit suite, or successful animation closes none of the other gates.

The Siemens semantic basis is the [LAD programming language, STEP 7 V21](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/lad-programming-language),
which states that a network has a left power rail where rungs originate and
that series/parallel arrangement defines circuit topology, and the [FB
documentation, STEP 7 V20](https://docs.tia.siemens.cloud/r/en-us/v20/programming-basics/blocks-in-the-user-program/function-blocks-fb),
which defines FB calls as instances with persistent instance-specific data.
The per-axis FB design is the requested simulator teaching architecture, not
a claim that these blocks are commissioned Siemens Motion Control technology
objects. A simulated trip remains an instructional interlock, never a
safety-rated E-stop.

Current-source disposition is unchanged: **Demo 5 FAIL**. The plant model
still consumes PLC-owned `gantry_cycle` and `vacuum_pick` points to advance its
own phases. Gallery-wide controls inventory and same-state TIA visual review
are **OPEN**. This clarification updates acceptance scope only; it does not
claim that the requested implementation work is complete.

### 2026-10-14 LAD FC/FB project-tree insertion slice

Siemens STEP 7 V21 documents inserting existing FC and FB calls into LAD by
dragging the block from Program blocks onto the network. Dropping an FB opens
Call options for a single, multi, or parameter instance; the documented
single-instance path assigns a DB and creates it if missing. See Siemens
[LAD block-call drag/drop](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/insert-block-calls-in-lad/inserting-block-calls-using-a-drag-and-drop-operation?contentId=DLDMKzyVsYR4GEuDUwdqBw)
and [S7-1500 CALL in STL](https://docs.tia.siemens.cloud/r/en-us/v20/stl-s7-1500/stl-mnemonic-s7-1500/program-control-operations-s7-1500/code-blocks-s7-1500/call-call-block-s7-1500).
The LAD element is the named FC/FB call box; the generic `CALL` mnemonic
belongs to the STL reference and is not the name of a generic LAD instruction.

RungProof's TIA project tree now provides a validated drag payload for program
block rows, and the LAD canvas accepts a parameterized FC/FB dropped onto a
network's terminal instruction. The handler selects the target block, opens
the call authoring form for typed actuals and any FB instance, and rejects
non-callable blocks, self-calls, and legacy-mode targets. A focused payload
test covers valid and invalid project-tree rows. The old palette shortcut
remains, and the editor data model still stores calls as terminal actions;
therefore this is an interaction improvement, not TIA workflow parity. It
does not support dropping the call at an arbitrary instruction slot or
multi-instance/parameter-instance FB calls. The current call authoring form is
also not a visual reproduction of TIA's FB Call options dialog or inline
parameter placeholders.

Verification: pinned .NET build succeeded with zero warnings/errors; virtual
controller/editor suite passed 347/347. A normal-renderer capture of the
running app shell at `build/review/block-call-dnd.png` was inspected for the
current project-tree and LAD layout; no physical drag gesture was performed,
so it does not prove that the new interaction behaves correctly end to end.
Same-state TIA rendering and FB dialog comparison remain OPEN. The current
`--verify-app-shell` run fails because its structure audit finds all five
saved demo project JSONs differ from the in-code authored demo projects; the
starter challenge hash warning also remains. The source of those differences
has not been established, and this call slice does not close that gate.

The call instruction row remains **PARTIAL** and the overall goal remains
**OPEN**. Next acceptance work is physical drag/drop verification (including
undo and project reload), a matched TIA FC and FB call capture, and a model
that can represent calls at TIA-supported network positions before removing
the palette shortcut.

### 2026-10-14 TIA empty instruction box authoring state

Siemens' V21 instructions describe placing an empty box into a LAD network,
then selecting the desired instruction from the box's dropdown; the editor
also supports F8 for inserting an empty box. See [Insert LAD elements using an
empty box](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/inserting-lad-elements-using-an-empty-box)
and [program-editor keyboard shortcuts](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/using-the-keyboard-in-the-program-editor).

RungProof now stores an empty box separately from an executable terminal
instruction, draws a compact `??? / IN / OUT` body directly on the rung wire,
provides a dropdown on its top-right corner, offers a `???` palette insertion,
and rejects Verify + Load until an instruction replaces the placeholder. F8
also inserts the empty box. The placeholder state survives project save/reopen
and undo. The dropdown currently offers only a subset of box instructions
already supported by RungProof; it is not the full TIA instruction chooser,
and its categories, search, placement, and instruction coverage remain
visibly different. The screenshot fixture uses a two-path conveyor rung to
inspect the branch join and wire contact.

Verification: build succeeded with zero warnings/errors; the virtual
controller/editor suite passed 348/348. I inspected the running-app captures
`build/review/tia-empty-box-final.png` and
`build/review/tia-empty-box-chooser-final.png`. A running-shell UI verifier
opened the chooser, selected CTU, checked that the placeholder became an
executable counter instruction, and confirmed the saved project retained the
replacement. The images show continuous wires meeting the placeholder body
and a reachable dropdown, but this is not a same-size/same-state comparison
against the actual TIA application. The
headless app still reports the pre-existing starter challenge hash warning
and Godot shutdown resource-leak diagnostics. The full instruction-matrix and
project-tree parity goals remain **OPEN**.

### 2026-10-14 TIA blank-box versus named-block-call correction

The latest supplied TIA screenshots show two distinct editor objects/workflows:
the selected `??? / IN / OUT` empty box with the instruction chooser is a
generic instruction insertion placeholder; the RungProof `Station counter`
box with `%DB2` above it is a named FB call. Siemens LAD does support named FC
and FB call boxes with EN/ENO, but that does not make a generic `CALL` palette
button equivalent to the empty-box chooser. Siemens documents the LAD box
classes and code-block calls [here](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/basic-information-on-lad/overview-of-the-lad-elements?contentId=Bdy6OXDeAq4nyi9Q1i214A)
and project-tree FC/FB insertion [here](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/insert-block-calls-in-lad/inserting-block-calls-using-a-drag-and-drop-operation?contentId=DLDMKzyVsYR4GEuDUwdqBw).

Correction made: the Siemens-facing generic `FC / FB CALL` palette button is
hidden, the call help text now identifies it as a named FC/FB block call, and a
missing target renders as `???` instead of the generic `CALL` title. The
project-tree drag/drop path remains available, but accepts only parameterized
targets at a network terminal and still uses a custom call form. No physical
drag gesture was performed during this review. The capture
`build/review/named-fb-box-no-generic-call.png` was inspected: it shows a named
`Station counter` FB box and its DB identity; it is not a same-state comparison
to the user’s empty-box chooser image.

The first-network visual gate therefore remains **OPEN**. The editor still
lacks TIA's empty-box chooser at the insertion point, inline instruction
editing, and the matched network/branch layout. The current RungProof screenshot
also exposes custom `Blocks / tasks` navigation and a separate palette layout;
this turn did not redesign those surfaces. Build passed with zero warnings and
errors; the virtual-controller suite passed 347/347; `--verify-call-authoring`
passed staging, invalid-input rejection, apply, persistence, undo, and cancel
checks. Runtime capture still reports the existing `CH_INVALID_CHALLENGE`
starter-content hash warning and Godot resource-leak warnings on process exit.
`--verify-app-shell` still fails because all five saved demo project JSON files
differ from the in-code Demo menu definitions. None of these checks passes the
first-network or physical drag/drop visual/functional gate. Overall goal remains
**OPEN**.

#### Current window-level visual inspection

The current review build was captured from its actual application window at
`build/review/tia-call-window-print.png` and inspected at 100%. It confirms a
named `Station counter` FB call with `%DB2 "FB_Station_B_0_DB"` above the box;
the generic `CALL` title is absent. The two branch legs originate at the left
rail and rejoin before this call. This answers the earlier screenshot mismatch:
the TIA `??? / IN / OUT` with its chooser open is an empty instruction
placeholder, while this RungProof fixture is a populated FB call. They are not
equivalent states and must not be compared as if they were.

The inspection also confirms remaining visible differences that keep parity
open: RungProof's custom tabbed left navigation and palette are unlike the TIA
project tree / inline chooser; the empty-box action is a literal `???` toolbar
button; the block's formal pins and selection treatment need a same-state
TIA comparison. A window capture verifies RungProof rendering only, not TIA
equivalence. The app was launched with an unsaved review fixture and must not be
treated as a user project or release build.

### 2026-10-14 named call versus empty-box review

Siemens STEP 7 V21 documents two valid but distinct LAD authoring paths: (1)
drag an existing FC/FB from Program blocks into the network; for an FB, TIA
opens call options for single, multi, or parameter instance; (2) place Basic
instructions > General > Empty box and choose a supported instruction from its
dropdown. The current user screenshot's `??? / IN / OUT` is path (2), not a
block call. The RungProof `Station counter` box labelled with `%DB2` is a named
FB call, a real LAD construct, but its existence does not match the empty-box
state in the TIA screenshot. See Siemens [empty-box workflow](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/inserting-lad-elements-using-an-empty-box)
and [FC/FB drag-and-drop workflow](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/insert-block-calls-in-lad/inserting-block-calls-using-a-drag-and-drop-operation?contentId=DLDMKzyVsYR4GEuDUwdqBw).

The misleading generic `FC / FB CALL` TIA-palette control remains hidden.
RungProof's review fixture now opens the launch modal-free ladder view with two
parallel contact paths and an empty terminal instruction, and `--verify-tia-empty-box`
exercises all 30 currently supported chooser entries, confirms each replaces
the placeholder with the expected model operation, then saves/reloads the
project. This is simulator-supported coverage, not TIA's complete instruction
catalog. A rendered capture was inspected at
`build/review/tia-empty-box-chooser-current.png`; it confirms a bounded,
scrollable chooser, but the 1481x837 viewport crops the lower part of the
network. The editor still has its custom `Blocks / tasks` tab, custom palette
and overall workbench chrome, so this capture does not pass full interface
parity. The first network is a matched object-state review only; placement,
selection, chooser positioning at other zooms, and actual physical drag/drop
remain OPEN. Named-call staging,
validation, apply, persistence, undo and cancel pass `--verify-call-authoring`;
that verifies RungProof behavior, not TIA call-options parity. Current model
does not support multi-instance or parameter-instance FB calls.

Build passed with 0 warnings and 0 errors; virtual-controller suite passed
348/348. Both headless editor verifiers passed. They emit the known
`CH_INVALID_CHALLENGE` starter hash warning and Godot shutdown resource-leak
diagnostics. Overall TIA parity remains **OPEN**.

### 2026-10-14 empty-box input lead review

The user's close-up showed the empty instruction box appearing attached to the
parallel junction. The TIA empty-box path reserves 80 logical pixels of
straight conductor from the branch merge to the left edge of the box, keeping
the box aligned to the upper branch row after both paths rejoin.
`GetTerminalInputLeadLength` exposes the measured distance to the review
harness, and `--review-tia-empty-box` asserts 80 px.

Captures `build/review/tia-empty-box-lead-80px-100.png` and
`build/review/tia-empty-box-lead-80px-75.png` were visually inspected. The
focused verifier passes all 30 chooser replacements and project JSON save/reload.
This verifies RungProof's geometry and editor model only; matching installed-TIA
placement/chooser comparison and full editor parity remain open.

### 2026-10-14 NO/NC contact geometry and state slice

The next unresolved bit-logic review used the user's actual installed-TIA
captures `codex-clipboard-feb7e9be-73a1-4048-955d-22dde472a6e1.png` and
`codex-clipboard-1ab7473b-10c5-406b-8b3a-50f5329fddd6.png`, not a
RungProof-only screenshot. Both TIA captures show conductors meeting the NO/NC
bars without a gap and an NC slash contained inside the bar envelope. The
slash direction is bottom-left to top-right. Operand text is centered above
the contact. The official STEP 7 V21
[normally open](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/normally-open-contact-s7-1200-s7-1500-s7-1200-g2)
and [normally closed](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/normally-closed-contact-s7-1200-s7-1500-s7-1200-g2?contentId=IeoGUKGSZqx5uM3E6NsvqQ)
references confirm the BOOL and series/parallel execution behavior; the supplied
screenshots are the geometry evidence.

The previous Siemens renderer reserved a 20 logical px half-gap around bars
drawn at 13 logical px from contact center, producing a visible conductor break.
It also drew the NC slash outside the bars. The TIA skin now ends each conductor
at the bar, uses a compact 26 x 30 logical bar envelope, and contains the slash
inside that envelope. The Rockwell skin retains its prior geometry. No other
instruction dimensions were changed.

The new `contact-tia` running-review fixture independently evaluates NO and NC
with both BOOL states and asserts `NO(FALSE)=0`, `NO(TRUE)=1`, `NC(FALSE)=1`,
and `NC(TRUE)=0`, including the matching output coils. Normal Vulkan captures
were visually inspected at `.tools/tia-contact-no-offline.png`,
`.tools/tia-contact-no-selected-offline.png`,
`.tools/tia-contact-nc-offline.png`,
`.tools/tia-contact-nc-selected-offline.png`,
`.tools/tia-contact-no-false-online.png`, and
`.tools/tia-contact-nc-online-75.png`. They show continuous conductors at 100%
and 75%, the corrected slash envelope, and true/false online paths.

This slice advances NO and NC to **PARTIAL**, not pass. The supplied TIA views
are offline and do not expose their LAD zoom. Same-state TIA evidence for
selected contacts, inline operand editing, and online status colors is still
missing. The current large RungProof subtree-selection outline is therefore
recorded but not claimed as TIA selection parity.

### 2026-10-14 editor delete-route verifier correction

The full native editor verifier initially stopped at
`OutputDelete_coil_keyboard`. Instrumentation showed that the selected output
and keyboard focus were valid, but the Siemens ladder canvas was not visible.
The preceding named FC creation test opens a new central editor document; its
undo removes that block without making the original block document active
again. `LadderEditorCanvas._ShortcutInput` correctly ignores shortcuts while
the canvas is hidden, so this was a verifier-state false negative rather than
an editor delete defect.

The command-route verifier now activates `EditorDocument_0` through the real
document-tab signal before testing output deletion and explicitly asserts
`OutputDeleteLadderDocumentVisible`. Temporary diagnostic prints were removed.
Two clean `--verify-ladder-editor` runs passed context-menu, toolbar, keyboard,
and undo routes for coil, counter, and call terminals, ending with
`EDITOR_COMMAND_ROUTES_VERIFY PASS`. Build passed with 0 warnings and 0 errors;
the virtual-controller suite passed 349/349. The existing invalid challenge
starter-hash warning and Godot headless shutdown leak diagnostics remain and
are not evidence of TIA interface parity. Overall TIA parity remains **OPEN**.

### 2026-10-14 simulator-only instruction segregation

Siemens STEP 7 V21 models counter preset loading as the `LD` input of
[CTD](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/counter-operations-s7-1200-s7-1500-s7-1200-g2/ctd-count-down-s7-1200-s7-1500-s7-1200-g2?contentId=oLiHDq7BX5tXv0mFOZV~pA)
and counter reset as the `R` input of
[CTU](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/counter-operations-s7-1200-s7-1500-s7-1200-g2/ctu-count-up-s7-1200-s7-1500-s7-1200-g2?contentId=b0mTsoQgH5~UUqxMm0Fc2Q).
RungProof's standalone counter LOAD/RESET operations and `FORMAT_TEXT` are
portable teaching/runtime helpers, not Siemens LAD instructions. Presenting
them inside the native-looking Counter and Math categories was misleading even
with tooltips.

The Siemens-facing toolbar now groups `LOAD COUNTER`, `RESET COUNTER`, and
`FORMAT_TEXT` under an explicit `RungProof local (not Siemens)` category. The
Instructions task tree places those operations plus `CODE MATCH` beneath the
same local heading. Native CTU/CTD remain in Counter operations, and the
empty-box chooser remains limited to its separately verified Siemens-facing
subset. Existing local projects still load, render, edit, and execute; this is
UI segregation, not removal of the local model.

The native verifier reports `TIA_LOCAL_INSTRUCTION_SEPARATION_VERIFY PASS` and
also exercises insertion/runtime behavior for the local operations. The normal
Vulkan running-editor capture `.tools/tia-local-instruction-category.png` was
visually inspected at 1600x900 and shows the explicit heading and three local
buttons without clipping. Build passed with 0 warnings and 0 errors. This
removes four false Siemens parity gates (`counter-load`, `counter-reset`,
`compare-6`, and `numeric-24`) by classifying them as `N/A`; it does not close
any remaining native-instruction state gate. Overall TIA parity remains
**OPEN**.

### 2026-10-14 Set/Reset output semantic and editor slice

Siemens STEP 7 V21 defines [S: Set output](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/s-set-output-s7-1200-s7-1500-s7-1200-g2?contentId=Wh9qyAlMOIlzh6OBxbOEYA)
as writing TRUE when incoming RLO is TRUE and otherwise preserving the target.
[R: Reset output](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/r-reset-output-s7-1200-s7-1500-s7-1200-g2?contentId=C2FGQIC3pRy1AxpaDUjqVg)
writes FALSE when incoming RLO is TRUE and otherwise preserves the target. For
S7-1200/S7-1500, Siemens permits these outputs within or at the end of a
network. RungProof's current editor/runtime model supports only a terminal
output. This is a material unsupported placement limitation, so neither row
can pass even if all terminal-output states match.

The new `coil-tia` running-review fixture contains four independent networks:
S with false RLO preserves TRUE, S with true RLO writes TRUE, R with false RLO
preserves FALSE, and R with true RLO writes FALSE. Each capture independently
asserts those final values while retaining the separate incoming RLO
observation and prints `TIA_SET_RESET_STATE_PASS`. This specifically prevents
the common monitoring error of coloring the coil from its stored target value
instead of the rung's power flow.

Normal Vulkan captures were visually inspected at
`.tools/tia-set-offline.png`, `.tools/tia-set-selected-offline.png`,
`.tools/tia-reset-offline.png`, `.tools/tia-reset-selected-offline.png`,
`.tools/tia-set-false-rlo-online.png`, `.tools/tia-set-true-rlo-online.png`,
`.tools/tia-reset-false-rlo-online.png`, and
`.tools/tia-reset-true-rlo-online.png`. The offline states show unboxed S/R
coil symbols, destination operands above the coil, and continuous conductors.
The selected states retain those features within the RungProof selection
outline. Online false-RLO paths are blue-dashed even when S preserves a TRUE
destination; online true-RLO paths are green even when R writes a FALSE
destination. Stored values remain visible above their respective coils.

The native interaction verifier now opens an output through the real editor
path, confirms the exact property choices `Assign (=)`, `Set (S)`, and
`Reset (R)`, applies Set and Reset to another writable BOOL operand, and undoes
back to the original mode, operand, and stable terminal ID. It reports
`TIA_COIL_PROPERTY_EDIT_VERIFY PASS` and the complete ladder interaction and
command-route verifiers pass. Build completed with 0 warnings and 0 errors;
the virtual-controller suite passed 349/349. The known invalid challenge
starter-hash warning and Godot shutdown leak diagnostics remain unrelated.

This advances the Set and Reset rows from mostly OPEN to **PARTIAL** terminal-
output coverage. Same-state installed-TIA offline, selected, property-edit,
and online-color comparisons are still missing, and within-network placement
is unsupported. Overall TIA parity remains **OPEN**.

### 2026-10-14 Assignment output overwrite and RLO slice

Siemens STEP 7 V21 defines the LAD
[Assignment](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/assignment-s7-1200-s7-1500-s7-1200-g2)
coil as copying its incoming RLO to the BOOL destination: TRUE writes TRUE and
FALSE writes FALSE. The instruction does not modify the RLO and passes it
directly to its output. RungProof's current model supports assignment coils
only as terminal actions, so it cannot place later logic after the coil. This
is an unsupported Siemens behavior, not a visual-only discrepancy.

The new `assignment-coil-tia` fixture prevents a stale destination value from
masking an error. Its false-RLO destination begins TRUE and must be overwritten
FALSE; its true-RLO destination begins FALSE and must be overwritten TRUE. The
fixture separately asserts the final destinations and the false/true incoming
RLO observations, then prints
`TIA_ASSIGNMENT_COIL_STATE_PASS falseWrite=0 trueWrite=1`.

Normal Vulkan captures `.tools/tia-assignment-coil-offline.png`,
`.tools/tia-assignment-coil-selected-offline.png`,
`.tools/tia-assignment-coil-false-online.png`, and
`.tools/tia-assignment-coil-true-online.png` were visually inspected at 100%.
The offline views show unboxed assignment coils, operands above them, and
continuous conductors. The selected view retains the symbol and conductors
inside the RungProof selection outline. Online monitoring shows the overwritten
FALSE destination with blue-dashed flow and the overwritten TRUE destination
with green flow, so both stored value and conductor state agree with the RLO.

The shared output-property verifier starts from an assignment coil, confirms
the exact `Assign (=)`, `Set (S)`, and `Reset (R)` choices, applies alternate
modes and another writable BOOL operand, and proves undo restores the original
assignment mode, operand, and stable terminal ID. It reports
`TIA_COIL_PROPERTY_EDIT_VERIFY PASS`; the complete ladder interaction and
command-route verifiers pass. Build completed with 0 warnings and 0 errors;
the virtual-controller suite passed 349/349 with no real PLC transport
constructed or connection attempted. A headless frame-capture attempt was
stopped after the process remained live without producing a frame; the normal
Vulkan capture route completed all four states. The known invalid challenge
starter-hash warning and Godot shutdown leak diagnostics remain unrelated.

This advances all four assignment-coil gates to **PARTIAL**. Same-state
installed-TIA offline, selected, property-edit, and monitoring comparisons are
still missing, and downstream in-network continuation remains unsupported.
Overall TIA parity remains **OPEN**.

### 2026-10-14 DIV zero-divisor V21 correction and exact-state evidence

This milestone supersedes the earlier audit notes that left DIV-by-zero open.
The official Siemens STEP 7 V21
[`DIV` LAD page](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/div-divide-s7-1200-s7-1500-s7-1200-g2)
is current and explicitly includes the target S7-1500. It states that division
by zero keeps ENO true; INT writes zero; REAL writes decimal `2143289344`,
except REAL zero divided by zero writes NaN. The official S7-1500 V21
[`/D` page](https://docs.tia.siemens.cloud/r/en-us/v21/stl-s7-1500/stl-mnemonic-s7-1500/math-functions-s7-1500/integers-s7-1500/d-divide-double-integers-32-bit-s7-1500)
independently states that 32-bit integer division by zero reports a zero
quotient. For this S7-1500/V21 parity target, those sources are more specific
than the older S7-1200-only manual that caused the earlier hold.

RungProof no longer faults and preserves OUT for DIV-by-zero. INT and DINT now
write zero; REAL writes `2143289344`; REAL 0/0 writes NaN; all four paths keep
ENO true and publish no diagnostic. The compiler now accepts a literal DIV
divisor of zero because the instruction defines its result. At this DIV
milestone, MOD remained unchanged pending target-S7-1500 evidence. The later
"MOD zero-divisor correction and exact-state evidence" section supersedes that
hold.

The controller suite asserts INT, DINT, REAL, and REAL 0/0 results, ENO/local
result, write disposition, and zero diagnostics. It also verifies that literal
INT 7/0 compiles and executes. Tests that only needed a recoverable no-write
numeric fault now use MOD rather than encoding the removed DIV behavior.

The dedicated native fixture `.tools/tia-div-zero-online-v3.png` was inspected
at 100%. It shows INT 7/0 writing 0 from an initial 12, with both EN and ENO
conductors green. `TIA_ARITHMETIC_STATE_PASS` independently verifies before
12, calculated/stored 0, `WrittenChanged`, ENO true, and zero diagnostics;
`TIA_ARITHMETIC_GEOMETRY_PASS` confirms the reviewed 132x96 body and 32 px
input pitch.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was
constructed or attempted. The pre-existing invalid challenge-content warning
and Godot shutdown RID/ObjectDB leak reports remain.

DIV is now **PARTIAL**, not complete: matched installed-TIA zero-divisor
monitoring, exact installed-TIA geometry, unsupported Siemens numeric widths,
other zooms and tag modes, project interchange, and live S7-1500 execution
remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens NORM_X and SCALE_X dual-type correction

Official STEP 7 V21 LAD documentation and example images for
[`NORM_X`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/norm_x-normalize-s7-1200-s7-1500-s7-1200-g2?contentId=uA~cqSMsnHtloOSfeOPy3A)
and
[`SCALE_X`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/scale_x-scale-s7-1200-s7-1500-s7-1200-g2)
were inspected directly. NORM_X shows `DINT to REAL`: MIN, VALUE, and MAX
share the input/range type while OUT is floating point. SCALE_X shows the
reciprocal `REAL to DINT`: VALUE is floating point while MIN, MAX, and OUT
share the output/range type. Both use EN/ENO and the existing
MIN/VALUE/MAX/OUT pin order. This contradicted RungProof's former untyped
three-operand presentation.

Both instructions now reuse the persisted independent input/output type fields
already used by CONVERT. The supported NORM_X subset offers INT, DINT, or REAL
for the input side and REAL for output. SCALE_X offers REAL for input and INT,
DINT, or REAL for output. Compiler validation enforces matching typed tags and
integer-literal representability. Program JSON and editor-project JSON retain
both sides. Runtime arithmetic rounds through the modeled REAL width and
range-checks the selected output. A VALUE outside MIN..MAX is not rejected when
the computed result remains representable, matching the documented equation
boundary rather than imposing an artificial 0..1 clamp.

The selected editor controls initially overlapped even though each stayed
inside the instruction body. Visual inspection caught that defect. The compact
controls now disable longest-item sizing, remain disjoint, and reserve a visible
`to` gap. Focused assertions reject future overlap. Normal Vulkan captures
`.tools/tia-norm-offline.png`, `.tools/tia-norm-selected-online.png`,
`.tools/tia-scale-offline.png`, and `.tools/tia-scale-selected-online.png` were
inspected at 100%. They show `DInt to Real` and `Real to DInt`, correct pin rows,
live values, EN/ENO flow, selection bounds, and dropdown interaction.

The local SDK build passed with 0 warnings and 0 errors. The rebuilt controller
suite passed 355/355 without constructing or attempting a real PLC transport.
Rows `numeric-18` and `numeric-19` advance to **PARTIAL**. Wider Siemens types,
matched installed-TIA offline/selected/online captures, overflow comparisons,
and authoritative fault-result values remain open. The simulator currently
preserves the prior destination on range, invalid-input, and overflow faults;
that is not claimed as an exact Siemens match. Overall TIA parity remains
**OPEN**.

### 2026-10-14 Siemens comparator family correction

The official STEP 7 V21 [`CMP ==`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/comparator-operations-s7-1200-s7-1500-s7-1200-g2/cmp-equal-s7-1200-s7-1500-s7-1200-g2?contentId=89iNuOP56Ffprg7zIjqDZQ)
and [`CMP <>`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/comparator-operations-s7-1200-s7-1500-s7-1200-g2/cmp-not-equal-s7-1200-s7-1500-s7-1200-g2?contentId=nQ9KvGMpDgqimsk~dbE63g)
documentation and the official equality example were inspected directly. The
example places operand 1 above the instruction, operand 2 below it, and the
selected data type in a small box between two vertical comparator rails. The
former RungProof rendering placed the operands to the left and right, omitted
the rails, and drew the type as unboxed text.

The shared six-operator renderer now centers the operands above and below,
draws the two rails, and uses the in-body type box/dropdown. Operand edit and
hit targets were moved with the visible labels. Normal Vulkan captures
`.tools/tia-comparator-offline-revised.png` and
`.tools/tia-comparator-selected-revised.png` were visually inspected at 100%;
the selected outline and DInt dropdown do not obscure the operands, rails, or
conductors. The deterministic review fixture now accepts all six comparison
operators and generated inspected true-state captures named
`.tools/tia-compare-*-true-revised.png`.

The Siemens not-equal documentation also states that a comparison with an
invalid floating-point operand returns FALSE. C# normally evaluates
`NaN != value` as true, so the numeric runtime now rejects NaN before applying
any of the six operators. A regression check generates NaN through SQRT(-1)
and proves `==`, `<>`, `>`, `>=`, `<`, and `<=` all return FALSE.

The local SDK build passed with 0 warnings and 0 errors. The rebuilt
virtual-controller/editor suite passed 351/351. `--verify-ladder-editor`
exited 0 with comparator type edit/undo/redo and the full command-route suite
passing. Its existing invalid challenge hash, offscreen headless popup, and
Godot shutdown-leak diagnostics remain unrelated.

All six comparator rows move from OPEN to PARTIAL. Exact matched installed-TIA
offline, selected, edit-popup, and monitoring captures are still absent, and
the supported type set remains narrower than Siemens. Overall TIA parity
remains **OPEN**.

### 2026-10-14 JMP and RET coil-form program-control slice

Siemens STEP 7 V21 documents
[JMP](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/program-control-operations-s7-1200-s7-1500-s7-1200-g2/jmp-jump-if-rlo-1-s7-1200-s7-1500-s7-1200-g2?contentId=KhfLS4DbhPuqOhfgl4f74A)
and
[RET](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/program-control-operations-s7-1200-s7-1500-s7-1200-g2/ret-return-s7-1200-s7-1500-s7-1200-g2?contentId=lhCb6mPKhOdBnmUeoI1CLA)
as coil-form terminal instructions. JMP displays its destination label above the
coil. A TRUE incoming RLO takes the jump or return; FALSE falls through. The
former RungProof renderer instead drew both as generic gray boxes with EN/ENO
pins. That representation was contradicted by the official instruction form.

The Siemens-style canvas now draws open IEC JMP and RET coils on continuous
conductors, places the JMP target above its symbol, retains the instruction
during selection, and colors the complete terminal path from the observed
incoming RLO. This is deliberately limited to the reference-backed JMP/RET
change. The current generic LABEL box remains known-unverified and is not
claimed as Siemens geometry.

The new `program-control-tia` review fixture executes nine networks in one
block. It proves a false JMP falls through, a true JMP skips to the block-local
label, a false RET falls through, and a true RET prevents the following network
from executing. It separately checks `NetworkEnable`, `ControlOutcome` values
`NotTaken`/`Taken`, final output values, and the absence of observations for
skipped coil instructions. Every capture prints
`TIA_PROGRAM_CONTROL_STATE_PASS jmpFalse=NotTaken jmpTrue=Taken retFalse=NotTaken retTrue=Taken`.

Normal Vulkan captures were visually inspected at
`.tools/tia-jmp-false-offline.png`, `.tools/tia-jmp-selected-offline.png`,
`.tools/tia-jmp-false-online.png`, `.tools/tia-jmp-true-online.png`,
`.tools/tia-ret-offline.png`, `.tools/tia-ret-selected-offline.png`,
`.tools/tia-ret-false-online.png`, and `.tools/tia-ret-true-online.png`. Offline
and selected images preserve symbol, text, and conductor continuity. Online
images distinguish false blue-dashed RLO from true green RLO and visibly leave
networks skipped by the taken transfer in the unknown/unobserved state.

Build completed with 0 warnings and 0 errors. The virtual-controller/editor
suite passed 349/349, and `--verify-ladder-editor` exited 0 with the complete
interaction and command-route checks passing. Its existing invalid challenge
starter-hash warning, offscreen headless popup diagnostics, and Godot shutdown
leak messages are unrelated to this slice.

The RET row cannot pass: RungProof does not model Siemens' selectable return
value (`RLO`, `TRUE`, `FALSE`, or BOOL operand) that supplies caller ENO, and it
does not claim complete OB return behavior. LABEL offline, selected, and monitor
gates also remain open because there is no accepted reference-backed geometry.
Same-state installed-TIA comparisons remain missing for JMP and RET. Overall
TIA parity remains **OPEN**.

### 2026-10-14 Siemens LABEL grammar correction

The official STEP 7 V21
[LABEL documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/program-control-operations-s7-1200-s7-1500-s7-1200-g2/label-jump-label-s7-1200-s7-1500-s7-1200-g2?contentId=8cBMdACqyMR4XJkgqaLcZA)
requires a label name made only of letters, or letters followed by digits. It
explicitly excludes special characters and number-first names. The existing
Siemens editor accepted every nonblank string and inserted `label_1`, so its
own default violated the documented grammar.

The Siemens-facing insertion path now creates `Label1` and reuses only existing
labels that satisfy the TIA grammar. Its transactional JMP/LABEL property form
rejects blank, number-first, underscore, whitespace, special-character, and
letters-after-digits forms before they can mutate the draft. The shared
program/compiler remains vendor-neutral so existing Logix and legacy label
names are not silently invalidated.

A new unit check covers accepted `A`, `finish`, `CAS1`, and `Network123` forms
plus the invalid boundary cases. The native editor verifier opens the actual
LABEL property form, submits `1finish`, proves the form stays open with the
exact grammar diagnostic and the rung remains `finish`, then commits the valid
original value. Its interaction summary now reports all five JMP/LABEL checks
true. Build passed with 0 warnings and 0 errors, the rebuilt suite passed
350/350, and `--verify-ladder-editor` exited 0. Known challenge-hash, offscreen
headless popup, and shutdown-leak diagnostics remain unrelated.

This closes the known Siemens lexical-authoring defect, but not LABEL visual
parity. Offline, selected, and monitored LABEL geometry remain **OPEN** because
the current generic EN/ENO box is not backed by an accepted reference image.
Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens LABEL network-marker correction

The official STEP 7 V21 LABEL example was inspected directly. It shows `CAS1`
as a compact marker above Network 3, immediately beside the left rail, while
the contact and output logic occupy the separate rung below. This directly
contradicts RungProof's former terminal `LABEL` box with `EN` and `ENO` pins.

The Siemens renderer now draws LABEL as a 62 by 24 logical-pixel network marker,
keeps the destination network conductor continuous, moves selection to the
marker, and removes the fabricated block title and enable pins. The focused
review route asserts the marker bounds and prints
`TIA_LABEL_MARKER_GEOMETRY_PASS`. Normal Vulkan captures
`.tools/tia-label-network-marker.png`,
`.tools/tia-label-network-marker-selected.png`, and
`.tools/tia-label-network-marker-online.png` were inspected at 100%.

This moves LABEL offline and selection evidence to **PARTIAL**, not PASS. The
shared schema still represents LABEL as a mutually exclusive terminal action,
so it cannot place a separate coil or box action in the same labeled network as
the Siemens example does. The online image proves only simulator flow; no
official monitored-state reference or matched installed-TIA capture exists.
Overall TIA parity remains **OPEN**.

This was the state at the time of that marker-only slice. The later
"LABEL network-metadata and destination-action audit" below closes the shared-
schema composition defect while retaining the remaining comparison gaps.

### 2026-10-14 Siemens EXPT type and fault-state correction

The official STEP 7 V21
[`EXPT` documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/expt-exponentiate-s7-1200-s7-1500-s7-1200-g2)
and its LAD example were inspected directly. Siemens requires a floating-point
base at IN1, permits an integer or floating-point exponent at IN2, writes a
floating-point OUT, and drops ENO on execution errors. The example shows two
separate REAL chips with `**` between them above the EN/ENO row. RungProof had
the correct pin names but treated EXPT as an untyped generic numeric box.

EXPT now requires a REAL base and REAL destination in the supported model. Its
type selection records the exponent type and offers only `???`, `Int`, `DInt`,
and `Real`; LREAL and the remaining Siemens integer widths stay explicitly
unsupported. The unselected renderer draws a fixed Real base chip, `**`, and a
separate exponent chip. Selection replaces only the exponent chip with the
dropdown. The compiler rejects non-REAL base/destination tags and exponent tags
that disagree with the selected or inferred type. The type survives program
JSON, editor conversion, and editor-project JSON.

Normal Vulkan captures `.tools/tia-expt-offline-v2.png`,
`.tools/tia-expt-selected-v2.png`, `.tools/tia-expt-online-v2.png`, and
`.tools/tia-expt-domain-fault-v2.png` were inspected at 100%. The valid state
shows 2 ** 8 = 256 with EN and ENO energized. The negative-base fractional-
exponent state keeps EN energized and drops ENO/downstream flow while reporting
the controller domain diagnostic. Destination retention on that fault is a
simulator behavior, not a claimed Siemens match.

The focused interaction capture `.tools/tia-expt-selected-int-interaction.png`
visually preserves the fixed Real base chip while selecting Int for the
exponent. The fixture emitted `ARITHMETIC_TYPE_INTERACTION_PASS requested=Int`,
proving the native dropdown signal rather than only its static appearance.

The local SDK build passed with 0 warnings and 0 errors. The rebuilt suite
passed 352/352, and `--verify-ladder-editor` exited 0 with its complete native
interaction and command-route checks passing. Existing invalid challenge-hash,
offscreen headless-popup, and shutdown-leak diagnostics remain unrelated.

EXPT advances from OPEN to **PARTIAL**. Exact installed-TIA scale, selected and
popup interaction, wider data types, and a matched online/fault comparison are
still missing. Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens LN and trigonometric unary-math slice

Official STEP 7 V20 LAD documentation for
[`LN`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/ln-form-natural-logarithm-s7-1200-s7-1500),
[`SIN`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/sin-form-sine-value-s7-1200-s7-1500),
[`COS`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/cos-form-cosine-value-s7-1200-s7-1500),
[`TAN`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/tan-form-tangent-value-s7-1200-s7-1500),
[`ASIN`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/asin-form-arcsine-value-s7-1200-s7-1500),
[`ACOS`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/acos-form-arccosine-value-s7-1200-s7-1500),
and [`ATAN`](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/math-functions-s7-1200-s7-1500/atan-form-arctangent-value-s7-1200-s7-1500)
establishes the common LAD contract: EN/ENO, floating-point `IN` and `OUT`, and
a selectable type field. SIN/COS/TAN inputs are radians; ASIN/ACOS/ATAN outputs
are radians. ASIN and ACOS accept only -1 through +1. Invalid or out-of-range
input drops ENO. LN documents negative input, rather than zero, as the domain
fault boundary and returns an invalid floating-point result for that case.

RungProof now gives all seven instructions the shared `???`, `Real` selector
instead of leaving them as untyped generic boxes. The compiler resolves or
validates matching REAL source and destination tags, rejects INT/DINT/Auto,
and preserves the selected type through program JSON and editor-project JSON.
LREAL remains deliberately unsupported. Normal results are rounded through the
modeled REAL width. Negative LN and out-of-range ASIN/ACOS write NaN and drop
ENO; LN(0) is accepted and stores negative infinity, preserving the explicit
Siemens negative-input boundary rather than extending it without evidence.
All seven reject invalid floating-point input at runtime.

The `numeric-scientific-tia` review fixture selects any of the seven mnemonics,
executes a deterministic radian case, optionally produces the documented LN,
ASIN, or ACOS domain fault, and asserts the common selector geometry. Normal
Vulkan captures `.tools/tia-ln-online.png`, `.tools/tia-sin-online.png`,
`.tools/tia-cos-online.png`, `.tools/tia-tan-online.png`,
`.tools/tia-asin-online.png`, `.tools/tia-acos-online.png`, and
`.tools/tia-atan-online.png` were visually inspected. The shared offline and
selected states were inspected at `.tools/tia-sin-offline.png` and
`.tools/tia-sin-selected-online.png`. Domain captures
`.tools/tia-ln-domain-fault.png`, `.tools/tia-asin-domain-fault.png`, and
`.tools/tia-acos-domain-fault.png` visibly retain energized EN, display NaN at
OUT, and drop ENO/downstream power flow.

The local SDK build passed with 0 warnings and 0 errors. The rebuilt suite
passed 353/353 and explicitly checks radians, range boundaries, type choice
order, type mismatch rejection, persistence, LN zero, and invalid-result
semantics. `--verify-ladder-editor` exited 0 with the full native interaction
and command-route suite passing. Its existing challenge starter-hash warning
and Godot shutdown resource-leak diagnostics are unrelated to this slice.

Rows `numeric-10` through `numeric-16` advance from OPEN to **PARTIAL**. Exact
installed-TIA scale and same-state offline/selected/online comparisons, direct
dropdown interaction, LREAL, invalid-input comparisons for every mnemonic, and
TAN near-singularity behavior remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens TRUNC input/output type correction

Official STEP 7 V21 LAD
[`TRUNC` documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/trunc-truncate-numerical-value-s7-1200-s7-1500-s7-1200-g2)
defines an asymmetric conversion box: `IN` is floating-point, `OUT` may be an
integer or floating-point type, and the operation discards the fraction toward
zero. Direct inspection of the official LAD example corrects the earlier
single-selector interpretation: the box visibly displays `REAL to DINT`, with
two type fields above EN/ENO and a lower IN/OUT row. ENO drops on execution
errors such as overflow. The broader Siemens family includes Real/LReal input
and integer-or-floating output widths beyond this simulator's type model.

RungProof previously executed TRUNC but exposed no TIA selector and allowed the
generic numeric model to imply that source and destination had the same type.
TRUNC now displays independent input/output controls: `???`/`Real` for IN and
`???`/`Int`/`DInt`/`Real` for OUT, joined by `to`. The compiler rejects a
non-REAL source, selected/destination
type disagreement, and Auto. Program JSON and editor-project JSON preserve the
choice. Runtime conversion occurs after REAL-width input rounding, truncates
toward zero, and range-checks the selected output. Integer overflow drops ENO
and preserves the simulator destination; invalid REAL input to REAL output
writes NaN. Unsupported Siemens widths remain explicit omissions.

The focused review routes prove -7.9 to INT produces -7 and 40000.9 to INT
produces an overflow diagnostic with EN true, ENO false, and the prior output
retained. Normal Vulkan captures `.tools/tia-trunc-offline.png`,
`.tools/tia-trunc-selected-online.png`, `.tools/tia-trunc-online.png`, and
`.tools/tia-trunc-overflow.png` were visually inspected. The selected fixture
prints `DUAL_TYPE_SELECTORS_PASS input=Real output=Int` and
`DUAL_TYPE_INTERACTION_PASS fixture=numeric-trunc-tia` after asserting that
both native controls stay within the instruction bounds without overlap.

The local SDK build passed with 0 warnings and 0 errors. The rebuilt suite
passed 354/354, covering selector order, REAL input checking, each supported
output type boundary, persistence, negative truncation, integer overflow, and
invalid REAL propagation. The native editor verifier also exited 0; its known
challenge starter-hash warning and Godot shutdown resource-leak diagnostics
remain unrelated.

TRUNC advances from OPEN to **PARTIAL**. Same-state installed-TIA geometry,
selection, online and overflow comparisons, direct dropdown interaction, wider
Siemens types, and authoritative integer-fault OUT behavior remain open.
Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens CONVERT box and dual-type correction

Official STEP 7 V21 LAD
[`CONVERT` documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/convert-convert-value-s7-1200-s7-1500-s7-1200-g2)
and its official example were inspected directly. The catalog operation is
named CONVERT, but the LAD box header is `CONV`. Its two independent type
selectors are joined by `to`; the example uses `INT to DINT`. EN and ENO sit
on the conductor row while IN and OUT occupy a distinctly lower row. Siemens
documents a broader conversion family than this simulator models, including
bit strings, smaller and unsigned integers, LREAL, character, and BCD types.

RungProof now uses the `CONV` box mnemonic and the taller two-row layout. The
review fixture uses the official INT-to-DINT example, exercises both selector
controls, and restores their values before monitoring. A separate REAL 40000
to INT fixture proves the bounded simulator fault behavior: EN remains true,
ENO and downstream flow drop, the diagnostic is published, and the prior
destination value is retained. That retained value is a simulator contract;
it is not asserted as the authoritative Siemens integer-overflow OUT value.

Normal Vulkan captures `.tools/tia-convert-offline.png`,
`.tools/tia-convert-online.png`, `.tools/tia-convert-selected-online.png`, and
`.tools/tia-convert-overflow.png` were visually inspected. The mnemonic, type
controls, `to` separator, separated EN/ENO and IN/OUT rows, operands, online
values, and fault conductor state are legible without overlap.

The local SDK build passed with 0 warnings and 0 errors. The executable
virtual-controller/editor suite passed 355/355, including independent
input/output selection, type mismatch rejection, JSON persistence,
REAL-to-integer rounding, and overflow handling. It explicitly reported that
no real PLC transport was constructed or connection attempted. The native
`--verify-ladder-editor` route exited 0 with the complete interaction and
command-route checks passing. Its existing challenge starter-hash warning,
headless popup-position messages, and Godot shutdown leak diagnostics are
unrelated to this slice.

CONVERT advances from OPEN to **PARTIAL**. Same-state installed-TIA geometry,
selection, online and overflow comparisons, direct dropdown interaction, the
broader Siemens type family, and authoritative Siemens fault-result behavior
remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens ROUND, CEIL, FLOOR, and TRUNC example correction

Official STEP 7 V21 LAD documentation and example images were inspected for
[`ROUND`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/round-round-numerical-value-s7-1200-s7-1500-s7-1200-g2),
[`CEIL`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/ceil-generate-next-higher-integer-from-floating-point-number-s7-1200-s7-1500-s7-1200-g2),
[`FLOOR`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/floor-generate-next-lower-integer-from-floating-point-number-s7-1200-s7-1500-s7-1200-g2),
and [`TRUNC`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/conversion-operations-s7-1200-s7-1500-s7-1200-g2/trunc-truncate-numerical-value-s7-1200-s7-1500-s7-1200-g2).
All four examples use an asymmetric conversion box displaying `REAL to DINT`,
place EN/ENO on the conductor, and place IN/OUT on a lower row. ROUND documents
nearest-even handling at exact half-way values; the examples confirm
1.5 to 2 and -1.5 to -2. CEIL confirms 0.5 to 1 and -0.5 to 0; FLOOR confirms
0.5 to 0 and -0.5 to -1.

RungProof now renders the four-instruction family with a taller two-row body,
independent `???`/`Real` input and `???`/`Int`/`DInt`/`Real` output controls,
and a visible `to` separator. Compiler checks require modeled REAL input and a
destination matching the selected output. Runtime behavior uses REAL-width
input, nearest-even ROUND, mathematical ceiling/floor, range-checked output,
ENO faults, destination retention for integer overflow, and NaN for invalid
REAL output. CEIL integer zero is explicitly normalized so -0.5 cannot leak a
negative-zero representation. TRUNC was corrected to the same dual-field
layout while retaining legacy project files that stored only its output type.

Normal Vulkan captures `.tools/tia-round-offline.png`,
`.tools/tia-round-online.png`, `.tools/tia-round-selected-online.png`,
`.tools/tia-round-overflow.png`, `.tools/tia-ceil-online.png`, and
`.tools/tia-floor-online.png` were visually inspected. Regenerated TRUNC
captures `.tools/tia-trunc-offline.png`, `.tools/tia-trunc-online.png`,
`.tools/tia-trunc-selected-online.png`, and `.tools/tia-trunc-overflow.png`
were also inspected. Normal states show continuous energized flow; overflow
shows EN true with ENO and downstream flow false while retaining the prior
simulator destination.

The local SDK build passed with 0 warnings and 0 errors. The executable
virtual-controller/editor suite passed 355/355, including type choices,
mismatch rejection, persistence, legacy TRUNC compatibility, ROUND ties,
negative CEIL/FLOOR behavior, signed-zero normalization, overflow, and invalid
REAL results. It reported `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native `--verify-ladder-editor`
route exited 0 with the full interaction and command-route checks passing; its
known challenge starter-hash warning, headless popup-position messages, and
Godot shutdown leak diagnostics remain unrelated.

Rows `numeric-21`, `numeric-22`, and `numeric-23` advance from OPEN to
**PARTIAL**. Row `numeric-17` remains **PARTIAL** with its evidence corrected.
Same-state installed-TIA geometry, selection, monitoring and fault comparisons,
broader Siemens types, and authoritative integer-fault OUT behavior remain
open. Overall TIA parity remains **OPEN**.

### 2026-10-14 Siemens MOVE two-row correction

Official STEP 7 V21
[`MOVE` documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/move-operations-s7-1200-s7-1500-s7-1200-g2/move-move-value-s7-1200-s7-1500-s7-1200-g2)
and the official LAD example were inspected directly. The example has MOVE with
EN/ENO on the conductor and IN/OUT1 on a distinct lower row; it does not show a
type selector. Siemens documents ENO false when EN is false or IN and OUT1 have
incompatible types. The initial box has one output and can be expanded with
OUT2 and later outputs, which all receive IN.

This inspection contradicted RungProof's prior compact single-row rendering.
MOVE now uses the same 132×128 two-row body as the reviewed unary conversion
family, with operand hit targets and output editing aligned to the lower row.
No type control was added because the official example has none. The modeled
IEC-checked numeric subset continues to require matching INT, DINT, or REAL
types; broader types, disabled IEC checking, and additional outputs remain
explicitly unsupported.

Normal Vulkan captures `.tools/tia-move-offline-v21.png`,
`.tools/tia-move-selected-v21.png`, `.tools/tia-move-online-v21.png`, and
`.tools/tia-move-disabled-v21.png` were visually inspected. The focused route
asserts 132×128 geometry, hidden type selectors, enabled DINT copy 42, disabled
destination retention at 7, and EN/ENO flow corresponding to the incoming RLO.
The native editor verifier reports `movePropertyFields=True`, proving source A
and destination are available while unused source B/C fields remain disabled.

The local SDK build passed with 0 warnings and 0 errors. The executable suite
passed 355/355 and reported no real PLC transport construction or connection
attempt. The native `--verify-ladder-editor` route exited 0 with all interaction
and command-route checks passing; its known starter-hash warning, headless
popup-position messages, and Godot shutdown leak diagnostics remain unrelated.

Row `numeric-0` remains **PARTIAL**, but its visual, selection, edit, and online
cells now contain direct RungProof evidence. Same-state installed-TIA comparison,
the official example's separately authored downstream BOOL coil, extended
outputs, broader types, and physical field interaction remain open. Overall TIA
parity remains **OPEN**.

### 2026-10-15 skipped IEC counter-family unknown-state visual gate

The shared executable gate now covers CTU and CTD as well as timers. CTU first
executes a rising CU input and captures CV=1, Q=FALSE, and R=FALSE. CTD first
executes LD=TRUE and captures the loaded CV=3 with Q=FALSE. The following scan
takes a JMP over each counter network, so neither counter appears in the
current observation capture.

`TIA_COUNTER_UNKNOWN_STATE_PASS` requires the skipped contact and counter
terminal input/Q output to remain nullable unknown, while the presentation is
`EarlierCycle=true` and `CurrentExecution=false`. It checks the exact retained
CV/Q values and CTD's consumed LD state. Both fixtures retain zero conductor
clearance at the 126x128 counter body.

Fresh 100% captures `.tools/tia-ctu-unknown-earlier-cycle-v1.png` and
`.tools/tia-ctd-unknown-earlier-cycle-v1.png` were visually inspected. Their
entire skipped networks are gray solid, with reduced-saturation values aligned
to distinct CU/CD, R/LD, PV, Q, and CV rows. The current JMP and visited LABEL
destination networks remain green. No black state is used because these are
interconnected networks that were not executed in the current scan.

This closes only the local CTU/CTD unknown-state gate. Numeric-box
skipped-state captures were still **OPEN** at this milestone and are closed by
the later numeric-family section; exact LABEL marker status,
same-state installed-TIA comparison, and explicit disconnected-output topology.
Overall TIA parity remains **OPEN**.

Final local regression after the timer/counter fixture changes passed both SDK
builds with zero warnings and zero errors, all 360 virtual-controller tests,
and the native `--verify-editor-monitoring` route. All six 100% capture routes
were then rerun against the final source and emitted their timer/counter
unknown-state pass marker plus zero terminal-wire clearance. No real PLC
transport was constructed or connection attempted. The unchanged challenge
starter-hash warning and Godot shutdown RID/ObjectDB leak diagnostics remain
known harness/runtime diagnostics rather than evidence of TIA parity.

### 2026-10-15 skipped numeric-comparator unknown-state visual gate

The executable review gate now covers every supported native numeric
comparison operator: `==`, `<>`, `>`, `>=`, `<`, and `<=`. Each fixture first
executes a true DInt comparison with operator-specific operands, then takes a
JMP over that comparison network in the following scan. The destination LABEL
network still executes, separating current control flow from the skipped
network's earlier observation.

`TIA_COMPARE_UNKNOWN_STATE_PASS` requires the earlier capture to contain the
requested true result, the current capture to omit the comparator, and the JMP
to report `Taken`. At the canvas boundary, both comparator input/output flow
and terminal-coil flow must be nullable unknown. The presentation must be
`EarlierCycle=true`, `CurrentExecution=false`, and retain the exact DInt A/B
identities, values, and true local result from the identity-matched prior scan.

Fresh 100% captures `.tools/tia-compare-{equal,not-equal,greater,
greater-or-equal,less,less-or-equal}-unknown-earlier-cycle-v1.png` were
visually inspected. All six show gray solid rails, comparator glyph/body, and
terminal coil; reduced-saturation operand/result values stay aligned above,
below, and after the comparator. The current JMP and visited LABEL destination
remain green. No clipping or overlap was observed.

This closes only the local unknown/not-executed gate for `compare-0` through
`compare-5`. Same-state installed-TIA comparison, wider Siemens operand types,
and unknown-state gates for numeric boxes were still **OPEN** at this milestone
and are closed by the later numeric-family section. Exact LABEL
marker status is also still unverified. Overall TIA parity remains **OPEN**.

Post-change local regression passed both SDK builds, all 360 virtual-controller
tests, and `--verify-editor-monitoring`. The test process again reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`.

### 2026-10-15 skipped JMP and RET unknown-state visual gate

Official STEP 7 V21 defines JMP and RET as conditional coil-form program-
control instructions. JMP transfers to a unique LABEL in the same block when
its RLO is true. RET conditionally exits the current block and supplies the
selected return value to the caller ENO. LABEL identifies the destination
network; it is not another RLO coil.

The new executable fixture first evaluates a false JMP and false RET, recording
`NotTaken` for both. On the following scan, a leading true JMP skips both review
networks and resumes at a later LABEL network. This construction preserves a
valid current execution path while proving that the reviewed JMP and RET are
absent from the current observation capture rather than merely false.

`TIA_PROGRAM_CONTROL_UNKNOWN_STATE_PASS` requires the current gate JMP to be
`Taken`, both reviewed instructions to be absent, and the LABEL destination
coil to be observed. At the canvas boundary, the skipped contact and terminal
flow must be nullable unknown. The retained presentation must report
`EarlierCycle=true`, `CurrentExecution=false`, earlier RLO `FALSE`, earlier
outcome `NotTaken`, and the deterministic half-saturation contact value.

Fresh 100% captures `.tools/tia-jmp-unknown-earlier-cycle-v1.png` and
`.tools/tia-ret-unknown-earlier-cycle-v1.png` were visually inspected. The
skipped conductors and JMP/RET coil symbols are gray solid; the earlier FALSE
values are reduced in saturation without overlap. RET's static RLO selector
and yellow edit affordance remain visible, while the current LABEL destination
network is green. No black state is used because the skipped networks remain
interconnected.

This closes only the local unknown/not-executed gate for JMP and RET. Exact
LABEL marker status color, same-state installed-TIA comparison, numeric boxes,
and explicit disconnected-output topology remain **OPEN**. Overall TIA
parity remains **OPEN**.

Post-change local regression passed both SDK builds, all 360 virtual-controller
tests, and `--verify-editor-monitoring`. The test process reported no real PLC
transport construction or connection attempt.

### 2026-10-15 skipped CALL unknown-state and instruction-time pin-value gate

The CALL evidence is grounded in Siemens V21's LAD block-call insertion,
instance-DB, and call-environment documentation: [inserting block calls by
drag and drop](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-lad-elements/insert-block-calls-in-lad/inserting-block-calls-using-a-drag-and-drop-operation?contentId=DLDMKzyVsYR4GEuDUwdqBw),
[instance data blocks](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/blocks-in-the-user-program/instance-data-blocks?contentId=qlit1RUsR5c7B9PaQ90WPg),
and [call environment](https://docs.tia.siemens.cloud/r/en-us/v21/testing-the-user-program/testing-with-program-status/setting-the-call-environment/basics-of-the-call-environment).
These sources support the named block/instance and selected call-path basis;
they do not establish exact RungProof colors, dimensions, or a claim of live
TIA/PLC equivalence.

CALL pin monitoring is now explicitly phase-qualified. Input displays the
value sampled by `CopyIn`; Output displays the value transferred by `CopyOut`;
InOut input displays `AliasResolved`; and InOut output displays the new
instruction-time `AliasFinal` transfer recorded when that exact call finishes.
The renderer must not read the end-of-scan global image for a call output. That
would make a later call retroactively rewrite an earlier call's visible result.
The refreshed two-call fixture proves Call A Shared 0→3 and Call B Shared 3→10,
while the final global value is 10. Disabled calls still produce zero
invocations and no fabricated pin-value badges.

The unknown-state fixture first completes `call-a` using `instance-a`, literal
Input 3, Output `result_a`, and InOut `shared_total`. On the next scan a leading
JMP skips the call and resumes at a later LABEL network. The current capture
contains no call invocation. `TIA_CALL_UNKNOWN_STATE_PASS` requires the exact
earlier instance and all four transfer phases (`CopyIn`, `CopyOut`,
`AliasResolved`, and `AliasFinal=3`), current nullable contact/terminal flow,
retained 3/3 and Shared 0→3 values, `EarlierCycle=true`, and
`CurrentExecution=false`.

Fresh 100% capture `.tools/tia-call-unknown-earlier-cycle-v1.png` was visually
inspected. The skipped call network is gray solid, its earlier values remain
visible at deterministic 50% saturation, the named `Station counter` body and
instance DB are unclipped, and the current JMP/LABEL path is green. Refreshed
`.tools/tia-call-online-enabled.png` and
`.tools/tia-call-online-disabled.png` were also inspected: monitored labels do
not overlap, Call A retains Shared output 3 rather than the later final 10, and
disabled calls remain blue dashed without monitored-value badges.

Both SDK builds completed with zero warnings and zero errors, all 360 offline
controller/editor tests passed, and `--verify-editor-monitoring` reported
`EDITOR_MONITORING_VERIFY PASS`. The test process reported no real PLC
transport construction or connection attempt. The unchanged challenge
starter-hash warning and Godot shutdown leak diagnostics remain known harness
diagnostics.

This closes the local CALL unknown/not-executed and instruction-time pin-value
gates only. Same-state installed-TIA comparison, exact Siemens metrics/palette,
single/multi/parameter-instance option parity, physical drag interaction,
exact LABEL marker status and explicit black disconnected topology remain
**OPEN**. The following numeric-family section closes the numeric-box
unknown-state captures. Overall TIA parity remains **OPEN**.

### 2026-10-15 native numeric-family unknown-state visual gate

The native Siemens-targeted numeric catalog is `numeric-0` through
`numeric-23`: MOVE; ADD/SUB/MUL/DIV/MOD; ABS/NEG/SQRT/EXPT; LN/SIN/COS/TAN/
ASIN/ACOS/ATAN; TRUNC/NORM_X/SCALE_X/CONVERT/ROUND/CEIL/FLOOR. The evidence
matrix links each mnemonic to its instruction-specific Siemens source.
`numeric-24` FORMAT_TEXT remains explicitly local and is not included in a
Siemens parity claim. The status-color basis remains Siemens V21's official
[program-status display for LAD](https://docs.tia.siemens.cloud/r/en-us/v21/testing-the-user-program/testing-with-program-status/examples-of-program-status-display/program-status-display-for-lad-programs?contentId=RmvHgF8sy0iBGUXHK9gmZQ): gray means unknown or not executed; it is not the
black not-interconnected state.

One shared route now wraps each existing normal-state numeric fixture with a
leading JMP and later LABEL. Scan one executes the exact instruction, types,
operands, and destination already used by its normal acceptance gate. Scan two
takes the JMP. `TIA_NUMERIC_UNKNOWN_STATE_PASS` then requires the instruction
to be absent from the current capture, the JMP to report `Taken`, and the LABEL
destination to execute. At the production canvas boundary it requires nullable
unknown flow for the contact, EN, ENO, terminal, and rails. The retained view
must preserve every applicable operand identity/type/value, exact destination
before/stored-after transition, `EarlierCycle=true`,
`CurrentExecution=false`, and deterministic half-saturation values.

The first ADD capture exposed a real current-versus-retained bug: contact and
outer rails were gray, but short EN/ENO conductors were green because
`NumericInstructionEnableFlow` read the retained presentation without checking
`CurrentExecution`. The production renderer now returns unknown for both pins
unless the presentation belongs to current execution. The standard native
monitoring verifier includes `TIA_NUMERIC_EARLIER_CYCLE_FLOW_VERIFY` so this
separation is not dependent on screenshot review. The matrix also exposed an
invalid NORM_X review fixture: C# conditional-expression promotion had turned
DINT initial values into doubles. Explicit object branches now preserve the
declared DINT values before compilation.

All 24 routes passed individually, covering one, two, and three input layouts,
single and dual type selectors, integer and REAL values, and all seven
scientific mnemonics. Fresh 100% normal-renderer captures
`.tools/tia-{move,add,sub,mul,div,mod,abs,neg,sqrt,expt,ln,sin,cos,tan,asin,acos,atan,trunc,norm,scale,convert,round,ceil,floor}-unknown-earlier-cycle-v1.png`
were each visually inspected. Every reviewed numeric network is gray solid from
rail through EN and ENO, earlier operand/result badges remain legible without
overlap, and the current JMP/LABEL path is green.

This closes the local unknown/not-executed gate for `numeric-0` through
`numeric-23`. It does not close same-state installed-TIA comparison, exact
Siemens palette/text metrics, omitted Siemens types or expandable operands,
exact LABEL marker status, or explicit black disconnected topology. Overall
TIA parity remains **OPEN**.

### 2026-10-14 Siemens binary-arithmetic geometry and ADD edit-state correction

Official STEP 7 V21 [`ADD`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/add-add-s7-1200-s7-1500-s7-1200-g2),
[`SUB`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/sub-subtract-s7-1200-s7-1500-s7-1200-g2),
[`MUL`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/mul-multiply-s7-1200-s7-1500-s7-1200-g2),
[`DIV`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/div-divide-s7-1200-s7-1500-s7-1200-g2),
and [`MOD`](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/mod-return-remainder-of-division-s7-1200-s7-1500-s7-1200-g2)
pages and LAD examples were inspected directly. All five examples place EN/ENO
on the conductor and align OUT with the lower IN2 row. ADD additionally
documents optional IN3 through INn; RungProof continues to support exactly two
inputs. At this geometry milestone, DIV-by-zero was still an unresolved runtime
gate: the V21 target-inclusive page and an older S7-1200-only manual conflicted,
so no runtime change was made then. The later
"DIV zero-divisor V21 correction and exact-state evidence" section supersedes
that hold after resolving the target/version evidence and adding exact tests.

The prior RungProof ADD body was 132 px tall and contained a large blank band
between the type field and conductor. ADD now uses the shared 132×96 two-input
body. ADD, SUB, MUL, DIV, and MOD now all align OUT with IN2, matching their
official examples. RungProof still limits the arithmetic family to its modeled
INT/DINT/REAL choices plus operation-appropriate Auto behavior; it does not
claim Siemens' wider types, expandable inputs, or unverified fault-result
semantics.

Normal Vulkan offline, selected, online, and disabled captures for ADD, SUB,
MUL, DIV, and MOD (`.tools/tia-{add,sub,mul,div,mod}-*-v21.png`) were visually
inspected. The focused fixture asserts 132×96 geometry for all five operations,
the supported selector order and bounds, enabled typed results, and disabled
states that retain the initial result while ENO/downstream flow is false. The
native editor verifier reports `arithmeticPropertyFields=True/True/True/True/True`
and `arithmeticPropertyEdits=True/True/True/True/True`, proving ADD, SUB, MUL,
DIV, and MOD each expose source A/B and destination while source C remains
disabled, and that contextual source edits apply with stable instruction identity
and exact Undo/Redo restoration. MOVE separately reports `movePropertyEdit=True`
for the same transaction guarantees with source B/C disabled.

The local SDK build passed with 0 warnings and 0 errors. The executable
virtual-controller/editor suite passed 356/356 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native `--verify-ladder-editor`
route exited 0 with all interaction and command routes passing; its known
challenge starter-hash warning, headless popup-position messages, and Godot
shutdown leak diagnostics remain unrelated.

Rows `numeric-1` through `numeric-5` remain **PARTIAL** overall. ADD now has
concrete visual, selection, edit, and online evidence replacing its old edit
OPEN cell. Same-state installed-TIA captures, physical mouse/keyboard field
interaction, input expansion, wider types, overflow/remainder fault comparisons,
and resolution of the conflicting DIV-by-zero documentation remain open. Overall
TIA parity remains **OPEN**.

### 2026-10-14 contact and edge property transaction audit

The native ladder-editor verifier now performs real contextual-property
transactions for ordinary and edge contacts instead of merely asserting that a
popup is visible. It opens an NO contact, changes its type to NC and selects a
different BOOL operand, then checks the applied model, stable instruction ID,
Undo, and Redo. It next reopens that actual NC state, changes it back to NO and
the original operand, and repeats the same identity and history checks.
`TIA_CONTACT_PROPERTY_EDIT_VERIFY PASS no=True nc=True` is the executable
evidence for both directions.

The verifier also creates a deliberately unused BOOL memory tag and edits the
separate edge-memory operand for both `—|P|—` and `—|N|—`. Each transaction
asserts that the signal operand and instruction ID remain stable while the
memory operand applies, undoes exactly, and redoes exactly. This avoids a false
pass through a duplicate storage alias. The result is
`TIA_EDGE_PROPERTY_EDIT_VERIFY PASS rising=True falling=True`.

That N-contact transaction exposed a functional defect in the shared property
apply path: selector index 3 encoded both `EdgeMode=Falling` and
`NormallyClosed=true`. The model rejected the combination with `Edge contacts
cannot also be normally closed.` The apply mapping now sets NormallyClosed only
for the standalone NC choice (index 1), matching the existing insertion path
and keeping P/N as distinct edge instructions.

The local SDK build passed with 0 warnings and 0 errors. The native
`--verify-ladder-editor` route now reports both property-edit checks plus
`LADDER_EDITOR_INTERACTION_VERIFY PASS` and `EDITOR_COMMAND_ROUTES_VERIFY PASS`.
The executable virtual-controller/editor suite passed 356/356 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`.
The known challenge starter-hash warning, headless popup-position diagnostics,
and Godot shutdown leak messages remain unrelated.

The NO and NC edit cells advance from **OPEN** to **PARTIAL**. P and N remain
**PARTIAL**, now with direct apply/Undo/Redo evidence rather than form-presence
only. Same-state installed-TIA property views, manual pointer/keyboard
interaction, and P/N selected-state captures remain open. Overall TIA parity
remains **OPEN**.

### 2026-10-14 IEC timer selected/edit-state expansion

The native ladder-editor verifier's former timer check submitted one TON PT
literal and one TONR R operand. It did not prove TOF or TP editing, instance-DB
changes, stable identity, draft isolation, or Undo/Redo. The replacement shared
transaction covers TON, TOF, TP, and TONR separately. For each timer it opens
the actual contextual form, verifies the timer instance/PT/single-instance-DB
controls, stages a different valid Siemens TIME literal and DB number/name,
confirms the model remains unchanged before Apply, then proves the applied
values, unchanged timer operand and terminal ID, exact Undo, and exact Redo.
TONR additionally edits and verifies its R operand while the other three prove
that R is hidden and unchanged.

The executable evidence is:

- `TIA_TIMER_PROPERTY_EDIT_DETAIL kind=OnDelay ... apply=True undo=True redo=True`
- `TIA_TIMER_PROPERTY_EDIT_DETAIL kind=OffDelay ... apply=True undo=True redo=True`
- `TIA_TIMER_PROPERTY_EDIT_DETAIL kind=Pulse ... apply=True undo=True redo=True`
- `TIA_TIMER_PROPERTY_EDIT_DETAIL kind=RetentiveOnDelay ... apply=True undo=True redo=True`
- `TIA_TIMER_PROPERTY_EDIT_VERIFY PASS ton=True tof=True tp=True tonr=True`

Fresh normal-Vulkan offline captures `.tools/tia-ton-selected-offline.png`,
`.tools/tia-tof-selected-offline.png`, `.tools/tia-tp-selected-offline.png`, and
`.tools/tia-tonr-selected-offline.png` were visually inspected at 100%. Each
selected outline contains the complete body and operands without clipping, DB
identity remains above the instruction, and the input/output conductors meet the
body. TON/TOF/TP retain their lower PT/ET row. TONR retains separate R and PT
rows, and the focused route again asserts its 158 px body height. These are
RungProof-side captures; there are no matched installed-TIA selected-state
captures.

The local SDK build passed with 0 warnings and 0 errors. The native
`--verify-ladder-editor` route reports the four timer transactions plus
`LADDER_EDITOR_INTERACTION_VERIFY PASS` and `EDITOR_COMMAND_ROUTES_VERIFY PASS`.
The executable virtual-controller/editor suite passed 356/356 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`.
Known challenge-content, headless popup-position, and Godot shutdown diagnostics
remain unrelated.

TON's selected and edit cells advance from **OPEN** to **PARTIAL**. TOF, TP, and
TONR offline, selected, and edit cells advance from **OPEN** to **PARTIAL**.
Matched installed-TIA selected/property views, manual OS-level interaction,
several monitor transitions, other zooms, and unsupported LTIME behavior remain
open. RT stays separate as a coil-form reset instruction and its edit cell
remains OPEN. Overall TIA parity remains **OPEN**.

### 2026-10-14 CTU/CTD selected and property transaction audit

The native editor verifier now opens actual CTU and CTD contextual forms and
exercises the controls that distinguish their Siemens shapes. Both transactions
stage a new PV, one of the nine supported TIA-ordered counter types, and a new
single-instance DB number/name. CTU additionally changes its R operand; CTD
changes its LD operand. Before Apply the model must remain unchanged. After
Apply the verifier requires the new values with the original counter instance
and terminal ID, then proves exact Undo and Redo.

The executable evidence is `TIA_COUNTER_PROPERTY_EDIT_VERIFY PASS ctu=True
ctd=True`, with each detail line reporting `opened=True draft=True apply=True
undo=True redo=True`.

Fresh normal-Vulkan captures `.tools/tia-ctu-selected-offline.png` and
`.tools/tia-ctd-selected-offline.png` were visually inspected at 100%. CTU's
focused route again asserts the supplied-reference 126×128 body. Both selected
outlines contain DB/type and all input/output rows without clipping, and both
conductors meet the instruction body. CTD remains a single box with an LD pin,
not a separate LOAD instruction. These remain RungProof-side captures without
matched installed-TIA selected states.

The local SDK build passed with 0 warnings and 0 errors. The native verifier
reports the two counter transactions plus `LADDER_EDITOR_INTERACTION_VERIFY
PASS` and `EDITOR_COMMAND_ROUTES_VERIFY PASS`.
The executable virtual-controller/editor suite passed 356/356 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`.

CTU's edit evidence advances from **OPEN** to **PARTIAL**, with its selection
evidence made explicit. CTD's selected and edit cells advance from **OPEN** to
**PARTIAL**. Matched installed-TIA property/selection captures, manual OS-level
interaction, exact CTD geometry comparison, and broader monitored transitions
remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 RT instance-selection transaction

RT remains a Siemens coil-form timer-reset instruction, not an IEC timer box.
The native verifier now builds two TIMER instances with distinct instance DBs
plus one RT targeting the first. Opening RT properties must expose only the
timer-instance selector: PT and R are hidden, and the DB number/name are visible
but read-only because they belong to the selected timer instance. Selecting the
second timer stays draft-only until Apply. Apply must change both the timer
operand and its reconciled DB while preserving terminal identity; Undo and Redo
must restore each exact operand/DB pair.

`TIA_RT_PROPERTY_EDIT_VERIFY PASS opened=True draft=True apply=True undo=True
redo=True` proves that transaction. The local SDK build passed with 0 warnings
and 0 errors, and the native route retained `LADDER_EDITOR_INTERACTION_VERIFY
PASS` and `EDITOR_COMMAND_ROUTES_VERIFY PASS`.

RT's edit cell advances from **OPEN** to **PARTIAL**. Matched installed-TIA form
evidence, manual OS-level interaction, and Siemens-permitted mid-line placement
remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 P/N selected-state fixture

The monitoring review harness now has an `edge-tia` fixture with distinct
positive- and negative-edge signals, explicit unique BOOL memory operands at
`%M0.0` and `%M0.1`, and separate output coils. This uses the actual compiled
edge instructions; it does not draw standalone mock symbols.

Normal-Vulkan captures `.tools/tia-edge-rising-selected-offline.png` and
`.tools/tia-edge-falling-selected-offline.png` were visually inspected at 100%.
Both preserve the P/N marker, signal operand above, edge-memory operand below,
and continuous conductor while selected. The selection rectangle includes the
editor's generous contact hit-area padding. There is no matched installed-TIA
selected-state reference, so that padding is not claimed as exact parity.

The local SDK build passed with 0 warnings and 0 errors. P and N selected cells
advance from **OPEN** to **PARTIAL**. Same-state installed-TIA selection remains
open. The executable virtual-controller/editor suite passed 356/356 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. Overall TIA parity remains **OPEN**.

### 2026-10-14 empty-box selected state and monitor classification

Normal-Vulkan capture `.tools/tia-empty-box-selected-offline.png` was visually
inspected at 100%. The selected border encloses the unresolved `???` chooser,
dropdown marker and IN/OUT pins while preserving the fixture-asserted 80 px
straight conductor after the branch merge. The surrounding unresolved contact
operands remain visibly `???`; the fixture does not substitute valid runtime
operands to make the draft appear executable.

The placeholder's selected cell advances from **OPEN** to **PARTIAL** because no
matched installed-TIA selected screenshot exists. Its monitor cell is now
**N/A**, not OPEN: the empty box is an editor state that must be replaced before
valid compile/load. Online power-flow acceptance applies to the chosen
replacement instruction. Overall TIA parity remains **OPEN**.

### 2026-10-14 CALL enabled/disabled online-state audit

The parameterized CALL review fixture now treats rung enable as an explicit
acceptance dimension instead of assuming that every capture enters both FB
instances. With enable TRUE, the fixture requires two completed invocations,
each containing CopyIn, CopyOut and AliasResolved transfer records. It also
requires the distinct instance results 3 and 7 plus the shared InOut result 10.
With enable FALSE, it requires zero invocations and verifies that both outputs
and the shared destination retain their initial zero values.

The executable evidence is:

- `TIA_PARAMETER_CALL_STATE_PASS enable=1 invocations=2 resultA=3 resultB=7 shared=10`
- `TIA_PARAMETER_CALL_STATE_PASS enable=0 invocations=0 resultA=0 resultB=0 shared=0`

Fresh normal-Vulkan captures `.tools/tia-call-online-enabled.png` and
`.tools/tia-call-online-disabled.png` were visually inspected at 100%. Both
show the two distinct FB instance DBs, literal Input values, Output and Shared
bindings, and ENO pins. The enabled image has TRUE contacts and continuous
green conductors through both calls. The disabled image has FALSE contacts and
blue-dashed conductors through both calls, with no parameter transfer or block
entry according to the runtime assertions.

The project's missing portable SDK was restored only under the ignored
`.tools/dotnet` directory using Microsoft .NET SDK 10.0.402, matching the
documented local-tool layout. The rebuilt project passed with 0 warnings and 0
errors. The native `--verify-ladder-editor` route retained
`LADDER_EDITOR_INTERACTION_VERIFY PASS` and `EDITOR_COMMAND_ROUTES_VERIFY PASS`.
The executable virtual-controller/editor suite passed 356/356 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. CALL's monitoring cell advances from
**OPEN** to **PARTIAL**. A matched installed-TIA scan-state capture, Siemens
single/multi/parameter-instance option parity, and physical drag interaction
remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 RET selectable return value and caller ENO audit

The official Siemens V21
[RET documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/program-control-operations-s7-1200-s7-1500-s7-1200-g2/ret-return-s7-1200-s7-1500-s7-1200-g2?contentId=lhCb6mPKhOdBnmUeoI1CLA)
states that RET executes only when the incoming RLO is 1. The selected return
value (`RLO`, `TRUE`, `FALSE`, or a BOOL operand) becomes the calling block's
ENO; an incoming RLO of 0 does not execute RET and the called block continues.
The official LAD image also shows the selected value above the coil, with the
documented small yellow dropdown triangle.

RungProof now stores and persists all four modes, validates the Value mode as
a declared BOOL operand, maps parameterized local operands through the call
frame, and returns the selected value through both legacy and parameterized
call frames. The Siemens property form exposes the four choices and shows the
operand field only for Value. The RET coil displays the current selection and
yellow triangle above the symbol. Instruction help now states the TIA operand
contract and keeps Logix RET operand-free in the supported subset.

Native property acceptance prints `TIA_RET_PROPERTY_EDIT_VERIFY PASS
opened=True draft=True apply=True undo=True redo=True`. This check found that
the exact in-memory editor snapshot clone initially omitted the two new RET
fields even though program/project round trips preserved them. The clone now
preserves both fields, and a focused transaction regression test proves Apply,
stable terminal identity, Undo, and Redo.

The `return-value-tia` review fixture compiles a main block that calls a second
block whose energized RET selects `FALSE`. It requires a completed invocation,
true incoming RET RLO, `Taken` control outcome, false call `EnableOutput`, and a
false caller-side CALL result. This exposed a second defect: legacy CALL
observations did not retain their lightweight frame ID, so the rendered ENO
still reflected incoming RLO. All CALL observations now retain that frame ID,
and CALL output-flow presentation uses the completed call result.

The fixture prints `TIA_RET_RETURN_VALUE_STATE_PASS incomingRlo=1
selectedValue=FALSE callerEno=0`. Fresh normal-Vulkan captures were visually
inspected at 100%:

- `.tools/tia-ret-false-value-selected-offline.png` shows selected RET with
  `FALSE`, dropdown triangle, and continuous conductors.
- `.tools/tia-ret-false-value-online.png` shows true green incoming RLO at RET
  while the selected return value remains `FALSE`.
- `.tools/tia-ret-false-caller-eno-online.png` shows green EN into the called
  block and blue-dashed false ENO leaving it.

The local build passed with 0 warnings and 0 errors. The virtual-controller and
editor suite passed 359/359 and reported `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE`
and `REAL_PLC_CONNECTION_ATTEMPTED FALSE`. Both native routes passed:
`LADDER_EDITOR_INTERACTION_VERIFY PASS`, `EDITOR_COMMAND_ROUTES_VERIFY PASS`,
and `EDITOR_MONITORING_VERIFY PASS`. Known challenge-content, headless popup
position, and Godot shutdown diagnostics remain unrelated.

RET's selected, edit, and caller-ENO monitoring evidence advances to
**PARTIAL**. It does not pass because matched installed-TIA selected/property
and monitored-state captures are still missing, and RungProof's task-entry
behavior is not evidence of complete Siemens OB behavior. Overall TIA parity
remains **OPEN**.

### 2026-10-14 LABEL network-metadata and destination-action audit

The official Siemens V21
[LABEL documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/program-control-operations-s7-1200-s7-1500-s7-1200-g2/label-jump-label-s7-1200-s7-1500-s7-1200-g2?contentId=8cBMdACqyMR4XJkgqaLcZA)
states that LABEL identifies the destination network where execution resumes.
LABEL and the jumping instruction must be in the same block, names are unique
within the block, and only one label is allowed per network. The official
example jumps to labeled Network 3 and then evaluates that network's contact
and output. This contradicts RungProof's former shared-schema assumption that
LABEL itself was the network's mutually exclusive terminal action.

RungProof now models LABEL as network metadata with its own stable ID and name.
The compiler counts only the coil/timer/counter/numeric/call/return/jump as the
single terminal action, while still accepting old label-only networks as
readable no-ops. The runtime publishes the label's visited state independently
of the action and executes the destination network's normal action. Import,
program JSON, editor-project JSON, history snapshots, command copies, and the
cross-reference index preserve the label alongside the terminal action.

The TIA editor now attaches LABEL without clearing the existing output. The
marker has its own hit target and selection state, so editing its name no
longer selects or rewrites the coil. Native acceptance prints
`TIA_LABEL_PROPERTY_EDIT_DETAIL opened=True staged=True edited=True`; it also
checks that the coil variable and terminal ID are unchanged and that the built
network contains both `LabelInstruction` and `Coil`. The invalid-name check
remains atomic.

The `program-control-tia` fixture now places `finish` on a network containing
the `rlo_true` contact and `label_destination_output` coil. Its taken JMP must
land on that network and energize the coil. Three normal-Vulkan captures were
visually inspected at 100%:

- `.tools/tia-label-coil-offline.png` shows the marker plus ordinary contact,
  coil, operand, and continuous conductors in one network.
- `.tools/tia-label-coil-selected.png` outlines only the marker while leaving
  the network action visible and unselected.
- `.tools/tia-label-coil-online.png` shows the visited destination logic and
  output as a continuous green path.

The local build passed with 0 warnings and 0 errors. The executable
virtual-controller/editor suite passed 359/359 and reported
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. Native checks reported
`LADDER_EDITOR_INTERACTION_VERIFY PASS`, `EDITOR_COMMAND_ROUTES_VERIFY PASS`,
and `EDITOR_MONITORING_VERIFY PASS`. Known challenge-content and Godot shutdown
diagnostics remain unrelated.

LABEL's monitoring cell advances from **OPEN** to **PARTIAL**, and the
same-network action-composition defect is closed. It does not pass because no
matched installed-TIA offline/selected/property/online captures exist, exact
pixel scale is unverified, and the label marker's monitoring color is still a
RungProof presentation choice. Overall TIA parity remains **OPEN**.

### 2026-10-14 Demo 5 PLC-owned sequence implementation audit

This section supersedes the earlier current-source statements that Demo 5 still
uses `gantry_cycle` and a plant-owned phase enum. Those statements remain above
as failure history explaining why this architectural repair was required.

The authored `Palletizing_Cell_Main` now contains:

- learner-facing System Enable, Start, Stop, machine Reset, simulated training
  trip, and pallet-permissive inputs;
- a sealed `machine_run` permission that drops on Stop, Reset, trip, permissive
  loss, diagnosed fault, or completed layer and requires a fresh Start;
- visible `sequence_step` transitions 0, 10, 20, 30, 40, 50, 60, 70, 80, and
  100, driven by measured pickup/place, attachment, encoder-window, placed-count,
  and home feedback;
- three calls to parameterized `FB_AxisDrive`, with `X_Axis_DB` DB101,
  `Y_Axis_DB` DB102, and `Z_Axis_DB` DB103;
- one call to parameterized `FB_VacuumGripper` with `Vacuum_Gripper_DB` DB104;
- six independent PLC outputs for X/Y/Z positive and negative motion, plus
  vacuum, plant-reset, active-status, and layer-complete outputs.

`PalletizerPlantModel` now has no `Phase` enum and no whole-cycle input. It
clamps/integrates the six direction commands, models attachment/release at the
measured pickup/place windows, stages the device-local next carton, reports
encoder/position/process feedback, and rejects opposed commands. The ±5 mm
process window matches the authored encoder comparisons. Holding an attached
carton with all commands off is an explicit offline lesson simplification; it
does not model gravity, stored pneumatic energy, or a safety-rated stop.

Current local offline evidence:

- build: 0 warnings, 0 errors;
- virtual-controller/editor executable suite: 359 passed, 0 failed;
- saved Demo 5 `.rpproj.json` regenerates from the same authored document with
  deterministic formal identities;
- `--audit-palletizer`: `failures=0`, 2,457 ordinary 20 ms controller/plant
  samples; one Start caused four measured placements; every sequence state and
  all six axis directions were observed; four return edges reached the CTU;
  Stop, simulated trip, pallet-permissive loss, fresh restart, machine Reset,
  command conflict, vacuum-loss fault, bearing, attachment, and sampled geometry
  checks passed;
- the trace ended at step 100, placed count 4, X/Y/Z encoder counts 0, home true,
  attachment false, fault false, and all actuator commands off.
- the full actual-editor five-demo learner regression completed with
  `FIVE_DEMO_LEARNER_RESULT failures=0`; Demo 5 proved one-Start/four-placement
  operation, all sequence states and axis directions, Stop/trip/permissive
  restart behavior, Reset recovery, wrong-current vacuum-loss faulting, and
  invalid-current scan/pose freeze;
- the native installation inventory now mounts all six Demo 5 learner controls
  (ENABLE, START, STOP, RESET, SIM TRIP, and PALLET) on the operator cabinet.
  `--verify-control-installations` passed with `mounted=6 bindings=6`, six
  accepted scripted picks, and no missing bindings;
- `.tools/demo5-controls-v2.png` and `.tools/demo5-completed-v2.png` were
  captured through the normal Windows GL renderer and visually inspected at
  100%. The first shows all six labeled cabinet controls. The second shows the
  actual scanned completion state at step 100, scan 2457, placed 4/4, fault
  false, vacuum false, and a four-carton pallet beside the monitored Main
  block. These images are RungProof evidence only, not matched TIA evidence.

This closes the **source-confirmed hidden-sequence defect** and advances Demo 5
PLC ownership to **PARTIAL**. It does not establish live Siemens execution,
hardware motion/safety performance, collision physics, native visual acceptance
of each expanded Main/FB/instance-DB view, or exact matched TIA presentation.
The cabinet controls are training devices; the simulated trip is not a
safety-rated E-stop or safety function. Those gates remain open, as do the
reset/control gaps in Demos 1–4 and the broader gallery-wide ownership
inventory. Overall parity remains **OPEN**.

### 2026-10-14 project-tree and instance-DB workflow correction

This section supersedes the current-state parts of the earlier independent
project-tree audit. The earlier list remains as defect history.

The Siemens-style project tree now presents each OB once under `Program
blocks`; the internal scheduler task still exists, but its pseudo
`Organization blocks / tasks` navigation row is not shown in the Siemens tree.
Single selection of an instance-DB row remains non-navigating. Activation opens
a dedicated central document tab for the actual current-project definition:

- IEC timer/counter definitions show DB number/name, IEC storage type, instance
  symbol, owning block, and network;
- FB instances show the assigned instance, called FB, and its declared
  interface storage;
- the document is explicitly read-only, and directs the learner to the owning
  instruction properties for the supported single-instance assignment edit.

Returning to a LAD block retains the shared pending-form guard. The native
route uncovered and fixed a real visibility defect where the block tab header
returned but its ladder canvas remained hidden. The test now resolves the
existing draft through the same Discard action exposed to the operator, then
requires the full ladder document to be visible.

Current local offline evidence:

- build: 0 warnings, 0 errors;
- `--verify-ladder-editor`: `TiaOrganizationBlockListedOnce`,
  `TiaInstanceDbListedInsideProgramBlocks`,
  `TiaInstanceDbSelectionDoesNotOpen`,
  `TiaInstanceDbActivationOpensDefinition`, and
  `TiaInstanceDbReturnRestoresLadder` all pass; the complete interaction and
  command-route verifiers pass;
- virtual-controller/editor executable suite: 360 passed, 0 failed, with no
  real PLC transport construction or connection attempt;
- `--verify-editor-monitoring`: `EDITOR_MONITORING_VERIFY PASS`;
- `--verify-five-demo-learner`: `FIVE_DEMO_LEARNER_RESULT failures=0`;
- `.tools/tia-project-tree-ob-single.png` was inspected at 100% and shows one
  OB under Program blocks without the duplicate pseudo task hierarchy;
- `.tools/tia-instance-db-definition.png` was captured with the normal Windows
  renderer and inspected at 100%. It shows DB123 under Program blocks and a
  readable `Pallet_Count_DB [DB123]` IEC_COUNTER definition tab without LAD
  instruction controls leaking into the document.

This advances the supported OB and single-instance-DB navigation slice to
**PARTIAL**. It does not establish TIA DB declaration editing, global DB subtype
parity, multi-instance or parameter-instance storage, named persistent watch
tables, physical keyboard/mouse activation, matched installed-TIA geometry, a
TIA project round trip, or live PLC execution. Overall TIA parity remains
**OPEN**.

### 2026-10-14 named watch-table workflow correction

This section supersedes the named-watch-table limitation in the immediately
preceding project-tree section. RungProof editor-project schema 5 now stores an
ordered collection of named watch tables, each with its own ordered symbols,
plus the active table used for navigation. Schema 1–4 projects migrate their
flat `watchVariables` list into `Watch table 1`. Selecting another table is
navigation and does not mark otherwise unchanged project content dirty. The
schema-5 compatibility `watchVariables` field is anchored to the default table
rather than the active table, so navigation cannot alter serialized project
content. Table-name uniqueness is enforced case-insensitively by both the
document model and the JSON loader, matching the creation dialog.

The TIA workbench now provides `Add new watch table` under the project-tree
Watch tables folder. Single selection does not execute the command; activation
opens a unique-name dialog. Created tables can be opened independently from the
tree or bottom selector, populated with symbols, cleared, deleted when more
than one table exists, undone/redone through project history, saved, and
reopened. Tag rename and deletion update every table. Verify + Load flattens
the union of watched symbols only for the immutable offline runtime monitor;
table membership and ordering remain editor metadata.

Current local offline evidence:

- build: 0 warnings, 0 errors;
- rebuilt virtual-controller/editor suite: 360 passed, 0 failed, including
  schema-5 migration and independent named-table persistence/lifecycle;
- native `--verify-ladder-editor` routes pass blank/duplicate rejection,
  selection-versus-activation, create/open/persist, independent selection, and
  exact undo;
- `.tools/tia-named-watch-table.png` was captured at 1920×1181 with the normal
  Windows renderer and inspected at 100%. It shows `Watch table 1` and
  `Commissioning` as separate project-tree rows, the Commissioning selector,
  five-symbol status, table controls, and its first typed/bound symbol row.

The named-table RungProof workflow is **PARTIAL**, not a TIA parity pass.
Matched installed-TIA layout, physical keyboard/mouse activation, modify/force
value columns, live PLC monitoring, and TIA project interchange remain open.
Overall TIA parity remains **OPEN**.

### 2026-10-14 global-DB structure and declaration correction

Siemens STEP 7 V21 defines a data block as declaration storage rather than a
code block with networks. A global DB has a freely declared structure; an
instance DB receives its structure from the assigned FB. The documented DB
creation choices are Global DB, ARRAY DB, an eligible FB, a PLC data type, or a
system data type. See [Creating data blocks](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/creating-data-blocks),
[Basic principles for programming data blocks](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/basic-principles-for-programming-of-data-blocks),
and [Structure of the declaration table](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/structure-of-the-declaration-table-for-data-blocks).
Siemens documents per-tag retentivity for optimized-access global DBs; for
standard-access global DBs, the Retain setting applies to all tags together.
See [Setting retentivity in a global data block](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/setting-retentivity/setting-retentivity-in-a-global-data-block).

RungProof now treats its supported DB choice explicitly as `Global DB`. The
creation dialog exposes that single supported type and names ARRAY DB,
PLC/system-type DB, and manual instance-DB creation as unsupported instead of
implying them. Activating a global DB opens a central declaration document with
Name, Data type, Start value, Retain, and Comment columns rather than a LAD
canvas. Existing Project-inspector controls add and remove persisted scalar
declarations; their storage section is fixed to Static. The supported DB is
explicitly labeled an optimized-access offline subset so its per-member Retain
setting is not confused with standard-access DB-wide retentivity. A created
block's OB/FB/FC/DB type is immutable outside the creation dialog.

The model and compiler now reject a data block that contains a LAD network,
uses Input/Output/InOut/Temp interface sections, becomes the program entry, is
assigned as a task entry, or is targeted by CALL. UI target selectors exclude
DBs; CALL choices are limited to FC/FB. These guards prevent invalid imported
JSON from bypassing the workbench.

Current local offline evidence:

- build: 0 warnings, 0 errors;
- virtual-controller/editor suite: 360 passed, 0 failed, including Retain and
  Comment persistence plus model/compiler rejection of executable DB content;
- native `--verify-ladder-editor` command routes pass Global-DB creation type,
  activation, five-column declaration rendering, immutable block/static-section
  choices, executable-target exclusion, and LAD-network rejection;
- `.tools/tia-global-db-definition.png` was captured at 1920×1181 with the
  normal Windows renderer and inspected at 100%. It shows `Machine_Setpoints`
  selected under Program blocks, a separate optimized-access DB document, and
  BOOL/INT/REAL declaration rows with persisted Retain and Comment values and
  no ladder canvas.

This Global-DB editor subset is **PARTIAL**. Monitor value, Snapshot, HMI-access
columns, standard-access offsets and DB-wide Retain semantics, selectable access
mode and other optimized-access properties, ARRAY DB, PLC/system-type DB, manual
instance-DB creation, runtime symbolic DB-member addressing, and matched
installed-TIA geometry remain open.
Overall TIA parity remains **OPEN**.

### 2026-10-14 P/N explicit-memory startup and online-state correction

Siemens STEP 7 V21 defines P and N contacts as comparisons between the current
BOOL signal and the prior signal stored in the separately declared edge-memory
operand. A detected 0→1 P edge or 1→0 N edge is true for one program cycle;
all other comparisons are false. See [P: Scan operand for positive signal
edge](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/p-scan-operand-for-positive-signal-edge-s7-1200-s7-1500-s7-1200-g2)
and [N: Scan operand for negative signal
edge](https://docs.tia.siemens.cloud/r/en-us/v21/fbd-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/n-scan-operand-for-negative-signal-edge-s7-1200-s7-1500-s7-1200-g2).

The runtime already used that explicit memory for newly authored TIA P/N
contacts, but the shared instruction catalog incorrectly described every first
execution after Stop or Reset as baseline-only. That statement applies only to
the compatibility path for legacy projects lacking an edge-memory operand. The
catalog now distinguishes the TIA path from that legacy subset. Focused tests
also prove that initialized FALSE memory plus TRUE input produces an immediate P
pulse, initialized TRUE plus FALSE input produces an immediate N pulse, Stop
preserves the explicit memory value, and Reset restores its authored initial
value before the next comparison.

The normal Windows renderer produced and this review inspected at 100%:

- `.tools/tia-edge-baseline-online.png`: scan 1, both contacts false while the
  explicit memory operands update from their authored initial values;
- `.tools/tia-edge-pulse-online.png`: scan 2, P sees FALSE→TRUE and N sees
  TRUE→FALSE; both complete rungs and outputs are green/TRUE;
- `.tools/tia-edge-held-online.png`: scan 3, unchanged inputs produce no second
  pulse; both outgoing paths and outputs return to blue-dashed/FALSE.

Every capture is generated from the compiled fixture and prints
`TIA_EDGE_STATE_PASS` only after checking previous/current observation values,
one-cycle results, and output flow. Build remains 0 warnings/0 errors and the
controller/editor suite remains 360 passed, 0 failed, with no real PLC transport
constructed or connection attempted.

P/N monitor evidence advances from missing to **PARTIAL**. Matched installed-TIA
colors, CPU-start behavior for specific retentivity/configuration choices,
physical interaction, and live PLC execution remain open. Overall TIA parity
remains **OPEN**.

### 2026-10-14 CTD online transition-state evidence

Siemens STEP 7 V21 states that CTD decrements CV on each positive CD edge,
loads PV into CV while LD is high and ignores CD during that load, and sets Q
when CV is less than or equal to zero. See [CTD: Count down](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/counter-operations-s7-1200-s7-1500-s7-1200-g2/ctd-count-down-s7-1200-s7-1500-s7-1200-g2?contentId=oLiHDq7BX5tXv0mFOZV~pA).

The native monitoring-review route now accepts explicit CTD states rather than
capturing only the LD-high load scan. Each state is produced by actual compiled
virtual-controller scans and must pass `TIA_CTD_STATE_PASS` before capture:

- `.tools/tia-ctd-load-online.png`: CD and LD are both true, LD wins, CV is 3,
  and Q is false;
- `.tools/tia-ctd-count-online.png`: after a false re-arm scan, one positive CD
  edge decrements CV to 2 while Q remains false;
- `.tools/tia-ctd-held-online.png`: CD remains true for a further scan and CV
  remains 2, proving level-high does not count twice;
- `.tools/tia-ctd-zero-online.png`: three separated positive CD edges reach
  CV 0 and Q true, with the outgoing conductor green.

All four 1920x1181 captures were made with the normal Windows Vulkan renderer
and inspected at 100%. DB/type identity, CD/LD/PV and Q/CV labels and values are
legible; CV remains on its lower output row; Q does not collide with the rung;
and the fixture reports zero left/right terminal-wire clearance in every state.
Build passes with 0 warnings and 0 errors, and the virtual-controller/editor
suite remains 360 passed, 0 failed with no real PLC transport constructed or
connection attempted.

CTD online monitoring advances but remains **PARTIAL**. These are RungProof-side
captures, not same-state installed-TIA comparisons. Exact installed-TIA
geometry, other zooms, physical interaction, TIA project interchange, and live
PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 TOF, TP, and TONR online transition-state evidence

The timer-family review fixture previously mixed each timer call with a second
terminal RT network. That was useful for shared-state observation testing but
could not produce authoritative multi-scan timer-family renders because every
enabled scan immediately reset the same timer instance. The dedicated TOF, TP,
and TONR review fixtures now contain only the timer under review. The generic
shared timer/RT fixture remains unchanged.

The transition gates follow official STEP 7 behavior:

- [TOF V21](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/tof-generate-off-delay-s7-1200-s7-1500-s7-1200-g2):
  Q is set with IN and remains set during the delay after the falling edge,
  then resets when PT expires.
- [TP V21](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/tp-generate-pulse-s7-1200-s7-1500-s7-1200-g2?contentId=QQuAEAuwAXH2pfmK0Ra99A):
  a positive IN edge starts Q for PT, and further input activity does not alter
  Q while the pulse runs.
- [TONR V21](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/tonr-time-accumulator-s7-1200-s7-1500-s7-1200-g2?contentId=szHGwOPqVvrH2iLn7swscw):
  ET accumulates while IN is true, retains while IN is false, Q latches at PT,
  and R=true clears ET/Q while blocking time measurement until R is false.

The normal Windows Vulkan renderer produced eleven 1920x1181 captures, all
inspected at 100%:

- TOF: `.tools/tia-tof-input-online.png`, `tia-tof-falling-online.png`, and
  `tia-tof-expired-online.png` prove ET 0/10/100 ms and Q true/true/false. The
  falling state visibly has blue-dashed false input flow with green true Q
  output.
- TP: `.tools/tia-tp-pulse-online.png`, `tia-tp-held-online.png`, and
  `tia-tp-expired-online.png` prove ET 10/20/100 ms and Q true/true/false while
  IN remains true.
- TONR: `.tools/tia-tonr-accumulating-online.png`, `tia-tonr-paused-online.png`,
  `tia-tonr-reset-online.png`, `tia-tonr-released-online.png`, and
  `tia-tonr-done-online.png` prove accumulate, retain, R-dominant reset,
  release/restart, and done states.

Every capture requires `TIA_TIMER_STATE_PASS` with exact IN, R, ET, Q, timing,
and scan-state values plus zero terminal-wire clearance. TONR also reasserts its
158 px body height in every state. Build passes with 0 warnings and 0 errors;
the virtual-controller/editor suite remains 360 passed, 0 failed with no real
PLC transport constructed or connection attempted.

These three online-state cells advance but remain **PARTIAL**. The captures are
RungProof-side evidence, not matched installed-TIA states. Tag-information modes
within every transition state, other zooms, physical interaction, TIA project
interchange, LTIME where applicable, and live PLC execution remain open.
Overall TIA parity remains **OPEN**.

### 2026-10-14 comparator true/false online-state evidence

All six supported inline comparator operators now have paired true and false
DInt monitoring captures. Each review run supplies true incoming continuity,
executes the compiled comparator, and requires `TIA_COMPARE_STATE_PASS` after
checking the selected operator, operands, requested local result, and executed
network-enable state.

The normal Windows Vulkan renderer produced twelve 1920x1181 captures, all
inspected at 100%:

- equal: `.tools/tia-compare-eq-true-online.png` and
  `.tools/tia-compare-eq-false-online.png`;
- not equal: `.tools/tia-compare-ne-true-online.png` and
  `.tools/tia-compare-ne-false-online.png`;
- greater than: `.tools/tia-compare-gt-true-online.png` and
  `.tools/tia-compare-gt-false-online.png`;
- greater or equal: `.tools/tia-compare-ge-true-online.png` and
  `.tools/tia-compare-ge-false-online.png`;
- less than: `.tools/tia-compare-lt-true-online.png` and
  `.tools/tia-compare-lt-false-online.png`;
- less or equal: `.tools/tia-compare-le-true-online.png` and
  `.tools/tia-compare-le-false-online.png`.

Every true state shows green continuity through the comparator, output
conductor, coil, and right rail. Every false state remains green from the left
rail to the comparator, then changes to blue-dashed continuity through the
output conductor, coil, and right rail. The two operands, DInt selector, and
operator remain legible without clipping or overlap in all twelve states.

Build passes with 0 warnings and 0 errors. The virtual-controller/editor suite
passes 360/360 with `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native editor-monitoring verifier
also reports `EDITOR_MONITORING_VERIFY PASS`; its pre-existing disabled starter
challenge warning and Godot shutdown RID/object leak diagnostics remain outside
this comparator slice.

This evidence advances all six comparator monitoring cells but remains
**PARTIAL**. Same-state installed-TIA captures, invalid-REAL visual states,
other supported data types, zooms and tag-information modes, physical
interaction, TIA project interchange, and live PLC execution remain open.
Overall TIA parity remains **OPEN**.

### 2026-10-14 CTU online transition-state evidence

The CTU-with-reset review fixture now accepts explicit `reset`, `count`,
`held`, and `done` states. These states exercise the compiled virtual
controller rather than substituting display values. Each capture requires
`TIA_CTU_STATE_PASS` after checking CU, R, CV, Q, and the positive-edge history
used for that scan.

The normal Windows Vulkan renderer produced four 1600x900 captures, all
inspected at 100%:

- `.tools/tia-ctu-reset-online.png`: CU and R are true, R wins, CV is 0, and Q
  is false;
- `.tools/tia-ctu-count-online.png`: R is false and the first positive CU edge
  increments CV to 1;
- `.tools/tia-ctu-held-online.png`: CU remains true for a second scan and CV
  remains 1, proving level-high does not count twice;
- `.tools/tia-ctu-done-online.png`: three separated positive CU edges reach
  CV/PV 3 and Q becomes true, energizing the outgoing conductor.

All four captures retain the single-instance DB/type identity, separate
CU/R/PV and Q/CV pin rows, legible live values, a fixture-asserted 126x128 body,
and zero left/right terminal-wire clearance. The review network comment is
state-neutral so it does not claim that R is asserted in the count, held, or
done states.

CTU online monitoring advances but remains **PARTIAL**. These are RungProof-side
captures, not same-state installed-TIA comparisons. Exact installed-TIA
geometry, tag-information modes within each transition state, other zooms,
physical interaction, TIA project interchange, and live PLC execution remain
open. Overall TIA parity remains **OPEN**.

### 2026-10-14 TON online transition-state evidence

The dedicated TON flow fixture now accepts explicit `accumulating`, `held`,
`done`, and `released` states. This closes the RungProof-side state-evidence gap
left after TOF, TP, and TONR received equivalent transition gates. Every state
comes from compiled virtual-controller scans and requires
`TIA_TIMER_STATE_PASS` with exact IN, ET, Q, timing, and scan-state values.

The normal Windows Vulkan renderer produced four 1600x900 captures, all
inspected at 100%:

- `.tools/tia-ton-accumulating-online.png`: IN is true, ET is 10 ms, timing is
  active, and Q is false;
- `.tools/tia-ton-held-online.png`: IN remains true, ET advances to 20 ms, and
  Q remains false;
- `.tools/tia-ton-done-online.png`: ET reaches PT at 100 ms and Q becomes true,
  energizing the outgoing conductor;
- `.tools/tia-ton-released-online.png`: IN becomes false on scan 11, ET resets
  to 0, and Q/outgoing flow become false.

The IEC instance DB/type, IN/PT and Q/ET rows, TIME values, and conductor states
remain legible without overlap. Each state also asserts zero left/right
terminal-wire clearance. This behavior follows the official STEP 7 V21
[TON documentation](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/timer-operations-s7-1200-s7-1500-s7-1200-g2/ton-generate-on-delay-s7-1200-s7-1500-s7-1200-g2).

After the comparator, CTU, and TON review-state additions, the build passes with
0 warnings and 0 errors, the virtual-controller/editor suite passes 360/360,
and the native monitoring verifier reports `EDITOR_MONITORING_VERIFY PASS`.
The suite also reports `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The pre-existing disabled starter
challenge warning and Godot shutdown RID/object leak diagnostics remain outside
these evidence slices.

TON online monitoring advances but remains **PARTIAL**. These are RungProof-side
captures, not same-state installed-TIA comparisons. Tag-information modes
within every transition state, other zooms, physical interaction, TIA project
interchange, LTIME where applicable, and live PLC execution remain open.
Overall TIA parity remains **OPEN**.

### 2026-10-14 RT powered/unpowered reset-state evidence

The previous RT evidence showed the terminal coil in powered and unpowered
states but did not prove the associated timer-storage transition. A dedicated
two-network fixture now drives TON and RT from independent inputs while both
reference the same `%DB1 "IEC_Timer_0_DB"` instance. This preserves scan order:
TON executes first, then RT observes and conditionally resets that instance.

The normal Windows Vulkan renderer produced two 1600x900 captures, both
inspected at 100%:

- `.tools/tia-rt-unpowered-online.png`: TON has accumulated 10 ms, the RT input
  and coil path are blue-dashed, and the RT observation proves the timer state
  remains at 10 ms;
- `.tools/tia-rt-powered-online.png`: TON's instruction-time row shows ET 20 ms,
  while the later RT network is green. The RT observation records 20 ms before
  execution and 0 ms afterward; the final snapshot also has ET 0, Q false,
  timing false, and stored input false.

Each state requires `TIA_RT_STATE_PASS`, including the RT enable result,
before/after timer states, final snapshot, and changed-versus-skipped write
disposition. The instruction remains a coil-form `RT` with shared DB identity,
not a synthetic EN/ENO box. This follows the official STEP 7 V20
[RT documentation](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/timer-operations-s7-1200-s7-1500/rt-reset-timer-s7-1200-s7-1500?contentId=AKFPqRIboeN2pXJD3JZD~w).

RT monitoring advances but remains **PARTIAL**. Same-state installed-TIA
comparison, Siemens-supported mid-line placement, physical interaction, other
zooms and tag-information modes, TIA project interchange, TIME/LTIME scope, and
live PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 ADD/SUB/MUL/DIV/MOD enabled/disabled online-state evidence

The shared binary-arithmetic review fixture now proves both enabled and
disabled execution for every supported two-input operation. Its second source
tag was renamed from the ADD-specific `addend` to the operation-neutral
`operand_b`, preventing SUB, MUL, DIV, and MOD captures from implying the wrong
operand role.

The normal Windows Vulkan renderer produced ten 1600x900 captures, all
inspected at 100%:

- ADD: `.tools/tia-add-enabled-online.png` and
  `.tools/tia-add-disabled-online.png` prove INT 3 + 1 = 4;
- SUB: `.tools/tia-sub-enabled-online.png` and
  `.tools/tia-sub-disabled-online.png` prove INT 3 - 1 = 2;
- MUL: `.tools/tia-mul-enabled-online.png` and
  `.tools/tia-mul-disabled-online.png` prove INT 3 x 2 = 6;
- DIV: `.tools/tia-div-enabled-online.png` and
  `.tools/tia-div-disabled-online.png` prove the nonfaulting INT 8 / 2 = 4
  case;
- MOD: `.tools/tia-mod-enabled-online.png` and
  `.tools/tia-mod-disabled-online.png` prove INT 8 MOD 3 = 2.

Every capture requires `TIA_ARITHMETIC_STATE_PASS`, which checks the requested
operation, EN/network-enable state, stored result, ENO/local result, and absence
of diagnostics. The review fixture also reasserts the 132x96 instruction body.
Enabled captures show continuous green EN/ENO and output flow. Disabled
captures show blue-dashed flow, result 0, explicit `Not consumed` operand
values, and ENO false; the operands are not evaluated while EN is false.

After the RT and arithmetic state additions, the build passes with 0 warnings
and 0 errors, the virtual-controller/editor suite passes 360/360, and the
native monitoring verifier reports `EDITOR_MONITORING_VERIFY PASS`. The suite
also reports `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The pre-existing disabled starter
challenge warning and Godot shutdown RID/object leak diagnostics remain outside
these evidence slices.

ADD, SUB, MUL, and MOD monitoring advance but remain **PARTIAL**. Same-state
installed-TIA comparisons, other data types, overflow or CPU-specific fault
semantics, expanded inputs where Siemens supports them, physical interaction,
other zooms, TIA project interchange, and live PLC execution remain open. DIV
monitoring remains **OPEN**: the fresh captures establish only normal and
disabled behavior. The cited Siemens V21 DIV page and official S7-1200 system
manual conflict on divide-by-zero OUT/ENO behavior, and RungProof's current
preserve-and-fault behavior matches neither documented result. No runtime
change or parity claim is justified without version- and CPU-specific installed
TIA or hardware evidence. Overall TIA parity remains **OPEN**.

### 2026-10-14 ABS, NEG, and SQRT enabled/disabled/fault evidence

The unary numeric review fixtures now distinguish three states with explicit
execution gates: enabled in-range, disabled, and enabled fault/domain error.
The normal Windows Vulkan renderer produced nine 1600x900 captures, all
inspected at 100%:

- ABS: `.tools/tia-abs-enabled-online.png`,
  `.tools/tia-abs-disabled-online.png`, and
  `.tools/tia-abs-overflow-online.png`;
- NEG: `.tools/tia-neg-enabled-online.png`,
  `.tools/tia-neg-disabled-online.png`, and
  `.tools/tia-neg-overflow-online.png`;
- SQRT: `.tools/tia-sqrt-enabled-online.png`,
  `.tools/tia-sqrt-disabled-online.png`, and
  `.tools/tia-sqrt-domain-online.png`.

`TIA_UNARY_STATE_PASS` proves INT ABS(-8)=8 and NEG(-8)=8 with ENO true.
Their disabled states leave result 0, mark the input not consumed, and keep ENO
false without a diagnostic. ABS(-32768) and NEG(-32768) each report one
overflow diagnostic: EN remains powered, result remains 0 in this fixture, and
ENO/outgoing flow becomes blue-dashed. `TIA_SQRT_STATE_PASS` proves REAL
SQRT(25)=5, the equivalent disabled behavior, and SQRT(-1)=NaN with one domain
diagnostic and ENO false.

The fault-gate diagnosis also clarified the observation contract. A numeric
instruction's `LocalResult` retains incoming powered rung flow; it is not by
itself the displayed ENO. The renderer derives ENO by combining network enable
with the absence of an instruction diagnostic. The state gates now assert that
contract directly, and the temporary diagnostic probe was removed.

After these unary-state additions, the build passes with 0 warnings and 0
errors, the virtual-controller/editor suite passes 360/360, and the native
monitoring verifier reports `EDITOR_MONITORING_VERIFY PASS`. The suite reports
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The pre-existing disabled starter
challenge warning and Godot shutdown RID/object leak diagnostics remain outside
this evidence slice.

These monitoring cells advance but remain **PARTIAL**. Same-state installed-TIA
comparisons, Siemens data types not modeled by RungProof, CPU-specific overflow
details, other zooms and tag-information modes, physical interaction, TIA
project interchange, and live PLC execution remain open. Overall TIA parity
remains **OPEN**.

### 2026-10-14 EXPT and scientific unary exact-state evidence

EXPT and all seven supported scientific unary functions now have explicit
enabled and disabled execution gates. LN, ASIN, and ACOS additionally exercise
their documented domain faults. `TIA_EXPT_STATE_PASS` and
`TIA_SCIENTIFIC_STATE_PASS` check the exact requested operation, incoming EN,
stored result, diagnostic count, and derived ENO state instead of accepting any
finite output as sufficient evidence.

The normal Windows Vulkan renderer produced 23 current 1600x900 captures, all
inspected at 100%. The set contains EXPT offline, selected,
selector-interaction, and enabled/disabled/fault monitoring, plus
enabled/disabled monitoring for LN, SIN, COS, TAN, ASIN, ACOS, and ATAN. LN,
ASIN, and ACOS also have enabled domain-fault captures. Representative state
groups are:

- EXPT: `.tools/tia-expt-offline-v3.png`,
  `.tools/tia-expt-selected-v3.png`,
  `.tools/tia-expt-selected-int-interaction-v2.png`,
  `.tools/tia-expt-enabled-online.png`,
  `.tools/tia-expt-disabled-online.png`, and
  `.tools/tia-expt-domain-online.png`;
- LN: `.tools/tia-ln-enabled-online.png`,
  `.tools/tia-ln-disabled-online.png`, and `.tools/tia-ln-domain-online.png`;
- ASIN and ACOS: matching `-enabled-online`, `-disabled-online`, and
  `-domain-online` captures;
- SIN, COS, TAN, and ATAN: matching `-enabled-online` and `-disabled-online`
  captures.

The state gates prove these REAL-width results: EXPT 2 ** 8 = 256; LN(e) is
approximately 1; SIN(pi/2)=1; COS(pi)=-1; TAN(pi/4)=1; ASIN(1) is approximately
pi/2; ACOS(-1) is approximately pi; and ATAN(1) is approximately pi/4. Every
disabled state retains the fixture's prior result 7, marks its operands not
consumed, reports no diagnostic, and keeps ENO false. EXPT(-2, 0.5) reports one
diagnostic and retains 7; LN(-1), ASIN(2), and ACOS(2) write NaN with one
diagnostic. All four fault states retain powered EN and drop ENO/outgoing flow.

Visual inspection also found and corrected two EXPT-specific geometry defects
before accepting the captures. The shared 20 px numeric input pitch allowed
IN2's monitored value chip to cover the IN1 operand name, and the exponent
dropdown's 74 px theme minimum covered the `**` token when selected. EXPT now
uses a centralized 32 px input pitch for both rendering and hit testing, a
152x112 body, and a header sized for the fixed Real chip, visible 16 px `**`
gap, and full dropdown. `TIA_EXPT_GEOMETRY_PASS` and the selected-control bounds
checks enforce those invariants. The temporary diagnostic probe was removed.

The native interaction fixture selected `Int` and printed
`ARITHMETIC_TYPE_INTERACTION_PASS requested=Int`; the reviewed image preserves
the fixed `Real` chip, visible `**`, and selected `Int` dropdown without overlap.

The final source validation completed with zero build warnings and zero build
errors, all 360 offline tests passed, and the native monitoring verifier exited
zero with `PASS`. The test environment explicitly reported both PLC transport
and PLC connection unavailable, so these results are not commissioning or live
PLC evidence. The pre-existing invalid challenge-content warning and Godot
shutdown RID/ObjectDB leak messages remain visible but did not fail these gates.

These rows advance but remain **PARTIAL**. Exact same-state installed-TIA
geometry and monitoring comparisons, LREAL and other unsupported Siemens
types, TAN near-singularity behavior, other zooms and tag-information modes,
TIA project interchange, and live PLC execution remain open. Direct popup
interaction for every supported selector-bearing mnemonic was subsequently
closed by the 2026-10-15 normal-Vulkan gate documented above. Overall TIA
parity remains **OPEN**.

### 2026-10-14 conversion-family exact enabled, disabled, and overflow evidence

TRUNC, NORM_X, SCALE_X, CONV, ROUND, CEIL, and FLOOR now have explicit
enabled/disabled execution gates in the native monitoring review. The shared
disabled invariant requires incoming EN false, local execution false, zero
diagnostics, `SkippedDisabled` write disposition, and `ActionDisabled` for each
applicable source operand. Each fixture starts with a non-result destination so
the gate also proves that disabled execution retains rather than recalculates
the destination.

The exact state markers prove:

- `TIA_TRUNC_STATE_PASS`: -7.9 truncates to -7; disabled retains 7; INT
  overflow retains 12 and reports one diagnostic;
- `TIA_CONVERT_STATE_PASS`: INT 42 converts to DINT 42; disabled retains 7;
  REAL 40000 to INT retains 12 and reports one diagnostic;
- `TIA_ROUNDING_STATE_PASS`: ROUND 1.5 produces 2, CEIL -0.5 produces normalized
  integer zero, and FLOOR -0.5 produces -1; each disabled state retains 7;
- `TIA_NORMALIZE_STATE_PASS`: DINT MIN/VALUE/MAX 10/20/30 produces REAL 0.5,
  while disabled retains 7 without consuming any of the three inputs;
- `TIA_SCALE_STATE_PASS`: REAL VALUE 0.5 with DINT MIN/MAX 10/30 produces 20,
  while disabled retains 7 without consuming any of the three inputs.

All normal enabled states keep EN and ENO/outgoing flow green. Disabled states
show blue-dashed EN/ENO/outgoing flow and explicit `Not consumed` values.
TRUNC, CONV, and ROUND overflow states retain green incoming EN while ENO and
downstream flow drop blue-dashed. The normal Windows Vulkan renderer produced
17 current 1600x900 captures, all inspected at 100%:

- `.tools/tia-trunc-enabled-online-v2.png`,
  `.tools/tia-trunc-disabled-online.png`, and
  `.tools/tia-trunc-overflow-online-v2.png`;
- `.tools/tia-convert-enabled-online-v2.png`,
  `.tools/tia-convert-disabled-online.png`, and
  `.tools/tia-convert-overflow-online-v2.png`;
- `.tools/tia-round-enabled-online-v2.png`,
  `.tools/tia-round-disabled-online.png`, and
  `.tools/tia-round-overflow-online-v2.png`;
- enabled/disabled `-online` pairs for CEIL, FLOOR, NORM_X, and SCALE_X using
  the corresponding `-enabled-online-v2` and `-disabled-online` names.

The source build passed with zero warnings and zero errors. All 360 offline
controller/editor tests passed with real PLC transport construction and
connection attempts both false. The native `--verify-editor-monitoring` route
exited zero with `EDITOR_MONITORING_VERIFY PASS`. Its pre-existing invalid
challenge-content warning and Godot shutdown RID/ObjectDB leak diagnostics
remain visible but did not fail the verifier.

These rows remain **PARTIAL**. Same-state installed-TIA comparisons, exact
Siemens integer-fault destination values, rendered NORM_X/SCALE_X range and
invalid-input fault states, unsupported Siemens widths/types, matched CEIL and
FLOOR selection states, other zooms/tag-information modes, project interchange,
and live PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 MOVE exact enabled and disabled evidence

The MOVE monitoring gate now checks more than the final destination. Enabled
execution must sample the `source` operand with declared DINT metadata and value
42, classify the destination update as `WrittenChanged`, store 42, report zero
diagnostics, and keep EN/ENO true. Disabled execution must retain the prior
destination 7, classify the write as `SkippedDisabled`, record the source as
`ActionDisabled` with no sampled value, report zero diagnostics, and keep
EN/ENO false.

The first strengthened run failed deterministically because the observation
pipeline normalizes sampled numeric values to `double` while preserving the PLC
declaration independently as `DeclaredType=DInt`. A tagged boundary probe
confirmed value `42` as `System.Double`, declared DInt, `WrittenChanged`, EN
true, and local result true. The gate now asserts both parts of that established
observation contract; the temporary probe was removed.

Fresh normal-renderer captures `.tools/tia-move-enabled-online-v2.png` and
`.tools/tia-move-disabled-online-v2.png` were inspected at 100%. The enabled
image shows consumed `42 (DINT)`, stored 42, and continuous green flow. The
disabled image shows explicit `Not consumed`, retained 7, and blue-dashed
EN/ENO/downstream flow. Both print `TIA_MOVE_STATE_PASS`; the existing
`TIA_MOVE_GEOMETRY_PASS` still enforces the reviewed 132x128 two-row body.

MOVE remains **PARTIAL**. Same-state installed-TIA comparison, extended
`OUT2...` outputs, downstream in-network continuation, broader Siemens types,
disabled IEC checking, other zooms/tag-information modes, project interchange,
and live PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 assignment, Set, and Reset exact write-state evidence

The assignment and Set/Reset monitoring gates now assert the exact destination
identity, incoming RLO, local result, before/after BOOL values, write
disposition, and zero diagnostics instead of relying only on the final bit.
Assignment writes on both RLO states: false changes `assign_false_writes` from
TRUE to FALSE and true changes `assign_true_writes` from FALSE to TRUE, with
both classified as `WrittenChanged`. Set and Reset are conditional: false RLO
preserves the destination with `SkippedDisabled`, while true RLO performs the
required change with `WrittenChanged`.

Six fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-assignment-false-online-v2.png`;
- `.tools/tia-assignment-true-online-v2.png`;
- `.tools/tia-set-false-online-v2.png`;
- `.tools/tia-set-true-online-v2.png`;
- `.tools/tia-reset-false-online-v2.png`;
- `.tools/tia-reset-true-online-v2.png`.

The selected network is fully visible in each capture with no clipping or
overlap. Assignment passes false or true RLO through the terminal coil. Set and
Reset show the important separation between power flow and stored destination:
a false blue-dashed RLO can preserve a TRUE Set destination or a FALSE Reset
destination, while a true green RLO can write the same displayed BOOL value.
`TIA_ASSIGNMENT_COIL_STATE_PASS` and `TIA_SET_RESET_STATE_PASS` report the exact
before/after dispositions for every capture.

The final source build passed with zero warnings and zero errors. All 360
offline controller/editor tests passed with
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native
`--verify-editor-monitoring` route exited zero with
`EDITOR_MONITORING_VERIFY PASS`. Its pre-existing invalid challenge-content
warning and Godot shutdown RID/ObjectDB leak diagnostics remain visible but did
not fail the verifier.

These rows remain **PARTIAL**. RungProof currently supports these coils only as
terminal actions; Siemens inline/downstream continuation is unsupported.
Same-state installed-TIA selection, editing, and online-color comparison, other
zooms/tag-information modes, project interchange, and live PLC execution also
remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 JMP and LABEL exact transfer evidence

The `program-control-tia` gate now asserts exact JMP observations and the
effects on both sides of the transfer. A false-RLO JMP must report
`NotTaken`, `NotApplicable`, and zero diagnostics, then allow the following
coil to change FALSE to TRUE with `WrittenChanged`. A true-RLO JMP must report
`Taken`, `NotApplicable`, and zero diagnostics, leave the intervening coil
entirely unobserved, visit the block-local `finish` label, and execute that
network's coil from FALSE to TRUE with `WrittenChanged`. The destination label
element must also be energized.

Three fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-jmp-false-online-v2.png` shows the false blue-dashed JMP followed
  by the executed green network;
- `.tools/tia-jmp-true-online-v2.png` shows the taken green JMP, the skipped
  network in gray/unobserved state, and the visited destination below;
- `.tools/tia-label-coil-online-v2.png` shows the compact `finish` marker beside
  the left rail and the destination contact/coil as one continuous green path.

Every capture prints `TIA_PROGRAM_CONTROL_STATE_PASS` with the exact
dispositions. The label capture also prints `TIA_LABEL_MARKER_GEOMETRY_PASS`
for the reviewed 62x24 marker. The final source build passed with zero warnings
and zero errors. All 360 offline controller/editor tests passed with
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native
`--verify-editor-monitoring` route exited zero with
`EDITOR_MONITORING_VERIFY PASS`; its existing challenge-content warning and
Godot shutdown RID/ObjectDB leak diagnostics remain visible but did not fail
the verifier.

These rows remain **PARTIAL**: same-state installed-TIA selection, editing,
monitoring colors, and marker status remain unmatched; exact installed-TIA
scale, manual pointer/keyboard interaction, project interchange, and live PLC
execution also remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 NO and NC exact observation evidence

The `contact-tia` gate now asserts the complete four-state contact contract.
Every NO/NC observation must execute with powered incoming continuity, sample
the expected operand identity as declared BOOL with the exact TRUE/FALSE value,
produce the expected local result, use `NotApplicable` write disposition, and
report zero diagnostics. The downstream assignment coils are checked
separately: false results retain FALSE with `WrittenSameValue`, while true
results change FALSE to TRUE with `WrittenChanged`.

Four fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-contact-no-false-online-v2.png`;
- `.tools/tia-contact-no-true-online-v2.png`;
- `.tools/tia-contact-nc-false-online-v2.png`;
- `.tools/tia-contact-nc-true-online-v2.png`.

The reviewed NO and NC networks are fully visible without clipping. Operand and
destination values remain legible, the NC slash retains the reference-backed
bottom-left-to-top-right orientation, and green/blue-dashed conductors remain
continuous through the contact, coil, and right rail. Every capture prints the
exact `TIA_CONTACT_STATE_PASS` result.

The final source build passed with zero warnings and zero errors. All 360
offline controller/editor tests passed with
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native
`--verify-editor-monitoring` route exited zero with
`EDITOR_MONITORING_VERIFY PASS`. Its existing invalid challenge-content
warning and Godot shutdown RID/ObjectDB leak diagnostics remain visible but did
not fail the verifier.

These rows remain **PARTIAL**. The installed-TIA reference zoom is unknown and
there are no matched installed-TIA selected, property-popup, or monitoring
captures for these exact states. Manual pointer/keyboard interaction, other
tag-information modes and zooms, project interchange, and live PLC execution
also remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 P and N exact edge-memory evidence

The `edge-tia` gate now checks the complete explicit-memory contract for both P
and N contacts. Each observation must execute with powered incoming continuity,
sample the expected declared BOOL signal, expose exact previous/current values,
classify the edge-memory update as `StateUpdatedChanged` or
`StateUpdatedSameValue`, and report zero diagnostics. The fixture also checks
the actual `rising_memory` and `falling_memory` values after each scan plus the
downstream assignment-coil before/after values and write dispositions.

Three fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-edge-baseline-online-v2.png` shows P FALSE→FALSE and N memory
  priming FALSE→TRUE without a pulse;
- `.tools/tia-edge-pulse-online-v2.png` shows P FALSE→TRUE and N TRUE→FALSE,
  both producing one green output scan;
- `.tools/tia-edge-held-online-v2.png` shows held TRUE→TRUE and FALSE→FALSE
  states returning both outputs to blue-dashed FALSE.

Both networks are fully visible in every capture. Signal operands remain above
the contacts, separate edge-memory operands remain below, P/N markers and
values are legible, and conductors remain continuous through the contacts,
coils, and right rails. Every capture prints the expanded
`TIA_EDGE_STATE_PASS` with memory and output dispositions.

The final source build passed with zero warnings and zero errors. All 360
offline controller/editor tests passed with
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native
`--verify-editor-monitoring` route exited zero with
`EDITOR_MONITORING_VERIFY PASS`. Its existing invalid challenge-content
warning and Godot shutdown RID/ObjectDB leak diagnostics remain visible but did
not fail the verifier.

These rows remain **PARTIAL**. There are no matched installed-TIA selected,
editing, or online captures for these exact states, and CPU start/retentivity
configurations remain unverified. Manual interaction, other zoom and
tag-information modes, project interchange, and live PLC execution also remain
open. Overall TIA parity remains **OPEN**.

### 2026-10-14 TON, TOF, TP, and TONR exact timer-state evidence

The shared IEC timer gate now asserts the exact pre-execution and
post-execution `LadderTimerState`, timer destination identity, configured
`T#100ms` preset, live IN, `StateUpdatedChanged` disposition, and zero
diagnostics. TONR additionally requires the `reset` operand to be captured by
identity as a declared BOOL with its exact R value. These checks distinguish
the timer's state update from its Q output and from the rung's incoming power.

Fifteen fresh 1600x900 normal-renderer captures were inspected at 100%:

- TON: `.tools/tia-ton-accumulating-online-v2.png`,
  `tia-ton-held-online-v2.png`, `tia-ton-done-online-v2.png`, and
  `tia-ton-released-online-v2.png`;
- TOF: `.tools/tia-tof-input-online-v2.png`,
  `tia-tof-falling-online-v2.png`, and `tia-tof-expired-online-v2.png`;
- TP: `.tools/tia-tp-pulse-online-v2.png`, `tia-tp-held-online-v2.png`, and
  `tia-tp-expired-online-v2.png`;
- TONR: `.tools/tia-tonr-accumulating-online-v2.png`,
  `tia-tonr-paused-online-v2.png`, `tia-tonr-reset-online-v2.png`,
  `tia-tonr-released-online-v2.png`, and `tia-tonr-done-online-v2.png`.

All 15 captures show the requested IN/R/PT/Q/ET values without clipping or
overlap. The left and right conductor-clearance gate reports exactly zero in
every state. TONR also passes the 158 px body-height gate with separate R and
PT rows. The expanded `TIA_TIMER_STATE_PASS` output records exact prior ET,
current ET, IN, R, Q, timing, disposition, diagnostics, and scan number.

These rows remain **PARTIAL**. Same-state installed-TIA selected, editing, and
online comparisons are missing; transition-specific tag modes, other zooms,
LTIME and broader CPU-dependent behavior, a TP falling-IN-during-pulse state,
project interchange, and live PLC execution remain open or unsupported as
documented. Overall TIA parity remains **OPEN**.

Final validation for this slice succeeded: the C# build completed with zero
warnings and zero errors, all 360 virtual-controller tests passed, and the
native editor-monitoring verifier reported `EDITOR_MONITORING_VERIFY PASS`.
The test run confirms that no real PLC transport was constructed or attempted.
The pre-existing `CHALLENGE_CONTENT_DISABLED CH_INVALID_CHALLENGE` warning and
Godot shutdown RID/ObjectDB leak reports remain; neither is evidence of an
installed-TIA comparison or live-controller validation.

### 2026-10-14 CTU and CTD exact counter-state evidence

The CTU and CTD review gates now require exact pre-execution and post-execution
`LadderCounterState` values rather than checking only final CV and Q. Each gate
also asserts the counter destination identity, configured PV 3, current and
previous CU/CD edge states, write disposition, and zero diagnostics. CTU's R
and CTD's LD must be captured by identity as declared BOOL operands with their
exact live values.

Eight fresh 1600x900 normal-renderer captures were inspected at 100%:

- CTU: `.tools/tia-ctu-reset-online-v2.png`,
  `tia-ctu-count-online-v2.png`, `tia-ctu-held-online-v2.png`, and
  `tia-ctu-done-online-v2.png`;
- CTD: `.tools/tia-ctd-load-online-v2.png`,
  `tia-ctd-count-online-v2.png`, `tia-ctd-held-online-v2.png`, and
  `tia-ctd-zero-online-v2.png`.

CTU proves CV transitions 0→0 under R-dominant reset, 0→1 on a positive CU
edge, 1→1 while CU stays high, and 2→3 when Q becomes true. CTD proves 0→3
under LD-dominant load, 3→2 on a positive CD edge, 2→2 while CD stays high,
and 1→0 when Q becomes true. The two held-input cases require
`StateUpdatedSameValue`; all reset, load, and edge transitions require
`StateUpdatedChanged`.

All captures keep DB/type, CU/CD, R/LD, PV, Q, and CV legible without clipping
or overlap. Both instruction families pass zero left/right conductor clearance
and a 126x128 body gate. These are RungProof-side rendered and runtime checks;
they do not replace a same-state installed-TIA comparison.

The CTU and CTD rows remain **PARTIAL**. Matched installed-TIA selection,
editing, and online states, exact installed-TIA geometry, other zooms and
transition-specific tag-information modes, project interchange, and live PLC
execution remain open. Overall TIA parity remains **OPEN**.

Final validation for this counter slice succeeded: the C# build completed with
zero warnings and zero errors, all 360 virtual-controller tests passed, and the
native editor-monitoring verifier reported `EDITOR_MONITORING_VERIFY PASS`.
The test run confirms that no real PLC transport was constructed or attempted.
The pre-existing invalid challenge-content warning and Godot shutdown
RID/ObjectDB leak reports remain and do not broaden this offline evidence.

### 2026-10-14 MOVE and binary-arithmetic exact numeric-state evidence

The MOVE and ADD/SUB/MUL/DIV/MOD review gates now validate the complete
available numeric observation instead of only the final destination. Each gate
requires evaluated versus disabled state, EN/ENO and local result, exact source
identity/type/value, destination identity and before/calculated/after values,
write disposition, no rounding or clamping for these fixtures, and zero
diagnostics. Disabled execution must record each source as `ActionDisabled`
with no sampled value and retain the destination with `SkippedDisabled`.

Fresh normal-renderer MOVE captures `.tools/tia-move-enabled-online-v3.png`
and `.tools/tia-move-disabled-online-v3.png` were inspected at 100%. Enabled
DINT MOVE samples 42 and changes the destination from 7 to 42; disabled MOVE
does not consume its source and retains 7. The existing 132x128 MOVE body gate
continues to pass.

The first fresh arithmetic captures exposed a visual defect that the runtime
gate could not detect: with the old 20 px input-row pitch, IN2's monitored-value
chip painted over IN1's symbolic operand. The official Siemens V21 ADD example
keeps IN1 and IN2 as separate visible operand rows. RungProof now uses its
existing 32 px two-input monitored-row spacing for ADD, SUB, MUL, DIV, MOD, and
EXPT. The instruction body remains 132x96 for ADD through MOD, OUT remains on
the IN2 row, and `TIA_ARITHMETIC_GEOMETRY_PASS` now also reports the required
32 px pitch. Presentation tests cover every affected operation plus the
unchanged 20 px MOVE pitch.

Ten replacement 1600x900 captures were inspected at 100%:

- `.tools/tia-add-enabled-online-v3.png` and
  `.tools/tia-add-disabled-online-v3.png`;
- `.tools/tia-sub-enabled-online-v3.png` and
  `.tools/tia-sub-disabled-online-v3.png`;
- `.tools/tia-mul-enabled-online-v3.png` and
  `.tools/tia-mul-disabled-online-v3.png`;
- `.tools/tia-div-enabled-online-v3.png` and
  `.tools/tia-div-disabled-online-v3.png`;
- `.tools/tia-mod-enabled-online-v3.png` and
  `.tools/tia-mod-disabled-online-v3.png`.

All ten keep `value`, `operand_b`, both monitored-value chips, pin labels,
OUT/result, and conductors legible without overlap. Enabled exact results are
ADD 3+1=4, SUB 3-1=2, MUL 3x2=6, DIV 8/2=4, and MOD 8 MOD 3=2, all with
`WrittenChanged`, ENO true, and zero diagnostics. Disabled cases consume
neither input, retain 0 with `SkippedDisabled`, keep ENO/downstream flow false,
and report zero diagnostics.

Final offline validation passed: the C# build completed with zero warnings and
zero errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. The test process confirms
that no real PLC transport was constructed or attempted. The pre-existing
invalid challenge-content warning and Godot shutdown RID/ObjectDB leak reports
remain.

At this exact-state milestone, MOVE and ADD/SUB/MUL/MOD were **PARTIAL** and DIV
was still **OPEN** because the cited sources had not yet been resolved. The
later "DIV zero-divisor V21 correction and exact-state evidence" section
supersedes that status and advances DIV to **PARTIAL**.
Same-state installed-TIA monitoring, other zooms and tag-information modes,
unsupported Siemens types and extended inputs/outputs, project interchange,
and live PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 ABS, NEG, and SQRT exact numeric-state evidence

The ABS, NEG, and SQRT monitoring gates now validate the complete available
numeric observation rather than only the final result. Each gate requires the
exact evaluated/disabled state, EN/ENO and local result, source
identity/type/value, destination identity and before/calculated/after values,
write disposition, rounding/clamping flags, and diagnostic count and code.
Disabled execution must leave the source unconsumed with `ActionDisabled`,
retain the destination, and report `SkippedDisabled`.

This review exposed a functional monitoring contradiction in the SQRT domain
fault path. Runtime execution deliberately stored NaN for SQRT(-1), matching
the cited Siemens invalid-REAL behavior, but its observation said
`FailedNoWrite`. The observation model now distinguishes that case as
`FailedInvalidValueWritten`. The same disposition is regression-tested for
the existing trigonometric, TRUNC, rounding, and scientific invalid-REAL
write paths. Faulted invalid-value writes remain non-energizing, so ENO and
downstream flow stay false even though the invalid value is visible at OUT.

Nine fresh 1600x900 normal-renderer captures were inspected at 100%:

- ABS: `.tools/tia-abs-enabled-online-v2.png`,
  `.tools/tia-abs-disabled-online-v2.png`, and
  `.tools/tia-abs-overflow-online-v2.png`;
- NEG: `.tools/tia-neg-enabled-online-v2.png`,
  `.tools/tia-neg-disabled-online-v2.png`, and
  `.tools/tia-neg-overflow-online-v2.png`;
- SQRT: `.tools/tia-sqrt-enabled-online-v2.png`,
  `.tools/tia-sqrt-disabled-online-v2.png`, and
  `.tools/tia-sqrt-domain-online-v2.png`.

The enabled cases prove INT ABS(-8)=8, INT NEG(-8)=8, and REAL SQRT(25)=5,
each with `WrittenChanged`, ENO true, and zero diagnostics. Disabled cases do
not consume their inputs, retain 0 with `SkippedDisabled`, and keep ENO false.
INT ABS(-32768) and NEG(-32768) retain 0 with `FailedNoWrite` and exactly one
operation-specific overflow diagnostic. SQRT(-1) stores NaN with
`FailedInvalidValueWritten` and exactly one `VC_RUNTIME_SQRT_DOMAIN`
diagnostic. All three fault captures show EN powered while ENO and outgoing
flow are blue-dashed. Operands, monitored values, pins, result, and conductors
are legible without overlap in all nine captures.

Final offline validation passed: the C# build completed with zero warnings and
zero errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. The test process confirms
that no real PLC transport was constructed or attempted. The pre-existing
invalid challenge-content warning and Godot shutdown RID/ObjectDB leak reports
remain.

ABS, NEG, and SQRT remain **PARTIAL**. Same-state installed-TIA selection,
editing, and monitoring comparisons, exact installed-TIA geometry, other
zooms and tag-information modes, unsupported Siemens widths and CPU-specific
behavior, project interchange, and live PLC execution remain open. Overall
TIA parity remains **OPEN**.

### 2026-10-14 EXPT REAL-precision and exact-state evidence

The official Siemens V21 EXPT reference defines IN1 as floating point, IN2 as
integer or floating point, OUT as floating point, and ENO=0 when execution is
disabled or faults. RungProof's bounded subset fixes IN1 and OUT to REAL and
allows INT, DINT, or REAL at IN2.

The runtime audit found two precision leaks inside that supported subset. The
shared numeric conversion path treated the EXPT selector as if it described
all operands, so an INT exponent left the REAL base at .NET double precision.
The Math.Pow result also bypassed the normal REAL-width result conversion.
EXPT now always samples IN1 at single precision, converts IN2 to single
precision only when its selected type is REAL, and reduces the valid OUT value
to single precision before storage. Regression cases prove that REAL base
16777217 with INT exponent 1 produces 16777216 and that a precision-sensitive
REAL-exponent result exactly matches the single-precision result.

The monitoring gate now validates the complete available observation: exact
evaluated/disabled state, EN/ENO and local result, both operand
identities/types/values, destination identity and before/calculated/after
values, write disposition, rounding/clamping flags, and diagnostic count/code.

Three fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-expt-enabled-online-v2.png`;
- `.tools/tia-expt-disabled-online-v2.png`;
- `.tools/tia-expt-domain-online-v2.png`.

The enabled state proves REAL 2 ** REAL 8 writes 256 with `WrittenChanged`,
ENO true, and zero diagnostics. The disabled state consumes neither operand,
retains 7 with `SkippedDisabled`, and keeps ENO false. The invalid REAL domain
(-2) ** 0.5 consumes both operands, retains 7 with `FailedNoWrite`, emits one
`VC_RUNTIME_DOMAIN` diagnostic, and drops ENO while EN remains powered. Both
operand rows, monitored values, fixed REAL base chip, exponent selector,
visible `**`, pins, result, and conductors are legible without overlap.

Final offline validation passed: the C# build completed with zero warnings and
zero errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. The test process confirms
that no real PLC transport was constructed or attempted. The pre-existing
invalid challenge-content warning and Godot shutdown RID/ObjectDB leak reports
remain.

EXPT remains **PARTIAL**. Same-state installed-TIA selection, editing, and
monitoring comparisons, exact installed-TIA geometry, fault/overflow OUT
behavior, unsupported LREAL and wider integer types, other zooms and tag modes,
project interchange, and live PLC execution remain open. Overall TIA parity
remains **OPEN**.

### 2026-10-14 scientific unary exact-state and REAL-image evidence

The LN, SIN, COS, TAN, ASIN, ACOS, and ATAN review gate now validates the
complete available numeric observation. It requires exact evaluated/disabled
state, EN/ENO and local result, source identity/type/value, destination
identity and before/calculated/after values, write disposition,
rounding/clamping flags, and diagnostic count/code. Disabled execution must
record the source as `ActionDisabled`, retain 7 with `SkippedDisabled`, and
produce no diagnostic. LN, ASIN, and ACOS domain faults must write NaN with
`FailedInvalidValueWritten`, emit exactly one `VC_RUNTIME_DOMAIN` diagnostic,
and drop ENO while EN remains powered.

The first capture batch exposed a type-boundary and display contradiction.
REAL inputs were executed at single precision inside individual instructions,
but the controller's stored input image and observation retained the original
.NET double value. Online chips therefore showed a value different from the
one actually consumed and clipped the 17-digit text with ellipses. REAL initial
values also bypassed type normalization.

The controller now normalizes every sampled, initial, and written REAL value
to single precision at the PLC type boundary. Tests prove that sampled REAL
16777217 becomes 16777216 in the tag image, operand observation, and EXPT
result. TIA online chips format REAL values with nine significant digits,
which is sufficient to round-trip a single-precision value. Fixed-REAL numeric
outputs use the same formatter. The corrected LN evidence now displays the
actual consumed 2.71828175 and stored 0.99999994 completely rather than a
clipped double representation.

The instruction-help restrictions now match the verified execution contract:
LN zero writes negative infinity, while negative/invalid LN and out-of-range or
invalid ASIN/ACOS inputs write an invalid REAL value and publish
`VC_RUNTIME_DOMAIN`. The stale claim that these faults preserve the prior
destination was removed and is regression-tested.

Seventeen fresh 1600x900 normal-renderer captures were inspected at 100%:

- LN enabled, disabled, and domain: `.tools/tia-ln-*-online-v3.png`;
- SIN, COS, TAN, and ATAN enabled/disabled:
  `.tools/tia-{sin,cos,tan,atan}-*-online-v3.png`;
- ASIN and ACOS enabled, disabled, and domain:
  `.tools/tia-{asin,acos}-*-online-v3.png`.

The normal cases prove LN(REAL e)=0.99999994, SIN(REAL pi/2)=1,
COS(REAL pi)=-1, TAN(REAL pi/4)=1, ASIN(1)=REAL pi/2, ACOS(-1)=REAL pi,
and ATAN(1)=REAL pi/4, each with `WrittenChanged`, ENO true, and zero
diagnostics. All seven disabled cases leave the operand unconsumed and retain
7. LN(-1), ASIN(2), and ACOS(2) show NaN at OUT with ENO/downstream flow
blue-dashed. Every operand, REAL value chip, pin, tag, result, and conductor is
legible without clipping or overlap in the reviewed captures.

Final offline validation passed: the C# build completed with zero warnings and
zero errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. The test process confirms
that no real PLC transport was constructed or attempted. The pre-existing
invalid challenge-content warning and Godot shutdown RID/ObjectDB leak reports
remain.

All seven rows remain **PARTIAL**. Same-state installed-TIA selection, editing,
and monitoring comparisons, exact installed-TIA geometry, unsupported LREAL,
TAN near-singularity behavior, invalid-input coverage for functions without a
finite domain fixture, other zooms and tag modes, project interchange, and live
PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-14 TRUNC exact-state and REAL display refresh

The prior TRUNC captures exposed the same REAL-display contradiction found in
the scientific unary review: the controller executed a single-precision input,
but the old online chip showed a clipped binary-double string such as
`-7.900000000000...` or `40000.90000000...`. Three fresh 1600x900 captures were
generated with the normal Windows Vulkan renderer and inspected at 100%:

- `.tools/tia-trunc-enabled-online-v3.png`;
- `.tools/tia-trunc-disabled-online-v3.png`;
- `.tools/tia-trunc-overflow-online-v3.png`.

The enabled view now shows the actual sampled REAL value `-7.9000001 (REAL)`
and INT result -7 without clipping. The overflow view shows the actual sampled
REAL value `40000.8984 (REAL)`, retains INT 12, keeps EN green, and drops
ENO/downstream flow blue-dashed. The disabled view keeps the explicit
`Not consumed` source and retained INT 7.

`TIA_TRUNC_STATE_PASS` now requires exact evaluated/disabled state, sampled
source identity/type/value, destination identity and before/calculated/stored
values, write disposition, rounding/clamping flags, diagnostic count/code, and
EN/ENO. A controller regression also proves that sampled -7.9 is normalized to
the same single-precision value in the input image and consumed operand before
TRUNC writes -7. The C# build completed with zero warnings/errors and all 360
controller/editor tests passed without constructing or attempting a real PLC
transport.

The MOD documentation review found a CPU-profile boundary that must not be
papered over. The generic S7-1200/S7-1500 V21 page documents the integer pins
and successful execution but not the divide-by-zero OUT value. The official
S7-1200 V20 manual explicitly assigns zero to OUT and drops ENO when IN2 is
zero. The supplied project identifies an S7-1500, and no target-S7-1500
same-version evidence was found in this review. RungProof's current
preserve-and-fault behavior therefore remains explicitly non-parity and was not
changed from S7-1200-only evidence.

TRUNC remains **PARTIAL** pending same-state installed-TIA selection/edit/
monitoring comparison, exact target geometry, other supported output modes,
other zoom/tag modes, project interchange, and live PLC execution. Overall TIA
parity remains **OPEN**.

### 2026-10-14 CONVERT exact-state evidence refresh

The Siemens STEP 7 V21 CONVERT page confirms that the instruction reads IN,
converts according to the two selected data types, and writes the converted
value at OUT. It also explicitly sets ENO to zero when EN is zero or an
execution error such as overflow occurs. The page lists a substantially wider
type set than RungProof supports. RungProof's bounded subset remains INT,
DINT, and REAL; bit strings, smaller and unsigned integers, LREAL, CHAR,
WCHAR, and BCD types remain unsupported.

`TIA_CONVERT_STATE_PASS` now validates the complete available observation:
exact evaluated/disabled state, EN/ENO and local result, source
identity/type/value, destination identity and before/calculated/after values,
write disposition, rounding/clamping flags, and diagnostic count/code. The
controller regression likewise checks the successful literal transition and
the overflowing no-write transition rather than only checking the final tag
and message text.

Three fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-convert-enabled-online-v3.png`;
- `.tools/tia-convert-disabled-online-v3.png`;
- `.tools/tia-convert-overflow-online-v3.png`.

The enabled state proves sampled INT 42 writes DINT 42 with
`WrittenChanged`, ENO true, and zero diagnostics. The disabled state records
IN as `ActionDisabled`, retains DINT 7 with `SkippedDisabled`, and keeps ENO
false without a diagnostic. The overflow state consumes sampled REAL 40000,
retains INT 12 with `FailedNoWrite`, emits exactly one
`VC_RUNTIME_CONVERT_OVERFLOW` diagnostic, and drops ENO while EN remains
powered. `CONV`, both type fields, `to`, IN/OUT pins, exact values, tags, and
all conductor states are legible without clipping or overlap.

The C# build completed with zero warnings and zero errors, all 360
controller/editor tests passed, and the native monitoring verifier reported
`EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was constructed or
attempted. The pre-existing invalid challenge-content warning and Godot
shutdown RID/ObjectDB leak reports remain. The Siemens page specifies overflow
ENO behavior but does not state the integer-fault OUT value, so RungProof's
retained destination is not claimed as Siemens parity without installed-target
evidence.

CONVERT remains **PARTIAL** pending same-state installed-TIA selection,
editing, and monitoring comparisons; exact installed-TIA geometry; verified
integer-fault OUT behavior; unsupported documented types; other zooms and tag
modes; project interchange; and live PLC execution. Overall TIA parity remains
**OPEN**.

### 2026-10-14 NORM_X and SCALE_X exact-state and range-fault evidence

The Siemens STEP 7 V21 pages define the NORM_X and SCALE_X equations and their
three data inputs. Both explicitly set ENO to zero when EN is false or when
MIN is greater than or equal to MAX. NORM_X additionally names invalid
floating-point and NaN inputs; SCALE_X also names invalid floating-point
values and output overflow. Neither page specifies the value retained or
written at OUT for these fault cases, so RungProof's retained destination is
recorded as simulator behavior rather than claimed Siemens parity.

The shared scaling review gate now validates the complete available
observation: exact evaluated/disabled state, EN/ENO and local result, all three
operand identities/types/values, destination identity and
before/calculated/after values, write disposition, rounding/clamping flags,
and diagnostic count/code. Dedicated `numeric-norm-range-tia` and
`numeric-scale-range-tia` fixtures exercise the documented MIN >= MAX fault.
Controller regressions also prove the exact successful transitions and the
invalid-range `FailedNoWrite` transition.

Six fresh 1600x900 normal-renderer captures were inspected at 100%:

- NORM_X enabled, disabled, and invalid range:
  `.tools/tia-norm-{enabled,disabled,range}-online-v3.png`;
- SCALE_X enabled, disabled, and invalid range:
  `.tools/tia-scale-{enabled,disabled,range}-online-v3.png`.

NORM_X with DINT MIN/VALUE/MAX 10/20/30 writes REAL 0.5 with
`WrittenChanged`, ENO true, and zero diagnostics. SCALE_X with REAL VALUE 0.5
and DINT MIN/MAX 10/30 writes DINT 20 under the same successful state. Both
disabled cases consume none of their three inputs, retain 7 with
`SkippedDisabled`, and keep ENO false without a diagnostic. Both 30..10
invalid-range cases consume all inputs, retain 7 with `FailedNoWrite`, emit
exactly one `VC_RUNTIME_RANGE` diagnostic, and drop ENO while EN remains
powered. The six reviewed views keep the mnemonics, type fields, `to`, four
data pins, operands, results, tags, and conductor states legible without
overlap.

The C# build completed with zero warnings and zero errors, all 360
controller/editor tests passed, and the native monitoring verifier reported
`EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was constructed or
attempted. The pre-existing invalid challenge-content warning and Godot
shutdown RID/ObjectDB leak reports remain.

NORM_X and SCALE_X remain **PARTIAL** pending same-state installed-TIA
selection/editing/monitoring comparisons, exact installed-TIA geometry,
verified faulted OUT behavior, NaN/IEEE-754 and SCALE_X overflow visual states,
unsupported Siemens widths, other zooms and tag modes, project interchange,
and live PLC execution. Overall TIA parity remains **OPEN**.

### 2026-10-14 ROUND, CEIL, and FLOOR exact-state and overflow evidence

The Siemens STEP 7 V21 pages define ROUND as nearest-integer conversion with
exact half-way values resolved to the even integer, CEIL as the next higher
integer, and FLOOR as the next lower integer. All three set ENO to zero when
EN is false or an error such as overflow occurs. The pages do not specify the
integer value at OUT after an overflow, so RungProof's retained destination is
recorded as simulator behavior rather than claimed Siemens parity.

The shared rounding review gate now validates the complete available
observation: exact evaluated/disabled state, EN/ENO and local result, source
identity/type/value or disabled non-consumption, destination identity and
before/calculated/after values, write disposition, rounding/clamping flags,
and diagnostic count/code. Dedicated `numeric-round-overflow-tia`,
`numeric-ceil-overflow-tia`, and `numeric-floor-overflow-tia` fixtures now
exercise each instruction's DINT overflow path independently. Controller
regressions also prove the exact successful and overflowing transitions plus
invalid NaN-to-REAL write behavior for every instruction.

Nine fresh 1600x900 normal-renderer captures were inspected at 100%:

- ROUND enabled, disabled, and overflow:
  `.tools/tia-round-{enabled,disabled,overflow}-online-v3.png`;
- CEIL enabled, disabled, and overflow:
  `.tools/tia-ceil-{enabled,disabled,overflow}-online-v3.png`;
- FLOOR enabled, disabled, and overflow:
  `.tools/tia-floor-{enabled,disabled,overflow}-online-v3.png`.

ROUND with REAL 1.5 writes DINT 2, CEIL with REAL -0.5 writes normalized DINT
0, and FLOOR with REAL -0.5 writes DINT -1. Each successful case reports
`WrittenChanged`, ENO true, and zero diagnostics. Each disabled case does not
consume IN, retains DINT 7 with `SkippedDisabled`, and keeps ENO false without
a diagnostic. Each overflow case consumes REAL 3E+09, retains DINT 12 with
`FailedNoWrite`, emits exactly one mnemonic-specific
`VC_RUNTIME_{ROUND|CEIL|FLOOR}_OVERFLOW` diagnostic, and drops ENO/downstream
flow while EN remains powered. The nine reviewed views keep the mnemonic,
type fields, `to`, IN/OUT pins, exact values, tags, and conductor states
legible without overlap or clipped numeric values.

The C# build completed with zero warnings and zero errors, all 360
controller/editor tests passed, and the native monitoring verifier reported
`EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was constructed or
attempted. The pre-existing invalid challenge-content warning and Godot
shutdown RID/ObjectDB leak reports remain.

ROUND, CEIL, and FLOOR remain **PARTIAL** pending same-state installed-TIA
selection/editing/monitoring comparisons, exact installed-TIA geometry,
verified integer-fault OUT behavior, unsupported Siemens widths, other zooms
and tag modes, project interchange, and live PLC execution. Overall TIA
parity remains **OPEN**.

### 2026-10-14 ADD, SUB, and MUL exact overflow evidence

The Siemens STEP 7 V21 ADD and SUB LAD pages and the V21 MUL semantic page
explicitly set ENO to zero when EN is false, the result falls outside the
range of the data type selected at OUT, or a floating-point value is invalid.
ADD and MUL also document extendable `IN3...INn` inputs. RungProof continues
to support exactly two inputs, so input expansion remains an explicit
limitation. None of these pages specifies the OUT value after overflow;
RungProof's retained destination is simulator behavior, not a Siemens parity
claim.

Dedicated `numeric-add-overflow-tia`, `numeric-sub-overflow-tia`, and
`numeric-mul-overflow-tia` fixtures now exercise each instruction's INT
overflow path. The shared arithmetic gate validates exact evaluated/disabled
state, EN/ENO and local result, both operand identities/types/values or
disabled non-consumption, destination identity and before/calculated/after
values, write disposition, rounding/clamping flags, and diagnostic
count/code. Controller regressions independently assert those exact overflow
observations rather than checking only the final tag and message text.

Nine fresh 1600x900 normal-renderer captures were inspected at 100%:

- ADD enabled, disabled, and overflow:
  `.tools/tia-add-{enabled,disabled,overflow}-online-v3.png`;
- SUB enabled, disabled, and overflow:
  `.tools/tia-sub-{enabled,disabled,overflow}-online-v3.png`;
- MUL enabled, disabled, and overflow:
  `.tools/tia-mul-{enabled,disabled,overflow}-online-v3.png`.

The enabled states prove INT 3+1 writes 4, INT 3-1 writes 2, and INT 3×2
writes 6 with `WrittenChanged`, ENO true, and zero diagnostics. Each disabled
state consumes neither input, retains 0 with `SkippedDisabled`, and keeps ENO
false without a diagnostic. ADD 32767+1, SUB -32768-1, and MUL 32767×2 each
retain INT 12 with `FailedNoWrite`, emit exactly one mnemonic-specific
`VC_RUNTIME_{ADD|SUB|MUL}_OVERFLOW` diagnostic, and drop ENO/downstream flow
while EN remains powered. All views keep both symbolic operands, both value
chips, IN1/IN2/OUT pins, result, type field, and conductor states legible in
the reviewed 132×96 instruction body.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was
constructed or attempted. The pre-existing invalid challenge-content warning
and Godot shutdown RID/ObjectDB leak reports remain.

ADD, SUB, and MUL remain **PARTIAL** pending same-state installed-TIA
selection/editing/monitoring comparisons, exact installed-TIA geometry,
verified faulted OUT behavior, ADD/MUL input expansion, unsupported Siemens
types, other zooms and tag modes, project interchange, and live PLC
execution. Overall TIA parity remains **OPEN**.

### 2026-10-14 EXPT overflow correction and exact-state evidence

The Siemens STEP 7 V21 EXPT page explicitly states that ENO becomes zero when
EN is false or an execution error such as overflow occurs. It defines a
floating-point IN1 base, integer-or-floating IN2 exponent, and floating-point
OUT, but it does not specify the value at OUT after a fault. RungProof's
retained destination therefore remains simulator behavior rather than a
Siemens parity claim.

Current-source inspection found that valid-domain REAL overflow was cast to
positive infinity and then rejected by the generic nonfinite-result path. ENO
was false, but the diagnostic was `VC_RUNTIME_NUMERIC`, so the instruction did
not expose a precise EXPT overflow state. The runtime now detects a nonfinite
single-precision power result at the EXPT operation and emits
`VC_RUNTIME_EXPT_OVERFLOW` before any write.

A dedicated `numeric-expt-overflow-tia` fixture and strengthened
`TIA_EXPT_STATE_PASS` gate now distinguish successful, disabled, domain-fault,
and overflow states. The gate verifies exact evaluated/disabled state,
EN/ENO and local result, both operand identities/types/values or disabled
non-consumption, destination identity and before/calculated/after values,
write disposition, rounding/clamping flags, and diagnostic count/code. The
controller regression independently requires the exact overflow code and
complete no-write observation.

Four fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-expt-enabled-online-v3.png`;
- `.tools/tia-expt-disabled-online-v3.png`;
- `.tools/tia-expt-domain-online-v3.png`;
- `.tools/tia-expt-overflow-online-v3.png`.

REAL 2 ** 8 writes 256 with `WrittenChanged`, ENO true, and zero diagnostics.
EN false consumes neither operand, retains 7 with `SkippedDisabled`, and
keeps ENO false without a diagnostic. REAL (-2) ** 0.5 retains 7 with
`FailedNoWrite` and one `VC_RUNTIME_DOMAIN` diagnostic. REAL 10 ** 50 retains
7 with `FailedNoWrite` and one `VC_RUNTIME_EXPT_OVERFLOW` diagnostic. Both
faults keep EN powered while ENO and downstream flow are false. The four
views keep both type chips, `**`, IN1/IN2/OUT pins, complete operands, values,
result, and conductor states legible in the reviewed 152x112 body.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was
constructed or attempted. The pre-existing invalid challenge-content warning
and Godot shutdown RID/ObjectDB leak reports remain.

EXPT remains **PARTIAL** pending same-state installed-TIA
selection/editing/monitoring comparisons, exact installed-TIA geometry,
verified faulted OUT behavior, unsupported Siemens exponent and floating-point
types, other zooms and tag modes, project interchange, and live PLC execution.
Overall TIA parity remains **OPEN**.

### 2026-10-14 SCALE_X exact overflow evidence

The official Siemens STEP 7 V21 SCALE_X page defines
`OUT = [VALUE * (MAX - MIN)] + MIN` and explicitly states that ENO becomes zero
when overflow occurs. It allows an out-of-range VALUE when the computed result
is representable; it does not specify the value retained or written at OUT
after overflow. RungProof's retained destination therefore remains simulator
behavior rather than a Siemens parity claim.

A dedicated `numeric-scale-overflow-tia` fixture now exercises a valid
`MIN < MAX` range whose computed result exceeds DINT. The strengthened
`TIA_SCALE_STATE_PASS` gate distinguishes successful, disabled, invalid-range,
and overflow states. It checks exact evaluated/disabled state, EN/ENO and local
result, all three operand identities/types/values or disabled non-consumption,
destination before/calculated/after values, write disposition, flags, and exact
diagnostic count/code. The controller regression independently requires the
same complete no-write observation and `VC_RUNTIME_SCALE_X_OVERFLOW` code.

Four fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-scale-enabled-online-v3.png`;
- `.tools/tia-scale-disabled-online-v3.png`;
- `.tools/tia-scale-range-online-v3.png`;
- `.tools/tia-scale-overflow-online-v3.png`.

REAL 0.5 across DINT 10..30 writes 20 with `WrittenChanged`, ENO true, and
zero diagnostics. EN false consumes none of MIN/VALUE/MAX, retains 7 with
`SkippedDisabled`, and keeps ENO false without a diagnostic. DINT 30..10
retains 7 with `FailedNoWrite` and one `VC_RUNTIME_RANGE` diagnostic. REAL 2
across DINT 0..2000000000 computes beyond DINT range, retains 12 with
`FailedNoWrite`, and emits exactly one `VC_RUNTIME_SCALE_X_OVERFLOW`
diagnostic. Both faults keep EN powered while ENO/downstream flow are false.
All four views keep the dual type fields, MIN/VALUE/MAX/OUT pins, complete
operands, values, retained result, and conductor states legible without
overlap or clipping.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was
constructed or attempted. The pre-existing invalid challenge-content warning
and Godot shutdown RID/ObjectDB leak reports remain.

SCALE_X remains **PARTIAL** pending same-state installed-TIA
selection/editing/monitoring comparisons, exact installed-TIA geometry,
verified faulted OUT behavior, unsupported Siemens widths, other zooms and tag
modes, project interchange, and live PLC execution. Overall TIA parity remains
**OPEN**.

### 2026-10-14 NORM_X and SCALE_X invalid IEEE-754 evidence

The official Siemens STEP 7 V21 NORM_X and SCALE_X pages state that ENO becomes
zero when a specified floating-point value is outside the normalized IEEE-754
range or VALUE is NaN. Neither page specifies the value at OUT after this
fault, so RungProof's retained destination remains simulator behavior rather
than a Siemens parity claim.

Current-source inspection found that both instructions routed every nonfinite
operand through `VC_RUNTIME_RANGE` with a misleading MIN/MAX-order message.
The runtime now distinguishes these conditions: NORM_X emits
`VC_RUNTIME_NORM_X_INVALID`, SCALE_X emits `VC_RUNTIME_SCALE_X_INVALID`, and
MIN >= MAX continues to emit `VC_RUNTIME_RANGE`. The predicate accepts zero
but rejects NaN, positive and negative infinity, and nonzero REAL subnormal
values below the IEEE-754 single-precision minimum-normal magnitude.

The controller regression exercises NaN, both infinities, and the smallest
REAL subnormal at every floating MIN, VALUE, and MAX position supported by
each instruction. Every case verifies the sampled REAL operand, powered EN,
false ENO, retained destination 12, `FailedNoWrite`, null calculated result,
and exactly one instruction-specific diagnostic.

Two fresh 1600x900 normal-renderer captures were inspected at 100%:

- `.tools/tia-norm-invalid-online-v3.png`;
- `.tools/tia-scale-invalid-online-v3.png`.

Both views show `NaN (REAL)` at VALUE, retained result 7, green EN flow, and
blue-dashed ENO/downstream flow. The dual type fields, MIN/VALUE/MAX/OUT pins,
operands, values, and conductor transitions remain legible without clipping or
overlap. `TIA_NORMALIZE_STATE_PASS` and `TIA_SCALE_STATE_PASS` independently
verify the exact runtime observations behind the pixels.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was
constructed or attempted. The pre-existing invalid challenge-content warning
and Godot shutdown RID/ObjectDB leak reports remain.

NORM_X and SCALE_X remain **PARTIAL** pending same-state installed-TIA
selection/editing/monitoring comparisons, exact installed-TIA geometry,
verified faulted OUT behavior, unsupported Siemens widths and LREAL behavior,
other zooms and tag modes, project interchange, and live PLC execution.
Overall TIA parity remains **OPEN**.

### 2026-10-14 MOD zero-divisor correction and exact-state evidence

The official Siemens STEP 7 V21
[`MOD` LAD page](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/math-functions-s7-1200-s7-1500-s7-1200-g2/mod-return-remainder-of-division-s7-1200-s7-1500-s7-1200-g2)
documents the target S7-1500 instruction and its EN/ENO interface. The
target-specific S7-1500 V21
[`MOD` STL page](https://docs.tia.siemens.cloud/r/en-us/v21/stl-s7-1500/stl-mnemonic-s7-1500/math-functions-s7-1500/integers-s7-1500/mod-return-remainder-of-division-s7-1500?contentId=jZMf_dpRP74h4DZ9qiPiGQ)
states that division by zero returns zero and sets the error/status bits. The
V21 [error-handling overview](https://docs.tia.siemens.cloud/r/en-us/v21/programming-basics/handling-program-execution-errors/overview-of-mechanisms-for-error-handling?contentId=xD546HyUpnsN9RwMXe2wSQ)
identifies ENO as the LAD/FBD runtime-error indication. Together those sources
establish OUT=0 with ENO=0 for the modeled target; the official S7-1200 MOD
table independently states the same OUT/ENO pair.

RungProof now writes zero instead of retaining the old destination when a MOD
divisor is zero. It emits exactly one `VC_RUNTIME_MOD_ZERO` diagnostic and
drops ENO/downstream power flow. Literal zero is accepted because it has a
defined runtime result. The observation model gained
`FailedErrorValueWritten`, distinct from both `FailedNoWrite` and
`FailedInvalidValueWritten`, so troubleshooting text accurately reports that
the instruction failed while writing Siemens' defined error value.

Controller regressions prove dynamic INT and DINT 7 MOD 0 transition OUT from
12 to 0, retain powered EN with false ENO, record calculated/stored zero, use
`FailedErrorValueWritten`, and publish exactly one diagnostic. A separate
literal INT 7 MOD 0 regression proves compile and execution behavior.

The fresh normal-renderer capture `.tools/tia-mod-zero-online-v3.png` was
inspected at 100%. It shows both inputs and monitored values, OUT 0, green EN,
and blue-dashed ENO/downstream flow without overlap or clipping.
`TIA_ARITHMETIC_STATE_PASS` verifies before 12, calculated/stored 0, the exact
disposition and diagnostic count; `TIA_ARITHMETIC_GEOMETRY_PASS` confirms the
reviewed 132x96 body and 32 px input pitch.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native monitoring
verifier reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was
constructed or attempted. The pre-existing invalid challenge-content warning
and Godot shutdown RID/ObjectDB leak reports remain.

MOD remains **PARTIAL** pending explicit target evidence for negative-operand
sign semantics, matched installed-TIA zero-divisor monitoring, unsupported
integer widths, other zooms and tag modes, project interchange, and live
S7-1500 execution. Overall TIA parity remains **OPEN**.

### 2026-10-14 cross-family operand and tag-information gate

Siemens STEP 7 V21 separates the program editor's operand representation
(`Symbolic and absolute`, `Symbolic`, or `Absolute`) from supplementary tag
information and its location. Siemens also states that hierarchical comments
place higher structure-level comments in brackets after the tag comment. See
the official [general program-editor settings](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/general-settings-for-the-plc-programming/overview-of-the-general-settings)
and [displaying or hiding tag information](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/displaying-or-hiding-tag-information).

The production canvas now keeps valid numeric, BOOL, TIME, and quoted-text
literals unchanged in every operand-representation mode instead of replacing
them with `???`. For an addressed tag, Absolute displays the address,
Symbolic displays the name, and Both displays both. Supplementary information
adds the complementary name/address plus the applicable comment without
changing the operand. Below-network rows now use that same selected operand
representation. For the supported one-level scalar STRUCT model, Show uses
the leaf comment and Hierarchical uses `leaf comment [parent comment]`; it no
longer reverses the comment levels.

The native `TiaViewSettingsCartesianProduct` gate exercises all 18 combinations
of three operand representations, three tag-information modes, and two
positions through the same functions used by the production draw path. It
also proves that numeric literal `123.456` remains a literal, unassigned `???`
remains unassigned, Hidden produces neither on-operand nor below-network
information, and non-hidden information appears at exactly one selected
location. `TiaStructuredSimpleCommentUsesLeaf` and
`TiaStructuredHierarchicalCommentUsesBracketedParent` verify the supported
STRUCT comment forms.

Three fresh 1600x900 normal-canvas captures were visually inspected at 100%:

- `.tools/tia-view-controls-absolute-hidden.png` shows addressed contacts and
  ADD operands with no supplementary information; the numeric constant stays
  `123.456` rather than becoming `???`.
- `.tools/tia-view-controls-both-below.png` shows symbolic and absolute forms
  on contacts and ADD operands, with one unclipped information row per used
  tag beneath the network.
- `.tools/tia-view-hierarchy-on-operands.png` shows the member comment followed
  by its bracketed parent comment directly at `motor_state.ready`.

The C# application and test builds completed with zero warnings and zero
errors, all 360 controller/editor tests passed, and the native
`--verify-ladder-editor` route reported `EDITOR_COMMAND_ROUTES_VERIFY PASS`.
The capture route also passed its scripted 75/100/150% target and reveal
checks. No real PLC transport was constructed or attempted. The pre-existing
invalid challenge-content warning and Godot shutdown RID/ObjectDB leak reports
remain.

This closes the local Cartesian control-state gap for addressed root tags and
the supported one-level STRUCT-comment form, but not overall view parity.
Same-state installed-TIA comparison, every other instruction family, physical
menu interaction, other zoom captures, and the exact Absolute/Both behavior
for local or optimized structured operands without modeled absolute layout
remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-14 deterministic true, false, and unexecuted power-flow gate

Siemens STEP 7 V20 explicitly defines four LAD program-status presentations:
green solid is satisfied, blue dashed is not satisfied, gray solid is unknown
or not executed, and black is not interconnected. It also distinguishes a
current value from one retained from an earlier cycle. See the official
[program-status display for LAD](https://docs.tia.siemens.cloud/r/en-us/v20/testing-the-user-program/testing-with-program-status/examples-of-program-status-display/program-status-display-for-lad-programs).
The Siemens source defines status categories, not exact RGB values, so the
RungProof palette values below are local deterministic rendering choices and
are not claimed as exact Siemens color samples.

The production canvas already used nullable flow state, but the distinction
was previously supported mainly by visual captures. New read-only review hooks
now expose the production tree projection and final line style without
duplicating either algorithm. `TIA_POWER_FLOW_STYLE_VERIFY PASS` is part of
the standard native monitoring verifier and enforces three distinct local
states: satisfied green/solid (`00a651`), unsatisfied blue/dashed (`233db8`),
and unknown gray/solid (`87949a`). The flow capture route additionally proves
that each evaluated parallel leg retains its own result and that every contact
in a JMP-skipped network projects `(null, null)`, not false.

Fresh 1600x900 captures were inspected at 100%:

- `.tools/tia-flow-mixed-v2.png` shows green incoming/satisfied flow, one green
  branch, one blue-dashed false branch, and green joined output through the Set
  coil.
- `.tools/tia-flow-false-v2.png` shows green flow to the split, both branch
  outputs blue dashed, and the joined conductor, Set coil, and right rail blue
  dashed.
- `.tools/tia-flow-unknown-v2.png` shows the entire JMP-skipped network gray
  solid, including rail, contacts, both parallel paths, coil, and right rail;
  the visited labeled destination below remains green and therefore visibly
  distinct.

Regenerating this evidence exposed an invalid review fixture: two top-level
networks used the parameterized-block-only `global:enable` syntax. They now use
the declared top-level `enable` tag, so the current compiler accepts the
fixture without weakening scope validation. The fixture's P contact now has
an explicit writable BOOL edge-memory operand, and its LABEL destination
network includes an independent terminal coil, removing unrelated `???` and
open-terminal artifacts from the evidence.

The application and test builds completed with zero warnings and zero errors,
all 360 controller/editor tests passed, and the native monitoring verifier
reported both `TIA_POWER_FLOW_STYLE_VERIFY PASS` and
`EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was constructed or
attempted. The pre-existing invalid challenge-content warning and Godot
shutdown RID/ObjectDB leak reports remain.

The local shared-canvas true/false/unexecuted distinction is now directly
gated and visually inspected, but overall monitoring parity remains
**PARTIAL**. At this checkpoint, same-state installed-TIA capture comparison,
exact installed palette/line metrics, stale-prior-cycle 50% saturation, black
unconnected status, and per-family unknown-state captures remained **OPEN**.
The next dated section closes only the local stale-value gate. Overall TIA
parity remains **OPEN**.

### 2026-10-15 earlier-cycle monitored-value saturation gate

The same official Siemens program-status reference states that values from the
current cycle use 100% color saturation and values retained from an earlier
cycle use 50% saturation. This rule applies to value presentation; it does not
turn a currently unexecuted rung into satisfied or unsatisfied power flow. See
the official [program-status display for LAD](https://docs.tia.siemens.cloud/r/en-us/v20/testing-the-user-program/testing-with-program-status/examples-of-program-status-display/program-status-display-for-lad-programs).

RungProof scan captures contain only instructions evaluated in that scan, so a
JMP-skipped instruction previously lost its displayed value entirely. The
production canvas now retains the most recent presentation separately from the
current publication. Fallback is allowed only while a completed scan is
running and the current reason is `NotEvaluatedThisScan` or
`ContextNotCaptured`. Retained entries are keyed by instruction and selected
call context, and the entire cache is retired on session, load,
execution-generation, or program-content identity change, Reset/Loaded
publication, explicit monitoring unavailability, or the legacy snapshot path.
The retained presentation is marked `EarlierCycle`; current energization and
power-flow projection continue to use only the current capture.

The Siemens source defines saturation percentages but does not publish an exact
RGB palette in this reference. RungProof therefore halves the HSV saturation
of its existing value-status color as a deterministic local rendering rule; it
does not claim `488f6d` as an exact Siemens sample. The standard native
monitoring verifier now reports `TIA_EARLIER_CYCLE_VALUE_VERIFY PASS` after
proving current `008f4c` at 100%, retained `488f6d` at 50%, gray/unknown
current flow, and cache clearing across a different load identity.

Fresh 100% capture `.tools/tia-flow-earlier-cycle-v1.png` was visually
inspected. Network 4 is skipped by the current scan: the rail, contacts,
parallel legs, coil, and right rail are all gray solid, while the prior
`FALSE` badges remain visible in reduced-saturation text. The visited LABEL
destination network below is green, making the current-versus-retained state
visibly distinct. The capture route independently reports
`TIA_EARLIER_CYCLE_VALUE_STYLE_PASS` and the existing three-state power-flow
gate.

The application and test builds completed with zero warnings and zero errors,
all 360 controller/editor tests passed, and the native monitoring verifier
reported `EDITOR_MONITORING_VERIFY PASS`. No real PLC transport was constructed
or attempted. The pre-existing invalid challenge-content warning and Godot
shutdown RID/ObjectDB leak reports remain.

This closes the local cross-cutting earlier-cycle-value gate, but overall
monitoring parity remains **PARTIAL**. Same-state installed-TIA capture
comparison, exact installed palette and text metrics, black unconnected status,
multiple-cycle/context visual cases, and per-instruction-family current versus
earlier-cycle captures remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 distinct not-interconnected rendering state

Siemens STEP 7 V20 and V21 define black as `Not interconnected`, separately
from gray `Unknown or not executed`. The official source does not say that an
unexecuted instruction is unconnected, so RungProof must never use black merely
because an observation is absent. See the official [program-status display for
LAD](https://docs.tia.siemens.cloud/r/en-us/v21/testing-the-user-program/testing-with-program-status/examples-of-program-status-display/program-status-display-for-lad-programs?contentId=RmvHgF8sy0iBGUXHK9gmZQ).

The production renderer now has an explicit four-state
`TiaPowerFlowStatus`: satisfied, not satisfied, unknown/not executed, and not
interconnected. The final color/line-style function maps them respectively to
local deterministic green solid (`00a651`), blue dashed (`233db8`), gray solid
(`87949a`), and black solid (`000000`). Existing nullable execution flow maps
only to the first three states, so this refactor does not silently reclassify
any current network. Both the normal capture route and the standard native
monitoring verifier now enforce all four distinct mappings;
`TIA_POWER_FLOW_STYLE_VERIFY PASS` reports
`notInterconnected=black/solid`.

This does **not** close the black-state parity gate. Current valid RungProof
editor networks are built as interconnected paths, while incomplete terminal
drafts and empty instruction placeholders fail Verify + Load and therefore
cannot supply an online observation. The supplied timer evidence also requires
Q/ET values at their pins, so those pins were not relabeled as unconnected
without matched TIA evidence. A future pass must add an explicit supported
connectivity model, compile/runtime semantics for that topology, and matching
installed-TIA and RungProof captures before any black instruction or conductor
is emitted. No synthetic visual capture was created for an unrepresentable
state.

The application build completed with zero warnings and zero errors, and the
native monitoring verifier retained the earlier-cycle and editor-monitoring
passes. No real PLC transport was constructed or attempted. Overall monitoring
parity remains **PARTIAL**, and overall TIA parity remains **OPEN**.

### 2026-10-15 skipped IEC timer-family unknown-state visual gate

The generic skipped-branch evidence did not prove that a stateful IEC box keeps
current power flow separate from retained values. A shared executable fixture
now runs TON, TOF, TP, or TONR for one scan, then takes a JMP over that network
on the next scan. It uses the same identity-checked publication path as the
normal monitoring canvas; no observation is fabricated for the skipped scan.

`TIA_TIMER_UNKNOWN_STATE_PASS` requires each contact's projected input/output
and each timer terminal input/Q output to remain nullable unknown, not false.
Every presentation must be `EarlierCycle=true` and
`CurrentExecution=false`. Retained values are checked exactly: TON ET=10 ms and
Q=FALSE; TOF ET=0 ms and Q=TRUE; TP ET=10 ms and Q=TRUE; TONR ET=10 ms,
Q=FALSE, and consumed R=FALSE. Every fixture also passes zero terminal-wire
clearance, proving the gray conductor meets both instruction edges rather than
disappearing around the box.

Fresh 100% captures `.tools/tia-ton-unknown-earlier-cycle-v2.png`,
`.tools/tia-tof-unknown-earlier-cycle-v1.png`,
`.tools/tia-tp-unknown-earlier-cycle-v1.png`, and
`.tools/tia-tonr-unknown-earlier-cycle-v1.png` were visually inspected. Their
current JMP and visited LABEL destination networks are green. Every skipped
timer network is gray solid from the left rail through the contact, box, Q
output, and right rail. Earlier-cycle frames remain visible at reduced
saturation without overlap; TONR keeps separate R and PT rows. These images do
not use black because each timer network is interconnected but not executed in
the current scan.

This closes only the local supported IEC timer-family unknown-state gate.
Equivalent skipped-state captures for CTU, CTD, CALL, comparison, numeric, and
program-control families remain **OPEN**, as do same-state installed-TIA
comparison and the distinct not-interconnected topology model. Overall TIA
parity remains **OPEN**.

### 2026-10-15 LABEL not-interconnected topology gate

The earlier four-color renderer gate proved only that black was available; it
did not identify a supported topology allowed to use it. Siemens V21 defines
black as `Not interconnected` in LAD program status, separately from gray
unknown/not executed. Siemens also defines LABEL as a marker that identifies
the destination network where execution resumes; the destination network then
executes its own contact/output logic. RungProof already stores LABEL as
network metadata and draws its compact marker beside the left rail rather than
inside the power-flow conductor.

The production renderer now applies `NotInterconnected` specifically to that
LABEL marker. It no longer derives the marker color from the destination
network's RLO. `TIA_LABEL_NOT_INTERCONNECTED_VERIFY PASS` executes one valid
loaded LABEL-plus-coil network twice and requires the marker to remain
black/solid while the contact and terminal conductors change independently
from blue-dashed false to green solid true. Existing skipped networks remain
gray; this change does not map absent observations to black.

Fresh normal-renderer capture
`.tools/tia-label-not-interconnected-online-v1.png` was visually inspected at
100%. The `finish` marker is black beside the rail while the destination
contact, coil, and rails are continuously green. The app and test projects
build with zero warnings/errors, all 360 controller/editor tests pass, and the
standard native monitoring verifier reports the new LABEL gate plus
`EDITOR_MONITORING_VERIFY PASS`. The known challenge-content hash warning and
Godot exit leak diagnostics are unchanged. No real PLC transport was
constructed or attempted.

This closes the local explicit-black topology gate for LABEL only. The exact
installed-TIA LABEL status/palette, other unconnected LAD output topologies,
and a same-state installed-TIA comparison remain **OPEN**. Overall TIA parity
remains **OPEN**.

### 2026-10-15 executable evidence-matrix inventory gate

The parity objective requires an audit row for every supported instruction and
editor control, but the matrix inventory previously had no executable link to
the production catalog. A catalog change could silently add or remove a key
without failing any parity check. The new
`TestTiaEvidenceMatrixCoverage` contract reads the checked-in matrix and now
requires:

- the exact seven-column `Key / TIA-facing name / claim boundary / Offline /
  Selected / Edit / Monitor` schema;
- exactly one row for every one of the 54 authoritative
  `LadderInstructionCatalog` keys, plus the separate empty-instruction-box
  authoring state;
- no missing, duplicate, or undocumented extra keys;
- a recognized `PASS`, `PARTIAL`, `OPEN`, or `N/A` status in each of the four
  state-gate cells;
- explicit all-`N/A` Siemens state gates for the four RungProof-local
  operations (`counter-load`, `counter-reset`, `compare-6`, `numeric-24`);
- an explicit Siemens/TIA claim boundary and at least one evidence-bearing gate
  for every native-facing catalog key; and
- an `N/A` monitor gate for the unresolved empty-box edit state.

The first run correctly failed rather than producing a false pass. `branch`
and `rung` had only one combined evidence cell and no TIA-facing-name column.
They now have separate Offline, Selected, Edit, and Monitor criteria with
current evidence and honest open boundaries. The same run found that TOF, TP,
and CTD depended on shared source lists without an instruction-row Siemens/TIA
claim boundary. Their rows now link the official STEP 7 V21 instruction pages
and state the governing pin and execution contracts directly.

After correction, `PASS TIA evidence matrix covers every catalog key and state
gate` is part of the normal controller/editor suite. The suite now reports
361 passed and 0 failed. This proves inventory synchronization and state-gate
structure, not visual equivalence: OPEN/PARTIAL rows, matched installed-TIA
captures, and unsupported limitations remain authoritative. No real PLC
transport was constructed or attempted. Overall TIA parity remains **OPEN**.

### 2026-10-15 branch and network selection-state gates

The executable matrix inventory exposed two Selected-state gaps. Recursive
branch selection already had a renderer path, but no dedicated current-build
capture or assertion isolated the root parallel topology. Whole-network
selection was a functional gap: `SelectedRung` identified the editor target,
but the TIA canvas did not render any visible network-only cue.

The canvas now distinguishes explicit network selection from its current-rung
cursor. Selecting a node, insertion slot, terminal, or LABEL clears the
network-only state, so existing instruction captures remain unchanged.
Network-only selection draws a thin border and four-pixel header marker after
the content; it does not fill or recolor the header, comment row, rail,
instructions, or power-flow conductors. This is deliberately documented as a
RungProof usability treatment until a matched installed-TIA reference exists.

Two normal Vulkan renderer captures were inspected at 100%:

- `.tools/tia-branch-selected-local-v1.png` encloses the root parallel split,
  join, both three-contact legs, and their conductors while stopping before the
  terminal coil. `TIA_BRANCH_SELECTION_PASS` reports root ID
  `branch-start-parallel`, bounds `(48, 222), (968, 308)`, and output center
  x=1122.
- `.tools/tia-network-selected-local-v1.png` shows an 818x320 border-only cue
  around Network 1. Its number/title, comment row, rail, contact, coil, and
  conductors remain visible, while Networks 2 and 3 remain unselected.
  `TIA_NETWORK_SELECTION_PASS` also requires no node, terminal, or LABEL to be
  simultaneously selected.

Both app and test projects build with zero warnings and zero errors. All 361
controller/editor tests pass, including the matrix inventory contract, with
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native monitoring verifier also
reports `EDITOR_MONITORING_VERIFY PASS`; its existing challenge-content warning
and Godot shutdown leak diagnostics remain visible and non-failing. These gates
close the missing local capture/assertion evidence only, so the matrix rows
move from Selected **OPEN** to **PARTIAL**. Insertion-slot selection, physical
pointer interaction, and same-state installed-TIA selection styling remain
**OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 NO/NC unknown-state and plain-network gates

The NO and NC rows previously had exact evaluated FALSE/TRUE evidence but no
instruction-family gate for a network that was not executed in the current
scan. The `rung` row likewise relied on mixed fixtures rather than a dedicated
plain-network offline witness and complete evaluated/unknown state set.

The new `contact-unknown` fixture first executes both reviewed networks, with
NO consuming TRUE and NC consuming FALSE so both local results and assignment
coils become TRUE. On the next scan a leading JMP transfers to a block-local
LABEL. The exact runtime gate requires the current observation capture to omit
both contacts and both coils, report the gate JMP as `Taken`, execute the LABEL
destination, and preserve the earlier output writes. This is skipped execution,
not a false contact result.

`TIA_CONTACT_UNKNOWN_STATE_PASS` then checks the production presentation path
for each skipped network. Contact input/output and terminal flow must all be
nullable unknown; the exact earlier BOOL identity/value and TRUE local result
must remain `EarlierCycle=true`, `CurrentExecution=false`, and use the
deterministic 50% saturation color. Siemens V21's official
[normally open contact](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/normally-open-contact-s7-1200-s7-1500-s7-1200-g2)
and [normally closed contact](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/normally-closed-contact-s7-1200-s7-1500-s7-1200-g2?contentId=IeoGUKGSZqx5uM3E6NsvqQ)
pages are now linked directly from their matrix rows and define the modeled
BOOL/RLO behavior.

Three fresh normal Vulkan renderer captures were inspected at 100%:

- `.tools/tia-contact-no-unknown-earlier-cycle-v1.png` shows the skipped NO
  network gray/solid with retained TRUE operand/result values at reduced
  saturation;
- `.tools/tia-contact-nc-unknown-earlier-cycle-v1.png` shows the skipped NC
  network gray/solid with retained FALSE operand and TRUE result, while the
  visited LABEL destination is independently green; and
- `.tools/tia-network-plain-offline-v1.png` provides the dedicated unselected
  plain-network witness with header/comment, left rail, contact, assignment
  coil, and right termination unobscured and continuously connected.

Together with the existing exact NO/NC true/false captures, this closes the
missing local true/false/unknown plain-network state set. Both projects build
with zero warnings/errors, all 361 controller/editor tests pass, and the native
monitoring verifier reports `EDITOR_MONITORING_VERIFY PASS`. Its existing
challenge-content warning and shutdown leak diagnostics remain non-failing.
The test process again confirms no real PLC transport was constructed or
attempted. Same-state installed-TIA colors, other network shapes, project
interchange, and live PLC execution remain **OPEN**. Overall TIA parity remains
**OPEN**.

### 2026-10-15 P/N edge unknown-state gates

The P and N edge-contact rows had exact baseline, pulse, and held-state gates,
but no instruction-family proof for an edge network skipped in the current
scan. The new `edge-unknown` fixture separates retained earlier-cycle state
from current execution rather than presenting a skipped edge as a false edge.

The earlier scan executes both transitions: P consumes `rising_signal` changing
FALSE→TRUE with dedicated `rising_memory`, while N consumes `falling_signal`
changing TRUE→FALSE with dedicated `falling_memory`. Both contacts pulse TRUE,
both assignment coils write TRUE, and both edge-memory bits update. On the next
scan `skip_edges` is TRUE, so a leading JMP transfers to the block-local
`done` LABEL and neither edge network executes. The exact runtime gate requires
the current observation capture to omit both contacts and coils, requires the
JMP to be `Taken`, requires the LABEL destination to execute, and requires both
earlier outputs and both dedicated edge-memory values to remain preserved.

`TIA_EDGE_UNKNOWN_STATE_PASS` checks the production presentation path for each
skipped network. Contact input/output and terminal flow must all be nullable
unknown. The retained earlier observation must identify the exact signal and
dedicated edge-memory operand, exact previous/current transition, TRUE local
result, `StateUpdatedChanged`, `EarlierCycle=true`, and
`CurrentExecution=false`. Retained values use the deterministic 50% saturation
color; the gray conductor represents current execution as unknown, not false.

The modeled P contract is anchored to Siemens STEP 7 V21
[P in LAD](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/p-scan-operand-for-positive-signal-edge-s7-1200-s7-1500-s7-1200-g2?contentId=s7_wEUjrTWNDuj1RbiydBQ): a 0→1 transition, a separate unique edge-memory
operand below the signal operand, and a TRUE result for one cycle. For N, the
directly accessible Siemens STEP 7 V20
[N LAD page](https://docs.tia.siemens.cloud/r/en-us/v20/lad-s7-1200-s7-1500/bit-logic-operations-s7-1200-s7-1500/n-scan-operand-for-negative-signal-edge-s7-1200-s7-1500)
provides the LAD placement reference and the STEP 7 V21
[N FBD page](https://docs.tia.siemens.cloud/r/en-us/v21/fbd-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/n-scan-operand-for-negative-signal-edge-s7-1200-s7-1500-s7-1200-g2)
independently confirms the 1→0 one-cycle semantics. The FBD page is not treated
as LAD visual evidence, and a directly accessible V21 LAD N reference remains
open.

Two fresh normal Vulkan renderer captures were visually inspected:

- `.tools/tia-edge-rising-unknown-earlier-cycle-v1.png` shows the skipped P
  network gray/solid with signal above, `rising_memory` below, and retained TRUE
  result/value chips at reduced saturation; and
- `.tools/tia-edge-falling-unknown-earlier-cycle-v1.png` shows the skipped N
  network with the same gray/unknown treatment and `falling_memory` below,
  while the visited LABEL destination is independently green.

Both captures preserve continuous conductors and readable operands with no
observed clipping or overlap. These are RungProof renderer observations, not a
matched installed-TIA comparison. Both app and test projects build with zero
warnings and zero errors. All 361 controller/editor tests pass, including the
matrix inventory contract, and report `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE`
and `REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native headless monitoring
verifier reports `EDITOR_MONITORING_VERIFY PASS`; its existing
challenge-content warning and Godot shutdown leak diagnostics remain visible
and non-failing. Same-state installed-TIA colors, CPU-start and retentivity
configurations, project interchange, and live PLC execution remain **OPEN**.
Overall TIA parity remains **OPEN**.

### 2026-10-15 assignment/Set/Reset unknown-state gates

The assignment, Set, and Reset rows had exact evaluated FALSE/TRUE write gates
but no instruction-family proof for terminal actions skipped in the current
scan. The new shared `coil-unknown` fixture first executes all three actions
from TRUE incoming RLO: assignment changes `assign_unknown_out` FALSE→TRUE,
Set changes `set_unknown_out` FALSE→TRUE, and Reset changes
`reset_unknown_out` TRUE→FALSE. Each earlier observation must identify the
exact destination, before/after values, and `WrittenChanged` disposition.

On the next scan `skip_coils` is TRUE and a leading JMP transfers to the
block-local `done` LABEL. The exact runtime gate requires all three skipped
contacts and coils to be absent from the current observation capture, the JMP
to report `Taken`, the LABEL destination to execute, and all three earlier
output states to remain preserved. This distinguishes skipped/current-unknown
execution from a FALSE RLO. A FALSE RLO would still execute an assignment write
and would produce explicit `SkippedDisabled` behavior for Set or Reset.

`TIA_COIL_UNKNOWN_STATE_PASS` checks each terminal presentation independently.
The skipped contact and terminal flow must be nullable unknown; the retained
presentation must report `EarlierCycle=true`, `CurrentExecution=false`, exact
destination identity, exact before/stored-after values, and `Written — value
changed`. Retained values use the deterministic 50% saturation color.

Three fresh normal Vulkan renderer captures were visually inspected:

- `.tools/tia-assignment-unknown-earlier-cycle-v1.png` shows the skipped
  assignment network gray/solid with its earlier TRUE destination value;
- `.tools/tia-set-unknown-earlier-cycle-v1.png` shows the skipped Set network
  gray/solid with the `S` marker and retained TRUE destination value; and
- `.tools/tia-reset-unknown-earlier-cycle-v1.png` shows the skipped Reset
  network gray/solid with the `R` marker and retained FALSE destination value,
  while the visited LABEL destination is independently green.

All three captures retain continuous conductors and readable operands without
observed clipping or overlap. Their semantics remain anchored to the official
Siemens STEP 7 V21 [Assignment](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/assignment-s7-1200-s7-1500-s7-1200-g2),
[Set output](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/s-set-output-s7-1200-s7-1500-s7-1200-g2?contentId=Wh9qyAlMOIlzh6OBxbOEYA),
and [Reset output](https://docs.tia.siemens.cloud/r/en-us/v21/lad-s7-1200-s7-1500-s7-1200-g2/bit-logic-operations-s7-1200-s7-1500-s7-1200-g2/r-reset-output-s7-1200-s7-1500-s7-1200-g2?contentId=C2FGQIC3pRy1AxpaDUjqVg)
pages already linked from the matrix rows. These captures are RungProof
renderer observations, not matched installed-TIA comparison. Both app and test
projects build with zero warnings and zero errors. All 361 controller/editor
tests pass, including the matrix inventory contract, and report
`REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native headless monitoring verifier
reports `EDITOR_MONITORING_VERIFY PASS`; its existing challenge-content warning
and Godot shutdown leak diagnostics remain visible and non-failing. Same-state
installed-TIA colors, Siemens-permitted inline placement for S/R, downstream
assignment continuation, project interchange, and live PLC execution remain
**OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 parallel-branch three-valued join gate

The branch monitor row already had running-editor evidence for a TRUE/FALSE
mixed branch and a completely JMP-skipped unknown branch, but it did not prove
the conservative join rule for a partially known branch. The production
projection previously implemented that rule inline, leaving the exact
TRUE/FALSE/unknown combinations unasserted.

The renderer now uses one `ParallelOutput` helper for the actual join. The
native monitoring verifier exercises that same production function and reports
`TIA_PARALLEL_TRISTATE_PROJECTION_PASS` only when all four contracts hold:

- TRUE + unknown joins TRUE because one energized path proves continuity;
- FALSE + unknown remains unknown because the false path cannot decide the
  other path;
- unknown + unknown remains unknown; and
- FALSE + FALSE joins FALSE.

This is a conservative display projection for incomplete monitoring evidence,
not invented PLC execution. RungProof's completed-scan runtime evaluates every
child of a parallel node, so it does not manufacture a partly observed branch
in normal completed execution. The gate ensures the renderer remains correct
if one path is unavailable because of monitoring/context evidence.

The official Siemens STEP 7 V21
[LAD program-status page](https://docs.tia.siemens.cloud/r/en-us/v21/testing-the-user-program/testing-with-program-status/examples-of-program-status-display/program-status-display-for-lad-programs?contentId=RmvHgF8sy0iBGUXHK9gmZQ)
defines satisfied as green/solid, not satisfied as blue/dashed, unknown or not
executed as gray/solid, and earlier-cycle values at 50% saturation. It does not
publish a separate mixed-unknown branch truth table, so RungProof documents the
join rule as a conservative local invariant rather than claiming a sampled TIA
state.

Fresh normal-Vulkan capture `.tools/tia-parallel-unknown-current-v1.png` was
visually inspected at 100%. Its JMP-skipped network is gray/solid continuously
from the left rail through the series contact, both parallel legs, join,
assignment coil, and right termination. Earlier FALSE values remain visible at
reduced saturation, while the visited LABEL destination below is independently
green. No conductor gaps, clipping, or operand overlap were observed. The
capture proves the all-unknown rendering state; the native gate separately
proves mixed-known/unknown aggregation.

Both app and test projects build with zero warnings and zero errors. All 361
controller/editor tests pass, including the matrix inventory contract, and
report `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The native headless monitoring verifier
reports both `TIA_PARALLEL_TRISTATE_PROJECTION_PASS` and
`EDITOR_MONITORING_VERIFY PASS`; its existing challenge-content warning and
Godot shutdown leak diagnostics remain visible and non-failing. Other branch
shapes, a naturally occurring partial-observation capture, same-state
installed-TIA monitoring, project interchange, and live PLC execution remain
**OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 non-root and nested parallel-branch gates

The branch row previously covered a root split at the left power rail, one
ordinary parallel shape, an all-unknown skipped branch, and the conservative
three-valued join rule. It did not have running-editor evidence for a branch
opened after a preceding contact or for a nested split/join.

The new `branch-nested` fixture is one executable network with this topology:
a TRUE lead contact; a non-root outer parallel branch; an upper TRUE contact
feeding an inner FALSE/TRUE parallel branch; a FALSE outer lower leg; a TRUE
trailing contact; and an assignment coil. Exact runtime assertions require the
inner join to resolve TRUE, the outer join to resolve TRUE, the trailing series
path and terminal flow to remain TRUE, and the output to be written TRUE.
`TIA_NESTED_BRANCH_STATE_PASS` verifies the captured instruction results, while
`TIA_NESTED_BRANCH_FLOW_PASS` verifies each production-renderer input/output
projection and both OR joins.

Three fresh normal-Vulkan captures were visually inspected at 100%:

- `.tools/tia-branch-nested-online-v1.png` shows the left rail, lead contact,
  non-root outer split, upper contact, nested split/join, lower leg, and trailing
  contact;
- `.tools/tia-branch-nested-online-right-v1.png` uses ordinary horizontal
  scrolling to show the nested join, outer join, trailing contact, assignment
  coil, and right termination; and
- `.tools/tia-branch-nested-selected-v1.png` shows the inner parallel subtree
  selected without obscuring either nested leg. The exact selection gate reports
  bounds `(764, 222), (392, 308)` and requires branch-only selection state.

The full topology is wider than the 1600 px review viewport at 100% zoom.
Evidence therefore uses two honest scroll positions rather than claiming a
one-screen fit. Across the pair, all conductor segments are continuous; TRUE
paths are green/solid, FALSE paths are blue/dashed, both joins have the expected
color, operands remain aligned, and no clipping or overlap was observed within
either viewport.

The first post-documentation test run correctly failed the matrix inventory
contract because literal `|` characters in prose split the Markdown row into
nine cells. Replacing those prose separators with `+` restored the required
seven-column row. Both app and test projects then build with zero warnings and
zero errors, all 361 controller/editor tests pass, the native monitoring
verifier reports `EDITOR_MONITORING_VERIFY PASS`, and the test process reports
no real PLC transport or connection attempt. The existing challenge-content
warning and Godot shutdown leak diagnostics remain visible and non-failing.

This closes local evidence for one non-root/nested shape only. Deeper or more
asymmetric branch layouts, physical branch editing, matched installed-TIA
selection/monitoring, project interchange, and live PLC execution remain
**OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 exact-slot pointer branch creation gate

The prior nested-branch evidence began with a preconstructed document. The
branch matrix therefore still lacked native proof that a user could point at a
specific series wire, create the nested path there, fill it, and recover the
exact prior state with editor history.

The new `branch-pointer` recursive-editor fixture begins with a flat two-path
seal-in network. It sends a real left-button canvas event to the wire slot
between `Drive_enable` and `Stop_request`, confirms the stable tree target is
exactly `seal-path[1]`, previews and drops the Branch palette item through
`DropInstructionAt`, then previews and drops the first NO contact into the new
path. The resulting document must compile, preserve every pre-existing node ID,
assign fresh IDs to the new path and contact, undo the contact and branch back
to byte-identical project JSON, and redo both edits back to byte-identical
edited JSON. The native gate reports:

`TIA_POINTER_BRANCH_EDIT_PASS parent=seal-path slot=1 newPath=tree-node-1 contact=tree-node-4 stableIds=true compile=true undo=true redo=true pointer=(388, 462)`

The same mutation is a required part of the full native editor regression and
reports `TIA_POINTER_BRANCH_ROUTE_VERIFY PASS`; the overall interaction result
includes `pointerBranch=True`.

The first run failed before mutation and exposed a real route inconsistency:
toolbar branch creation honored a selected exact slot in a flat network, while
dragging the Branch palette item at that same slot fell through the legacy
flat-instruction callback. `LadderEditorCanvas.DropInstructionAt` now treats a
branch drop as structural at every valid tree slot. Ordinary flat contact drops
retain their existing event path; recursive contact drops still use exact tree
commands.

Fresh normal-Vulkan capture `.tools/tia-branch-pointer-edit-v1.png` was visually
inspected at 100%. It shows the original root parallel paths, the new nested
split after `Drive_enable`, the original NC series leg, the new NO contact leg,
both continuous joins, the output coil and right termination. The blue dotted
selection rectangle contains only the new path and does not obscure its contact
or either conductor. No gap, collision, operand overlap, rail duplication, or
viewport clipping was observed.

Both application and test projects build with zero warnings and zero errors.
All 361 controller/editor tests pass, including the seven-column evidence-matrix
contract, and report `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The full native ladder-editor interaction
and command-route verifiers also pass. Their existing headless popup-position
errors and Godot shutdown leak diagnostics remain visible and non-failing.

This closes local exact-slot pointer branch creation, first-leg insertion, and
two-level history proof. A matched installed-TIA pointer/menu capture, deeper or
asymmetric editing gestures, project interchange, and live PLC execution remain
**OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 TIA branch-connection deletion route

The branch audit previously treated the toolbar's explicit `− BRANCH` action as
acceptable local deletion evidence. Current Siemens STEP 7 V21 documentation is
more specific. [Inserting branches into the LAD network](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/inserting-branches-into-the-lad-network?contentId=zxo~IvumbOlQ8TVX6DAL3A)
requires an existing LAD element and places `Open branches` from the Simple
instructions palette by drag-and-drop. [Deleting branches in LAD networks](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/deleting-branches-in-lad-networks)
selects the connection line linking the branch to the main branch, then chooses
`Delete` from its shortcut menu. The separate [rules for branches](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/rules-for-branches-in-lad?contentId=jsBAKV~wzHUUwt657xLTzw)
also state that deleting the final LAD element removes the remaining branch.

RungProof now hit-tests rendered conductor segments as first-class targets. The
parallel input segment carries the stable ID of its exact path, so clicking the
nested branch connection no longer falls back to the enclosing root composite.
In the Siemens canvas, a right-clicked path whose parent is Parallel presents
`Delete branch`; the unrelated generic `Remove selected subtree` wording is not
shown for that target. The direct `− BRANCH` toolbar control is hidden in the
TIA workbench while remaining available to the separate Logix-style surface.

The fresh `branch-delete-menu` native fixture first creates and fills the nested
path through the already-proved pointer route. It then right-clicks the exact
input connection, verifies the selected stable path and menu text, activates
the popup's real `Delete` command ID, proves that only the nested path is gone
and the remaining program compiles, and verifies byte-identical project JSON
through delete Undo, Redo, and capture restoration. It reports:

`TIA_BRANCH_CONNECTION_DELETE_PASS path=tree-node-1 pointer=(426, 634) selected=true menu=Delete_branch compile=true undo=true redo=true toolbarShortcut=hidden`

The same route is a required part of the full editor verifier and reports
`TIA_BRANCH_CONNECTION_DELETE_ROUTE_VERIFY PASS`; the overall result now
includes `branchConnectionDelete=True`. Renderer-neutral regression
`ParallelConnectionWireTargetsExactPath` verifies that a branch input segment
resolves to its path rather than the containing parallel root.

The separate Siemens last-element rule is also enforced in the TIA command
dispatcher. When `Delete` targets the only leaf in a Series path whose parent is
a multi-path Parallel node, the one history transaction is retargeted to that
exact Series path. The UI still presents `Delete LAD element` for the selected
contact, while the resulting empty branch is removed atomically. Other element
deletes and the Logix-style dispatcher are unchanged. The full native verifier
reports:

`TIA_LAST_BRANCH_ELEMENT_DELETE_VERIFY PASS contact=tree-node-4 path=tree-node-1 selected=True menu=True collapsed=True undo=True redo=True`

The overall interaction result includes `lastBranchElementDelete=True`.

Fresh normal-Vulkan capture `.tools/tia-branch-delete-menu-v1.png` was inspected
at 100%. Godot's native popup is a separate window and is not present in the
viewport PNG, so the image is evidence only for the restored exact path
selection and clean topology; the executable menu-text gate is the menu
evidence. The selected path encloses its contact and horizontal conductor
without touching the original upper leg. The outer root branch, nested split,
both joins, output coil, and right termination remain continuous and unclipped.

Both application and test projects build with zero warnings and zero errors.
All 362 controller/editor tests pass, including the matrix inventory contract,
and report no real PLC transport construction or connection attempt. The full
native interaction and command-route verifiers pass. An initial combined run
correctly failed a later focus-ownership gate because the review activator had
emitted the popup command without dismissing the popup; it now mirrors a real
menu click by hiding the popup after activation.

Connection-line deletion and last-element automatic branch removal are now
proved locally and grounded in official V21 documentation. Drag-to-close branch
placement, deeper/asymmetric editing, a matched installed-TIA gesture capture,
project interchange, and live PLC execution remain **OPEN**. Overall TIA parity
remains **OPEN**.

### 2026-10-15 physical Enter route for TIA project-tree activation

The project-tree matrix previously proved selection-versus-activation only by
emitting Godot's `ItemActivated` signal directly. That established the command
dispatcher but not that a focused tree accepted a real keyboard activation.

The native editor verifier now gives the actual TIA project tree focus and
dispatches `InputEventKey(Enter)` through the viewport. It proves that physical
Enter opens a selected program block while a preceding selection event leaves
the current block unchanged. The route reports:

`TIA_PROJECT_DOCUMENT_INPUT_DETAIL single=True physicalEnter=True activation=True tableSingle=True tableActivation=True allTags=True ladderTab=True`

The command-route verifier uses the same focused viewport input route for the
`Add new block` and `Add new watch table` rows. It retains explicit negative
checks that selecting either command does not open its dialog, then requires:

- `TiaAddNewBlockOpensOnPhysicalEnter PASS`
- `TiaAddNewWatchTableOpensOnPhysicalEnter PASS`

No production activation handler was added: the existing tree already routes
Godot's native `ItemActivated` signal to the supported object and command
workflows. Adding a second script-level Enter or double-click handler would risk
executing a real activation twice. An attempted viewport double-click probe did
not reach the native tree handler in the headless 28 by 24 px project-tree
layout, so that unreliable probe was removed rather than treated as evidence.
Physical pointer double-click therefore remains **OPEN**.

Both application and controller/editor test projects build with zero warnings
and zero errors. The full native ladder-editor verifier reports
`TIA_PROJECT_DOCUMENT_NAV_VERIFY PASS`, `LADDER_EDITOR_INTERACTION_VERIFY PASS`,
and `EDITOR_COMMAND_ROUTES_VERIFY PASS`. The 362-test controller/editor suite
passes and continues to report no real PLC transport construction or connection
attempt. The existing challenge-content warning, headless popup-position
messages, and Godot shutdown leak diagnostics remain non-failing.

This closes the focused physical-Enter gap for supported project-tree block and
command activation only. Physical double-click, unsupported DB/access-mode
workflows, runtime DB-member addressing, multi-instance/parameter-instance
storage, exact installed-TIA comparison, project interchange, and live PLC
execution remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 exact downstream branch-close gesture

The earlier branch pointer route always left the join at the end of the entire
downstream suffix. That was structurally valid ladder logic, but it did not
implement Siemens' separate close operation or let the author retain later
instructions in series after the join. Siemens STEP 7 V21's
[closing-branches procedure](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/closing-branches-in-the-lad-network)
selects an open branch, holds the left mouse button, shows a dashed route and
permissible green connections, and closes when released on a permissible
connection. The [branch rules](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/rules-for-branches-in-lad?contentId=jsBAKV~wzHUUwt657xLTzw)
require an LAD element in the main branch.

RungProof now supports that exact downstream-placement effect for a two-path
branch. Pressing the lower path's output connection starts the close gesture.
Pointer motion shows a dashed green route and highlights only downstream slots
that leave at least one LAD element in the main path. Releasing on a valid slot
moves the remaining upper-path suffix after the parallel join in one history
transaction. Existing node IDs are preserved; no instruction is cloned or
recreated. Key input, document refresh, an invalid release, mutation lock, or a
stale geometry rebuild cancels the transient drag safely.

The renderer-neutral `CloseBranchMovesJoinToExactDownstreamSlot` regression
proves exact placement, slot-zero rejection, preservation of all IDs,
compilation, and byte-identical Undo/Redo. The full production-canvas route
uses press, motion, and release events and reports:

`TIA_BRANCH_CLOSE_ROUTE_VERIFY PASS path=tree-node-1 main=tree-node-2 started=True preview=True applied=True stableIds=True compile=True undo=True redo=True`

Normal-Vulkan fixtures independently report `TIA_BRANCH_CLOSE_PREVIEW_PASS`
and `TIA_BRANCH_CLOSE_FINAL_PASS`. Fresh 1600 by 900 captures were inspected at
100%:

- `.tools/tia-branch-close-preview-v1.png` shows the selected lower path, a
  green permissible point after `Stop_request`, and a dashed green vertical
  route from the lower output connection. Operands and conductors remain clear.
- `.tools/tia-branch-close-final-v1.png` shows the nested join immediately after
  `Stop_request`, with `Photoeye_clear` retained in series after that join. The
  split, both legs, relocated join, trailing contact, outer join, coil, and
  termination are continuous, with no crossing, gap, or operand overlap.

The application and test projects build with zero warnings and zero errors.
All 363 controller/editor tests pass, including the matrix inventory contract,
and report `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The full native ladder-editor route
reports `LADDER_EDITOR_INTERACTION_VERIFY PASS` with `branchClose=True`; the
command-route verifier also passes.

This closes exact earlier-join placement for one main path plus one lower path.
The open/dangling authoring state and closing at the already-current join were
still open in this checkpoint; the next checkpoint resolves both. Independently
restructuring one leg of a multi-path parallel, matched installed-TIA gesture
capture, deeper asymmetric cases, project interchange, and live PLC execution
remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 persistent open branch and current-join close

The editor now represents an authored lower leg as an explicit open branch
rather than silently joining it at the downstream suffix. `OpenEnd` is valid
only on a lower Series child of a Parallel node. Opening a branch sets that
state; inserting instructions into the leg preserves it. The production layout
draws the split and the lower leg but omits its output wire and join, leaving a
visible dangling end until the author performs the close gesture.

Open work is deliberately an editor-project state, not executable ladder.
Editor-project schema 6 persists and reloads `openEnd`; the compiler reports
`VC003` at the exact `.openEnd` path until the leg is closed. Executable-program
JSON export rejects any open branch, preventing an invalid draft from being
silently reinterpreted by the older runtime format. Closing clears `OpenEnd`,
restores a compilable topology, and remains one exact Undo/Redo transaction.

The pointer candidate route now accepts both permissible forms for a two-path
branch: the existing downstream join and an earlier downstream slot. The native
production-canvas verifier reports:

- `TIA_POINTER_BRANCH_ROUTE_VERIFY PASS ... open=True undo=True redo=True`
- `TIA_BRANCH_CLOSE_CURRENT_JOIN_VERIFY PASS ... slot=2 ... applied=True undo=True redo=True`
- `TIA_BRANCH_CLOSE_ROUTE_VERIFY PASS ... preview=True applied=True stableIds=True compile=True undo=True redo=True`

The renderer-neutral regressions prove that an open leg remains rejected after
its first contact is inserted, survives schema-6 save/reload, cannot be exported
as an executable program, closes at the current join, and can still relocate to
an exact earlier join. The full controller/editor suite passes **364/364** and
continues to report `REAL_PLC_TRANSPORT_CONSTRUCTED FALSE` and
`REAL_PLC_CONNECTION_ATTEMPTED FALSE`. The application build has zero warnings
and zero errors; the full native ladder-editor and command-route gates pass.

Fresh 1600 by 900 normal-Vulkan captures were inspected at original resolution:

- `.tools/tia-branch-open-contact-v1.png` shows the selected lower path ending
  visibly after its first contact, with no premature output wire or join.
- `.tools/tia-branch-close-preview-v2.png` preserves that dangling leg while a
  dashed green route points to the permissible earlier connection.
- `.tools/tia-branch-close-final-v2.png` shows the earlier join with
  `Photoeye_clear` retained in series after it.
- `.tools/tia-branch-close-current-final-v1.png` shows the same lower leg closed
  at the existing downstream join, with continuous split, paths, join, coil,
  and termination.

These captures and gates prove the current RungProof implementation, not visual
parity with an installed TIA build. Multi-path close restructuring, deeper
asymmetric authoring, matched installed-TIA gesture captures, project
interchange, and live PLC execution remain **OPEN**. Overall TIA parity remains
**OPEN**.

### 2026-10-15 multi-path branch-close restructuring

Siemens V21 explicitly permits
[several branches in a network](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/inserting-branches-into-the-lad-network?contentId=zxo~IvumbOlQ8TVX6DAL3A).
Its [close procedure](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/closing-branches-in-the-lad-network)
requires dragging an open leg to a suitable connection and states that branches
are rearranged when necessary so they do not cross. The
[branch rules](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/rules-for-branches-in-lad?contentId=jsBAKV~wzHUUwt657xLTzw)
require downward opening, upward termination, and an LAD element in the main
branch. Those sources define the visible topology and gesture, not an internal
tree encoding.

RungProof now closes one newly opened leg of a three-path parallel at an earlier
main-path connection without moving or flattening the pre-existing lower leg.
The selected leg and the consumed main prefix become an explicit nested
two-path Parallel inside the outer main Series. The remaining main suffix stays
after that nested join, while every unselected outer leg keeps its original
outer split/join. Every existing ID is preserved; exactly two new structural
IDs are allocated for the nested Parallel and its main-prefix Series. A
power-rail-rooted two-path branch can also move its join earlier by adding one
new Series root rather than rejecting the otherwise valid topology.

Renderer-neutral regressions `CloseOnePathOfMultiPathParallelCreatesExplicitNestedJoin`
and `ClosePowerRailBranchMovesJoinEarlier` prove the exact shapes, ID subset,
minimal structural allocation, compilation, and byte-identical Undo/Redo. The
native production route opens and fills a third leg through the palette, drags
its output connection to the earlier main slot, releases it, verifies the
nested topology, then restores the original open draft exactly. It reports:

`TIA_MULTIPATH_BRANCH_CLOSE_ROUTE_VERIFY PASS outer=tree-node-3 path=tree-node-5 contact=tree-node-6 opened=True inserted=True started=True preview=True applied=True undo=True redo=True restored=True`

Fresh normal-Vulkan captures were inspected at original resolution:

- `.tools/tia-branch-close-multipath-preview-v1.png` shows the existing closed
  lower leg, the new third leg still dangling, the earlier selected connection,
  the current-join alternative, and the dashed green close route without
  crossings or operand overlap.
- `.tools/tia-branch-close-multipath-final-v1.png` shows the new leg nested with
  `Stop_request`, `Photoeye_clear` remaining in series after that inner join,
  and the original `Start_request` lower leg retaining the outer join. All
  conductors, the coil, and the termination remain continuous.

The application build passes with zero warnings and zero errors. The full
controller/editor suite passes **366/366**, reports no real PLC transport or
connection attempt, and the complete native ladder-editor and command-route
gates pass with `multiPathBranchClose=True`.

This evidence covers one new third leg closing to an earlier slot on the upper
main path. More-than-three-path cases, multiple simultaneously open legs,
closing toward a non-main lower leg, deeper asymmetric authoring, matched
installed-TIA gesture captures, project interchange, and live PLC execution
remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 multiple independently open branch legs

Siemens V21 states that an LAD network can contain
[several branches](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/inserting-branches-into-the-lad-network?contentId=zxo~IvumbOlQ8TVX6DAL3A),
and its [close procedure](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/closing-branches-in-the-lad-network)
operates on one selected open branch at a time. Each selected leg is dragged to
a suitable green connection; branches may be rearranged to avoid crossings.
The [branch rules](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/branches-in-lad/rules-for-branches-in-lad?contentId=jsBAKV~wzHUUwt657xLTzw)
still require downward opening, upward termination, and an LAD element before
the selected termination point.

The new `MultipleOpenPathsCloseIndependentlyWithoutConsumingSiblings`
regression constructs a four-path parallel with two independently open lower
legs. The compiler reports two exact `.openEnd` diagnostics. Closing the first
leg removes only that diagnostic and leaves the sibling open, selected, and
unchanged. Closing the second removes the final diagnostic and produces a valid
nested topology. Every pre-existing ID survives; each earlier close allocates
exactly one nested Parallel and one main-prefix Series. Two Undo operations
restore the byte-identical two-open project, and two Redo operations reproduce
the fully closed project.

The normal-Vulkan production-canvas fixtures add event-path and visual evidence:

- `.tools/tia-branch-close-multiopen-preview-v1.png` shows four visible paths:
  the upper main path, one existing closed lower path, and two separate dangling
  lower legs. Only the selected open leg has the dashed green route; its sibling
  remains independently open.
- `.tools/tia-branch-close-multiopen-final-v1.png`, captured at 2200 by 1200 so
  the complete rung is visible, shows the sequential nested result from the
  power rail through both inner joins, the retained outer lower leg,
  `Photoeye_clear`, coil, and termination. Original-resolution inspection found
  no crossing, gap, clipped operand, or detached conductor.

The fixtures assert `TIA_MULTIPLE_OPEN_BRANCH_SETUP_PASS ... openIssues=2` and
`TIA_MULTIPLE_OPEN_BRANCH_CLOSE_FINAL_PASS ... stableIds=true compile=true undo=true redo=true`.
The application builds with zero warnings and zero errors. The complete
controller/editor suite passes **367/367** with no real PLC transport or
connection attempt. The standard native ladder-editor and command-route gates
also remain green.

This closes the modeled four-path/two-open-leg case when both legs terminate on
the upper main path. Closing onto a non-main lower leg is not inferred from the
general Siemens prose and remains **OPEN** pending an installed-TIA capture or a
more specific Siemens example. Higher path counts, other close orders, deeper
asymmetric shapes, matched installed-TIA gesture captures, project interchange,
and live PLC execution also remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 visible physical double-click route for the TIA project tree

The earlier headless pointer probe was invalid because its project tree laid out
at only 28 by 24 pixels. A dedicated normal-Vulkan verifier now opens the real
ladder workspace at 2048 by 1152, waits for the containers to settle, resolves
the exact visible row rectangle, and routes left-button press/release events
through `Viewport.PushInput`. It does not emit `ItemActivated` and does not add
a second production input handler.

The gate creates an in-memory FC fixture and proves that a first physical click
selects its 220 by 31 pixel row without opening it. The second press carries the
native double-click flag and reaches the existing `Tree.ItemActivated` handler,
which opens the FC in the central editor. The same negative-selection and
physical-double-click sequence opens each supported creation command dialog:
`Add new block`, `Add new tag table`, and `Add new watch table`. Each dialog is
closed without creating an object. The run reports:

`TIA_PROJECT_TREE_POINTER_VERIFY PASS singleSelected=True singleStayed=True doubleOpened=True commands=True/True/True objects=True/True/True/True finalSelected=True finalTitle=True focus=True tree=240x715 row=220x31 route=Viewport.PushInput`

The gate also applies that physical sequence to the default tag table, default
watch table, a process-local optimized Global DB, and a process-local IEC
counter instance DB. Each single click must leave its document/workspace closed;
each double-click must open the correct central document or watch workspace.

The expanded capture initially exposed a stale secondary title after returning
from an instance DB to the already-active FC. `ActivateEditorDocument` now
refreshes block chrome even when the block index itself does not change, and the
standard instance-DB return route plus this pointer gate require the block name
to replace the old DB title.

Fresh normal-Vulkan capture `.tools/tia-project-tree-pointer-v5.png` was
inspected at original resolution. It shows the selected FC row, the matching FC
document tab and declaration workspace, the complete project-tree command rows,
and no obscuring modal. The fixture is process-local; it does not write a project
file or attempt controller execution.

The application builds with zero warnings and zero errors. This closes the
RungProof physical pointer double-click gap for supported program-block, tag-
table, watch-table, optimized Global-DB, and generated instance-DB rows plus the
three supported project-tree creation commands. A matched installed-TIA
pointer/layout comparison remains **OPEN**. Unsupported DB/access
modes, runtime DB-member addressing, multi-instance/parameter-instance storage,
project interchange, and live PLC execution also remain **OPEN**. Overall TIA
parity remains **OPEN**.

### 2026-10-15 universal Siemens-facing renderer sweep

A single production-canvas verifier now covers the exact Siemens-facing catalog
set instead of relying on scattered instruction fixtures. It subtracts only the
four explicitly local keys (`counter-load`, `counter-reset`, `compare-6`, and
`numeric-24`) from the 54-key catalog and fails closed if the resulting set is
not exactly 50 keys. Every representative document must compile before it is
rendered.

`--verify-tia-renderer-sweep` executes 900 states: 50 instructions at 75, 100,
and 150 percent zoom, selected and unselected, under three representative TIA
operand/tag-information configurations. It uses the production layout and
selection APIs to verify stable selection identity, network containment,
measurable editor-field separation, input/output conductor exposure, and zero
terminal/body endpoint clearance. The CALL case is a parameterized FB with
Input, Output, and InOut bindings and an assigned instance DB. The RT case
shares the owning timer's instance DB rather than using an orphan fixture.

The sweep found two cross-family defects. EXPT and CONV conductor endpoints
used the ordinary numeric half-width even though those bodies are wider.
NORM_X and SCALE_X placed three 22-pixel editor regions on a 20-pixel pitch.
The production renderer now uses the true EXPT/CONV half-widths and a 28-pixel
NORM_X/SCALE_X input pitch within a 144-pixel body. The final focused run reports:

`TIA_RENDERER_SWEEP_PASS keys=50 cases=900 externalTia=false`

The consolidated interaction profile passes all nine commands, including all
368 controller/editor tests and the new renderer sweep. Its renderer record maps
to exactly 100 unique evidence-registry gates: offline and selected for each of
the 50 keys. The result explicitly records `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

The visual profile was regenerated after the shared geometry fixes. Its
1600-by-900 nested-network capture was inspected at original resolution: branch
joins remain continuous, the positive-edge memory operand is present and
unclipped, the selected subtree remains bounded, and the coil and termination
remain connected.

This is broad local offline renderer consistency evidence. It is not matched
installed-TIA evidence, it does not render online-only value chips, and it does
not prove live PLC behavior, Siemens project interchange, or commissioning.
Compact family contact sheets and matched installed-TIA captures remain
**OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 full-catalog visual audit set and stable CALL symbols

The universal renderer command now optionally captures a deterministic visual
audit set from the production canvas. For each of the 50 Siemens-facing keys it
retains a selected, symbolic-plus-absolute, tag-information-on-operand detail at
75, 100, and 150 percent zoom. The resulting 150 1600-by-520 detail PNGs are
indexed into 24 family contact sheets and a SHA-256 manifest. Generation fails
closed on duplicate or missing key/zoom pairs, unexpected detail or sheet
counts, missing files, and save failures. The contact sheets are overview and
navigation artifacts; they do not replace original-resolution detail review.
The manifest therefore remains `humanReviewStatus=pending`.

Original-resolution inspection exposed a cross-cutting CALL presentation bug.
Parameterized bindings correctly stored explicit stable references such as
`global:<id>`, but the renderer exposed that internal identity and then emitted
a red `???` because address lookup only understood display names. The common TIA
operand path now resolves explicit global and local identities to user-facing
symbols for display and tag information while leaving the stored reference
unchanged for validation and execution. A fail-closed sweep assertion requires
the representative CALL input to render as `enable`, `%I0.0` in combined mode,
and forbids any visible line beginning with `global:`.

The regenerated 100-percent CALL detail was inspected at original resolution.
It shows `enable`, `bool_memory`, and `bool_memory_io` with `%I0.0`, `%M0.0`, and
`%M0.1`, their comments, `ReviewFB`, assigned `%DB1` instance DB, and separate
Input, Output, and InOut pin rows. No internal binding ID or false missing-
address marker remains.

The consolidated full profile at `.tools/tia-parity-full-site-audit.json`
passed all ten commands in 132 seconds: zero-warning build, all 368
controller/editor tests, transaction/editor routes, physical operand and
project-tree routes, empty-box and view-state checks, all 900 renderer states,
150 detail captures, 24 contact sheets, and the nested zoom review. The result
records `offlineOnly=true`, `installedTiaExecuted=false`, and
`livePlcExecuted=false`.

Review of the remaining original-resolution instruction details, content-level
assertions beyond geometry, physical Apply/Cancel/Undo/Redo matrices, project-
tree mutation and FC/FB drag-to-CALL, online monitor-state sweeps, durable
registry artifact hashes, matched installed-TIA captures, interchange, and live
PLC execution remain **OPEN**. Overall TIA parity remains **OPEN**.

### 2026-10-15 atomic project-tree FC/FB CALL authoring

A site-wide interaction audit found a shared transaction-boundary defect rather
than an instruction-specific rendering issue. The project-tree FC/FB drop
handler opened a live document edit and converted the terminal into a partial
CALL before opening the detached authoring window. Cancel could therefore leave
the partial mutation in the project. Apply could also span two history entries,
so a single Undo was not guaranteed to restore the original document.

The production route now uses
`LadderCallAuthoring.CreateForDroppedBlock(...)`. It validates a parameterized,
non-self FC/FB target and stages terminal conversion, target selection,
bindings, and instance data entirely inside the detached draft. The live
document changes only when the form's Apply handler commits the draft. The
existing refresh and selection callback runs after that one commit.

The focused `--verify-call-authoring` gate reports:

`CALL_AUTHORING staged=True invalidRejected=True applied=True persisted=True undo=True cancelled=True dropStaged=True dropCancelExact=True dropApplied=True dropUndoExact=True dropRedoExact=True dropError=`

The expanded project-tree gate also resolves the actual FC row, constructs the
same production payload used by tree drag, calls the production canvas
acceptance/drop route at the visible terminal, and drives the real Cancel and
Apply button handlers. It reports:

`TIA_PROJECT_TREE_POINTER_VERIFY PASS singleSelected=True singleStayed=True doubleOpened=True commands=True/True/True objects=True/True/True/True callDrag=True/True/True/True/True/True finalSelected=True finalTitle=True focus=True tree=240x463 row=208x31 route=Viewport.PushInput+TreePayloadCanvasDrop`

The `callDrag` fields prove open-for-Cancel, exact Cancel, open-for-Apply,
applied CALL, exact one-step Undo, and exact one-step Redo. Rung and terminal
identities are retained. This route intentionally does not claim OS-level drag
gesture evidence: headless Windows/Godot synthetic `MouseMotion.Relative`
coordinates diverged from viewport-space control geometry. That limitation is
now explicit instead of being hidden behind a false physical-input claim.

The consolidated interaction run at
`.tools/tia-parity-interaction-atomic-call-tree-drop.json` passes all ten
commands, including all 368 controller/editor tests and all 900 renderer cases.
It is local offline evidence only: `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

The remaining batch target is one table-driven transaction matrix for the 12
non-CALL property families: physical open, invalid Apply with no mutation,
exact Cancel, valid Apply with stable IDs, one-step Undo/Redo, and reopen of the
committed value. Manual OS-level FC/FB drag, matched installed-TIA behavior,
project interchange, and live PLC execution remain **OPEN**. Overall TIA parity
remains **OPEN**.

### 2026-10-15 all-family property transaction matrix

The physical property-form verifier now applies one transaction protocol to
every supported non-CALL family instead of treating form opening as sufficient
evidence. The 12 rows are contact, comparator, coil, timer, RT, counter, LD,
counter reset, numeric, RET, JMP, and LABEL. Each row physically opens the
selected instruction's production form, then verifies:

- invalid Apply preserves the byte-identical serialized document and leaves
  Undo empty;
- Cancel closes the form with the same exact document and stable identity;
- valid Apply commits the targeted value and creates one history entry;
- the contact/comparator node, terminal action, or network-label ID remains
  stable as appropriate;
- one Undo restores the byte-identical baseline and exhausts Undo;
- one Redo restores the byte-identical applied document;
- reopening displays the committed value, and Cancel after reopen is exact.

The first execution found a shared production defect. `ApplyProperties` routed
compiler errors back to output forms only when the editable rung already held a
nonempty terminal action ID. Several fresh editor drafts derive that ID during
program export, so invalid symbolic TON PT, CTU reset, numeric source, and RET
operands produced compiler diagnostics but the form missed them and committed.
The validation route now accepts a diagnostic captured to the edited output
network even when the draft's terminal ID is empty. Contact and comparator
forms remain restricted to their exact selected tree node.

The audit fixture was also corrected to use separate CTU and CTD instance
owners, reuse the CTD owner for LD, and use TIA-valid alphanumeric label names.
This removes unrelated instance-DB and label-grammar failures from the matrix;
it does not weaken those validators.

The focused native run reports:

`PROPERTY_TRANSACTION_MATRIX_VERIFY PASS cases=[contact:True,compare:True,coil:True,timer:True,timer-reset:True,counter:True,counter-load:True,counter-reset:True,numeric:True,return:True,jump:True,label:True]`

The complete interaction result at
`.tools/tia-parity-interaction-property-transaction-matrix.json` passes all ten
commands, all 368 controller/editor tests, and the 900-case renderer sweep. It
records `offlineOnly=true`, `installedTiaExecuted=false`, and
`livePlcExecuted=false`.

This closes the local transaction-lifecycle gate for the supported property
forms. It does not prove matched installed-TIA field layout/gesture behavior,
manual OS-level input, project interchange, or live PLC execution. Those items
remain **OPEN**, and overall TIA parity remains **OPEN**.

### 2026-10-15 physical palette and project-tree mutation matrices

The physical editor audit now treats the visible mutation surface as a catalog,
not a set of hand-picked examples. One table-driven gate activates all 48
visible palette controls through the production Godot viewport. It covers
NO/NC and edge contacts, compare, branch/network/delete/empty-box, coil/set/
reset, TON/TOF/TP/TONR/RT, CTU/CTD, MOVE, arithmetic, math, conversion and
scaling boxes, RETURN/JMP/LABEL, plus the three helpers whose parent palette is
explicitly named `RungProof local (not Siemens)`.

For every row the gate captures the exact serialized baseline, establishes the
required insertion or selected element, physically clicks the visible button,
checks the intended mutation and stable rung identity, requires exactly one
history entry, proves a byte-exact one-step Undo and Redo, and cleans up with one
final exact Undo. Generic CALL, assignment, and remove-branch helpers remain
hidden/internal and are not counted as Siemens-facing palette controls. The
focused result is:

`TIA_PALETTE_MUTATION_MATRIX_VERIFY PASS cases=48 localSeparated=True`

The project-tree gate applies the same mutation contract to the three supported
creation commands. It physically double-clicks the visible `Add new block`,
`Add new tag table`, and `Add new watch table` rows; verifies that blank names
leave the dialog open, show an error, preserve byte-identical JSON, and leave
Undo empty; then creates `AuditFB99`, `AuditTagTable99`, and
`AuditWatchTable99`. Each object and its tree row must exist after Apply, vanish
after exactly one Undo, return after exactly one Redo, and be removed by the
cleanup Undo. The focused results are:

`TIA_PROJECT_TREE_MUTATION_DETAIL name=AuditFB99 pass=True invalid=True create=True undo=True redo=True cleanup=True`

`TIA_PROJECT_TREE_MUTATION_DETAIL name=AuditTagTable99 pass=True invalid=True create=True undo=True redo=True cleanup=True`

`TIA_PROJECT_TREE_MUTATION_DETAIL name=AuditWatchTable99 pass=True invalid=True create=True undo=True redo=True cleanup=True`

`TIA_PROJECT_TREE_MUTATION_MATRIX_VERIFY PASS block=True tagTable=True watchTable=True`

The consolidated interaction profile at
`.tools/tia-parity-interaction-mutation-matrices.json` passes all ten commands:
zero-warning build, all 368 controller/editor tests, editor transactions,
ladder editing, atomic CALL authoring, operand placement, project-tree pointer
and mutation behavior, empty-box behavior, all 900 renderer cases, and editor
view state. It explicitly records `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

This is strong local production-route evidence, but `Viewport.PushInput` is
Godot viewport injection rather than OS-level human-input proof. It also does
not establish matched installed-TIA layout or gestures, TIA project
interchange, live PLC execution, downloading, forcing, or commissioning. Those
items remain **OPEN**, and overall TIA parity remains **OPEN**.

### 2026-10-15 complete renderer-catalog visual review

The local visual lane now has a complete reviewed artifact set instead of
instruction spot checks. The production renderer generated 150 original
1600x1080 details: all 50 Siemens-facing catalog keys at 75, 100, and 150
percent zoom in selected, symbolic-plus-absolute, tag-information-on-operands
state. It also generated 24 family contact sheets and a SHA-256 manifest.

Two bounded independent reviewers split the catalog 25/25. Together they
inspected every original detail and every contact sheet; the lead rechecked the
corrected high-risk files. Recomputed hashes matched all 150 details and 24
sheets. The accepted manifest digest is
`6CF209B0427189655B3001A34F0AC8A594EA459D4CFAF93BBB11801E29ACA581`.

The review rejected two earlier generations and drove shared fixes rather than
per-instruction cropping:

- the audit no longer moves the whole production canvas, which had created
  black bands at 75 percent and removed shell/network headers at 150 percent;
- the native capture is 1600x1080, large enough to retain both networks for RT
  and JMP, with fail-closed witness and target-network containment checks;
- CALL rows use sufficient pitch for comments, symbols, and absolute addresses;
- coil, SET, RESET, and RT selection bounds include their complete external
  operand or instance-DB text;
- CTU/CTD use taller 40-pixel pin rows so secondary tag comments do not cross
  the main conductor;
- fixture titles use catalog `TiaName` values instead of internal audit keys.

The final visual profile at
`.tools/tia-parity-visual-full-viewport-v6.json` passes build, the 900-state
renderer/contact-sheet command, and bounded editor zoom review. The source-tree
`docs/tia/tia-renderer-visual-review.json` carries the separate human verdict
and exact manifest hash while the generated manifest deliberately remains
`humanReviewStatus=pending`. A new regression test enforces catalog equality,
scope, counts, hash/path structure, and the local-only boundary; the full
controller/editor suite passes 369/369.

The final consolidated full-profile result at
`.tools/tia-parity-full-reviewed-catalog.json` passes all 11 commands in 138
seconds with the same explicit local boundary: `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

The PNGs and manifest are still ignored `ephemeral-local` files. This review
does not establish installed-TIA visual equivalence, edit behavior, project
interchange, live PLC execution, downloading, forcing, or commissioning.
Those gates remain **OPEN**, and overall TIA parity remains **OPEN**.

### 2026-10-15 evidence-registry claim correction

A gate-by-gate audit found that the registry was stronger than its artifacts.
The retained v6 visual-review set contains selected-state details only. The
900-case renderer command does exercise unselected state, but automated geometry
coverage is not a human visual inspection. The registry's offline claims were
therefore reduced from `visual-inspected` to `automated-pass` for all 50
Siemens-facing keys.

The same audit found no exact per-key mapping from the existing form, palette,
and runtime tests to all 50 edit and all 50 monitor gates. Those tests remain
valuable broad regression evidence, but they cannot close a finer-grained gate
they do not enumerate. All 100 Siemens edit/monitor local statuses are now
`not-assessed`; no implementation behavior was removed or weakened.

The new `docs/tia/tia-local-run-evidence.json` provides the one-way provenance
chain from the registry to the exact full local run, 50-key/900-case renderer
coverage, reviewed artifact-set identity, and
`docs/tia/tia-renderer-visual-review.json`. It explicitly names edit and monitor
as unproved. Registry gates reference only hashed source-tree records rather
than stale unhashed `.tools` PNG paths. Vendor evidence remains independent:
49 sourced keys remain `source-only`, CALL remains `not-assessed`, and no local
artifact promotes any vendor gate to `matched`.

The controller/editor suite now validates the local evidence record, exact 100
offline/selected gate resolution, state-specific statuses and references,
source-tree file existence and SHA-256 contents, absence of direct `.tools`
gate bindings, and unchanged local-only boundaries. The suite passes 370/370.
The source tree itself remains untracked, so the records are still honestly
classified `ephemeral-local`. Exact 50-key edit and monitor sweeps are the next
local closure work. Overall TIA parity remains **OPEN**.

### 2026-10-15 exact 50-key monitor-state sweep

The new headless `--verify-tia-monitor-sweep` command closes the missing local
per-key monitor mapping without broadening the vendor claim. It executes the
exact 50 Siemens-facing registry keys in four states: evaluated false,
evaluated true, unknown from an earlier cycle, and never executed. The resulting
artifact contains 200 unique key/state cases and the full parity profile binds
the command to exactly 50 `monitor` gate IDs.

The verifier uses the compiled program, `VirtualControllerRuntime`, identity-
bearing controller publications, and the production `LadderEditorCanvas` flow
and terminal-pin projection paths. It requires exact observation ID/key,
session/load/execution identity, current-versus-retained state, Siemens flow
styles, 50-percent earlier-cycle saturation, load-boundary value clearing, and
family-specific pin projection. The exact full result
`.tools/tia-parity-full-monitor-sweep.json` passes 12/12 commands; the monitor
artifact reports 50 keys, 200 cases, `offlineOnly=true`,
`installedTiaExecuted=false`, and `livePlcExecuted=false`.

The sweep exposed a real observation defect: when a network contained both a
terminal action and a LABEL, the terminal ID could shadow the label's stable
observation. `VirtualControllerRuntime` now includes and publishes the
coexisting LABEL observation independently as `Visited`; the strengthened jump/
label regression requires both IDs and keys. All 370 controller/editor tests
pass after that correction.

All 50 Siemens monitor gates are now `automated-pass` and reference the hashed
source-tree local-run summary. They are not `automated-and-visual`, and their
vendor status is unchanged. Human-reviewed monitor pixels, matched installed-
TIA behavior, live PLC execution, and the exact 50-key physical edit sweep
remain open. Overall TIA parity therefore remains **OPEN**.

### 2026-10-15 exact 50-key physical edit sweep

The new normal-Vulkan `--verify-tia-edit-sweep` command maps the exact 50
Siemens-facing catalog keys to their 50 stable `edit` gate IDs. It opens each
target through the production canvas, uses the actual contextual form or
structural palette path, and drives Apply/Cancel plus toolbar Undo/Redo through
viewport pointer or focused keyboard input. Each case requires a valid applied
mutation, stable target identity, successful compile, file-backed byte-exact
Save/Load/Save, exact Undo and Redo, production-form reopen, and exact cleanup.

Forty-nine operand/property cases stage an invalid value and require visible
rejection with no document or history mutation before the valid transaction.
The network/rung case is structural insertion and has no malformed operand; the
artifact records `not-representable` for that one invalid-input field while still
requiring its complete applicable lifecycle. The branch gate edits an actual
contact inside the parallel subtree while retaining both structural and child
identity. The CALL gate uses the detached production modal, rejects an invalid
BOOL actual, changes the typed binding, assigns DB77 `ReviewFB_Edit_DB`, and
requires the binding, instance identity, and instance DB to survive compile,
persistence, Undo/Redo, and reopen.

The focused run reports `TIA_EDIT_GATE_SWEEP_PASS keys=50 cases=50`. The
consolidated `.tools/tia-parity-full-edit-sweep.json` result passes 13/13
commands, with 50 unique edit gate IDs and a 50-record artifact. All controller/
editor tests remain 370/370. Registry edit gates are therefore local
`automated-pass`; they are not visual-reviewed and no vendor gate is promoted.
Matched installed-TIA editing, per-key retained edit screenshots, OS-level human
input, and live PLC execution remain open. Overall TIA parity remains **OPEN**.

### 2026-10-15 evidence-contract and monitor-shutdown correction

The first edit artifact used `invalidRejected=true` for the network/rung case
while also declaring its malformed operand `not-representable`. That was an
evidence-schema contradiction, not a product edit failure. Edit artifact schema
2 now records `invalidApplicable=false` and `invalidRejected=null` for the one
structural case, while all 49 validated forms still require an actual atomic
invalid-input rejection. The artifact also enumerates the two production input
routes it exercised: `Viewport.PushInput` and `Window.PushInput:Enter`.

The first consolidated rerun exposed a separate verifier-lifecycle defect. The
monitor sweep printed its 200-case pass and wrote its artifact, then Windows
raised `0xC0000005` from `GodotObject.Finalize` because the final CanvasLayer was
queued for release after the scene tree had already been told to quit. The
verifier now queues that layer, waits two process frames, and only then exits.
The exact isolated headless command passed twice without a fatal finalizer, and
the original full sequence then passed all 13 commands. The authoritative local
result is `.tools/tia-parity-full-contract-v3.json`; its SHA-256 is
`7A51A3970D6F9A8C20220C018E76E4DA5558C38DACD77BF2669C37124AFD573D`.
This remains offline-only: installed TIA and live PLC execution are both false.
Overall TIA parity remains **OPEN**.

The earlier CALL provenance gap is now closed only at the documentation layer.
The official Siemens STEP 7 V21 LAD procedure, "Inserting block calls using a
drag-and-drop operation," confirms that existing FC/FB blocks are dragged from
the project tree into a network and that an FB opens single-, multi-, and
parameter-instance call options. The registry now records this exact V21 source,
so all 50 Siemens-facing keys are `source-only`. No vendor gate is `matched`;
installed-TIA pixels and behavior remain required.

## Unselected renderer review and CALL annotation correction

A subsequent unselected renderer review closed the remaining local offline
visual lane. The v3 normal-Vulkan set contains 150 original 1600x1080 details
and 24 contact sheets for all 50 Siemens-facing keys at 75/100/150 percent using
`SymbolicAndAbsolute`, `Show`, and `OnOperands`. All 174 manifest hashes matched.

Original-resolution review found that the CALL InOut output actual at 150
percent was intersected by the main conductor. The shared CALL renderer now
uses a 48 px formal-row pitch and anchors complete actual-annotation stacks
above or below each pin row.
`ReviewTiaCallOutputOperandRowsClearConductors` checks every CALL sweep case.
The three regenerated CALL details and three Program sheets passed at
75/100/150 percent; all unchanged unselected artifacts were byte-identical.
`docs/tia/tia-renderer-unselected-visual-review.json` records `visual-pass` and
artifact-set SHA-256
`7ECF09D337FA245F62A0C5F01BF5EBFDD7F5A22B771BA508198EFA6C223A1588`.

Because the fix is shared by selected and unselected rendering, it also changed
six selected artifacts: the CALL detail and Program sheet at each zoom. Those
six selected artifacts were re-reviewed and passed; the other 168 selected
artifacts were byte-identical. Both selected and unselected renderer gates are
therefore `automated-and-visual`. Edit and monitor remain `automated-pass`.

The new `docs/tia/editor-control-evidence-registry.json` establishes a separate
inventory for non-catalog editor behavior: seven LAD editor gates and nine
project-tree gates. All 16 of 16 remain `not-assessed` because neither declared
per-gate structured sweep exists yet. Source presence, screenshots, and the
current broad native verifiers remain useful implementation evidence but do not
satisfy those gate contracts.

Installed TIA and live PLC execution remain false, no vendor gate is matched,
and overall TIA parity remains **OPEN**.

## 2026-10-15 structured editor-control closure

The two previously declared non-catalog control sweeps are now implemented and
bound to exact JSON evidence. `tia-editor-control-gate-sweep` passes all 7 gates
and 36 assertions. `tia-project-tree-gate-sweep` passes all 9 gates and 45
assertions. The integrated v6 full profile passes 15/15 commands and validates
the exact gate-ID partitions, per-gate source/build identity, timestamps,
passing assertion arrays, and `externalTia=false` / `livePlc=false` boundaries.

The project-tree sweep exposed and corrected a real local defect: visible OB and
Global DB rows could produce a raw CALL drag payload. The visible tree source now
emits CALL payloads only for FC and FB rows, while the canvas retains its target
and block-type validation. All 16 non-catalog local gates are now
`automated-pass`. This does not change vendor status: no installed-TIA or live
PLC execution occurred, no same-state vendor comparison is bound, and overall
TIA parity remains **OPEN**.

## 2026-10-18 instance DB declaration layout correction

The latest user screenshot exposed a concrete layout mismatch: generated FB
instance DBs opened as a paragraph list instead of a declaration table. The
current view now uses a central six-column declaration grid (`Name`, `Data
type`, `Default value`, `Start value`, `Retain`, `Comment`) with Input, Output,
InOut, and Static sections. TEMP locals are excluded because they do not belong
to the instance DB. It identifies the assigned instance and
FB above the table. Siemens V21 documents that FB defaults seed the generated
instance DB's start values; when no instance-specific value is supplied, the
default is used at startup. The renderer therefore shows the inherited value
in both `Default value` and `Start value`, with the start value visually muted
and a tooltip identifying it as inherited. The model still does not support
editing and persisting a per-instance override, so the read-only boundary says
so explicitly. See Siemens V21's [data block declaration table](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/structure-of-the-declaration-table-for-data-blocks)
and [start-value rules](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/defining-start-values/basic-information-on-start-values).

The dedicated project-tree gate now also constructs an FB-backed instance and
checks section/member rows, the separate Default/Start columns, the inherited
start value and its disclosure, the comment, absence of TEMP locals, read-only
disclosure, and return to the owner. That exact
`tia.project-tree.instance-db-contract` gate passed. A
normal-renderer capture at `.tools/instance-db-table-review.png` was visually
inspected for grouping, contrast, and column fit. Build passed with zero
warnings/errors; the controller/editor suite passed 372/372; and
`--verify-app-shell` passed for 77 scenes and five demos.

The first full `--verify-tia-project-tree-gates` run exposed a stale pointer-
verifier assertion: it looked for the DB name in the subtitle from the former
text-only document. The production view put the DB name in its new heading.
After updating that assertion, the full verifier passed all 9 gates and 45
assertions, including physical pointer-injected activation, creation,
navigation, CALL drag/drop, Undo/Redo, and the instance-DB table. This is
Godot viewport input evidence, not OS-level human interaction or installed-TIA
evidence. A separate CUA retry could not bind to the running RungProof window:
the session exposed no native apps and has no `listApps`/`listWindows` API. The
capture is a local renderer artifact, not a manual screen-control walkthrough
or installed-TIA comparison.

Independent source review also confirmed the existing limits: Global DB is a
five-column supported subset; instance DB start-value overrides, online monitor
values, offsets and extended DB attributes are absent; PLC tag tables omit
TIA's User/System constants views; and block metadata remains narrower than
TIA. Keep those items in the partial/open scope. Overall main-screen and
property-management parity remains **OPEN**.

## 2026-10-18 Global DB edit isolation and property requirements

The failed Global DB edit assertion was first traced to the verifier selecting
one child too deep in a hidden-root declaration tree. Correcting the row lookup
made the edit path executable and exposed a separate production risk found by
independent code review: the selected declaration was stored only as an array
index, so changing the active block could leave an edit form that targeted the
same row number in another block. The form now records its owning block and
declaration name, clears selection on block changes, and rejects stale targets.
The gate selects an existing declaration, edits Name/Start/Retain/Comment,
checks exact Undo/Redo, selects that declaration again, activates a second DB
with a declaration at the same row index, and proves the second DB declaration
remains byte-identical after an Apply attempt. Switching into a DB also leaves
the disabled declaration-section selector on `STATIC`.

The first rerun then found that the suite aggregator still required exactly five
assertions per gate. It now checks each gate against its declared requirement
count, so adding coverage does not produce a false top-level failure. Build
passed with zero warnings/errors. The current
`--verify-tia-project-tree-gates` artifact passes 9/9 gates and 47/47
assertions, with `externalTia=false` and `livePlc=false`. The no-reselection
Apply attempt is a no-op and the selector remains `STATIC`. Godot still logs
renderer/ObjectDB leak warnings during headless shutdown; the command exits 0
and the structured evidence is passing, but those shutdown warnings remain.

`TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md` now defines the full main-workbench,
property-editor, DB, PLC-tag/constants, watch-table, interaction, and matched
TIA V21 acceptance scope. The batch plan orders the implementation lanes and
the external evidence gates. Current local gaps remain expanded Global DB
columns/access modes, per-instance start overrides and online monitor values,
PLC User/System constants and tag attributes, watch-table addresses/display
formats/modify values, full screen coverage across every supported property
surface, and matched installed-TIA V21 captures. Overall parity remains
**OPEN**.

## 2026-10-18 Global DB screen route and OS-level walkthrough

The CUA native-window integration remains unavailable: its inventory exposes
no native applications and its runtime has no native app/window discovery
methods. Under the user's explicit authorization, a temporary loopback browser
bridge now mirrors the Godot window using `PrintWindow` and sends targeted
window messages for clicks and keys. CUA Chrome can use the live screen through
that page. This is an input/screen bridge to the native app, not a browser build
of RungProof; the listener binds only to `127.0.0.1` and the helper is stored in
ignored `.tools`.

The walkthrough exposed a production navigation defect: Global DB told users to
use the Project inspector, but Siemens mode hides the Project tabs, so the
existing member form had no discoverable route. The DB screen now has an
`EDIT MEMBERS` action that switches the left dock to `Project objects`, expands
that dock to a usable width, and changes to `PROJECT TREE` for the return action.
The route captures the live splitter position when the dock is visible (falling
back to its saved position when collapsed), then restores that exact width on
return. An independent code review confirmed this path has no remaining
actionable defect. The offline project-tree gate starts at 240 px, verifies the
member form is visible after widening to 280 px or more, and verifies the exact
240 px width is restored.

On the live desktop, I created a Global DB through the corrected creation
dialog, opened the member form, added `WalkthroughFlag : BOOL := FALSE`, selected
it, changed its comment and Retain value, and returned to the tree. The DB grid
showed the resulting declaration and updated values. A fresh visible Windows
walkthrough also created a DB, opened the member form at its widened dock width,
and returned to the declaration grid with the original dock width restored.
The browser bridge was then used from CUA to open the member form, enter
`CUAProbe2`, add it, and verify that it appeared in the central DB grid. This
confirms an actual browser-mediated screen-control path despite the missing
native CUA app API. The gate passes
9/9 gates and 48/48 assertions; the virtual-controller suite passes 372/372;
the app-shell verifier passes 77 scenes, 3 groups, and 5 demos. The normal
renderer, OS mouse walkthrough, and browser-mediated CUA walkthrough are
distinct evidence. The scratch session is unsaved and remains open for
inspection.

The Global DB gate's `fields-persist` result means an edit renders and survives
the gate's in-memory Undo/Redo snapshots. It does not yet cover duplicate
declaration rejection or a project-file save/reopen cycle, both required by
PROP-03; treat the gate as partial for those criteria. A reviewer reported a
355 px width fixture, but the current checked-in gate source sets and verifies
a 240 px starting/restored width, so the existing 240 px audit description
matches the source.

The creation dialog received a fixed geometry and DB-specific resize behavior
after the screen walkthrough showed the action row below the window. The
independent reviewer caught a reopen sizing edge case and a centering concern;
the code resets its size on each open and recenters on type change. The current
live DB screen visibly fits its OK/Cancel actions. Small-window and high-DPI
limits still need viewport-relative verification. No matched installed-TIA
capture was made; overall parity remains **OPEN**.

The browser bridge was subsequently used to exercise the visible main-screen
controls in the offline Conveyor Inspection Cell. Return to Scene, built-in
Run/Stop, conveyor Start, simulated E-stop, blocked Start while E-stop was
latched, and E-stop Reset all produced visible status/history or point changes.
The E-stop changed `estop_ok` false, prevented a machine Start, and reset to
true while leaving the built-in controller stopped. This confirms the local
scene action flow only; it does not prove the ladder program owns these
decisions, safety-rated behavior, or installed-TIA parity. The final stopped
capture is `.tools/cua-walkthrough-main-controls.png`. The broad property and
watch-table walkthrough remains open.

The live screen walkthrough then used the Project Tree to select Watch table 1
and double-click it. A single selection left DB1 active; the double-click
opened the watch-table workspace, and selecting `start_command` highlighted
the row without changing its value. The screen exposes Symbol, Type, Value,
Role, Quality/State, and Scene Binding; address/display-format/modify-value
controls were not exercised. The screenshot is `.tools/cua-walkthrough-watch-table.png`.

On 2026-10-18, the temporary bridge was reconnected after the earlier CUA
native-app attempts failed. Its image reported `connected · 1616×939`; clicks
on the built-in Run and Stop controls visibly changed the offline runtime
between RUNNING and STOPPED while the scan advanced. Clicking `+ TABLE` opened
the named-table dialog, and Escape canceled it without committing. The symbol
selector was opened and `stop_command` selected, but the available screenshots
disagreed on whether the table contained four or five symbols. Therefore the
`+ SYMBOL` attempt and count are inconclusive; no exact row set is accepted.
An independent screen-control agent separately confirmed Return to Scene,
Logic Editor, DB1 → Edit Members, and opening/canceling the Add Watch Table
dialog. Default tag table was already open as a tab. Project Tree
collapse/expand and splitter resizing remain unverified because some
coordinate clicks selected adjacent rows/properties. The agent made no field
edits and did not use Save/Load. These observations verify a local bridge input
route and visible responses, but do not repair native CUA app discovery or
prove installed-TIA behavior. Persistent project mutation remains untested.

The post-walkthrough screen then showed an empty active watch table. The Undo
status identified `ADD WATCH TABLE`; applying Undo restored a table showing
four symbols. This indicates that a table-creation action was committed during
the coordinate walkthrough despite the agent's interpretation that Escape had
canceled the dialog. The active table/count do not match earlier screenshots,
so the scratch table state is inconclusive. Nothing was saved to the project.

The local bridge helper was then revised to serialize input and screenshot
refresh, remove duplicate click dispatch from browser pointer down/up, and
provide an explicit drag gesture. PowerShell parsing passed. Through the
restarted bridge, Run/Stop again visibly toggled the offline runtime. The
Global DB `EDIT MEMBERS` and `PROJECT TREE` actions were visibly exercised in
both directions. Enter opened the Default tag table; its Name, Address,
Comment, Type, Role, Initial, and Binding columns and selected-tag properties
were visible. The Add new tag table dialog was opened and its visible Cancel
button clicked; it closed without creating an item.

A screen drag widened the Project Tree dock by roughly 60 px, and the reverse
drag returned it to approximately the starting width. This confirms splitter
input and response in the running app; restoration after a resize performed
inside the DB member-editor route remains open. No property field was edited
and no Save/Load was used. Current scratch session remains unsaved, DB1 remains
open, the tag-table tab is active, the runtime is stopped, and PLC status is
disconnected. Exact Watch-table symbol count stays inconclusive because the
walkthrough screenshots disagreed; current visible UI reports four.

The user-directed CUA workaround is now enacted for the current workstation
session: Chrome reads a loopback-only bridge page that mirrors the Godot
window and forwards window messages. This bypasses the empty native-app
inventory without changing RungProof's network exposure. Return to Scene and
Logic Editor were visibly navigated in both directions. The browser bridge
requires coordinate values to be entered and verified in one step, then
clicked in a later step; combining fill and click sometimes dispatched the
previous target. Scene-panel hit coordinates also differed from the apparent
image-relative location, so a click is accepted as evidence only when the
resulting history/point/runtime state changes.

During this live check, the simulator E-stop visibly set `estop_ok` false,
blocked Start until explicit E-stop reset, then reset to true. The local
controller was started and stopped; the final state is STOPPED, E-stop reset,
PLC DISCONNECTED, and the project remains UNSAVED. A scene action was logged
as `conveyor_run` and cartons appeared at a different position while the
visible PLC→Simulator table showed `conveyor_run BOOL False`. The observation
was not synchronized to a scan/output capture, so it does not prove an
ownership bypass or stale display. Independent source tracing found that the
table reads current runtime points, virtual outputs are mapped and committed
before its state refresh, and conveyor-cell motion requires
`conveyor_run && estop_ok`. The history message means the command request was
accepted; cartons may have moved while the output was true and stopped before
the screenshot. A separate source caveat remains: `RunDefault()` directly
sets `conveyor_run` true for generic conveyor scenes. No evidence ties that
entry point to this named command. Keep the ownership question open until a
synchronized scan/output/point/carton-position capture resolves it. No fields
were edited and no Save/Load was used. The bridge remains temporary,
local-only, and tied to the active workstation session.

The independent screen-control agent retried the full visible route through
the same bridge. It confirmed both Logic Editor↔Scene navigation, DB1→Edit
Members and return, the Default tag table, and Add Watch Table followed by
Cancel. Runtime Run, Stop, E-stop, and reset each produced visible history or
point changes, but some required a second click and the agent reported that
repeats could land on neighboring controls. It therefore did not claim
Project Tree collapse/expand, splitter resize, or its final Return-to-Scene
attempt. Final observed safety state was STOPPED, `estop_ok=true`, PLC
DISCONNECTED, and UNSAVED; the editor was left visible after a canceled watch
dialog. Do not treat repeated clicks as a general workaround until the target
and state are rechecked, because duplicate actions may toggle state twice.

## 2026-10-18 CUA session workaround and IEC timer DB schema

The native-app inventory still returns no applications, but the browser
inventory exposes the active Chrome tab `RungProof CUA Screen Bridge`. Reusing
that existing session-local bridge returned a live 1616×939 screenshot of the
running Godot window and a clickable browser control surface. This resolves the
walkthrough exposure blocker for this workstation/session; it does not add a
native-app API or change RungProof's network exposure. The browser bridge
session must remain available while screen-control work continues.

Review against Siemens STEP 7 V21 found the timer instance DB renderer showed
only `Q` and `ET`. The V21 IEC_TIMER system type declares `PT`, `ET`, `IN`, and
`Q`. The renderer now lists all four in that order, and the existing
`tia.project-tree.instance-db-contract` gate checks their types and row order.
The instance-view footer now names omitted capabilities: per-instance start
overrides, standard-access offsets, monitor/snapshot values, HMI attributes,
setpoints, and supervision. Siemens docs: [declaration table](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/structure-of-the-declaration-table-for-data-blocks)
and [system data types](https://docs.tia.siemens.cloud/r/en-us/v21/data-types/system-data-types/system-data-types).

Independent code review found three coverage/reporting gaps. The focused gate
now asserts exactly four IEC timer rows, first/last member, sequence, and types;
asserts every capability named by the read-only footer; and describes timer /
counter checks as member schema only because the offline renderer does not
model their start values or Retain state. V21 documentation confirms the
IEC_TIMER members and TIME types, but does not establish that TIA groups the
members beneath `STATIC`; the current view's grouping stays a pending matched
screen comparison.

The previously unavailable screen-control route is confirmed active: CUA's
native-app inventory remains empty, but Chrome exposes the current
`RungProof CUA Screen Bridge` tab. Reusing it returned the running application
at 1616×939. The earlier native-window attempt failed at CUA surface discovery,
not because the app was closed or because user authorization was missing.

After the IEC_TIMER declaration correction, the normal renderer was launched
with `--app-shell --shell-view=ladder --capture-instance-db-definition` and
captured `.tools/instance-db-table-review-cua-fix.png`. Visual inspection at
1600×900 confirms the X_Axis_DB heading and owner, six declaration columns,
Input/Output/Static grouping, inherited default/start values, comments, and the
read-only capability boundary are visible and unclipped. The focused
`--verify-tia-project-tree-gates` run passed 9 gates / 48 assertions, including
PT/ET/IN/Q type and order. `--verify-app-shell` passed for 77 scenes, 5 demos,
and 295 assets. These are RungProof implementation/renderer checks; no matched
installed-TIA capture is available, so visual parity remains open.

The independent screen agent's final pass confirms the bridge control surface
remains available, but reports unreliable placement for typed pointer targets:
attempted tree collapse/expand, splitter resize, and scene-nav coordinates hit
other controls and were not accepted as proof. The browser page exposes the
screen image at CSS origin (152,45), size 1616×939, while the bridge's x/y
fields accept image-local coordinates. This coordinate-origin difference is a
likely source of the misses; direct typed targets must be derived from the
image bounds and verified after each action. A single semantic route from Scene
back to Logic Editor was confirmed with the bridge button. The session was left
UNSAVED, STOPPED, PLC DISCONNECTED; no Save/Load or declaration/tag edits were
made. Do not treat tree/splitter interaction as verified until a correct
image-local click or resize produces a visible state change.

The independent code review confirmed the timer member schema and found no
remaining actionable issue in the correction. IEC timer/counter value and
retentivity behavior remain unmodeled, and exact IEC DB grouping remains open
pending matched TIA evidence.

## 2026-10-18 follow-up: live scene prerequisite and splitter interaction

The active CUA screen-control route is Chrome → the existing local-only
`RungProof CUA Screen Bridge`; native-app enumeration remains empty. A direct
screen click on the Conveyor Inspection Cell Start/Stop action while the local
runtime was STOPPED produced the visible event/workspace message `Action
blocked · Run the controller before issuing a machine command`. The local
point table remained `conveyor_run = FALSE`, `estop_ok = TRUE`. This resolves
the reported dead-button symptom for this stopped/offline state: the scene
command is live but requires the controller to be running. It does not prove
that ladder logic owns the action. A prior independent screen walkthrough
separately verified offline Run/Stop and simulated E-stop latch/reset. Runtime
was left STOPPED, PLC DISCONNECTED, with E-stop reset; no physical PLC or
safety-rated behavior was exercised.

A targeted Project Tree splitter drag from its measured grab strip widened the
dock by about 60 px; the reverse drag restored the prior width. The first drag
missed the grab strip, and later uncalibrated clicks selected nearby rows; only
the visible successful resize/reversal counts. Collapse/reopen remains
unverified. The current browser bridge provides a workaround for this
workstation's missing native CUA surface, not a portable native-window API.
Property-form acceptance remains incomplete, and the current-screen tests do
not change the existing UNSAVED project state or establish installed-TIA V21
parity.

## 2026-10-18: CUA bridge recovery and transactional DB member form

The previous CUA block was caused by binding the wrong session: native app
enumeration returned no window, the Chrome extension tab was already owned by
another CUA session, and the current task's separate local bridge key returned
403. A fresh in-app browser tab using the existing authorized local bridge
connected to the running Godot window at 1616×939. This restores a practical
screen-control route on this workstation without establishing general native
window discovery.

The independent screen agent verified Run/Stop, Conveyor Start, simulated
E-stop and reset, controller Reset, Scene/Logic Editor navigation, Project
Tree collapse/reopen and splitter resize/reversal, DB member navigation,
existing tag selection, and Add Tag Table / Add Watch Table cancellation. The
final state was STOPPED, `estop_ok=true`, `conveyor_run=false`, PLC disconnected,
scan 0, and project UNSAVED. Tag/watch counts remained 5/4 after cancellation.
No member/tag data was edited and no Save/Load was used. These observations
cover the offline simulator UI/model only.

Read-only code review found an unsaved Global DB member draft could be cleared
by New without applying, discarding, or canceling, and its action labels said
parameter. It also found the labels/defaults could remain stale on initial DB
entry and DB↔FB navigation. New now uses the common pending-form resolver;
DB actions use member-specific wording and the form resets against its owner
block. The gate exercises Cancel-preserves-draft and Discard-clears-draft.
The member form now has an explicit Cancel action and type-aware initial-value
defaults for a new declaration when its type changes. A fresh normal-renderer
capture at `.tools/instance-db-table-review-db101.png` shows `X_Axis_DB [DB101]`
assigned to `FB_AxisDrive [FB]` with a six-column grouped declaration grid. This
matches the current implementation, while the earlier attached text-only
“Interface storage” image does not; the live CUA walkthrough was on a Global DB,
so it did not establish the instance view by direct interaction. The project
tree gate now passes 9 gates / 52 assertions. The build
passed with 0 warnings/errors, app-shell verification
passed for 77 scenes / 5 demos / 295 assets, and virtual-controller tests pass
372/372. A follow-up review of this patch is pending. TIA V21 matched screen
evidence, remaining DB/tag/watch property workflows, and a lower-level direct
scene-action E-stop invariant review are still open.

### Screen-control route clarification

A fresh CUA inventory still returned no bindable native app, but it exposed the
existing Chrome extension tab titled `RungProof CUA Screen Bridge`. Binding that
Chrome tab—not the ChatGPT in-app browser tab—showed the connected native-window
mirror at 1616×939. A coordinate click through the bridge visibly opened the
Project Objects member editor from the Global DB view. This establishes that
the previous walkthrough failure was a session/browser-target selection issue,
not proof that RungProof was absent or that screen control is impossible. The
currently open app is an older, UNSAVED build and still shows the pre-Cancel
member form; it was left open and unsaved. This bridge proves this workstation's
input route only. It does not prove the new source revision's form behavior,
installed-TIA parity, or PLC/safety behavior.

The follow-up review then found that a Retain-only checkbox toggle did not mark
the member form dirty and that ENUM's blank start value could not pass its
declaration parser. Both are fixed: checkbox toggles now participate in pending
form resolution, and choosing ENUM seeds `Value1` in both the member domain and
initial value when no domain exists (or selects the first existing member).
The production-path sweep now checks both cases and passes 9 gates / 57
assertions. The live older app remains untouched; these UI handlers are verified
through the current-source gate only.

The same review then identified a Global DB validation gap when changing an
ENUM's domain after seeding its first member. The form now follows the first
domain member while the start value is untouched, validates identifier count,
uniqueness, and start-value membership before committing, and leaves document
and Undo state unchanged on invalid input. The gate covers a valid domain edit,
Undo, and invalid-domain rejection. Latest project-tree result: 9 gates / 57
assertions; editor-control registry total: 16 gates / 93 assertions.
The independent follow-up review found no remaining P1/P2 issue in this member-
form flow. This is a scoped code review, not a claim that the remaining TIA
property-management work or other application paths have no defects.

## 2026-10-19 follow-up: active editor document and DB rendering

The earlier editor-layout/operand sweep failures were caused by test setup
leaving a DB or tag document active after replacing the ladder program. The
production document-body switch correctly hides the ladder canvas while a
non-block document is active. The review harness now activates the current
program-block tab by stable block identity and checks that the routine viewport
is visible and large enough before running pointer routes. The fresh
`--verify-editor-transactions` run passes 58 routes. The fresh
`--verify-operand-placement` run passes at 1600×900, including contact/coil,
timer/counter/numeric/comparison pins, all supported type selectors, 13
property forms, 12 property transactions, and 48 palette mutations. This fixes
the false negative in the review route; it does not establish exact TIA parity.

The compact 900 px layout now uses a 112 px instruction palette minimum, an
8 px gap above the diagnostics/Scene I/O dock, and a 44 px block-interface
summary minimum. Current-source normal-renderer captures were inspected at
1600×900: `.tools/tia-parity-current-review/operand-placement-current.png`,
`.tools/tia-parity-current-review/instance-db-current.png`, and
`.tools/tia-parity-current-review/global-db-current.png`. The instance view
shows Name, Data type, Default value, Start value, Retain, and Comment, grouped
by Input/Output/Static; the Global DB view shows the supported editable
Name/Data type/Start value/Retain/Comment subset. The focused project-tree
gate passes 12 gates / 82 assertions, including block, tag-table, watch-table,
Global DB, and instance-DB navigation and property contracts. An independent
read-only source review found no P1/P2 issue in the layout or verifier changes.

These renderer captures and automated gates establish current RungProof
behavior only. Exact installed-TIA V21 comparison remains OPEN. The model does
not provide all optional TIA declaration columns, standard-access offsets, all
DB families/access modes, online monitor/snapshot and HMI attributes, or
instance-specific override workflows. A separate screen-control retry could
read the active UI and navigate between Scene and Logic Editor, but coordinate
misrouting prevented reliable DB/tag edits, cancel/apply, and save/reopen; those
functions remain unverified by a human-visible screen walkthrough. No project
data was saved through that retry, and no external TIA or live PLC was used.
