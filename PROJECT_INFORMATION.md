# Project information

## Canonical repository

- Product: **RungProof**
- Repository: `mnwinter/RungProof`
- Default branch: `main`
- Local clean checkout: `C:\Users\matt.winter\Documents\RungProof`

## Current architecture

### October 2026 playtest and laptop migration

Current explicit work limit: stop project testing, discovery, repairs and agent
work at **2026-10-23 04:00 America/Los_Angeles (11:00 UTC)**. The user authorized
only final checkpoint/skill updates and verified D:/Git backup closeout after
that time, then pause the active goal. Do not resume automatically after this
cutoff. See `rungproof-next/outputs/screen-audit-2026-10-23/work-order.json`.

Current October23 discovery is indexed by that folder's
`native-continuation-checkpoint.md`, `hard-issue-index.md` and finite native
queue. Frozen parcel V7 completes three deliveries on fresh scene load and
after a qualified handoff Stop/Run, but two actual Reset repeats stall at
medium outbound phase50. Source analysis isolates a transform/world-AABB reset
ordering hazard; no repaired application build is claimed. Read later scoped
reviews before using historical broad failures or pending-case lists.

The next-agent entry point is `skills/rungproof-playtest-handoff/SKILL.md`.
The migration record is `RUNGPROOF-MIGRATION-2026-10-22.md`. The full native
77-scene acceptance audit remains incomplete; read the skill's current-state
reference and latest individual native reviews before repeating old cases or
assuming historical ledger cells are current. The deadline application backup
was `41188c70bb86e3305f7cd7c8759f000f9c396729`; later migration work preserves
newer QA evidence and saved drafts without claiming a new application fix.

`rungproof-next/` is the current Godot 4.7.2/.NET 10 application. It contains
the integrated ladder editor, virtual controller, visual scene catalog,
training challenges, deterministic offline test harnesses, and current runtime
assets.

The surrounding repository contains only dependencies that the current source
references:

- original scene contracts used by migration/parity tooling;
- guarded PLC profiles and Python bridge code;
- the pinned Siemens interface vendor snapshot;
- third-party notices and contributor/project guidance.

Generated imports, SDK downloads, compiler outputs, release builds, logs,
backups, and superseded review archives are not source and are excluded.

## AI development handoff

`skills/rungproof-development/SKILL.md` is the maintained entry point for an AI
coding agent continuing RungProof development. It routes to focused references
for the project map, controls and live-PLC boundaries, Siemens TIA evidence,
and verification. Keep it aligned when those governing contracts change.

## Siemens TIA Portal V17 UI parity

The current main-screen and property-management acceptance ledger is
`rungproof-next/docs/TIA_MAIN_SCREEN_PROPERTY_REQUIREMENTS.md`; the active
execution order and status dashboard is
`rungproof-next/docs/TIA_PARITY_BATCH_PLAN.md`. Keep local source, automated
gate, screen-control, and installed-TIA V17 evidence distinct. UI/property
parity remains in progress, installed-TIA comparison is partially captured, and
the five-demo ladder-logic remediation has completed its local offline
acceptance under the user's revised work order. That demo result does not close
the remaining main-screen/property, matched installed-TIA, or live-PLC gates.

The first three installed-V17-driven GUI corrections are now in current source. At the
required 1600×900 RungProof frame, the TIA project dock defaults to 250 px
instead of 340 px, duplicate OPEN/PROPS header controls are hidden, and the
selection help names double-click/Enter plus right-click Properties. Full
program-block and instance-DB identities remain available in row tooltips. The
document header also hides redundant LOCATE/PROPS controls, and the Global DB
header retains only compact + MEMBER, EDIT, and REMOVE actions; declaration
Properties remains on the row context menu. Both focused regressions pass, and
the normal-renderer captures `outputs/tia-v17-compact-project-dock.png` and
`outputs/tia-v17-compact-global-db-toolbar.png` were inspected. Broader V17
pairing and the pre-existing project-tree gate failures remain open. The Watch
document now relies on its project-tree row and editor tab instead of repeating
table selection and lifecycle controls in the header; absolute-address entry is
collapsed behind `ADDRESS…` until requested. The same 1600×900 review also
found and fixed the top-level view switcher collapse: `SCENARIO` and
`LOGIC EDITOR` retain their full 92 px and 116 px widths. The focused interaction
and width assertions pass, and `outputs/tia-v17-compact-watch-document.png` was
inspected. The PLC-tag document follows the same rule: table lifecycle remains
in the project tree; Properties and Cross-reference remain on the selected-row
context menu; only compact `+ TAG`, `EDIT`, and `VERIFY + LOAD` actions stay
visible. Its focused assertion passes and
`outputs/tia-v17-compact-plc-tag-document.png` was inspected. That view now also
uses compact 30 px Tags/User constants/System constants selectors at
64/120/128 px and the concise boundary `OFFLINE SIMULATOR · no TIA project file
or Siemens PLC connection`.
The instance-DB document now relies on declaration-row activation and its
right-click Properties action rather than a permanent 170 px MEMBER PROPERTIES
button. The focused assertion passes; both the clean declaration view and the
still-functional property route were rendered and inspected in
`outputs/tia-v17-compact-instance-db-document-recheck.png` and
`outputs/tia-v17-instance-member-properties-route.png`.
The User constants view now keeps only compact `+ CONSTANT` and `EDIT` primary
actions; Properties, Cross-reference, Duplicate, and Delete remain on the row
context menu. The focused assertion passes, and both
`outputs/tia-v17-user-constants-compact.png` and the still-functional
`outputs/tia-v17-user-constant-properties-route.png` were inspected.
`VERIFY + LOAD` is now scoped to the Tags view; it is hidden from both User
constants and read-only System constants. The focused three-view interaction
assertion passes, and `outputs/tia-v17-system-constants-compact.png` was
inspected.

Current 2026-10-20 parity continuation: User Constants are executable immutable
LAD inputs with typed literal lowering, read-only destination enforcement,
rename following, referenced-delete blocking, and CALL Input selection. The
focused regressions pass. Block/routine and tag-table duplication are now
transactional source features with stable-identity and Undo/Redo coverage. The
full virtual-controller suite passes 396 tests
and has one separate evidence-integrity failure: the current local project-tree
artifact hash differs from the hash recorded in `tia-local-run-evidence.json`.
Do not update that recorded hash without a fresh controlled parity run.

The User Constants grid now has direct object-style property management:
double-click or right-click `Edit`, right-click `Cross-reference`, and
right-click `Delete`. Edit and Delete reuse the existing mutation/history path;
Delete is disabled while the constant is referenced or the editor is locked.
Cross-reference reuses the project cross-reference dock with the selected
constant name. A compile-only build completed with 0 warnings and 0 errors; no
installed-TIA, screen-control, or live-PLC verification is claimed by that build.

The Siemens PLC tag table now provides the same direct row workflow: right-click
`Edit`, `Cross-reference`, or `Delete unused tag`, while double-click continues
to open the tag editor. The menu selects the row under the pointer, uses the
existing tag form and project cross-reference dock, and disables deletion for
referenced tags or while editing is locked. A compile-only build completed with
0 warnings and 0 errors; no broader parity or runtime sweep was run.

Individual PLC tags and user constants now also support right-click
`Duplicate`. User constants copy their typed value, tag-table membership, and
comment under a unique CPU symbol. PLC tags copy type, role, initial value,
aggregate/ENUM schema, table, and comment, but intentionally clear address and
scene binding so two declarations cannot claim the same endpoint. Both actions
use normal editor history and honor lesson edit locks. The compile-only build
passed with 0 warnings and 0 errors; no broader sweep ran.

## Five-demo curriculum GUI

Current 2026-10-20 learner-facing GUI work adds a guided curriculum card to
the Scenario Browser and carries the active lesson context onto the Operator
Console. The UI now shows lesson number, objective, operator exercise, PLC
focus, Previous/Next navigation, and a direct Open Demo Logic action for the
five authored demos. The top menu is labeled `App Playback`, and the Operator
Console explicitly distinguishes simulator Run/Stop/Reset from the declared
machine actions consumed by ladder logic. Simulated trip controls remain
clearly described as training-only, not safety-rated functions.

Each lesson also has a current-runtime completion criterion and session-latched
learner feedback: Demo 1 `threshold_lamp=TRUE`, Demo 2
`delayed_lamp=TRUE`, Demo 3 `parts_completed>=3`, Demo 4
`pallet_count>=5`, and Demo 5 `placed_cartons>=4`. The Scenario Browser and
Operator Console show `IN PROGRESS` or `PASSED THIS SESSION` plus the exact
proof condition and overall passed count. Resetting a machine does not erase
the session's learner progress; restarting RungProof does.

The capstone procedures now teach repeatability explicitly instead of ending at
the first pass. Demo 4 requires machine Reset followed by the first valid event
of a second batch, proving the counter restarts at one without a fabricated held
edge. Demo 5 requires Reset, fresh Start, and a second complete four-carton
layer after the Stop/trip/permissive recovery exercise. The pass dialog's
focused repeat action supports those rehearsals without erasing other lessons.

Completed demos now receive checkmarks in the Scenario Browser. The curriculum
card includes an explicit `RESET CURRICULUM + SIMULATOR` action that clears all
session completion marks and resets the local simulator/scene together, which
prevents an already-true completion point from immediately relatching a cleared
mark. Demo 1, Demo 4, and Demo 5 scene-control labels were also made specific;
Demo 4 now shows `PALLET`, `TYPE OK`, and `COUNT REQ` faces instead of three
generic unlabeled inputs. Demo 1 and Demo 2 PLC tags now carry learner-facing
comments, and all five checked-in demo project files were regenerated from the
authoritative authored documents.

Demo 1 and Demo 2 now also publish their internal teaching state through
explicit PLC-owned numeric outputs rather than UI-only copies. Demo 1 moves the
CTU accumulator into `press_count_value`, bound to an on-machine `COUNT`
display. Demo 2 converts `delay_timer.ET` into `delay_elapsed_ms`, bound to an
`ELAPSED MS` display. Both outputs are included in their authored watch tables,
and the five checked-in project files were regenerated from the authoritative
authored documents after the change. Their curriculum procedures, action hints,
PLC-focus descriptions, and live traces now name those displays and output tags
directly, connecting the internal CTU/TON state to the visible machine result.
Their per-scene machine guides now state the expected display sequence and
machine-reset result as well.

The Operator Console's declared machine actions are now state-aware. Maintained
scene inputs render as toggle buttons with live `ON`/`OFF` text and green active
styling; momentary inputs are explicitly labeled `MOMENTARY`. Tooltips identify
the exact PLC point and whether the action is maintained or momentary. The
buttons synchronize from `SceneSimulationRuntime.Points` rather than keeping a
separate UI-only state.

The Operator Console now orders those buttons into `MACHINE CONTROLS /
INTERLOCKS` and `PROCESS / SENSOR SIMULATION` groups. Each curriculum lesson
also carries a three-step operating procedure in both the Scenario Browser and
Operator Console. The procedures explicitly distinguish App Playback from
machine inputs, identify required maintained states and off-to-on event edges,
and include Stop/trip/permissive-loss, cause-clear, Reset, and fresh-Start work
where appropriate.

Curriculum navigation now reacts to success: a passed lesson changes the action
to `PASSED · NEXT: DEMO n`, and completing all five produces the explicit
`CURRICULUM COMPLETE · 5/5` state in both the Scenario Browser and Operator
Console. The curriculum-reset button is disabled until at least one lesson has
been passed.

Both the Scenario Browser curriculum card and Operator Console now include a
five-button lesson rail. The buttons now expose the lesson purpose directly as
`1 COUNT`, `2 TIMER`, `3 SORT`, `4 BATCH`, and `5 CELL` instead of five generic
Demo labels. Each button opens its demo directly, the active lesson is marked
with `▶`, completed lessons are marked with `✓`, and tooltips show the full
lesson name, objective, and exact proof condition. Those tooltips now update to
show Current, Passed, Next Recommended, or Not Yet Passed; completed lessons
also expose their captured scan evidence and pass-time signal trace on hover.
The Operator Console rail hides for non-curriculum scenes, while Previous/Next
remains available for linear study. The compile-only build passed with 0
warnings and 0 errors; no background, screen-control, installed-TIA, or runtime
sweep was run.

Both curriculum surfaces now show a prominent state-aware `NEXT ACTION`. A
failed qualification names the immediate correction (offline mode, load,
Run, force removal, or matching demo logic); a qualified lesson gives its
specific operator action; a passed lesson points to the next unfinished demo;
and 5/5 offers review or curriculum reset. These hints are authored per demo so
they name the actual controls and proof signals instead of giving generic
training advice. The compile-only build passed with 0 warnings and 0 errors;
no broader sweep was run.

`NEXT ACTION` is now executable from both curriculum surfaces. Its button
returns to the built-in simulator, opens the demo logic for `Verify + Load`,
clears local simulator forces, requests scan Run, scrolls to Lesson Inputs, or
opens the next unfinished/review demo according to current state. Qualification
priority is now offline mode → matching loaded logic → zero forces → running
scan → lesson operation, so the curriculum no longer advises starting a stale
or mismatched controller program. The compile-only build passed with 0 warnings
and 0 errors; no parity, screen-control, installed-TIA, or runtime sweep ran.

The matching-logic branch of `NEXT ACTION` now invokes the existing Siemens
`VERIFY + LOAD OFFLINE` command directly instead of only opening the editor. It
selects the TIA workspace and then follows the normal pending-form resolution,
compiler validation, Error List, and offline-runtime load path; no parallel or
weakened loading path was introduced. The compile-only build passed with 0
warnings and 0 errors; no broader sweep ran.

The always-visible bottom transport now includes a curriculum button. During a
lesson it shows the active demo number, session pass count, and a checkmark when
that demo has passed; pressing it returns directly to the Operator Console.
Outside a curriculum scene it shows overall progress and opens the next
unfinished demo (or Demo 1 review after 5/5). This preserves the learner's route
back to lesson controls after Verify + Load or other engineering views. The
compile-only build passed with 0 warnings and 0 errors; no broader sweep ran.

Each completed demo now retains session-local pass evidence: the exact proof
value used by the completion rule and the local virtual-controller scan number
at first qualification, plus a pass-time snapshot of every signal in that
lesson's live trace. Both curriculum surfaces display the proof and snapshot;
the completion dialog and copied session results retain the same captured state
instead of substituting later live values. Restart-current removes only that
demo's evidence; curriculum reset clears all five. The UI labels it local
virtual evidence and does not imply installed-TIA or live-PLC proof. The
compile-only build passed with 0 warnings and 0 errors; no broader sweep ran.

Both curriculum surfaces now provide `COPY CURRICULUM SESSION RESULTS`. The
clipboard summary lists all five lessons, passed/not-passed state, captured
local proof, scan number, and pass-time signal snapshot for completed lessons,
and the required proof for unfinished lessons. It ends with the explicit
local-only evidence boundary and stays disabled until at least one lesson
passes. This is a session handoff, not an installed-TIA or live-PLC report. The
compile-only build passed with 0 warnings and 0 errors; no broader sweep ran.

Main-screen discovery now names `Five-Demo Curriculum & Scenarios` directly in
the Scenario menu, and Help includes `Open Five-Demo Curriculum Guide`. That
Help command now opens a real quick-start dialog listing the Count, Timer, Sort,
Batch, and Cell progression; explaining the App Playback versus machine-input
boundary; and giving direct `OPEN CURRICULUM` and `RESUME NEXT DEMO` actions.
Its lesson map uses the same visible instrument names as the scenes: `COUNT`,
`ELAPSED MS`, `HEIGHT MM`, `DONE`, batch `COUNT`, `X/Y/Z CNT`, and `PLACED`.
The dialog reads current session progress when opened, names the first
unfinished lesson, labels its action `RESUME DEMO n`, and changes to `REVIEW
DEMO 1` after 5/5. At completion it also reveals `START FRESH CURRICULUM`, which
uses the shared all-progress reset, resets the simulator/scene, and opens Demo 1
in Operator Console for a clean new run. That reset now runs inside the accepted
workspace-replacement callback; cancelling an unsaved-workspace prompt leaves
all curriculum passes and evidence intact.
The Operator Console no longer calls the ladder workspace `BASIC LOGIC`; its
action is `OPEN DEMO n PLC LOGIC` for a curriculum lesson and `OPEN CURRENT PLC
LOGIC` elsewhere.

The always-visible bottom transport now labels its lifecycle controls `APP RUN`,
`APP STOP`, and `APP RESET`, with green/red/amber treatment and tooltips that
explicitly state they do not assert the lesson's PLC-consumed machine
Start/Stop/Reset inputs. This matches the already-renamed `App Playback` menu
and removes the last prominent unlabeled Run/Stop/Reset ambiguity.

The curriculum card now includes a live 0–5 progress bar. Maintained-action
labels in Demos 2–5 were shortened to selector/status names so the runtime
suffix reads cleanly (for example, `SYSTEM ENABLE SELECTOR · ON`) instead of
duplicating `Toggle` or `OFF / ON` in the same button.

The Scenario Browser now marks every curriculum row as `PASSED`, `CURRENT`,
`NEXT`, or not yet passed, with objective and proof details in its tooltip.
When the learner returns from another scene, both curriculum entry actions
resume the first unfinished demo instead of restarting at Demo 1. After 5/5,
those actions explicitly offer a Demo 1 review while the reset action remains
available for a fresh curriculum run.

The Scenario Browser curriculum-detail viewport was increased from 218 to 310
pixels and explicitly retains internal scrolling, with a scroll hint. This keeps
the expanded qualification, proof, live trace, objective, procedure, PLC focus,
and troubleshooting content readable without consuming the separate scenario
list below it.

The visible Viewer browser now places the five authored lessons in a dedicated
`FIVE-DEMO CURRICULUM` section above `OTHER SCENES`, in curriculum order. It
uses the same Passed/Current/Next markers and objective/proof tooltips, and its
search matches learner-facing demo names, objectives, and PLC-focus text rather
than requiring knowledge of the underlying lab filename.

Both the Scenario Browser curriculum card and Operator Console now include
`RESTART DEMO n · KEEP OTHER PASSES` for an active curriculum lesson. They use
one shared action that resets the simulator/scene and clears only that lesson's
session pass, allowing targeted rehearsal without erasing progress on the other
four demos. The controls hide for non-curriculum scenes so they cannot be
mistaken for general scene commands.

The first qualified transition to each lesson's completion condition now raises
an explicit green workspace status and operator-history event: `DEMO n PASSED ·
NEXT RECOMMENDED: DEMO m`. Passing the fifth lesson instead raises `CURRICULUM
COMPLETE · ALL FIVE DEMOS PASSED THIS SESSION`. Scan refreshes after the pass do
not repeat or flood the event because completion remains session-latched.

That first qualified transition now also opens a learner-facing completion
dialog containing the captured proof value and local virtual scan number. Until
all five are complete, it names and opens the first unfinished lesson rather
than assuming the learner completed them in numeric order, while retaining a
`STAY` choice. The same dialog now offers compact `TRACE`, `MONITOR`, and
`REPEAT DEMO` actions, with tooltips describing the full scene/logic, offline
watch, and focused-rehearsal behavior. This keeps the single-row action strip
usable even when the final dialog also reveals `START FRESH`. The repeat
action uses the same simulator/scene reset and single-lesson evidence removal as
the curriculum-card and Operator Console restart controls. The final pass
changes the dialog to the explicit five-demo completion state and offers
`REVIEW DEMO 1` plus the same guarded `START FRESH CURRICULUM` action available
from the guide. The dialog is raised only when a new session pass latches, so
subsequent scans do not repeatedly interrupt the learner. Its popup expands for
long signal snapshots while remaining clamped to the current viewport, keeping
the Demo 5 trace and action row readable. Non-final pass titles include overall
progress such as `Demo 3 passed · 3/5`.

The Scenario Browser card, Operator Console, and demo-row tooltips now show a
live qualification checklist before the proof condition: built-in simulator,
scan running, zero forces, matching demo logic loaded, and correct source scene.
Each item uses a visible check or failure mark, so `NOT QUALIFIED` no longer
requires fixing one hidden prerequisite at a time.

Each lesson now shows a compact live trace of its essential scene/PLC boundary
signals beside the proof condition. Demo 1 follows pulse/reset/lamp, Demo 2 the
request/stop/reset/lamp path, Demo 3 measured height/conveyor/pusher/completion,
Demo 4 event qualification/count/validity, and Demo 5 cell state, all three
encoders, vacuum, placed count, and layer completion. The signal names are also
searchable in the Viewer browser.

That live trace now resolves controller-internal values as well as scene I/O:
BOOL, numeric, text, TIMER (`ET`/`Q`), and COUNTER (`CV`/`Q`) snapshots. The
lesson rows therefore expose the actual `press_count`, `delay_timer`, machine
run/latch state, batch FB values, and palletizer `sequence_step` alongside the
physical inputs and outputs instead of omitting the teaching state between them.

The Scenario Browser curriculum card and Operator Console now provide
`OPEN DEMO n LIVE MONITOR` beside the PLC-logic action. They route directly to
the offline virtual-controller monitor and the demo's authored watch list;
their tooltips explicitly state that these are local simulator values, not
live-PLC evidence. Non-curriculum Operator Console scenes receive the generic
`OPEN CURRENT LIVE MONITOR` label.

Both curriculum surfaces also provide `TRACE DEMO n · SCENE + PLC LOGIC`,
opening the existing split workspace with the machine model and displayed
ladder side by side. This gives the curriculum an explicit input → logic →
output teaching route without requiring repeated manual switching between
scenario and logic-only views. The curriculum-card study actions remain hidden
until one of the five lessons is active, preventing them from opening an
unrelated scene's program or watch list.

Maintained field conditions now use maintained-looking scene controls: Demo 4
`PALLET`, `TYPE OK`, and `COUNT REQ`, plus Demo 5 `PALLET`, use rotary selector
style. START, STOP, RESET, and event pulses remain pushbuttons. This visual
contract matches the action semantics learners see in the Operator Console.

Operator action colors now carry control meaning: Start is green, Stop red,
Reset amber, and ordinary maintained permissives become green when active.
Active simulated `safety_trip` and `drive_overload` inputs become red instead
of green, avoiding the unsafe implication that an asserted trip/fault is a
healthy state.

Curriculum passes are qualified, not inferred from a point value alone. A new
pass requires the built-in simulator, a running controller, no simulator
forces, the displayed draft matching the loaded controller program, and the
demo's exact source scene. External mode, stopped scans, forces, or a mismatched
draft show `NOT QUALIFIED` with the corrective action and cannot latch progress.

The authored scene labels were cleaned up so learner-visible indicators and
equipment describe their actual function instead of generic lab names. Demo 3
System Enable, simulated trip, and overload are now rotary maintained controls;
its formerly unbound status beacon is explicitly bound to the PLC
`conveyor_running` output. Demo 3 also has supported numeric readouts for the
existing measured `carton_height_mm` feedback and simulated `parts_completed`
count, labeled `HEIGHT MM` and `DONE`, so the 600 mm boundary case and three-part
completion are visible on the machine. Demo 4 identifies its inspection
conveyor, sample, station, and valid-batch lamp. Demo 5 identifies carton
infeed/pickup, vacuum, cell-active, and layer-complete indications.

The five scene control stations now use the same control-color contract as the
Operator Console: Start/permissive controls are green, Stop/trip/overload are
red, and Reset is amber. Demo 5's displayed scene and gantry names now say
`Integrated XYZ Palletizing Cell` and `PLC-controlled XYZ palletizing gantry`,
matching the three independently commanded axes instead of the legacy XY label.
Demo 5 also places a supported numeric count display beside its cell-active and
layer-complete lamps. It reads the existing PC-owned `placed_cartons` measured
feedback as `PLACED`, giving the learner an on-machine 0–4 progress indication
without creating another counter or changing PLC ownership. A second display
row shows the existing `gantry_x_encoder_counts`, `gantry_y_encoder_counts`, and
`gantry_z_encoder_counts` feedback as `X CNT`, `Y CNT`, and `Z CNT`. The lesson
procedure and action hint now direct the learner to follow those measured values
through pickup, transfer, release, and home. Demo 5's machine guide now uses the
same instrument names and documents their zeroed Reset state. Demo 3's help page
now lists the `HEIGHT MM` and `DONE` equipment/bindings and keeps them explicitly
downstream of modeled feedback rather than process decisions.

Demo 3 now also carries the same structured in-scene `machineGuide` contract as
the other four curriculum lessons. It documents start conditions, the fixed
420/820/600 mm boundary sequence, main/side destinations, `HEIGHT MM` and `DONE`
observations, Stop/trip/overload recovery, machine Reset state, and the explicit
non-safety-rated boundary. This removes the curriculum's only missing structured
scene guide without changing its PLC or plant behavior.

Completion criteria for the more complex lessons require the final PLC status,
not just a numeric plant value: Demo 4 needs `pallet_count>=5` and
`pallet_count_valid=TRUE`; Demo 5 needs `placed_cartons>=4` and
`layer_complete=TRUE`. The live curriculum status displays both values.

Curriculum Start/Previous/Next and Scenario-menu selections now enter the
selected lesson's Operator Console only after the workspace-replacement guard
accepts the scene change. Cancelling a guarded replacement leaves both the
current scene and view intact; deferred save/discard approval carries the view
transition inside the accepted callback.

The Siemens project tree now exposes supported object management where users
expect it: right-clicking a program block, named PLC tag table, or watch table opens an
`Open / Properties / Rename / Duplicate / Delete` menu. These routes reuse the
existing transactional editor commands and Undo/Redo history. Actions are
disabled when the selected object is protected, referenced, non-empty, or the
lesson has locked project mutation; block Properties opens the project-object
inspector and Rename focuses the block-name field. Watch-table deletion remains
disabled when it would remove the project's last watch table.

Project-tree folder rows now participate in the same workflow: right-clicking
`Program blocks`, `PLC tags`, or `Watch tables` (including their visible
`Add new...` child rows) opens the appropriate block, tag-table, or watch-table
creation dialog. Lesson mutation locks disable the creation command in place.

Generated instance-DB rows now expose `Open` and `Properties` through the same
project-tree menu. Rename, Duplicate, and Delete remain visibly disabled because
their identity and lifecycle belong to the FB or IEC-instruction assignment
that generated them.

Program-block context menus now include `Cross-reference`. It opens the existing
project search/cross-reference dock with the selected block symbol and lists its
definition, calls, and scheduling references; the command remains disabled for
tag tables, watch tables, and generated instance DBs where a block-symbol query
would be misleading.

Project-tree `Properties` is now distinct from `Open` for PLC tag tables, watch
tables, and generated instance DBs. A dedicated object-properties dialog shows
the selected object's name and type plus tag/constant counts, watch-row count,
or DB number and owning FB/IEC instruction. It also states whether the object is
editable and, for generated instance DBs, directs changes to the owning
assignment. The compile-only build passed with 0 warnings and 0 errors; no
screen-control, installed-TIA, parity, or live-PLC sweep was run.

Generated instance-DB Properties now includes `OPEN OWNER`. For FB instances it
locates the actual CALL using the stable instance identity, opens its owning
block/network, selects the CALL terminal, and opens CALL properties. For IEC
timer/counter DBs it opens the owning instruction network and property form.
If the CALL was removed but the FB definition remains, it falls back to the FB
block rather than retargeting an unrelated DB. The dialog callback is wired
through a deferred action assigned only after the editor controls exist,
avoiding construction-order access to uninitialized controls. The compile-only
build passed with 0 warnings and 0 errors; no parity, capture, or evidence sweep
was run.

Tag-table deletion availability is now consistent across the project-tree
context menu, Delete-key route, tag editor, and Properties dialog. A user table
must contain neither PLC tags nor User Constants before Delete enables. Its
Properties summary states `Available`, identifies a built-in table, or reports
the exact tag/constant counts that protect the table from deletion. The
compile-only build passed with 0 warnings and 0 errors; no parity, capture, or
evidence sweep was run.

Watch-table rows now provide the same object-style interaction in both the
bottom inspector and central watch-table document. Right-click can open the tag
declaration, run cross-reference, edit display format/modify value/comment,
move the row, or remove it. Declaration and cross-reference are disabled for
absolute-address rows, and mutation commands honor the lesson edit lock. Row
actions now resolve selection from the visible watch table, preventing a stale
central-document selection from overriding a docked-table action. The
compile-only build passed with 0 warnings and 0 errors; no broader sweep ran.

The block-interface declaration grid now supports direct row property
management. Double-click focuses the selected declaration's form; right-click
offers `Edit declaration`, `Properties`, `Move declaration up/down`, and
`Delete declaration`, while the visible command row also provides `PROPS`.
The Properties dialog reports owner/type/semantic mode, stable identity,
position, section, type, initial value, ENUM domain, retain attribute, comment,
the exact Input/Output/InOut/Static/Temp runtime meaning, owner CALL-site count,
and binding count. Deletion is now protected while a CALL binding targets the
declaration instead of leaving an invalid stale binding. The comment editor is
now visible for every FB/FC/OB interface declaration, and applying an edit
preserves the authored comment instead of silently clearing it. Reordering and
deletion continue through normal editor history and respect the lesson lock.
Both compile-only solution builds passed with 0 warnings and 0 errors; no
parity, screen-control, installed-TIA, runtime, capture, or broader evidence
sweep ran.

The central Global DB declaration grid now has matching row-level property
management. Right-click offers `Edit properties`, `Move declaration up/down`,
and `Delete declaration`; existing inline cell editing remains available.
Reordering mutates the authoritative DB declaration list through normal editor
history, then restores the moved row by stable declaration ID. Boundary and
lesson-lock conditions disable invalid moves and deletion. The compile-only
build passed with 0 warnings and 0 errors; no broader sweep was run.

TIA project-tree root objects now participate in the same navigation and
property workflow. `Offline_Controller`, `PLC_1 [S7-1500]`, `Symbolic I/O`, and
`Online & diagnostics` accept double-click/Open and right-click `Properties`.
Their dialogs report only modeled facts: project identity and object counts,
configured scan period/runtime scope, symbolic binding counts and roles, or
offline controller/force/scan status. Unsupported rack, slot, physical
transport, installed-TIA, and live-PLC details are explicitly not invented.
The compile-only build passed with 0 warnings and 0 errors; no broader sweep ran.

The shared project-object properties dialog is now actionable rather than a
read-only dead end. `OPEN OBJECT` dispatches the selected object's existing Open
command, while `COPY DETAILS` places the displayed modeled property text on the
clipboard and keeps the inspector open. These actions apply to controller/root,
Symbolic I/O, diagnostics, tag-table, watch-table, and generated instance-DB
properties. The compile-only build passed with 0 warnings and 0 errors; no
broader sweep ran.

That shared properties dialog now also exposes `RENAME`, `DUPLICATE`, and
`DELETE`. Each button delegates to the same project-tree transaction already
used by the context menu, so tag/watch table history and object protection stay
centralized. Buttons disable in place for built-in tag tables, generated
instance DBs, controller/root objects, referenced or nonempty objects, the last
watch table, and lesson mutation locks. The compile-only build passed with 0
warnings and 0 errors after correcting the dialog action comparison to Godot's
`StringName` type; no parity, capture, or evidence sweep was run.

Program-block `Properties` now opens the same actionable dialog for OB, FB, FC,
and Global DB objects instead of silently routing into the rename/edit dock. It
shows type, name, network and declaration counts, semantic mode, call-reference
count, assigned-instance count, and offline source scope. Open, Rename,
Duplicate, and Delete delegate to the existing block workflows; referenced or
lesson-locked deletion remains disabled. The compile-only build passed with 0
warnings and 0 errors; no parity, capture, or evidence sweep was run.

Program-block Properties now also exposes `CROSS-REFERENCE`, routed through the
existing project search. The block summary reports whether Delete is available
or names the current protection reason, so referenced/called/entry objects do
not rely on a dim button alone to explain why removal is blocked. The
compile-only build passed with 0 warnings and 0 errors; no parity, capture, or
evidence sweep was run.

The Siemens project tree now supports the familiar keyboard routes `F2` for
Rename and `Delete` for deletion on blocks, user tag tables, and watch tables.
They dispatch through the same existing actions and refuse protected/default
tables, referenced blocks, non-empty tag tables, the final watch table, and all
lesson-locked mutations. Unsupported selections show a direct status message.

Every curriculum lesson now includes a learner-facing `FIRST CHECKS IF IT
DOESN'T WORK` section in both the Scenario Browser card and Operator Console.
The guidance is demo-specific: scan/button edge checks for Demo 1, TON enable
and reset checks for Demo 2, interlock and pusher-feedback checks for Demo 3,
event qualification for Demo 4, and sequence/encoder/position feedback checks
for Demo 5. These terms are also searchable in the Viewer browser.

The bounded build after these GUI changes passed with zero warnings and zero
errors. No parity sweep, capture loop, or broad regression suite was run; those
checks remain deferred unless explicitly requested.

Both five-demo curriculum surfaces now expose an `OPEN DEMO n MACHINE GUIDE`
action. It opens the active scene's authored `training.machineGuide` in a
scrollable dialog with purpose, start conditions, normal sequence, stop/reset
behavior, fault response, expected observations, and acceptance when supplied.
The action is hidden outside the five curriculum demos, so it cannot imply that
an unrelated scene has the same structured lesson content. The compile-only
build passed with 0 warnings and 0 errors; no parity, capture, or evidence sweep
was run.

The machine-guide dialog is now an operational lesson panel rather than a
read-only popup. It prepends the active demo objective, current state-aware next
action, and pass condition, then provides direct `RETURN TO MACHINE`, `OPEN PLC
LOGIC`, and `TRACE SCENE + LOGIC` routes. These actions keep the learner moving
between instructions, controls, and ladder tracing without reopening the
Scenario Browser. The compile-only build passed with 0 warnings and 0 errors;
no parity, capture, or evidence sweep was run.

All five curriculum machine guides now include an explicit acceptance line that
matches the lesson's visible proof. Demo 1 and Demo 4 no longer use generic
foundation/fail-safe placeholders: their guides name the real offline load,
force, scan, reset/start, edge, permissive, COUNT, and lamp behavior. This makes
the new guide dialog actionable against the controls and displays actually
shown by those demos. The compile-only build passed with 0 warnings and 0
errors; no parity, capture, or evidence sweep was run.

The machine-guide header now reports the active lesson's current pass state,
qualification, next action, and live trace values. Its compact footer actions
open Machine, Logic, split Trace, or Monitor directly, and `REFRESH STATUS`
updates the guide in place after the learner operates controls. This turns the
guide into a live lesson companion instead of a static reference page. The
compile-only build passed with 0 warnings and 0 errors; no parity, capture, or
evidence sweep was run.

The machine-guide footer now includes `DO NEXT ACTION`, wired to the same
state-aware curriculum command used by the Scenario Browser and Operator
Console. From the guide it can select the built-in simulator, verify/load the
matching demo logic, clear simulator forces, start the scan, route to lesson
inputs, or continue to the first unfinished lesson according to current state.
The compile-only build passed with 0 warnings and 0 errors; no parity, capture,
or evidence sweep was run.

Demo 5 now publishes its internal `sequence_step` to the PLC-owned DINT output
`sequence_step_value`. The machine scene binds that output to a visible `STEP`
display beside the X/Y/Z and PLACED readouts. Curriculum instructions, live
trace, troubleshooting, scene guide, and scene help all name the same visible
STEP value, so a stalled palletizer can be diagnosed from the machine view.
All five authored demo project files regenerated successfully, and the
compile-only build passed with 0 warnings and 0 errors. No parity, capture, or
evidence sweep was run.

Demo 5's machine guide and scene help now define every visible STEP value:
idle 0; pickup descent 10; vacuum dwell/attach 20; lift 30; X/Z transfer 40;
place descent 50; release 60; clear lift 70; measured home return 80; and layer
complete 100. The shared guide renderer exposes this authored `stateLegend` as
a dedicated sequence-step section. The compile-only build passed with 0
warnings and 0 errors; no parity, capture, or evidence sweep was run.

The Scenario Browser catalog entries for the five curriculum demos now report
their actual enriched scene equipment counts: Demo 1 = 4, Demo 2 = 5, Demo 3 =
17, Demo 4 = 13, and Demo 5 = 20. This removes the stale pre-enhancement counts
from the visible demo metadata, including the new Demo 5 STEP display. The
compile-only build passed with 0 warnings and 0 errors; no parity, capture, or
evidence sweep was run.

Curriculum completion for Demos 4 and 5 now matches their procedures instead
of passing early. Demo 4 records the first valid five-pallet batch, requires an
observed machine RESET to COUNT 0, then passes only after a fresh START and
qualified event produce batch-two COUNT 1. Demo 5 records the first completed
layer, requires an observed machine RESET to STEP/PLACED 0, then passes only
after a fresh START completes the second four-carton layer. The GUI next-action
text walks the learner through each recovery stage, and restarting either demo
clears only that demo's staged recovery evidence. The compile-only build passed
with 0 warnings and 0 errors; no parity, capture, or evidence sweep was run.

Recovery proof is now visible as a numbered 1/3, 2/3, or complete stage on the
Scenario Browser curriculum card, Operator Console, and machine-guide header
for Demos 4 and 5. Their pass record also stores and copies the exact recovery
chain, not only the final count value, so a session result distinguishes the
required first cycle, machine reset, and second-cycle proof. The compile-only
build passed with 0 warnings and 0 errors; no parity, capture, or evidence sweep
was run.

The Scenario Browser curriculum card and Operator Console now include a live
active-lesson progress bar in addition to the overall 0/5 bar. It follows COUNT
0-3 for Demo 1, ELAPSED 0-2000 ms for Demo 2, DONE 0-3 for Demo 3, the
first-batch/reset/batch-two sequence for Demo 4, and the first-layer/reset/
second-layer sequence for Demo 5. Passed lessons show `PASS COMPLETE`. The
compile-only build passed with 0 warnings and 0 errors; no parity, capture, or
evidence sweep was run.

## Verification boundary

Successful build, unit-test, and headless Godot checks prove only the local
offline software paths they exercise. They do not prove installed Siemens TIA
Portal parity, live hardware behavior, network readiness, or commissioning.

The guarded external-PLC path must retain exact ownership separation, explicit
write authorization, configured addresses, and PLC watchdog behavior. It must
never write physical `%I` or `%Q` addresses or control CPU operating state.

Watch-table Properties now exposes the table's stable project identity and
separates symbolic-reference rows from absolute-address rows. It also reports
why Delete is unavailable when an active challenge locks project editing or
when the selected table is the project's final required watch table. The same
Properties dialog continues to route Open, Rename, Duplicate, and Delete
through the existing project-tree transactions.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The TIA-style Program blocks, PLC tags, and Watch tables folders now expose a
real Properties command alongside Add new object. Folder Properties reports
live modeled counts (block types and networks, tag tables/tags/constants/
bindings, or watch tables/rows/active table), and OPEN OBJECT navigates to the
corresponding workspace instead of ending at a passive tree node.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

Folder Properties now includes an ADD NEW action. It delegates to the existing
validated creation workflow for blocks, tag tables, or watch tables and is
disabled while an active lesson locks project mutation.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The Siemens Project tree header now has a visible PROPS button. It opens the
same object-aware Properties dialog for the selected controller, Program
blocks/PLC tags/Watch tables folder, authored block, generated instance DB, tag
table, watch table, Symbolic I/O node, or diagnostics node. Add shortcuts and
Show all tags resolve to their owning folder instead of producing a dead end.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

Every Project-tree Properties dialog now begins with the selected object's full
tree breadcrumb and provides COPY PATH separately from COPY DETAILS. Folder
shortcuts resolve to their owning folder path, so copied identity remains stable
and meaningful even when Properties was opened from an Add or Show-all row.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The Siemens main Project tree now has a persistent two-line selected-object
summary directly below its header. It identifies the selected object type,
reports the most useful live count where applicable, and tells the user whether
to double-click OPEN, use PROPS, or use the folder's Add command. The summary is
hidden while a non-tree Project dock page is active and follows restored block
selection when the editor returns focus to the active LAD document.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The Siemens Project tree header now also has a visible OPEN button. It shares
the same selected-object normalization and protection setup as PROPS, then uses
the established open route for controller nodes, folders, authored blocks,
generated instance DBs, tag tables, watch tables, Symbolic I/O, and diagnostics.
Add and Show-all shortcuts resolve to their owning folder rather than failing.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The Siemens main Project tree now has a text filter with match count and clear
button. Matching objects remain visible with their parent hierarchy; matching a
folder reveals its current children. Intentionally hidden scheduler rows and
inactive pooled tree rows stay hidden, and every editor refresh reapplies the
active filter against a freshly captured unfiltered tree state.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

RESET CURRICULUM + SIMULATOR no longer destroys the session immediately. It now
opens a confirmation that reports the current number of passed lessons and
staged Demo 4/5 recovery records, explicitly lists captured pass traces and
local simulator/scene state as reset targets, and states that authored logic,
project files, and demo scenes are retained. CANCEL leaves the session intact.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The Scenario Browser and Operator Console now both provide SAVE CURRICULUM
SESSION RESULTS beside COPY. The save dialog defaults to Documents with a dated
text filename and writes the same five-lesson status, pass-time scan/trace data,
recovery proof, and evidence boundary used by the clipboard report. Save errors
are reported in the workspace status instead of silently losing the report.
The repaired compile-only solution build passed with 0 warnings and 0 errors;
no parity, capture, evidence, or regression sweep was run.

The Siemens central editor now keeps a document breadcrumb visible beneath the
open-document tabs for OB/FB/FC/global DB, generated instance DB, PLC tag table,
and watch-table documents. A type badge identifies the open object, and LOCATE
clears any Project-tree filter, reopens the tree dock, selects the exact owning
row, scrolls it into view, and focuses the tree. This remains available when the
left Project dock was previously collapsed.
The compile-only build completed its generated-source gates and emitted a fresh
`RungProof.Next.dll`. The command crossed the terminal output window and its
final warning/error summary was not retained; the build was not restarted and
no parity, capture, evidence, or regression sweep was run.

The central document breadcrumb now also provides PROPS. It resolves the active
editor tab back to its exact Project-tree object, updates selection without
requiring the left dock to be open, and launches the same object-aware
Properties workflow used by the Project tree. LOCATE and PROPS share one active
document resolver so block, instance DB, tag-table, and watch-table routing
cannot drift apart.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

Individual Siemens PLC-tag rows now have a Properties command and dedicated
dialog. It reports stable identity, table, type, role, address metadata, initial
value, scene binding, comment, executable-reference count, and the exact Delete
state. COPY DETAILS is read-only; EDIT DEFINITION, CROSS-REFERENCE, DUPLICATE,
and DELETE delegate to the existing validated tag transactions and inherit
their challenge-lock, pending-edit, and reference protections.
After correcting the Godot `StringName` action dispatch, the compile-only
solution build passed with 0 warnings and 0 errors; no parity, capture,
evidence, or regression sweep was run.

Individual Siemens User Constant rows now have the matching Properties
workflow. The dialog reports stable identity, containing tag table, data type,
fixed value, comment, LAD-reference count, exact Delete state, and the offline
read-only execution boundary. COPY DETAILS is read-only; EDIT DEFINITION,
CROSS-REFERENCE, DUPLICATE, and DELETE delegate to the existing constant
transactions and inherit their challenge-lock, pending-edit, and reference
protections.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

The Siemens PLC Tags and User Constants toolbars now each expose a visible
PROPS button for the selected row. The toolbar and right-click entry points
share one action-state calculation per object type, so edit, duplicate, and
delete availability cannot drift between the menu and the Properties dialog.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

Individual Watch-table rows now have Properties from both the docked and central
watch grids. The dialog reports table and row stable identities, row position,
symbolic versus absolute reference, canonical address identity, resolved tag
type/role, format, modify and monitor values, row/tag comments, and Remove state.
COPY DETAILS is read-only; OPEN TAG, CROSS-REFERENCE, EDIT FORMAT, EDIT MODIFY,
EDIT COMMENT, and REMOVE ROW delegate to the existing watch-row actions and
retain the active challenge lock.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, evidence, or regression sweep was run.

Generated FB and IEC instance-DB member rows are now selectable instead of
passive text. A visible PROPS button, double-click, and right-click all open the
same read-only member dialog. FB rows report the instance DB/assigned instance,
source FB and stable declaration identity, section/type, inherited default and
start value, Retain/comment metadata, exact offline execution behavior, and the
unsupported per-instance override/monitor boundary. IEC_TIMER and IEC_COUNTER
rows report their generated PT/ET/IN/Q or Q/CV identity and meaning. OPEN OWNER
returns to the defining FB or IEC instruction, while COPY DETAILS preserves the
inspection result. The compile-only solution build passed with 0 warnings and
0 errors; no parity, capture, screen-control, installed-TIA, runtime, PLC, or
broader evidence sweep was run.

All three learner surfaces—the Scenario Browser, Operator Console, and Machine
Guide—now include a live procedure checklist instead of relying only on static
instructions. It marks qualification as complete or names the blocking reason,
shows the active measured COUNT/ELAPSED/DONE/layer step, and changes the active
arrow as the lesson advances. Demo 4 tracks first batch, reset-to-zero, and the
first event of batch two; Demo 5 tracks first layer, reset-to-home/zero, and the
second layer. Completed lessons show all three phases checked from the existing
session-latched proof state. The compile-only solution build passed with 0
warnings and 0 errors; no parity, capture, screen-control, installed-TIA,
runtime, PLC, or broader evidence sweep was run.

`DO NEXT ACTION` now identifies the exact PLC-consumed machine control required
by the active lesson state. Once qualification is complete, its label changes
from the generic `GO TO LESSON INPUTS` to `FOCUS <control>` when an operator
action is required. Pressing it opens the Operator Console, scrolls that exact
button into view, gives it an amber focus frame, and reports the focused action
with the current instruction. The mapping covers pulse/request, enable,
trip/overload clearing, Start, Reset, pallet qualification/event, and Demo 5
pallet-position controls. Timer timing and automatic sequence phases deliberately
focus the lesson area without fabricating a control press. The compile-only
solution build passed with 0 warnings and 0 errors; no parity, capture,
screen-control, installed-TIA, runtime, PLC, or broader evidence sweep was run.

The Operator Console now keeps that guidance visible before the helper is
pressed. Once the active lesson is qualified—matching authored logic loaded,
offline scan running, and zero forces—the required machine action is prefixed
`▶ NEXT`, outlined in amber, and given a tooltip that identifies it as the next
lesson control. Toggle controls retain their green normal-active or red
abnormal-active fill while the amber border marks the requested transition.
The marker follows live state and disappears during timer/automatic wait phases,
after a lesson passes, or whenever qualification is lost. The compile-only
solution build passed with 0 warnings and 0 errors; no parity, capture,
screen-control, installed-TIA, runtime, PLC, or broader evidence sweep was run.

Accepted scene actions now produce learner-facing feedback instead of exposing
only the internal action ID. The workspace status and Operator Event History
record the authored control label, whether the action is maintained or
momentary, the exact PLC-bound scene point, and its value immediately after the
request. Existing rejected-action paths still report the specific blocked
reason. The same feedback applies to sidebar buttons and clickable 3D machine
controls because both already share the selected-controller action route. The
compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, screen-control, installed-TIA, runtime, PLC, or broader evidence sweep
was run.

The Operator Console Event History now provides visible COPY LOG and CLEAR LOG
actions plus an explicit empty state. COPY exports the bounded current-session
history with its local-only evidence boundary. CLEAR removes only the event
history and explicitly retains curriculum passes and captured proof. Event
storage now remains plain text while the RichText display escapes authored
content safely. Curriculum COPY/SAVE reports also include the 12 newest operator
events so a pass report carries the recent control and troubleshooting sequence.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, screen-control, installed-TIA, runtime, PLC, or broader evidence sweep
was run.

The central Global DB declaration grid now has a visible PROPS action and a
dedicated member Properties dialog. It reports owner/declaration stable IDs,
position, type/start value/ENUM domain, Retain/comment metadata, optimized-access
offline storage, exact Delete state, and the unsupported offset/HMI/supervision/
snapshot/online-monitor boundary. COPY DETAILS is read-only; EDIT DEFINITION,
DUPLICATE, MOVE UP/DOWN, and DELETE delegate to history-backed member actions.
The pre-existing context menu ID drift introduced when Duplicate was added is
also corrected: Duplicate now duplicates, Move commands move in the requested
direction, and Delete deletes. Edit and Remove now resolve the row's stored DB
owner identity instead of relying on whichever block is active. The compile-only
solution build passed with 0 warnings and 0 errors; no parity, capture,
screen-control, installed-TIA, runtime, PLC, or broader evidence sweep was run.

The existing LAD Network/Instruction Properties popup now identifies the exact
authored object instead of presenting only editable fields. It reports the
owning block and network stable IDs, network position, branch/instruction counts,
and terminal-action type. Contact/compare selections add their persistent
instruction ID and branch/position; terminal and LABEL selections add their
network-scoped location identity. COPY DETAILS exports that inspection summary,
while the existing transactional Apply/Cancel workflow remains the sole mutation
path. The compile-only solution build passed with 0 warnings and 0 errors; no
parity, capture, screen-control, installed-TIA, runtime, PLC, or broader evidence
sweep was run.

Open Siemens editor-document tabs now provide a main-screen right-click menu:
Activate, Properties, Locate in Project tree, Close, and Close other documents.
Properties and Locate activate the exact stable document identity before using
the shared breadcrumb/project-tree routes. Closing remains tab-only and never
deletes or mutates the project object. Deleted-object tabs keep only Close
available until Undo restores their identity. Block, tag-table, watch-table,
and instance-DB tabs now show consistent unsaved `*` state and saved/unsaved
tooltips, including the right-click discovery hint. The compile-only solution
build passed with 0 warnings and 0 errors; no parity, capture, screen-control,
installed-TIA, runtime, PLC, or broader evidence sweep was run.

FB/FC/OB interface declarations now expose DUPLICATE from the visible command
rail, row context menu, and declaration Properties dialog. A duplicate is
inserted directly after its source with the same section, data type, initial
value, ENUM domain, Retain setting, and comment, but receives a unique name and
a new stable declaration identity. Parameterized Input/Output/InOut copies do
not fabricate CALL bindings; the workspace tells the author that existing CALL
sites must bind the new formal before Verify + Load. The change participates in
normal edit history and respects the active challenge lock. The compile-only
solution build passed with 0 warnings and 0 errors; no parity, capture,
screen-control, installed-TIA, runtime, PLC, or broader evidence sweep was run.

The Scenario Browser curriculum card now includes a persistent five-row session
map above the numbered lesson controls. Every demo is labeled visibly as
CURRENT, NEXT, PASSED, or READY, so the operator no longer has to infer the
curriculum sequence from button colors or hover text. The map refreshes on
scene changes, lesson passes, individual demo restarts, and full curriculum
resets. The compile-only solution build passed with 0 warnings and 0 errors; no
parity, capture, screen-control, installed-TIA, runtime, PLC, or broader
evidence sweep was run.

Active curriculum lessons now show the same live phase rail in both the
Scenario Browser and Operator Console: QUALIFY, OPERATE, RECOVERY, and PASS.
The rail advances from the lesson's actual eligibility, recovery-stage, and
completion state rather than from manual clicks. Demos 1–3 mark recovery as not
applicable; Demos 4–5 activate it during their required reset-and-second-cycle
proof. The compile-only solution build passed with 0 warnings and 0 errors; no
parity, capture, screen-control, installed-TIA, runtime, PLC, or broader
evidence sweep was run.

Both curriculum workspaces now expose VIEW CURRICULUM SESSION REPORT. It opens
a scrollable in-app preview of the exact five-demo report, including every
lesson state, captured local pass proof/trace, recent operator events, and the
evidence boundary. The preview provides COPY REPORT and SAVE REPORT without
forcing the operator to leave the active lesson. The compile-only solution
build passed with 0 warnings and 0 errors; no parity, capture, screen-control,
installed-TIA, runtime, PLC, or broader evidence sweep was run.

The Operator Console now presents a four-chip lesson-control legend before the
machine inputs. It visually separates top-toolbar App Run/Stop/Reset from
momentary auto-release inputs, maintained On/Off inputs, and the amber guided
NEXT control. This makes control behavior discoverable without relying on hover
tooltips or interpreting PLC point names. The compile-only solution build
passed with 0 warnings and 0 errors; no parity, capture, screen-control,
installed-TIA, runtime, PLC, or broader evidence sweep was run.

The Operator Console now also contains a local playback strip with RUN
SIMULATOR, STOP, and RESET plus a live RUNNING/PAUSED/STOPPED state label. These
buttons invoke the same guarded simulator actions as the top App Playback menu,
so a learner can operate a complete demo without navigating away from its
lesson and machine controls. The compile-only solution build passed with 0
warnings and 0 errors; no parity, capture, screen-control, installed-TIA,
runtime, PLC, or broader evidence sweep was run.

Lesson inputs in the Operator Console are now grouped as MACHINE COMMANDS,
PROCESS CONDITIONS / SENSOR SIMULATION, and FAULT / INTERLOCK SIMULATION.
Start/Stop/Reset/System Enable are kept apart from simulated field conditions,
while Safety Trip and Drive Overload retain their red abnormal-state styling in
a dedicated section. The compile-only solution build passed with 0 warnings and
0 errors; no parity, capture, screen-control, installed-TIA, runtime, PLC, or
broader evidence sweep was run.

The Operator Console playback strip now reflects actual local simulator state:
RUN is disabled while running, STOP is disabled while stopped, and the status
label displays RUNNING/PAUSED/STOPPED with the current or last virtual scan
number. RESET remains available as the explicit scene/simulator reset action.
The compile-only solution build passed with 0 warnings and 0 errors; no parity,
capture, screen-control, installed-TIA, runtime, PLC, or broader evidence sweep
was run.

The Project objects OB schedule editor now has a visible PROPS action and a
dedicated schedule Properties dialog. It reports the schedule's stable identity,
displayed OB label, authored name, continuous/cyclic execution class, interval,
priority, exact entry-block identity, modeled execution meaning, and current
delete protection. COPY DETAILS is read-only; EDIT SCHEDULE focuses the existing
form; DELETE OB remains challenge- and scheduler-rule guarded. The dialog states
the offline Verify + Load and installed-TIA/live-PLC boundary. The compile-only
solution build passed with 0 warnings and 0 errors; no parity, capture,
screen-control, installed-TIA, runtime, PLC, or broader evidence sweep was run.

OB/task schedules now expose DUPLICATE from both the visible schedule command
rail and the schedule Properties dialog. A copy retains the selected entry
block, continuous/cyclic class, period, and priority, while receiving a unique
authored name and new stable task identity. The operation is challenge-locked,
history-backed, selects the new schedule for editing, and explicitly requires
Verify + Load before the offline change can run. The compile-only solution build
passed with 0 warnings and 0 errors; no parity, capture, screen-control,
installed-TIA, runtime, PLC, or broader evidence sweep was run.

Schedule Properties now reports the schedule's displayed position and provides
MOVE UP / MOVE DOWN. Reordering preserves the schedule's stable identity and is
history-backed. The GUI explicitly states the runtime rule represented by this
order: lower numeric priority executes first, and displayed schedule order is
the tiebreak for equal priorities. The compile-only solution build passed with
0 warnings and 0 errors; no parity, capture, screen-control, installed-TIA,
runtime, PLC, or broader evidence sweep was run.

OB/task schedule Properties now includes OPEN ENTRY BLOCK. It resolves the
schedule's stored entry-block identity, passes through the existing pending-form
guard, and opens the exact scheduled block in the central program editor. A
missing target is reported without changing the active document. The source
change compiled with 0 warnings and 0 errors before the current app inspection;
the already-open app was not restarted or driven by automation afterward.

While the app remained open for user inspection, schedule Properties source was
extended to show whether the schedule identity exists in the currently loaded
local virtual-runtime snapshot, whether it is due or idle in that snapshot, and
its execution count. This is intentionally local runtime status, not installed-
TIA or live-PLC evidence. Because Siemens mode intentionally represents the
scheduled OB through its program block instead of inventing a second schedule
tree row, no misleading Locate Schedule action was added. This source-only edit
has not been rebuilt or loaded into the already-running window yet.

Program-block Properties source now reports every OB/task schedule whose entry
block targets that program block, including the displayed OB/task label,
continuous/cyclic class, and priority. Blocks with no direct scheduler entry
show None, keeping CALL references and scheduled entry distinct. This source-
only edit also remains outside the already-running inspection window and awaits
the next normal build/launch.

Program-block Properties source now also reports the block's stable identity,
project position, and whether the loaded local virtual runtime matches the
current offline draft. A mismatch is presented as a direct Verify + Load
requirement, not as installed-TIA or PLC status. This remains a source-only
change for the next launch; the open inspection window was not restarted.

Program-block Properties source now expands relationship counts into usable
identities: each CALL reference names its owning block and network, and each FB
instance names the instance plus its assigned DB number/name when present. This
lets an author see why a block is referenced or protected without leaving the
Properties summary. The running inspection window remains untouched; this edit
awaits the next normal build/launch.

The Operator Console was simplified after direct user review. Its visible left
panel now contains only the active demo summary, one primary guided action,
lesson inputs, compact runtime state, PLC logic, and the machine guide. The
duplicated playback controls, five-demo rail, reports, long procedure text,
trace/monitor/engineering shortcuts, and other curriculum controls were removed
from the run surface; they remain available on their dedicated screens where
applicable. The primary guided action now operates the requested safe local
simulator input instead of merely moving keyboard focus, fixing the apparent
no-op in Demo 4 (START, permissives, and PALLET cycling). Repeated scene requests
behind the unsaved-workspace guard now keep the latest destination and re-show
the Save / Discard / Cancel decision instead of silently ignoring the click.
The compile-only build passed with 0 warnings and 0 errors. The app window that
was already open during review still contains the previous build and must be
restarted to display these changes; no parity or broader verification ran.

Demo 3's automatic carton source now has a physical entry boundary. Its main
conveyor was extended upstream while retaining the existing downstream end, the
initial/recycled carton position was moved fully behind a portal wall, and the
carton now travels through that opening before it becomes visible in the cell.
The portal is implemented as a validated, data-driven scene-composer contract
(`materialEntryPortal`) so any other scene with an automatic material spawn can
use the same installation rule instead of adding one-off wall geometry. Demo 5
was not given a redundant wall because its authored source is already an actual
pallet magazine. The compile-only build passed with 0 warnings and 0 errors;
the already-running app must be restarted to show the new Demo 3 geometry.

Direct Demo 4 visual review found an unrealistic split operator station: START,
STOP, and RESET were omitted from the cabinet inventory and therefore remained
on individual floor pedestals, while PALLET PRESENT, TYPE VALID, COUNT REQUEST,
and COUNT were cabinet-mounted. All seven devices are now consolidated on the
single operator cabinet and use the existing functional scene action bindings.
The shared machining-center asset's decorative `CTRL_*` START/E-STOP cluster has
no scene action contract, so unbound instances are now hidden by default across
scenes rather than presented as dead controls. Demo 4's small low-contrast wall
placard was replaced with a 1.45 m high dark-blue process board using large
pale-yellow outlined text that identifies the batch inputs and directs machine
commands to the operator station. The compile-only build passed with 0 warnings
and 0 errors; the running app must be restarted for visual review.

Direct Demo 2 visual review exposed the same inventory failure on a teaching
bench: its functional STOP and RESET pushbuttons retained their delivered floor
pedestals because only REQUEST and the lamp were listed in the explicit bench
inventory. Bench composition is now contract-driven: every scene-bound switch,
selector, indicator, and readout is automatically added to the teaching bench,
with its original pedestal removed and the existing bench terminal/wire covers
retained. This prevents any omitted functional bench control from falling back
to a floor mount in other lessons. The compile-only build passed with 0 warnings
and 0 errors; the running app must be restarted for visual review.

Five-demo presentation no longer inherits one distant elevated three-quarter
room view. Each curriculum demo now has a separate equipment-focused camera
direction, field of view, focus set, and close framing margin; the ordinary
lesson view fits the active process and consolidated operator station while
View > Whole Scene remains available for architecture inspection. The five
environments also use distinct, higher-contrast industrial palettes and the
existing concrete, paint, metal, rubber, and wood material treatments. Demo 2's
reference board is larger, dark blue, and uses large outlined pale-yellow type.
Selector detent labels and operator face labels are larger and outlined, and
the label-fitting rule now applies to bench-mounted controls instead of only
cabinet controls. The direct bundled-SDK compile succeeded with 0 warnings and
0 errors. The running app was not restarted or screen-driven in this turn, so
the visual result still requires review after relaunch.

Demo 5 visual review exposed five raw count-display assets arranged as separate
floor signs and all showing the asset fallback `COUNT / NO LIVE VALUE`. Their
scene configurations used unsupported presentation aliases even though their
existing point bindings already declared five numeric values. The displays now
use the supported numeric presentation with distinct `PLACED`, `X CNT`, `Y CNT`,
`Z CNT`, and `STEP` labels. A rendered follow-up showed that mounting five full
display housings made the cabinet oversized, so the corrected composition uses
one shared dark HMI area with those five independently bound labels. The original
display bases, masts, housings, and duplicate legends are hidden; ENABLE, START,
STOP, RESET, SIM TRIP, and PALLET occupy two compact lower rows. The station is
located from the actual gantry and carton stock at the cell's front-right service
corner instead of the room-wide equipment envelope. A second render confirmed
the five floor signs are gone, all five HMI labels are distinct, and the compact
station is adjacent to the cell. The Demo 5 working camera was then lowered and
tightened to reduce empty floor, including reduced conservative depth allowance
for all five equipment-focused curriculum profiles. The final bundled-SDK build
passed with 0 warnings and 0 errors, and a post-build Demo 5 render confirmed the
compact station/HMI composition remained intact.

Direct Demo 1 reproduction found the reported load failure. Composition aborted
at `Bench identification source drift` because the identification ledger still
expected older switch and lamp descriptions after the authoritative four-device
lesson source had changed. The ledger now matches the current PULSE input,
threshold output, RESET input, and COUNT readout; RESET is also validated against
its pulse action. The count-display configuration now uses the supported numeric
presentation instead of the stale `pressCount` alias. Demo 1 then loaded through
`MIGRATED_SCENE_LOADED` and rendered all four devices. That render also showed
redundant fascia signs below already self-labeled controls, so bench composition
now omits identification strips when an operator face or live/static readout
already supplies the label; lamps retain one functional output legend. The final
Demo 1 render is a clean four-device bench with no duplicated PULSE, RESET, or
COUNT signs. The bundled-SDK build passed with 0 warnings and 0 errors.

Direct Demo 2 rendering confirmed the consolidated five-device teaching bench
and readable high-contrast timing board. Its elapsed-time accessory still used
the unsupported `elapsedMilliseconds` presentation alias, which produced the
generic `COUNT / NO LIVE VALUE` fallback despite a correct `delay_elapsed_ms`
point binding. The source now uses the supported numeric presentation and the
rendered bench shows `ELAPSED MS` with its live value. The focused Demo 2 render
completed through `MIGRATED_SCENE_LOADED`; the bundled-SDK build remained clean.

Direct Demo 3 rendering found the same freestanding-sign anti-pattern on its two
functional readouts. The separate floor-mounted HEIGHT and completed-part signs
are now hidden and their independently bound `HEIGHT MM` and `DONE` values are
integrated into one dark HMI area on the adjacent operator cabinet. The cabinet
controls were lowered to reserve a clear display zone, while the wall-fed source
conveyor and existing runtime bindings remain intact. A focused post-build render
completed through `MIGRATED_SCENE_LOADED` with no floor displays; the bundled-SDK
build passed with 0 warnings and 0 errors. Demo 4 was also rendered after its
earlier consolidation: all seven functional controls and COUNT readout are on the
single cabinet beside the machine, the unbound decorative machine controls remain
hidden, and the large process board is readable.

The post-layout five-demo learner verifier then completed against current source
with `167` checks, `0` failed, and terminal result
`FIVE_DEMO_LEARNER_RESULT failures=0 scope=20ms_actual_editor_joint_offline`.
It loaded each checked-in authored project, compiled and ran the current draft,
exercised the real scene actions and scan path, checked the visible readouts and
scene feedback, performed machine Stop/Reset/repeat cases, and proved save/reopen,
wrong-program, and invalid-current-draft behavior. Demo 4 specifically counted
five qualified events on its cabinet COUNT display, reset to zero/stopped, then
started a second batch at one; this closes the reported local no-op behavior.
Demo 5 completed four physical placements through every PLC-owned state and all
axis directions. This is current local offline controller/scene evidence; it is
not installed-TIA, screen-control, live-PLC, or safety-validation evidence.

After the five-demo local runtime gate passed, main-screen/property work resumed
with the first remaining visible Global DB issue. The optimized-access capability
note no longer uses dim disabled-looking blue-gray text; it is now 13 px pale
blue disclosure text. A fresh normal-Vulkan 1600x900 Global DB capture showed the
complete note wrapping beneath the five-column declaration grid with all actions
and rows unobscured. The build passed with 0 warnings and 0 errors, and the
visible app was relaunched on the new assembly. This is RungProof renderer
evidence only; installed-TIA V21 comparison remains open.

Demo 3's first material-entry treatment was rejected because its 4 m portal wall
was a freestanding slab with a visible end rather than part of the building. The
entry wall is now 20 m across and 7.5 m high so the open-roof camera cannot see
over it and reveal the structure behind it. More
importantly, `AttachDemoEnvironment` excludes `MaterialEntryPortal` from the
machinery envelope used to size the room; otherwise every wider wall enlarged the
room around itself and could never meet the perimeter. A fresh normal-renderer
capture shows the wall terminating into the rear building with the conveyor
passing through its framed opening. A later 1081x637 live-window capture also
confirmed the wall's top edge and the structure behind it are no longer visible;
it reads as the building boundary rather than a freestanding partition. The
build passed with 0 warnings and 0 errors.

The learner-facing Operator view is now intentionally compact. Its left rail
contains the active lesson, one guided next action, actual lesson inputs, a short
runtime line, and direct Logic/Guide actions; the earlier navigation, reporting,
legend, duplicated playback, and engineering actions remain out of that run
surface. The permanent right event/equipment/health rail and bottom I/O tables
are also hidden in Operator view so the machine uses the available viewport.
Those diagnostics remain available in Engineering, Logic, and split views. The
shared viewport calculation and app-shell acceptance gate were updated for this
contract. The bundled-SDK build and focused `--verify-app-shell` gate passed.

Normal interactive startup now opens directly on Demo 1 instead of the unrelated
generic conveyor scene, and the large automatic Simulator Mode modal no longer
blocks the machine. Its boundary explanation remains available from Help. The
curriculum's green action now uses the existing App Run path to validate, compile,
load, and run the current authored demo draft; it no longer forces a redundant
Verify + Load editor detour before a demo can respond. A fresh exact-window
capture confirmed Demo 1 opens with the compact operator rail, no blocking modal,
and one `RUN CURRENT DEMO LOGIC` action. The build and `--verify-app-shell` gate
passed; the visible app was relaunched on this assembly.

Demo camera framing now treats the unobstructed operator aperture correctly.
The horizontal Camera3D offset uses the aperture-minus-viewport direction and
the horizontal projection scale; the earlier reversed/vertical-scale offset
pushed the physical station behind the far edge of the lesson rail. Demo 1's
profile explicitly frames PULSE, the threshold stacklight, RESET, and COUNT with
a narrow-window safety margin. An exact 1081x637 live-window capture confirmed
all four devices remain fully visible in the close operator view. The bundled-SDK
build passed with 0 warnings and 0 errors, and the visible app was relaunched on
the corrected assembly.

The same corrected camera aperture was applied to the remaining curriculum
profiles. Exact 1081x637 live-window captures now show Demo 2's REQUEST, lamp,
STOP, RESET, and ELAPSED MS readout; Demo 3's sorter and continuous entry wall;
Demo 4's inspection machine and adjacent operator cabinet; and Demo 5's gantry
plus PLC-owned axis/step readout. The profiles retain close machine views rather
than returning to room-wide three-quarter shots. A real window interaction on
Demo 4 changed the visible lesson state from NOT QUALIFIED / RUN CURRENT DEMO
LOGIC to IN PROGRESS / OPERATE MACHINE START PUSHBUTTON, proving the green run
action now loads and starts the authored draft through the user-facing GUI. A
separate OS-level automation attempt did not provide reliable evidence for the
momentary START click because Windows DPI/input scaling affected synthetic mouse
delivery; do not treat that attempt as screen-control proof of START. The build
passed with 0 warnings and 0 errors, and one Demo 4 app instance remains open.

The learner rail no longer duplicates physical machine START/STOP/RESET controls.
Those mapped actions remain registered for the green guided step, while ordinary
operation uses the ray-pickable 3D panel. The rail now shows one compact
`MACHINE COMMANDS - USE 3D PANEL` heading followed only by process-condition and
fault-simulation inputs that do not belong on the physical command station. A
live Demo 4 capture confirmed the duplicate button bank is gone and the cabinet,
machine, and required condition controls remain visible. The bundled-SDK build
passed with 0 warnings and 0 errors, and Demo 4 was reopened on that assembly.

Mapped 3D controls now request the pointing-hand cursor from the raw scene input
path whenever `SceneControlInteractor.TryPick` identifies a declared control
under the pointer; all other positions restore the arrow. This makes actionable
machine controls distinguishable from decorative equipment without adding more
sidebar UI. The source builds with 0 warnings and 0 errors. A Windows cursor-
handle probe at one guessed cabinet coordinate did not hit a mapped control, so
it is not retained as visual hover proof; the normal app remains open for an
ordinary pointer walkthrough.

The three curriculum operator cabinets (height sorter, batch counter, and XY
palletizer) now use larger high-contrast legend plates and a larger Label3D cap.
Their actual retained control geometry is measured after the plate enlargement,
so cabinet pitch and width expand from real bounds instead of allowing labels to
overlap. A live Demo 4 capture confirmed the machine and resized adjacent panel
remain in the close operator view. The build passed with 0 warnings and 0 errors,
and Demo 4 remains open on the updated assembly.

The vertical Camera3D aperture correction now uses the screen-space direction
required by Godot's physical camera offset, opposite the horizontal expression.
Fresh 1081x637 live captures show Demo 3 against one uninterrupted full-height
building boundary with no visible wall top or exposed space behind it, while
Demo 5 keeps the machine and its integrated operator cabinet in the close view.
Do not reintroduce short freestanding scenic walls as a substitute for a room or
building envelope; material entry must read as a conveyor passing through a real
wall opening.

The horizontal physical-camera offset now uses the same viewport-to-aperture
screen-space direction as the corrected vertical offset. This centres the machine
inside the unobstructed operator viewport instead of applying the lesson-rail
width twice and leaving unused space on the right. Fresh 1920x1080 captures show
the Demo 1 bench centred, Demo 4 tightened around the inspection machine and its
adjacent cabinet, and Demo 5 from a front-left working angle where the cabinet no
longer blocks the gantry. Demo 4 no longer uses the entire long infeed conveyor to
set its camera distance. Demo 3 is also closer, and its full-span material-entry
wall now uses the authored room-wall finish plus a contrasting impact-resistant
base band. The three machine-cell cabinets use larger measured legend plates and
larger fitted Label3D text. The build passed with 0 warnings and 0 errors.

After the final demo presentation changes, the single bounded actual-editor
five-demo walkthrough was rerun from the current build. It completed with
`FIVE_DEMO_LEARNER_RESULT failures=0 scope=20ms_actual_editor_joint_offline`.
This proves the saved local drafts, scene bindings, declared 3D actions,
machine Reset/restart cases, Demo 4 five-event batch, and Demo 5 four-carton
feedback-owned sequence in the offline model. It is not installed-TIA,
screen-control, live-PLC, commissioning, or safety-validation evidence.

Demo 3's entry treatment must be a connected building volume, not merely a wide
portal plane. `materialEntryPortal` now authors a configurable source-room depth
and direction; the composer creates the attached ceiling, rear wall, full-depth
returns, base protection, and an interior service light behind the conveyor
opening. The dispatch-room return for this scene also spans the complete room
depth and rises to the same 7.5 m envelope, while the lesson camera uses an
operator-height direction. A fresh 1920x1080 current-source capture confirms no
wall top, short end, sky gap, or empty space behind the entry wall is visible.
The source build passed with 0 warnings and 0 errors. This is rendered local GUI
evidence, not live PLC or installed-TIA evidence.

After demo presentation acceptance resumed TIA property work, a bounded
current-source 1600x900 screen-control pass closed the previously missing User
Constant edit transaction. One session-local scratch constant was created, the
existing row was reopened and renamed through the visible form, Apply committed
the change, reopening showed the edited value, and Undo restored the prior name
with `UNDO - UPDATE PLC USER CONSTANT - OFFLINE` and Redo enabled. The bridge
repeated submitted text batches, so the literal probe spelling is excluded from
text-input evidence. No project Save/Load, Run, Connect, installed TIA, or live
PLC action participated.

The earlier Demo 3 room-envelope acceptance above was rejected after the user
showed that the material-entry boundary still read as a short freestanding set
wall with its exterior visible. That visual claim is superseded. The entry
partition now spans 32 m beyond the production envelope, the source volume is
10 m deep, and Demo 3 owns a continuous ceiling with four ceiling-mounted
high-bay fixtures. A fresh current-build screen inspection shows neither the
partition top nor either end, and no exterior gap behind it. The app was left
open on Demo 3. Build result: 0 warnings, 0 errors. This is local rendered-GUI
evidence only.

The Siemens editor now preserves usable ladder space when the PLC-tag detail
drawer is open. Empty OB interface summaries stay hidden during editor-document
restoration, the instruction palette no longer claims an oversized minimum,
and opening the tag drawer selects/reveals the first leaf operand when none is
selected. A fresh 1600×900 current-build screen showed the tag list and the
entire first Demo 3 rung together. Build result: 0 warnings, 0 errors. A single
bridge drag did not commit and is not treated as product interaction evidence;
physical tag-to-operand drag remains open. The project stayed unmodified in the
running session, with no Save/Load, Run, or PLC connection.

Pending editor navigation now identifies the exact staged object instead of
showing a generic form warning. Registered transaction descriptions cover PLC
tags, user constants, block/task/interface properties, instruction properties,
inline operands, and call parameters. The visible actions are **APPLY AND
CONTINUE**, **DISCARD AND CONTINUE**, and **CANCEL**; multiple dirty forms are
listed individually and combined Apply remains disabled because their revisions
cannot be committed atomically. A current-build 1600×900 screen pass staged a
name edit on `system_enable`, attempted OB navigation, visibly received the named
PLC-tag prompt, and used Discard to restore the original value and navigate. The
project remained unmodified. Focused gate assertions cover the named prompt,
button labels, multi-form names, and disabled combined Apply. Build result: 0
warnings, 0 errors. This is local RungProof GUI evidence only.

A current-build 1600×900 screen pass completed the visible offline Ladder project
Save → Load → Save route on Demo 3. The first save wrote a disposable schema-9
draft. A staged `reset_command` comment edit then triggered the named pending-edit
dialog when **Open project** was selected; **DISCARD AND CONTINUE** restored the
form before the file picker opened. Reopening showed the original comment, and a
second **Save project** completed on the reopened path. The workspace status
explicitly said `VALID DRAFT` and `NOT LOADED`; runtime stayed STOPPED and PLC
DISCONNECTED. The JSON parsed as `scene-2-conveyor-pusher` with 24 tags and was
moved from the repository root into the task `work` folder after verification.
This proves the local current-source general project-file route and tag discard
case, not DB/Watch mutation persistence, running monitoring, live PLC behavior,
or installed-TIA parity.

The Demo 3 production-room correction was tightened again after the user still
saw the generic cutaway wall ending inside the camera shot. The Demo 3 side
wall now continues 30 m beyond the normal room depth, its rear wall is widened
by the same amount, and its continuous ceiling and floor cover the enlarged
envelope. Two additional full-height boundaries close the camera side and room
front beyond the camera position, making the environment a four-wall room
rather than an invisible cutaway.
The normal lesson camera is lower and closer to an operator's sightline. A
fresh current-build 1600x900 screen inspection shows a continuous interior wall
across the complete background with no visible wall end, wall top, sky, or
exterior space. Build result: 0 warnings, 0 errors. This is local rendered-GUI
evidence only.

Demo 4 no longer requires the learner to toggle a simulated detector ON and
then OFF for every count. The cabinet and Operator Console now expose a
momentary **NEW PALLET** event; each press creates one `pallet_detected` rising
edge and releases automatically. TYPE VALID and COUNT REQUEST remain maintained
permissives, and the scanned FB/FC logic still owns qualification, counting,
DB101 state, and the COUNT/pallet-valid outputs. The current five-demo learner
verifier passed with `failures=0`, including COUNT 1-5, machine Reset to zero,
and a fresh batch-two COUNT 1. The normal renderer was inspected with the new
control visible. A measured camera pass located the fitted view at approximately
(-7.9, 5.3, 8.4); Demo 4 now uses an 8 m-high, 36 m-square enclosing room with
continuous floor, ceiling, four outer walls, and four interior high-bay lights.
The current rendered view keeps the machine and adjacent cabinet visible with
no exposed wall end, wall top, sky, or blank exterior. Build result: 0 warnings,
0 errors. This is local offline/runtime and rendered-GUI evidence, not
installed-TIA or live-PLC proof.

The remaining five-demo presentation audit now uses the same point-of-use rule.
Demo 1 is a close, straight-on test-bench view with its functional PULSE and
RESET stations, stacklight, and live COUNT display; the redundant wall legend
was removed instead of enlarging another duplicate sign. Demo 2 is also a close
bench view with no floor-mounted control pedestals; its single teaching board
now says `2 SECOND ON-DELAY / HOLD REQUEST ON / LAMP TURNS ON AT 2.0 S` instead
of exposing internal point names. Demo 5's normal view already keeps the gantry,
pallet position, infeed, and adjacent operator cabinet together without the
previous field of duplicate count signs, so it was retained. Fresh current-build
1600x900 captures were inspected for Demos 1, 2, 3, and 5, and the app was left
open on the corrected enclosed Demo 3 view. The final build passed with 0
warnings and 0 errors. This is local rendered-GUI evidence only.

The bounded five-demo learner walkthrough was rerun once against the current
build and completed with `FIVE_DEMO_LEARNER_RESULT failures=0` at the actual
20 ms editor/runtime cadence. After that curriculum check, TIA property work
resumed. Hardware-sourced System constants are now read-only project metadata
with required provenance, CPU-wide name validation, history support, schema-10
Save/Load, and a four-column Default/All-tags presentation. A focused persistence
test passed, the build completed with 0 warnings/errors, and the normal-renderer
project-tree sweep passed 15/15 gates and 105/105 assertions. The evidence is
local/offline only; installed TIA V21 pairing, physical tag-to-operand drag,
Watch persistence screen proof, and live-PLC evidence remain open.

The PLC-tag drag gate now begins at the rendered tag-detail row's production
`_GetDragData` method rather than a payload helper. A single current-build
normal-renderer run passed 15/15 gates and 105/105 assertions and recorded one
production request for the exact `pulse_received` stable identity. Build result:
0 warnings/errors. This is stronger local application evidence, but an OS-level
human pointer drag and installed-TIA V21 comparison remain open.

The default demo operator rail was simplified again. Its large curriculum,
report, navigation, trace, monitor, engineering, and control-boundary group was
already hidden in current source; the remaining duplicate **Open Machine Guide**
button is now secondary as well because Curriculum already provides the full
guide route. The normal rail now stays focused on the active lesson action,
lesson inputs, runtime state, and PLC logic. Build result: 0 warnings/errors.

Demo 1 camera framing was corrected against the real `--shell-scene` startup
route. Its default lesson view now fills the usable scene aperture with the
complete test bench: PULSE, RESET, stack light, COUNT display, and the output
legend are all visible, while the surrounding empty room is excluded. A fresh
1600x900 current-build capture was visually inspected; the final build passed
with 0 warnings and 0 errors. This is local rendered-GUI evidence only.

The TIA workbench no longer labels a freshly opened authored editor draft as a
loaded controller. The conflicting green `AUTHORED LADDER LOADED` header was
reproduced with no runtime snapshot, then replaced by the amber
`AUTHORED LADDER READY · VERIFY + LOAD` state and an explicit editor-only
tooltip. Matching 1600x900 before/after captures were inspected and the build
passed with 0 warnings and 0 errors. This resolves the local contradictory
editor/Watch load-state wording; it is not installed-TIA or PLC evidence.

The normal 1600x900 TIA workspace now preserves meaningful object titles. PLC
tag-table rows show their full table name in the project tree while tag and
User Constant counts remain in the tooltip/detail document. The top toolbar
keeps the RungProof wordmark but hides the redundant engineering-workbench
subtitle below 1800 px, leaving the complete active Demo title visible. A fresh
current-build capture confirmed both changes without widening the project dock;
the build passed with 0 warnings and 0 errors.

The Siemens project-tree header was simplified without removing a workflow.
Its unexplained `+` duplicated the visible **Add new block** tree row, and the
external `x` duplicated the filter field's built-in clear affordance; both are
now hidden in the Siemens view. The tree receives a deliberate 340 px initial
width so removing those controls cannot collapse and ellipsize the OB or
instance-DB names. A fresh 1600x900 capture confirmed the reduced header and
complete object identities. Build result: 0 warnings, 0 errors.

The Global DB declaration header no longer permanently shows the redundant
**EDIT MEMBERS** action beside **+ MEMBER** and **EDIT**. Those two declaration
actions still enter the validated member editor directly; once inside, the same
control appears contextually as **PROJECT TREE**, preserving the explicit return
path required by the property workflow. A fresh 1600x900 capture confirmed the
clean four-action declaration header. Build result: 0 warnings, 0 errors.

PLC tag-table lifecycle actions are now contextual. Protected Default/SceneIO
tables no longer display disabled Rename/Delete controls; a user-created table
shows those actions, with Delete disabled until it is empty. Duplicate remains
available where supported. The active table identity was also raised from an
11 px low-contrast label to a 13 px pale-blue heading. A fresh 1600x900 capture
confirmed the cleaner Default-table view. Build result: 0 warnings, 0 errors.

Demo 3's conveyor-entry architecture now reads as one continuous enclosed
sorting room. The authored portal and the generated room shell share the same
wall finish, removing the brown/green vertical seam that made the portal look
like a short freestanding set wall. The full room shell already extends beyond
the camera and includes ceiling, rear, and camera-side boundaries; a fresh
1600x900 stopped-state capture confirmed no exposed wall end or empty world
behind the conveyor entry. Build result: 0 warnings, 0 errors.

Demo 4 now uses a tighter, lower operator viewpoint centered on the active
carton, inspection machine, and adjacent operator cabinet. This removes much of
the empty floor/room context and makes the installed cabinet controls and count
readout materially larger without cutting the process path out of the lesson
shot. A fresh 1600x900 stopped-state capture confirmed the new framing. Build
result: 0 warnings, 0 errors.

Demo 5 now uses the concise visible title **Demo 5 - Integrated Palletizer** so
the header and lesson panel no longer clip or wrap an implementation-detail
suffix. Its single adjacent operator cabinet remains the only status/control
station, and the five live HMI values now use substantially larger type with
deliberate three-column/two-column spacing across the available black display.
A fresh 1600x900 capture confirmed readable, non-overlapping labels without the
former row of duplicate freestanding count signs. Build result: 0 warnings, 0
errors.

The learner rail now suppresses empty machine-command headings and suppresses
the category heading when only one visible lesson-input category exists. Demo 1
therefore presents exactly one Pulse input directly beneath **Lesson Inputs**,
followed by runtime status and the PLC-logic route; it no longer spends three
rows describing one control. Multi-category lessons retain process-versus-fault
headings. A fresh 1600x900 Demo 1 capture confirmed the reduced rail. Build
result: 0 warnings, 0 errors.

Demo 2 now uses a low opposite-side operator view that explicitly frames all
five installed bench devices, eliminating the repeated elevated three-quarter
room composition and most unused floor. The oversized rear-wall timing sign was
removed because it duplicated the learner objective and became a cropped prop
in the close view. The maintained request, lamp, stop/reset, and elapsed-time
display remain together on the test bench. A fresh 1600x900 capture confirmed
the clean bench composition. Build result: 0 warnings, 0 errors.

Demo 4's existing `machine_running` seal-in is now a declared PLC-owned BOOL
scene output rather than unexposed editor memory. It drives both the inspection
conveyor and the machine spindle, so an accepted Start produces visible machine
motion and Stop/Reset removes that command. The adjacent cabinet now uses a
single wide COUNT HMI instead of the tiny accessory readout, and its machine
status tower shows amber while the PLC run latch is active before changing to
the existing green batch-valid state at five. A fresh running-state 1600x900
capture showed the amber tower and large cabinet HMI; build result: 0 warnings,
0 errors.

The Siemens Watch-table workspace now has one authoritative editor surface.
The table selector, table lifecycle controls, row controls, editable values,
comments, and simulator-only modify action were moved into the Watch document;
the former duplicate Watch table in the bottom diagnostics dock is no longer
shown when a Watch document opens. Diagnostics remains available through its
collapsed reopen bar. The Studio 5000 Watch-list layout is unchanged. A fresh
1600x900 capture confirmed the full Watch workflow fits in the editor without a
second clipped table; build result: 0 warnings, 0 errors.

The TIA `ladder-properties` review route no longer assumes that the active
draft contains contact 0 in network 0. It now selects the first actual contact,
falls back to a terminal instruction, and creates one disposable unsaved review
network only when the current draft has no editable element. This removed the
deterministic out-of-range startup exception and makes the route open a real
Contact Properties form. The shared instruction-properties popup is wider and
taller, uses larger high-contrast title/scope/identity/status text, and has
clearly separated COPY DETAILS, APPLY, and CANCEL actions. A fresh 1600x900
capture completed without the exception and showed the readable working form;
build result: 0 warnings, 0 errors. This is local RungProof UI evidence only.

TIA PLC-tag documents now select their first visible tag when opened without an
explicit row target. EDIT, PROPS, and XREF therefore start in a meaningful
enabled state instead of looking like dead controls until an unexplained extra
click. Explicit selections and per-table filtering still take precedence. The
offline/TIA boundary disclosure beneath the tag table now uses 13 px pale-blue
text with reserved height instead of low-contrast dark text. A fresh 1600x900
capture showed the selected tag, enabled actions, and readable disclosure;
build result: 0 warnings, 0 errors. This remains local offline UI evidence.

User Constants and System Constants now have dedicated empty states instead of
rendering a full column header over a large unexplained blank area. The User
Constants message points directly to `+ CONSTANT` and states that the object is
a typed read-only LAD input; the System Constants message states that the
offline project has no configured hardware metadata. The real grids return as
soon as rows exist. User Constants also select the first visible existing row
when a view is opened without an explicit target, while explicit/table-scoped
selection remains authoritative. Both disclosure lines use the same readable
13 px pale-blue treatment as PLC tags. New focused review routes make both
screens directly inspectable. Fresh 1600x900 captures showed both empty states;
build result: 0 warnings, 0 errors. Installed-TIA matching remains unverified.

The Siemens Project Objects rail is now contextual instead of exposing two
permanent layers of block and declaration commands. The top fixed declaration
toolbar was moved beside the declaration fields it operates on. A blank member
form shows only `+ PARAMETER`; New, Cancel, Properties, Duplicate, and Delete
appear only when selection or pending state makes them meaningful. Block-level
Duplicate/Delete remain available through the existing Properties workflow and
are no longer repeated in the narrow rail; block creation is labeled `+ BLOCK`
instead of misleadingly `+ FC`, and Cancel appears only during a pending rename.
The legacy/parameterized status is readable dark text, the conversion action is
styled consistently, the interface note wraps, and compact field placeholders
fit the dock. Internal verifier node paths were updated with the moved controls.
A fresh 1600x900 capture showed the reduced rail; build result: 0 warnings,
0 errors. This remains local RungProof UI evidence only.

The Siemens instance-DB workspace now opens with the project tree in the left
rail instead of leaving an unrelated editable FB-interface form beside the
read-only generated DB. The first inherited member is selected automatically,
so the clearly named MEMBER PROPERTIES action is immediately meaningful. The
offline-support boundary was shortened, enlarged, and given the same readable
pale-blue treatment used by the tag and constants screens. A dedicated
`ladder-instance-db` review state opens the same unsaved representative FB
instance in the visible app without writing project data. A fresh 1600x900
capture confirmed the contextual rail, selected member, readable table, and
concise boundary; build result: 0 warnings, 0 errors. This is local RungProof
UI evidence only; installed-TIA behavior remains unverified.

The Siemens project-object Properties dialog now presents a compact operator
summary instead of a diagnostic dump. Program-block properties group the type,
contents, execution schedule, references, assigned instances, offline status,
delete protection, and simulator boundary into eight readable lines. The
separate Copy Path control and unavailable lifecycle commands are hidden;
applicable Open, Rename, Duplicate, and Cross-reference actions remain visible,
with one Copy Summary action. The dialog is narrower, uses a wider readable text
column, and larger body type. A dedicated `ladder-object-properties` review state
opens the selected program-block dialog deterministically. A fresh 1600x900
capture confirmed the simplified dialog; build result: 0 warnings, 0 errors.
This remains local RungProof UI evidence only.

The Siemens PLC-tag Properties dialog is now a focused property summary rather
than another lifecycle-command toolbar. Its body combines type and role, drops
redundant name/identity rows, uses larger text, and presents Table, Address,
Initial value, Scene binding, Comment, References, Delete status, and the
offline hardware boundary in nine concise lines. Duplicate, Delete,
Cross-reference, and copy actions remain available from the tag table/right-click
workflow and are no longer repeated in the dialog; Properties shows only Close
and Edit Definition when editing is allowed. The new `ladder-tag-properties`
review state selects the first visible tag and opens the same production dialog.
A fresh 1600x900 capture confirmed the focused layout; build result: 0 warnings,
0 errors. This remains local RungProof UI evidence only.

The Siemens Watch-row Properties dialog is now a compact inspection surface
instead of duplicating the entire Watch context menu. It presents the row
reference/kind, table and position, resolved tag type/role, display format,
monitor and modify values, both comments, removal availability, and the offline
evidence boundary in ten concise lines with larger type. Copy, cross-reference,
format/modify/comment editing, and removal remain available from the Watch table
cells, toolbar, or right-click menu and are no longer repeated in Properties;
only Open Tag is shown when a symbolic declaration resolves. The dialog minimum
size was reduced to fit its actual content. A deterministic
`ladder-watch-properties` review state creates an unsaved Commissioning table,
selects its first production row, and opens the same Properties handler. A fresh
1600x900 capture confirmed the focused layout; build result: 0 warnings,
0 errors. This remains local simulator evidence, never live-PLC evidence.

The Siemens generated instance-member Properties dialog now shows only the
engineering facts needed to understand the selected member. FB members present
section/type, instance DB and assigned instance, owning FB, inherited
default/start value, Retain, comment, execution meaning, edit source, and the
read-only/live-value boundary. IEC members use the same compact structure. Copy
Details was removed from the visible dialog; Open Owner is the single applicable
action. Body text is larger and the dialog footprint is smaller. The new
`ladder-instance-member-properties` review state opens a representative unsaved
member through the production instance-DB route. That route exposed and fixed
two deterministic harness defects: repeated review-state initialization now
reuses the stable `db_layout_axis` instance instead of adding a duplicate FB,
and its deferred signal callback is explicitly void rather than returning a
nullable Godot Error. Re-running the exact failing capture completed without
either exception. A fresh 1600x900 capture confirmed the focused dialog; build
result: 0 warnings, 0 errors. This remains local offline UI evidence only.

The Siemens Global-DB member Properties dialog is now a focused declaration
summary. It presents type, DB and row position, start value, Retain metadata,
comment, storage model, deletion availability, and one explicit capability
boundary in eight readable lines. Copy, Duplicate, Move, and Delete remain in
the declaration grid toolbar/context menu and are no longer repeated in the
dialog; only Edit Definition is shown when allowed. The long tiny capability
paragraph below the Global-DB grid was replaced with one readable high-contrast
line, while its detailed supported/omitted feature list remains available in
the tooltip. The `ladder-global-db-member-properties` review state creates or
reuses the unsaved representative DB, selects its first row, and opens the same
production Properties handler. A fresh 1600x900 capture confirmed both the
focused dialog and concise boundary; build result: 0 warnings, 0 errors. This
remains local RungProof UI evidence only.

The Siemens FB/FC interface-declaration Properties dialog is now a focused
parameter summary instead of a second lifecycle toolbar. It presents the
section/type, owning block and semantics, row position, initial value, Retain,
comment, execution meaning, CALL impact, deletion availability, and the local
offline boundary in readable text. Copy, Duplicate, Move, and Delete remain in
the interface editor workflow and are no longer repeated in the dialog; only
Edit Definition is shown when editing is allowed. The deterministic
`ladder-interface-properties` review state now selects the owning FB before it
opens the first interface declaration through the production Properties
handler. A fresh 1600x900 capture confirmed the focused dialog; build result:
0 warnings, 0 errors. This remains local RungProof UI evidence only; installed
TIA behavior is not claimed.

The Siemens OB/task schedule Properties dialog is now a focused schedule
summary instead of a duplicate lifecycle toolbar. It presents schedule class
and order, entry block, cycle and priority, execution meaning, local runtime
state, delete protection, offline-change behavior, and the local evidence
boundary in eight readable lines. Copy, Duplicate, Move, and Delete remain in
the project-object workflow and are no longer repeated in Properties; Open
Entry Block and Edit Schedule are the only applicable actions shown. The new
`ladder-task-properties` review state selects the first modeled OB schedule and
opens the production Properties handler. A fresh 1600x900 normal-renderer
capture confirmed the focused dialog; build result: 0 warnings, 0 errors. This
remains local RungProof UI evidence only and does not prove installed-TIA or PLC
execution behavior.

The shared Siemens project-object Properties dialog now behaves as an
inspection surface instead of recreating the project-tree toolbar. Copy,
Add, Rename, Duplicate, Delete, and Cross-reference remain available from the
project tree and are hidden in Properties for blocks, DBs, tag tables, watch
tables, folders, controller entries, and diagnostics. Properties shows only
Open Object when the selected object can be opened, plus Open Owner for a
generated instance DB. The new `ladder-active-document-properties` review state
opens Properties through the active editor document's production PROPS action.
A fresh 1600x900 normal-renderer capture confirmed the Main OB summary with
only Close and Open Object; build result: 0 warnings, 0 errors. This is local
RungProof UI evidence only.

The Siemens PLC User Constant Properties dialog is now a focused immutable
declaration summary. It presents type and read-only role, tag table, fixed
value, comment, LAD references, delete protection, compile-time execution
meaning, and the offline boundary in eight readable lines. Copy,
Cross-reference, Duplicate, and Delete remain in the constants grid/context
menu and are no longer repeated in Properties; Edit Definition is the only
applicable action shown. The deterministic `ladder-user-constant-properties`
review state adds an unsaved DINT training constant to Default tag table,
opens that table through the production project-tree route, selects User
Constants, and opens the production Properties handler. A fresh 1600x900
normal-renderer capture confirmed the focused dialog; build result: 0 warnings,
0 errors. This remains local RungProof UI evidence only.

The Siemens System Constants view now exposes selected hardware provenance in
a persistent, high-contrast read-only detail panel instead of requiring a
hover tooltip. Selecting a row shows its name, hardware type/value, exact
hardware source, and comment; no edit or lifecycle controls were added. The
new `ladder-system-constant-detail` review state inserts one unsaved sourced
hardware constant, opens Default tag table through the project tree, switches
to System Constants, and selects the production grid row. A fresh 1600x900
normal-renderer capture confirmed the readable panel; build result: 0 warnings,
0 errors. This remains local offline metadata evidence only.

The Siemens Watch-table editor no longer presents two full rows of duplicate
lifecycle buttons. Table creation, rename, duplicate, reorder, and delete are
consolidated under TABLE ACTIONS; row removal, reorder, Add All, and Clear are
consolidated under ROW ACTIONS. Both menus invoke the existing production
handlers and mirror their enabled state. The primary authoring controls remain
visible: symbol selection, + SYMBOL, absolute address, + ADDRESS, and SIM
MODIFY. The deterministic `ladder-watch-compact` route opens an unsaved named
Watch table through the production project-tree path with diagnostics
collapsed. A fresh 1600x900 normal-renderer capture confirmed the cleaner
workspace and larger usable table area; build result: 0 warnings, 0 errors.
Watch-table disk persistence was still open at this checkpoint; the later
`ladder-watch-persistence` review route below now closes that local evidence.

The Siemens ladder-instruction Properties dialog now presents only operator-
meaningful location and edit data. Internal owner, network, and instruction
IDs were removed from the visible form and replaced with a concise network,
branch, position, instruction-count, and terminal summary. The redundant Copy
Details action is hidden; Apply and Cancel retain the existing transactional
editing behavior. The deterministic `ladder-properties` route opens the
production contact Properties handler. A fresh 1600x900 normal-renderer
capture confirmed the focused form; build result: 0 warnings, 0 errors. This
remains local RungProof UI evidence only and does not prove installed-TIA or
PLC execution behavior.

The Global DB editor now keeps its capability boundary to one readable line:
five editable scalar fields, optimized-access offline scope, and no live values
or simulated power-cycle retention. The representative Retain row no longer
claims post-power-cycle behavior; it identifies Retain as offline metadata. The
new `ladder-global-db` review state opens the production Global DB surface. A
fresh 1600x900 normal-renderer capture confirmed that the declaration table is
the visual focus; build result: 0 warnings, 0 errors.

On 2026-10-21 the remaining local Global DB helper-note contrast issue was
closed. The scope disclosure is now a 42 px bordered information band with
14 px high-contrast text rather than a small line that resembles debug output.
The fresh 1600x900 normal-renderer capture
`outputs/tia-global-db-scope-band.png` shows the complete disclosure and the
unobscured five-column declaration grid. The focused build passed with 0
warnings and 0 errors. Visible project Save/Load and matched installed-TIA V21
comparison remain open; no physical PLC behavior is claimed.

Watch-table rejection feedback no longer disappears during the refresh that
restores an invalid cell. The handler now restores the active Watch document
first, then publishes and repaints the rejection. The message uses 14 px,
high-contrast text directly above the row controls. The deterministic
`ladder-watch-format-feedback` state invokes the production rejection path and
shows `Float` rejected for BOOL while the stored `Auto` format remains intact.
A fresh 1600x900 normal-renderer capture confirmed the visible persistent
message; build result: 0 warnings, 0 errors. This is local RungProof UI evidence,
not installed-TIA or live-PLC proof.

The deterministic `ladder-watch-running` state now uses the production Verify
+ Load and Run handlers before reopening the Watch document. A fresh 1600x900
normal-renderer frame shows Demo 3 running at scan 660, live BOOL Monitor
values, SIMULATOR ONLY, and NO PHYSICAL PLC. That frame exposed a large-project
layout defect: project-tree content could paint below the Ladder workspace over
Scene I/O and the global transport. The Ladder panel now clips descendants to
its assigned viewport while the project tree remains scrollable. The
regenerated frame keeps Scene I/O and the complete global transport visible;
build result: 0 warnings, 0 errors. This is production-handler/rendered local
evidence, not an OS-level click walkthrough or installed-TIA/live-PLC proof.

Demo 3 no longer places a short freestanding annex behind the height-sorting
cell. Its warehouse composition now carries the rear wall across the full
protected-room width, cuts the conveyor infeed through that wall, adds deep
portal jamb/header reveals, and connects a full far-side wall. The two near
walls remain omitted only as the intentional camera cutaway. A fresh 1600x900
normal-renderer capture confirmed that the viewport no longer exposes sky or
an artificial gap behind the infeed wall. The source side now also has a dark,
three-metre-deep framed material tunnel around the unobstructed belt. This makes
the wall penetration and carton emerging from the staging room readable in the
normal close operator view instead of blending into another pale wall.
`outputs/demo3-wall-infeed-final.png` records the current view; build result:
0 warnings, 0 errors.

Watch-table disk persistence now has a deterministic visible review route.
`ladder-watch-persistence` creates a named Commissioning table with authored
row format, modify value, and comment; saves through the production validated
project writer; destroys the in-memory table content; and reopens through the
production project Open path. It rejects the review unless table and row IDs
plus all authored row metadata match exactly. A fresh 1600x900 normal-renderer
capture shows the reopened project filename, restored table/rows and metadata,
and the green identity-preserved status. Build result: 0 warnings, 0 errors.
This closes local RungProof Watch persistence evidence only; installed-TIA and
live-PLC proof remain separate.

Block and PLC tag-table duplication now have a current visible production-path
review. `ladder-duplication` invokes the existing Duplicate Block and Duplicate
Tag Table handlers, then requires regenerated block/interface/network IDs and
independent copied tag symbols with blank addresses and scene bindings. The
review route is idempotent for repeated capture initialization. A fresh
1600x900 normal-renderer frame shows `FB_ConveyorDrive Copy` in Program blocks
and the active `Default tag table Copy` grid with copied names and blank address
cells; build result: 0 warnings, 0 errors. This remains local RungProof evidence.

The deterministic pending-form Project Open review now stages the active PLC-tag
comment through the same `TextChanged` event used by real editing before invoking
the production Open command. This corrected a capture-harness omission that had
allowed the file picker to appear without a dirty form. The rendered guard names
`machine_permissive`, exposes Apply/Discard/Cancel, blocks the file picker, and
uses a 720×250 minimum with 16 px body text. A fresh 1600x900 normal-renderer
capture confirmed the readable guard; build result: 0 warnings, 0 errors. This
is local RungProof UI evidence only.

The current-source PLC-tag/User Constant CRUD route now drives production
controls rather than mutating the document directly. It creates and edits an
unreferenced BOOL PLC tag, verifies Undo/Redo, deletes it, then creates and edits
`ReviewTrainingEnabled` through the User Constants Apply path and verifies its
Undo/Redo result. It now saves through the production serializer, removes the
completed constant from memory, reopens the project, and requires the same
stable BOOL `TRUE` row and comment before the final frame. The capture route
reopens the live Default tag table after project replacement so the evidence
actually shows the persisted User Constants grid rather than the fallback OB.
Its shorter comment, `Applied, undone, redone, and reopened`, fits visibly in
the table without ellipsis. A fresh 1600x900 normal-renderer capture was visually
inspected. That frame exposed the User Constants action row clipped by the
Diagnostics splitter; the non-edit grid minimum was reduced from 180 px to
120 px, retaining expansion while making **+ CONSTANT**, **EDIT**, **PROPS**,
**DELETE**, and the scope disclosure fully visible at 1600x900. The post-fix
build passed with 0 warnings and 0 errors and the corrected frame was inspected.
This is local production-handler, rendered, and file-backed persistence
evidence, not OS-level pointer, installed-TIA, or live-PLC proof.

The user set installed TIA Portal V17 as the required comparison baseline on
2026-10-21. The V17-native `Winters_TIA_Bench.ap17` project was opened offline
without Save, download, connection, force, or PLC action. Current captures cover
the installed Project View, project tree, instance DB declaration/properties,
PLC tag table/properties, and Watch table. Same-object and same-state pairing to
RungProof remains open; historical V21 blocker statements are superseded by
this baseline decision.

The PLC-tag operand drawer now has a deterministic `ladder-tag-drag-ready`
state with `reset_command` selected beside the exact compatible BOOL contact.
Its tag-name column is 190 px and Data type is fixed at 92 px, eliminating the
ellipsized source names visible in the first frame. A fresh 1600x900 normal-
renderer capture was inspected; build result: 0 warnings, 0 errors. A PID-
pinned window-message drag and a physical `SendInput` drag did not generate the
application-owned post-drop capture because the current execution desktop does
not expose the Godot window on its interactive capture surface. The physical
pointer item remains open; delivery was not misreported as UI success.

Demo 4 now has current visible production-path proof instead of a stopped
scene-only screenshot. The idempotent `operator-demo4-active` review state
presses the same guided control available to the learner and completes the
actual sequence: run current logic, Start, Type OK, Count Request, five distinct
momentary pallet events, machine Reset, fresh Start, and the first event of the
second batch. The runtime capture now waits for that route's explicit prepared
state; it can no longer save the former in-progress fourth-event frame. The
inspected 1600x900 capture `outputs/demo4-current-functional-pass.png` shows the real pass dialog with
first-batch count 5, reset to 0, second-batch count 1, and the local runtime
still running. The focused build passed with 0 warnings and 0 errors. This is
offline RungProof UI/scan evidence only; PLC transport remained disconnected.

Demo 1's curriculum acceptance now matches its authored lesson instead of
passing at the first threshold. The session requires count 3, machine Reset
with both lamp OFF and published count 0, then a repeat count to 3. Pass capture
also waits until the CTU snapshot agrees with the visible published count, which
eliminated a misleading one-scan `CV=2` / `COUNT 3` evidence mismatch exposed
during review. The idempotent `operator-demo1-active` route uses the same guided
control shown to the learner. The inspected 1600x900 capture
`outputs/demo1-passed-reset-repeat.png` shows `CV=3 Q=TRUE`, visible COUNT 3,
lamp ON, and the complete reset/repeat proof. The focused build passed with 0
warnings and 0 errors; PLC transport remained disconnected.

Demo 2's curriculum acceptance now requires the full operating and recovery
lesson instead of passing at the first lamp-on event. The learner must prove a
first 2000 ms TON, machine Stop with output off, machine Reset with ET 0,
REQUEST OFF, REQUEST ON, and a second complete delay. The idempotent
`operator-demo2-active` route uses the production guided control and waits on
accepted runtime state between phases. The inspected 1600x900 capture
`outputs/demo2-passed-stop-reset-repeat.png` shows ET=2000 ms, Q=TRUE, visible
elapsed 2000, lamp TRUE, and the complete recovery proof. The focused build
passed with 0 warnings and 0 errors; PLC transport remained disconnected.

Demo 3's curriculum acceptance now requires the entire authored recovery
exercise after the three correct carton routes: machine Stop/fresh Start,
simulated trip clear/Reset/fresh Start, and drive-overload clear/Reset/fresh
Start. PLC-owned `machine_run` gates use the accepted controller snapshot,
while trip and overload remain scene-owned lesson inputs. The idempotent
`operator-demo3-active` route uses the production guided control, and the
generic capture timer is held until that route explicitly reports passed so it
cannot save an in-progress frame. The inspected 1600x900 capture
`outputs/demo3-passed-full-recovery.png` shows the real pass dialog,
`machine_run=TRUE`, both faults OFF, and the complete route/Stop/trip/overload
proof. The focused build passed with 0 warnings and 0 errors; PLC transport
remained disconnected.

Demo 5's curriculum acceptance now matches the authored PLC restart
constraints. A completed first layer must be Reset before Start is accepted;
the learner then proves Stop/restart, trip clear/Reset/restart, pallet-position
permissive loss/restore/Reset/restart, and a complete second four-carton layer.
The idempotent `operator-demo5-active` route uses only the production guided
control and waits on measured PLC/plant transitions. The inspected 1600x900
capture `outputs/demo5-passed-full-recovery.png` shows `placed_cartons=4`,
`layer_complete=TRUE`, restored permissives, and the full recovery proof. The
focused build passed with 0 warnings and 0 errors; PLC transport remained
disconnected.

The five-demo curriculum now has one cumulative production-path proof rather
than five isolated 1/5 captures. `operator-five-demo-active` changes scenes
through the normal request path, runs each lesson through its production guided
control, and waits for each session completion latch before advancing. The
current rebuilt GUI was rerun through that same uninterrupted path after the
Demo 3 infeed and Demo 4 capture-timing corrections. The inspected 1600x900
capture `outputs/five-demo-current-complete.png` shows the
real **Five-demo curriculum complete** dialog, `All five demos passed this
session`, and bottom status `DEMO 5/5 · 5 PASSED`. The focused build passed with
0 warnings and 0 errors. This is cumulative offline RungProof evidence; it is
not installed-TIA, OS-pointer, live-PLC, commissioning, or safety proof.

The Global DB property workbench now has a file-backed persistence review route.
`ladder-global-db-persistence` saves `Machine_Setpoints`, destroys its in-memory
name and member collection, reopens the saved project, and requires stable block
and member identities plus exact BOOL/INT/REAL types, start values, Retain, and
comments. The inspected 1600x900 capture
`outputs/tia-global-db-save-open-persistence.png` shows the reopened three-row
declaration and visible `REOPENED FROM DISK · IDENTITIES PRESERVED` status. The
focused build passed with 0 warnings and 0 errors. This is local production
Save/Open and renderer evidence, not installed-TIA or live-PLC proof.

The Global DB property form now has one deterministic production transaction
route. `ladder-global-db-transaction` proves invalid INT and duplicate-name
rejection without mutation/history, Cancel, Apply, the actual ladder Undo/Redo
controls, production Save/Open, and stable declaration identity after destroying
the in-memory rows. It also uses two Global DBs with a same-index `BatchTarget`,
rejects a stale first-DB form after the active block changes, and applies a
second-DB-only edit through the second DB's visible property form. Both DB member
collections are cleared before the saved file is reopened.
The inspected 1600x900 capture
`outputs/tia-global-db-property-transaction.png` shows `BatchTarget` reopened as
INT start value 7 with Retain checked and comment `Applied through property form`.
The declaration Tree now uses the shared high-contrast selected-row style, fixing
the unreadable light-on-light selected Name cell exposed by the first capture.
The focused build passed with 0 warnings and 0 errors. This is local RungProof
form/renderer/persistence evidence only; physical pointer repetition,
installed-TIA V21 comparison, and live-PLC proof remain open.

The same Global DB transaction route now enters the real focused declaration
cell editor for Name, Start Value, and Comment. It commits the stable
`ConveyorSpeed` member as `LineSpeed`, REAL `1300`, and `Inline edit persisted`,
then proves those exact cells and identity survive the same destructive
Save/Open cycle. The inspected 1600x900 frame visibly shows the reopened row and
the expanded inline/form/isolation completion status. This closes the local
current-build inline cell path only; physical pointer and installed-V17 evidence
remain separate.

On 2026-10-21 the Demo 3 Watch workspace was rerendered through the production
`ladder-watch-running` route. The inspected 1600x900 frame shows consistent
loaded offline-runtime state across the workbench header, Watch document, and
global runtime strip at scan 660; the older contradictory loaded/not-loaded
caption did not reproduce. A physical Windows-pointer retry could not observe
the Godot surface because the execution desktop returned wallpaper for the
responsive app window rectangle, so no tag drag is claimed. The visible app
was left open on the current Demo 3 editor build. Matched installed-TIA V17 and
physical pointer evidence remain external blockers.

On 2026-10-21 the Demo 5 operator installation was simplified after direct
visual review. The unbound red and amber roof-tower segments were removed.
`vacuum_pick`, `cell_active`, and `layer_complete` now remain as functional
PLC-bound green pilot lights on the cabinet fascia with concise `VACUUM`,
`ACTIVE`, and `LAYER` legends.
No signal or runtime binding was removed. The focused build passed with 0
warnings and 0 errors, and the current visible app was opened successfully on
`Demo 5 - Integrated Palletizer` (PID 36736, responsive).

The same GUI pass corrected Demo 2's `delayed_lamp` device from an unjustified
three-color status tower to one green bench output lamp. The existing PLC point
binding is unchanged. The inspected 1600x900 capture
`outputs/demo2-single-green-lamp.png` shows REQUEST, the green timed lamp,
working STOP and RESET, and the elapsed-time display together on one close-up
test bench. The focused build passed with 0 warnings and 0 errors.

Demo 1 received the same source-level correction: `threshold_lamp` is now one
green bench output lamp rather than a decorative three-color tower. Pulse,
Reset, the live count display, and the PLC point binding remain unchanged. The
inspected 1600x900 capture `outputs/demo1-single-green-lamp.png` shows the four
functional lesson devices together on one front-facing bench.

Demo 4's remaining generic tower was then removed because its red segment had
no binding. Amber `machine_running` and green `pallet_count_valid` are now two
independently bound cabinet pilot lights labeled `RUNNING` and `BATCH VALID`.
The dedicated COUNT HMI remains live and is no longer duplicated in the normal
panel-layout inventory. The inspected 1600x900 capture
`outputs/demo4-two-pilot-cabinet-final.png` shows the consolidated installation;
the focused build passed with 0 warnings and 0 errors. The updated visible app
was opened on Demo 4 (PID 29480, responsive).

Demo 3's cabinet tower no longer contains decorative fault segments. Green is
bound to `conveyor_running`, red to the simulated `safety_trip`, and amber to
the simulated `drive_overload`. All three use independent `indicatorChannel`
bindings so concurrent trip and overload indications remain visible. The
inspected 1600x900 capture `outputs/demo3-fault-tower-functional-final.png`
shows red and amber illuminated together with both existing lesson inputs ON.
The focused build passed with 0 warnings and 0 errors, and the current visible
app was opened on Demo 3 (PID 30608, responsive).

Demo 4's presentation profile now frames only the active carton, inspection
machine, and consolidated cabinet as required subjects. Passive pallet
inventory and the long conveyor can remain in natural context but no longer
force a distant room-wide camera. The inspected 1600x900 capture
`outputs/demo4-tight-machine-cabinet.png` shows the tighter operator view with
the process and cabinet filling the frame. The focused build passed with 0
warnings and 0 errors, and the current visible app was reopened on Demo 4
(PID 39944, responsive).

Demo 5 now uses the same process-first framing rule. The gantry, working
carton/pallet area, and adjacent cabinet determine the camera distance; the
long infeed and pickup support remain contextual rather than forcing a wide
room shot. The inspected 1600x900 capture
`outputs/demo5-tight-gantry-cabinet.png` shows the larger HMI, pilot legends,
working controls, gantry, and pallet in one focused view. The focused build
passed with 0 warnings and 0 errors, and the current visible app was reopened
on Demo 5 (PID 40168, responsive).

Demo 4 now uses a distinct near-head-on operator view rather than another
three-quarter room angle. The count HMI and control labels face the camera while
the carton path remains visible beside the inspection machine. The redundant
wall-sized `PALLET BATCH INPUTS` board was removed because the lesson panel and
functional cabinet already identify the same controls. The inspected 1600x900
capture `outputs/demo4-head-on-clean-wall.png` shows the clean final framing.
The focused build passed with 0 warnings and 0 errors, and the current visible
app was reopened on Demo 4 through the guided production route (PID 31796,
responsive). Its log shows the maintained Type/Count inputs and six accepted
momentary pallet events spanning the five-count batch plus the first event after
Reset/restart. An attempted OS desktop snapshot captured another foreground
application, so it was moved to the Recycle Bin and is not cited as evidence.

Demo 4's generic CNC/VMC enclosure was not credible for the pallet-counting
lesson and has been replaced by the approved fixed industrial barcode-scanner
asset, rotated toward the conveyor. The catalog asset's loose preview carton,
printed barcode, and fixed scan beam are suppressed in this scene because the
lesson already has one real conveyor load and uses an explicitly manual NEW
PALLET event; retaining them would imply an automatic decode that the model
does not perform. `machine_running` continues to drive the conveyor and amber
RUNNING pilot, with no fake spindle binding. The inspected 1600x900 capture
`outputs/demo4-barcode-inspection-station-final.png` shows one load, the
adjacent scanner, and the nearby operator cabinet. The focused build passed
with 0 warnings and 0 errors, and the visible app was reopened on the corrected
guided Demo 4 route (PID 36652, responsive).

Curriculum launch behavior was simplified on 2026-10-21. Opening any numbered
Demo 1–5 route now follows the existing validate/compile/load path and starts
the authored local scan automatically after the guarded scene replacement is
accepted. Invalid or edited drafts still block through the normal validator;
non-curriculum scenes remain manual. Normal startup does the same for Demo 1,
so the primary learner action is immediately `OPERATE PULSE COUNT BUTTON`
instead of another load/run prerequisite. Lesson procedures and the quick-start
guide now describe this behavior and retain App Stop/Run as an explicit learner
exercise. The inspected 1600x900 capture
`outputs/demo1-auto-running-launch.png` shows Demo 1 `IN PROGRESS`, local scan
`RUNNING`, and the compact operational controls. The focused build passed with
0 warnings and 0 errors, and the visible app was reopened on this current Demo
1 build (PID 30356, responsive).

The Operator transport was reduced to the controls needed while running a
lesson: App Run, App Stop, App Reset, the curriculum return/progress button,
and the current scene/status. Pause, single-scan, scan history, Save, and Load
are now hidden in Operator and remain available in Logic and split engineering
views. This removes editor/session-management controls from the machine-facing
surface without deleting their functionality. The inspected 1600x900 capture
`outputs/demo1-compact-operator-transport-final.png` shows the reduced strip
with Demo 1 already running. The focused build passed with 0 warnings and 0
errors, and the visible app was reopened on the current build (PID 38692,
responsive).

Demo 3's focused camera margin was increased just enough to retain the complete
adjacent operator cabinet. The inspected 1600x900 capture
`outputs/demo3-complete-cabinet-framing.png` shows the full functional
START/STOP/RESET/ENABLE and trip/overload controls, both readouts, the complete
sorting process, and the wall infeed in one operator-height view; it does not
return to the former room-wide three-quarter composition. The same render also
confirms the simplified Operator transport applies outside Demo 1. The focused
build passed with 0 warnings and 0 errors, and the visible app was refreshed on
the current build (PID 40928, responsive).

The compact Operator rail no longer repeats a large `OPEN DEMO n PLC LOGIC`
button because `LOGIC EDITOR` remains permanently available in the header.
The retained hidden route preserves structural compatibility for existing
review code, while the normal learner surface now contains only lesson
identity, one primary next action, actual lesson inputs, and runtime state.
Static inspection of all five scene contracts also confirmed every visible
lesson pushbutton/selector references a declared runtime action; the shared 3D
interactor supplies a pointing-hand cursor and accepted mechanical feedback.
The inspected 1600x900 capture
`outputs/demo1-final-minimal-operator-panel.png` shows the final minimal rail.
The focused build passed with 0 warnings and 0 errors, and the visible app was
reopened on this build (PID 23700, responsive).

Curriculum auto-start now applies to every user-facing scene-selection route,
including the Scenario browser, not only controls that explicitly requested the
Operator view. All Demo 1–5 selections therefore pass through the same existing
validate/compile/load/run path; an invalid cached draft still blocks safely and
non-curriculum catalog scenes remain manual. This closes the route where a demo
selected from Scenario could appear loaded but do nothing. The focused build
passed with 0 warnings and 0 errors, no broad runtime sweep was started, and
the visible app was refreshed on the current build (PID 38428, responsive).

Generated installation legends no longer share a coplanar face with the bench
fascia or operator-cabinet door. The shared composer now gives every generated
identification plate a physical 3 mm mounting-surface standoff and records that
surface/clearance as geometry evidence. The focused verifier rejects missing,
coplanar, or penetrating plates and passed the five curriculum scenes: Demo 1
and Demo 2 each have one generated output legend, Demo 3 has none, Demo 4 has
two, and Demo 5 has three. Fresh 1600x900 normal-renderer captures of all five
demos were inspected; the wood/sign competition reported on Demo 1 is gone and
no remaining generated legend intersects its host face. The focused build
passed with 0 warnings and 0 errors. The visible app was relaunched on the
corrected Demo 1 build (PID 37336, responsive); no TIA or live-PLC claim is
attached.

The same five-demo visual pass exposed a separate Demo 5 cabinet collision:
the lower edge of the shared black HMI physically crossed the third-row
VACUUM/ACTIVE/LAYER pilot lights. The HMI and its five large readouts now use
the previously unused upper-door area, leaving the three functional green
pilots on a distinct row below the screen. The inspected 1600x900 capture
`outputs/demo5-hmi-pilot-clearance.png` shows the separation. The focused build
passed with 0 warnings and 0 errors, and the visible app was reopened on the
corrected Demo 5 view (PID 16336, responsive).

The remaining Demo 5 moving-value smear was renderer history, not overlapping
HMI geometry: stopped values were crisp, while the changing X encoder retained
several prior frames. The project had temporal anti-aliasing enabled on top of
4x MSAA. TAA is now disabled and 4x MSAA remains enabled, avoiding old-frame
blending without removing edge smoothing. The inspected running capture
`outputs/demo5-hmi-msaa-no-taa.png` shows one crisp X-count value during gantry
motion; the unsuccessful display-throttling and Label3D alpha-mode experiments
were removed rather than left in the runtime.

The curriculum rail no longer duplicates a visible highlighted lesson input
with a second large green `OPERATE ...` button above it. The primary green
action remains available for setup, load/run, completion navigation, and the
machine START/STOP/RESET commands intentionally omitted from the lesson-input
bank. Once a visible process or fault input is next, that input alone receives
the `NEXT` highlight. The inspected running Demo 4 capture
`outputs/demo4-no-duplicate-guided-control.png` shows the smaller rail with one
clear next action; the focused build passed with 0 warnings and 0 errors.

Automatic curriculum pass popups were removed. Completing a demo still records
its evidence, updates the pass count/status, and exposes normal next/repeat/
trace/monitor navigation in the existing interface, but no modal dialog covers
the machine scene. The obsolete completion-dialog fields, construction, action
handlers, and popup trigger were deleted rather than merely hidden. The focused
build passed with 0 warnings and 0 errors.

Demo 4 now has visible material flow instead of only a changing counter. Each
`NEW PALLET` event queues the scene carton at the conveyor infeed; the existing
PLC-owned `machine_running` output advances it through the fixed inspection
scanner, machine STOP freezes travel, and machine RESET clears queued events
and restores the infeed pose. Multiple learner presses are queued so visual
events are not discarded while a carton is already moving. The PLC counter
logic and its qualification inputs remain authoritative and unchanged. The
focused learner regression now requires the carton position to advance after a
qualified event, the build passed with 0 warnings and 0 errors, and the
inspected active capture is `outputs/demo4-moving-pallet.png`.

The Operator side rail no longer renders scene-input buttons or the duplicate
green guided machine-action button. Scene actions remain registered internally
for the 3D interaction path and deterministic review automation, but learners
operate the installed controls in the scene. A follow-up render exposed that
Demo 4's TYPE OK and COUNT REQUEST devices had occupied the HMI area; the
cabinet rows were lowered so all required controls are now visible and
clickable below the count display. The inspected capture
`outputs/demo4-all-controls-on-cabinet.png` shows TYPE OK, COUNT REQ, START,
STOP, RESET, NEW PALLET, BATCH VALID, RUNNING, and COUNT on the cabinet, while
the side rail contains status only. The build passed with 0 warnings/errors.

Current normal-renderer captures confirm the same scene-control-only layout
across the complete curriculum: `outputs/demo1-scene-controls-only.png`,
`outputs/demo2-scene-controls-only.png`, `outputs/demo3-scene-controls-only.png`,
`outputs/demo4-all-controls-on-cabinet.png`, and
`outputs/demo5-scene-controls-only.png`. Demo 1 exposes PULSE/RESET and its
lamp/readout on the bench; Demo 2 exposes REQUEST/STOP/RESET and its lamp/time
readout on the bench; Demo 3 exposes enable, machine commands, simulated
faults, status tower, and both readouts on its cabinet; Demo 4 exposes both
permissives, all machine/event commands, two indicators, and COUNT on its
cabinet; Demo 5 exposes its permissive, commands, recovery inputs, indicators,
and live HMI on its cabinet. Every side rail contains lesson identity and
runtime status only.

The status-only Operator rail now collapses to its real content height instead
of retaining the former full-height control-bank bounds. It shows only the
active lesson identity/objective and local runtime state; the large empty black
overlay no longer blocks the machine after the side controls were removed.
The inspected 1600x900 normal-renderer frame is
`rungproof-next/outputs/demo4-compact-status-rail-visible.png`. The focused
build passed with 0 warnings and 0 errors, and the visible Demo 4 app was
restarted on this build (PID 26652, responsive). All lesson inputs remain on
the physical scene cabinet; no TIA or live-PLC work was performed.

Demo 4's moving carton no longer begins at an exposed conveyor end inside the
room. The authored conveyor now crosses a full-height material-entry wall and
the carton resets inside the connected source room before emerging through the
framed opening. The lesson camera was moved to the cabinet side so the portal
is seen face-on instead of becoming a foreground wall slab; the adjacent
cabinet, scanner, pallet inventory, conveyor, and moving carton remain in the
working shot. The current normal-renderer evidence
`rungproof-next/outputs/demo4-wall-infeed-cabinet-side.png` was produced through
the real scene action path: machine Start, TYPE OK, COUNT REQ, and NEW PALLET.
It shows local runtime RUNNING, COUNT 1, the running indicator on, and the
carton emerging from the wall opening. The focused build passed with 0 warnings
and 0 errors, and the visible Demo 4 app was restarted on the change (PID 3804,
responsive). A current Demo 3 room review also exists at
`rungproof-next/outputs/demo3-current-room-review.png`; it shows the corrected
full enclosure and conveyor portal rather than the former short freestanding
wall. No TIA work or broad parity suite ran.

Demo 5 no longer makes replacement cartons appear instantly at the gantry
pickup after each placement. Its device-local magazine now advances each later
carton from the connected source-room conveyor to the pickup table while the
PLC remains the sole owner of gantry/vacuum sequence transitions. The conveyor
crosses a full-height framed infeed wall, and the close low cabinet-side camera
keeps that opening, gantry, pallet, HMI, and every physical control in one
readable machine view. The focused build passed with 0 warnings and 0 errors.
The real `operator-demo5-active` route then completed both four-carton layers
and its Stop, simulated-trip, and pallet-permissive recovery exercise without a
modal popup. The inspected 1600x900 normal-renderer capture
`rungproof-next/outputs/demo5-wall-infeed-two-layer-pass.png` shows PASSED,
PLACED 4, STEP 100, zeroed X/Y/Z feedback at home, four physical cartons on the
pallet, and local runtime still RUNNING. The visible app was restarted on Demo
5 with this build (PID 16404, responsive). This was a single focused Demo 5 run;
no broad suite, TIA work, or live-PLC claim was made.

Demo 1 was rechecked specifically against the earlier report that it would not
load. A fresh normal-renderer launch loaded the authored bench and its four
physical scene devices: PULSE, threshold lamp, RESET, and COUNT, with no side
input bank and no completion popup. The real `operator-demo1-active` route then
completed three distinct pulses, machine Reset to zero, and three fresh pulses.
The inspected 1600x900 capture
`rungproof-next/outputs/demo1-current-reset-repeat-pass.png` shows PASSED,
COUNT 3, the threshold lamp visibly on, and local runtime still RUNNING. This
was a focused Demo 1 load/operation check rather than a broad suite. The visible
app was reopened directly on Demo 1 (PID 38780, responsive); no TIA or live-PLC
work was performed.

Demo 2 now has current physical-layout and operating evidence. REQUEST, the
timed lamp, machine Stop, machine Reset, and ELAPSED MS are all installed on one
close-view commissioning bench; there are no floor-mounted pushbutton stands,
side lesson controls, or completion popup. The first focused run exposed that
the generic screenshot countdown could save the asynchronous lesson at ET 1960
ms before the 2000 ms TON and recovery sequence completed. Demo 1 and Demo 2
now publish explicit prepared states to the capture path, which refuses to save
until their production control sequences qualify. After the fix, the real
`operator-demo2-active` route operated REQUEST, Stop, Reset, REQUEST off, and
REQUEST on, then saved only after the repeated TON completed. The inspected
1600x900 capture
`rungproof-next/outputs/demo2-current-stop-reset-repeat-passed.png` shows PASSED,
ELAPSED MS 2000, the timed lamp visibly on, and local runtime RUNNING. The build
passed with 0 warnings and 0 errors, and the visible app was reopened on Demo 2
(PID 31032, responsive). This was a focused Demo 2 run; no broad suite, TIA, or
live-PLC work ran.

Demo 3 now has current focused operating evidence on the corrected enclosed
production-room scene. The real `operator-demo3-active` route sorted the 420,
820, and 600 mm cartons, including the 600 mm boundary, and exercised machine
Stop, simulated safety trip, drive overload, Reset, and a fresh Start through
the physical cabinet controls. The side rail contains lesson identity and
runtime status only; it has no duplicate lesson-input controls or completion
popup. The inspected 1600x900 normal-renderer capture
`rungproof-next/outputs/demo3-current-sort-recovery-pass.png` shows PASSED,
local runtime RUNNING, the complete connected room envelope and conveyor
portal, and the in-scene control cabinet. No source changed during this focused
run, so the build was not repeated; the immediately preceding source build had
passed with 0 warnings and 0 errors. The visible app was reopened directly on
Demo 3 (PID 40184, responsive). No broad suite, TIA, or live-PLC work ran.

Demo 4 was rerun after the scene-control and material-entry corrections using
the real `operator-demo4-active` production route. The route operated machine
Start, TYPE OK, COUNT REQ, five momentary NEW PALLET events, machine Reset, a
fresh Start, and the first valid event of batch two. The inspected 1600x900
normal-renderer capture
`rungproof-next/outputs/demo4-current-visible-motion-pass.png` shows PASSED,
COUNT 1 after the required batch Reset, a new carton physically emerging from
the full-height wall opening, and four completed cartons visibly stacked on the
pallet. Every learner control is on the adjacent cabinet; the side rail is
status-only and there is no completion popup. No source changed during this
focused run, so the build was not repeated. The visible app was reopened
directly on Demo 4 (PID 8008, responsive). No broad suite, TIA, or live-PLC
work ran.

The corrected curriculum was then exercised once end-to-end through the real
`operator-five-demo-active` production route. The first attempt used a
600-frame capture shutdown limit and reached Demo 5 before the renderer exited;
no completion image was produced, so that timeout is not acceptance evidence.
The same bounded route was rerun with sufficient capture time and completed all
five lessons in one session: Demo 1 count/reset/repeat, Demo 2 TON and
Stop/Reset/repeat, Demo 3 sorting plus Stop/trip/overload recovery, Demo 4
five-item batch/Reset/restart, and Demo 5 two-layer placement plus
Stop/trip/pallet-permissive recovery. The inspected 1600x900 normal-renderer
capture `rungproof-next/outputs/five-demo-current-end-to-end-pass.png` shows
Demo 5 PASSED, curriculum 5/5 PASSED, PLACED 4, X/Y/Z feedback at 0, STEP 100,
four cartons physically on the pallet, local runtime RUNNING, scene-mounted
controls, and a status-only side rail. This is current local offline/runtime
and rendered-GUI evidence; it is not TIA or live-PLC evidence.

The ladder workspace now has a persistent whole-application Interface Scale
control, separate from its existing ladder-diagram zoom. View offers Auto,
75%, 85%, 100%, and 115%; the same selection is available in Application
Settings. Auto resolves from the actual client resolution and uses 85% for the
default 1600x900 window, which removes the cramped fixed-density presentation
without stretching a fixed design canvas. The focused policy regression, clean
build, real-window density verifier, and inspected normal-renderer capture
`rungproof-next/outputs/ladder-auto-scale-1600x900-after.png` confirm the current
behavior. This is local RungProof GUI evidence, not installed-TIA parity.

The TIA-facing LAD editor chrome is now reduced to the document tab and one
compact instruction strip. The redundant full project breadcrumb, duplicate
block-title band, separate selection/help row, and separate ladder-zoom row no
longer consume space above the networks. Instruction categories, the active
category's commands, and ladder zoom share one horizontal toolbar; long
instruction groups scroll horizontally instead of wrapping into additional
rows. The clean build, 1600x900 density verifier, and inspected normal-renderer
capture `rungproof-next/outputs/tia-compact-ladder-toolbar.png` confirm the
current local GUI. This does not claim a matched installed-TIA capture or live
PLC behavior.


### Screen audit remediation checkpoint —2026-10-22

Current scope and evidence are maintained in
[`SCREEN_AUDIT_REMEDIATION_PLAN.md`](rungproof-next/docs/SCREEN_AUDIT_REMEDIATION_PLAN.md)
and its linked audit ledger/index. All77 scenes have a tested normal-route
disposition:67 bounded normal reviews, five blocked legacy routes and five
partial demo workflows. This is not full functional acceptance; independent
case gaps and remaining native fix retests remain open. The original fume,
common-stop, supplied-reference and2.11/2.12 plant subcases have bounded native
verification; both tank request routes now have reviewed flow/lifecycle/scaling
evidence. Documentation inventory/pulse-procedure fixes are source-verified.
Visionclass2/3/4 selection/routing is verified; exact native
invalid-author-Run diagnostic reproduction remain pending. Do not classify the
weak earlier navigation candidate as a confirmed Discard callback defect.

Final B5 build is clean; full controller checks report424PASS/2existingFAIL
(watch schema/TIA evidence hash), not a green suite. Historical app-shell
failure remains recorded despite its targeted diagnostics subcheck passing.
Current root-confirmed GUI44956/bridgePID81220 is live; handles and active work
belong to audit SESSION-STATE.json, not this durable note. Preserve unrelated
dirty work and all historical failure/exclusion evidence; no full-goal
completion, installed-TIA parity or live-PLC proof is claimed.


