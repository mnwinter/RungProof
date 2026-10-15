# Motor aggregate interface implementation plan

Current-source audit, 2026-10-07. This is a scoped implementation design and acceptance queue, not an implementation or verification claim.

## Requirements and source evidence

Lab 10.5's original contract describes a motor record combining command, power, temperature and alarm fields (`../../prototype/scenes/lab-10-05-motor-struct-data.plcscene`). Its actual original and migrated points are scalar BOOLs. The current migrated description and help correctly disclose that gap. Three display housings show NO DATA; inventing numbers or renaming BOOLs does not satisfy the record requirement.

Lab 10.6 requires ten motors with staggered startup and group alarm handling. Current individual `motor_0_run` through `motor_9_run` commands and the retained group BOOL drive actual independent visual shafts. `Main.MotorArrayWorkflowReview.cs` supplies a separate 500 ms staggered QA controller, not an ARRAY-valued interface. The existing native timed-stage evidence proves scalar sequencing only.

`PlcVariableType` in `LadderProgram.cs` contains Bool/Int/DInt/Real/Timer/Counter. `LadderCompiler.cs` rejects other kinds. `SceneIoBindingValidator.TypesCompatible`, `SceneIoImageMapper`, and the scene runtime sampling/commit APIs accept scalar values only. The controller snapshot has separate Boolean and numeric maps. The editor project format is schemaVersion 1. Therefore the missing interfaces cross controller, persistence, scene and editor boundaries; scene JSON alone cannot implement them.

## Proposed first implementation

1. Add a renderer-neutral aggregate type descriptor with ordered STRUCT fields and fixed-bound ARRAY element descriptors. Scalar descriptors reuse current PlcVariableType. Store schema and initial aggregate values on declared tags; reject duplicate fields, cycles, unknown types, invalid bounds and mismatched initial shapes before load. Prefer additive optional metadata for old schemaVersion 1 documents, with explicit version handling if any incompatible serialization becomes necessary.
2. Resolve member paths such as `motor.temperature` and constant array paths such as `motors[0].run` through one compiler resolver. Preserve the current direct-name lookup and TIMER/COUNTER member behavior. A path must resolve against a declared aggregate: a dotted scalar name must not become an undeclared record. Report unresolved members, out-of-range indices and BOOL/numeric mismatches at the operand's source location.
3. Compile aggregate leaves into the existing typed scalar execution image while retaining their aggregate schema and parent identity. This preserves deterministic scalar ladder execution without pretending that ten unrelated BOOL declarations form an ARRAY. Reconstruct immutable aggregate snapshots at scan boundaries. Copy and validate input aggregates before scan; commit complete output aggregates after scan. Do not expose partially updated records to scene consumers. Whole-record MOVE, dynamic indexing and arbitrary nesting may be later features only if clearly rejected now; the first motor lessons need constant element/member operands and leaf contacts/coils/comparisons.
4. Bind aggregates through schema-checked scene points. Keep input feedback and output command records separate: an aggregate cannot mix PC and PLC ownership under one root. Reject overlapping whole-root/member writers. Retain all existing legacy scalar scene points and commands, with documented arbitration before adding a second writer path.
5. Extend tag creation/editing, operand selection, watch display and both TIA/Studio renderings to present parent/field/element structure. Save/open, Undo/Redo, rename, type-change and delete must preserve references or report affected operands. A headless declaration that users cannot author or inspect does not close the editor requirement.
6. Implement motor scene projection only after the type seam is supported. For Lab 10.5 propose separate `motorFeedback` STRUCT (valid, temperatureValid, alarmClear, power, temperature) and `motorCommand` STRUCT (enable, recordReady); field names are a design proposal, not an existing contract. Original contract provides no units, numeric ranges or thermal/power dynamics. Initially expose operator-controlled educational values, clearly marked simulated; specify units/ranges and validity behavior in the scene help before displaying numbers. Do not assert measured motor feedback from a command bit.
7. For Lab 10.6 use a genuine fixed-bound command ARRAY [0..9] of BOOL, plus separate per-motor feedback only if a feedback model is implemented. Preserve the existing group override and scalar commands for old projects, with an explicitly documented OR rule matching current legacy behavior; native acceptance must prove aggregate-only, scalar-only and combined operation. Keep the educational solution separate from the exercise scene.

## Owned implementation boundaries

- Aggregate descriptor/path resolver/new pure tests: new files under src/VirtualController and tests; one owner.
- Declaration/compile/runtime/session/mapper/persistence: LadderProgram.cs, LadderCompiler.cs, VirtualControllerRuntime.cs, VirtualControllerSession.cs, SceneIoBindingValidator.cs, SceneIoImageMapper.cs, LadderProgramJson.cs, LadderEditorDocument.cs, LadderEditorProjectJson.cs; one coordinated owner.
- Native authoring/rendering: SimulatorShell.cs; coordinator owns it and must sequence these changes with current workflow repairs.
- Scene sampling and external contract: SceneSimulationRuntime.cs plus Connections/ExternalSceneContract and consumers need audit before enabling aggregate points. External PLC wire layout is outside this offline repair: no inferred byte offsets, packing or live transport changes.
- Motor projection: dedicated partial runtime file, two migrated motor scene JSON files and two corresponding help files; scene owner.

No shared-file edits were made during this audit. No build or test was run because the coordinator owns the active native/output slot.

## Deterministic acceptance gates

- Existing scalar projects save/open and execute unchanged; full existing controller regression passes.
- STRUCT containing BOOL/INT/DINT/REAL leaves round-trips schema, values, bindings and watch paths; malformed values and nonfinite REALs fail before load.
- ARRAY [0..9] of BOOL round-trips and compiles contacts/coils at indices 0 and 9. Indices -1/10 and noninteger syntax fail visibly; writing one element preserves all others.
- Duplicate fields, recursive type declarations, incompatible aggregate shapes, ownership errors and overlapping writers are rejected with specific diagnostics.
- TIMER/COUNTER member access and literal dotted scalar names remain backward compatible.
- Input snapshots are immutable and sampled once per accepted scan; aggregate output commits are atomic; Stop clears output leaves and Reset restores all declared initial leaves without retaining old nested references.
- Motor STRUCT fixture verifies valid/invalid record, each permissive loss independently, numeric display bindings and command/feedback distinction.
- Motor ARRAY fixture verifies all ten 500 ms stages, individual off/on, alarm loss next scan, Stop, Reset and legacy group arbitration using real ARRAY operands.

## Coordinator native verification queue

Reuse one native window after rebuilding in the coordinator's reserved slot. First author an aggregate from the actual tag editor, use a member contact and coil, save, reopen and Verify + Load. Inspect both TIA and Studio styles, operand dropdown readability, watch expansion, resizing, Undo/Redo and unresolved references. Test invalid array bounds and member type mismatch through normal authoring; errors must be visible and must not replace the currently running valid program.

Lab 10.5: run a separate aggregate QA ladder; inspect readable power/temperature/record displays and their simulated units at stopped, valid and invalid conditions. Lose each permissive independently; verify enable/recordReady and actual motor shaft state. Stop and Reset; verify cleared commands and restored input defaults. Inspect FR/FL/RL/RR/Top and focused readouts, retaining the supported display geometry.

Lab 10.6: run aggregate QA with legacy group command false; observe every stage 0.50 through 5.00 s, one new motor per stage. Inspect all ten shafts and current aggregate element values, not only a total command count. Exercise alarm loss, Stop, Reset, scalar compatibility and explicit group override. Repeat FR/FL/RL/RR/Top at completed and reset states. Keep command panels labelled commands unless actual feedback is separately modeled.

Sizing: approximately 1-2 focused implementation days for the aggregate/controller/editor seam, followed by native acceptance and any discovered fixes. The estimate assumes fixed constant indexing and no real PLC packing; it is uncertain until the implementation owner sizes the editor/persistence changes. These two open implementation rows must stay open until these gates pass.

## Backend implementation checkpoint

Implemented first slice during this parallel task: explicit Struct/Array PlcVariable types, retained aggregate schema on parent declarations, fixed scalar leaf expansion, compiler member/constant-index resolution, immutable reconstructed Snapshot.Aggregates, whole-input validation before aggregate scans, exact scene root schema binding validation, root input/output mapper APIs, editor document AddAggregateTag and project/program persistence. Existing normal scalar scan execution also reconstructs parent values. Nested aggregate leaves, whole-root instructions and dynamic indexing remain unsupported and rejected.

Standalone isolated controller run passed 147 tests, 0 failed (145 previous tests plus two grouped aggregate cases), `.tools/motor-aggregate-tests/test.log`; no real PLC transport constructed or connection attempted. Earlier build attempts exposed a missing LINQ import and incorrect test result property; both repaired. Isolated outputs also exposed twelve fixture-path failures, fixed by walking ancestors to RungProof.Next.csproj instead of relying on four fixed parents. The final full run passed.

This supersedes the earlier audit's no-shared-edits statement. No SimulatorShell.cs or SceneSimulationRuntime.cs edits were made by the motor agent. Scene decoder/projector and actual native authoring remain required; motor rows stay open.

Coordinator seam: SceneIoPoint now has optional Aggregate schema. Parse the scene root JSON type and schema before registering points; call PlcAggregates.ValidateSchema and validate initial values via Expand. Add PC-owned aggregate sampling (root objects) and PLC-owned complete-root application: map with SampleAggregateInputs/CommitAggregateOutputs, validate all shapes and ownership before mutating any scene root, and rebuild canonical leaf values together before ApplyBindings. Never infer aggregate packing in ExternalSceneContract/live transport. A complete typed controller fixture can already use motors[0] and motorFeedback.valid with the new backend.

## Typed motor scene and audit integration

Both migrated motor scenes now declare actual STRUCT/ARRAY roots with camelCase schema metadata parsed through PlcAggregates.ParseSchema. The scene partial registers typed leaves for existing scalar scan transport, reconstructs root values before binding projection, validates complete-root output commits, and exposes explicit PC fixture input. Motor STRUCT numeric displays report user/fixture values with validity and no assumed units; no physical motor process was added. Motor command STRUCT projects through separate SIM effective OR points, preserving legacy PLC commands. ARRAY root element bindings combine with retained independent/group commands without rewriting root values.

Focused new Main.MotorAggregateWorkflowReview.cs audit prepares typed native QA projects and covers normal controller scan ownership, fixture numeric displays, independent permissive losses, lifecycle, all ten 500 ms ARRAY stages, alarm, malformed root rejection and scalar compatibility projection. Coordinator owns dispatcher/hooks and executes the Godot audit. Latest isolated pure controller suite remains 147 passed/0 failed; actual rendered acceptance is still pending.

## Structured tag authoring checkpoint

New AggregateTagEditor provides native structured field/type/initial controls and ARRAY element type/bounds/individual initial rows. Tag add/apply/binding/select handlers now retain schema; leaf operands appear in real contact/coil/numeric dropdowns with parent/type tooltips. ARRAY headers and field rows wrap in narrow drawers. Invalid UpdateTag validation runs before any reference/watch renaming; pure regression proved rejected malformed shape leaves serialized work unchanged.

SimulatorShell.VerifyAggregateTagEditor exercises actual widget signal handlers, typed BOOL/REAL STRUCT creation, ARRAY creation, leaf operand contents, apply rename, Undo/Redo, persistence and invalid initial rejection. Initial isolated run passed all except operand checks because their query searched Workbench while selectors live in its sibling properties overlay; corrected the query to the TIA environment parent, keeping actual dropdown-content assertions. Coordinator must rerun it and retain the real native screen acceptance queue. Motor readout audit found a real display mismatch (staticText configs lacked NumericReadout); changed temperature/power housings to numeric configs and asserted actual NumericReadout. Coordinator's integrated motor audit then passed before structured UI continuation.
