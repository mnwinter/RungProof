# RungProof TIA-side editor upgrade — design for review

Status: proposed, not implementation acceptance. Date: 2026-10-11.
Owner: root; independent comparison and instruction-monitor audits attached.

Revision 2 incorporates Dot's first review. Detailed contracts are in `tia-editor-semantics-contract.md`, `tia-editor-monitoring-contract.md`, and `tia-editor-command-ledger.md`; these govern precise behavior where this overview is abbreviated. Dot's verdict is recorded separately in `tia-editor-dot-design-review.md`.

## User intent and success
Matt asked for the entire TIA-side editor to be compared with TIA Portal, then improved in UX and functionality, including right-click menus and all logic-block values. Every exposed function must work; decorative controls and unimplemented parameter semantics are unacceptable. Keep existing scene integration, saved projects, current counter improvement, and the separate Studio 5000 presentation working.

Baseline: STEP 7 LAD for S7-1200/1500, using version-labeled Siemens V21 detailed editor documentation and V20 general keyboard/watch documentation. This is a RungProof offline editor, not a Siemens project format, CPU emulator or engineering-station replacement. The objective is familiar, productive authoring and truthful monitoring of the implemented simulator language.

## Evidence and important findings
Read tia-editor-independent-audit.md and instruction-monitoring-full-audit.md in this directory. Root separately checked wire right-click ordering, popup predicates, target-only CALL execution, interface representation, and Stop snapshot handling in source.

1. Existing insertion, clipboard/history, typed tag pickers, cross-reference navigation, watch views, compile/load and issue navigation have real implementations. Their reachability and exact command behavior still need native/end-to-end tests.
2. Right-click currently handles empty space as a network before testing wire insertion slots. A local click can lose its branch/slot target.
3. Interface declarations are saved, but runtime CALL only queues target networks. Parameter passing and per-instance FB storage are not implemented. An existing tooltip discloses this; it is not sufficient to present these declarations as working call parameters.
4. Element monitoring stores ID and Boolean result, not typed consumed operands or actual write result. Final snapshot values can differ from values used earlier in the scan. Stop/force snapshots can retain previous element highlights.
5. The editor uses flat contact lists and parallel paths. Runtime LadderNode already supports recursive Series/Parallel nodes; inspect compiler and document round-trip behavior before deciding which layers require changes. Do not rewrite the runtime unnecessarily.
6. V25 counter readouts are verified for the single-counter demo. Shared instances require configured preset versus instance preset, and box enable versus instance CountInput, to be distinguished.

## Product behavior
### A. Editing at the selected location
One immutable selection target describes network, branch, insertion slot, instruction and operand role. Mouse hit-testing, keyboard navigation, drop and context menus resolve the same target. Commands carry stable IDs plus document revision; a menu from an old block/revision cannot mutate the new selection.

Instruction menu: Edit operand/Properties, supported Insert before/after, compatible Replace, Copy/Cut/Paste, Delete with exact target name, references, declaration, watch and instruction help. Wire menu inserts at that slot; network header menu inserts/duplicates/moves/deletes networks. CALL menu opens the target and finds callers. Commands appear only where semantically valid; disabled commands explain their prerequisite. No generic Delete network on an ambiguous wire click.

Use one command dispatch and CanExecute path for menu, toolbar, shortcuts and drag/drop. Reuse current document mutation/history; each user operation is one undo entry. Validate before mutation, preserve invalid-but-saveable drafts, regenerate pasted IDs, retain unaffected IDs and selection. Escape cancels without edits.

Inline operand entry opens at the operand; shows expected type and role, filters compatible symbols and aggregate members, Enter commits and Escape cancels. Undefined symbols may be defined through a real undoable tag-creation/assignment flow. Required/unbound operands remain explicit and compilation blocks invalid execution. Do not silently manufacture addresses or bind a local name to a global.

### B. Honest monitoring for every supported instruction
A shared typed presentation model supplies rows, status, tooltip/full detail, and layout bounds for contacts, comparisons, coils, timer/reset, counter/load/reset, every numeric/text operation, CALL, RETURN, JMP and LABEL. Both vendor renderers consume the same semantics with style-specific presentation.

Value states distinguish FALSE, zero, empty text, unavailable, not monitored, not evaluated, skipped, success and fault. Current snapshot values and instruction-time values are different fields. Do not recompute a historical comparison or call result from final tag values. No green success indication merely because enable was true.

Add bounded immutable instruction observations during real execution: instruction ID, scan identity, task/block/call path, last invocation ordinal/count, enable/local condition, typed operands actually consumed, actual result, destination before/after, write disposition and structured fault. Collect last invocation per instruction with an invocation count initially; disclose that it is not a full loop trace. Do not rerun an instruction for monitoring. Reset clears observations; stopped/force updates explicitly retain last-evaluated provenance separately from current values. Preserve exact-draft matching, paused/step state and accepted-history identity. Establish a memory/allocation budget through profiling before choosing trace capacity.

Numeric/math blocks show named inputs, destination, types and observed result/write/fault. NORM/SCALE use MIN/VALUE/MAX labels. Text/enum values are quoted with full details available. Timer blocks expose configured/instance PT, ET, input, timing and done with units. Counters distinguish instruction preset, shared-instance preset/current state and instruction enable. Contacts show operand and local evaluation separately from continuity. Calls and flow instructions show executed/skipped/target, with no invented parameters or frames.

Layout is measured once for drawing, hit-testing, selection and scroll reveal. Allow selected detail expansion; never silently clip digits or symbol suffixes into a different value/name. Keep full values available with explicit truncation indicators. Monitor rows must not overlap conductors, adjacent branches, network headings or dialogs.

### C. Real block interfaces and nested editor branches
These are required later workstreams, not pretend buttons in the first wave.

Parameterized calls need an explicit schema for formal-to-actual bindings, compiler type/role checks, scoped variable identity, FC invocation locals, FB instance storage, initialization, input/output/InOut transfer, conditional call behavior and instance-qualified monitoring. Existing target-only calls retain their shared-project-tag semantics through versioned migration. Never silently reinterpret old projects. Reject unsupported/reentrant/ambiguous configurations with actionable diagnostics. Specify supported aggregate aliasing, reset/retention and nested-call behavior before code; initial UI must expose only verified cases. A declaration cannot count as a working parameter.

Nested branch editing must preserve the recursive expression tree through editor save/open/import, stable node identity and undo. Implement exact branch start/end selection, node replacement and branch pruning against existing runtime capabilities. Lock evaluation order in regression tests. Define legal output placement; do not advertise arbitrary TIA network interconnections outside the represented language.

### D. Whole editor workflow
Project New/Open/Save/Save As, unsaved guards and invalid draft recovery; project-tree selection; tags and supported data types; block/task editing; instructions/favorites/search; property dialogs; diagnostics; watch/override; cross-references; keyboard and pointer navigation all receive a command inventory row.

Each row records target, preconditions, source handler, document/runtime effect, persistence, undo behavior, test evidence, native visual evidence and current status. Classify scope as required capability, supported informational content, or explicitly excluded capability. Required capabilities remain incomplete until behavior and applicable native evidence pass; disabled-with-explanation is a temporary state, never fulfillment. Presence of a control, green build or status message does not mean verified working. Parameterized calls and recursive authoring are mandatory final gates.

## Implementation sequence and review gates
1. Truthfulness and inventory: reconcile every visible TIA command with its handler; make declaration-only interface limits prominent; fix precise context targets and establish shared command predicates. Add tests reproducing the right-click slot problem before fixing it.
2. Monitoring foundation: presenter and execution observations with lifecycle/provenance tests. Implement every family in the audit matrix; ensure same-destination overwrite, shared counters and faulted math remain truthful. Capture timer, counter, comparison, math, text and flow displays in the real app.
3. Point-of-use authoring: context insert/replace/xref/watch, keyboard popup, inline typed operands, network operations and navigation. Test complete actions through GUI event routing, undo/redo and save/reopen.
4. Semantic completion: implement the reviewed call-interface and recursive-branch contract through compiler/runtime/document/editor support in small reviewed slices. Migration fixtures must pass before enabling new controls. Schema and semantic decisions are defined before this phase, not deferred until coding.
5. Integrated qualification: use the final executable to create a project, declare tags, author nested logic and a parameterized supported call, fix diagnostics, compile/load, run/monitor, debug, save/close/reopen and compare results. Update fixed candidate and desktop shortcut only after final integrity/visual checks. Preserve prior test build.

The concrete file-level implementation plan is `tia-editor-implementation-plan.md`. Matt's explicit studio sequence requests a complete plan before final authorization, so the spec and plan are submitted together after Dot review rather than adding an intermediate spec-only permission prompt. Root alone owns build/export/captures to avoid shared artifacts racing. Dot reviews each integrated wave against this contract; only verified evidence moves the capability ledger to working. The source-audit reports are not function-test passes.

## Acceptance matrix
- All enum/catalog instructions have tests for valid behavior, disabled/skipped behavior, reset/lifecycle, invalid type/operand and relevant arithmetic faults. Include DIV/MOD zero, nonfinite/domain errors, conversion boundaries, empty text and aggregate members.
- Scan-order adversarial tests: later MOV changes earlier comparison operand; multiple writes; output override; shared CTU/CTD/LOAD/RES; timer then reset; repeated CALL; skipped task; jump watchdog; stop and force without a new scan; stale draft and undo restoring match.
- Each context target and command tested at start/middle/end/empty branches; exact node IDs, history count, undo/redo and persisted round-trip verified. Mutation lock applies to every route.
- CALL tests prove parameter transfer, supported aliasing, separate FB instance state, reset/retention, nested calls, diagnostics and old-project compatibility. No initial-value display substituted for live parameters.
- Native TIA-side workflow and both-renderer regressions at 1280x720 and 1600x900 logical pixels, DPI 100/150/200%, zoom 75/100/150%, with long tags/text and viewport-edge menus. Test keyboard popup, focus, Enter/Escape, text-field shortcut isolation, scroll and selected-item reveal. Use the companion contract's explicit matrix; captures alone do not prove physical interaction.
- Old project/workspace fixtures round-trip without data loss. Runtime checks confirm no real PLC connection. Exported candidate checked, not only development source.

## Open limits and explicit exclusions
Physical keyboard/mouse acceptance remains pending implementation and available native-control tooling. Honor Matt's latest input permission at execution time; if a gaming reservation is reinstated, use only non-disruptive checks. Never claim synthetic events prove human ergonomics. Existing Handle issue remains open and is not fixed by editor changes. No paid software install, debugger install, PLC connection, Siemens archive importer, full CPU instruction compatibility, WinCC/hardware configuration, signing, publication or release approval is included. Unsupported features must be explained or omitted, not represented as operational controls.

## Human decision requested
Review this design and `tia-editor-implementation-plan.md` together, then authorize the planned studio implementation or amend the scope. This is the final preimplementation authorization requested in Matt's studio workflow. No editor behavior changes have been made during this whole-editor audit.
## Whole-editor command audit addendum
The comparison agent completed a surface-by-surface appendix. These defects/inconsistencies are mandatory initial corrections, not optional cosmetic work:

| ID | Source-confirmed gap | Required behavior / proof |
|---|---|---|
| EDIT-01 | Wire right-click bypasses insertion hit-test | Exact branch/slot preserved; beginning/middle/end and empty-path insertion round-trip tested |
| EDIT-02 | Context Delete disabled for network; generic delete can remove last contact instead | Explicit Delete contact/path/network commands with exact selected target, one undo transaction and no fallback to another target |
| EDIT-03 | Copy on output copies entire network | Real output-only clipboard; Copy network remains a separate explicitly named command. Paste validates destination compatibility before mutation. |
| EDIT-04 | Double-click coil in instruction tree only focuses selector | Every insertion entry point produces the same intended mutation and history result |
| EDIT-05 | Tools Add BOOL tag does not select BOOL | Opens visible tag editor with BOOL selected; confirms actual created type and undo/save/reopen |
| EDIT-06 | Aggregate-leaf watch declaration navigation returns silently | Resolve leaf to owning declaration and selected member, or explain unavailable target; never an apparently dead action |
| EDIT-07 | Device node shows information only; diagnostics hardcode ready/disconnected wording | Explicit symbolic-I/O overview/binding navigation; diagnostics derived from actual validation/load state and clearly scoped offline |
| EDIT-08 | Add block/task ignores entered name; Apply status can misdescribe type change | Validate and honor entered creation name; state actual changed fields; duplicate/blank cases have actionable feedback |

Also test hidden-dock menu navigation, blank tag/interface forms, property-dialog cancellation/history grouping, and precise diagnostic operand selection. These findings are source confirmed; native reproductions precede fixes. The full report distinguishes intentional informational navigation from misleading operational controls.
