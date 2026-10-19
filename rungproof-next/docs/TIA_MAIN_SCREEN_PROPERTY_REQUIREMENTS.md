# TIA Portal V21 main-screen and property-management requirements

Updated: 2026-10-19

## Purpose and evidence boundary

This document defines the local RungProof Next work needed to make its Siemens
workbench understandable and usable as a TIA Portal V21-style offline training
environment. It is a requirements and execution ledger, not a claim of complete
TIA parity. The supported PLC presentation is S7-1500; unsupported CPU-specific
features must remain identified as unsupported until validated against V21
documentation and a matched installed-TIA project.

Evidence must remain separated:

1. Source review establishes what RungProof implements.
2. Offline gates establish the exercised application routes and saved state.
3. Normal-renderer inspection establishes RungProof's actual appearance.
4. A screen-control walkthrough establishes real user-facing click/key
   behavior. Viewport-injected tests and screenshots do not substitute for it.
5. Matched installed TIA V21 screenshots establish visual/workflow comparison.
   No RungProof evidence alone can close this requirement.

## Main workbench

| ID | Requirement | Acceptance evidence |
| --- | --- | --- |
| MAIN-01 | Keep one consistent TIA-style engineering workspace with project tree, active editor documents, navigation/tool panes, diagnostics/output, and clear offline/online status. | Matched-state captures at the required window size; no clipped titles, hidden controls, or contradictory status. |
| MAIN-02 | Project-tree selection must not accidentally open or mutate an object; double-click and focused Enter must activate the exact selected object. | Exercise every supported block, DB, tag-table, watch-table, and creation row with pointer and keyboard; verify exact target and no content change from selection. |
| MAIN-03 | Open editor documents must retain stable identity and correct title when navigating between blocks, tags, watch tables, and DBs. | Switch repeatedly among documents and verify owner/title restoration, dirty-state prompts, Save/Undo/Redo, and no stale document content. |
| MAIN-04 | Main-screen commands, diagnostics, editing mode, simulator state, and transport state must be visibly distinct. | Offline/connected/blocked status captures and state tests; never imply live TIA or PLC execution from an offline run. |
| MAIN-05 | Scene-level machine commands must expose their runtime prerequisites and report a clear blocked reason when the offline controller is stopped. E-stop/reset state must be visible and remain distinct from a safety-rated circuit. | Through the live screen bridge, click scene Start while STOPPED and confirm a visible controller-prerequisite message; run the offline controller, exercise Start/Stop and E-stop/reset, then leave STOPPED with `estop_ok=true` and `conveyor_run=false`. This proves simulator UI behavior only. |

## Property and declaration management

| ID | Requirement | Acceptance evidence |
| --- | --- | --- |
| PROP-01 | Every supported property form loads the selected object's current values, validates before mutation, and has clear Apply/Cancel behavior. | For each form: valid edit, invalid edit, Cancel, Apply, reopen, Undo, Redo, Save/Load/Save, and stable object identity. |
| PROP-02 | Selection state is scoped to the owning block and persistent declaration ID. A row index from one block or an earlier row order must never retarget an edit. | Select a declaration, insert/reorder same-block rows, rename/edit it, and prove its persistent ID follows it; switch to another block with a same-index row and prove only the newly selected row can change. |
| PROP-03 | Global DB declarations are edited as declarations with Name, Data type, Start value, Retain, and Comment. The declaration grid must expose direct Add/Edit/Remove actions and a clear return path to the project tree; a side form may supplement, but not replace, the visible declaration table. Changes and block-scoped selection must persist transactionally. | Select, add, edit, and remove rows through the visible DB table; verify invalid edits, Cancel, Apply, Undo/Redo, Save/Load/Save, duplicate rejection, two-DB isolation, and exclusion from executable block targets. |
| PROP-04 | Global DB presentation must distinguish the supported scalar optimized-access subset from TIA's configurable declaration columns and access modes. Show the supported Name, Data type, Start value, Retain, and Comment fields with usable widths. | RungProof renderer capture at matching zoom; capability and omitted-column disclosure; V21 reference comparison. Do not display offsets for optimized access or imply unsupported standard access. |
| PROP-05 | FB instance DB declarations show assigned FB, Input/Output/InOut/Static members, inherited default/start values, Retain metadata, comments, and generated IEC timer/counter members. TEMP locals are excluded. IEC_TIMER includes PT, ET, IN, and Q. | Gate covers FB sections, inherited values, IEC PT/ET/IN/Q and counter Q/CV, no TEMP, Save/Load identity, and clear read-only/override disclosure. |
| PROP-06 | Per-instance start, comment, and Retain overrides, power-cycle retention behavior, and online Monitor values are either implemented end-to-end or visibly disclosed as unsupported. | If implemented, verify value storage, precedence, persistence, inheritance update behavior, Undo/Redo, and offline/online distinction. Otherwise keep them explicitly out of the supported feature set. |
| PROP-07 | PLC tag management supports the declared subset without suggesting that omitted TIA constants/attributes exist. The complete parity target includes PLC tags, User constants, and the Default/All-tags System constants view. | Add typed rows/tabs, validation, duplicate handling, addressing, comments, table membership, drag-to-operand, persistence, and undo. System constants must be read-only hardware metadata, not invented. |
| PROP-08 | Watch tables support symbolic or absolute-address entries, type-valid display formats, live monitor values, and simulator-safe modify values. | Verify row add/remove/reorder, display-format rendering, value entry/type validation, simulator-only modification, quality/status, Save/Load, and Undo/Redo. Never imply the action writes to a physical PLC. |
| PROP-09 | Create/remove/rename/duplicate and navigation actions are consistent across blocks, tag tables, and watch tables. | Invalid names are rejected atomically; valid operations persist; one Undo/Redo restores exact serialized states and selection. |

## Visual parity criteria

- Use TIA Portal V21 as the target baseline.
- Siemens V21's [data-block declaration table reference](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/structure-of-the-declaration-table-for-data-blocks)
  lists Name, Data type, Default value, Start value, Monitor value, Snapshot,
  Retain, HMI visibility/access/write attributes, Setpoint, Supervision, and
  Comment; Offset is shown for standard access, while online Monitor value is
  conditional on an online connection. The supported RungProof scalar grid is
  intentionally smaller and must disclose omitted columns and its fixed
  optimized-access model.
- Siemens V21's [Global DB retentivity reference](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/setting-retentivity/setting-retentivity-in-a-global-data-block)
  distinguishes per-tag Retain under optimized access from centralized retain
  behavior under standard access. Do not present the current Retain checkbox as
  proof that power-cycle retention is implemented by the offline runtime.
- Compare the same editor object, declaration/member set, zoom, selection,
  display settings, and online state. Record monitor/modify state separately.
- Match hierarchy, table columns, row grouping, selection feedback, editor tabs,
  dock sizing, toolbar placement, captions, and diagnostics placement before
  expanding instruction-level appearance.
- Preserve legibility at the required window sizes; no horizontal clipping,
  hidden edit fields, or truncation of meaningful names/comments.
- Do not copy transient device-specific offsets, system constants, monitor
  values, or enabled controls into offline fixtures unless their provenance is
  explicit.
- Track all differences as supported, intentionally omitted, or open pending
  evidence. Do not label a custom RungProof control as a TIA control.
- A DB editor action must name the destination and return action. Any side dock
  used for declaration fields must fit its controls at the supported viewport
  and restore the project tree without losing the active DB document.

## Execution plan

| Batch | Work | Exit gate |
| --- | --- | --- |
| 0 | Preserve current dirty work; confirm canonical repo/branch; establish current normal-renderer captures; record the native-window CUA gap and the session-only browser bridge used for screen control. | Source revision, status, clean build, baseline captures and review notes are current. |
| 1 | Main workbench layout and navigation parity: shell chrome, project tree, document tabs, inspector/work panes, diagnostics and status. | Requirement MAIN-01 through MAIN-04 have source, offline, rendered, and interaction evidence; remaining TIA differences are listed. |
| 2 | Property form correctness: transactional selection, form loading, validation, Cancel, Apply, Undo/Redo, and Save/Load across existing editor forms. | PROP-01/02/09 gates pass, including cross-block same-index isolation. |
| 3 | Data blocks: finish Global DB editing and declaration presentation; decide the supported optimized-access model; implement or disclose advanced columns and access modes. | PROP-03/04/06 gates pass and the Global/Instance DB normal-renderer captures are inspected. |
| 4 | PLC tags and constants: editable tag metadata, User constants, System constants display, and all-tags navigation. | PROP-07 round-trip and row-action gates pass; hardware constants are sourced from project metadata. |
| 5 | Watch table: addresses, display formats, modification values, simulator execution/quality, and safe offline messaging. | PROP-08 row-action and persistence gates pass; no route reaches a physical PLC write. |
| 6 | Independent review and manual walkthrough. | Independent code review has no unresolved high-severity findings; screen-control agent exercises each relevant visible function and records pass/fail evidence. |
| 7 | TIA V21 comparison and closeout. | Matched installed-TIA captures and a requirement-by-requirement evidence audit are complete; report installed-TIA gaps separately from local application results. |
| 8 | Demo ladder-logic remediation, **last**: audit Demo 1–5 against their machine behavior; move every sequence, interlock, motor command, simulated E-stop latch, reset, encoder count/position handoff, and fault response into executable LAD/FB calls. Scripts may supply scene metadata and animation bindings only. Demo 3 is a pusher cell; Demo 4 Start/Stop/E-stop/Reset must operate; Demo 5 must call its motor/axis and encoder FBs and pass their data into the main sequence. | For each demo, trace scene input → ladder rung/FB → output/DB → scene motion and status; exercise start, stop, E-stop, reset, permissive/fault cases, and prove the result from runtime state. Label simulated E-stop behavior as training logic, never as a safety-rated circuit. |

Do not start Batch 8 until Batches 1–7 are complete or each remaining GUI/TIA
parity gap has an explicit accepted disposition. Logic acceptance cannot be
inferred from authored scripts, rungs drawn without execution, or animations
that move without ladder-owned outputs.

## Current state and known open items

### Latest review snapshot — 2026-10-18

- CUA's native application provider returns no Godot windows, even while
  RungProof is running. This is a provider-binding limitation, not evidence
  that the process is closed. The local browser bridge works when started with
  `-TargetProcessId <visible-RungProof-PID>` and opened at its matching port
  and generated key. A fresh bridge pinned to disposable PID 30740 returned a
  live 1328×837 screen. The default unpinned helper is ambiguous when several
  Godot windows are visible; never reuse a key with another listener or PID.
  Keep this session-only input relay on loopback and use scratch data only.
- The Siemens tag Add and Apply handlers now validate the supported address
  syntax, type/role pairing, address bounds, and overlap before committing.
  Siemens-target validation/diagnostics for imported project documents remain
  open. The shared project JSON loader has no target-vendor field and serves
  both the TIA and Studio 5000 views, so keep generic loading lossless and add
  Siemens-specific diagnostics only at a vendor-aware boundary. Studio 5000
  needs its own validator after its address rules are established. TIMER,
  COUNTER, and aggregate tag-address policies also need explicit treatment.
- Watch entries remain persisted as symbol strings. Absolute addresses,
  display formats, and simulator-only modify values require a watch-entry
  model and schema change; they cannot be completed as a layout-only fix.
- Global DB authoring and JSON loading now apply the same case-insensitive
  member-name rule; blank and duplicate names and non-Static members are
  rejected by the loader. The remaining Global DB evidence gaps are duplicate
  rejection through the visible UI and Save/Load/Save. Tag-table lifecycle
  remains add-only; watch tables support add/remove but not rename, duplicate,
  or reorder.
- The latest review/walkthrough are separate evidence streams. A visible
  browser-mirrored screen proves RungProof click behavior only; it does not
  establish installed-TIA parity, live PLC behavior, safety-rated behavior, or
  ladder ownership of scene actions.
- On the disposable scratch process, the independent screen-control pass
  selected Watch table 1 and used Enter to open Diagnostics/Watch. `+ SYMBOL`
  changed the count from 5 to 6; `REMOVE` restored 5; `ADD ALL` restored the
  missing eligible entry; a second named watch table was created, selected,
  and removed with Undo. No Save/Load occurred, the controller remained
  STOPPED, and the PLC stayed DISCONNECTED. This process started before the
  Siemens address-validator change, so that specific Add/Apply behavior still
  needs a fresh current-source run. The `CLEAR` action has not yet been
  verified by this pass.

- Global DB declaration editing now exposes `+ MEMBER`, `EDIT`, and `REMOVE`
  beside the central declaration grid. These actions target the selected row
  and use the existing validated property form and undoable document changes.
  The member form now follows persistent declaration IDs across same-DB row
  insertions and renames; it uses a name/index fallback only for legacy
  declarations without IDs. New model declarations receive stable IDs, and
  project-tree selection no longer calls the PLC-tag-only `tag:index` row
  helper. The production-shell gate exercises same-DB insertion and then edits
  the selected ID. A two-DB same-index switch test protects against
  cross-block edits.
- PROP-03 remains **PARTIAL**: the current Global DB gate covers in-memory
  declaration Add/Edit/Remove routes, selection, Undo/Redo, and same-index DB
  isolation, but it does not exercise duplicate-member rejection or
  save-to-disk/reopen. Those are still required acceptance evidence; do not
  infer them from JSON snapshot comparisons or a successful UI edit.
- Instance DB table layout and inherited FB start-value rendering have local
  offline gate coverage. IEC timer members now follow the V21 IEC_TIMER schema
  (PT, ET, IN, Q), asserted in row order and by type. The normal-renderer
  capture `.tools/instance-db-direct-layout-v1.png` was inspected: it shows
  the X_Axis_DB declaration grid, grouped Input/Output/Static rows, and the
  offline feature boundary without clipping at 1600×900. Per-instance
  overrides, access-mode/offset selection, monitor/snapshot values, HMI
  attributes, setpoints, and supervision remain unsupported and are disclosed
  in the instance view.
- MAIN-03's block identity defect is **fixed in source**: program tabs resolve
  persistent block IDs, update titles after rename/reorder, disable when their
  block is deleted, and bind to that same ID again after Undo. A regression
  test covers rename, earlier-block deletion/reindex, Undo, and Redo; the
  production-shell project-tree gate exercises the corresponding tab buttons,
  including startup verification that the initial tab uses its persistent ID
  and does not leave the old `block:0` key behind. A fresh current-source
  screen-control session also opened Demo 5's X_Axis_DB [DB101] from the tree
  and visibly confirmed its owner title and grouped six-column declaration
  grid. This is local simulator UI evidence, not installed-TIA parity.
- PLC tag views omit User/System constants and several TIA attributes.
- Watch rows omit address editing, display-format selection, and a modify-value
  workflow.
- The Global DB declaration grid now has direct Add/Edit/Remove commands and
  an inspected current-source normal-renderer capture at
  `.tools/global-db-direct-actions-v1.png`. The property form remains the
  editor for selected declarations. The source also handles inline edits for
  Name, Data Type, Start Value, Retain, and Comment. Manual current-build
  commit/recovery behavior and file Save/reopen coverage remain open. The form
  has an explicit Cancel action; Remove
  resolves pending edits before deletion, covered by an exact
  Cancel/Discard/Undo/Redo gate. The form also tracks Retain-only edits and
  supplies an ENUM
  initial value from the member domain (seeding `Value1` when that domain is
  empty). Untouched defaults also change to the selected scalar type. The
  current project-tree sweep passes 9 gates / 65 assertions, including
  visible blank-name rejection with unchanged document and Undo history.
- The Global DB declaration grid remains a bounded scalar optimized-access
  subset; broader TIA column/access-mode support is still open.
- MAIN-03 project replacement now clears project-owned document tabs and
  selection maps before refreshing the replacement project. Its gate opens a
  tag table, instance DB, and non-entry block, then opens a second project with
  the same project-local initial block ID and verifies that only the new active
  block tab remains selected. Re-run this gate after changes to project-open,
  tab identity, or editor-document navigation.
- The project-tree gate now passes with 65 assertions, including project-open
  tab replacement and ID/DB-number collision checks. At
  800×600, the last verified narrow-layout run rendered the member dock at 281 px,
  wrapped all four actions, kept them inside the dock, and restored the prior
  240 px width. The blank-name assertion visibly reports validation, focuses
  Name, and preserves both serialized DB state and Undo availability. An
  independent code review found no new defect in these changes. The fast and
  interaction parity profiles also passed; all remain offline RungProof
  evidence, not installed-TIA or PLC evidence.
- A fresh-PID screen-control pass used Add new block → Enter, created an
  unsaved Global DB, and visibly applied `AuditMember BOOL FALSE` into the DB
  grid and member list. Escape closed the pending-changes prompt while keeping
  the draft. The Remove click did not produce an unambiguous visible result;
  existing-row Edit/Cancel and successful Remove remain unverified. No Save
  was used, and the isolated process/helper were closed without affecting the
  original unsaved Demo 5 window.
- CUA's native-app inventory is empty, but the existing Chrome extension tab
  titled `RungProof CUA Screen Bridge` exposes the live native Godot window at
  1616×939 when bound in the owning CUA session. An independent screen-control
  pass opened the Global DB view and Edit Members form, inspected NEW and the
  return-to-Project-Tree path, and canceled Add Tag Table; existing DB and tag
  values stayed unchanged. It did not Apply, Remove, Save, Load, or test the
  complete form matrix. The running window is already UNSAVED and uses a form
  without the source revision's Cancel control, so it was not restarted. This
  proves input and those navigation routes in that session only, not every
  current-source action or an installed-TIA match.
- The latest bridge session confirmed the browser-mirrored native window and
  DB navigation. A STOP click returned an already-running offline runtime to
  STOPPED. A Run attempt from STOPPED did not start it; that window was
  UNSAVED and showed a verify/load-before-run condition, so it was left
  untouched. A second screen-control session sent a coordinate click while
  the root session was active and the runtime became RUNNING; the root session
  stopped it. Coordinate control is usable, but simultaneous bridge sessions
  are not a reliable walkthrough method. No current-source Add/Edit/Remove
  click walk or matched installed-TIA interaction has been completed. The
  older Add Tag Table dialog was canceled before commit; no DB or tag content
  was saved or changed.
- An independent screen-control agent separately verified Return to Scene,
  Logic Editor, DB1 → Edit Members, and opening/canceling the Add Watch Table
  dialog. It confirmed Default tag table was already an open tab but did not
  edit values. Project Tree collapse/expand and splitter resizing remain
  unverified because coordinate clicks sometimes selected adjacent rows or
  properties. No Save/Load was used.
- After the walkthrough, a fresh screenshot showed an empty active watch table.
  Undo reported `ADD WATCH TABLE`; applying it restored a four-symbol table.
  Because earlier screenshots disagreed on the count and active table identity,
  the watch-table scratch state remains inconclusive. No project Save was used.
- The local bridge helper now serializes input and screenshot refresh, avoids
  dispatching a second click after pointer down/up, and has an explicit drag
  control. PowerShell parsing passed. Through the restarted bridge, Run and
  Stop again produced visible runtime changes; the controller was left stopped.
- The screen walkthrough then verified the DB `EDIT MEMBERS` and `PROJECT
  TREE` routes in both directions, opened Default tag table with Enter, and
  displayed its Name/Address/Comment/Type/Role/Initial/Binding columns and
  selected-tag properties. It opened Add new tag table and explicitly clicked
  Cancel; the dialog closed without adding a tree item. No property field was
  edited.
- A controlled splitter drag widened the Project Tree dock by roughly 60 px;
  the reverse drag returned it to approximately its starting width. This is
  direct screen evidence for resizing, but exact pixel persistence across the
  DB member-editor route has not yet been exercised. Current scratch state is
  unsaved, DB1 remains open, the tag-table tab is active, runtime is stopped,
  and PLC status is disconnected.
- A separate CUA walkthrough on the live offline Conveyor Inspection Cell
  exercised Return to Scene, built-in Run/Stop, the declared conveyor Start,
  simulated E-stop, blocked Start while E-stop was latched, and E-stop Reset.
  The screen showed `estop_ok` change false then true, Start blocked until the
  controller was running, and the controller stopped without changing PLC
  connection state. This is visible local-model behavior, not evidence that
  ladder logic owns those controls or that the E-stop is safety-rated. The
  final stopped-screen capture is ephemeral at
  `rungproof-next/.tools/cua-walkthrough-main-controls.png`.
- The screen-control walkthrough also selected Watch table 1 in the Project
  Tree, confirmed selection alone left the DB document active, then
  double-clicked the selected row and confirmed the watch workspace opened.
  Selecting its `start_command` row highlighted the exact row without editing
  it. The visible table shows Symbol, Type, Value, Role, Quality/State, and
  Scene Binding; address, display-format, and modify-value editing were not
  exercised and remain unsupported gaps. The stopped-state capture is
  ephemeral at `rungproof-next/.tools/cua-walkthrough-watch-table.png`.
- A direct click on the Global DB's `EDIT MEMBERS` control opened the visible
  declaration form, and `PROJECT TREE` returned to the tree while retaining the
  DB document. This verifies navigation only; the live CUA session did not
  change member fields or exercise duplicate rejection, Apply/Cancel, Undo/Redo,
  or file Save/Load through that form.
- The user-authorized loopback bridge is now available to CUA through Chrome
  for the current workstation session, despite the native-app inventory
  returning no app windows. Return to Scene, Logic Editor, Run/Stop, the
  simulated E-stop latch, its blocked-start condition, and explicit reset were
  visibly exercised. Coordinate fields must be set and checked before a
  separate click action; combined updates sometimes used the previous target.
  One live screen showed cartons at a different position after a
  `conveyor_run` scene action while the PLC-to-scene output row read FALSE;
  the observation was not synchronized to a scan. Independent source review
  traced the table to current scene points, output commit before table refresh,
  and motion gating on `conveyor_run && estop_ok`, so the screenshot alone
  cannot establish a bypass. A separate `RunDefault()` path directly sets the
  generic conveyor point and requires review in the logic phase. The app was
  left STOPPED, reset, disconnected, and UNSAVED; no property values were
  edited.
- The second screen-control pass confirmed Logic Editor↔Scene, DB1 Edit
  Members and return, Default tag table, and Add Watch Table/Cancel. Run, Stop,
  E-stop, and reset showed status/history/point changes. Some actions appeared
  to need a repeated click, but repeats sometimes activated neighboring
  controls; accept only state-verified actions and do not prescribe blind
  retries. Project Tree collapse/expand, splitter resizing, and the final
  Return-to-Scene attempt remain unverified. The latest pass ended in the
  editor with STOPPED, `estop_ok=true`, PLC disconnected, and project UNSAVED.
- Installed TIA Portal V21 captures and project interchange have not been
  performed. No parity or live-PLC claim follows from the local gates.
- Latest live-screen correction: the native app inventory remains empty, but
  the active Chrome loopback bridge exposes the running app. Clicking the
  Conveyor Inspection Cell Start/Stop action while the offline controller was
  STOPPED displayed `Action blocked · Run the controller before issuing a
  machine command`; `conveyor_run` stayed FALSE and `estop_ok` stayed TRUE. The
  control is responsive and gated, not dead. A 60 px Project Tree splitter drag
  followed by a reverse drag visibly widened and restored the dock. Collapse /
  reopen and the complete property-form matrix remain open. The independent
  walkthrough's Run/Stop and simulated E-stop/reset evidence remains separate;
  the current session was left STOPPED, PLC DISCONNECTED, and UNSAVED.

## 2026-10-18: PID-pinned CUA recovery and independent review

The native-app inventory still returns no windows because the Godot process is
not exposed through the native-app provider. This is a discovery/provider gap,
not evidence that RungProof is unavailable. A local Chrome bridge rendered the
native window, and the dedicated bridge on port 18766 was verified to target
PID 33392. The exact bridge token is intentionally omitted. Use a bridge that
is bound to the intended process; matching window titles alone are ambiguous
because two Godot processes are open.

The independent walkthrough exercised scene/editor navigation; OB1 selection
and activation; tag-grid navigation; Add Tag Table, Add Program Block, and Add
Watch Table dialogs with Cancel; watch-table view/selector; Playback Run,
Stop, and Reset; simulated E-stop and reset; and the conveyor command's
permissive/controller-running gates. With the offline controller running,
conveyor Start/Stop was accepted and cartons visibly advanced. Final state was
OB1 active, local runtime STOPPED at scan 0, `estop_ok=true`, PLC DISCONNECTED,
and no project Save or edits. These observations are local simulator evidence
only.

The restored unsaved draft used for this walkthrough contains only
`Conveyor_Main [OB1]`. The DB member editor was therefore not reachable without
creating a DB in the user's in-memory draft; the creation dialog's DB choice
was inspected and canceled. Watch `+ SYMBOL` produced no visible change during
one click. This is inconclusive because the selection may have been empty or
already present in the table; verify it with a not-yet-watched tag in a
disposable project before treating it as a defect. No saved project state was
changed.

To verify the DB layout without changing that draft, I ran a fresh current-source
normal-renderer capture using the built-in `--capture-instance-db-definition`
fixture. `.tools/instance-db-layout-turn2-v2.png` now visibly shows the X-axis
instance DB as a declaration table with grouped Input/Output/Static rows,
Name/Data Type/Default Value/Start Value/Retain/Comment columns, and the
read-only capability note. The first attempt used `--capture-state=stopped`,
which starts a 45-frame capture and skips the fixture hook scheduled at frame
100; rerunning with the default capture state invoked the fixture and produced
the inspected image. This confirms current-source rendering, but not live
pointer interaction or a matched TIA V21 comparison.

Independent source review recorded these UI/property findings for the next
GUI batches; the first two have since been corrected in code:

- CPU-wide PLC tag/block symbol uniqueness now compares names
  case-insensitively across tag tables and block declarations; compiler checks
  imported programs as well. Regression coverage passes, but installed-TIA V21
  comparison remains open.
- Editor block tabs previously used shifting ordinals; stable block IDs and a
  delete/Undo-aware regression now protect against that retargeting.
- Global DB's declaration table still relies on a side form for editing; the
  declaration-grid interaction, duplicate rejection, and file Save/reopen
  acceptance are open.
- PLC User/System constants and watch-table address, display-format, and
  simulator-only modify workflows remain absent or explicitly unsupported.

The user-supplied DB screenshot shows the older paragraph-style “Interface
storage” view. Current source contains no such label: ordinary Project Tree
activation resolves the assigned FB instance DB to the six-column declaration
grid. A fresh current-source Demo 5 screen-control session now directly opened
X_Axis_DB [DB101] and confirmed the group headings and Name/Data Type/Default
Value/Start Value/Retain/Comment columns. A separate disposable Global DB also
showed the declaration grid with Add Member/Edit/Remove. No member was edited
or saved; both test sessions were discarded, and the original unsaved draft
was left unchanged. The older screenshot therefore shows a different build
or session, not the current-source DB101 screen.

The independent review did not execute the UI or tests and did not establish
installed-TIA V21 parity. Keep all these items in the GUI/property batches;
Demo ladder remediation remains the final batch as defined above.

## Siemens V21 references

- [Data block declaration table](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/structure-of-the-declaration-table-for-data-blocks)
- [Start-value rules](https://docs.tia.siemens.cloud/r/en-us/v21/programming-data-blocks/defining-start-values/basic-information-on-start-values)
- [PLC tag-table structure](https://docs.tia.siemens.cloud/r/en-us/v21/declaring-plc-tags/structure-of-the-plc-tag-tables?contentId=WO~yJSVCPRXPhLZRr~9VSQ)
- [Declaring global constants](https://docs.tia.siemens.cloud/r/en-us/v21/declaring-plc-tags/declaring-global-constants/declaring-global-constants)
- [Creating and editing watch tables](https://docs.tia.siemens.cloud/r/en-us/v21/testing-the-user-program/testing-with-the-watch-table/creating-and-editing-watch-tables/creating-a-watch-table?contentId=BVks1B30kTbTXRScDxAHpQ)

## 2026-10-18: CUA recovery and full main-screen walkthrough

The native-app inventory still returns no windows; that inventory is not proof
that the Godot window is closed or unreachable. The Chrome extension bridge on
port 18765 targets the older unsaved process (PID 26948, 1616×939). The
current-source process (PID 33392, 1416×829) has a separate local-only bridge
on port 18766. A fresh in-app browser tab using the local bridge URL and its
session token reopened that exact process and returned a live screenshot. This
resolves the session-binding and wrong-process problems: the native app list
can remain empty while a browser tab provides live screen and input access. The
token is intentionally not recorded here. The workaround stays on loopback and
does not add native-window discovery, public network access, or a browser build
of RungProof.

An independent screen-control walkthrough used that connected bridge and
verified controller Run/Stop, Conveyor Start, simulated E-stop latch and
reset, built-in controller Reset, Scene/Logic Editor navigation, Project Tree
collapse/reopen, splitter widen/restore, DB1 Edit Members navigation, existing
tag-table selection, and Add Tag Table / Add Watch Table cancellation. Visible
machine state changed: cartons moved, `photoeye_blocked` changed, E-stop set
`estop_ok=false`, and reset restored `estop_ok=true` while leaving the
controller STOPPED. Cancel left Default tag table at 5 symbols and Watch table
1 at 4 symbols. The app ended STOPPED, `estop_ok=true`, `conveyor_run=false`,
PLC DISCONNECTED, scan 0, project UNSAVED. No declarations or tag values were
edited and no Save/Load action was used. This is simulator UI/model evidence,
not real PLC, safety-rated E-stop, ladder-ownership, or installed-TIA proof.

In the resumed browser-control session, the PID-pinned bridge accepted clicks
on the current-source window: Logic Editor → Scene, Run → RUNNING, Stop →
STOPPED, simulated E-stop → `estop_ok=false`, and built-in Reset →
`estop_ok=true` with the runtime still STOPPED. The window was returned to the
Logic Editor. No tag/DB values were edited, no Save/Load was used, PLC remained
disconnected, and the UNSAVED project was left intact. These are simulator
screen-control results; the E-stop remains a symbolic training behavior, not a
safety-rated circuit or proof that ladder logic owns the action.

The independent code review found that `NewInterface` reset a dirty member form
without resolving it and showed parameter wording in a Global DB. It now routes
through the shared Apply/Discard/Cancel resolver; the gate checks Cancel keeps
the draft and Discard clears it atomically. Global DB actions say `+ MEMBER` /
`APPLY MEMBER`. A separate owner-block identity now refreshes the labels and
Static/Input defaults on initial DB entry and DB↔FB navigation. The
project-tree sweep passes 9 gates / 56 assertions. The current local review
artifact is `.tools/tia-parity-member-form-context-artifacts/tia-project-tree-gates.json`.

## 2026-10-18: Current-source verification continuation

The current source builds cleanly and the project-tree sweep passes 9 gates / 57 assertions with `externalTia=false` and `livePlc=false`. The native-app inventory is still empty, and the old Chrome bridge remains bound to the first Godot process. A local-only helper now accepts and validates an explicit PID; a second bridge instance on port 18766 is pinned to current-source process 33392 and opened in a fresh in-app browser tab. This solved the screen-control binding problem without closing or changing the older unsaved process.

On the current-source window, the startup dialog dismissed, Run changed the visible controller status to RUNNING, Stop returned it to STOPPED, E-stop changed `estop_ok` to false, and reset restored `estop_ok=true`. The Logic Editor opened. A conveyor command displayed `ACTION BLOCKED · Run the controller before issuing a machine command`, so the bottom Run/Stop control is not proof that the selected ladder controller is executing. This click walkthrough covers current-source shell controls only; it does not prove machine sequence, ladder-owned scene motion, DB property workflows, safety-rated E-stop behavior, or installed-TIA parity. The window was left STOPPED with E-stop reset, PLC disconnected, and no project edits.

The gate manifest previously had contradictory project-tree counts (50 in the local-run command summary versus 57 in its artifact) and a stale registry hash/total. The stable count is now 57, the test checks the same value, the bound project-tree artifact SHA-256 is refreshed, and the local-run evidence hash is rebound in the evidence registry. The prior 372/372 virtual-controller result predates this manifest correction; the full suite must be rerun to establish the updated result. The local evidence remains offline-only.

## 2026-10-18: Browser-bridge recovery and Global DB member-panel correction

The native CUA inventory still reports no app windows for Godot. A local-only
browser bridge, pinned to the disposable `.tools/cua-sandbox` process, works in
the parent screen-control session. The bridge tab provides a live screenshot
and relays coordinate clicks; its coordinates are relative to the native window,
so the browser toolbar offset must be excluded when mapping screenshot pixels.
Each helper launch creates a random key and is pinned to an explicit PID. A key
sent to the wrong listener or an expired PID binding returns `forbidden` or
`unavailable`. The native CUA inventory does not expose a Godot window ID. The
independent agent's first browser attempt failed because it had a stale bridge
binding; after refreshing the bridge against the sandbox PID, it independently
verified the Global DB member panel, Cancel behavior, and Project Tree return.
This is screen-control evidence for the disposable current-source sandbox, not
installed-TIA V21 parity evidence.
The current walk used only this sandbox and left the primary user-owned unsaved
processes untouched.

Opening `DB1 [DB2]` and choosing `+ MEMBER` exposed a real current-source
presentation defect in the shared member dock: it called global DB declarations
an `DB INTERFACE`, showed a disabled `STATIC` selector, and displayed a generic
FB/FC capability note. The Global DB table itself showed the five supported
columns (Name, Data Type, Start Value, Retain, Comment) and Add/Edit/Remove
actions. The dock now says `GLOBAL DB DECLARATIONS`, hides the implied Static section selector, and gives a Global DB-specific optimized-access subset note. It requests 560 px on wide viewports; on narrower viewports the member actions wrap so they remain reachable, then the previous dock width is restored when returning to the project tree. The FB capability note remains specific to FB editing. No declaration was applied or saved during the screen inspection.

The updated source builds with 0 warnings/errors. The production project-tree
gate passes 9 gates / 65 assertions; `externalTia=false` and `livePlc=false`.
The gate checks that the Global DB dock label, hidden Static selector, and
context-specific note match the selected block. A fresh-source browser-bridge walkthrough confirmed the corrected label, hidden Static selector, Global DB note, and full-width member controls at a 1328×837 sandbox window. Cancel left the draft empty, and Project Tree restored the original dock width. Installed TIA V21 comparison and Save/Load parity remain open.

## 2026-10-18: CUA native-window workaround and current tag-address attempt

The Windows CUA app inventory still returns `apps: []` while Godot windows are
open. The app is therefore not addressable through `getApp(windowId)` in this
session. The workaround is a loopback browser bridge bound to an exact visible
RungProof PID. A fresh browser tab connected to the scratch PID and reported
`connected · 1328×837`; it showed live Godot pixels and relayed coordinate
clicks, Tab, and text to the native window. This is a usable screen-control
surface even though it does not populate the native-app inventory. The helper
must be launched with the intended PID and matching listener URL/key; old tabs
can otherwise target an older listener or report a stale binding.

On a rebuilt current-source scratch process, I entered an invalid string in
the unsaved tag-address editor and navigated to the form's `+ ADD TAG` control.
The button was visible and clicked, but the screen did not expose a clear
validation result or a reliable row-count delta during this attempt, so the
interactive rejection behavior remains unverified. No Save or Load was used;
the scratch process was stopped after the check. The validator's source tests
remain separate evidence. Do not count this as successful tag-address UI
verification.

The independent source review also identified follow-up parity gaps: Siemens
address diagnostics for imported documents need a vendor-aware audit boundary
(generic project loading must stay vendor-neutral); watch entries currently
store symbols only and cannot represent per-entry absolute address, display
format, or modify value; DB member uniqueness needs loader and authoring-path
coverage; and tag/watch table rename, delete, duplicate, and reorder actions
are missing. These findings join the GUI/property remediation plan. Ladder
execution and scene ownership remain deferred until after GUI parity work.

## 2026-10-18: Browser bridge recovery and project-tree follow-up

The CUA native-app inventory still returns `apps: []`, but the Chrome browser
provider can control a local-only bridge pinned to the isolated current-source
Godot process. The active bridge rendered the native window and relayed clicks;
the bridge tab and helper remain available for continued review. This is a
workaround for CUA's missing native-window binding, not a browser build of
RungProof. Never copy the session token into project files or reports.

Screen control verified closing the only open OB1 document tab while keeping
the `Conveyor_Main` project-tree row, then reopening the block. A separate
screen agent opened the Default tag table, inspected its visible address and
role columns, opened Watch table 1 and inspected the symbol/quality controls,
opened the block-creation dialog and saw OB/FB/FC/DB choices, and created a
disposable Global DB. Its screen showed the five-column declaration grid
(Name, Data Type, Start Value, Retain, Comment) and Add/Edit/Remove actions.
The DB choice explicitly excludes manual instance-DB creation. An empty tag
table was accidentally created while aiming at the creation dialog's X; this
was confined to the unsaved sandbox. Because bridge coordinates can hit a
different dialog control than intended, this is not evidence that the product's
Cancel/X behavior is defective. Main-block activation from the separate agent's
coordinate attempt was also inconclusive. Do not save this sandbox.

The source verifier's stale tab lookup was updated to follow each tab wrapper
and resolve its child button by stable block identity. The focused project-tree
gate passes 9/9 gates and 62/62 assertions at the documented real 2048x1152
window size. A `--headless` attempt is invalid for this geometry check because
Godot supplies a synthetic 64x64 viewport; the apparent member-dock clipping
was caused by that unsupported viewport. The offline build succeeds with zero
warnings/errors, the virtual-controller/editor suite passes 376/376, and the
app-shell verifier passes for 77 scenes, five demos, and 295 assets. Evidence
remains local/offline; no installed-TIA or PLC execution occurred.

Direct inline Global DB row editing is implemented in source and the five-column
grid is visible in the current-source UI. The gate now verifies draft locking
and Cancel restoration, but manual per-column commits, invalid-value recovery,
Undo/Redo for each inline column, and Save/Load/Save remain open. Remaining
property work includes the full V21 declaration presentation, manual
instance-DB/instance-override support boundaries, PLC User/System constants and
tag attributes, and watch table address/display/modify workflows. A current
screen-control pass confirmed tag-table and watch-table creation dialogs open
on double-click and Cancel without adding an object. The tree's Add new block
path and Global DB creation route are now visibly confirmed; single-click
activation and remaining property controls still need function-by-function
screen walkthrough.
Matched installed TIA V21 captures remain required before claiming visual
parity. Demo sequence and safety logic remain deferred until this GUI/property
batch is closed.

## 2026-10-18 independent review and verification continuation

Independent source review found that JSON loading accepted non-Static
declarations in a Global DB even though authoring rejects them. The loader now
rejects that malformed project with a Global DB diagnostic, and a regression
case covers it. The earlier DB row-traversal finding was stale: current source
walks direct declaration-row siblings, and the focused gate confirms all five
cells lock during a pending member edit and restore after Cancel. Apply-unlock
and Save/Load/Save remain unasserted.

The independent screen pass confirmed the visible five-column Global DB grid,
member actions, and DB-to-Project-Tree route. Add Member→Apply produced a
visible declaration row. Inline Name, Start value, Retain, and Comment edits
were visibly reflected in the grid; the BOOL Start value was restored after
inspection. Alternate-type editing was not verified. Remove redraw was
ambiguous. A focused screen retry confirmed existing-row Edit when selection
is explicit: click the Name cell without typing, press Escape to leave inline
editing, then click EDIT. The member pane loaded the selected member's name,
BOOL type, FALSE start value, and Retain state; its action caption changed to
APPLY MEMBER. Cancel returned to a blank form and preserved the row. The earlier
blank form was a selection-state issue, not a confirmed Edit defect. The Default tag table exposes
Name, Address, Comment, Type, Role, Initial, and Binding; selecting a row loads
its property form. Watch table 1 shows symbol quality, `PROGRAM NOT LOADED`,
and Add/Remove/Add All/Clear/Add Table controls. Scene Start/Stop displays a
blocked reason when no ladder controller is loaded; simulated E-stop changes
`estop_ok` true-to-false and Reset restores it. After starting the local
simulator, Start changed `conveyor_run` false-to-true and cartons moved; a
second click changed it back to false. Bottom STOP returned LOCAL RUNTIME to
STOPPED, while Watch table 1 showed RUNNING / SIMULATOR ONLY and advancing
scan status. All scratch changes remain unsaved. These are RungProof screen
results, not installed-TIA or safety-rated proof.

The current source builds with zero warnings/errors. The visible 2048x1152
project-tree gate passes 9/9 gates and 62/62 assertions; the virtual-controller
suite passes 376/376; the app-shell verifier passes for 77 scenes, five demos,
and 295 assets. Headless geometry is not layout evidence because its synthetic
viewport produced an inconsistent dock-fit sample. Matched installed-TIA V21
captures, confirmed Global DB Remove result,
full property
Save/Load workflows, and the remaining screen walkthrough are still open.

## 2026-10-18: Browser bridge recovery and DB inline-edit review

The native CUA app inventory still reports no Godot windows. A loopback-only
Chrome tab now mirrors a visible RungProof window and relays coordinate clicks
to that exact process, so the parent CUA session can perform the required
screen-control work without a browser build of RungProof. The bridge is bound
to one process and does not expose a public network service. A separate agent
cannot claim the parent-owned tab; it can create its own bridge tab, although
the screenshot-to-window mapping is now calibrated: numeric bridge inputs use native coordinates in the 1456x849 captured image; clicks on the displayed image subtract the browser-page origin automatically. A later frame may be needed to observe Godot redraws. Keep one controller per target PID because concurrent agents can interleave input and screenshots.

In the parent session, the current-source TIA editor visibly showed the Global
DB declaration table with Name, Data Type, Start Value, Retain, and Comment,
plus + MEMBER, EDIT, REMOVE, and PROJECT TREE. Retain and comment changes were
visible in the disposable unsaved DB. Text replacement through the bridge was
not reliable, so manual Name/Start Value/Type inline commit behavior is still
unverified. An independent screen agent visibly opened + MEMBER; its later
coordinate attempts became ambiguous and it created only unsaved scratch data. In the current parent session, the actual Create Program Block dialog visibly offers OB/FB/FC/DB; choosing DB identifies Global DB and states that ARRAY DB, PLC/system-type DB, and manual instance DB creation are unsupported.
Creating a disposable unsaved DB visibly opens a declaration grid with NAME, DATA TYPE, START VALUE, RETAIN, COMMENT, plus + MEMBER, EDIT, REMOVE, and EDIT MEMBERS. No Save was used. This is current RungProof screen evidence only, not installed-TIA parity.
No Save was used, and no manual screen result is treated as installed-TIA
parity.

Independent source review found that inline grid edits could race an unapplied
member form. All Global DB cells are now disabled while a property draft is
pending and restored when the draft is applied or discarded. The focused
project-tree gate confirms the draft disables all five cells and Cancel restores
them without changing the DB. The updated gate passes 9/9 gates and 62/62
assertions at 2048x1152. The virtual-controller suite passes 376 tests with zero failures; the app-shell verifier passes for 77 scenes, five demos, and 295 assets.
Evidence hashes are refreshed. Installed TIA V21 comparison and
manual inline-column commit behavior remain open.

## 2026-10-18: Fresh-build live instance-DB confirmation

CUA continues to report no native Godot app. The working workaround is the
local-only `.tools/cua-desktop-bridge.ps1` helper, started with an exact visible
process ID and a distinct loopback port. One previously bridged process had
started before the current managed assembly was built. It was left untouched
because it contained an unsaved draft. A separate process was started from the
current build and bound to its own bridge.

The fresh screen-control pass dismissed the startup notice, opened Scenario
Browser, selected Demo 5, entered Logic Editor, and opened both
`X_Axis_DB [DB101]` and `Y_Axis_DB [DB102]`. Both live document tabs showed the
assigned FB and the six-column Name / Data Type / Default Value / Start Value /
Retain / Comment declaration table with interface section groups. Clicking a
Start Value cell did not enter edit mode; the visible note accurately discloses
that per-instance overrides and online features are not modeled. No project
data was edited or saved. This live check confirms the current RungProof build
does not show the older paragraph-style “Interface storage” layout from the
user screenshot. It does not establish a visual match to installed TIA V21.

The next instance-DB parity change must be a complete vertical slice. Start
value overrides require stable per-member identity, typed validation,
serialization/migration, Undo/Redo, and one-time runtime initialization; until
those are implemented together, the accurate read-only boundary stays in the
UI. Retain and Comment editing remain open design/evidence items because the
project does not yet model TIA access mode, retentive power-cycle behavior, or
instance-vs-FB comment semantics.

## 2026-10-19: Project replacement and isolated screen-control follow-up

Project replacement now clears old editor tabs and project-local document
maps before refreshing the replacement project. The new native project-tree
gate opens a tag table, instance DB, and non-entry block in the old project,
then opens a project that deliberately reuses the old block IDs and DB number.
It confirms that the replacement opens on its active block with only that
document tab selected and that the loaded project retains the DB-number
collision. The gate passes 9/9 with 65/65 assertions; the editor-control gate
passes 7/7 with 36/36 assertions. The controller/editor suite passes 377/377.
These are local offline results (`externalTia=false`, `livePlc=false`).

A separate screen-control agent used its own scratch process and loopback
bridge; the pre-existing unsaved process was left untouched and no scratch
project was saved. The visible walk reached the scene, Logic Editor, default
tag table, watch table, table-creation dialogs, Global DB creation, and Add
Member. Clear visibly changed the watch count from five to zero, and Add Member
visibly added `audit_bit | BOOL | FALSE`. In this run, Watch Remove and Global
DB Edit/Remove clicks did not produce an unambiguous visible result; delayed
repaint and coordinate targeting limit that evidence. Instance DB navigation
was not reached. Existing earlier Edit/Remove evidence remains separate and
does not turn this incomplete retry into a pass.

The broader interaction profile initially exposed an internal verifier that
selected `EditorDocument_0` by generated node name. It now finds the initial
block tab by stable document key. The final interaction profile passes all
14/14 commands, including ladder-editor, project-tree, property-edit, and
view-state routes. Installed TIA V21 visual comparison, instance-DB editing,
and confirmed Remove behavior remain open.
