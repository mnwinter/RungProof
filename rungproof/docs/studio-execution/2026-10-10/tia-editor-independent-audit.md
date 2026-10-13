# Independent TIA-style editor audit

Audit date: 2026-10-11. Scope: current dirty working source in `rungproof-next`; read-only source/documentation inspection, with this report as the only authored file. No build, application launch, UI focus change, PLC connection, or visual acceptance was performed. References below use source line positions at inspection; concurrent edits may move them.

## Conclusion

Improve the actions available at the selected instruction, operand, wire, and network before adding another large instruction palette. RungProof already implements substantial editing and troubleshooting functions, but users must leave the diagram to reach many of them. Its largest semantic limits are the flat parallel-branch network model and target-only block calls. A TIA-colored interface does not establish Siemens execution semantics.

This is a proposal for a scoped design decision, not implementation approval or proof of working rendered interactions. I applied the `plc-tia-wincc` skill and read repository AGENTS, context, project information, README, AI handoff, and demo release review. Existing changes were preserved.

## Authoritative comparison baseline

Baseline is STEP 7 LAD for S7-1200/1500. Siemens help resolved most detailed editor pages to **V21 (11/2025)**; those claims are explicitly V21, not asserted as exact V20 menu parity. V20 references are identified individually. No third-party tutorial is used as behavioral authority.

* [S1: STEP 7 V21 keyboard operations](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/using-the-keyboard-in-the-program-editor): operand editing, definition/use navigation, insertion, and separate open/close branch commands. Useful subset: F2 edits an operand, Ctrl+R inserts a network, F9/F10 insert contacts, Shift+F8/F9 open/close branches. These are comparison examples, not a request to remap every key immediately.
* [S2: STEP 7 V21 inserting operands](https://docs.tia.siemens.cloud/r/en-us/v21/creating-lad-programs/inserting-operands-into-lad-instructions/inserting-operands): operands are edited at placeholders; required connections, expected types, syntax feedback, and define-tag shortcut-menu workflow support programming at the diagram.
* [S3: STEP 7 V21 graphical autocompletion](https://docs.tia.siemens.cloud/r/en-us/v21/program-editor/using-autocompletion/using-autocompletion-in-graphic-programming-languages): context filters admissible operands, supports local/global symbols and structured member navigation.
* [S4: TIA Portal V20 basic keyboard operations](https://docs.tia.siemens.cloud/r/en-us/v20/introduction-to-the-tia-portal/user-interface-and-operation/keyboard-operation-in-the-tia-portal/basic-functions-of-the-tia-portal): Shift+F10 opens an object's shortcut menu; help, find and cross-reference commands have keyboard access.
* [S5: S7-1200 V20 watch and force tables](https://docs.tia.siemens.cloud/r/simatic_s7_1200_manual_collection_enus_20/programming-concepts/debugging-and-testing-the-program/watch-tables-and-force-tables): saved watch tables monitor and modify CPU values; force behavior belongs to the CPU. RungProof must describe its own offline snapshot/override behavior separately.
* [S6: STEP 7 V21 block interface overview](https://docs.tia.siemens.cloud/r/en-us/v21/declaring-the-block-interface/overview-of-the-block-interface?contentId=T0YPEpx8slfyTRvR2XBL1A): interface sections describe parameters with execution meaning, including values read and returned through InOut.
* [S7: STEP 7 V21 interface types for S7-1200/1500](https://docs.tia.siemens.cloud/r/en-us/v21/declaring-the-block-interface/rules-for-declaring-the-block-interface/valid-data-types-in-the-block-interface-s7-1200-s7-1500?contentId=wwX~ovzjkqEk8fbM7T2xwg): allowed declaration sections/types depend on block and CPU family.

The proposed context-menu contents below are a RungProof design recommendation grounded in these workflows, not a claim that Siemens has the identical menu arrangement.

## Prioritized gap matrix

| Priority / area | Current source evidence | Assessment and proposed improvement |
|---|---|---|
| P0: accurate context target | `LadderEditorCanvas.cs:269-297`: instruction hit test precedes right-click, but right-click otherwise selects the network before insertion-point hit testing | Right-click on a wire loses the precise insertion target. Use one typed hit-test result for mouse, keyboard and popup commands. Wire menu should insert at that slot, while network menu acts on the whole network. |
| P0: semantic scope | `LadderEditorDocument.cs:24-53,68,895`: flat contacts/branches, one output operation, call target only, interface metadata | OB/FB/FC/DB names and an interface table do not provide full Siemens block semantics. Explain supported offline subset and unsupported parameterized/instance behavior in instruction help and call UI. Avoid presenting a generic call as a working FB with an instance DB. [S6,S7] |
| P1: context menu completeness | `SimulatorShell.cs:5148-5156,7113`: Properties, Copy, Paste after selection, Delete instruction/network, Help | Real menu exists but has no insert, replace, operand xref/declaration, watch, network duplicate/move, or branch-specific access. Route menus to existing commands first. Use names matching the target: Delete contact versus Delete network. |
| P1: operand entry | Shell `4770-4808,5550-5596`: typed OptionButtons; canvas double-click opens properties; F2 opens tag properties (`309-317`) | Type-filtered selection exists; inline operand edit/completion is missing in this path. Add an anchored editor with type/role information, filtered symbols, Enter commit, Escape cancel. Preserve role restrictions. F2 currently differs from documented Siemens operand editing. [S1-S3] |
| P1: insertion/replacement | Canvas insertion slots/drop paths `226-256`; shell `7040,7170`: palette command dispatch and NO/NC replacement preserve current operand | Existing capability, poor contextual discoverability. Explicit Insert before/after and Replace submenu should explain compatible replacements. Do not silently replace a comparison or discard timer/call data. |
| P1: xrefs and watch | Shell `6086-6210` watch values/status; `6660-6758` access categories, index and navigation; selected-tag xref at `6799` | These are functional source-backed features, not decoration. Expose Show references, Go to declaration and Add to watch directly on operand targets. Preserve stale-loaded-program checks. Watch is an offline snapshot view, not CPU monitoring. [S1,S5] |
| P1: keyboard/undo/clipboard | Canvas `309-334`: save, undo/redo, copy/paste, delete, arrows, Home/End, Insert, Shift+Insert, Ctrl+Insert; shell `189-190` separate contact/network clipboards | Core shortcuts/history already exist. Add keyboard context menu, consistent target focus and visible shortcut labels. No evidence here of general rectangular/multiple selection, cut, arbitrary subgraph clipboard, or OS/inter-vendor clipboard compatibility. Do not claim them. [S4] |
| P1: diagnostics/project workflow | Shell `4522-4547`: new/save/open, Verify + load offline; `8290` issue activation navigates; source draft history/cache exists | Existing compile/load and diagnostic navigation should be retained. Add precise instruction/operand targets where only network-level diagnostics exist; keep draft validation and loaded-program provenance visible. Do not conflate native JSON save with TIA archive compatibility or offline load with PLC download. |
| P2: network ergonomics | Document `719-724` adds an entire parallel branch; canvas branch drawing uses shared join | Full-rung parallel paths exist. Arbitrary branch start/end around a subexpression and nested branch graphs cannot be implemented by menu labels alone. Decide graph/AST migration separately; current UI should say Parallel path rather than imply unrestricted branch routing. [S1] |
| P2: block calls/interfaces | Shell `4844-4905` creates block kinds and interface rows; document `625,895` stores/emits a target-only call | Calls exist; parameter binding/instance identity are absent from this call representation. Prioritize Open called block and show interface metadata; parameterized calls need separate compiler/runtime/schema work and executable tests. [S6,S7] |

## Concrete proposed menus and workflows

**Contact/instruction:** Edit operand / Properties; Insert before / after; Replace with compatible instruction; Copy; Delete contact; Show references; Go to declaration; Add operand to watch; Instruction help. For a comparison, identify left/right operand separately. For output instructions, do not offer contact-only insertion/replacement.

**Wire/insertion slot:** Insert contact; Insert comparison; Paste compatible contact; Add parallel path (existing topology, explicitly labeled). Preserve the exact branch and slot under the cursor. A right-click should never accidentally turn a local operation into a whole-network delete.

**Network header:** Edit title; Insert network before/after; Copy/duplicate network; Move up/down; Delete network. Future collapse/expand is presentation state, not a logic edit.

**Call:** Properties; Open called block; Find callers; Help explaining supported call semantics. Do not offer Edit instance DB or Bind formal parameters until those capabilities exist end to end.

Workflow A: right-click the wire between two contacts, insert NC, type/filter a BOOL tag, Enter, Undo, Redo. Expected: exact insertion slot, stable IDs for unaffected nodes, one history transaction, no output change.

Workflow B: select an output operand, Show references, choose a write/read occurrence in another block, return to the original location, add the operand to watch. Expected: precise selected node and original navigation context retained; snapshot values disappear or show stale status if draft no longer matches loaded program.

Workflow C: enter an invalid operand, save draft, close/reopen, validate, activate the issue, correct it, verify/load offline. Expected: invalid draft survives save; runtime remains on last accepted program until load succeeds; diagnostic reveals the actual offending operand without resetting unrelated selection/history.

## Phased implementation proposal

1. **Point-of-use commands, no schema migration.** Introduce an editor selection target (network, instruction, operand, insertion slot) and a small command adapter shared by palette, menu and shortcuts. Keep document mutations/history functions authoritative. Replace numeric popup indices with named command IDs and state predicates. Implement right-click slot targeting, context insert/replace, xref/declaration/watch and keyboard popup. This is the recommended first approved slice.
2. **Operand entry and navigation.** Add an anchored operand field, typed symbol completion and clear validation. Support aggregate leaves using existing metadata. Define-tag should be one undoable transaction including assignment. Add navigation back/forward and precise diagnostic selection; make keyboard mappings explicit rather than silently borrowing incompatible TIA keys.
3. **Network authoring convenience.** Duplicate/move networks, predictable paste target, collapse state, optional multiple selection. Prove deletion and clipboard behavior on invalid drafts and across block changes. Keep current branch topology until a separately reviewed design exists.
4. **Semantic expansion only after specification.** Specify nested branches and parameterized calls separately: serialization migration, expression evaluation order, type checks, parameter transfer, state ownership, recursion/limits and watch attribution. Do not add fake hardware catalogs, online downloads, Siemens force behavior, or nominal instruction buttons without matching semantics.

Avoid a broad rewrite of the large shell. Extract only the command/selection boundary needed by phase 1, then incrementally move reusable operand and navigation behavior behind it.

## Testable acceptance criteria for phase 1

* Right-click each of instruction body, operand, wire slot, branch path, output and header. The highlighted target and menu label agree; selecting an action changes only that target. Clicking outside/Escape changes no document or history.
* Insert at beginning/middle/end of each existing branch, including empty branch. Verify the ordered contact IDs and operands, Undo/Redo and saved/reopened structure.
* Context, toolbar, shortcut and drop actions produce equivalent model changes and exactly one undo entry. Mutation lock blocks all edit routes; read-only help/xref/watch navigation remains usable where appropriate.
* Replacement preserves compatible operands and rejects incompatible instruction/type combinations without partial edits. Copy/paste creates new IDs and does not retain mutable references to the source.
* Reference navigation reaches exact supported uses across blocks; declaration and timer/aggregate-member handling are explicit. Add-to-watch has no duplicate rows and preserves saved selection. Editing the loaded draft never presents old snapshot values as current.
* Shift+F10/menu key opens at the keyboard-selected target; arrow navigation/Enter/Escape work without stealing text-field shortcuts. Menus stay within the viewport at each edge and at DPI settings selected for supported desktop review.
* Native rendered inspection remains mandatory: capture menus, operand editor, long symbols and diagnostics in the actual application; verify legibility, clipping, overlap and focus. Static inspection or headless tests cannot close this criterion.
* Use existing deterministic controller tests for affected semantics, targeted editor command tests and shell verifier; native inspection proves interaction. No test may contact a plant.

This audit is complete as a source-based proposal. Rendered UX quality and execution of the proposed workflows remain unverified.

## Architecture clarification

The flat-branch limitation above is specifically the **editable document and its projection/import/export**, not a lack of recursive runtime logic representation. `LadderProgram.cs:65-73` already provides recursive `LadderNode.Children` with Series/Parallel kinds. Reuse and test that representation; do not assume a new runtime/compiler architecture is necessary for nested editing. The work is primarily a faithful editor representation, renderer/hit testing and round-trip preservation, followed by verification of existing evaluation behavior.

The interface limitation is already disclosed in `SimulatorShell.cs:5966`: its tooltip says offline calls use shared project tags and do not implement parameter passing or per-instance FB storage. The add-interface success message also repeats shared-tag scope (`7616`). Improve prominence at call authoring; do not report the disclosure as absent.

## Command coverage appendix

This is handler/source coverage, not execution coverage. All line references below are in `src/App/SimulatorShell.cs` unless qualified. Findings labeled source-confirmed follow directly from handlers; impact still needs native interaction verification.

### Concrete inconsistencies to fix first

1. **P1, source-confirmed: Delete network is not the menu's actual behavior.** Menu item at `5154` says Delete instruction / network, but its enabled predicate at `7118` disables it whenever no contact is selected, including network selection. The shared remove handler (`7814-7855`) deletes the last contact if the selected branch has contacts and no particular contact is selected. Only an empty final branch permits whole-network removal, and the final empty network is retained silently. Introduce explicit Delete contact / Delete parallel path / Delete network commands with exact targets. Acceptance: selecting a network never deletes an arbitrary last contact.
2. **P1, source-confirmed: Copy on an output copies the entire network.** `CopyLadderSelection` (`7867-7890`) supports contacts, otherwise captures the rung. Copy on a timer/counter/call/coil output therefore becomes whole-network copy under generic instruction-copy wording. Label it Copy network when that is the supported operation, or implement output-specific clipboard semantics deliberately.
3. **P1, source-confirmed: Add BOOL tag does not select BOOL.** Tools label `4559`; handler around `8559` only switches to the tag page and focuses its name field. A previously selected TIMER/REAL/STRUCT type remains selected. Rename to Open tag editor, or explicitly initialize a new BOOL definition without overwriting the currently selected tag. Acceptance: invoke after selecting REAL and verify intended resulting type.
4. **P1, source-confirmed: coil insertion differs by entry path.** Instruction-tree activation `8343-8389` handles coil only with `coilSelector.GrabFocus()`. Palette Assign (`7350`) and canvas coil drop (`7144-7150`) commit a coil through `SelectCoilMode`. Focusing the selector may also be ineffective while its property row is hidden; that visibility effect requires native verification. One insert command should serve all paths.
5. **P2, source-confirmed: aggregate watch rows have no declaration navigation.** Watch rows support aggregate leaves (`6160`), but double-click at `8581-8589` searches only exact top-level `document.Tags` names and returns silently when absent. Resolve leaf to root/member and select the declaration/member, or clearly mark rows without navigation.
6. **P2, source-confirmed: device tree node is explanatory text only.** `organization.ItemSelected`, case device (`8428` vicinity), writes a message about symbolic I/O; it opens no configuration surface. Make the node an explicit simulator-I/O overview/link to bindings, or label it informational. Do not add a cosmetic hardware editor.
7. **P2, source-confirmed: diagnostics message is hardcoded.** Tree diagnostics and View diagnostics (`8420` and `8524` vicinity) write compiler ready / transport disconnected / no physical PLC regardless of validation results. The separate error list is real, but this text is explanatory scope presented like measured state. Replace with Offline editor diagnostics plus actual draft/load state; keep live transport outside this panel.
8. **P2, source-confirmed: Add block/task ignores entered names.** `7504-7532` generates OB/FB/FC/DB names from count rather than `blockName`; `7728-7748` generates CyclicOB names rather than `taskName`. Apply later performs rename. If the same form appears to create a named item, state the two-step flow or honor validated name on creation. Do not silently imply the name field affects Add.

### Surface-by-surface coverage

| Surface | Source-backed handler coverage | Qualification / gap |
|---|---|---|
| Project: New, Save, Open | `8458-8499`: guarded reset, native project dialogs, history/clipboard reset | Real mutations/dialog paths. Save/open round-trip and unsaved-change behavior not exercised here. |
| Edit: Add, Delete, Copy, Paste, Undo, Redo | `8500-8509`, shared commands `7790-7940` | Connected; deletion/copy semantics above. Add network appends (`7790`), not insertion relative to selection. |
| Online: verify/load, run, stop, reset | `8510-8517`, validation/load `8291-8309` | Routes to real validation/program request and playback events. Overall Online label is vendor-style navigation for offline operations, not real CPU access. Event subscriber outcomes not rerun. |
| View: editor, tags, diagnostics, search, help, watch, instructions | `8518-8560` | Mostly selects dock/tab and focuses relevant control; watch refreshes snapshots, help renders content. Diagnostics wording issue above. Test visibility when project dock was previously hidden: some handlers switch tabs without explicitly reopening their host. |
| Tools: Add BOOL, Help (F1), Cross-reference | `8561-8580` | BOOL label mismatch; Help invokes renderer; cross-reference opens search and requires query. F1 keyboard binding was not found in this file; wider input routing must be checked before declaring shortcut broken. |
| Project tree: blocks, tasks, tags, watch, diagnostics, device | `8390-8456` | Block selection changes active document block; task selection loads controls; folders open appropriate tabs. Device informational-only. Root selection status-only is normal navigation feedback, not a missing command. |
| Tags: add/apply/delete, initial values, type/role, binding, selected xref | `6759-6840,8130-8210` | Actual document edits/preflight and xref navigation. Duplicate/type errors report messages. Blank name Add returns silently (`8188`); disable with reason or show validation. TIMER/COUNTER role locked to internal scope; binding status already explains internal/unbound states (`5369+`). |
| Blocks: add/apply/delete | `7504-7583` | Real create/rename/type change and guarded remove with reason. Apply can change block kind although output says only renamed. Name-on-add issue above. |
| Interfaces: add/remove | `7584-7632` | Declaration metadata edits, duplicate/parse feedback. Empty name returns silently; parameter execution explicitly disclaimed. No general edit-in-place handler established by this audit. |
| Tasks: add/apply/remove, kind/period/priority/target | `7715-7789` | Actual scheduler model edits; protected removal gives reason; continuous task period not editable. Runtime scheduler semantic parity with Siemens not claimed. Name-on-add issue above. |
| Instruction palette/tree | `7040-7090,7185-7503,7633-7714,8310-8389` | Contacts/edges, branches, timers, counters, comparisons, arithmetic, calls, return/jump/label and networks route to handlers. Single-click tree text/help is intentional preview; double-click dispatch is real. Coil path mismatch is the concrete exception. No blanket claim of inert palette. |
| Properties | `5060-5141,5158+`, edit subscriptions `7940-8130` | Contextual network/contact/output/timer/counter/comparison/numeric/call/control rows exist; close returns canvas focus. Shared fields mutate on selection/submission rather than a single explicit Apply transaction; verify dismissal semantics and grouping natively. |
| Error list | `8210-8290` | Real issues and double-click navigation to block/network/tag/task. Instruction/operand paths are not selected precisely: block branch sets selectedContact=-1. Root/general issues can only show message. |
| Watch | `6086-6258,8581` | Real selected-symbol list/add/remove/all/clear; snapshot refresh and program-match gating. Aggregate declaration-navigation limitation above. Not CPU modify/force table. |
| Search/xref/navigation | `6660-6758,6791` | Real index queries and result activation across block/network/declaration locations. Selected-tag entry is connected. No back/forward history established by this audit. |
| Undo, redo, movement disabled states | `5638-5639,5725-5734,5760-5764` | Undo/redo explicitly explain Nothing to undo/redo via tooltip. Move buttons explain action but do not distinguish no selection from boundary position. Context delete has no reason beyond disabled state. Do not report all disabled controls as unexplained. |

### Additional acceptance cases

* Exercise each command from all available entry points against the same document and compare resulting model/history, especially coil insert, delete, copy and paste.
* Start with dock collapsed, another tag type selected, a leaf watch row selected and an invalid draft. Menus must still reach the correct controls and show honest current state.
* Verify blank form fields, missing operands and no selection produce an actionable reason rather than apparently dead enabled buttons.
* Use a named task/block creation case to settle whether Add honors the form or creates defaults; label/output must match the approved behavior.
* Preserve all existing scope disclosures; promote parameter/instance limitations to call authoring rather than introducing decorative controls.
