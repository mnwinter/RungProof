# TIA editor ledger evidence status

Independent reconciliation, 2026-10-11. All 167 IDs appear below. Changing-source checkpoint only; no final-candidate or export acceptance. Reviewer did not run builds, tests, or native interactions.

**Partial** means bounded source/automated evidence exists, but the complete route, history, persistence and rendered acceptance is unproven. **Missing** means a confirmed required behavior is absent. **Excluded** preserves the approved boundary. No row receives a native pass from handler existence, emitted signals, headless logs, or older screenshots.

## Confirmed behavior blockers

1. **P01:** SimulatorShell project-tree program selection resets selectedRung=0, selectedBranch=0, selectedContact=-1, selectedOutput=false. No per-block stable target/scroll restoration was found.
2. **A04:** AggregateTagEditor type-change confirmation calls AggregateArrayDraft.TryChangeType(type,true), clearing retained values and replacing strings with defaults. The approved ledger requires retaining strings across type changes and rejecting invalid Apply. The explicit-reset regression tests verify a different design.
3. **C06/C07:** LadderEditorCanvas._ShortcutInput handles Shift+F10, but no menu-key alias was found. These rows explicitly require both routes.
4. **P12/P13/B01/X03:** InterfaceList tooltip in SimulatorShell still says parameter passing and per-instance FB storage are unimplemented. This is now false for parameterized mode. Keep Legacy disclosure but make the tooltip mode-aware.

## Evidence gaps rather than confirmed product defects

- All required rows need final mapping against one frozen candidate; broad successful logs do not establish all row criteria.
- Q01-Q13/L05: TX19 fails per-form Escape with a sibling draft. A source fix exists; fresh green verification is required.
- F04/F05: atomic storage tests do not prove native file-dialog cancel/overwrite/Save As path-marker sequences.
- I/M families: runtime semantics and representative route tests pass; exhaustive palette/tree/drop serialization equivalence was not found.
- B02: runtime instance isolation is covered; native instance-qualified inspection needs explicit evidence.
- L06: minimum-window/DPI/long-name/all-dock and zoom interaction checks remain open. Zoom is actively changing.
- Monitoring performance budgets remain separate open gates. Runtime semantics do not prove timing or retained-memory compliance.

## Evidence keys

Logs live under worktree `.superpowers/sdd/tia-editor-implementation-plan`. Source paths are relative to rungproof-next.

- **UT37**: integration-37.log, 305 passed/1 deliberate zoom geometry failure. Individual semantic/model/history/CALL/recursive/migration/monitor tests are useful; this is not an all-green run.
- **LE08**: ladder-editor-08.log, LADDER_EDITOR_INTERACTION_VERIFY and EDITOR_COMMAND_ROUTES_VERIFY pass. Programmatic Godot checks, not physical native coverage. Invalid window-position errors also occur for headless creation dialogs, so visual placement is not established.
- **TX17**: editor-transactions-17.log, 42 programmatic transaction checks pass. **TX19**: editor-transactions-19-red.log, 42 pass then PerFormEscapeRestoresFieldsWithSiblingPending fails; newer source fix needs rerun.
- **CA01**: call-authoring-01.log, staged/invalidRejected/applied/persisted/undo/cancelled all true through production form signals. Root reported physical invalid-field focus/Escape success separately; this reviewer did not inspect those screenshots and does not count that as independent native acceptance.
- **ST03**: storage-03.log, PROJECT_STORAGE_TESTS_PASS checks=14. **BU03**: bundle-green-03.log, actual runtime joint reopen preserves relation/draft/stopped state.
- **OP03**: operand-placement-03.log, rendered 1600x900 popup geometry pass only; not every keyboard or styling case.
- **S/C**: SimulatorShell.cs/LadderEditorCanvas.cs. **TX**: SimulatorShell.EditorTransactions.cs and LadderEditTransaction.cs. **CALL**: LadderCallEditor.cs, SimulatorShell.CallAuthoring.cs, LadderCallAuthoring.cs. **ARRAY**: AggregateTagEditor.cs and AggregateArrayDraft.cs.

Outstanding column preserves the original specific acceptance case. Every required row also needs actual rendered interaction evidence and route/history/persistence checks on the frozen candidate. The matrix is conservative: partial evidence is not a release pass.

| ID | Classification | Status | Bounded current evidence | Outstanding specific acceptance |
|---|---|---|---|---|
| F01 | required | partial | S save/load routes; UT37 invalid-work round-trip; ST03/BU03; TX17 Save/New guards | Cancel then confirm; compare draft/history |
| F02 | required | partial | S save/load routes; UT37 invalid-work round-trip; ST03/BU03; TX17 Save/New guards | Save/reopen invalid draft and scene identity |
| F03 | required | partial | S save/load routes; UT37 invalid-work round-trip; ST03/BU03; TX17 Save/New guards | Valid, invalid, cancel and dirty open |
| F04 | required | partial | S save/load routes; UT37 invalid-work round-trip; ST03/BU03; TX17 Save/New guards | Read-only path, existing file, cancel; compare saved marker |
| E01 | required | partial | S addRung inserts at selectedRung+1; LE08 rungAdded; first/middle/last/no-selection full matrix open | Select first/middle/last network and no selection; verify exact order, undo and round-trip |
| E02 | required | partial | S/C shared commands; UT37 EditorCommandTests/RecursiveEditorTests/history; LE08 clipboard/delete/undo | Select populated network; delete/undo |
| E03 | required | partial | S/C shared commands; UT37 EditorCommandTests/RecursiveEditorTests/history; LE08 clipboard/delete/undo | Copy timer/CALL/coil output and paste; unaffected contacts stay intact; Copy network duplicates whole network with new IDs |
| E04 | required | partial | S/C shared commands; UT37 EditorCommandTests/RecursiveEditorTests/history; LE08 clipboard/delete/undo | Beginning/middle/end and cross-block paste |
| E05 | required | partial | S/C shared commands; UT37 EditorCommandTests/RecursiveEditorTests/history; LE08 clipboard/delete/undo | Undo each command including invalid draft |
| E06 | required | partial | S/C shared commands; UT37 EditorCommandTests/RecursiveEditorTests/history; LE08 clipboard/delete/undo | Undo/redo/edit/redo sequence |
| O01 | required | partial | S controller events; UT37 lifecycle/offline workflow; LE08 monitor bindings | Invalid retains old runtime; valid loads exact draft |
| O02 | required | partial | S controller events; UT37 lifecycle/offline workflow; LE08 monitor bindings | Run loaded/current draft with visible provenance |
| O03 | required | partial | S controller events; UT37 lifecycle/offline workflow; LE08 monitor bindings | Stop scan/plant per established contract |
| O04 | required | partial | S controller events; UT37 lifecycle/offline workflow; LE08 monitor bindings | Reset state per established contract |
| V01 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke Program editor, verify focus/content |
| V02 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke PLC tags, verify focus/content |
| V03 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke Diagnostics / output, verify focus/content |
| V04 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke Find / cross-reference, verify focus/content |
| V05 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke Instruction help, verify focus/content |
| V06 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke Watch table, verify focus/content |
| V07 | required | partial | S View menu; LE08 HiddenDockNavigationReopensHost/help/browser/diagnostics | Collapse target dock, invoke Instruction browser, verify focus/content |
| T01 | required | partial | S Tools menu; LE08 BoolMenuResetsPendingType/help/search; UT37 help catalog | Select REAL first, invoke Add BOOL, create tag; verify BOOL, undo and saved/reopened type |
| T02 | required | partial | S Tools menu; LE08 BoolMenuResetsPendingType/help/search; UT37 help catalog | Press F1 with canvas and text focus |
| T03 | required | partial | S Tools menu; LE08 BoolMenuResetsPendingType/help/search; UT37 help catalog | Run from selected contact and collapsed dock |
| P01 | required | missing | S organization.ItemSelected resets selection; no per-block target/scroll cache found | Navigate A/B with different selection/scroll, return; remove prior target and verify header fallback |
| P02 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Select periodic task; inspect period/target |
| P03 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Select with dock collapsed |
| P04 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Select after failed compile |
| P05 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Invoke with project dock hidden; verify visible editable bindings and no hardware claim |
| P06 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Select before load and after edit |
| P07 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Add/remove objects then select |
| P08 | informational | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Keyboard expand/collapse, no document edit |
| P09 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Create FB with custom name, default name, blank and duplicate; verify model/history |
| P10 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Rename called block and change kind; validate |
| P11 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Referenced vs unused blocks; undo |
| P12 | required | partial | CALL formal validation/S interface form; UT37 direction/type/scope cases; stale S tooltip | Blank/duplicate/bad initial; forbidden types/sections; local/global name collision; no partial history |
| P13 | required | partial | S parameter Add/CALL; UT37 typed/Legacy persistence/execution; stale S tooltip | Add/save/reopen both modes; execute parameter transfer and FB state; Legacy unchanged; invalid calls diagnose exact binding |
| P14 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | No selection and selected row; undo |
| P15 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Switch continuous/periodic; retain intended values |
| P16 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Create task with custom/default names; reject blank/duplicate without document/history changes |
| P17 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Invalid name, target changes, undo |
| P18 | required | partial | S tree/object forms; UT37 block/task lifecycle and CALL interface; LE08 creation/lifecycle | Protected and removable task |
| G01 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Select BOOL, timer, aggregate |
| G02 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Each supported type; invalid initial |
| G03 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Add/edit/remove member; invalid schema; round-trip |
| G04 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Compatible/incompatible/missing scene point |
| G05 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Blank, duplicate, valid; undo/save |
| G06 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Rename used tag; undo; xref |
| G07 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Used vs unused aggregate/timer tag |
| G08 | required | partial | S tag forms/ARRAY; UT37 tag types/rename/binding/references; LE08 tag/watch; TX17 pending forms | Multiple blocks and derived members |
| I00 | informational | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | All categories at narrow viewport |
| I01 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | NO: valid/missing operand, correct slot, undo/redo, save/reopen |
| I02 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | NC: valid/missing operand, correct slot, undo/redo, save/reopen |
| I03 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | Positive edge: valid/missing operand, correct slot, undo/redo, save/reopen |
| I04 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | Negative edge: valid/missing operand, correct slot, undo/redo, save/reopen |
| I05 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | SET: valid/missing operand, correct slot, undo/redo, save/reopen |
| I06 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | RESET: valid/missing operand, correct slot, undo/redo, save/reopen |
| I07 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | BRANCH: valid/missing operand, correct slot, undo/redo, save/reopen |
| I08 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | − BRANCH: valid/missing operand, correct slot, undo/redo, save/reopen |
| I09 | required | partial | S shared addRung inserts at selectedRung+1; LE08 rungAdded; route-equivalence/order matrix open | From middle and no selection, compare palette/tree/drop/keyboard order and one undo transaction |
| I10 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | TON: valid/missing operand, correct slot, undo/redo, save/reopen |
| I11 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | TOF: valid/missing operand, correct slot, undo/redo, save/reopen |
| I12 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | TP: valid/missing operand, correct slot, undo/redo, save/reopen |
| I13 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | TONR: valid/missing operand, correct slot, undo/redo, save/reopen |
| I14 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | RT: valid/missing operand, correct slot, undo/redo, save/reopen |
| I15 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | CTU: valid/missing operand, correct slot, undo/redo, save/reopen |
| I16 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | CTD: valid/missing operand, correct slot, undo/redo, save/reopen |
| I17 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | LD: valid/missing operand, correct slot, undo/redo, save/reopen |
| I18 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | RES: valid/missing operand, correct slot, undo/redo, save/reopen |
| I19 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | CMP: valid/missing operand, correct slot, undo/redo, save/reopen |
| M01 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | MOV: valid numeric/type case, invalid/domain case, undo and round-trip |
| M02 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | ADD: valid numeric/type case, invalid/domain case, undo and round-trip |
| M03 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | SUB: valid numeric/type case, invalid/domain case, undo and round-trip |
| M04 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | MUL: valid numeric/type case, invalid/domain case, undo and round-trip |
| M05 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | DIV: valid numeric/type case, invalid/domain case, undo and round-trip |
| M06 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | MOD: valid numeric/type case, invalid/domain case, undo and round-trip |
| M07 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | ABS: valid numeric/type case, invalid/domain case, undo and round-trip |
| M08 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | NEG: valid numeric/type case, invalid/domain case, undo and round-trip |
| M09 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | SQRT: valid numeric/type case, invalid/domain case, undo and round-trip |
| M10 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | EXPT: valid numeric/type case, invalid/domain case, undo and round-trip |
| M11 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | LN: valid numeric/type case, invalid/domain case, undo and round-trip |
| M12 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | SIN: valid numeric/type case, invalid/domain case, undo and round-trip |
| M13 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | COS: valid numeric/type case, invalid/domain case, undo and round-trip |
| M14 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | TAN: valid numeric/type case, invalid/domain case, undo and round-trip |
| M15 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | ASIN: valid numeric/type case, invalid/domain case, undo and round-trip |
| M16 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | ACOS: valid numeric/type case, invalid/domain case, undo and round-trip |
| M17 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | ATAN: valid numeric/type case, invalid/domain case, undo and round-trip |
| M18 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | TRUNC: valid numeric/type case, invalid/domain case, undo and round-trip |
| M19 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | NORM_X: valid numeric/type case, invalid/domain case, undo and round-trip |
| M20 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | SCALE_X: valid numeric/type case, invalid/domain case, undo and round-trip |
| M21 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | CONVERT: valid numeric/type case, invalid/domain case, undo and round-trip |
| M22 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | ROUND: valid numeric/type case, invalid/domain case, undo and round-trip |
| M23 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | CEIL: valid numeric/type case, invalid/domain case, undo and round-trip |
| M24 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | FLOOR: valid numeric/type case, invalid/domain case, undo and round-trip |
| M25 | required | partial | S SelectNumericOperation and tree dispatch; UT37 math/domain/JSON; LE08 representative math/scaling/conversion | FORMAT_TEXT: valid numeric/type case, invalid/domain case, undo and round-trip |
| I20 | required | partial | CALL; UT37 typed/legacy transfer/instance/abort/type cases; CA01 form signal pass | Prove bindings/types/directions, FC invocation locals, distinct/shared FB instances, lifecycle/aborts/aliasing, exact diagnostics and legacy round-trip |
| I21 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | RETURN: valid/invalid targets, compiler result and undo |
| I22 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | JMP: valid/invalid targets, compiler result and undo |
| I23 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | LABEL: valid/invalid targets, compiler result and undo |
| I24 | required | partial | S coil tree activation SelectCoilMode(Assign); C drop route; replace-timer route-equality evidence absent | Replace timer with coil via both paths; compare |
| I25 | informational | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | Single-click each family; draft unchanged |
| I26 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | Drop on contact/wire/output/outside |
| I27 | required | partial | S palette/tree dispatch and C drop; UT37 runtime semantics/round-trip; LE08 representative insertion/undo/drop | First/middle/last contact and lock |
| C01 | required | partial | C typed selection/popup and S commands; LE08 WirePopupPreservesSlot/exact deletion; UT37 target/lock tests | Click each hit region including adjacent labels |
| C02 | required | partial | C typed selection/popup and S commands; LE08 WirePopupPreservesSlot/exact deletion; UT37 target/lock tests | Contact/output/network popup then close |
| C03 | required | partial | C typed selection/popup and S commands; LE08 WirePopupPreservesSlot/exact deletion; UT37 target/lock tests | Right-click every insertion position |
| C04 | required | partial | C typed selection/popup and S commands; LE08 WirePopupPreservesSlot/exact deletion; UT37 target/lock tests | Select populated/empty network and delete exact network in one transaction; undo restores IDs/order; no last-contact fallback |
| C05 | required | partial | C typed selection/popup and S commands; LE08 WirePopupPreservesSlot/exact deletion; UT37 target/lock tests | Timer/compare/call help |
| C06 | required | partial | C Shift+F10 implemented; menu-key alias absent; physical focus/edge matrix open | Select target; open/navigate/escape |
| Q01 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q02 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q03 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q04 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q05 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q06 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q07 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q08 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q09 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q10 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q11 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q12 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q13 | required | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Change field, blur and reopen: pending retained/document unchanged; Escape restores original; Apply/Enter commits once; invalid commit stays pending; undo restores original |
| Q14 | informational | partial | S staged property handlers/TX; UT37 InvalidPropertyHasNoPartialEdit; TX17 Apply/undo/navigation; TX19 Escape red | Open RETURN properties |
| W01 | required | partial | S watch routes; UT37 watch persistence/undo/lifecycle; LE08 WatchRefresh/AggregateWatchRevealsMember | Add scalar and leaf twice |
| W02 | required | partial | S watch routes; UT37 watch persistence/undo/lifecycle; LE08 WatchRefresh/AggregateWatchRevealsMember | Select middle row; remove/undo where supported |
| W03 | required | partial | S watch routes; UT37 watch persistence/undo/lifecycle; LE08 WatchRefresh/AggregateWatchRevealsMember | Add all twice; compare rows |
| W04 | required | partial | S watch routes; UT37 watch persistence/undo/lifecycle; LE08 WatchRefresh/AggregateWatchRevealsMember | Clear/save/reopen |
| W05 | required | partial | S watch routes; UT37 watch persistence/undo/lifecycle; LE08 WatchRefresh/AggregateWatchRevealsMember | Double-click scalar and aggregate leaf; verify visible exact root/member after dock collapse |
| W06 | informational | partial | S watch routes; UT37 watch persistence/undo/lifecycle; LE08 WatchRefresh/AggregateWatchRevealsMember | Load/run/edit/stop/reset |
| D01 | informational | partial | S diagnostic navigation; UT37 ExactDiagnosticTargetsOperand; LE08 DiagnosticNavigation/status; TX17 guards | Long issue at narrow dock |
| D02 | required | partial | S diagnostic navigation; UT37 ExactDiagnosticTargetsOperand; LE08 DiagnosticNavigation/status; TX17 guards | Contact, output, task and general issues |
| D03 | informational | partial | S diagnostic navigation; UT37 ExactDiagnosticTargetsOperand; LE08 DiagnosticNavigation/status; TX17 guards | Compile fail then open diagnostics |
| N01 | required | partial | S search/xref/help; UT37 cross-reference/search/catalog; LE08 navigation/help; TX17 navigation guards | Partial tag/block/operand query |
| N02 | required | partial | S search/xref/help; UT37 cross-reference/search/catalog; LE08 navigation/help; TX17 navigation guards | Tag substring collision and cross-block uses |
| N03 | required | partial | S search/xref/help; UT37 cross-reference/search/catalog; LE08 navigation/help; TX17 navigation guards | Offscreen other-block contact and declaration |
| N04 | informational | partial | S search/xref/help; UT37 cross-reference/search/catalog; LE08 navigation/help; TX17 navigation guards | Every family; no blank help |
| K01 | required | partial | C _ShortcutInput; LE08 LADDER_SHORTCUT_FOCUS_VERIFY/navigation; UT37 RecursiveNavigationIncludesTerminalAndSlots | Navigate multiple branches/long rung |
| K02 | required | partial | C _ShortcutInput; LE08 LADDER_SHORTCUT_FOCUS_VERIFY/navigation; UT37 RecursiveNavigationIncludesTerminalAndSlots | Compare each keyboard result with button |
| K03 | required | partial | LadderOperandEditor/TX; UT37 typed/scoped suggestions; TX17 F2/blur/Escape/atomic create+assign; OP03 placement | Scalar/aggregate leaf/timer member/output; exact operand anchored; type filtering, Enter/Escape/blur and undo |
| L01 | required | partial | S dock/scroll/tab handlers; LE08 docks/tool rail; TX17 pending navigation; full DPI/native matrix absent | Collapse/reopen after resize |
| L02 | required | partial | S dock/scroll/tab handlers; LE08 docks/tool rail; TX17 pending navigation; full DPI/native matrix absent | All tabs at minimum size |
| L03 | required | partial | S dock/scroll/tab handlers; LE08 docks/tool rail; TX17 pending navigation; full DPI/native matrix absent | Collapse then invoke each View command |
| L04 | required | partial | S dock/scroll/tab handlers; LE08 docks/tool rail; TX17 pending navigation; full DPI/native matrix absent | Invoke each after collapsing both docks |
| L05 | required | partial | S dock/scroll/tab handlers; LE08 docks/tool rail; TX17 pending navigation; full DPI/native matrix absent | Switch with partially entered form |
| L06 | required | partial | S dock/scroll/tab handlers; LE08 docks/tool rail; TX17 pending navigation; full DPI/native matrix absent | Small supported window, long names, DPI |
| X01 | excluded | excluded | Agreed exclusion; retain honest UI wording; no functional pass claimed | Verify no misleading enabled affordance |
| X02 | excluded | excluded | Agreed exclusion; retain honest UI wording; no functional pass claimed | Verify no misleading enabled affordance |
| X03 | required | partial | UT37 typed transfer/instance timers/counters/edges/lifecycle/alias cases; CALL; stale S interface tooltip | Contract tests prove parameter transfer, state isolation/lifecycle, diagnostics and migrated Legacy unchanged |
| X04 | required | partial | UT37 RecursiveEditorTests tree/IDs/depth/empty shapes/paste/undo; C recursive layout; full native author/save workflow open | Author nested Series/Parallel, save/reopen/import/paste/undo; prove evaluation order and no Boolean distribution |
| X05 | excluded | excluded | Agreed exclusion; retain honest UI wording; no functional pass claimed | Verify no misleading enabled affordance |
| F05 | required | partial | S ShowLadderSaveAsDialog distinct route; ST03 storage; native cancel/failure/path-marker sequence open | Save As success/cancel/failure; old file intact; subsequent Save correct |
| E07 | required | partial | C Ctrl+X and shared cut; TX17 OutputCutKeepsConditions/CutUndo; all typed targets not collectively evidenced | Cut each target, undo/redo, paste; invalid target changes neither clipboard nor document |
| C07 | required | partial | C Shift+F10 implemented; menu-key alias absent; physical focus/edge matrix open | Contact/operand/wire/network, viewport edges; arrows/Enter/Escape; compare pointer menu |
| N05 | required | partial | UT37 CreateTagAndAssignIsAtomic; TX17 CreateAndAssignCommit/SingleUndoRestoresBoth; OP03 placement | New/duplicate/invalid symbol; cancellation; undo removes assignment and new declaration; round-trip |
| E08 | required | partial | UT37 TypedOutputClipboardPreservesExecutableCallMetadata/OutputCopyIsOutputOnly; LE08 OutputPastePreservesConditions | All terminal action kinds including bound CALL; copy/paste/undo; contacts unchanged |
| E09 | required | partial | UT37 fresh-ID network clipboard/undo and recursive import; LE08 rung clipboard; cross-block bound-CALL matrix open | Nested network copy across blocks; valid/invalid calls; undo/round-trip |
| E10 | required | partial | UT37 RecursiveMoveAndRootPruneAreAtomic/PasteRegeneratesAllIds; C recursive popup | Nested Series/Parallel and empty shapes; incompatible output target; cut undo restores IDs |
| B01 | required | partial | CALL; UT37 binding field/type/direction/identity/rename-save-reopen-execution; CA01; stale S tooltip | Missing/duplicate/extra/wrong-type/read-only/alias bindings; diagnostics exact field; save/undo; execute transfers |
| B02 | required | partial | UT37 instance isolation/reopen/lifecycle/reentry; CA01 create+rename; native instance-qualified inspection open | Two instances, shared sequential instance, wrong type/reentry, reset/load and qualified monitor values |
| B03 | required | partial | UT37 explicit conversion/unresolved caller cases; CALL confirmation/transaction; CA01 conversion/undo | Migrate legacy fixture; conversion/undo; callers require compatible mode; prior behavior preserved until conversion |
| X06 | excluded | excluded | Agreed exclusion; retain honest UI wording; no functional pass claimed | UI exposes supported recursive tree only; imported unsupported topology rejects clearly |
| A01 | required | partial | ARRAY default name count+1; duplicate TryRead error actionable; delete-middle/add native case open | Add after deleting a middle field; apply/save |
| A02 | required | partial | ARRAY; UT37 aggregate schema/range model; TX17 array range/type-form checks | Remove used field; Apply/Verify/undo |
| A03 | required | partial | ARRAY; UT37 aggregate schema/range model; TX17 array range/type-form checks | BOOL/INT/DINT/REAL valid/invalid values |
| A04 | required | missing | ARRAY TryChangeType(type,true) resets strings; explicit-reset tests contradict preserve-strings contract | Change BOOL to REAL and back with distinct strings; verify pending strings unchanged and invalid Apply rejected without mutation |
| A05 | required | partial | UT37 absolute-index preservation; TX17 ArrayResizePreservesAbsoluteIndices; full UI Apply/undo removed-values case open | Shift FROM with distinct indexed values, shrink/grow COUNT; verify index identity/defaults; Apply then undo restores removed indices/values |
| A06 | required | partial | ARRAY scroll and4096 rows supported in source; first/middle/last large-array rendered evidence absent | First/middle/last of large array; scroll/edit/save |

Reconciled IDs: 167. No final-candidate acceptance claimed.
